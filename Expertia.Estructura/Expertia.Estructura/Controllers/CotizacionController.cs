﻿using Expertia.Estructura.Controllers.Base;
using Expertia.Estructura.Models;
using Expertia.Estructura.Models.Auxiliar;
using Expertia.Estructura.Repository.Behavior;
using Expertia.Estructura.Repository.Condor;
using Expertia.Estructura.Repository.DestinosMundiales;
using Expertia.Estructura.RestManager.Base;
using Expertia.Estructura.RestManager.RestParse;
using Expertia.Estructura.Utils;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace Expertia.Estructura.Controllers
{
    [RoutePrefix(RoutePrefix.Cotizacion)]
    public class CotizacionController : BaseController<Cotizacion>
    {
        #region Properties
        private ICotizacion_CT _cotizacionRepository_CT;
        private ICotizacion_DM _cotizacionRepository_DM;
        #endregion

        #region PublicMethods
        [Route(RouteAction.Read)]
        public IHttpActionResult Read(CotizacionRequest cotizacionRequest)
        {
            var error = string.Empty;
            try
            {
                if (RepositoryByBusiness(cotizacionRequest.Region.ToUnidadNegocioByCountry()) != null)
                {
                    var cotizacionResponse = _cotizacionRepository_CT.GetCotizaciones(cotizacionRequest);
                    var codigoRetorno = cotizacionResponse[OutParameter.CodigoError].ToString();
                    var mensajeRetorno = cotizacionResponse[OutParameter.MensajeError].ToString();
                    var data = (IEnumerable<CotizacionResponse>)cotizacionResponse[OutParameter.CursorCotizacion];
                    return Ok(new { codigoRetorno, mensajeRetorno, data });
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return InternalServerError(ex);
            }
            finally
            {
                (new
                {
                    Error = error,
                    Body = cotizacionRequest
                }).TryWriteLogObject(_logFileManager, _clientFeatures);
            }
        }

        [Route(RouteAction.Send)]
        public IHttpActionResult Send(UnidadNegocio unidadNegocio)
        {
            IEnumerable<Cotizacion_DM> cotizaciones = null;
            var exceptionMsg = string.Empty;
            try
            {
                var _unidadNegocio = unidadNegocio.Descripcion.ToUnidadNegocio();
                RepositoryByBusiness(_unidadNegocio);
                cotizaciones = (IEnumerable<Cotizacion_DM>)_cotizacionRepository_DM.GetCotizaciones()[OutParameter.CursorCotizacionDM];
                if (cotizaciones == null || cotizaciones.ToList().Count.Equals(0)) return Ok(cotizaciones);
                
                var cotizaciones_SF = new List<object>();
                foreach (var cotizacion in cotizaciones)
                {
                    cotizaciones_SF.Add(cotizacion.ToSalesforceEntity());
                }

                /// Obtiene Token para envío a Salesforce
                var token = RestBase.GetTokenByKey(SalesforceKeys.AuthServer, SalesforceKeys.AuthMethod, Method.POST);

                /// II. Enviar Oportunidad a Salesforce
                try
                {
                    
                    var responseOportunidad = RestBase.ExecuteByKey(SalesforceKeys.CrmServer, SalesforceKeys.OportunidadMethod, Method.POST, cotizaciones_SF, true, token);
                    if (responseOportunidad.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        dynamic jsonResponse = new JavaScriptSerializer().DeserializeObject(responseOportunidad.Content);
                        try
                        {
                            var responseList = jsonResponse["Cotizaciones"]; // Obtiene todo el json
                            foreach (var response in (List<object>)responseList)
                            {
                                #region Deserialize
                                dynamic item = new JavaScriptSerializer().DeserializeObject(response.Stringify());

                                var codigoRetorno = item["CODIGO_RETORNO"].ToString();
                                var mensajeRetorno = item["MENSAJE_RETORNO"].ToString();
                                var idCotizacionSf = item["ID_COTIZACION_SF"].ToString();
                                var idOportunidadSf = item["ID_OPORTUNIDAD_SF"].ToString();
                                var idCotizacion = item["COTIZACION"].ToString();

                                var cotizacion = cotizaciones.SingleOrDefault(c =>
                                    c.IdCotizacionSf.Equals(idCotizacionSf) &&
                                    c.IdOportunidadSf.Equals(idOportunidadSf) &&
                                    c.IdCotizacion.Equals(idCotizacion));

                                cotizacion.CodigoError = codigoRetorno;
                                cotizacion.MensajeError = mensajeRetorno;
                                #endregion

                                #region ReturnToDB

                                #endregion
                            }
                        }
                        catch
                        {
                        }
                    }
                }
                catch (Exception ex)
                {
                    exceptionMsg = ex.Message;
                    //cotizacion.CodigoError = ApiResponseCode.ErrorCode;
                    //cotizacion.MensajeError = ex.Message;
                }

                

                return Ok(cotizaciones);
            }
            catch (Exception ex)
            {
                exceptionMsg = ex.Message;
                return InternalServerError(ex);
            }
            finally
            {
                (new
                {
                    Error = exceptionMsg
                }).TryWriteLogObject(_logFileManager, _clientFeatures);
            }
        }
        #endregion

        #region Auxiliar
        private void LoadResults(UnidadNegocioKeys? unidadNegocio, out object logResult, out object result)
        {
            switch (unidadNegocio)
            {
                case UnidadNegocioKeys.DestinosMundiales:
                    #region Log
                    logResult = new
                    {
                        Result = new
                        {
                            DestinosMundiales = new
                            {
                                Codes = GetErrorResult(UnidadNegocioKeys.DestinosMundiales),
                                IdCuenta = _operCollection[UnidadNegocioKeys.DestinosMundiales][OutParameter.IdCuenta].ToString(),
                                IdCotizacion = _operCollection[UnidadNegocioKeys.DestinosMundiales][OutParameter.IdCotizacion].ToString()
                            }
                        }
                    };
                    #endregion
                    #region Client
                    result = new
                    {
                        Result = new
                        {
                            DestinosMundiales = new
                            {
                                CodigoError = _operCollection[UnidadNegocioKeys.DestinosMundiales][OutParameter.CodigoError].ToString(),
                                MensajeError = _operCollection[UnidadNegocioKeys.DestinosMundiales][OutParameter.MensajeError].ToString(),
                                IdCuenta = _operCollection[UnidadNegocioKeys.DestinosMundiales][OutParameter.IdCuenta].ToString(),
                                IdCotizacion = _operCollection[UnidadNegocioKeys.DestinosMundiales][OutParameter.IdCotizacion].ToString()
                            }
                        }
                    };
                    #endregion
                    break;
                default:
                    logResult = null; result = null;
                    break;
            }
        }

        protected override UnidadNegocioKeys? RepositoryByBusiness(UnidadNegocioKeys? unidadNegocioKey)
        {
            switch (unidadNegocioKey)
            {
                case UnidadNegocioKeys.DestinosMundiales:
                    _cotizacionRepository_DM = new Cotizacion_DM_Repository(UnidadNegocioKeys.DestinosMundiales);
                    break;
            }
            return unidadNegocioKey;
        }
        #endregion

        #region SalesforceEntities
        private object ToCotizacionEntity(Cotizacion cotizacion)
        {
            try
            {
                return new
                {
                    info = new
                    {
                        CodigoRetorno = "OK",
                        MensajeRetorno = "Mensaje Retorno",
                        Grupo = cotizacion.Grupo
                    }
                };
            }
            catch
            {
                throw;
            }
        }
        #endregion
    }
}

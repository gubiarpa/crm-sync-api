﻿using Expertia.Estructura.Controllers.Base;
using Expertia.Estructura.Models;
using Expertia.Estructura.Models.Auxiliar;
using Expertia.Estructura.Models.Journeyou;
using Expertia.Estructura.Repository.Journeyou;
using Expertia.Estructura.RestManager.Base;
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
    [RoutePrefix(RoutePrefix.CotizacionJourneyou)]
    public class CotizacionJYController : BaseController
    {
        private CotizacionJYRepository _cotizacionRepository;

        #region PublicMethods
        [Route(RouteAction.Read)]
        public IHttpActionResult Read(Cotizacion_JY cotizacion)
        {
            try
            {
                if (RepositoryByBusiness(cotizacion.Region.ToUnidadNegocioByCountry()) != null)
                {
                    var operation = _cotizacionRepository.GetCotizaciones(cotizacion);
                    var cotizaciones = (List<CotizacionJYResponse>)operation["P_CUR_COTIZACION_ASOCIADA"];
                    if (cotizaciones.Count.Equals(0)) return NotFound();

                    var cotizacionResponse = cotizaciones.ElementAt(0);
                    cotizacionResponse.CodigoError = operation[OutParameter.CodigoError].ToString();
                    cotizacionResponse.MensajeError = operation[OutParameter.MensajeError].ToString();
                    
                    return Ok(cotizacionResponse);
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }




        [Route(RouteAction.Send)]
        public IHttpActionResult Send(UnidadNegocio unidadNegocio)
        {
            try
            {
                var _unidadNegocioKey = RepositoryByBusiness(unidadNegocio.Descripcion.ToUnidadNegocioByCountry());

                var operation = _cotizacionRepository.Lista_CotizacionB2C();
                Respuesta Rpta = new Respuesta();
                Rpta.CodigoError = operation[OutParameter.CodigoError].ToString();
                Rpta.MensajeError = operation[OutParameter.MensajeError].ToString();
                var cotizacionJYUpdResponse = ((List<CotizacionJYUpdResponse>)operation[OutParameter.CursorCotizacionB2C]);

                /// Obtiene Token para envío a Salesforce
                var authSf = RestBase.GetToken();
                var token = authSf[OutParameter.SF_Token].ToString();
                var crmServer = authSf[OutParameter.SF_UrlAuth].ToString();

                /// Envío de cotizacion a Salesforce
                var cotizacionSF = new List<object>();
                foreach (var cotizacion in cotizacionJYUpdResponse)
                {
                    cotizacionSF.Add(ToSalesforceEntity(cotizacion));
                }

                try
                {
                    ClearQuickLog("body_request.json", "CotizacionJY"); /// ♫ Trace
                    var objEnvio = new { cotizaciones = cotizacionSF };
                    QuickLog(objEnvio, "body_request.json", "CotizacionJY"); /// ♫ Trace
                    var response = RestBase.ExecuteByKeyWithServer(crmServer, SalesforceKeys.CotizacionJYUpdMethod, Method.POST, objEnvio, true, token);
                    if (response.StatusCode.Equals(HttpStatusCode.OK))
                    {
                        dynamic jsonResponse = new JavaScriptSerializer().DeserializeObject(response.Content);

                        foreach (var Cotizacion in cotizacionJYUpdResponse)
                        {
                            foreach (var jsResponse in jsonResponse["Cotizaciones"])
                            {
                                if (Cotizacion.ID_OPORTUNIDAD_SF == jsResponse["ID_OPORTUNIDAD_SF"] && Cotizacion.ID_COTIZACION_SF == jsResponse["ID_COTIZACION_SF"])
                                {

                                    var cotizacionJYUpd = new CotizacionJYUpd()
                                    {
                                        Cotizacion = jsResponse[OutParameter.SF_Cotizacion],
                                        File = jsResponse[OutParameter.SF_File_SubFile],
                                        Es_Atencion = jsResponse[OutParameter.SF_CodigoRetorno],
                                        Descripcion = jsResponse[OutParameter.SF_MensajeRetorno]
                                };

                                    /// Actualización de estado de subcodigo a PTA
                                    operation = _cotizacionRepository.Actualizar_EnvioCotizacionB2C(cotizacionJYUpd);
                                    Rpta.CodigoError = operation[OutParameter.CodigoError].ToString();
                                    Rpta.MensajeError = operation[OutParameter.MensajeError].ToString();
                                    Rpta.Numero_Afectados = operation[OutParameter.NumeroActualizados].ToString();

                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Rpta.CodigoError = ApiResponseCode.ErrorCode;
                    Rpta.MensajeError = ex.Message;
                }

                return Ok(Rpta);
            }
          
            
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        #endregion

        #region Auxiliar
        #endregion

        protected override UnidadNegocioKeys? RepositoryByBusiness(UnidadNegocioKeys? unidadNegocioKey)
        {
            if (unidadNegocioKey != null)
            {
                _cotizacionRepository = new CotizacionJYRepository(unidadNegocioKey);
            }
            return unidadNegocioKey; // Devuelve el mismo parámetro
        }


        private object ToSalesforceEntity(CotizacionJYUpdResponse Cotizacion)
        {
            try
            {
                return new{
                            Cotizacion.ID_OPORTUNIDAD_SF,
                            Cotizacion.ID_COTIZACION_SF,
                            Cotizacion.ID_CUENTA_SF,
                            Cotizacion.GRUPO,
                            Cotizacion.ESTADO,
                            Cotizacion.VENTA_ESTIMADA,
                            Cotizacion.FILE_SUBFILE,
                            Cotizacion.VENTA_FILE,
                            Cotizacion.MARGEN_FILE,
                            Cotizacion.PAXS_FILE,
                            Cotizacion.ESTADO_FILE,
                            FECHA_INICIO_VIAJE = Cotizacion.FECHA_INICIO_VIAJE.ToString().Substring(0,10),
                            Cotizacion.Cotizacion
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}

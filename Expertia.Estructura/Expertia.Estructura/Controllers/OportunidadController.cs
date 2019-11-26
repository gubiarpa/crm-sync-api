﻿using Expertia.Estructura.Controllers.Base;
using Expertia.Estructura.Models;
using Expertia.Estructura.Models.Auxiliar;
using Expertia.Estructura.Repository.AppWebs;
using Expertia.Estructura.Repository.Behavior;
using Expertia.Estructura.Repository.InterAgencias;
using Expertia.Estructura.RestManager.Base;
using Expertia.Estructura.Utils;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace Expertia.Estructura.Controllers
{
    [RoutePrefix(RoutePrefix.Oportunidad)]
    public class OportunidadController : BaseController<object>
    {
        #region Properties
        private IOportunidadRepository _oportunidadRepository;
        #endregion

        #region Constructor
        public OportunidadController()
        {
        }
        #endregion

        #region PublicMethods
        [Route(RouteAction.Send)]
        public IHttpActionResult Send(UnidadNegocio unidadNegocio)
        {
            IEnumerable<Oportunidad> oportunidades = null;
            try
            {
                var _unidadNegocio = GetUnidadNegocio(unidadNegocio.Descripcion);
                RepositoryByBusiness(_unidadNegocio);
                _instants[InstantKey.Salesforce] = DateTime.Now;

                /// I. Consulta de Oportunidades
                oportunidades = (IEnumerable<Oportunidad>)_oportunidadRepository.GetOportunidades()[OutParameter.CursorOportunidad];
                if (oportunidades == null || oportunidades.ToList().Count.Equals(0)) return Ok();

                /// Obtiene Token para envío a Salesforce
                var token = RestBase.GetTokenByKey(SalesforceKeys.AuthServer, SalesforceKeys.AuthMethod, Method.POST);

                //var oportunidadTasks = new List<Task>();
                foreach (var oportunidad in oportunidades)
                {
                    //var task = new Task(() =>
                    {
                        /// II. Enviar Oportunidad a Salesforce
                        try
                        {
                            oportunidad.UnidadNegocio = unidadNegocio.Descripcion;
                            oportunidad.CodigoError = oportunidad.MensajeError = string.Empty;
                            var oportunidadSf = ToSalesfoceEntity(oportunidad);
                            var responseOportunidad = RestBase.ExecuteByKey(SalesforceKeys.CrmServer, SalesforceKeys.OportunidadMethod, Method.POST, oportunidadSf, true, token);
                            if (responseOportunidad.StatusCode.Equals(HttpStatusCode.OK))
                            {
                                dynamic jsonResponse = new JavaScriptSerializer().DeserializeObject(responseOportunidad.Content);
                                oportunidad.CodigoError = jsonResponse[OutParameter.SF_CodigoError];
                                oportunidad.MensajeError = jsonResponse[OutParameter.SF_MensajeError];
                                
                                /// Actualización de estado de Oportunidad a PTA
                                var updateResponse = _oportunidadRepository.Update(oportunidad);
                                oportunidad.Actualizados = int.Parse(updateResponse[OutParameter.IdActualizados].ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            oportunidad.CodigoError = ApiResponseCode.ErrorCode;
                            oportunidad.MensajeError = ex.Message;
                        }
                    }//);
                    //task.Start();
                    //oportunidadTasks.Add(task);
                }

                //Task.WaitAll(oportunidadTasks.ToArray());
                return Ok(oportunidades);
            }
            catch (Exception ex)
            {
                oportunidades = null;
                throw ex;
            }
            finally
            {
                (new
                {
                    LegacySystems = oportunidades
                }).TryWriteLogObject(_logFileManager, _clientFeatures);
            }
        }
        #endregion

        #region Auxiliar
        #endregion

        #region SalesforceEntities
        private object ToSalesfoceEntity(Oportunidad oportunidad)
        {
            try
            {
                return new
                {
                    info = new
                    {
                        idOportunidad = oportunidad.IdOportunidad,
                        accion = oportunidad.Accion,
                        etapa = oportunidad.Etapa,
                        dkCuenta = oportunidad.DkCuenta.ToString(),
                        unidadNegocio = oportunidad.UnidadNegocio,
                        sucursal = oportunidad.Sucursal,
                        puntoVenta = oportunidad.PuntoVenta,
                        subcodigo = oportunidad.Subcodigo,
                        fechaOportunidad = oportunidad.FechaOportunidad.ToString("dd/MM/yyyy"),
                        nombreOportunidad = oportunidad.NombreOportunidad,
                        origenOportunidad = oportunidad.OrigenOportunidad,
                        medioOportunidad = oportunidad.MedioOportunidad,
                        gds = oportunidad.GDS,
                        tipoProducto = oportunidad.TipoProducto,
                        rutaViaje = oportunidad.RutaViaje,
                        ciudadOrigen = oportunidad.CiudadOrigen,
                        ciudadDestino = oportunidad.CiudadDestino,
                        tipoRuta = oportunidad.TipoRuta,
                        numPasajeros = oportunidad.NumPasajeros,
                        fechaInicioViaje1 = oportunidad.FechaInicioViaje1.ToString("dd/MM/yyyy"),
                        fechaFinViaje1 = oportunidad.FechaFinViaje1.ToString("dd/MM/yyyy"),
                        fechaInicioViaje2 = oportunidad.FechaInicioViaje2.ToString("dd/MM/yyyy"),
                        fechaFinViaje2 = oportunidad.FechaFinViaje2.ToString("dd/MM/yyyy"),
                        montoEstimado = oportunidad.MontoEstimado,
                        montoReal = oportunidad.MontoReal,
                        pnr1 = oportunidad.Pnr1,
                        pnr2 = oportunidad.Pnr2,
                        motivoPerdida = oportunidad.MotivoPerdida
                    }
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        protected override UnidadNegocioKeys? RepositoryByBusiness(UnidadNegocioKeys? unidadNegocioKey)
        {
            switch (unidadNegocioKey)
            {
                case UnidadNegocioKeys.Interagencias:
                    _oportunidadRepository = new Oportunidad_IA_Repository();
                    break;
                case UnidadNegocioKeys.AppWebs:
                    _oportunidadRepository = new Oportunidad_AW_Repository();
                    break;
            }
            return unidadNegocioKey;
        }
    }
}

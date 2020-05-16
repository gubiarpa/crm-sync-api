﻿using Expertia.Estructura.Controllers.Base;
using Expertia.Estructura.Models.Auxiliar;
using Expertia.Estructura.Models.NuevoMundo;
using Expertia.Estructura.Models.Retail;
using Expertia.Estructura.Repository.Behavior;
using Expertia.Estructura.Repository.General;
using Expertia.Estructura.Repository.Retail;
using Expertia.Estructura.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Expertia.Estructura.Controllers
{
    /// CRMC008 : Registro y actualización de Oportunidad de venta en Sistema Expertia
    [RoutePrefix(RoutePrefix.OportunidadVentaNM)]
    public class OportunidadVentaNMController : BaseController
    {
        #region Properties
        private IOportunidadVentaNMRepository _oportunidadVentaNMRepository;
        protected override ControllerName _controllerName => ControllerName.OportunidadVentaNM;
        private OportunidadRetailRepository _repository;
        private DatosUsuario _datosUsuario;
        #endregion

        #region PublicMethods
        [Route(RouteAction.Send)]
        public IHttpActionResult Create(OportunidadVentaNM oportunidadVentaNM)
        {
            string exMessage = string.Empty;
            int idCotizacion = 0;
            try
            {
                var intIdUsuWeb = oportunidadVentaNM.UsuarioCre;
                var usuarioLogin = _datosUsuario.Get_Dts_Usuario_Personal(oportunidadVentaNM.IdUsuarioSrv_SF);
                int? intIdCliCot = null;

                int intIdOcurrencias, pIntIdCot = 0;
                var objPersonal = (Personal)_repository.ObtienePersonalXId_TrustWeb(intIdUsuWeb)["pCurResult_out"];
                var objUsuarioWeb = (UsuarioWeb)_repository.ObtieneUsuarioWebXId(intIdUsuWeb)["pCurResult_out"];

                if (oportunidadVentaNM.IdCotSRV == null) // Nuevo SRV
                {
                    List<ClienteCot> clientes;

                    #region Get_HdIdCli
                    if ((new List<string> { "DNI", "PSP", "CEX" }).Contains(oportunidadVentaNM.IdTipoDoc))
                        clientes = (List<ClienteCot>)_repository.SelectByDocumento(oportunidadVentaNM.IdTipoDoc, oportunidadVentaNM.NumDoc)["pCurResult_out"];
                    else
                        clientes = (List<ClienteCot>)_repository.SelectByEmail(oportunidadVentaNM.EmailCli)["pCurResult_out"];
                    var hdIdCli = clientes != null && clientes.Count > 0 ? clientes.ElementAt(0).IdCliCot : 0;
                    #endregion

                    #region IdClienteCotización
                    if (hdIdCli.Equals(0)) // ◄ String.IsNullOrEmpty(Trim(hdIdCli.Value))
                    {
                        /// (i) Inserta Cliente
                        intIdCliCot = (int)(_repository.InsertaClienteCotizacion(
                            oportunidadVentaNM.NombreCli,
                            oportunidadVentaNM.ApePatCli,
                            oportunidadVentaNM.ApeMatCli,
                            oportunidadVentaNM.EmailCli,
                            null,
                            null,
                            oportunidadVentaNM.EnviarPromociones.Equals("1"),
                            null,
                            oportunidadVentaNM.NumDoc,
                            oportunidadVentaNM.IdTipoDoc,
                            oportunidadVentaNM.IdUsuarioSrv_SF,
                            Webs_Cid.ID_WEB_WEBFAREFINDER,
                            null,
                            false,
                            null,
                            null
                            )["pNumIdNewCliCot_out"]);

                        /// (ii) Inserta Teléfonos
                        /// (iii) Inserta Archivos
                    }
                    else
                    {
                        intIdCliCot = hdIdCli;
                    }
                    #endregion

                    _repository.ActualizaClienteCotizacion(
                        intIdCliCot ?? 0,
                        oportunidadVentaNM.NombreCli,
                        oportunidadVentaNM.ApePatCli,
                        oportunidadVentaNM.ApeMatCli,
                        oportunidadVentaNM.EmailCli,
                        oportunidadVentaNM.IdUsuarioSrv_SF
                        );

                    var idDestino = string.Empty;
                    if (!string.IsNullOrEmpty(oportunidadVentaNM.IdDestino) && oportunidadVentaNM.IdDestino.Length >= 3)
                        idDestino = oportunidadVentaNM.IdDestino.Substring(0, 3);

                    #region RegistraCotizacion
                    var intIdCotVta = (int)_repository.InsertaCotizacionVenta(
                        3,
                        null,
                        objPersonal.NomCompletoPer,
                        objUsuarioWeb.LoginUsuWeb,
                        Constantes_SRV.IP_GENERAL,
                        intIdCliCot ?? 0,
                        intIdUsuWeb,
                        objPersonal.IdDepartamento,
                        objPersonal.IdOficina,
                        Webs_Cid.ID_WEB_WEBFAREFINDER,
                        1,
                        short.Parse(oportunidadVentaNM.IdCanalVenta),
                        null,
                        idDestino,
                        null,
                        (short)ENUM_ESTADOS_COT_VTA.Solicitado,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null
                        )["pNumIdNewCot_out"];
                    #endregion

                    #region RegistraIngresoCliente
                    intIdOcurrencias = (int)_repository.InsertaIngresoCliente(
                        DateTime.Now,
                        oportunidadVentaNM.NombreCli,
                        oportunidadVentaNM.ApePatCli,
                        oportunidadVentaNM.ApeMatCli,
                        oportunidadVentaNM.EmailCli,
                        0, // ♫ IdMotivo pendiente
                        null,
                        oportunidadVentaNM.IdDestino,
                        oportunidadVentaNM.IdUsuarioSrv_SF,
                        usuarioLogin.IdOfi,
                        usuarioLogin.IdDep,
                        DateTime.Now,
                        oportunidadVentaNM.UsuarioCre,
                        objPersonal.IdOficina,
                        objPersonal.IdDepartamento,
                        oportunidadVentaNM.Comentario,
                        Constantes_SRV.IP_GENERAL,
                        intIdCotVta,
                        intIdCliCot,
                        oportunidadVentaNM.IdTipoDoc,
                        oportunidadVentaNM.NumDoc,
                        null
                        )["pNumIdOcurr_out"];
                    #endregion

                    #region EnvioPromociones
                    if (_repository.ValidarEnvioPromociones(intIdOcurrencias, oportunidadVentaNM.EnviarPromociones))
                    {
                        /*
                        _repository.EnviarPromociones(
                            objPersonal,
                            oportunidadRetail,
                            oportunidadRetail.EnviarPromociones.Equals("1"),
                            objPersonal.NomCompletoPer,
                            objPersonal.ApePatPer,
                            objPersonal.EmailPer);
                        */
                    }
                    #endregion

                    var strCommentAttCli = !string.IsNullOrEmpty(oportunidadVentaNM.Comentario) ?
                        "<br /><br /><strong>Comentarios: " + oportunidadVentaNM.Comentario + "</strong>" :
                        string.Empty;

                    #region InsertaPostCot
                    pIntIdCot = intIdCotVta;
                    string pStrTipoPost = Constantes_SRV.ID_TIPO_POST_SRV_USUARIO;
                    string pStrTextoPost = "Asignado por " + usuarioLogin.LoginUsuario + strCommentAttCli;
                    string pStrIPUsuCrea = Constantes_SRV.IP_GENERAL;
                    string pStrLoginUsuCrea = usuarioLogin.LoginUsuario;
                    int pIntIdUsuWeb = intIdUsuWeb;
                    int pIntIdDep = usuarioLogin.IdDep;
                    int pIntIdOfi = usuarioLogin.IdOfi;
                    List<ArchivoPostCot> pLstArchivos = null;
                    List<FilePTACotVta> pLstFilesPTA = null;
                    Int16 pIntIdEstado = (short)ENUM_ESTADOS_COT_VTA.Solicitado;
                    bool pBolCambioEstado = true;
                    ArrayList pLstFechasCotVta = null;
                    bool pBolEsAutomatico = false;
                    byte[] pBytArchivoMail = null;
                    bool pBolEsCounterAdmin = false;
                    int? pIntIdUsuWebCounterCrea = null;
                    int? pIntIdOfiCounterCrea = null;
                    int? pIntIdDepCounterCrea = null;
                    bool? pBolEsUrgenteEmision = null;
                    DateTime? pDatFecPlazoEmision = null;
                    Int16? pIntIdMotivoNoCompro = null;
                    string pStrOtroMotivoNoCompro = null;
                    double? pDblMontoEstimadoFile = null;

                    var intIdPost = _repository.Inserta_Post_Cot(
                        pIntIdCot,
                        pStrTipoPost,
                        pStrTextoPost,
                        pStrIPUsuCrea,
                        pStrLoginUsuCrea,
                        pIntIdUsuWeb,
                        pIntIdDep,
                        pIntIdOfi,
                        pLstArchivos,
                        pLstFilesPTA,
                        pIntIdEstado,
                        pBolCambioEstado,
                        pLstFechasCotVta,
                        pBolEsAutomatico,
                        pBytArchivoMail,
                        pBolEsCounterAdmin,
                        pIntIdUsuWebCounterCrea,
                        pIntIdOfiCounterCrea,
                        pIntIdDepCounterCrea,
                        pBolEsUrgenteEmision,
                        pDatFecPlazoEmision,
                        pIntIdMotivoNoCompro,
                        pStrOtroMotivoNoCompro,
                        pDblMontoEstimadoFile
                        );

                    if (pLstArchivos != null)
                    {
                        foreach (var objArchivoPost in pLstArchivos)
                        {
                            _repository.Inserta_Archivo_Post_Cot(
                                pIntIdCot,
                                intIdPost,
                                objArchivoPost.RutaArchivo,
                                objArchivoPost.NombreArchivo,
                                objArchivoPost.ExtensionArchivo,
                                objArchivoPost.Archivo
                                );
                        }
                    }


                    if (pBolCambioEstado)
                    {
                        if (pBolEsCounterAdmin)
                        {
                            _repository.Update_Estado_Cot_Vta(
                                pIntIdCot,
                                pStrLoginUsuCrea,
                                pStrIPUsuCrea,
                                pIntIdEstado,
                                pIntIdUsuWebCounterCrea.Value,
                                pIntIdDepCounterCrea.Value,
                                pIntIdOfiCounterCrea.Value,
                                pBolEsAutomatico,
                                pIntIdUsuWeb
                                );
                        }
                        else
                        {
                            _repository.Update_Estado_Cot_Vta(
                                pIntIdCot,
                                pStrLoginUsuCrea,
                                pStrIPUsuCrea,
                                pIntIdEstado,
                                pIntIdUsuWeb,
                                pIntIdDep,
                                pIntIdOfi,
                                pBolEsAutomatico,
                                null);
                        }
                    }

                    if (pIntIdEstado == 8 && pIntIdMotivoNoCompro.HasValue)
                    {
                        _repository.Update_MotivoNoCompro(pIntIdCot, pIntIdMotivoNoCompro, pStrOtroMotivoNoCompro);
                    }
                }
                #endregion
                else
                {
                    #region RegistraIngresoCliente
                    intIdOcurrencias = (int)_repository.InsertaIngresoCliente(
                        DateTime.Now,
                        objPersonal.NomCompletoPer,
                        objPersonal.ApePatPer,
                        objPersonal.ApeMatPer,
                        objPersonal.EmailPer,
                        0, // ♫ IdMotivo pendiente
                        null,
                        oportunidadVentaNM.IdDestino,
                        intIdUsuWeb,
                        objPersonal.IdOficina,
                        objPersonal.IdDepartamento,
                        null,
                        intIdUsuWeb,
                        objPersonal.IdOficina,
                        objPersonal.IdDepartamento,
                        oportunidadVentaNM.Comentario,
                        Constantes_SRV.IP_GENERAL,
                        null,
                        null,
                        oportunidadVentaNM.IdTipoDoc,
                        oportunidadVentaNM.NumDoc,
                        null
                        )["pNumIdOcurr_out"];
                    #endregion

                    #region EnvioPromociones
                    if (_repository.ValidarEnvioPromociones(intIdOcurrencias, oportunidadVentaNM.EnviarPromociones))
                    {
                        _repository.EnviarPromociones(
                            objPersonal,
                            new OportunidadRetailReq()
                            {
                               
                            },
                            oportunidadVentaNM.EnviarPromociones.Equals("SI"),
                            objPersonal.NomCompletoPer,
                            objPersonal.ApePatPer,
                            objPersonal.EmailPer);
                    }
                    #endregion
                }

                _repository.RegistraOportunidad(oportunidadVentaNM.IdOportunidad_SF, pIntIdCot);
                idCotizacion = pIntIdCot;
                var oportunidadRetailRes = new OportunidadRetailRes()
                {
                    CodigoError = "OK",
                    MensajeError = "Se agregó correctamente",
                    IdOportunidad_SF = oportunidadVentaNM.IdOportunidad_SF,
                    IdCotSrv = pIntIdCot,
                    FechaCreacion = DateTime.Now.ToString("dd/MM/yyyy")
                };

                return Ok(oportunidadRetailRes);
            }
            catch (Exception ex)
            {
                var oportunidadRetailResError = new OportunidadRetailRes()
                {
                    CodigoError = "ER",
                    MensajeError = ex.Message,
                    IdOportunidad_SF = oportunidadVentaNM.IdOportunidad_SF,
                    IdCotSrv = null,
                    FechaCreacion = DateTime.Now.ToString("dd/MM/yyyy")
                };

                exMessage = ex.Message;

                return Ok(oportunidadRetailResError);
            }
            finally
            {
                (new
                {
                    Body = oportunidadVentaNM,
                    IdCotSrv = idCotizacion,
                    Error = exMessage
                }).TryWriteLogObject(_logFileManager, _clientFeatures);
            }
        }
        #endregion

        #region Helpers
        private OportunidadRetailReq ToOportunidadRetailReq(OportunidadVentaNM oportunidadVentaNM)
        {
            return new OportunidadRetailReq();
        }

        protected override UnidadNegocioKeys? RepositoryByBusiness(UnidadNegocioKeys? unidadNegocioKey)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
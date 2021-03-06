﻿using Expertia.Estructura.Controllers.Base;
using Expertia.Estructura.Models;
using Expertia.Estructura.Repository.Behavior;
using Expertia.Estructura.Repository.Condor;
using Expertia.Estructura.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace Expertia.Estructura.Controllers
{
    [RoutePrefix(RoutePrefix.Ventas)]
    public class VentaController : BaseController<object>
    {
        #region Properties
        private IDictionary<UnidadNegocioKeys?, IVentaCT> _ventaCollection = new Dictionary<UnidadNegocioKeys?, IVentaCT>();

        protected override ControllerName _controllerName => ControllerName.Venta;
        #endregion

        #region PublicMethods
        [Route(RouteAction.Read)]
        public IHttpActionResult Read(VentasRequest ventasRequest)
        {
            var error = string.Empty;
            var data = new List<VentasRow>();
            try
            {
                var regiones = ventasRequest.Region.Split(Auxiliar.ListSeparator).ToList();
                var tasks = new List<Task<VentasResponse>>();
                foreach (var region in regiones)
                {
                    try
                    {
                        var unidadNeg = RepositoryByBusiness(region.ToUnidadNegocioByCountry());
                        tasks.Add(new Task<VentasResponse>(() =>
                        {
                            var ventasOper = _ventaCollection[unidadNeg].GetVentasCT(ventasRequest);
                            return new VentasResponse()
                            {
                                Region = region,
                                CodigoRetorno = ventasOper[OutParameter.CodigoError].ToString(),
                                MensajeRetorno = ventasOper[OutParameter.MensajeError].ToString(),
                                VentaRowList = (IEnumerable<VentasRow>)ventasOper[OutParameter.CursorVentas]
                            };
                        }));
                    }
                    catch
                    {
                    }
                }
                tasks.ForEach(t => t.Start());
                Task.WaitAll(tasks.ToArray());
                var result = new List<object>();
                tasks.ForEach(t =>
                {
                    result.Add(new { t.Result.Region, t.Result.CodigoRetorno, t.Result.MensajeRetorno });
                    data.AddRange(t.Result.VentaRowList); // Agrega lista
                });
                return Ok(new { result, data });
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
                    Body = ventasRequest
                }).TryWriteLogObject(_logFileManager, _clientFeatures);
            }
        }
        #endregion

        #region Auxiliar
        protected override UnidadNegocioKeys? RepositoryByBusiness(UnidadNegocioKeys? unidadNegocioKey)
        {
            _ventaCollection[unidadNegocioKey] = new Venta_CT_Repository(unidadNegocioKey);
            return unidadNegocioKey;
        }
        #endregion
    }
}

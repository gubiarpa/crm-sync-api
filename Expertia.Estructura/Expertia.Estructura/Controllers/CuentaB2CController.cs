﻿using Expertia.Estructura.Controllers.Base;
using Expertia.Estructura.Models;
using Expertia.Estructura.Utils;
using System;
using System.Web.Http;

namespace Expertia.Estructura.Controllers
{
    [RoutePrefix(RoutePrefix.CuentaB2C)]
    public class CuentaB2CController : BaseController<CuentaB2C>
    {
        #region PublicMethods
        [Route(RouteAction.Create)]
        public override IHttpActionResult Create(CuentaB2C entity)
        {
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route(RouteAction.Update)]
        public override IHttpActionResult Update(CuentaB2C entity)
        {
            try
            {
                return Ok();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
        #endregion

        #region Auxiliar
        protected override UnidadNegocioKeys? RepositoryByBusiness(UnidadNegocioKeys? unidadNegocioKey)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}

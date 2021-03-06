﻿using Expertia.Estructura.Controllers.Behavior;
using Expertia.Estructura.Filters;
using Expertia.Estructura.Repository.Behavior;
using Expertia.Estructura.Utils;
using Expertia.Estructura.Utils.Behavior;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Expertia.Estructura.Controllers.Base
{
    public abstract class BaseController : BaseController<object> { }

    [BasicAuthentication]
    public abstract class BaseController<T> : ApiController
    {
        #region Properties
        protected ILogFileManager _logFileManager;
        protected IClientFeatures _clientFeatures;
        protected IDictionary<UnidadNegocioKeys?, ICrud<T>> _crmCollection;
        protected IDictionary<UnidadNegocioKeys?, Operation> _operCollection;
        protected IDictionary<UnidadNegocioKeys?, bool> _operRetry;
        protected IDictionary<InstantKey, DateTime> _instants;
        protected abstract ControllerName _controllerName { get; }
        #endregion

        #region DatabaseError
        protected int _delayTimeRetry;
        #endregion

        #region Constructor
        public BaseController()
        {
            _logFileManager = new LogFileManager(LogKeys.LogPath, LogKeys.LogName, _controllerName);
            _clientFeatures = new ClientFeatures();
            _crmCollection = new Dictionary<UnidadNegocioKeys?, ICrud<T>>();
            _operCollection = new Dictionary<UnidadNegocioKeys?, Operation>();
            _operRetry = new Dictionary<UnidadNegocioKeys?, bool>();
            _delayTimeRetry = GetDelayRetryTime() * 1000;
            _instants = new Dictionary<InstantKey, DateTime>();
        }
        #endregion

        #region Auxiliar
        private int GetDelayRetryTime()
        {
            int delayInt;
            try
            {
                // (1) Intentamos leer el tiempo en el config
                var delayStr = ConfigAccess.GetValueInAppSettings(DbResponseCode.DelayRetryKey);

                // (2) Intentamos parsear a Int el tiempo leído
                if (!int.TryParse(delayStr, out delayInt)) throw new Exception();

                // (3) Validamos que sea un número no negativo
                if (delayInt < 0) throw new Exception();
            }
            catch
            {
                delayInt = DbResponseCode.DefaultDelay;
            }
            return delayInt;
        }

        protected object GetInstants()
        {
            try
            {
                return new
                {
                    Salesforce = _instants[InstantKey.Salesforce].ToString(FormatTemplate.LongDate),
                    Oracle = _instants[InstantKey.Oracle].ToString(FormatTemplate.LongDate)
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        protected void ClearQuickLog(string fileName, string path = "SF_Entities\\")
        {
            try
            {
                string _path = ConfigAccess.GetValueInAppSettings("QuickLogPath", "") + path + "\\";
                if (!System.IO.Directory.Exists(_path)) System.IO.Directory.CreateDirectory(_path);
                System.IO.File.WriteAllText(_path + fileName, string.Empty);
            }
            catch
            {
            }
        }

        protected void QuickLog(object obj, string fileName, string path = null, bool append = false, bool previousClear = false)
        {
            if (string.IsNullOrEmpty(path)) path = _controllerName.ToString();
            if (previousClear) ClearQuickLog(fileName, path);

            try
            {
                string _path = string.Format(ConfigAccess.GetValueInAppSettings("QuickLogPath", "") + path + "\\", _controllerName.ToString());
                if (!System.IO.Directory.Exists(_path)) System.IO.Directory.CreateDirectory(_path);

                if (append)
                    System.IO.File.AppendAllText(_path + fileName, obj.Stringify(true, false));
                else
                    System.IO.File.WriteAllText(_path + fileName, obj.Stringify(true, false));
            }
            catch
            {
            }
        }

        protected string GetMethod { get { return Request.RequestUri.Segments[Request.RequestUri.Segments.Length - 1]; } }

        protected UnidadNegocioKeys? GetUnidadNegocio(string unidadNegocioName)
        {
            if (ConfigAccess.GetValueInAppSettings(UnidadNegocioKeys.CondorTravel.GetKeyValues()).ToUpper().Split(Auxiliar.ListSeparator).ToList().Contains(unidadNegocioName.ToUpper()))
            {
                return UnidadNegocioKeys.CondorTravel;
            }
            else if (ConfigAccess.GetValueInAppSettings(UnidadNegocioKeys.DestinosMundiales.GetKeyValues()).ToUpper().Split(Auxiliar.ListSeparator).ToList().Contains(unidadNegocioName.ToUpper()))
            {
                return UnidadNegocioKeys.DestinosMundiales;
            }
            else if (ConfigAccess.GetValueInAppSettings(UnidadNegocioKeys.Interagencias.GetKeyValues()).ToUpper().Split(Auxiliar.ListSeparator).ToList().Contains(unidadNegocioName.ToUpper()))
            {
                return UnidadNegocioKeys.Interagencias;
            }
            else if (ConfigAccess.GetValueInAppSettings(UnidadNegocioKeys.AppWebs.GetKeyValues()).ToUpper().Split(Auxiliar.ListSeparator).ToList().Contains(unidadNegocioName.ToUpper()))
            {
                return UnidadNegocioKeys.AppWebs;
            }
            if (ConfigAccess.GetValueInAppSettings(UnidadNegocioKeys.CondorTravel_CL.GetKeyValues()).ToUpper().Split(Auxiliar.ListSeparator).ToList().Contains(unidadNegocioName.ToUpper()))
            {
                return UnidadNegocioKeys.CondorTravel_CL;
            }
            return null;
        }

        protected abstract UnidadNegocioKeys? RepositoryByBusiness(UnidadNegocioKeys? unidadNegocioKey);
        #endregion

        #region RetryMethods
        protected void CreateOrUpdate(UnidadNegocioKeys? unidadNegocio, T entity, string codigoError, int delayTimeRetry = 0)
        {
            if (_operRetry[unidadNegocio] =
                ((_operCollection[unidadNegocio] =
                    _crmCollection[unidadNegocio].Create(entity))
                        [OutParameter.CodigoError].ToString().Equals(codigoError)))
            {
                System.Threading.Thread.Sleep(delayTimeRetry);
                _operCollection[unidadNegocio] = _crmCollection[unidadNegocio].Update(entity);
            }
        }

        protected void UpdateOrCreate(UnidadNegocioKeys? unidadNegocio, T entity, string codigoError, int delayTimeRetry = 0)
        {
            if (_operRetry[unidadNegocio] =
                ((_operCollection[unidadNegocio] =
                    _crmCollection[unidadNegocio].Update(entity))
                        [OutParameter.CodigoError].ToString().Equals(codigoError)))
            {
                System.Threading.Thread.Sleep(delayTimeRetry);
                _operCollection[unidadNegocio] = _crmCollection[unidadNegocio].Create(entity);
            }
        }

        protected object GetErrorResult(UnidadNegocioKeys? unidadNegocio)
        {
            object codigoError, mensajeError;
            
            try { codigoError = _operCollection[unidadNegocio][OutParameter.CodigoError].ToString(); } catch (Exception ex) { codigoError = ex.Message; }
            try { mensajeError = _operCollection[unidadNegocio][OutParameter.MensajeError].ToString(); } catch (Exception ex) { mensajeError = ex.Message; }

            return new
            {
                CodigoError = codigoError,
                MensajeError = mensajeError
            };
        }
        #endregion
    }
}

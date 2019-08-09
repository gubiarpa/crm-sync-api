﻿using Expertia.Estructura.Controllers.Behavior;
using Expertia.Estructura.Utils.Behavior;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace Expertia.Estructura.Utils
{
    public static class ExtensionMethods
    {
        #region Nullable
        public static object Coalesce(this object obj, object nullValue = null)
        {
            try
            {
                return obj == null ? (nullValue == null ? DBNull.Value : nullValue) : obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Json
        public static string Stringify(this object obj, bool indented = false)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Formatting = indented ? Formatting.Indented : Formatting.None,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
                return JsonConvert.SerializeObject(obj, settings);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Log
        public static void WriteLogObject(this object obj, ILogFileManager logFileManager, IClientFeatures clientFeatures, LogType logType = LogType.Info, bool indented = true)
        {
            logFileManager.WriteText(obj.BuildLogObject(clientFeatures, logType).Stringify(indented) + "\n");
        }

        private static object BuildLogObject(this object obj, IClientFeatures clientFeatures, LogType logType = LogType.Info)
        {
            try
            {
                return new
                {
                    Client = new
                    {
                        IP = clientFeatures.IP,
                        Method = clientFeatures.Method,
                        Log = logType,
                        Url = clientFeatures.URL,
                        Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                    },
                    Entity = obj
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Invest.WebApp.Core
{
    public static class Settings
    {
        //private static ILog log = LogManager.GetLogger(typeof(Settings));
        public static readonly FileServiceApiParams FileServiceApi;
        
        static Settings()
        {
            FileServiceApi = new FileServiceApiParams();
        }

        /// <summary>
        /// Загружает параметры из appsettings.json
        /// </summary>
        /// <remarks>Реализована загрузка параметров VideoFilesFolder и Email</remarks>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static void Load(IConfiguration configuration)
        {
            ParseFileApiService(configuration);
        }

        private static void ParseFileApiService(IConfiguration configuration)
        {
            var serviceApi = configuration.GetSection("ServiceApi").Get<Dictionary<string, string>>();
            if (serviceApi == null || string.IsNullOrEmpty(serviceApi["url"]) || string.IsNullOrEmpty(serviceApi["key"]))
                throw new Exception("Settings(): ServiceApi params are wrong or empty");

            FileServiceApi.Url = serviceApi["url"];
            FileServiceApi.Key = serviceApi["key"];
        }
		
        public class FileServiceApiParams
        {
            public string Url;
            public string Key;
        }
    }
}
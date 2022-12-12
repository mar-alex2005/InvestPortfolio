using System;
using System.Linq;
using log4net;
using Microsoft.Extensions.Configuration;

namespace Core.Console
{
    public class Config
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Config));
        
        public ServiceAPI ServiceApi;
        public ImportConfig Import;

        public struct ServiceAPI
        {
            public string Url;
            public string Key;

			public override string ToString()
			{
				return $"{Url}";
			}
		}
		
        public void Parse(IConfiguration configuration)
        {
            ParseServiceApi(configuration.GetSection("ServiceApi"));
            ParseImport(configuration.GetSection("Import"));
        }

        private void ParseServiceApi(IConfigurationSection section)
        {
            if (section == null)
                throw new Exception("Config.Parse(): ServiceApi section is null or empty");

            if (string.IsNullOrEmpty(section["url"]))
                throw new Exception("Config.Parse(): ServiceApi.Url is null or empty");

            ServiceApi = new ServiceAPI { Url = section["url"], Key = section["key"] };
        }

		private void ParseImport(IConfigurationSection section)
		{
			if (!section.Exists())
				return;

			Import = ImportConfig.Parse(section);
		}
	}

	public class ImportConfig
	{
		public string ReportRootDir;
		public BrokerDto[] Brokers;

		public static ImportConfig Parse(IConfigurationSection section)
		{
			var instance = new ImportConfig();

			if (string.IsNullOrEmpty(section.GetSection("reportRootDir").Value))
				throw new Exception("ImportConfig(), reportRootDir is null or empty");

			instance.ReportRootDir = section.GetSection("reportRootDir").Value;
			
			var brokers = section.GetSection("brokers").GetChildren();
			if (brokers == null || !brokers.Any())
				throw new Exception("ImportConfig(), brokers section is null or empty");

			instance.Brokers = brokers.Select(x => new BrokerDto { Id = x["id"], Dir = x["dir"] }).ToArray();

			return instance;
		} 
            
		public override string ToString()
		{
			return $"{ReportRootDir}";
		}

		public struct BrokerDto
		{
			public string Id;
			public string Dir;
		}
	}
}

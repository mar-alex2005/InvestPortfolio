using System;
using System.Collections.Generic;

namespace Invest.Common
{
	public interface IHttpHost
	{
		/// <summary></summary>
		void Run();
		/// <summary></summary>
		void Stop();
		/// <summary></summary>
		bool IsRunning { get; }

		/// <summary></summary>
		event EventHandler<ApiEventArgs> RequestSentHandler;
	}

	public interface IApiController
	{
		/// <summary></summary>
		/// <returns></returns>
		ApiResult Parse(string url, string httpMethod, string body,  Dictionary<string, string> queryParams);
     
		/// <summary></summary>
		List<ApiCommand> Commands {get; set;}
	}


	/// <summary></summary>
	public class ApiCommand
	{
	}
}
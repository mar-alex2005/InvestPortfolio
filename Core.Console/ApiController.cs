using System;
using System.Collections.Generic;
using Invest.Common;

namespace Invest.Core.Console
{
     public delegate void ComnandRecievedHandler(Dictionary<string, string> cmdParams);

    public class ApiController : IApiController
    {
        public event ComnandRecievedHandler OnCommandRecieved;
		private Builder _builder;
        //private readonly Config.ServiceAPI _apiConfig;
        
        public ApiController(Builder builder)
        {
            //_apiConfig = config;
			//_jobManager = jobManager;
			_builder = builder;
        }

        public ApiResult Parse(string url, string httpMethod, string body, Dictionary<string, string> queryParams)
        {
            var result = new ApiResult();

            if (string.IsNullOrEmpty(url)) {
                result.SetError(ApiResult.ErrorCode.UncorrectUrl, "The url is empty");
                return result;
            }

            if (!url.Contains("/Api/"))
            {
                result.SetError(ApiResult.ErrorCode.UncorrectUrl, "The url hasn`t contain /api/ sequence");
                return result;
            }

            var method = url.Replace("/Api/", "", StringComparison.CurrentCultureIgnoreCase);

            if (string.IsNullOrEmpty(method))
            {
                result.SetError(ApiResult.ErrorCode.UncorrectUrl, "Unrecognized url");
                return result;
            }

            try
            {
                Parse(result, method, httpMethod, body, queryParams);
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }

            return result;
        }

        private void Parse(ApiResult result, string method, string httpMethod, string body, Dictionary<string, string> queryParams)
        {
            method = method.ToLower();
            var isPost = httpMethod == "POST";
            var isPut = httpMethod == "PUT";

            var id = queryParams.ContainsKey("id") ? queryParams["id"] : null;
            var name = queryParams.ContainsKey("name") ? queryParams["name"] : null;

            result.Data = null;

            if (method == "accounts")
            {
                //var query = new DirectoryQuery(queryParams);
                result.Data = _builder.Accounts;
            }
            else if (method == "vaccounts")
            {
	            //var query = new DirectoryQuery(queryParams);
	            result.Data = _builder.VirtualAccounts;
            }
            else if (method == "stocks")
            {
	            //var query = new DirectoryQuery(queryParams);
	            result.Data = _builder.Stocks;
            }
            else if (method == "operations")
            {
	            //var query = new DirectoryQuery(queryParams);
	            result.Data = _builder.Operations;
            }
            else if (method == "files/checkexist")
            {
                //if (string.IsNullOrEmpty(name))
                //{
                //    result.SetError(ApiResult.ErrorCode.InvalidInputParams, "checkExist(): param 'name' is null or empty");
                //    return;
                //}

                //var flags = new string[] {};
                //var process = new FileExistProcessing(_dataProvider);
                //try
                //{
                //    flags = process.Do(name);
                //    _dataProvider.UpdateFile(name, flags, false);
                //}
                //catch (Exception ex)
                //{
                //    result.SetException("Error occured in FileExistProcessing.Do()", ex);
                //}

                //result.Data = flags;
            }
            else if (method == "jobs/add" && isPost)
            {
                //var job = JsonConvert.DeserializeObject<BL.Api.JobModel>(body, new BL.Api.DirConverter());
                //if (job == null)
                //{
                //    result.SetError(ApiResult.ErrorCode.InvalidInputParams, $"Error in DeserializeObject directory ({body})");
                //    return;
                //}

                //_dataProvider.AddJob(job);
            }

            else
                result.SetError(ApiResult.ErrorCode.UncorrectUrl, $"The method name hasn`t unrecognized in url ({method})");
        }

        public List<ApiCommand> Commands { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Invest.Common
{
        /// <summary></summary>
    public delegate void RequestCompletedCallBack(ApiResult contentResult);

    /// <summary>Структура, которая возвращает результат работы метода Api</summary>
    public class ApiResult
    {
        /// <summary>Конструктор по умолчанию для JSon десириализации</summary>
        public ApiResult()
        {
            Error = null;
            Data = "";
            ErrCode = 0;
        }

        /// <summary>Числовой код ошибки - 0 - not errors</summary>
        public ErrorCode ErrCode { get; set; }

        /// <summary>Текст ошибки, которая генерируется при выполнении метода</summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Error { get; set; }

        /// <summary>Возвращаемые данные (если есть)</summary>
        public dynamic Data { get; set; }

        /// <summary></summary>
        public void SetError(ErrorCode errCode, string errText)
        {
            Error = errText;

            if (errCode != 0)
                ErrCode = errCode;
        }

        /// <summary></summary>
        public void SetException(Exception ex)
        {
            SetException("Error occured:", ex);
        }

        /// <summary></summary>
        public void SetException(string text, Exception ex)
        {
            ErrCode = ErrorCode.InternalError;
#if DEBUG
            Error = text + Environment.NewLine + ex.Message + Environment.NewLine + ex.InnerException + Environment.NewLine + Environment.NewLine + ex.StackTrace;
#else
            Error = text + Environment.NewLine + ex.Message;
#endif
        }

        /// <summary>Сериализация данных объекта в json (используем, например, в контроллере)</summary>
        /// <param name="settings">Параметры сериаализации</param>
        /// <returns></returns>
        public string ToJson(JsonSerializerSettings settings = null)
        {
            var defaultSettings = new JsonSerializerSettings { 
                ContractResolver = new CamelCasePropertyNamesContractResolver(), 
                Formatting = Formatting.Indented };

            return JsonConvert.SerializeObject(this, settings ?? defaultSettings);
        }

        /// <summary></summary>
        public enum ErrorCode
        {
            /// <summary></summary>
            InternalError = 1,
            /// <summary></summary>
            UncorrectUrl = 2,
            /// <summary></summary>
            InvalidInputParams = 4,
            /// <summary></summary>
            InvalidSecretKey = 8
        }
    }

    /// <summary></summary>
    public class ApiEventArgs : EventArgs
    {
        /// <summary></summary>
        public readonly string Url;
        /// <summary></summary>
        public readonly string HttpMethod;
        /// <summary></summary>
        public string Body;
        /// <summary></summary>
        public readonly Dictionary<string, string> Params;
        /// <summary></summary>
        public RequestCompletedCallBack Callback;

        /// <summary></summary>
        public HttpListenerRequest Request;

        /// <summary></summary>
        public ApiResult ContentResult { get; private set; }

        /// <summary></summary>
        public ApiEventArgs(string url, string httpMethod, Dictionary<string, string> queryParams)
        {
            Url = url;
            HttpMethod = httpMethod;
            Params = queryParams;
            Callback = RequestCompletedCallBack;
        }

        private void RequestCompletedCallBack(ApiResult result)
        {
            ContentResult = result;
        }
    }
}

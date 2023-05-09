using Newtonsoft.Json;
using OpenAIRefactor.Models.Common;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAIRefactor.Services
{
    public interface IApiHttpService
    {
        Task<HttpOperationResult<TResult>> PostAsync<TData, TResult>(string apiKey, string uri, TData data, Func<TData, CancellationToken, Task<HttpContent>>
            contentFactory, CancellationToken cancellationToken = default)
      where TData : class
      where TResult : class;
        Task<bool> ValidateApiKeyAsync(string apiKey, string uri);
    }


    public class ApiHttpService : IApiHttpService
    {
        private const string ORGANIZATION = "Openai-Organization";
        private const string REQUEST_ID = "X-Request-ID";
        private const string PROCESSING_TIME = "Openai-Processing-Ms";
        private const string OPENAI_VERSION = "Openai-Version";
        private const string OPENAI_MODEL = "Openai-Model";

        public ApiHttpService()
        {
        }

        public async Task<HttpOperationResult<TResult>> PostAsync<TData, TResult>(string apiKey, string uri, TData data, Func<TData, CancellationToken, Task<HttpContent>> contentFactory, CancellationToken cancellationToken = default)
            where TData : class
            where TResult : class
        {
            return await ApiCallAsync<TData, TResult>(apiKey, HttpMethod.Post, uri, data, cancellationToken).ConfigureAwait(false);
        }


        private async Task<HttpOperationResult<TResult>> ApiCallAsync<TData, TResult>(string apiKey, HttpMethod httpMethod,
                string uri,
                TData data,
                CancellationToken cancellationToken = default)
                where TData : class
                where TResult : class
        {
            HttpOperationResult<TResult> result = default;

            using (HttpRequestMessage request = new HttpRequestMessage(httpMethod, uri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("Authorization", $"Bearer {apiKey}");


                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.None
                };
                var jsonText = JsonConvert.SerializeObject(data, settings);


                request.Content = new StringContent(jsonText, Encoding.UTF8, "application/json");


                using (HttpClient httpClient = new HttpClient())
                {

                    using (HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                    {


                        if (response.IsSuccessStatusCode)
                        {
                            if (typeof(string).IsAssignableFrom(typeof(TResult)))
                            {
                                string jsonResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                result = new HttpOperationResult<TResult>(jsonResult as TResult);
                            }
                            else
                            {
                                string jsonResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                TResult jsonresult = JsonConvert.DeserializeObject<TResult>(jsonResult);
                                result = new HttpOperationResult<TResult>(jsonresult);
                                SetResponseData(response, result.Result);
                            }
                        }
                        else
                        {
                            string jsonResult = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            result = new HttpOperationResult<TResult>(new Exception(response.StatusCode.ToString(), new Exception(jsonResult)), response.StatusCode, jsonResult);
                        }

                    }
                }
            }

            return result;
        }

        private static void SetResponseData<TResult>(HttpResponseMessage response, TResult result)
        {
            if (typeof(ResponseBase).IsAssignableFrom(typeof(TResult)))
            {
                ResponseBase rb = (result as ResponseBase);
                if (response.Headers.Contains(ORGANIZATION)) rb.Organization = response.Headers.GetValues(ORGANIZATION).FirstOrDefault();
                if (response.Headers.Contains(REQUEST_ID)) rb.RequestId = response.Headers.GetValues(REQUEST_ID).FirstOrDefault();
                if (response.Headers.Contains(PROCESSING_TIME)) rb.ProcessingTime = TimeSpan.FromMilliseconds(int.Parse(response.Headers.GetValues(PROCESSING_TIME).First()));
                if (response.Headers.Contains(OPENAI_VERSION)) rb.OpenAIVersion = response.Headers.GetValues(OPENAI_VERSION).FirstOrDefault();
                if (string.IsNullOrEmpty(rb.Model) && response.Headers.Contains(OPENAI_MODEL)) rb.Model = response.Headers.GetValues(OPENAI_MODEL).FirstOrDefault();
            }
        }
        public async Task<bool> ValidateApiKeyAsync(string apiKey, string uri)
        {

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                using (HttpClient httpClient = new HttpClient())
                {
                    using (HttpResponseMessage response = await httpClient.SendAsync(request).ConfigureAwait(false))
                    {
                        return response.IsSuccessStatusCode;
                    }
                }
            }
        }


    }
}

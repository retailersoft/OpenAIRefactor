using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net.Http.Headers;
using TestExtension.Settings;

namespace TestExtension.ChatService
{
    public interface IProviderEndpointService
    {

        /// <summary>Builds the base URI.</summary>
        /// <returns>
        ///   URI as string
        /// </returns>
        string BuildBaseUri();

        /// <summary>Configures the HTTP request headers.</summary>
        /// <param name="requestHeaders">The request headers.</param>
        void ConfigureHttpRequestHeaders(HttpRequestHeaders requestHeaders);

    }

    public class OpenAIProviderEndpointService : IProviderEndpointService
    {

        public OpenAIProviderEndpointService()
        {
        }

        public virtual string BuildBaseUri()
        {
            return $"{OpenAIDefaultOptions.DefaultOpenAIBaseAddress}/{OpenAIDefaultOptions.DefaultOpenAIApiVersion}/" + "{0}";
        }

        public virtual void ConfigureHttpRequestHeaders(HttpRequestHeaders requestHeaders)
        {
            var apiKey = "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";
            requestHeaders.Add("Authorization", $"Bearer {apiKey}");

            requestHeaders.Add("OpenAI-Organization", $"RetailerSoft");
            requestHeaders.Add("User-Agent", "RetailerSoft.OpenAI");
        }

    }
}

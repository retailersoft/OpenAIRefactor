using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TestExtension.Models.HTTP;
using TestExtension.Models.TextCompletions;
using TestExtension.Settings;

namespace TestExtension.ChatService
{
    public interface ITextCompletionService
    {
        Task<HttpOperationResult<TextCompletionResponse>> GetAsync(TextCompletionRequest request, CancellationToken cancellationToken = default);
    }

    public class TextCompletionService : ITextCompletionService
    {
        public TextCompletionService(IApiHttpService api)
        {
            this.api = api;
        }

        readonly IApiHttpService api;

        public async Task<HttpOperationResult<TextCompletionResponse>> GetAsync(TextCompletionRequest request, CancellationToken cancellationToken = default)
        {
            var uri = $"{OpenAIDefaultOptions.DefaultOpenAIBaseAddress}/{OpenAIDefaultOptions.DefaultOpenAIApiVersion}/{OpenAIDefaultOptions.DefaultTextCompletionsUri}";
            return await api.PostAsync<TextCompletionRequest, TextCompletionResponse>(uri, request, null, cancellationToken).ConfigureAwait(false);
        }

    }
}

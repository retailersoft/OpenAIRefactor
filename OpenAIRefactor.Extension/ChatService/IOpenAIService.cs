using Microsoft.Build.Framework;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestExtension.Models.HTTP;
using System.Windows.Navigation;
using TestExtension.Models.ChatCompletions;

namespace TestExtension.ChatService
{
    public interface IOpenAIService
    {
        //Task<HttpOperationResult<TextCompletionResponse>> GetTextCompletionAsync(TextCompletionRequest request, CancellationToken cancellationToken = default);
        Task<HttpOperationResult<ChatCompletionResponse>> GetChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);

    }

    public class OpenAIService : IOpenAIService
    {
        //readonly ITextCompletionService textCompletionService;
        readonly IChatCompletionService chatCompletionService;
        public OpenAIService(IChatCompletionService chatCompletionService) //ITextCompletionService textCompletionService
        {
            this.chatCompletionService = chatCompletionService;
            //this.textCompletionService = textCompletionService;
        }

        public async Task<HttpOperationResult<ChatCompletionResponse>> GetChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            return await chatCompletionService.GetAsync(request, cancellationToken);
        }

        //public async Task<HttpOperationResult<TextCompletionResponse>> GetTextCompletionAsync(TextCompletionRequest request, CancellationToken cancellationToken = default)
        //{
        //    return await textCompletionService.GetAsync(request, cancellationToken);
        //}
    

     

    }
}

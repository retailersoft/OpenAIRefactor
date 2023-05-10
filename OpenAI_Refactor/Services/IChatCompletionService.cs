using OpenAI.ChatGpt;
using OpenAI.ChatGpt.Models;
//using OpenAI.ChatGpt.Models.ChatCompletion;
using OpenAI_Refactor.Models.ChatCompletions;
using SharpToken;

namespace OpenAI_Refactor.Services;

public interface IChatCompletionService
{
    Task<string> RefactorCodeAsync(string language, string version, string codeText);
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task<HttpOperationResult<ChatCompletionResponse>> GetAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
}

public class ChatCompletionService : IChatCompletionService, IDisposable
{
    public ChatCompletionService(IApiHttpService api, IChatGptSettingsService chatGptSettingsService)
    {
        this.chatGptSettingsService = chatGptSettingsService;
        this.api = api;
    }
    ChatService chatService;
    readonly IApiHttpService api;
    readonly IChatGptSettingsService chatGptSettingsService;
    public async Task<HttpOperationResult<ChatCompletionResponse>> GetAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {

        return await api.PostAsync<ChatCompletionRequest, ChatCompletionResponse>(chatGptSettingsService.ChatGptApiKey, chatGptSettingsService.ChatCompletionUri, request, null, cancellationToken).ConfigureAwait(false);
    }
    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        return await api.ValidateApiKeyAsync(apiKey, chatGptSettingsService.ApiValidationUri);
    }

    public async Task<string> RefactorCodeAsync(string language, string version, string codeText)
    {
        string model = "gpt-3.5-turbo";
        var message = $"Refactor the following code using {language} Version {version}.\nDont give any explanations.\n" + codeText;

        var gptEncoding = GptEncoding.GetEncodingForModel(model);
        var modelMax = OpenAI.ChatGpt.Models.ChatCompletion.ChatCompletionModels.GetMaxTokensLimitForModel(model) - message.Length;

        var config = new ChatGPTConfig()
        {
            Model = model,
            MaxTokens = modelMax,
            Temperature = 0.5f,
            TopP = 1.0f,
            FrequencyPenalty = 0.0f,
            PresencePenalty = 0.0f
        };
        chatService = await ChatGPT.CreateInMemoryChat(chatGptSettingsService.ChatGptApiKey, config);
        var systemMessage = $"You are a {language} code refactoring assistant.";
        var userMessage = $"I will provide you code that I would like you to refactor using {language} Version {version}.\nDont give any explanations. Let me know when you are ready.";
        //var initMessage = await chatService.InitializeChat(systemMessage, userMessage);

        var result = await chatService.GetNextMessageResponse(message + codeText);

        return result;
    }

    public void Dispose()
    {
        chatService?.Dispose();
    }
}

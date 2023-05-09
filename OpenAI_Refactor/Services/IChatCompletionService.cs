using OpenAI_Refactor.Models.ChatCompletions;

namespace OpenAI_Refactor.Services;

public interface IChatCompletionService
{
    Task<bool> ValidateApiKeyAsync(string apiKey);
    Task<HttpOperationResult<ChatCompletionResponse>> GetAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
}

public class ChatCompletionService : IChatCompletionService
{
    public ChatCompletionService(IApiHttpService api, IChatGptSettingsService chatGptSettingsService)
    {
        this.chatGptSettingsService = chatGptSettingsService;
        this.api = api;
    }

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
}

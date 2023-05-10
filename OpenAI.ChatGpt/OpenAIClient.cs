using OpenAI.ChatGpt.Exceptions;
using OpenAI.ChatGpt.Models.ChatCompletion;
using OpenAI.ChatGpt.Models.ChatCompletion.Messaging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.ChatGpt
{


    public class OpenAiClient : IDisposable
    {
        private const string DefaultHost = "https://api.openai.com/v1/";
        private const string ImagesEndpoint = "images/generations";
        private const string ChatCompletionsEndpoint = "chat/completions";

        private readonly HttpClient _httpClient;
        private readonly bool _isHttpClientInjected;

        private readonly JsonSerializerOptions _nullIgnoreSerializerOptions = new JsonSerializerOptions()
        {
            IgnoreNullValues = true
        };

        /// <summary>
        ///  Creates a new OpenAI client with given <paramref name="apiKey"/>.
        /// </summary>
        /// <param name="apiKey">OpenAI API key. Can be issued here: https://platform.openai.com/account/api-keys</param>
        /// <param name="host">Open AI API host. Default is: <see cref="DefaultHost"/></param>
        public OpenAiClient(string apiKey, string host = DefaultHost)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(apiKey));
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException();
            if (!Uri.TryCreate(host, UriKind.Absolute, out _) || !host.EndsWith("/"))
                throw new ArgumentException("Host must be a valid absolute URI and end with a slash." +
                                            $"For example: {DefaultHost}", nameof(host));
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(host)
            };
            var header = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Authorization = header;
        }

        /// <summary>
        /// Creates a new OpenAI client from DI with given <paramref name="httpClient"/>.
        /// </summary>
        /// <param name="httpClient">
        /// <see cref="HttpClient"/> from DI. It should have an Authorization header set with OpenAI API key.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Indicates that OpenAI API key is not set in
        /// <paramref name="httpClient"/>.<see cref="HttpClient.DefaultRequestHeaders"/>.<see cref="HttpRequestHeaders.Authorization"/> header.
        /// </exception>
        public OpenAiClient(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            ValidateHttpClient(httpClient);
            _isHttpClientInjected = true;
        }

        private static void ValidateHttpClient(HttpClient httpClient)
        {
            if (httpClient.DefaultRequestHeaders.Authorization is null)
            {
                throw new ArgumentException(
                    "HttpClient must have an Authorization header set." +
                    "It should include OpenAI's API key.",
                    nameof(httpClient)
                );
            }

            if (httpClient.BaseAddress is null)
            {
                throw new ArgumentException(
                    "HttpClient must have a BaseAddress set." +
                    "It should be set to OpenAI's API endpoint.",
                    nameof(httpClient)
                );
            }
        }

        public void Dispose()
        {
            if (!_isHttpClientInjected)
            {
                _httpClient.Dispose();
            }
        }

        public async Task<string> GetChatCompletions(
            UserOrSystemMessage dialog,
            int maxTokens = ChatCompletionRequest.MaxTokensDefault,
            string model = ChatCompletionModels.Default,
            float temperature = ChatCompletionTemperatures.Default,
            string user = null,
            Action<ChatCompletionRequest> requestModifier = null,
            CancellationToken cancellationToken = default)
        {
            if (dialog == null) throw new ArgumentNullException(nameof(dialog));
            if (model == null) throw new ArgumentNullException(nameof(model));
            var request = CreateChatCompletionRequest(dialog.GetMessages(),
                maxTokens,
                model,
                temperature,
                user,
                false,
                requestModifier
            );
            var response = await GetChatCompletionsRaw(request, cancellationToken);
            return response.Choices[0].Message.Content;
        }

        public async Task<string> GetChatCompletions(
            IEnumerable<ChatCompletionMessage> messages,
            int maxTokens = ChatCompletionRequest.MaxTokensDefault,
            string model = ChatCompletionModels.Default,
            float temperature = ChatCompletionTemperatures.Default,
            string user = null,
            Action<ChatCompletionRequest> requestModifier = null,
            CancellationToken cancellationToken = default)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            if (model == null) throw new ArgumentNullException(nameof(model));
            var request = CreateChatCompletionRequest(messages,
                maxTokens,
                model,
                temperature,
                user,
                false,
                requestModifier
            );
            var response = await GetChatCompletionsRaw(request, cancellationToken);
            return response.GetMessageContent();
        }

        public async Task<ChatCompletionResponse> GetChatCompletionsRaw(
            IEnumerable<ChatCompletionMessage> messages,
            int maxTokens = ChatCompletionRequest.MaxTokensDefault,
            string model = ChatCompletionModels.Default,
            float temperature = ChatCompletionTemperatures.Default,
            string user = null,
            Action<ChatCompletionRequest> requestModifier = null,
            CancellationToken cancellationToken = default)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            if (model == null) throw new ArgumentNullException(nameof(model));
            var request = CreateChatCompletionRequest(messages,
                maxTokens,
                model,
                temperature,
                user,
                false,
                requestModifier
            );
            var response = await GetChatCompletionsRaw(request, cancellationToken);
            return response;
        }

        internal async Task<ChatCompletionResponse> GetChatCompletionsRaw(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var requestUri = ChatCompletionsEndpoint;
            var requestData = JsonSerializer.Serialize(request, _nullIgnoreSerializerOptions);
            var httpContent = new StringContent(requestData, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(requestUri, httpContent, cancellationToken);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new NotExpectedResponseException(response.StatusCode, responseContent);
            }

            var jsonResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent);
            if (jsonResponse == null)
            {
                throw new JsonException($"Failed to deserialize response: {responseContent} to type {typeof(ChatCompletionResponse)}");
            }

            return jsonResponse;

        }

        /// <summary>
        /// Start streaming chat completions like ChatGPT
        /// </summary>
        /// <param name="messages">The history of messaging</param>
        /// <param name="maxTokens">The length of the response</param>
        /// <param name="model">One of <see cref="ChatCompletionModels"/></param>
        /// <param name="temperature">
        ///     What sampling temperature to use, between 0 and 2.
        ///     Higher values like 0.8 will make the output more random,
        ///     while lower values like 0.2 will make it more focused and deterministic.
        /// </param>
        /// <param name="user">
        ///     A unique identifier representing your end-user, which can help OpenAI to monitor
        ///     and detect abuse.
        /// </param>
        /// <param name="requestModifier">A modifier of the raw request. Allows to specify any custom properties.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Chunks of ChatGPT's response, one by one.</returns>
        public IAsyncEnumerable<string> StreamChatCompletions(
            IEnumerable<ChatCompletionMessage> messages,
            int maxTokens = ChatCompletionRequest.MaxTokensDefault,
            string model = ChatCompletionModels.Default,
            float temperature = ChatCompletionTemperatures.Default,
            string user = null,
            Action<ChatCompletionRequest> requestModifier = null,
            CancellationToken cancellationToken = default)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            if (model == null) throw new ArgumentNullException(nameof(model));
            var request = CreateChatCompletionRequest(messages,
                maxTokens,
                model,
                temperature,
                user,
                true,
                requestModifier
            );
            return (IAsyncEnumerable<string>)StreamChatCompletions(request, cancellationToken);
        }



        private static ChatCompletionRequest CreateChatCompletionRequest(
            IEnumerable<ChatCompletionMessage> messages,
            int maxTokens,
            string model,
            float temperature,
            string user,
            bool stream,
            Action<ChatCompletionRequest> requestModifier)
        {
            var request = new ChatCompletionRequest(messages)
            {
                Model = model,
                MaxTokens = maxTokens,
                Stream = stream,
                User = user,
                Temperature = temperature
            };
            requestModifier?.Invoke(request);
            return request;
        }


        public IEnumerable<string> StreamChatCompletions(
            UserOrSystemMessage messages,
            int maxTokens = ChatCompletionRequest.MaxTokensDefault,
            string model = ChatCompletionModels.Default,
            float temperature = ChatCompletionTemperatures.Default,
            string user = null,
            Action<ChatCompletionRequest> requestModifier = null,
            CancellationToken cancellationToken = default)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));
            if (model == null) throw new ArgumentNullException(nameof(model));
            var request = CreateChatCompletionRequest(messages.GetMessages(),
                maxTokens,
                model,
                temperature,
                user,
                true,
                requestModifier
            );
            return StreamChatCompletions(request, cancellationToken);
        }

        public IEnumerable<string> StreamChatCompletions(
            ChatCompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            request.Stream = true;

            using (var responseStream = StreamChatCompletionsRaw(request, cancellationToken).GetEnumerator())
            {
                while (responseStream.MoveNext())
                {
                    var content = responseStream.Current.Choices[0].Delta?.Content;
                    if (content != null)
                        yield return content;
                }
            }
        }

        public IEnumerable<ChatCompletionResponse> StreamChatCompletionsRaw(
            ChatCompletionRequest request, CancellationToken cancellationToken = default)
        {
            var responses = new List<ChatCompletionResponse>();
            request.Stream = true;

            var responseStreamTask = _httpClient.StreamUsingServerSentEvents<ChatCompletionRequest, ChatCompletionResponse>(
                ChatCompletionsEndpoint,
                request,
                _nullIgnoreSerializerOptions,
                cancellationToken);

            var responseStream = responseStreamTask.Result;

            foreach (var response in responseStream)
            {
                responses.Add(response);
            }

            return responses;
        }


        //public IAsyncEnumerable<string> StreamChatCompletions(
        //    UserOrSystemMessage messages,
        //    int maxTokens = ChatCompletionRequest.MaxTokensDefault,
        //    string model = ChatCompletionModels.Default,
        //    float temperature = ChatCompletionTemperatures.Default,
        //    string user = null,
        //    Action<ChatCompletionRequest> requestModifier = null,
        //    CancellationToken cancellationToken = default)
        //{
        //    if (messages == null) throw new ArgumentNullException(nameof(messages));
        //    if (model == null) throw new ArgumentNullException(nameof(model));
        //    var request = CreateChatCompletionRequest(messages.GetMessages(),
        //        maxTokens,
        //        model,
        //        temperature,
        //        user,
        //        true,
        //        requestModifier
        //    );
        //    return StreamChatCompletions(request, cancellationToken);
        //}

        //public async IAsyncEnumerable<string> StreamChatCompletions(
        //    ChatCompletionRequest request,
        //    [EnumeratorCancellation] CancellationToken cancellationToken = default)
        //{
        //    if (request == null) throw new ArgumentNullException(nameof(request));
        //    request.Stream = true;
        //    await foreach (var response in StreamChatCompletionsRaw(request, cancellationToken)
        //        .WithCancellation(cancellationToken))
        //    {
        //        var content = response.Choices[0].Delta?.Content;
        //        if (content is not null)
        //            yield return content;
        //    }
        //}

        //public async Task<List<ChatCompletionResponse>> StreamChatCompletionsRaw(
        //    ChatCompletionRequest request, CancellationToken cancellationToken = default)
        //{
        //    var responses = new List<ChatCompletionResponse>();
        //    request.Stream = true;

        //    

        //    try
        //    {
        //        while (await responseStream.MoveNextAsync())
        //        {
        //            var response = responseStream.Current;
        //            responses.Add(response);
        //        }
        //    }
        //    finally
        //    {
        //        await responseStream.DisposeAsync();
        //    }

        //    return responses;
        //}


        //// Will be moved to a separate package.
        //internal async Task<byte[]> GenerateImageBytes(
        //    string prompt,
        //    string user = null,
        //    OpenAiImageSize size = OpenAiImageSize._1024,
        //    CancellationToken cancellationToken = default)
        //{
        //    if (string.IsNullOrWhiteSpace(prompt))
        //        throw new ArgumentException("Value cannot be null or whitespace.", nameof(prompt));
        //    var request = new ImageGenerationRequest(prompt, SizeToString(size), 1, "b64_json", user);

        //    var json = JsonSerializer.Serialize(request);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var response = await _httpClient.PostAsync(ImagesEndpoint, content, cancellationToken);

        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new NotExpectedResponseException(response.StatusCode, responseContent);
        //    }

        //    var jsonResponse =
        //        JsonSerializer.Deserialize<ImagesGenerationB64Response>(responseContent)!;
        //    return Convert.FromBase64String(jsonResponse.Data[0].B64Json);
        //}

        //// Will be moved to a separate package.
        //internal async Task<Uri[]> GenerateImagesUris(
        //    string prompt,
        //    string user = null,
        //    OpenAiImageSize size = OpenAiImageSize._1024,
        //    int count = 1,
        //    CancellationToken cancellationToken = default)
        //{
        //    if (string.IsNullOrWhiteSpace(prompt))
        //        throw new ArgumentException("Value cannot be null or whitespace.", nameof(prompt));
        //    if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        //    var request = new ImageGenerationRequest(prompt, SizeToString(size), count, "url", user);
        //    var json = JsonSerializer.Serialize(request);
        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var response = await _httpClient.PostAsync(ImagesEndpoint, content, cancellationToken);
        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new NotExpectedResponseException(response.StatusCode, responseContent);
        //    }

        //    var jsonResponse = JsonSerializer.Deserialize<ImagesGenerationUriResponse>(responseContent);
        //    return jsonResponse.Data.Select(it => it.Url).ToArray();
        //}

        //private static string SizeToString(OpenAiImageSize size)
        //{
        //    string imageSize;
        //    switch (size)
        //    {
        //        case OpenAiImageSize._256:
        //            imageSize = "256x256";
        //            break;
        //        case OpenAiImageSize._512:
        //            imageSize = "512x512";
        //            break;
        //        case OpenAiImageSize._1024:
        //            imageSize = "1024x1024";
        //            break;
        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(size), size, null);
        //    }
        //    return imageSize;

        //}
    }
}
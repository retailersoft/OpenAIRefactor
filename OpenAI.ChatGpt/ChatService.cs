using OpenAI.ChatGpt.Interfaces;
using OpenAI.ChatGpt.Internal;
using OpenAI.ChatGpt.Models;
using OpenAI.ChatGpt.Models.ChatCompletion;
using OpenAI.ChatGpt.Models.ChatCompletion.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.ChatGpt
{

    public class ChatService : IDisposable, IAsyncDisposable
    {
        public Topic Topic { get; }
        public string UserId { get; }
        public Guid TopicId => Topic.Id;
        public bool IsWriting { get; private set; }
        public bool IsCancelled => _cts?.IsCancellationRequested ?? false;

        public ChatCompletionResponse LastResponse { get; private set; }

        private readonly IChatHistoryStorage _chatHistoryStorage;
        private readonly ITimeProvider _clock;
        private readonly OpenAiClient _client;
        private bool _isNew;
        private readonly bool _clearOnDisposal;
        private CancellationTokenSource _cts;

        internal ChatService(
            IChatHistoryStorage chatHistoryStorage,
            ITimeProvider clock,
            OpenAiClient client,
            string userId,
            Topic topic,
            bool isNew,
            bool clearOnDisposal)
        {
            _chatHistoryStorage = chatHistoryStorage ?? throw new ArgumentNullException(nameof(chatHistoryStorage));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _client = client ?? throw new ArgumentNullException(nameof(client));
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            _isNew = isNew;
            _clearOnDisposal = clearOnDisposal;
        }

        public void Dispose()
        {
            _cts?.Dispose();
            if (_clearOnDisposal)
            {
                // TODO: log warning about sync disposal
                _chatHistoryStorage.DeleteTopic(UserId, TopicId, default)
                    .GetAwaiter().GetResult();
            }
        }

        public async ValueTask DisposeAsync()
        {
            _cts?.Dispose();
            if (_clearOnDisposal)
            {
                await _chatHistoryStorage.DeleteTopic(UserId, TopicId, default);
            }
        }

        public Task<string> InitializeChat(string systemMessage, string userMessage, CancellationToken cancellationToken = default)
        {
            if (systemMessage == null) throw new ArgumentNullException(nameof(systemMessage));
            if (userMessage == null) throw new ArgumentNullException(nameof(userMessage));

            IEnumerable<ChatCompletionMessage> messages = new List<ChatCompletionMessage>()
            {
                new ChatCompletionMessage("system", systemMessage),
                new ChatCompletionMessage("user", userMessage),
            };
            if (messages == null || messages.Count() == 0) throw new ArgumentNullException(nameof(messages));
            return GetInitializeChatResponse(messages, cancellationToken);
        }

        public Task<string> GetNextMessageResponse(
            string message,
            CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            var chatCompletionMessage = new UserMessage(message);
            return GetNextMessageResponse(chatCompletionMessage, cancellationToken);
        }
        public Task<string> GetInitializeChatResponse(
            IEnumerable<ChatCompletionMessage> messages,
            CancellationToken cancellationToken = default)
        {
            return GetNextMessageResponse(messages, cancellationToken);
        }


        private async Task<string> GetNextMessageResponse(
            IEnumerable<ChatCompletionMessage> messages,
            CancellationToken cancellationToken)
        {
            if (IsWriting)
            {
                throw new InvalidOperationException("Cannot start a new chat session while the previous one is still in progress.");
            }
            var originalCancellation = cancellationToken;
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(originalCancellation);
            cancellationToken = _cts.Token;

            var history = await LoadHistory(cancellationToken);
            IsWriting = true;
            try
            {
                var response = await _client.GetChatCompletionsRaw(
                    messages,
                    user: Topic.Config.PassUserIdToOpenAiRequests is true ? UserId : null,
                    requestModifier: Topic.Config.ModifyRequest,
                    cancellationToken: cancellationToken
                );
                SetLastResponse(response);

                var assistantMessage = response.GetMessageContent();
                //await _chatHistoryStorage.SaveMessages(
                //    UserId, TopicId, message, assistantMessage, _clock.GetCurrentTime(), cancellationToken);
                _isNew = false;
                return assistantMessage;
            }
            finally
            {
                IsWriting = false;
            }
        }

        private async Task<string> GetNextMessageResponse(
            UserOrSystemMessage message,
            CancellationToken cancellationToken)
        {
            if (IsWriting)
            {
                throw new InvalidOperationException("Cannot start a new chat session while the previous one is still in progress.");
            }
            var originalCancellation = cancellationToken;
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(originalCancellation);
            cancellationToken = _cts.Token;

            var history = await LoadHistory(cancellationToken);
            var messages = history.Append(message);

            IsWriting = true;
            try
            {
                var response = await _client.GetChatCompletionsRaw(
                    messages,
                    user: Topic.Config.PassUserIdToOpenAiRequests is true ? UserId : null,
                    requestModifier: Topic.Config.ModifyRequest,
                    cancellationToken: cancellationToken
                );
                SetLastResponse(response);

                var assistantMessage = response.GetMessageContent();
                await _chatHistoryStorage.SaveMessages(
                    UserId, TopicId, message, assistantMessage, _clock.GetCurrentTime(), cancellationToken);
                _isNew = false;
                return assistantMessage;
            }
            finally
            {
                IsWriting = false;
            }
        }

        public IEnumerable<string> StreamNextMessageResponse(
            string message,
            bool throwOnCancellation = true,
            CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            UserMessage chatCompletionMessage = new UserMessage(message);
            return StreamNextMessageResponseAsync(chatCompletionMessage, throwOnCancellation, cancellationToken).GetAwaiter().GetResult();
        }

        private async Task<IEnumerable<string>> StreamNextMessageResponseAsync(
      UserOrSystemMessage message,
      bool throwOnCancellation,
      CancellationToken cancellationToken)
        {
            if (IsWriting)
            {
                throw new InvalidOperationException("Cannot start a new chat session while the previous one is still in progress.");
            }

            var originalCancellationToken = cancellationToken;
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(originalCancellationToken);
            cancellationToken = _cts.Token;

            var history = await LoadHistory(cancellationToken);
            var messages = history.Append(message);
            var sb = new StringBuilder();
            IsWriting = true;

            var stream = _client.StreamChatCompletions(
                messages,
                user: Topic.Config.PassUserIdToOpenAiRequests is true ? UserId : null,
                requestModifier: Topic.Config.ModifyRequest,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            var chunks = new List<string>();
            var enumerator = stream.GetAsyncEnumerator();
            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    var chunk = enumerator.Current;
                    chunks.Add(chunk);
                    sb.Append(chunk);
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            if (cancellationToken.IsCancellationRequested && !throwOnCancellation)
            {
                return Enumerable.Empty<string>();
            }

            SetLastResponse(null);

            try
            {
                await _chatHistoryStorage.SaveMessages(
                    UserId,
                    TopicId,
                    message,
                    sb.ToString(),
                    _clock.GetCurrentTime(),
                    cancellationToken);
                _isNew = false;
            }
            finally
            {
                IsWriting = false;
            }

            return chunks;
        }


        private async Task<IEnumerable<ChatCompletionMessage>> LoadHistory(
            CancellationToken cancellationToken)
        {
            if (_isNew) return Enumerable.Empty<ChatCompletionMessage>();
            return await _chatHistoryStorage.GetMessages(UserId, TopicId, cancellationToken);
        }


        /// <summary> Returns topic messages history. </summary>
        public Task<IEnumerable<PersistentChatMessage>> GetMessages(
            CancellationToken cancellationToken = default)
        {
            return _chatHistoryStorage.GetMessages(UserId, TopicId, cancellationToken);
        }

        private void SetLastResponse(ChatCompletionResponse response)
        {
            LastResponse = response;
        }

        public void Stop()
        {
            _cts?.Cancel();
        }
    }
}
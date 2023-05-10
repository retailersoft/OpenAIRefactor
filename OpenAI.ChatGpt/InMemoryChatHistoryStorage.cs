using OpenAI.ChatGpt.Exceptions;
using OpenAI.ChatGpt.Interfaces;
using OpenAI.ChatGpt.Models;
using OpenAI.ChatGpt.Models.ChatCompletion.Messaging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static OpenAI.ChatGpt.Models.ChatCompletion.ChatCompletionRoles;

namespace OpenAI.ChatGpt
{

    /// <summary>
    /// Represents an in-memory storage for managing messages and topics.
    /// </summary>
    /// <remarks>
    /// Thread safe for different users. Not thread safe for the same user.
    /// </remarks>
    public class InMemoryChatHistoryStorage : IChatHistoryStorage
    {
        private readonly ConcurrentDictionary<string, Dictionary<Guid, Topic>> _users = new ConcurrentDictionary<string, Dictionary<Guid, Topic>>();

        private readonly ConcurrentDictionary<string, Dictionary<Guid, List<PersistentChatMessage>>>
            _messages = new ConcurrentDictionary<string, Dictionary<Guid, List<PersistentChatMessage>>>();


        public Task SaveMessages(
            string userId,
            Guid topicId,
            UserOrSystemMessage message,
            string assistantMessage,
            DateTimeOffset? dateTime,
            CancellationToken cancellationToken)
        {
            if (userId == null) throw new ArgumentNullException(nameof(userId));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (assistantMessage == null) throw new ArgumentNullException(nameof(assistantMessage));
            if (dateTime == null)
            {
                dateTime = DateTimeOffset.UtcNow;
            }
            var enumerable = new PersistentChatMessage[]
            {
            new PersistentChatMessage(NewMessageId(), userId, topicId, dateTime.Value, message),
            new PersistentChatMessage(NewMessageId(), userId, topicId, dateTime.Value, Assistant, assistantMessage),
            };
            return SaveMessages(userId, topicId, enumerable, cancellationToken);
        }

        /// <inheritdoc/>
        public Task SaveMessages(
            string userId,
            Guid topicId,
            IEnumerable<PersistentChatMessage> messages,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            if (messages == null) throw new ArgumentNullException();

            cancellationToken.ThrowIfCancellationRequested();
            if (!_messages.TryGetValue(userId, out var userMessages))
            {
                userMessages = new Dictionary<Guid, List<PersistentChatMessage>>();
                _messages.TryAdd(userId, userMessages);
            }

            if (!userMessages.TryGetValue(topicId, out var chatMessages))
            {
                chatMessages = new List<PersistentChatMessage>();
                userMessages.Add(topicId, chatMessages);
            }

            chatMessages.AddRange(messages);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<PersistentChatMessage>> GetMessages(
            string userId, Guid topicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_messages.TryGetValue(userId, out var userMessages))
            {
                return Task.FromResult(Enumerable.Empty<PersistentChatMessage>());
            }

            if (!userMessages.TryGetValue(topicId, out var chatMessages))
            {
                return Task.FromResult(Enumerable.Empty<PersistentChatMessage>());
            }

            return Task.FromResult(chatMessages.AsEnumerable());
        }

        /// <inheritdoc/>
        public Task<Topic> GetMostRecentTopicOrNull(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_users.TryGetValue(userId, out var userChats))
            {
                return Task.FromResult<Topic>(null);
            }

            var lastTopic = userChats.Values.OrderByDescending(x => x.CreatedAt).First();
            return Task.FromResult(lastTopic);
        }

        /// <inheritdoc/>
        public async Task EditTopicName(
            string userId, Guid topicId, string newName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();

            if (string.IsNullOrEmpty(newName)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            var topic = await GetTopic(userId, topicId, cancellationToken);
            topic.Name = newName;
        }

        /// <inheritdoc/>
        public Task<bool> DeleteTopic(string userId, Guid topicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_users.TryGetValue(userId, out var userChats))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(userChats.Remove(topicId));
        }

        /// <inheritdoc/>
        public Task<bool> ClearTopics(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_users.TryGetValue(userId, out var topics))
            {
                return Task.FromResult(false);
            }

            topics.Clear();
            return Task.FromResult(true);
        }

        /// <inheritdoc/>
        public async Task EditMessage(
            string userId,
            Guid topicId,
            Guid messageId,
            string newMessage,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            if (string.IsNullOrEmpty(newMessage)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            var messages = await GetMessages(userId, topicId, cancellationToken);
            var message = messages.FirstOrDefault(x => x.Id == messageId);
            if (message == null)
            {
                throw new MessageNotFoundException(messageId);
            }

            message.Content = newMessage;
        }

        /// <inheritdoc/>
        public Task<bool> DeleteMessage(
            string userId, Guid topicId, Guid messageId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_messages.TryGetValue(userId, out var userMessages))
            {
                return Task.FromResult(false);
            }

            if (userMessages.TryGetValue(topicId, out var chatMessages))
            {
                return Task.FromResult(chatMessages.RemoveAll(m => m.Id == messageId) == 1);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public Task<bool> ClearMessages(string userId, Guid topicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_messages.TryGetValue(userId, out var userMessages))
            {
                return Task.FromResult(false);
            }

            if (userMessages.TryGetValue(topicId, out var chatMessages))
            {
                chatMessages.Clear();
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<Topic>> GetTopics(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_users.TryGetValue(userId, out var topics))
            {
                return Task.FromResult(Enumerable.Empty<Topic>());
            }

            return Task.FromResult(topics.Values.AsEnumerable());
        }

        /// <inheritdoc/>
        public Task<Topic> GetTopic(string userId, Guid topicId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_users.TryGetValue(userId, out var userChats))
            {
                throw new TopicNotFoundException(topicId);
            }

            if (!userChats.TryGetValue(topicId, out var topic))
            {
                throw new TopicNotFoundException(topicId);
            }

            return Task.FromResult(topic);
        }

        /// <inheritdoc/>
        public Task AddTopic(Topic topic, CancellationToken cancellationToken)
        {
            if (topic == null) throw new ArgumentNullException();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_users.TryGetValue(topic.UserId, out var userTopics))
            {
                userTopics = new Dictionary<Guid, Topic>();
                _users.TryAdd(topic.UserId, userTopics);
            }

            userTopics.Add(topic.Id, topic);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task EnsureStorageCreated(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public Guid NewTopicId()
        {
            return Guid.NewGuid();
        }

        public Guid NewMessageId()
        {
            return Guid.NewGuid();
        }


    }
}
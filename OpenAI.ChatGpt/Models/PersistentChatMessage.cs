﻿using OpenAI.ChatGpt.Models.ChatCompletion.Messaging;
using System;

namespace OpenAI.ChatGpt.Models
{

    public class PersistentChatMessage : ChatCompletionMessage
    {
        public Guid Id { get; private set; }
        public string UserId { get; set; }
        public Guid TopicId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public PersistentChatMessage(
            Guid id,
            string userId,
            Guid topicId,
            DateTimeOffset createdAt,
            string role,
            string content) : base(role, content)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (content == null) throw new ArgumentNullException(nameof(content));
            Id = id;
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            TopicId = topicId;
            CreatedAt = createdAt;
        }

        public PersistentChatMessage(
            Guid id,
            string userId,
            Guid topicId,
            DateTimeOffset createdAt,
            ChatCompletionMessage message) : base(message.Role, message.Content)
        {
            Id = id;
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            TopicId = topicId;
            CreatedAt = createdAt;
        }
    }
}
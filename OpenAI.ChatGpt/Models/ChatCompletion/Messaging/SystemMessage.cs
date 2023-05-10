﻿using System;
using System.Collections.Generic;

namespace OpenAI.ChatGpt.Models.ChatCompletion.Messaging
{

    /// <summary>
    /// A message from the system.
    /// The recommended way to create it using method <see cref="Dialog"/>.<see cref="Dialog.StartAsSystem(string)"/>
    /// </summary>
    /// <remarks>
    /// Typically, a conversation is formatted with a system message first,
    /// followed by alternating user and assistant messages.
    /// The system message helps set the behavior of the assistant. In the example above,
    /// the assistant was instructed with “You are a helpful assistant.”
    /// </remarks>
    public class SystemMessage : UserOrSystemMessage
    {
        /// <summary>
        /// A message from the system
        /// </summary>
        /// <remarks>
        /// Typically, a conversation is formatted with a system message first,
        /// followed by alternating user and assistant messages.
        /// The system message helps set the behavior of the assistant.
        /// </remarks>
        /// <param name="content">The message text</param>
        public SystemMessage(string content) : base(ChatCompletionRoles.System, content)
        {
        }

        internal SystemMessage(List<ChatCompletionMessage> messages, string content)
            : base(messages, ChatCompletionRoles.Assistant, content)
        {
        }

        public UserMessage ThenUser(string userMessage)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(userMessage));
            return new UserMessage(Messages, userMessage);
        }
    }
}
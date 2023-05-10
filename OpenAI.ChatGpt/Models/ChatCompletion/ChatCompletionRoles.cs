using System;

namespace OpenAI.ChatGpt.Models.ChatCompletion
{

    /// <summary>
    /// The available roles of the message in the chat.
    /// See https://github.com/openai/openai-python/blob/main/chatml.md for more information.
    /// </summary>
    public static class ChatCompletionRoles
    {
        public const string System = "system";
        public const string User = "user";
        public const string Assistant = "assistant";

        public static bool IsValid(string role)
        {
            return string.Equals(role, "System", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "User", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Assistant", StringComparison.OrdinalIgnoreCase);
        }
        public static void ThrowIfInvalid(string role)
        {
            if (!IsValid(role))
                throw new ArgumentException($"Invalid role: {role}");
        }
    }
}
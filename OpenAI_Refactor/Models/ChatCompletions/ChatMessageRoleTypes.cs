namespace OpenAI_Refactor.Models.ChatCompletions;

public static class ChatMessageRoleTypes
{

    public const string USER = "user";
    public const string SYSTEM = "system";
    public const string ASSISTANT = "assistant";

    public static readonly string[] ValidRoleTypes = new string[] { USER, SYSTEM, ASSISTANT };

}

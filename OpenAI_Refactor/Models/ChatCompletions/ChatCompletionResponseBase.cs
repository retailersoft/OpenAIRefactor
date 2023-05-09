namespace OpenAI_Refactor.Models.ChatCompletions;
public abstract class ChatCompletionResponseBase : ResponseBase
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("usage")]
    public Usage Usage { get; }

}

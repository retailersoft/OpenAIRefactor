using System.Collections.Generic;

namespace OpenAI_Refactor.Models.ChatCompletions;


public class ChatCompletionResponse : ChatCompletionResponseBase
{

    [JsonProperty("choices")]
    public List<ChatChoice> Choices { get; set; } = new List<ChatChoice>();

}

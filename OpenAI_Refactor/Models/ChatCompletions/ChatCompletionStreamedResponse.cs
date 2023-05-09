using System.Collections.Generic;

namespace OpenAI_Refactor.Models.ChatCompletions;
public class ChatCompletionStreamedResponse : ChatCompletionResponseBase
{

    [JsonProperty("choices")]
    public List<ChatChoiceStreamed> Choices { get; set; } = new List<ChatChoiceStreamed>();

}

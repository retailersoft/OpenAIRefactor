using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAIRefactor.Models.ChatCompletions
{

    public class ChatCompletionStreamedResponse : ChatCompletionResponseBase
    {

        [JsonProperty("choices")]
        public List<ChatChoiceStreamed> Choices { get; set; } = new List<ChatChoiceStreamed>();

    }

}

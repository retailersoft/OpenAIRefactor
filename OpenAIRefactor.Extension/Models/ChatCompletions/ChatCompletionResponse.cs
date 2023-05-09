using Newtonsoft.Json;
using System.Collections.Generic;

namespace OpenAIRefactor.Models.ChatCompletions
{

    public class ChatCompletionResponse : ChatCompletionResponseBase
    {

        [JsonProperty("choices")]
        public List<ChatChoice> Choices { get; set; } = new List<ChatChoice>();

    }

}

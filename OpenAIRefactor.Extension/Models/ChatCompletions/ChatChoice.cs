using Newtonsoft.Json;

namespace OpenAIRefactor.Models.ChatCompletions
{

    public class ChatChoice
    {

        [JsonProperty("message")]
        public ChatMessage Message { get; set; }

        [JsonProperty("finish_reason")]
        public string FinishReason { get; set; }

        [JsonProperty("index")]
        public int? Index { get; set; }

    }

}

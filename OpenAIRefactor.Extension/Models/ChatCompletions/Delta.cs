using Newtonsoft.Json;

namespace OpenAIRefactor.Models.ChatCompletions
{

    public class Delta
    {

        [JsonProperty("content")]
        public string Content { get; set; }

    }

}

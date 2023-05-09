using Newtonsoft.Json;
using OpenAIRefactor.Models.Common;

namespace OpenAIRefactor.Models.ChatCompletions
{

    public abstract class ChatCompletionResponseBase : ResponseBase
    {

        /// <summary>
        /// The identifier of the result, which may be used during troubleshooting
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>Gets the token usage numbers.</summary>
        /// <value>The usage.</value>
        [JsonProperty("usage")]
        public Usage Usage { get; }

    }

}

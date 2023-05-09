namespace OpenAI_Refactor.Models.ChatCompletions;


public class Delta
{

    [JsonProperty("content")]
    public string Content { get; set; }

}

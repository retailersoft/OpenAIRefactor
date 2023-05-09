namespace OpenAI_Refactor.Models.ChatCompletions;

public class ChatChoiceStreamed
{

    [JsonProperty("delta")]
    public Delta Delta { get; set; }

    [JsonProperty("index")]
    public int? Index { get; set; }

    [JsonProperty("finish_reason")]
    public object FinishReason { get; set; }

}

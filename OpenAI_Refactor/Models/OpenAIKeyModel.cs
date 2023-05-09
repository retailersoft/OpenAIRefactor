namespace OpenAI_Refactor.Models;
public class OpenAIKeyModel
{
    public string ApiKey { get; set; } = string.Empty;
    [JsonIgnore]
    public bool Validated => !string.IsNullOrEmpty(ApiKey) && ValidationDate.HasValue;
    public DateTime? ValidationDate { get; set; }

}

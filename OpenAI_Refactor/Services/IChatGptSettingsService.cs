using System.IO;

namespace OpenAI_Refactor.Services;

public interface IChatGptSettingsService
{
    string ChatCompletionUri { get; }
    string ApiValidationUri { get; }
    bool ChatGptApiKeyIsValid { get; }
    DateTime? ChatGptApiKeyValidationDate { get; }
    string ChatGptApiKey { get; set; }
    void ChatGptApiKeyValidated(bool valid, string apiKey = "");

}
public class ChatGptSettingsService : IChatGptSettingsService
{
    readonly OpenAIOptions openAIOptions;
    public ChatGptSettingsService()
    {
        openAIOptions = new OpenAIOptions();
    }
    public string ChatCompletionUri => $"{openAIOptions.BaseAddress}/{openAIOptions.OpenAIApiVersion}/{openAIOptions.ChatCompletionsUri}";
    public string ApiValidationUri => $"{openAIOptions.BaseAddress}/{openAIOptions.OpenAIApiVersion}/models";
    public bool ChatGptApiKeyIsValid => KeyModel.Validated;

    OpenAIKeyModel keyModel = null;
    private OpenAIKeyModel KeyModel
    {
        get
        {
            if (keyModel == null)
            {
                if (File.Exists(KeyFileName))
                {
                    var json = File.ReadAllText(KeyFileName).Trim();
                    keyModel = JsonConvert.DeserializeObject<OpenAIKeyModel>(json);
                }
                else
                {
                    keyModel = new OpenAIKeyModel();
                }
            }
            return keyModel;
        }
        set
        {
            keyModel = value;
            SaveOpenAIKeyModel();
        }
    }

    private void SaveOpenAIKeyModel()
    {
        File.WriteAllText(KeyFileName, JsonConvert.SerializeObject(keyModel));
    }

    public void ChatGptApiKeyValidated(bool valid, string apiKey = "")
    {
        if (valid)
        {
            KeyModel.ValidationDate = DateTime.Now;
            KeyModel.ApiKey = apiKey;
        }
        else
        {
            KeyModel.ValidationDate = null;
            KeyModel.ApiKey = string.Empty;
        }
        SaveOpenAIKeyModel();
    }

    private string KeyFileName
    {
        get
        {
            string dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RetailerSoft", "OpenAIRefactor");
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            }
            return Path.Combine(dataFolder, "api_key.json");
        }
    }

    public string ChatGptApiKey
    {
        get => KeyModel.ApiKey;
        set
        {
            KeyModel.ApiKey = value;
            SaveOpenAIKeyModel();
        }
    }
    public DateTime? ChatGptApiKeyValidationDate => KeyModel.ValidationDate;
}

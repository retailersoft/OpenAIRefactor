using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OpenAIRefactor.Models;

namespace OpenAIRefactor.Settings
{

    public static class OpenAIDefaultOptions
    {
        public static string DefaultOpenAIBaseAddress { get; set; } = "https://api.openai.com";
        public static string DefaultOpenAIApiVersion { get; set; } = "v1";
        public static string DefaultModelsUri { get; set; } = "models";
        public static string DefaultChatCompletionModel { get; set; } = KnownModelTypes.Gpt3_5Turbo;
        public static string DefaultChatCompletionsUri { get; set; } = "chat/completions";
        public static JsonSerializerSettings DefaultJsonSerializerOptions { get; set; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };


    }
}

using Newtonsoft.Json;
using System;

namespace OpenAIRefactor.Settings
{

    public class OpenAIOptions : ICloneable
    {

        public string BaseAddress { get; set; } = OpenAIDefaultOptions.DefaultOpenAIBaseAddress;

        /// <summary>Gets or sets the API version of the OpenAI provider.</summary>
        /// <value>The API version.</value>
        public string OpenAIApiVersion { get; set; } = OpenAIDefaultOptions.DefaultOpenAIApiVersion;

        public string ModelsUri { get; set; } = OpenAIDefaultOptions.DefaultModelsUri;
        public string ChatCompletionModel { get; set; } = OpenAIDefaultOptions.DefaultChatCompletionModel;
        public string ChatCompletionsUri { get; set; } = OpenAIDefaultOptions.DefaultChatCompletionsUri;
        [JsonIgnore]
        public JsonSerializerSettings JsonSerializerOptions { get; set; } = OpenAIDefaultOptions.DefaultJsonSerializerOptions;

        public virtual object Clone()
        {
            OpenAIOptions cloned = (OpenAIOptions)GetType().GetConstructor(Type.EmptyTypes).Invoke(null);

            cloned.OpenAIApiVersion = OpenAIApiVersion;
            cloned.BaseAddress = BaseAddress;
            cloned.JsonSerializerOptions = JsonSerializerOptions;
            cloned.ModelsUri = ModelsUri;
            cloned.ChatCompletionsUri = ChatCompletionsUri;
            return cloned;
        }

    }
}

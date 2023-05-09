using Community.VisualStudio.Toolkit;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace OpenAIRefactor
{
    internal partial class OptionsProvider
    {
        // Register the options with this attribute on your package class:
        // [ProvideOptionPage(typeof(OptionsProvider.ConfigurationOptionsOptions), "TestExtension", "ConfigurationOptions", 0, 0, true, SupportsProfiles = true)]
        [ComVisible(true)]
        public class ConfigurationOptionsOptions : BaseOptionPage<ConfigurationOptions> { }
    }

    public class ConfigurationOptions : BaseOptionModel<ConfigurationOptions>
    {
        [Category("OpenAI Refactor")]
        [DisplayName("OpenAI APIKey")]
        [Description("The OpenAI APIKey received from https://platform.openai.com/account/api-keys")]
        [DefaultValue("[Enter OpenAI ApiKey]")]
        public string OpenAI_ApiKey { get; set; } = "[Enter OpenAI ApiKey]";
    }
}

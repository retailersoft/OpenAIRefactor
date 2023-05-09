global using Community.VisualStudio.Toolkit;
global using Community.VisualStudio.Toolkit.DependencyInjection.Microsoft;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.VisualStudio.Shell;
global using Newtonsoft.Json;
global using OpenAI_Refactor.Commands;
global using OpenAI_Refactor.Models;
global using OpenAI_Refactor.Models.Common;
global using OpenAI_Refactor.Services;
global using OpenAI_Refactor.Settings;
global using System;
global using System.Threading;
global using System.Threading.Tasks;

using System.Runtime.InteropServices;

namespace OpenAI_Refactor;

[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
[ProvideOptionPage(typeof(OptionsProvider.ConfigurationOptionsOptions), "OpenAI Refactor", "Settings", 0, 0, true, SupportsProfiles = true)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[Guid(PackageGuids.OpenAI_RefactorString)]
public sealed class OpenAI_RefactorPackage : MicrosoftDIToolkitPackage<OpenAI_RefactorPackage>
{
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        try
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            //// Commands
            await RefactorCommand.InitializeAsync(this);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    protected override void InitializeServices(IServiceCollection services)
    {
        services.AddSingleton<IChatGptSettingsService, ChatGptSettingsService>();
        services.AddSingleton<IApiHttpService, ApiHttpService>();
        services.AddSingleton<IChatCompletionService, ChatCompletionService>();
        services.RegisterCommands(ServiceLifetime.Singleton);
    }
}
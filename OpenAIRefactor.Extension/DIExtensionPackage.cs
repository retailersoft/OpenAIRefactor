using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Microsoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using OpenAIRefactor;
using OpenAIRefactor.Commands;
using OpenAIRefactor.Services;
using System;
using System.Runtime.InteropServices;

using System.Threading;

using System.Threading.Tasks;


namespace OpenAIRefactor
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuids.OpenAIRefactorString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionsProvider.ConfigurationOptionsOptions), "OpenAI Refactor", "Settings", 0, 0, true, SupportsProfiles = true)]

    public sealed class DIExtensionPackage : MicrosoftDIToolkitPackage<DIExtensionPackage>
    {
        protected override void InitializeServices(IServiceCollection services)
        {
            services.AddSingleton<IChatGptSettingsService, ChatGptSettingsService>();
            services.AddSingleton<IApiHttpService, ApiHttpService>();
            services.AddSingleton<IChatCompletionService, ChatCompletionService>();
            services.RegisterCommands(ServiceLifetime.Singleton);
        }


        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            //// Commands
            await RefactorCommand.InitializeAsync(this);
        }
    }
}

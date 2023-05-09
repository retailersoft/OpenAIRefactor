using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using Microsoft.VisualStudio.Shell;
using System.Threading.Tasks;
using VSSDK.TestExtension;

namespace TestExtension.Commands
{
    [Command(PackageIds.ConfigureCommand)]
    internal sealed class ConfigureCommand : BaseCommand<ConfigureCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IToolkitServiceProvider<DITestExtensionPackage> serviceProvider = await VS.GetServiceAsync<SToolkitServiceProvider<DITestExtensionPackage>, IToolkitServiceProvider<DITestExtensionPackage>>();

            var config = await ConfigurationOptions.GetLiveInstanceAsync();

        }
    }
}

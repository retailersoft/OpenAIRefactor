using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using SharpToken;
using System;
using System.Linq;
using System.Threading.Tasks;
using VSSDK.TestExtension;

namespace TestExtension.Commands
{
    [Command(PackageIds.TestDependencyInjection2)]
    internal sealed class DITestCommand : BaseCommand<DITestCommand>
    {
        GptEncoding gptEncoding;

        public DITestCommand()
        {
            gptEncoding = GptEncoding.GetEncodingForModel("gpt-4");
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IToolkitServiceProvider<DITestExtensionPackage> serviceProvider = await VS.GetServiceAsync<SToolkitServiceProvider<DITestExtensionPackage>, IToolkitServiceProvider<DITestExtensionPackage>>();
            if (serviceProvider != null)
            {
            }
            else
            {
                await VS.MessageBox.ShowErrorAsync($"Service Provider Not Found.");
            }
        }
    }
}

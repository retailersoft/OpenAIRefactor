using System.Threading.Tasks;

namespace OpenAI_Refactor
{
    [Command(PackageIds.MyCommand)]
    internal sealed class MyCommand : BaseCommand<MyCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await VS.MessageBox.ShowWarningAsync("OpenAI_Refactor", "Button clicked");
        }
    }
}

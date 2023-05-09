using Community.VisualStudio.Toolkit;
using Community.VisualStudio.Toolkit.DependencyInjection.Core;
using EnvDTE;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using OpenAIRefactor.Models;
using OpenAIRefactor.Models.ChatCompletions;
using OpenAIRefactor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAIRefactor.Commands
{
    [Command(PackageIds.RefactorCommand)]
    internal class RefactorCommand : BaseCommand<RefactorCommand>
    {
        private DTE dte;
        private EnvDTE80.DTE2 dte2;

        private Dictionary<string, EditorLanguageInfo> EditorLanguageInfos = new Dictionary<string, EditorLanguageInfo>();
        private IVsUIShell uiShell;

        public RefactorCommand()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            uiShell = (IVsUIShell)ServiceProvider.GlobalProvider.GetService(typeof(SVsUIShell));
            Assumes.Present(uiShell);
            dte = (DTE)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
            Assumes.Present(dte);
            dte2 = (EnvDTE80.DTE2)ServiceProvider.GlobalProvider.GetService(typeof(DTE));
        }

        private SyntaxNode FindNode<T>(SnapshotPoint caretPosition, SyntaxNode root) where T : SyntaxNode
        {
            var span = new TextSpan(caretPosition.GetContainingLine().Start, caretPosition.GetContainingLine().End);
            var node = root.FindNode(span, getInnermostNodeForTie: true);
            while (node != null && !(node is T))
            {
                node = node.Parent;
            }
            return node;
        }

        private void FormatDocument()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            dte.ExecuteCommand("Edit.FormatDocument");
        }

        private AccessorDeclarationSyntax GetAccessorFromPropertyNode(PropertyDeclarationSyntax propertyNode, bool isGetAccessor)
        {
            var accessors = propertyNode.AccessorList.Accessors;
            if (accessors != null)
            {
                return accessors.FirstOrDefault(a => a.Keyword.ValueText == (isGetAccessor ? "get" : "set"));
            }
            return null;
        }

        private EditorLanguageInfo GetLanguageInfo()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (dte != null && dte2 != null)
            {

                try
                {

                    EnvDTE.Project project = dte2.ActiveDocument.ProjectItem.ContainingProject;
                    if (EditorLanguageInfos.ContainsKey(project.Name))
                        return EditorLanguageInfos[project.Name];

                    CodeModel codeModel = project.CodeModel;
                    string language = codeModel.Language;

                    if (language == CodeModelLanguageConstants.vsCMLanguageCSharp)
                    {
                        EditorLanguageInfo info = new EditorLanguageInfo();
                        info.VisualStudioVersion = dte.Version;
                        info.Language = "C#";

                        string targetFrameworkMoniker = project.Properties.Item("TargetFrameworkMoniker").Value.ToString();
                        FrameworkName frameworkName = new FrameworkName(targetFrameworkMoniker);
                        Version frameworkVersion = frameworkName.Version;
                        var maxCSharpVersion = 7.3;
                        if (frameworkVersion.Major >= 7)
                            maxCSharpVersion = 11;
                        else if (frameworkVersion.Major == 6)
                            maxCSharpVersion = 10;
                        else if (frameworkVersion.Major == 5)
                            maxCSharpVersion = 9;
                        else if (frameworkVersion.Major == 3)
                            maxCSharpVersion = 7;
                        else if (frameworkVersion.Major == 2 && frameworkVersion.Minor == 1)
                            maxCSharpVersion = 8;
                        else
                            maxCSharpVersion = 7.3;


                        info.Version = maxCSharpVersion.ToString();

                        EditorLanguageInfos.Add(project.Name, info);
                        return info;
                    }
                }
                catch (Exception)
                {
                }
            }
            return null;
        }

        private int GetNumberOfLines(SyntaxNode node)
        {
            var lineSpan = node.SyntaxTree.GetLineSpan(node.Span);
            return lineSpan.EndLinePosition.Line - lineSpan.StartLinePosition.Line + 1;
        }


        private async Task ProcessRefactoringAsync(string refactorCommand, string methodName, string originalMethod, DocumentView documentView, IWpfTextView textView, Span span)
        {
            var fullMessage = refactorCommand + ":\n" + originalMethod;
            var cm = ChatMessage.CreateFromUser(fullMessage);
            ChatCompletionRequest request = new ChatCompletionRequest(cm);

            var textCompletionResponse = await chatCompletionService.GetAsync(request, CancellationToken.None);

            if (textCompletionResponse.IsSuccess && textCompletionResponse.Result.Choices.Any())
            {
                var choice = textCompletionResponse.Result.Choices.FirstOrDefault();
                var text = choice.Message.Content;

                var methodSpan = new SnapshotSpan(textView.TextSnapshot, span);
                documentView.TextBuffer.Replace(methodSpan, text);
                FormatDocument();

                await VS.StatusBar.ShowProgressAsync($"Refactored {methodName} successfully.", 3, 3);

            }
            else
            {
                var errorHead = $"Error Refactoring {methodName}.";
                await VS.StatusBar.ShowProgressAsync(errorHead, 3, 3);

                await VS.MessageBox.ShowErrorAsync(errorHead, textCompletionResponse.ErrorMessage);
            }

        }


        private async Task ProcessMethodNodeAsync(EditorLanguageInfo editorLanguageInfo, DocumentView documentView, IWpfTextView textView, SyntaxTree syntaxTree, MethodDeclarationSyntax methodNode)
        {
            var lines = GetNumberOfLines(methodNode);
            var originalMethod = methodNode.ToFullString();
            string methodName = methodNode.Identifier.ValueText;

            await VS.StatusBar.ShowProgressAsync($"Refactoring Node: {methodName}", 2, 3);
            await ProcessRefactoringAsync(editorLanguageInfo.ChatMessage, methodName, originalMethod, documentView, textView, new Span(methodNode.Span.Start, methodNode.Span.Length));

        }

        private async Task ProcessSelectedTextAsync(EditorLanguageInfo editorLanguageInfo, DocumentView documentView, IWpfTextView textView)
        {
            SnapshotPoint selectionStart = textView.Selection.Start.Position;
            SnapshotPoint selectionEnd = textView.Selection.End.Position;
            SnapshotSpan selectionSpan = new SnapshotSpan(selectionStart, selectionEnd);
            Span selectedSpan = selectionSpan.Span;

            var originalMethod = textView.Selection.SelectedSpans[0].GetText();
            string methodName = "Selection";

            await VS.StatusBar.ShowProgressAsync($"Refactoring Node: {methodName}", 2, 3);

            await ProcessRefactoringAsync(editorLanguageInfo.ChatMessage, methodName, originalMethod, documentView, textView, selectedSpan);

        }



        private async Task ProcessPropertyNodeAsync(EditorLanguageInfo editorLanguageInfo, DocumentView documentView, IWpfTextView textView, SyntaxTree syntaxTree, PropertyDeclarationSyntax node, bool isInGetAccessor)
        {
            var lines = GetNumberOfLines(node);
            bool refactorEntireProperty = lines <= 20;

            string methodName = node.Identifier.ValueText;
            //var language = syntaxTree.Options.Language;
            string originalMethod;
            Span originalNodeSpan;
            if (refactorEntireProperty)
            {
                originalMethod = node.ToFullString();
                originalNodeSpan = new Span(node.Span.Start, node.Span.Length);

                await VS.StatusBar.ShowProgressAsync($"Refactoring Property: {methodName}", 2, 3);
            }
            else
            {
                var assessor = GetAccessorFromPropertyNode(node, isInGetAccessor);
                originalMethod = assessor.ToFullString();
                originalNodeSpan = new Span(assessor.Span.Start, assessor.Span.Length);

                await VS.StatusBar.ShowProgressAsync($"Refactoring Property {(isInGetAccessor ? "[Get]" : "[Set]")}: {methodName}", 2, 3);
            }

            var methodSpan = new SnapshotSpan(textView.TextSnapshot, originalNodeSpan);

            await ProcessRefactoringAsync(editorLanguageInfo.ChatMessage, methodName, originalMethod, documentView, textView, originalNodeSpan);

        }


        private async Task ProcessRefactorAsync(EditorLanguageInfo editorLanguageInfo, DocumentView documentView, IWpfTextView textView, SnapshotPoint caretPosition, string text)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(text);
            if (syntaxTree != null)
            {
                var root = await syntaxTree.GetRootAsync();
                if (TryGetSyntaxNode(caretPosition, root, out MethodDeclarationSyntax methodDeclarationSyntax))
                {
                    await ProcessMethodNodeAsync(editorLanguageInfo, documentView, textView, syntaxTree, methodDeclarationSyntax);
                }
                else if (TryGetPropertyDeclarationSyntax(caretPosition, root, out PropertyDeclarationSyntax propertyDeclarationSyntax, out bool isInGetAccessor))
                {
                    await ProcessPropertyNodeAsync(editorLanguageInfo, documentView, textView, syntaxTree, propertyDeclarationSyntax, isInGetAccessor);
                }


            }
        }

        private int ShowMessage(string title, string message, OLEMSGBUTTON messageButton, OLEMSGDEFBUTTON defaultButton, OLEMSGICON icon)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Guid clsid = Guid.Empty;

            uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       title,
                       message,
                       string.Empty,
                       0,
                       messageButton,
                       defaultButton,
                       icon,
                       0,
                       out int result);
            return result;
        }



        private bool TryGetPropertyDeclarationSyntax(SnapshotPoint caretPosition, SyntaxNode root, out PropertyDeclarationSyntax propertyDeclarationSyntax, out bool isInGetAccessor)
        {
            propertyDeclarationSyntax = null;
            isInGetAccessor = false;

            if (TryGetSyntaxNode(caretPosition, root, out propertyDeclarationSyntax))
            {
                //var propertySyntax = (PropertyDeclarationSyntax)propertyDeclarationSyntax;
                //propertyDeclarationSyntax = propertySyntax;

                if (propertyDeclarationSyntax.AccessorList != null)
                {
                    var getAccessor = propertyDeclarationSyntax.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
                    var setAccessor = propertyDeclarationSyntax.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.SetAccessorDeclaration);

                    if (getAccessor != null && getAccessor.Body != null && getAccessor.Body.FullSpan.Contains(caretPosition))
                    {
                        isInGetAccessor = true;
                    }
                    else if (setAccessor != null && setAccessor.Body != null && setAccessor.Body.FullSpan.Contains(caretPosition))
                    {
                        isInGetAccessor = false;
                    }
                }

                return true;
            }

            return false;
        }

        private bool TryGetSyntaxNode<T>(SnapshotPoint caretPosition, SyntaxNode root, out T syntaxNode) where T : SyntaxNode
        {
            syntaxNode = root.DescendantNodes().OfType<T>()
                .FirstOrDefault(n => n.FullSpan.Contains(caretPosition));

            //syntaxNode = (T)FindNode<T>(caretPosition, root);
            return syntaxNode != null;
        }

        private bool IsBusy
        {
            get { return false; }
            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (value == true)
                {
                    uiShell.SetWaitCursor();
                    uiShell.EnableModeless(0);
                }
                else
                {
                    uiShell.EnableModeless(1);
                }
            }
        }

        IChatCompletionService chatCompletionService;

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            DocumentView docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
            {
                await VS.MessageBox.ShowErrorAsync($"Unable to Process", "No Active Document found!");
                return;
            }

            try
            {
                IsBusy = true;
                IToolkitServiceProvider<DIExtensionPackage> serviceProvider = await VS.GetServiceAsync<SToolkitServiceProvider<DIExtensionPackage>, IToolkitServiceProvider<DIExtensionPackage>>();
                if (serviceProvider != null)
                {
                    IChatGptSettingsService chatGptSettingsService = (IChatGptSettingsService)serviceProvider.GetService(typeof(IChatGptSettingsService)) ?? null;
                    if (chatGptSettingsService == null)
                    {
                        await VS.MessageBox.ShowErrorAsync($"Unable to Process", "ChatGPT Settings Not Found!");
                        return;
                    }
                    chatCompletionService = (IChatCompletionService)serviceProvider.GetService(typeof(IChatCompletionService)) ?? null;

                    if (chatCompletionService == null)
                    {
                        await VS.MessageBox.ShowErrorAsync($"Unable to Process", "OpenAIService was NOT found!");
                    }
                    else
                    {
                        if (chatGptSettingsService.ChatGptApiKeyIsValid == false)
                        {
                            var config = await ConfigurationOptions.GetLiveInstanceAsync();
                            if (string.IsNullOrEmpty(config.OpenAI_ApiKey) || config.OpenAI_ApiKey.Equals("[Enter OpenAI ApiKey]"))
                            {
                                await VS.MessageBox.ShowWarningAsync("Invalid API Key", "A valid API Key must be configured.\n\nConfigure your API Key in Tools/Options/OpenAI Refactor\nand Try Again!");
                                return;
                            }
                            var isValid = await chatCompletionService.ValidateApiKeyAsync(config.OpenAI_ApiKey);
                            if (isValid == false)
                            {
                                chatGptSettingsService.ChatGptApiKeyValidated(false);
                                await VS.MessageBox.ShowWarningAsync("Invalid API Key", "The API Key Entered is invalid!\n\nConfigure your API Key in Tools/Options/OpenAI Refactor\nand Try Again!");
                                return;
                            }
                            chatGptSettingsService.ChatGptApiKeyValidated(true, config.OpenAI_ApiKey);

                        }
                        if (chatGptSettingsService.ChatGptApiKeyIsValid == false)
                        {

                        }

                        await VS.StatusBar.ShowProgressAsync("Identifying Node", 1, 3);
                        var editorLanguageInfo = GetLanguageInfo();
                        if (editorLanguageInfo != null)
                        {
                            DocumentView documentView = await VS.Documents.GetActiveDocumentViewAsync();
                            var textView = documentView.TextView;
                            if (textView.Selection.IsEmpty)
                            {
                                var caret = textView.Caret;
                                SnapshotPoint caretPosition = caret.Position.BufferPosition;
                                string text = documentView.TextBuffer.CurrentSnapshot.GetText();
                                await ProcessRefactorAsync(editorLanguageInfo, documentView, textView, caretPosition, text);
                            }
                            else
                            {
                                await ProcessSelectedTextAsync(editorLanguageInfo, documentView, textView);
                            }
                        }
                        else
                        {
                            await VS.MessageBox.ShowErrorAsync($"Unable to Process", "OpenAIService was NOT found!");
                        }

                    }
                }

            }
            catch (Exception)
            {
            }
            finally
            {
                IsBusy = false;
                var timer = new Timer((s) => { Task.Run(ClearStatusBarAsync).Wait(); }, null, TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(-1));
            }
        }

        private async Task ClearStatusBarAsync()
        {

            await VS.StatusBar.ClearAsync();
        }




    }
}

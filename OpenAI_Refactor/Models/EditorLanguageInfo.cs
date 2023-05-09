namespace OpenAI_Refactor.Models;
public class EditorLanguageInfo
{
    public string Language { get; set; }
    public string Version { get; set; }
    public string VisualStudioVersion { get; set; }
    public string ChatMessage
    {
        get
        {
            return $"Refactor the following code using {Language} Version {Version}\nDont give any explanation\n";
        }
    }
}

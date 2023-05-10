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
            return $"Refactor the {Language} code using Version {Version}\nDont give any explanation";
        }
    }
    public string SystemMessage
    {
        get
        {
            return $"You are an advanced {Language} Programmer";
        }
    }
}

# OpenAIRefactor 
## Visual Studio Extension

### Configuration
- In Visual Studio Tools/Options - search for OpenAI Refactor, and set your API KEY.
![Alt text](docs/images/Settings.jpg "Settings")

### Usage
- Select text in the editor window
-or-
- Based on the cursor position with nothing selected

![Alt text](docs/images/OpenAIRefactor.png "Execute")

### Results will appear in a few moments

+ You must have a valid OpenAPI API Key
+ Sometimes, the refactoring is less that perfect.  Simply undo the change or edit as desired.

### Whats Happening

- The extension identifies the Language and Version of the Code.  (i.e. C# v9.0)
- A chat session is created with OpenAI asking for a code refactor using the language parameters


#### Issues
- Only tested with C#
- Maximum token allowed can limit responses
- This is Version 1 - really?

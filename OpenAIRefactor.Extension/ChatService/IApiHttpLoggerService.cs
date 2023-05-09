using System;
using System.Linq;
using TestExtension.Settings;

namespace TestExtension.ChatService
{
    public interface IApiHttpLoggerService
    {
        IApiHttpLoggerContext Create();
    }

    public class ApiHttpLoggerService : IApiHttpLoggerService
    {

        private readonly bool _isLogEnabled;
        private readonly string _logDirectory;

        public ApiHttpLoggerService()
        {
        }

        public IApiHttpLoggerContext Create()
        {
            return null;
        }

    }
}

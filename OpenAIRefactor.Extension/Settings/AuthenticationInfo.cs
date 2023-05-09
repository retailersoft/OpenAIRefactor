using Newtonsoft.Json;
using System;

namespace OpenAIRefactor.Settings
{
    public class AuthenticationInfo
    {

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public const string OPENAI_KEY = "OPENAI_KEY";
        public const string OPENAI_API_KEY = "OPENAI_API_KEY";
        public const string OPENAI_SECRET_KEY = "OPENAI_SECRET_KEY";
        public const string TEST_OPENAI_SECRET_KEY = "TEST_OPENAI_SECRET_KEY";
        public const string ORGANIZATION = "ORGANIZATION";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private static readonly object DEFAULT_LOCK_OBJECT = new object();
        private static AuthenticationInfo DEFAULT;

        private string _apiKey;
        private string _organization;

        /// <summary>Initializes a new instance of the <see cref="AuthenticationInfo" /> class.</summary>
        public AuthenticationInfo()
        {
        }

        public AuthenticationInfo(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentNullException(nameof(apiKey));

            if (!apiKey.Contains("sk-"))
            {
                throw new ArgumentException($"{apiKey} parameter must start with 'sk-'");
            }

            ApiKey = apiKey;
        }

        [JsonConstructor]
        public AuthenticationInfo(string apiKey, string organization)
            : this(apiKey)
        {
            if (!string.IsNullOrWhiteSpace(organization))
            {
                if (!organization.Contains("org-"))
                {
                    throw new ArgumentException($"{nameof(organization)} parameter must start with 'org-'");
                }

                Organization = organization;
            }
        }

        [JsonProperty("apiKey")]
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));

                if (!value.Contains("sk-"))
                {
                    throw new ArgumentException($"{value} parameter must start with 'sk-'");
                }

                _apiKey = value;
            }
        }

        [JsonProperty("organization")]
        public string Organization
        {
            get => _organization;
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    if (!value.Contains("org-"))
                    {
                        throw new ArgumentException($"{nameof(value)} parameter must start with 'org-'");
                    }

                    _organization = value;
                }
                else
                {
                    _organization = value;
                }
            }
        }

        public static implicit operator AuthenticationInfo(string key) => new AuthenticationInfo(key);

        public static AuthenticationInfo Default
        {
            get
            {
                if (DEFAULT == null)
                {
                    lock (DEFAULT_LOCK_OBJECT)
                    {
                        if (DEFAULT == null)
                        {
                            DEFAULT = new AuthenticationInfo()
                            {
                                ApiKey = string.Empty,
                                Organization = "org-retailersoft"
                            };
                        }
                    }
                }

                return DEFAULT;
            }
            set
            {
                lock (DEFAULT_LOCK_OBJECT) DEFAULT = value;
            }
        }


        public static AuthenticationInfo LoadFromEnvironmentVariables(string organization = null)
        {
            var apiKey = Environment.GetEnvironmentVariable(OPENAI_KEY);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable(OPENAI_API_KEY);
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable(OPENAI_SECRET_KEY);
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable(TEST_OPENAI_SECRET_KEY);
            }

            return string.IsNullOrEmpty(apiKey) ? null : new AuthenticationInfo(apiKey, organization);
        }



    }
}

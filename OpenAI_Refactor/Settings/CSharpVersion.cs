using System;
using System.Linq;
using System.Runtime.Versioning;

namespace OpenAI_Refactor.Settings;
internal class CSharpVersion
{

    internal static decimal HighestLanguageVersion(FrameworkName frameworkName, Version frameworkVersion)
    {
        switch (frameworkName.FrameworkType())
        {
            case NetFrameworkType.NETFramework:
                return NETFrameworkLanguageVersion(frameworkVersion);
            case NetFrameworkType.NETStandard:
                return NETStandardLanguageVersion(frameworkVersion);
            case NetFrameworkType.NETCore:
                return NETCoreLanguageVersion(frameworkVersion);
            case NetFrameworkType.Unknown:
            default:
                return 7.0m;
        }
    }

    static decimal NETCoreLanguageVersion(Version frameworkVersion)
    {
        if (frameworkVersion >= new Version(5, 0))
        {
            // .NET Core 5.0 and later versions support C# 9.0
            return 9.0m;
        }
        else if (frameworkVersion >= new Version(3, 1))
        {
            // .NET Core 3.1 and later versions support C# 8.0
            return 8.0m;
        }
        else if (frameworkVersion >= new Version(2, 1))
        {
            // .NET Core 2.1 and later versions support C# 7.3
            return 7.3m;
        }
        else
        {
            // Default to C# 7.0 for earlier versions of .NET Core
            return 7.0m;
        }
    }

    static decimal NETStandardLanguageVersion(Version frameworkVersion)
    {
        if (frameworkVersion >= new Version(2, 1))
        {
            // .NET Standard 2.1 and later versions support C# 8.0
            return 8.0m;
        }
        else if (frameworkVersion >= new Version(2, 0))
        {
            // .NET Standard 2.0 supports C# 7.3
            return 7.3m;
        }
        else
        {
            // Default to C# 7.0 for earlier versions of .NET Standard
            return 7.0m;
        }
    }

    static decimal NETFrameworkLanguageVersion(Version frameworkVersion)
    {
        if (frameworkVersion >= new Version(4, 8))
        {
            // .NET Framework 4.8 and later versions support C# 7.3
            return 7.3m;
        }
        else if (frameworkVersion >= new Version(4, 7))
        {
            // .NET Framework 4.7 and later versions support C# 7.0
            return 7.0m;
        }
        else if (frameworkVersion >= new Version(4, 6))
        {
            // .NET Framework 4.6 and later versions support C# 6.0
            return 6.0m;
        }
        else
        {
            // Default to C# 5.0 for earlier versions of .NET Framework
            return 5.0m;
        }
    }

}

public static class StringExtensions
{
    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
        return source.IndexOf(toCheck, comp) >= 0;
    }

    public static bool ContainsIgnoreCase(this string str, string value)
    {
        return str.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}


public enum NetFrameworkType
{
    Unknown,
    NETFramework,
    NETStandard,
    NETCore
}
public static class FrameworkNameExtensions
{
    public static NetFrameworkType FrameworkType(this FrameworkName frameworkName)
    {
        switch (frameworkName.Identifier)
        {
            case ".NETFramework":
                return NetFrameworkType.NETFramework;
            case ".NETStandard":
                return NetFrameworkType.NETStandard;
            case ".NETCoreApp":
                return NetFrameworkType.NETCore;
            default:
                return NetFrameworkType.Unknown;
        }
    }
}



//.NET Framework:

//".NETFramework,Version=v4.8"
//".NETFramework,Version=v4.7.2"
//".NETFramework,Version=v4.6.1"
//...
//.NET Core:

//".NETCoreApp,Version=v3.1"
//".NETCoreApp,Version=v5.0"
//".NETCoreApp,Version=v6.0"
//...
//.NET Standard:

//".NETStandard,Version=v2.0"
//".NETStandard,Version=v2.1"

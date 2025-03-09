using System.Text.RegularExpressions;

namespace Multiscreen.Util;

public static class StringExtensions
{
    /// <summary>
    /// Splits a camel case string into separate words with spaces
    /// </summary>
    public static string SplitCamelCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // This regex looks for:
        // - A lowercase letter followed by an uppercase letter (e.g., "tT" in "timeTable")
        // - Multiple uppercase letters followed by a lowercase letter (e.g., "ABc" in "HTMLCode")
        return Regex.Replace(input,
            @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])",
            " ");
    }
}

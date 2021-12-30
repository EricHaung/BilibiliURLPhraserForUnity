using System.Text.RegularExpressions;
using System;

public class UnityHelper
{
    public static string ConvertUnicodeToString(string source)
    {
        if (string.IsNullOrEmpty(source))
            return "";

        return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase).Replace(
          source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
    }
}

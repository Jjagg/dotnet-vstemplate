using System;
using System.Text.RegularExpressions;

namespace VSTemplate
{
    internal partial class Program
    {
        public struct TemplatePropertyMapping
        {
            public Regex IdRegex { get; }
            public string Value { get; }

            public TemplatePropertyMapping(string argument)
            {
                if (argument.Contains('='))
                {
                    var parts = argument.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                    IdRegex = StrToRegex(parts[0]);
                    Value = parts.Length > 1 ? parts[1] : "";
                }
                else
                {
                    IdRegex = new Regex(".*");
                    Value = argument;
                }
            }

            public TemplatePropertyMapping(string idRegex, string value)
            {
                IdRegex = new Regex('^' + idRegex.Replace("*", ".*") + '$', RegexOptions.IgnoreCase);
                Value = value;
            }

            private static Regex StrToRegex(string s)
                => new Regex('^' + s.Replace("*", ".*") + '$', RegexOptions.IgnoreCase);

            public bool AppliesTo(string identity)
                => IdRegex.IsMatch(identity);
        }
    }
}

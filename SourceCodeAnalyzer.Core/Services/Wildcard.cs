using System.Text.RegularExpressions;

namespace SourceCodeAnalyzer.Core.Services
{
    public class Wildcard
    {
        private readonly Regex _regex;

        public Wildcard(string pattern)
        {
            pattern = pattern.Replace(".", @"\.");
            pattern = pattern.Replace("?", ".");
            pattern = pattern.Replace("*", ".*");
            _regex = new Regex($"^{pattern}$", RegexOptions.IgnoreCase);
        }

        public bool IsMatch(string input)
        {
            return _regex.IsMatch(input);
        }
    }
}
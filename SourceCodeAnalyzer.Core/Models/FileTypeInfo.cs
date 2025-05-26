namespace SourceCodeAnalyzer.Core.Models
{
    public class FileTypeInfo
    {
        public string LanguageName { get; }
        public string LineComment { get; }
        public string BlockCommentStart { get; }
        public string BlockCommentEnd { get; }

        public FileTypeInfo(string languageName, string lineComment, string blockCommentStart, string blockCommentEnd)
        {
            LanguageName = languageName;
            LineComment = lineComment;
            BlockCommentStart = blockCommentStart;
            BlockCommentEnd = blockCommentEnd;
        }
    }
}

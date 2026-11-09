using System;
using System.Text.RegularExpressions;

namespace LongJump
{
    /// <summary>A file of the game's own to attach to a bug report, such as a save. At most 10MB.</summary>
    /// <remarks>
    /// Its checks run when the file is attached, so a game can leave out a bad file and still send the
    /// report; the server would refuse the whole report.
    /// </remarks>
    public sealed class IssueFile
    {
        public const int MaxBytes = 10 * 1024 * 1024;
        // The server's rule: ASCII only, so not .NET's \w, which matches any letter.
        static readonly Regex NamePattern = new Regex(@"^[A-Za-z0-9_.-]{1,100}\z");

        internal string Name { get; }
        internal string Filename { get; }
        internal byte[] Bytes { get; }
        internal string ContentType { get; }

        /// <param name="name">Unique within the issue, such as "save": 1 to 100 letters, digits, '_', '.' or '-'.</param>
        public IssueFile(string name, string filename, byte[] bytes, string contentType = "application/octet-stream")
        {
            if (name == null || !NamePattern.IsMatch(name)) throw new ArgumentException("Use 1 to 100 letters, digits, '_', '.' or '-'.", nameof(name));
            if (string.IsNullOrEmpty(filename) || filename.Length > 255) throw new ArgumentException("Use 1 to 255 characters.", nameof(filename));
            if (bytes == null || bytes.Length == 0) throw new ArgumentException("The file is empty.", nameof(bytes));
            if (bytes.Length > MaxBytes) throw new ArgumentException("Files are limited to 10MB.", nameof(bytes));

            Name = name;
            Filename = filename;
            Bytes = bytes;
            ContentType = contentType;
        }
    }
}

using System;
using System.IO;

namespace VSTemplate
{
    public class Logger
    {
        private TextWriter _writer;
        private int _indent;

        public bool ColorizeOutput { get; set; }

        public Logger()
        {
            _writer = Console.Out;
        }

        public void Indent()
        {
            _indent++;
        }

        public void Dedent()
        {
            _indent--;
        }

        public void Log(string text)
        {
            Write(text);
        }

        public void LogWarning(string text)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Write("Warning: " + text);
            Console.ResetColor();
        }

        public void LogError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Write("Error: " + text);
            Console.ResetColor();
        }

        private void Write(string text)
        {
            _writer.WriteLine(new string(' ', _indent * 2) + text);
        }
    }
}

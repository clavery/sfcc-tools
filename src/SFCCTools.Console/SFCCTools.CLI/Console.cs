using System;
using McMaster.Extensions.CommandLineUtils;

namespace SFCCTools.CLI
{
    public interface IConsoleOutput
    {
        void WriteLine(string line);
        void Write(string str);
        void Yellow(string str, string eol = "\n");
        void Red(string str, string eol = "\n");
        string ReadLine();
        void Blue(string str, string eol = "\n");
        void Green(string str, string eol = "\n");
        void ClearLine();
        void WriteColor(ConsoleColor color, string str, string eol = "\n");
        ProgressBar CreateProgressBar();
    }

    public class ColorConsole : IConsoleOutput
    {
        public void WriteLine(string line)
        {
            Console.WriteLine(line);
        }

        public void Write(string line)
        {
            Console.Write(line);
        }

        public void WriteColor(ConsoleColor color, string str, string eol = "\n")
        {
            Console.ForegroundColor = color;
            Console.Write(str + eol);
            Console.ResetColor();
        }

        public void Yellow(string str, string eol = "\n") => WriteColor(ConsoleColor.DarkYellow, str, eol);
        public void Blue(string str, string eol = "\n") => WriteColor(ConsoleColor.DarkBlue, str, eol);
        public void Green(string str, string eol = "\n") => WriteColor(ConsoleColor.DarkGreen, str, eol);
        public void Red(string str, string eol = "\n") => WriteColor(ConsoleColor.DarkRed, str, eol);

        public void ClearLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        public string ReadLine() => Console.In.ReadLine();

        public ProgressBar CreateProgressBar()
        {
            return new ProgressBar(this);
        }
    }

    public class ProgressBar
    {
        private readonly IConsoleOutput _console;
        private bool _started;

        public ProgressBar(IConsoleOutput console)
        {
            _console = console;
            _started = false;
        }

        public void ProgressHandler(object sender, long current, long total)
        {
            if (!_started)
            {
                _console.WriteLine($"0%".PadLeft(7));
                _started = true;
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }

            double percent = (double) current / (double) total * 100;
            _console.ClearLine();
            _console.Write($"{percent:F1}%".PadLeft(7));

            if (current == total)
            {
                _console.WriteLine("");
            }
        }
    }
}
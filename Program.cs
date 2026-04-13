using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AssemblyLexer
{
    public interface IFileSystem
    {
        string ReadAllText(string path);
    }

    public interface IConsole
    {
        void WriteLine(string message);
        void WriteError(string message);
        string ReadToEnd();
    }

    public class LexerApp
    {
        private readonly IFileSystem _fileSystem;
        private readonly IConsole _console;

        // Структура для зберігання правил
        struct Rule
        {
            public Regex Pattern { get; }
            public string Type { get; }

            public Rule(string pattern, string type)
            {
                Pattern = new Regex(pattern, RegexOptions.Compiled);
                Type = type;
            }
        }

        public LexerApp(IFileSystem fileSystem, IConsole console)
        {
            _fileSystem = fileSystem;
            _console = console;
        }

        public int Run(string[] args)
        {
            string input;

            if (args.Length >= 1)
            {
                try
                {
                    input = _fileSystem.ReadAllText(args[0]);
                }
                catch (Exception)
                {
                    _console.WriteError($"Cannot open file: {args[0]}");
                    return 1;
                }
            }
            else
            {
                input = _console.ReadToEnd();
            }


            List<Rule> rules = new List<Rule>
            {
                new Rule(@"^0[xX][0-9A-Fa-f]+", "NUMBER_HEX"),
                new Rule(@"^[0-9A-Fa-f]+[hH]", "NUMBER_HEX"),
                new Rule(@"^[0-9]+\.[0-9]*([eE][+-]?[0-9]+)?", "NUMBER_FLOAT"),
                new Rule(@"^[0-9]*\.[0-9]+([eE][+-]?[0-9]+)?", "NUMBER_FLOAT"),
                new Rule(@"^[0-9]+", "NUMBER_DEC"),
                new Rule(@"^""([^""\\]|\\.)*""", "STRING"),
                new Rule(@"^'([^'\\]|\\.)'", "CHAR"),
                new Rule(@"^;[^\n]*", "COMMENT"),
                new Rule(@"^//[^\n]*", "COMMENT"),
                new Rule(@"^/\*[\s\S]*?\*/", "COMMENT"),
                new Rule(@"^\.[A-Za-z_][A-Za-z0-9_]*", "DIRECTIVE"),
                new Rule(@"^%[A-Za-z_][A-Za-z0-9_]*", "DIRECTIVE"),
                new Rule(@"^\$", "SPECIAL"),
                new Rule(@"^[A-Za-z_][A-Za-z0-9_]*", "IDENTIFIER"),
                new Rule(@"^[+\-*/=<>!&|^~]+", "OPERATOR"),
                new Rule(@"^[,:()\[\]{}]", "SEPARATOR"),
                new Rule(@"^#.*", "DIRECTIVE")
            };

            HashSet<string> reserved = new HashSet<string> { "mov", "ret" };
            HashSet<string> regNames = new HashSet<string> { "ax", "bx" };

            int pos = 0;
            int line = 1, col = 1;

            while (pos < input.Length)
            {
                char currentChar = input[pos];

                if (char.IsWhiteSpace(currentChar))
                {
                    if (currentChar == '\n') { line++; col = 1; }
                    else { col++; }
                    pos++;
                    continue;
                }

                bool matched = false;
                string rest = input.Substring(pos);

                foreach (var rule in rules)
                {
                    Match match = rule.Pattern.Match(rest);
                    if (match.Success)
                    {
                        string lexeme = match.Value;
                        string type = rule.Type;

                        if (type == "IDENTIFIER")
                        {
                            string low = lexeme.ToLower();
                            if (reserved.Contains(low)) type = "RESERVED";
                            else if (regNames.Contains(low)) type = "REGISTER";
                        }

                        // Замість Console.WriteLine використовуємо інтерфейс
                        _console.WriteLine($"<{lexeme} , {type}>  // line {line} col {col}");

                        pos += lexeme.Length;
                        col += lexeme.Length;
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    _console.WriteLine($"<{input[pos]} , ERROR>  // line {line} col {col}");
                    pos++;
                    col++;
                }
            }

            return 0;
        }
    }
    public class RealFileSystem : IFileSystem
    {
        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
    }

    public class RealConsole : IConsole
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteError(string message)
        {
            Console.Error.WriteLine(message);
        }

        public string ReadToEnd()
        {
            return Console.In.ReadToEnd();
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            IFileSystem realFileSystem = new RealFileSystem();
            IConsole realConsole = new RealConsole();

            var app = new LexerApp(realFileSystem, realConsole);

            return app.Run(args);
        }
    }
}
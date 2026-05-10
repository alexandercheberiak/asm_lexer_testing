using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AssemblyLexer
{
    public class Program
    {
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

        private static readonly List<Rule> Rules = new List<Rule>
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

        private static readonly HashSet<string> ReservedWords = new HashSet<string>
        {
            "mov","add","sub","mul","div","inc","dec","push","pop",
            "jmp","je","jne","jg","jl","cmp","call","ret","int","syscall",
            "xor","and","or","not","shr","shl","db","dw","dd","dq",
            "section","global","extern","proc","endp","equ","org"
        };

        private static readonly HashSet<string> RegNames = new HashSet<string>
        {
            "ax","bx","cx","dx","si","di","sp","bp",
            "ip","cs","ds","es","ss","rdi","rsi","rdx","rax"
        };

        public static int Main(string[] args)
        {
            if (!TryGetInput(args, out string? input) || input == null)
            {
                string fileName = args.Length > 0 ? args[0] : "Standard Input";
                Console.Error.WriteLine($"Cannot open file: {fileName}");
                return 1;
            }

            AnalyzeInput(input);
            return 0;
        }

        private static bool TryGetInput(string[] args, out string? input)
        {
            if (args.Length == 0)
            {
                input = Console.In.ReadToEnd();
                return input != null;
            }

            try
            {
                input = File.ReadAllText(args[0]);
                return true;
            }
            catch
            {
                input = null;
                return false;
            }
        }

        private static void AnalyzeInput(string input)
        {
            int pos = 0;
            int line = 1, col = 1;

            while (pos < input.Length)
            {
                char currentChar = input[pos];

                if (char.IsWhiteSpace(currentChar))
                {
                    HandleWhitespace(currentChar, ref line, ref col, ref pos);
                    continue;
                }

                if (TryMatchToken(input, pos, out string? lexeme, out string? type) && lexeme != null)
                {
                    Console.WriteLine($"<{lexeme} , {type}>  // line {line} col {col}");
                    pos += lexeme.Length;
                    col += lexeme.Length;
                }
                else
                {
                    Console.WriteLine($"<{currentChar} , ERROR>  // line {line} col {col}");
                    pos++;
                    col++;
                }
            }
        }

        private static void HandleWhitespace(char currentChar, ref int line, ref int col, ref int pos)
        {
            if (currentChar == '\n')
            {
                line++;
                col = 1;
            }
            else
            {
                col++;
            }
            pos++;
        }

        private static bool TryMatchToken(string input, int pos, out string? lexeme, out string? type)
        {
            string rest = input.Substring(pos);

            foreach (var rule in Rules)
            {
                Match match = rule.Pattern.Match(rest);
                if (match.Success)
                {
                    lexeme = match.Value;
                    type = rule.Type == "IDENTIFIER" ? ResolveIdentifierType(lexeme) : rule.Type;
                    return true;
                }
            }

            lexeme = null;
            type = null;
            return false;
        }

        private static string ResolveIdentifierType(string lexeme)
        {
            string low = lexeme.ToLower();

            if (ReservedWords.Contains(low)) return "RESERVED";
            if (RegNames.Contains(low)) return "REGISTER";
            if (lexeme.Length > 0 && (lexeme[0] == '_' || char.IsLetter(lexeme[0]))) return "IDENTIFIER";
            
            return "ERROR";
        }
    }
}
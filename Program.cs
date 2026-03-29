using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace AssemblyLexer
{
    public class Program
    {
        // Структура для зберігання правил (регулярний вираз + тип)
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

        public static int Main(string[] args)
        {
            string input;

            // Читання з файлу або стандартного вводу
            if (args.Length >= 1)
            {
                try
                {
                    input = File.ReadAllText(args[0]);
                }
                catch (Exception)
                {
                    Console.Error.WriteLine($"Cannot open file: {args[0]}");
                    return 1;
                }
            }
            else
            {
                input = Console.In.ReadToEnd();
            }

            // Регулярні вирази для кожного типу лексем
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

            // Зарезервовані слова асемблера
            HashSet<string> reserved = new HashSet<string>
            {
                "mov","add","sub","mul","div","inc","dec","push","pop",
                "jmp","je","jne","jg","jl","cmp","call","ret","int","syscall",
                "xor","and","or","not","shr","shl","db","dw","dd","dq",
                "section","global","extern","proc","endp","equ","org"
            };

            // Назви регістрів
            HashSet<string> regNames = new HashSet<string>
            {
                "ax","bx","cx","dx","si","di","sp","bp",
                "ip","cs","ds","es","ss","rdi","rsi","rdx","rax"
            };

            int pos = 0;
            int line = 1, col = 1;

            while (pos < input.Length)
            {
                char currentChar = input[pos];

                // Пропуск пробілів
                if (char.IsWhiteSpace(currentChar))
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
                    continue;
                }

                bool matched = false;
                // Отримуємо підрядок, що залишився
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
                            // Якщо токен не є reserved або регістром, не починається з _ або букви, то ERROR
                            string low = lexeme.ToLower();

                            if (reserved.Contains(low))
                            {
                                type = "RESERVED";
                            }
                            else if (regNames.Contains(low))
                            {
                                type = "REGISTER";
                            }
                            else if (lexeme[0] == '_' || char.IsLetter(lexeme[0]))
                            {
                                type = "IDENTIFIER";
                            }
                            else
                            {
                                type = "ERROR";
                            }
                        }

                        Console.WriteLine($"<{lexeme} , {type}>  // line {line} col {col}");

                        pos += lexeme.Length;
                        col += lexeme.Length;
                        matched = true;
                        break;
                    }
                }

                // Якщо жоден шаблон не підійшов
                if (!matched)
                {
                    Console.WriteLine($"<{input[pos]} , ERROR>  // line {line} col {col}");
                    pos++;
                    col++;
                }
            }

            return 0;
        }
    }
}
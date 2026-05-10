using NUnit.Framework;
using System;
using System.IO;
using AssemblyLexer;

namespace AssemblyLexer.Tests
{
    [TestFixture]
    public class LexerTests
    {
        private StringWriter _consoleOut = null!;
        private StringWriter _consoleError = null!;
        private static readonly string[] NonExistentFileArgs = { "non_existent_file_12345.asm" };

        // 1. Setup (fixture) метод: перехоплення консолі перед кожним тестом
        [SetUp]
        public void Setup()
        {
            _consoleOut = new StringWriter();
            _consoleError = new StringWriter();

            Console.SetOut(_consoleOut);
            Console.SetError(_consoleError);
        }

        [TearDown]
        public void TearDown()
        {
            _consoleOut.Dispose();
            _consoleError.Dispose();

            StreamWriter standardOut = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(standardOut);
        }

        // 2. Параметризований тестовий метод
        [TestCase("mov", "<mov , RESERVED>")]
        [TestCase("ax", "<ax , REGISTER>")]
        [TestCase("0x1A", "<0x1A , NUMBER_HEX>")]
        [TestCase("100", "<100 , NUMBER_DEC>")]
        [TestCase("12.5e-3", "<12.5e-3 , NUMBER_FLOAT>")]
        [TestCase("; коментар", "<; коментар , COMMENT>")]
        [TestCase("$", "<$ , SPECIAL>")]
        [TestCase("+", "<+ , OPERATOR>")]
        [TestCase("[", "<[ , SEPARATOR>")]
        public void ValidInputPrintsCorrectToken(string input, string expectedTokenOutput)
        {

            Console.SetIn(new StringReader(input));

            int exitCode = Program.Main(Array.Empty<string>());
            string output = _consoleOut.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.EqualTo(0), "Програма має завершитись успішно.");

                Assert.That(output, Does.Contain(expectedTokenOutput), $"Вивід має містити токен: {expectedTokenOutput}");
            });
        }

        // 3. Тестування виключень (Exceptions)
        [Test]
        public void StreamReadFails_ThrowsException()
        {
            var throwingReader = new ThrowingTextReader();
            Console.SetIn(throwingReader);

            Assert.Throws<InvalidOperationException>(() =>
            {
                Program.Main(Array.Empty<string>());
            }, "Має викидатися InvalidOperationException, якщо читання потоку перервано.");
        }

        [Test]
        public void ProgramWithErrors_FindsErrors()
        {
            // імітуємо багаторядковий ввід з помилковими символами
            string input = "mov ax, 10\n?invalid";
            Console.SetIn(new StringReader(input));

            Program.Main(Array.Empty<string>());

            string[] outputLines = _consoleOut.ToString().Split(
                new[] { Environment.NewLine },
                StringSplitOptions.RemoveEmptyEntries
            );

            // Assert #4
            Assert.Multiple(() =>
            {
                Assert.That(outputLines, Has.Length.GreaterThanOrEqualTo(5), "Має бути згенеровано мінімум 5 токенів.");

                Assert.That(outputLines, Has.Exactly(1).Contains("ERROR"), "У виводі має бути рівно один невідомий токен (ERROR).");

                Assert.That(outputLines, Has.Some.Contains("<invalid , IDENTIFIER>"), "Слово 'invalid' має розпізнатись як ідентифікатор.");
            });
        }

        [Test]
        public void MissingFileReturnsErrorCode()
        {
            int exitCode = Program.Main(NonExistentFileArgs);
            string errorOutput = _consoleError.ToString();

            Assert.Multiple(() =>
            {
                Assert.That(exitCode, Is.EqualTo(1));
                Assert.That(errorOutput, Does.StartWith("Cannot open file:"));
            });
        }

        // імітація падіння потоку вводу
        private sealed class ThrowingTextReader : StringReader
        {
            public ThrowingTextReader() : base("") { }

            public override string ReadToEnd()
            {
                throw new InvalidOperationException("Імітація помилки читання потоку");
            }
        }
    }
}
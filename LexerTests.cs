using System;
using System.IO;
using NUnit.Framework;
using NSubstitute; 
using AssemblyLexer;

namespace AssemblyLexer.Tests
{
    [TestFixture]
    public class LexerAppTests
    {
        // Обробка виключень
        [Test]
        public void FileNotReadable()
        {
            var mockFileSystem = Substitute.For<IFileSystem>();
            var mockConsole = Substitute.For<IConsole>();

            mockFileSystem
                .ReadAllText(Arg.Is<string>(path => path.EndsWith(".asm")))
                .Returns(x => throw new UnauthorizedAccessException());

            var app = new LexerApp(mockFileSystem, mockConsole);

            int result = app.Run(new[] { "error_file.asm" });

            Assert.That(result, Is.EqualTo(1));
            
            mockConsole.Received(1).WriteError("Cannot open file: error_file.asm");
        }

        [Test]
        public void NumberAndOrder()
        {
            var mockFileSystem = Substitute.For<IFileSystem>();
            var mockConsole = Substitute.For<IConsole>();

            mockFileSystem.ReadAllText("test.asm").Returns("ret");

            var app = new LexerApp(mockFileSystem, mockConsole);

            int result = app.Run(new[] { "test.asm" });

            Assert.That(result, Is.EqualTo(0));

            mockFileSystem.Received(1).ReadAllText(Arg.Any<string>());

            Received.InOrder(() =>
            {
                mockFileSystem.ReadAllText("test.asm");
                mockConsole.WriteLine("<ret , RESERVED>  // line 1 col 1");
            });
        }

        // Різні відповіді для кожного наступного виклику методу.
        [Test]
        public void DifferentResults()
        {
            var mockFileSystem = Substitute.For<IFileSystem>();
            var mockConsole = Substitute.For<IConsole>();

            mockFileSystem.ReadAllText("testfile.asm").Returns(
                x => "mov",
                x => "ax",
                x => throw new FileNotFoundException()
            );

            var app = new LexerApp(mockFileSystem, mockConsole);

            app.Run(new[] { "testfile.asm" }); // Обробляє "mov"
            app.Run(new[] { "testfile.asm" }); // Обробляє "ax"

            mockFileSystem.Received(2).ReadAllText("testfile.asm");

            mockConsole.Received(1).WriteLine("<mov , RESERVED>  // line 1 col 1");
            mockConsole.Received(1).WriteLine("<ax , REGISTER>  // line 1 col 1");
        }
    }
}
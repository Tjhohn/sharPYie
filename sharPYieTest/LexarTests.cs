using NUnit.Framework;
using sharPYieLib;

namespace sharPYieTest
{
    [TestFixture]
    public class LexerTests
    {
        [TestCase("x = 1", TokenType.Identifier, TokenType.Assign, TokenType.Integer,TokenType.Newline,TokenType.EOF)]
        [TestCase("a = 2", TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline, TokenType.EOF)]
        [TestCase("a = 2\ny=57", TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline, TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline, TokenType.EOF)]
        [TestCase("a = 2\nb = 3\nc = a + b", TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline,
                  TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline,
                  TokenType.Identifier, TokenType.Assign, TokenType.Identifier, TokenType.Plus, TokenType.Identifier
            , TokenType.Newline, TokenType.EOF)]
        [TestCase("d = 5 / 1", TokenType.Identifier, TokenType.Assign, TokenType.Integer,
                  TokenType.Divide, TokenType.Integer, TokenType.Newline, TokenType.EOF)]
        [TestCase("a = 5\nb = 5\nc = 1\nd = a / b - c", TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline,
                  TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline,
                  TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline,
                  TokenType.Identifier, TokenType.Assign, TokenType.Identifier, TokenType.Divide, TokenType.Identifier,
                  TokenType.Minus, TokenType.Identifier, TokenType.Newline, TokenType.EOF)]
        public void Tokenize_ValidInput_ReturnsCorrectTokens(string input, params TokenType[] expectedTokens)
        {
            var lexer = new Lexer(input);
            var tokens = lexer.Tokenize();

            Assert.AreEqual(expectedTokens.Length, tokens.Count);
            for (int i = 0; i < expectedTokens.Length; i++)
            {
                Assert.AreEqual(expectedTokens[i], tokens[i].Type);
            }
        }

        [Test]
        public void Tokenize_InvalidInput_ThrowsException()
        {
            string input = "x = $"; // Invalid character '$'
            var lexer = new Lexer(input);
            Assert.Throws<Exception>(() => lexer.Tokenize());
        }

        [TestCase("testinputs/firstfunc.py", TokenType.Identifier, TokenType.Assign, TokenType.Integer, 
            TokenType.Newline,
            TokenType.Def, TokenType.Identifier, TokenType.LeftParen, TokenType.Identifier,
            TokenType.RightParen, TokenType.Colon, TokenType.Newline, TokenType.Indent, TokenType.Return,
            TokenType.Identifier, TokenType.Plus, TokenType.Integer, TokenType.Newline,TokenType.Dedent,
            TokenType.Identifier, TokenType.Assign, TokenType.Identifier, TokenType.LeftParen, TokenType.Identifier, 
            TokenType.RightParen, TokenType.Newline, TokenType.Identifier, TokenType.LeftParen, TokenType.Identifier,
            TokenType.RightParen, TokenType.Newline, TokenType.Def, TokenType.Identifier, TokenType.LeftParen,
            TokenType.Identifier, TokenType.Comma, TokenType.Identifier, TokenType.Comma, 
            TokenType.Identifier, TokenType.Comma, TokenType.Identifier, TokenType.RightParen,
            TokenType.Colon, TokenType.Newline, TokenType.Indent, TokenType.Return, TokenType.Identifier,
            TokenType.Plus, TokenType.Identifier, TokenType.Plus, TokenType.Identifier, TokenType.Plus,
            TokenType.Identifier, TokenType.Newline, TokenType.Dedent, TokenType.Identifier, TokenType.LeftParen,
            TokenType.Identifier,TokenType.RightParen, TokenType.Newline, TokenType.EOF)]
        [TestCase("testinputs/stringliteraltoConsole.py", TokenType.Identifier, TokenType.Assign, TokenType.StringLiteral,
            TokenType.Newline,TokenType.Identifier, TokenType.LeftParen, TokenType.Identifier,
            TokenType.RightParen, TokenType.Newline,
            TokenType.EOF)]
        [TestCase("testinputs/simpleAssignment.py", TokenType.Identifier, TokenType.Assign, TokenType.Integer,
            TokenType.Newline,
                  TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Newline,
                  TokenType.Identifier, TokenType.Assign, TokenType.Identifier,
            TokenType.Plus, TokenType.Identifier, TokenType.Newline, TokenType.EOF)] // Assume the file contains "a = 2\nb = 3\nc = a + b"
        public void LexarAST_ValidInputFromFile_ReturnsCorrectResult(string relativePath, params TokenType[] expectedTokens)
        {

            string projectDirectory = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            string filePath = Path.Combine(projectDirectory, relativePath);

            if (!File.Exists(filePath))
            {
                Assert.Fail($"File '{relativePath}' not found in directory '{projectDirectory}'");
            }

            string script = File.ReadAllText(filePath);

            var lexer = new Lexer(script);
            var tokens = lexer.Tokenize();

            //lexer.PrintTokensByType(tokens);

            Assert.AreEqual(expectedTokens.Length, tokens.Count );
            for (int i = 0; i < expectedTokens.Length; i++)
            {
                Assert.AreEqual(expectedTokens[i], tokens[i].Type, $"expected {expectedTokens[i]} got {tokens[i].Type} at index : {i}");
            }
        }
    }
}
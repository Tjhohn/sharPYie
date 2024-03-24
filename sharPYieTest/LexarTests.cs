using NUnit.Framework;
using sharPYieLib;

namespace sharPYieTest
{
    [TestFixture]
    public class LexerTests
    {
        [TestCase("x = 1", TokenType.Identifier, TokenType.Assign, TokenType.Integer)]
        [TestCase("a = 2", TokenType.Identifier, TokenType.Assign, TokenType.Integer)]
        [TestCase("a = 2\ny=57", TokenType.Identifier, TokenType.Assign, TokenType.Integer, TokenType.Identifier, TokenType.Assign, TokenType.Integer)]
        [TestCase("a = 2\nb = 3\nc = a + b", TokenType.Identifier, TokenType.Assign, TokenType.Integer,
                  TokenType.Identifier, TokenType.Assign, TokenType.Integer,
                  TokenType.Identifier, TokenType.Assign, TokenType.Identifier, TokenType.Plus, TokenType.Identifier)]
        [TestCase("d = 5 / 1", TokenType.Identifier, TokenType.Assign, TokenType.Integer,
                  TokenType.Divide, TokenType.Integer)]
        [TestCase("a = 5\nb = 5\nc = 1\nd = a / b - c", TokenType.Identifier, TokenType.Assign, TokenType.Integer,
                  TokenType.Identifier, TokenType.Assign, TokenType.Integer,
                  TokenType.Identifier, TokenType.Assign, TokenType.Integer,
                  TokenType.Identifier, TokenType.Assign, TokenType.Identifier, TokenType.Divide, TokenType.Identifier,
                  TokenType.Minus, TokenType.Identifier)]
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
            Assert.Throws<LexerException>(() => lexer.Tokenize());
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace sharPYieLib
{
    public class Lexer
    {
        private readonly string input;
        private int position;

        public Lexer(string input)
        {
            this.input = input;
            this.position = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (position < input.Length)
            {
                if (char.IsWhiteSpace(input[position]))
                {
                    position++; // Skip whitespace
                }
                else if (char.IsDigit(input[position]))
                {
                    // Tokenize integers
                    string integer = "";
                    while (position < input.Length && char.IsDigit(input[position]))
                    {
                        integer += input[position];
                        position++;
                    }
                    tokens.Add(new Token(TokenType.Integer, integer));
                }
                else if (char.IsLetter(input[position]))
                {
                    // Tokenize identifiers or keywords
                    string identifier = "";
                    while (position < input.Length && (char.IsLetter(input[position]) || char.IsDigit(input[position])))
                    {
                        identifier += input[position];
                        position++;
                    }
                    switch (identifier)
                    {
                        case "if":
                            tokens.Add(new Token(TokenType.If, "if"));
                            break;
                        case "print":
                            tokens.Add(new Token(TokenType.Print, "print"));
                            break;
                        default:
                            tokens.Add(new Token(TokenType.Identifier, identifier));
                            break;
                    }
                }
                else if (input[position] == '"')
                {
                    // Tokenize string literals
                    string literal = "\"";
                    position++;
                    while (position < input.Length && input[position] != '"')
                    {
                        literal += input[position];
                        position++;
                    }
                    if (position == input.Length)
                    {
                        throw new LexerException("Unterminated string literal.");
                    }
                    literal += input[position]; // Add closing quote
                    tokens.Add(new Token(TokenType.StringLiteral, literal));
                    position++;
                }
                else if (input[position] == '=')
                {
                    // Handle '=='
                    if (position + 1 < input.Length && input[position + 1] == '=')
                    {
                        tokens.Add(new Token(TokenType.Equal, "=="));
                        position += 2;
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Assign, "="));
                        position++;
                    }
                }
                else
                {
                    // Handle other token types
                    switch (input[position])
                    {
                        case '/':
                            tokens.Add(new Token(TokenType.Divide, "/"));
                            break;
                        case '-':
                            tokens.Add(new Token(TokenType.Minus, "-"));
                            break;
                        case '+':
                            tokens.Add(new Token(TokenType.Plus, "+"));
                            break;
                        case '(':
                            tokens.Add(new Token(TokenType.LeftParen, "("));
                            break;
                        case ')':
                            tokens.Add(new Token(TokenType.RightParen, ")"));
                            break;
                        case ':':
                            tokens.Add(new Token(TokenType.Colon, ":"));
                            break;
                        default:
                            throw new LexerException($"Unexpected character '{input[position]}' at position {position}.");
                    }
                    position++;
                }
            }

            return tokens;
        }
    }

    public enum TokenType
    {
        Identifier,
        Assign,
        Divide,
        Integer,
        Minus,
        Plus,
        Multiply,
        If,
        Print,
        Equal,
        Colon,
        LeftParen,
        RightParen,
        StringLiteral
        // Add more token types as needed
    }

    public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }

    public class LexerException : Exception
    {
        public LexerException(string message) : base(message)
        {
        }
    }
}

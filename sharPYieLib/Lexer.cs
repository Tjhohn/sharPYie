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
        private int indentationLevel;
        private bool isAtLineBeginning;

        public Lexer(string input)
        {
            // Preprocess input to replace "\r\n" or "\n\r" with "\n" - All windows fault
            this.input = input.Replace("\r\n", "\n").Replace("\n\r", "\n");
            this.position = 0;
            this.indentationLevel = 0;
            this.isAtLineBeginning = true;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            int lineStartPosition = 0; // Track the position of the beginning of the line

            while (position < input.Length)
            {
                var ch = input[position];
                isAtLineBeginning = (position == 0 || input[position - 1] == '\n');
                //if ( position != 0 && input[position - 1] == '\n')
                //{
                //    lineStartPosition = position;
                //}
                // TODO : Lexar needs to not just indent needs to sens undents


                if (isAtLineBeginning && (input[position] == ' ' || input[position] == '\t'))
                {
                    HandleIndentation(tokens, lineStartPosition); // Pass lineStartPosition to HandleIndentation
                }
                else if (char.IsWhiteSpace(input[position]))
                {
                    // Skip regular whitespace characters
                    position++;
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
                        case "def":
                            tokens.Add(new Token(TokenType.Def, "def"));
                            break;
                        case "return":
                            tokens.Add(new Token(TokenType.Return, "return"));
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
                else if (input[position] == '(')
                {
                    // Tokenize left parenthesis
                    tokens.Add(new Token(TokenType.LeftParen, "("));
                    position++;

                    // Tokenize function parameters
                    while (position < input.Length && input[position] != ')')
                    {
                        // Skip whitespace
                        if (char.IsWhiteSpace(input[position]))
                        {
                            position++;
                            continue;
                        }

                        // Tokenize string literals
                        if (input[position] == '"')
                        {
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
                        // Tokenize integer literals
                        else if (char.IsDigit(input[position]))
                        {
                            string integer = "";
                            while (position < input.Length && char.IsDigit(input[position]))
                            {
                                integer += input[position];
                                position++;
                            }
                            tokens.Add(new Token(TokenType.Integer, integer));
                        }

                        // Tokenize parameter identifier
                        if (char.IsLetterOrDigit(input[position]) || input[position] == '_')
                        {
                            string parameter = "";
                            while (position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_'))
                            {
                                parameter += input[position];
                                position++;
                            }
                            tokens.Add(new Token(TokenType.Identifier, parameter));
                        }

                        // Tokenize comma separator
                        if (position < input.Length && input[position] == ',')
                        {
                            tokens.Add(new Token(TokenType.Comma, ","));
                            position++;
                        }
                    }

                    // Tokenize right parenthesis
                    tokens.Add(new Token(TokenType.RightParen, ")"));
                    position++;
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

        private void HandleIndentation(List<Token> tokens, int lineStartPosition)
        {
            int start = lineStartPosition; // Use lineStartPosition as the beginning of the line

            // Count leading spaces or tabs
            while (position < input.Length && (input[position] == ' ' || input[position] == '\t'))
            {
                position++;
            }

            int spaces = position - start;

            // Compare with previous indentation level
            if (spaces > indentationLevel)
            {
                int increase = spaces - indentationLevel;
                for (int i = 0; i < increase; i++)
                {
                    indentationLevel++; 
                }
                tokens.Add(new Token(TokenType.IndentIncrease, "INDENT+"));
            }
            else if (spaces < indentationLevel)
            {
                int decrease = indentationLevel - spaces;
                for (int i = 0; i < decrease; i++)
                {
                    indentationLevel--;
                }
                tokens.Add(new Token(TokenType.IndentDecrease, "INDENT-"));
            }
        }

        public void PrintTokensByType(List<Token> tokens)
        {
            Console.WriteLine("Token types:");
            foreach (Token token in tokens)
            {
                Console.Write($"TokenType.{token.Type}, ");
            }
            Console.WriteLine();
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
        StringLiteral,
        Def,
        Comma,
        Return,
        IndentIncrease,
        IndentDecrease  
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

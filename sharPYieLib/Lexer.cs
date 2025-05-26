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
        private readonly string[] _lines;
        private readonly List<Token> _tokens = new();
        private readonly Stack<int> _indentStack = new();

        private static readonly Dictionary<string, TokenType> KeywordMap = new()
        {
            ["if"] = TokenType.If,
            ["elif"] = TokenType.Elif,
            ["else"] = TokenType.Else,
            ["for"] = TokenType.For,
            ["while"] = TokenType.While,
            ["def"] = TokenType.Def,
            ["class"] = TokenType.Class,
            ["return"] = TokenType.Return,
            ["import"] = TokenType.Import,
            ["from"] = TokenType.From,
            ["as"] = TokenType.As,
            ["pass"] = TokenType.Pass,
            ["break"] = TokenType.Break,
            ["continue"] = TokenType.Continue,
            ["in"] = TokenType.In,
            ["is"] = TokenType.Is,
            ["not"] = TokenType.Not,
            ["and"] = TokenType.And,
            ["or"] = TokenType.Or,
            ["None"] = TokenType.None,
            ["True"] = TokenType.True,
            ["False"] = TokenType.False,
            ["with"] = TokenType.With,
            ["try"] = TokenType.Try,
            ["except"] = TokenType.Except,
            ["finally"] = TokenType.Finally,
            ["raise"] = TokenType.Raise,
            ["global"] = TokenType.Global,
            ["nonlocal"] = TokenType.Nonlocal,
            ["assert"] = TokenType.Assert,
            ["lambda"] = TokenType.Lambda,
            ["yield"] = TokenType.Yield,
            ["await"] = TokenType.Await,
            ["async"] = TokenType.Async
        };

        public Lexer(string input)
        {
            // Preprocess input to replace "\r\n" or "\n\r" with "\n" - All windows fault
            this._lines = input.Replace("\r\n", "\n").Replace("\n\r", "\n").Split("\n");
            _indentStack.Push(0);
        }

        public List<Token> Tokenize()
        {
            for (int i = 0; i < _lines.Length; i++)
            {
                string line = _lines[i];
                ProcessLine(line, i + 1);
            }

            while (_indentStack.Count > 1)
            {
                _tokens.Add(new Token(TokenType.Dedent, "", _lines.Length, 0));
                _indentStack.Pop();
            }

            _tokens.Add(new Token(TokenType.EOF, "", _lines.Length + 1, 0));
            return _tokens;
        }

        private void ProcessLine(string line, int lineNum)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            int indent = line.TakeWhile(char.IsWhiteSpace).Count();
            int col = 0;

            if (indent > _indentStack.Peek())
            {
                _tokens.Add(new Token(TokenType.Indent, "", lineNum, col));
                _indentStack.Push(indent);
            }
            while (indent < _indentStack.Peek())
            {
                _tokens.Add(new Token(TokenType.Dedent, "", lineNum, col));
                _indentStack.Pop();
            }

            for (int pos = indent; pos < line.Length;)
            {
                char c = line[pos];
                col = pos;

                if (char.IsWhiteSpace(c)) { pos++; continue; }

                // COMMENT
                if (c == '#')
                {
                    _tokens.Add(new Token(TokenType.Comment, line[pos..], lineNum, pos));
                    break;
                }

                // IDENTIFIERS & KEYWORDS
                if (char.IsLetter(c) || c == '_')
                {
                    int start = pos;
                    while (pos < line.Length && (char.IsLetterOrDigit(line[pos]) || line[pos] == '_'))
                        pos++;

                    string word = line[start..pos];

                    if (KeywordMap.TryGetValue(word, out var keywordType))
                        _tokens.Add(new Token(keywordType, word, lineNum, start));
                    else
                        _tokens.Add(new Token(TokenType.Identifier, word, lineNum, start));

                    continue;
                }

                // NUMBERS
                if (char.IsDigit(c))
                {
                    int start = pos;
                    while (pos < line.Length && (char.IsDigit(line[pos]) || line[pos] == '.'))
                        pos++;
                    _tokens.Add(new Token(TokenType.Integer, line[start..pos], lineNum, start));
                    continue;
                }

                // STRINGS & F-STRINGS
                if (c == '"' || c == '\'')
                {
                    int start = pos;
                    char quote = c;
                    bool isFString = (start > 0 && line[start - 1] == 'f');

                    pos++;  // skip opening quote
                    while (pos < line.Length && line[pos] != quote)
                    {
                        if (line[pos] == '\\') pos++;  // skip escaped char
                        pos++;
                    }
                    pos++;  // skip closing quote
                    string val = line[start..pos];
                    var type = isFString ? TokenType.Fstring : TokenType.StringLiteral;
                    _tokens.Add(new Token(type, val, lineNum, start));
                    continue;
                }

                // OPERATORS (multi-char first)
                if (line[pos..].StartsWith("=="))
                {
                    _tokens.Add(new Token(TokenType.Equal, "==", lineNum, pos));
                    pos += 2; continue;
                }
                if (line[pos..].StartsWith("!="))
                {
                    _tokens.Add(new Token(TokenType.NotEqual, "!=", lineNum, pos));
                    pos += 2; continue;
                }
                if (line[pos..].StartsWith(">="))
                {
                    _tokens.Add(new Token(TokenType.GreaterEqual, ">=", lineNum, pos));
                    pos += 2; continue;
                }
                if (line[pos..].StartsWith("<="))
                {
                    _tokens.Add(new Token(TokenType.LessEqual, "<=", lineNum, pos));
                    pos += 2; continue;
                }

                // SINGLE-CHAR OPERATORS
                switch (c)
                {
                    case '=': _tokens.Add(new Token(TokenType.Assign, "=", lineNum, pos)); break;
                    case '+': _tokens.Add(new Token(TokenType.Plus, "+", lineNum, pos)); break;
                    case '-': _tokens.Add(new Token(TokenType.Minus, "-", lineNum, pos)); break;
                    case '*': _tokens.Add(new Token(TokenType.Multiply, "*", lineNum, pos)); break;
                    case '/': _tokens.Add(new Token(TokenType.Divide, "/", lineNum, pos)); break;
                    case '>': _tokens.Add(new Token(TokenType.Greater, ">", lineNum, pos)); break;
                    case '<': _tokens.Add(new Token(TokenType.Less, "<", lineNum, pos)); break;

                    case '(': _tokens.Add(new Token(TokenType.LeftParen, "(", lineNum, pos)); break;
                    case ')': _tokens.Add(new Token(TokenType.RightParen, ")", lineNum, pos)); break;
                    case '[': _tokens.Add(new Token(TokenType.LeftBracket, "[", lineNum, pos)); break;
                    case ']': _tokens.Add(new Token(TokenType.RightBracket, "]", lineNum, pos)); break;
                    case '{': _tokens.Add(new Token(TokenType.LeftBrace, "{", lineNum, pos)); break;
                    case '}': _tokens.Add(new Token(TokenType.RightBrace, "}", lineNum, pos)); break;
                    case ':': _tokens.Add(new Token(TokenType.Colon, ":", lineNum, pos)); break;
                    case ',': _tokens.Add(new Token(TokenType.Comma, ",", lineNum, pos)); break;
                    case '.': _tokens.Add(new Token(TokenType.Dot, ".", lineNum, pos)); break;
                    case ';': _tokens.Add(new Token(TokenType.Semicolon, ";", lineNum, pos)); break;

                    default:
                        throw new Exception($"Unexpected character '{c}' at {lineNum}:{pos}");
                }

                pos++;  // advance after single-char token
            }

            _tokens.Add(new Token(TokenType.Newline, "", lineNum, line.Length));
        }

        public string PrintTokensByType(List<Token> tokens)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Token types:");
            foreach (Token token in tokens)
            {
                sb.Append($"TokenType.{token.Type}, ");
            }
            sb.AppendLine(); // To match the final Console.WriteLine() in original version
            return sb.ToString();
        }

    }

    public enum TokenType
{
    Identifier,
    Assign,         // =
    Divide,         // /
    Integer,
    Minus,          // -
    Plus,           // +
    Multiply,       // *
    Equal,          // ==
    NotEqual,       // !=
    Greater,        // >
    GreaterEqual,   // >=
    Less,           // <
    LessEqual,      // <=
    LeftParen,      // (
    RightParen,     // )
    LeftBracket,    // [
    RightBracket,   // ]
    LeftBrace,      // {
    RightBrace,     // }
    Comma,          // ,
    Colon,          // :
    Dot,            // .
    Semicolon,      // ;

    // Keywords
    If,
    Elif,
    Else,
    For,
    While,
    Def,
    Class,
    Return,
    Import,
    From,
    As,
    Pass,
    Break,
    Continue,
    In,
    Is,
    Not,
    And,
    Or,
    None,
    True,
    False,
    With,
    Try,
    Except,
    Finally,
    Raise,
    Global,
    Nonlocal,
    Assert,
    Lambda,
    Yield,
    Await,
    Async,

    // Literals and structure
    StringLiteral,
    Fstring,
    Indent,
    Dedent,
    Comment,
    Newline,
    EOF
}

public class Token
    {
        public TokenType Type { get; }
        public string Value { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Type}({Value}) @ {Line}:{Column}";
        }
    }

    public class LexerException : Exception
    {
        public LexerException(string message) : base(message)
        {
        }
    }
}

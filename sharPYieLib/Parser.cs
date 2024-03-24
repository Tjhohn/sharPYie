using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sharPYieLib
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int position = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
            this.position = 0;
        }

        public List<AstNode> Parse()
        {
            var ast = new List<AstNode>();

            while (position < tokens.Count)
            {
                ast.Add(ParseAssignmentOrStatement());
            }

            return ast;
        }

        private AstNode ParseAssignmentOrStatement()
        {
            Token currentToken = tokens[position];

            if (currentToken.Type == TokenType.Identifier)
            {
                return ParseAssignment();
            }
            else if (currentToken.Type == TokenType.If)
            {
                return ParseIfStatement();
            }
            else if (currentToken.Type == TokenType.Print)
            {
                return ParsePrintStatement();
            }
            else
            {
                throw new ParserException($"Unexpected token '{currentToken.Value}'");
            }
        }

        private AstNode ParseAssignment()
        {
            Token currentToken = tokens[position++]; // Move to the next token

            if (currentToken.Type != TokenType.Identifier)
            {
                throw new ParserException($"Expected an identifier, but found '{currentToken.Value}'");
            }

            string variableName = currentToken.Value;

            if (position >= tokens.Count || tokens[position].Type != TokenType.Assign)
            {
                throw new ParserException("Expected an assignment operator '='");
            }

            // Skip the assignment operator
            position++;

            // Parse the value expression
            AstNode value = ParseExpression();

            return new AssignmentNode(variableName, value);
        }

        private AstNode ParseExpression()
        {
            AstNode left = ParseTerm(); // Parse the left side of the expression

            // Loop to handle comparison operations
            while (position < tokens.Count && tokens[position].Type == TokenType.Equal)
            {
                Token operatorToken = tokens[position++]; // Move to the next token (operator)

                // Parse the right side of the expression
                AstNode right = ParseTerm();

                // Create a BinaryOperationNode with the parsed left, operator, and right values
                left = new BinaryOperationNode(left, operatorToken.Value, right);
            }

            // Loop to handle binary operations (addition and subtraction)
            while (position < tokens.Count && (tokens[position].Type == TokenType.Plus || tokens[position].Type == TokenType.Minus))
            {
                Token operatorToken = tokens[position++]; // Move to the next token (operator)

                // Parse the right side of the expression
                AstNode right = ParseTerm();

                // Create a BinaryOperationNode with the parsed left, operator, and right values
                left = new BinaryOperationNode(left, operatorToken.Value, right);
            }

            return left;
        }

        private AstNode ParseTerm()
        {
            AstNode left = ParseFactor(); // Parse the left side of the term

            // Loop to handle multiplication and division operations
            while (position < tokens.Count && (tokens[position].Type == TokenType.Multiply || tokens[position].Type == TokenType.Divide))
            {
                Token operatorToken = tokens[position++]; // Move to the next token (operator)

                // Parse the right side of the term
                AstNode right = ParseFactor();

                // Create a BinaryOperationNode with the parsed left, operator, and right values
                left = new BinaryOperationNode(left, operatorToken.Value, right);
            }

            return left;
        }

        private AstNode ParseFactor()
        {
            Token currentToken = tokens[position++]; // Move to the next token

            if (currentToken.Type == TokenType.Integer)
            {
                return new IntLiteralNode(int.Parse(currentToken.Value)); // Construct IntLiteralNode
            }
            else if (currentToken.Type == TokenType.Identifier)
            {
                // Check if the identifier is a function call
                if (position < tokens.Count && tokens[position].Type == TokenType.LeftParen)
                {
                    // Move past the left parenthesis
                    position++;

                    // Parse the expression inside the function call
                    AstNode argument = ParseExpression();

                    // Check if the next token is a right parenthesis
                    if (position >= tokens.Count || tokens[position].Type != TokenType.RightParen)
                    {
                        throw new ParserException("Expected a right parenthesis ')' after function call");
                    }

                    // Move past the right parenthesis
                    position++;

                    // Return a function call node
                    return new FunctionCallNode(currentToken.Value, argument);
                }
                else
                {
                    return new VariableNode(currentToken.Value); // Construct VariableNode
                }
            }
            else if (currentToken.Type == TokenType.StringLiteral)
            {
                // Construct StringLiteralNode
                return new StringLiteralNode(currentToken.Value);
            }
            else if (currentToken.Type == TokenType.LeftParen)
            {
                // Parse the expression inside the parentheses
                AstNode expression = ParseExpression();

                // Check if the next token is a right parenthesis
                if (position >= tokens.Count || tokens[position].Type != TokenType.RightParen)
                {
                    throw new ParserException("Expected a right parenthesis ')' after '('");
                }

                // Move past the right parenthesis
                position++;

                return expression;
            }
            else
            {
                throw new ParserException($"Unexpected token '{currentToken.Value}'");
            }
        }

        private AstNode ParseValue()
        {
            Token currentToken = tokens[position++]; // Move to the next token

            if (currentToken.Type == TokenType.Integer)
            {
                return new IntLiteralNode(int.Parse(currentToken.Value)); // Construct IntLiteralNode
            }
            else if (currentToken.Type == TokenType.Identifier)
            {
                return new VariableNode(currentToken.Value); // Construct VariableNode
            }
            else
            {
                throw new ParserException($"Unexpected token '{currentToken.Value}'");
            }
        }

        private AstNode ParseIfStatement()
        {
            position++; // Move past the 'if' token

            AstNode condition = ParseExpression();

            if (position >= tokens.Count || tokens[position].Type != TokenType.Colon)
            {
                throw new ParserException("Expected a colon after 'if' condition");
            }

            position++; // Move past the colon

            var body = new List<AstNode>();

            while (position < tokens.Count && tokens[position].Type != TokenType.Identifier)
            {
                body.Add(ParseAssignmentOrStatement());
            }

            return new IfStatementNode(condition, body);
        }

        private AstNode ParsePrintStatement()
        {
            position++; // Move past the 'print' token

            AstNode expression = ParseExpression();

            return new PrintStatementNode(expression);
        }

        private bool IsOperator(TokenType type)
        {
            return type == TokenType.Plus || type == TokenType.Minus || type == TokenType.Multiply || type == TokenType.Divide;
        }
    }

    public class ParserException : Exception
    {
        public ParserException(string message) : base(message)
        {
        }
    }


    public abstract class AstNode
        {
        }

        public class AssignmentNode : AstNode
        {
            public string VariableName { get; }
            public AstNode Value { get; }

            public AssignmentNode(string variableName, AstNode value)
            {
                VariableName = variableName;
                Value = value;
            }
        }

        public class IntLiteralNode : AstNode
        {
            public int Value { get; }

            public IntLiteralNode(int value)
            {
                Value = value;
            }
        }

    public class VariableNode : AstNode
    {
        public string Name { get; }

        public VariableNode(string name)
        {
            Name = name;
        }
    }

    public class BinaryOperationNode : AstNode
    {
        public AstNode Left { get; }
        public string Operator { get; }
        public AstNode Right { get; }

        public BinaryOperationNode(AstNode left, string op, AstNode right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }
    }

    public class IfStatementNode : AstNode
    {
        public AstNode Condition { get; }
        public List<AstNode> Body { get; }

        public IfStatementNode(AstNode condition, List<AstNode> body)
        {
            Condition = condition;
            Body = body;
        }
    }

    public class PrintStatementNode : AstNode
    {
        public AstNode Expression { get; }

        public PrintStatementNode(AstNode expression)
        {
            Expression = expression;
        }
    }

    public class FunctionCallNode : AstNode
    {
        public string FunctionName { get; }
        public AstNode Argument { get; }

        public FunctionCallNode(string functionName, AstNode argument)
        {
            FunctionName = functionName;
            Argument = argument;
        }
    }

    public class StringLiteralNode : AstNode
    {
        public string Value { get; }

        public StringLiteralNode(string value)
        {
            // Remove surrounding quotes from the string value
            Value = value.Trim('"');
        }
    }
}

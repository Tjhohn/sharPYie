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
                ast.Add(ParseAssignmentOrExpression());
            }

            return ast;
        }

        private AstNode ParseAssignmentOrExpression()
        {
            Token currentToken = tokens[position];

            if (currentToken.Type == TokenType.Identifier)
            {
                return ParseAssignment();
            }
            else
            {
                return ParseExpression();
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
            AstNode left = ParseValue(); // Parse the left side of the expression

            // Loop to handle binary operations
            while (position < tokens.Count && IsOperator(tokens[position].Type))
            {
                Token operatorToken = tokens[position++]; // Move to the next token (operator)

                // Parse the right side of the expression
                AstNode right = ParseValue();

                // Create a BinaryOperationNode with the parsed left, operator, and right values
                left = new BinaryOperationNode(left, operatorToken.Value, right);
            }

            return left;
        }

        private bool IsOperator(TokenType type)
        {
            return type == TokenType.Plus || type == TokenType.Minus || type == TokenType.Multiply || type == TokenType.Divide;
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
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
                var ast_node = ParseAssignmentOrStatement();
                if (ast_node != null)
                {
                    ast.Add(ast_node);
                }
                
            }

            return ast;
        }

        private AstNode ParseAssignmentOrStatement()
        {
            Token currentToken = tokens[position];

            if (currentToken.Type == TokenType.Identifier)
            {
                if (currentToken.Value == "print")
                {
                    return ParsePrintStatement();
                }
                return ParseAssignment();
            }
            else if (currentToken.Type == TokenType.If)
            {
                return ParseIfStatement();
            }
            else if (currentToken.Type == TokenType.Def)
            {
                return ParseFunctionDefinition();
            }
            else if (currentToken.Type == TokenType.Return)
            {
                return ParseReturnStatement();
            }
            else if (currentToken.Type == TokenType.Newline)
            {
                position++;
                return null; //unsure what else to deal with hear?
            }
            else if (currentToken.Type == TokenType.EOF)
            {
                position++;
                return null; //unsure what else to do? for eof? I think I wil need to handle multiple eof...
            }
            else if (currentToken.Type == TokenType.Indent)
            {
                position++;
                return null; // I think indent should be only really for making proper "blocks"
            }
            else if (currentToken.Type == TokenType.Dedent)
            {
                position++;
                return null; // detent only for blocks? maybe need to handle assignmets and functions
            }
            else
            {
                throw new ParserException($"Unexpected token : type {currentToken.Type} -> '{currentToken.Value}'  - {nameof(ParseAssignmentOrStatement)}");
            }
        }

        private AstNode ParseReturnStatement()
        {
            position++; // Move past the 'return' token

            // Parse the value expression after 'return'
            AstNode value = ParseExpression();

            return new ReturnStatementNode(value);
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

        private AstNode ParseFunctionDefinition()
        {
            position++; // Move past the 'def' token

            Token functionNameToken = tokens[position++]; // Get the function name token
            string functionName = functionNameToken.Value;

            if (position >= tokens.Count || tokens[position].Type != TokenType.LeftParen)
            {
                throw new ParserException("Expected a left parenthesis '(' after function name");
            }

            position++; // Move past the left parenthesis

            var parameters = new List<string>();

            // Parse function parameters
            while (position < tokens.Count && tokens[position].Type != TokenType.RightParen)
            {
                Token parameterToken = tokens[position++]; // Get the parameter token

                if (parameterToken.Type != TokenType.Identifier)
                {
                    throw new ParserException($"Expected an identifier for function parameter, but found '{parameterToken.Value}'");
                }

                parameters.Add(parameterToken.Value);

                // Check for comma separator
                if (position < tokens.Count && tokens[position].Type == TokenType.Comma)
                {
                    position++; // Move past the comma
                }
            }

            if (position >= tokens.Count || tokens[position].Type != TokenType.RightParen)
            {
                throw new ParserException("Expected a right parenthesis ')' after function parameters");
            }

            position++; // Move past the right parenthesis

            // Check for colon token
            if (position >= tokens.Count || tokens[position].Type != TokenType.Colon)
            {
                throw new ParserException("Expected a colon ':' after function parameters");
            }

            position++; // Move past the colon

            // Parse function body
            var body = new List<AstNode>();

            while (position < tokens.Count && tokens[position].Type != TokenType.Identifier)
            {
                body.Add(ParseAssignmentOrStatement());
            }

            return new FunctionDefinitionNode(functionName, parameters, body);
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
                throw new ParserException($"Unexpected token '{currentToken.Value}' - {nameof(ParseFactor)}");
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
                throw new ParserException($"Unexpected token '{currentToken.Value}'  - {nameof(ParseValue)}");
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

        public string PrintAST(List<AstNode> ast)
        {
            var stringBuilder = new StringBuilder();
            PrintASTRecursive(ast, stringBuilder, 0);
            return stringBuilder.ToString();
        }

        private void PrintASTRecursive(List<AstNode> ast, StringBuilder stringBuilder, int depth)
        {
            foreach (var node in ast)
            {
                if (node is AssignmentNode assignmentNode)
                {
                    stringBuilder.AppendLine($"{GetIndent(depth)}Assignment: {assignmentNode.VariableName} = {GetValueAsString(assignmentNode.Value)}");
                }
                else if (node is IfStatementNode ifStatementNode)
                {
                    stringBuilder.AppendLine($"{GetIndent(depth)}If Statement:");
                    stringBuilder.AppendLine($"{GetIndent(depth + 1)}Condition: {GetValueAsString(ifStatementNode.Condition)}");
                    stringBuilder.AppendLine($"{GetIndent(depth + 1)}Body:");
                    PrintASTRecursive(ifStatementNode.Body, stringBuilder, depth + 2);
                }
                else if (node is PrintStatementNode printStatementNode)
                {
                    stringBuilder.AppendLine($"{GetIndent(depth)}Print Statement: {GetValueAsString(printStatementNode.Expression)}");
                }
                else if (node is FunctionDefinitionNode funcDefNode)
                {
                    stringBuilder.AppendLine($"{GetIndent(depth)}Function Definition: {funcDefNode.FunctionName}({string.Join(", ", funcDefNode.Parameters)})");
                    stringBuilder.AppendLine($"{GetIndent(depth)}Body:");
                    PrintASTRecursive(funcDefNode.Body, stringBuilder, depth + 1);
                }
                // Add other node types as needed
            }
        }

        private string GetValueAsString(AstNode node)
        {
            if (node is IntLiteralNode intNode)
            {
                return intNode.Value.ToString();
            }
            else if (node is VariableNode varNode)
            {
                return varNode.Name;
            }
            else if (node is StringLiteralNode stringLiteralNode)
            {
                return $"\"{stringLiteralNode.Value}\"";
            }
            else if (node is BinaryOperationNode binaryOpNode)
            {
                string left = GetValueAsString(binaryOpNode.Left);
                string right = GetValueAsString(binaryOpNode.Right);
                return $"({left} {binaryOpNode.Operator} {right})";
            }
            else
            {
                throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
            }
        }

        private string GetIndent(int depth)
        {
            return new string(' ', depth * 4); // Adjust indentation level as needed
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

    public class FunctionDefinitionNode : AstNode
    {
        public string FunctionName { get; }
        public List<string> Parameters { get; }
        public List<AstNode> Body { get; }

        public FunctionDefinitionNode(string functionName, List<string> parameters, List<AstNode> body)
        {
            FunctionName = functionName;
            Parameters = parameters;
            Body = body;
        }
    }

    public class ReturnStatementNode : AstNode
    {
        public AstNode ReturnValue { get; }

        public ReturnStatementNode(AstNode returnValue)
        {
            ReturnValue = returnValue;
        }
    }
}

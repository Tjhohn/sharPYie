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
                else if (position + 1 < tokens.Count && tokens[position + 1].Type == TokenType.LeftParen)
                {
                    return ParseExpression(); // Handles func(args)
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
                return null; // I think indent should be only really for making proper "blocks" maybe need to remove like dedent?
            }
            else if (currentToken.Type == TokenType.Dedent)
            {
                throw new ParserException("Unexpected DEDENT at top-level", tokens[position]);
                //position++;
                //return null; // detent only for blocks? maybe need to handle assignmets and functions
            }
            else if (currentToken.Type == TokenType.Comment)
            {
                position++;
                return null; //literally ignore comments
            }
            else
            {
                throw new ParserException($"Unexpected token : type {currentToken.Type} -> '{currentToken.Value}'  - {nameof(ParseAssignmentOrStatement)}", tokens[position]);
            }
        }

        private AstNode ParseReturnStatement()
        {
            position++; // Move past the 'return' token

            // If the next token starts a new line or is a DEDENT, it's a bare return
            if (position >= tokens.Count ||
                tokens[position].Type == TokenType.Newline ||
                tokens[position].Type == TokenType.Dedent)
            {
                return new ReturnStatementNode(null); // null = "bare" return
            }

            // Parse the value expression after 'return'
            AstNode value = ParseExpression();

            return new ReturnStatementNode(value);
        }

        private AstNode ParseAssignment()
        {
            int startPos = position;
            Token currentToken = tokens[position++]; // Move to the next token

            if (currentToken.Type != TokenType.Identifier)
            {
                throw new ParserException($"Expected an identifier, but found '{currentToken.Value}'", tokens[position]);
            }

            // Check if this identifier is followed by a dot (attribute access) → not an assignment target
            if (position < tokens.Count && tokens[position].Type == TokenType.Dot)
            {
                // Roll back and treat this as a general expression statement
                position = startPos;
                return ParseExpression();
            }


            string variableName = currentToken.Value;

            if (position >= tokens.Count || tokens[position].Type != TokenType.Assign)
            {
                throw new ParserException("Expected an assignment operator '='", tokens[position]);
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

            Token functionNameToken = tokens[position++];
            string functionName = functionNameToken.Value;

            if (position >= tokens.Count || tokens[position].Type != TokenType.LeftParen)
                throw new ParserException("Expected '(' after function name", tokens[position]);
            position++;

            var parameters = new List<string>();
            while (position < tokens.Count && tokens[position].Type != TokenType.RightParen)
            {
                Token parameterToken = tokens[position++];
                if (parameterToken.Type != TokenType.Identifier)
                    throw new ParserException($"Expected parameter name, got '{parameterToken.Value}'", tokens[position]);

                parameters.Add(parameterToken.Value);

                if (tokens[position].Type == TokenType.Comma)
                    position++;
            }

            if (position >= tokens.Count || tokens[position].Type != TokenType.RightParen)
                throw new ParserException("Expected ')' after function parameters", tokens[position]);
            position++;

            if (position >= tokens.Count || tokens[position].Type != TokenType.Colon)
                throw new ParserException("Expected ':' after function signature", tokens[position]);
            position++;

            if (position >= tokens.Count || tokens[position].Type != TokenType.Newline)
                throw new ParserException("Expected newline after ':'", tokens[position]);
            position++;

            if (position >= tokens.Count || tokens[position].Type != TokenType.Indent)
                throw new ParserException("Expected indentation for function body", tokens[position]);
            position++;

            var body = new List<AstNode>();
            while (position < tokens.Count && tokens[position].Type != TokenType.Dedent)
            {
                var bodyNode = ParseAssignmentOrStatement();
                if (bodyNode != null)
                    body.Add(bodyNode);
            }

            if (position < tokens.Count && tokens[position].Type == TokenType.Dedent)
            {
                position++;
            }
                

            return new FunctionDefinitionNode(functionName, parameters, body);
        }


        private AstNode ParseExpression()
        {
            return ParseOr();
        }

        private AstNode ParseOr()
        {
            AstNode left = ParseAnd();
            while (position < tokens.Count && tokens[position].Type == TokenType.Or)
            {
                string op = tokens[position++].Value;
                AstNode right = ParseAnd();
                left = new BinaryOperationNode(left, op, right);
            }
            return left;
        }

        private AstNode ParseAnd()
        {
            AstNode left = ParseNot();
            while (position < tokens.Count && tokens[position].Type == TokenType.And)
            {
                string op = tokens[position++].Value;
                AstNode right = ParseNot();
                left = new BinaryOperationNode(left, op, right);
            }
            return left;
        }

        private AstNode ParseNot()
        {
            if (position < tokens.Count && tokens[position].Type == TokenType.Not)
            {
                string op = tokens[position++].Value;
                AstNode right = ParseNot();
                return new UnaryOperationNode(op, right);
            }
            return ParseEquality();
        }

        private AstNode ParseEquality()
        {
            AstNode left = ParseComparison();
            while (position < tokens.Count && tokens[position].Type == TokenType.Equal)
            {
                string op = tokens[position++].Value;
                AstNode right = ParseComparison();
                left = new BinaryOperationNode(left, op, right);
            }
            return left;
        }

        private AstNode ParseComparison()
        {
            AstNode left = ParseTerm();
            while (position < tokens.Count &&
                   (tokens[position].Type == TokenType.Less ||
                    tokens[position].Type == TokenType.Greater ||
                    tokens[position].Type == TokenType.LessEqual ||
                    tokens[position].Type == TokenType.GreaterEqual ||
                    tokens[position].Type == TokenType.NotEqual))
            {
                string op = tokens[position++].Value;
                AstNode right = ParseTerm();
                left = new BinaryOperationNode(left, op, right);
            }
            return left;
        }

        private AstNode ParseTerm()
        {
            AstNode left = ParseFactor();

            while (position < tokens.Count &&
                   (tokens[position].Type == TokenType.Plus || tokens[position].Type == TokenType.Minus))
            {
                string op = tokens[position++].Value;
                AstNode right = ParseFactor();
                left = new BinaryOperationNode(left, op, right);
            }

            return left;
        }

        private AstNode ParseFactor()
        {
            AstNode left = ParseUnary();

            while (position < tokens.Count &&
                   (tokens[position].Type == TokenType.Multiply || tokens[position].Type == TokenType.Divide))
            {
                string op = tokens[position++].Value;
                AstNode right = ParseUnary();
                left = new BinaryOperationNode(left, op, right);
            }

            return left;
        }

        private AstNode ParseUnary()
        {
            if (position < tokens.Count && tokens[position].Type == TokenType.Minus)
            {
                string op = tokens[position++].Value;
                AstNode right = ParseUnary();
                return new UnaryOperationNode(op, right);
            }

            return ParseAtom();
        }

        private AstNode ParseAtom()
        {
            if (position >= tokens.Count)
                throw new ParserException("Unexpected end of input in expression");

            Token currentToken = tokens[position++];
            AstNode baseNode;

            if (currentToken.Type == TokenType.Integer)
            {
                baseNode = new IntLiteralNode(int.Parse(currentToken.Value));
            }
            else if (currentToken.Type == TokenType.StringLiteral)
            {
                baseNode = new StringLiteralNode(currentToken.Value);
            }
            else if (currentToken.Type == TokenType.None)
            {
                baseNode = NoneLiteralNode.Instance;
            }
            else if (currentToken.Type == TokenType.Identifier)
            {
                baseNode = new VariableNode(currentToken.Value);
                if (position < tokens.Count && tokens[position].Type == TokenType.LeftParen)
                {
                    position++; // skip '('
                    var args = new List<AstNode>();
                    if (tokens[position].Type != TokenType.RightParen)
                    {
                        while (true)
                        {
                            args.Add(ParseExpression());
                            if (tokens[position].Type == TokenType.Comma) position++;
                            else break;
                        }
                    }
                    if (tokens[position].Type != TokenType.RightParen)
                        throw new ParserException("Expected ')' after function call arguments", tokens[position]);
                    position++; // skip ')'
                    baseNode = new FunctionCallNode(currentToken.Value, args);
                }
            }
            else if (currentToken.Type == TokenType.LeftParen)
            {
                baseNode = ParseExpression();
                if (tokens[position].Type != TokenType.RightParen)
                    throw new ParserException("Expected ')' after expression", tokens[position]);
                position++;
            }
            else if (currentToken.Type == TokenType.LeftBracket)
            {
                var elements = new List<AstNode>();

                if (tokens[position].Type != TokenType.RightBracket)
                {
                    while (true)
                    {
                        elements.Add(ParseExpression());
                        if (tokens[position].Type == TokenType.Comma)
                        {
                            position++;
                            continue;
                        }
                        break;
                    }
                }

                if (tokens[position].Type != TokenType.RightBracket)
                    throw new ParserException("Expected ']' to close list literal", tokens[position]);
                position++;

                baseNode = new ListLiteralNode(elements);
            }
            else
            {
                throw new ParserException($"Unexpected token '{currentToken.Value}' in ParseAtom", currentToken);
            }

            // Handle chained function calls like list.append(...)
            while (position < tokens.Count && tokens[position].Type == TokenType.Dot)
            {
                position++; // skip dot
                if (tokens[position].Type != TokenType.Identifier)
                    throw new ParserException("Expected method name after '.'", tokens[position]);
                string methodName = tokens[position++].Value;

                if (tokens[position].Type != TokenType.LeftParen)
                    throw new ParserException("Expected '(' after method name", tokens[position]);
                position++;

                var args = new List<AstNode> { baseNode }; // Pass the baseNode as the first arg
                if (tokens[position].Type != TokenType.RightParen)
                {
                    while (true)
                    {
                        args.Add(ParseExpression());
                        if (tokens[position].Type == TokenType.Comma) position++;
                        else break;
                    }
                }

                if (tokens[position].Type != TokenType.RightParen)
                    throw new ParserException("Expected ')' after method arguments", tokens[position]);
                position++;

                baseNode = new FunctionCallNode(methodName, args);
            }

            return baseNode;
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
                throw new ParserException($"Unexpected token '{currentToken.Value}'  - {nameof(ParseValue)}", tokens[position]);
            }
        }

        private AstNode ParseIfStatement()
        {
            position++; // Move past the 'if' token

            AstNode condition = ParseExpression();

            if (position >= tokens.Count || tokens[position].Type != TokenType.Colon)
            {
                throw new ParserException("Expected a colon after 'if' condition", tokens[position]);
            }
            position++; // Move past the colon
            if (tokens[position].Type != TokenType.Newline)
                throw new ParserException("Expected newline after ':' in if", tokens[position]);
            position++; // past \n
            if (position >= tokens.Count || tokens[position].Type != TokenType.Indent)
                throw new ParserException("Expected INDENT to begin 'if' body", tokens[position]);
            position++; // pass indent

            var body = new List<AstNode>();

            while (position < tokens.Count && tokens[position].Type != TokenType.Dedent)
            {
                var node = ParseAssignmentOrStatement();
                if (node != null)
                {
                    body.Add(node);
                } 
            }

            if (tokens[position].Type == TokenType.Dedent)
                position++; // consume the dedent

            ///// else
            List<AstNode>? elseBody = null;

            if (position < tokens.Count && tokens[position].Type == TokenType.Else)
            {
                position++; // skip 'else'

                if (tokens[position].Type != TokenType.Colon)
                    throw new ParserException("Expected ':' after 'else'", tokens[position]);
                position++;

                if (tokens[position].Type != TokenType.Newline)
                    throw new ParserException("Expected newline after ':' in 'else'", tokens[position]);
                position++;

                if (tokens[position].Type != TokenType.Indent)
                    throw new ParserException("Expected INDENT to begin 'else' body", tokens[position]);
                position++;

                elseBody = new List<AstNode>();
                while (position < tokens.Count && tokens[position].Type != TokenType.Dedent)
                {
                    var stmt = ParseAssignmentOrStatement();
                    if (stmt != null)
                        elseBody.Add(stmt);
                }

                if (tokens[position].Type == TokenType.Dedent)
                    position++; // Skip else dedent
            }

            return new IfStatementNode(condition, body, elseBody);
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
                switch (node)
                {
                    case AssignmentNode assignmentNode:
                        stringBuilder.AppendLine($"{GetIndent(depth)}[Depth {depth}] Assignment: {assignmentNode.VariableName} =");
                        PrintExpression(assignmentNode.Value, stringBuilder, depth + 1);
                        break;

                    case PrintStatementNode printNode:
                        stringBuilder.AppendLine($"{GetIndent(depth)}[Depth {depth}] Print Statement:");
                        PrintExpression(printNode.Expression, stringBuilder, depth + 1);
                        break;

                    case IfStatementNode ifNode:
                        stringBuilder.AppendLine($"{GetIndent(depth)}[Depth {depth}] If Statement:");
                        stringBuilder.AppendLine($"{GetIndent(depth + 1)}Condition:");
                        PrintExpression(ifNode.Condition, stringBuilder, depth + 2);
                        stringBuilder.AppendLine($"{GetIndent(depth + 1)}Body:");
                        PrintASTRecursive(ifNode.Body, stringBuilder, depth + 2);
                        if (ifNode.ElseBody is not null && ifNode.ElseBody.Count > 0)
                        {
                            stringBuilder.AppendLine($"{GetIndent(depth + 1)}Else:");
                            PrintASTRecursive(ifNode.ElseBody, stringBuilder, depth + 2);
                        }
                        break;

                    case ReturnStatementNode returnNode:
                        stringBuilder.AppendLine($"{GetIndent(depth)}[Depth {depth}] Return Statement:");
                        PrintExpression(returnNode.ReturnValue, stringBuilder, depth + 1);
                        break;

                    case FunctionDefinitionNode funcNode:
                        stringBuilder.AppendLine($"{GetIndent(depth)}[Depth {depth}] Function Definition: {funcNode.FunctionName}({string.Join(", ", funcNode.Parameters)})");
                        stringBuilder.AppendLine($"{GetIndent(depth + 1)}Body:");
                        PrintASTRecursive(funcNode.Body, stringBuilder, depth + 2);
                        break;

                    default:
                        PrintExpression(node, stringBuilder, depth);
                        break;
                }
            }
        }

        private void PrintExpression(AstNode node, StringBuilder stringBuilder, int depth)
        {
            if (node == null)
            {
                stringBuilder.AppendLine($"{GetIndent(depth)}<null>");
                return;
            }

            switch (node)
            {
                case IntLiteralNode intNode:
                    stringBuilder.AppendLine($"{GetIndent(depth)}Int Literal: {intNode.Value}");
                    break;

                case StringLiteralNode stringNode:
                    stringBuilder.AppendLine($"{GetIndent(depth)}String Literal: \"{stringNode.Value}\"");
                    break;

                case VariableNode varNode:
                    stringBuilder.AppendLine($"{GetIndent(depth)}Variable: {varNode.Name}");
                    break;

                case UnaryOperationNode unary:
                    stringBuilder.AppendLine($"{GetIndent(depth)}Unary Operation: {unary.Operator}");
                    PrintExpression(unary.Operand, stringBuilder, depth + 1);
                    break;

                case BinaryOperationNode binOp:
                    stringBuilder.AppendLine($"{GetIndent(depth)}Binary Operation: {binOp.Operator}");
                    stringBuilder.AppendLine($"{GetIndent(depth + 1)}Left:");
                    PrintExpression(binOp.Left, stringBuilder, depth + 2);
                    stringBuilder.AppendLine($"{GetIndent(depth + 1)}Right:");
                    PrintExpression(binOp.Right, stringBuilder, depth + 2);
                    break;

                case FunctionCallNode callNode:
                    stringBuilder.AppendLine($"{GetIndent(depth)}Function Call: {callNode.FunctionName}");
                    stringBuilder.AppendLine($"{GetIndent(depth + 1)}Arguments:");
                    foreach (var arg in callNode.Arguments)
                    {
                        PrintExpression(arg, stringBuilder, depth + 2);
                    }
                    break;

                case ListLiteralNode listNode:
                    stringBuilder.AppendLine($"{GetIndent(depth)}List Literal:");
                    foreach (var element in listNode.Elements)
                    {
                        PrintExpression(element, stringBuilder, depth + 1);
                    }
                    break;

                // Optional: catch block in case expression accidentally passes a full statement node
                case ReturnStatementNode returnNode:
                    stringBuilder.AppendLine($"{GetIndent(depth)}Return Statement (expression context):");
                    PrintExpression(returnNode.ReturnValue, stringBuilder, depth + 1);
                    break;

                case IfStatementNode:
                case FunctionDefinitionNode:
                case AssignmentNode:
                    stringBuilder.AppendLine($"{GetIndent(depth)}<Statement node passed to expression printer: {node.GetType().Name}>");
                    break;

                default:
                    stringBuilder.AppendLine($"{GetIndent(depth)}[Unknown Expression Type: {node.GetType().Name}]");
                    break;
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
                return $"{stringLiteralNode.Value}";
            }
            else if (node is BinaryOperationNode binaryOpNode)
            {
                string left = GetValueAsString(binaryOpNode.Left);
                string right = GetValueAsString(binaryOpNode.Right);
                return $"({left} {binaryOpNode.Operator} {right})";
            }
            else if (node is FunctionCallNode callNode)
            {
                var argStrings = callNode.Arguments
                    .Select(arg => GetValueAsString(arg))
                    .ToList();
                string argsJoined = string.Join(", ", argStrings);
                return $"{callNode.FunctionName}({argsJoined})";
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
        public int Line { get; }
        public int Column { get; }

        public ParserException(string message, int line = -1, int column = -1)
            : base(FormatMessage(message, line, column))
        {
            Line = line;
            Column = column;
        }

        public ParserException(string message, Token token)
            : this(message, token?.Line ?? -1, token?.Column ?? -1)
        {
        }

        private static string FormatMessage(string message, int line, int column)
        {
            if (line >= 0 && column >= 0)
                return $"[Line {line}, Col {column}] {message}";
            return message;
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

    public class UnaryOperationNode : AstNode
    {
        public string Operator { get; }
        public AstNode Operand { get; }

        public UnaryOperationNode(string op, AstNode operand)
        {
            Operator = op;
            Operand = operand;
        }
    }

    public class IfStatementNode : AstNode
    {
        public AstNode Condition { get; }
        public List<AstNode> Body { get; }
        public List<AstNode>? ElseBody { get; }

        public IfStatementNode(AstNode condition, List<AstNode> body, List<AstNode>? elseBody = null)
        {
            Condition = condition;
            Body = body;
            ElseBody = elseBody;
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
        public List<AstNode> Arguments { get; }

        public FunctionCallNode(string functionName, List<AstNode> arguments)
        {
            FunctionName = functionName;
            Arguments = arguments;
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

    public class NoneLiteralNode : AstNode
    {
        public static readonly NoneLiteralNode Instance = new();

        private NoneLiteralNode() { }
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

    public class ListLiteralNode : AstNode
    {
        public List<AstNode> Elements { get; }

        public ListLiteralNode(List<AstNode> elements)
        {
            Elements = elements;
        }
    }
}

using sharPYieLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sharPYieTest
{
    [TestFixture]
    public class ParserTests
    {

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
                return stringLiteralNode.Value; // Return the string literal value
            }
            else if (node is BinaryOperationNode binaryOpNode)
            {
                string left = GetValueAsString(binaryOpNode.Left);
                string right = GetValueAsString(binaryOpNode.Right);
                return $"({left} {binaryOpNode.Operator} {right})";
            }
            else if (node is FunctionDefinitionNode funcDefNode)
            {
                string parameters = string.Join(", ", funcDefNode.Parameters);
                string body = string.Join("; ", funcDefNode.Body.Select(GetValueAsString));
                return $"def {funcDefNode.FunctionName}({parameters}): {body}";
            }
            else if (node is ReturnStatementNode returnNode)
            {
                string returnValue = GetValueAsString(returnNode.ReturnValue);
                return $"return {returnValue}";
            }
            else
            {
                throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
            }
        }

        [TestCase("x = 1", "x", "1")]
        [TestCase("a = 2", "a", "2")]
        [TestCase("a = 2\ny=57", "a", "2", "y", "57")]
        [TestCase("d = 5 / 1", "d", "(5 / 1)", "5", "1")]
        [TestCase("a = 2\nb = 3\nc = a + b", "a", "2", "b", "3", "c", "(a + b)")]
        [TestCase("a = 5\nb = 5\nc = 1\nd = a / b - c", "a", "5", "b", "5", "c", "1", "d", "((a / b) - c)")]
        [TestCase("x =1\nif x ==1:\n    print(\"x is 1\")\nx = 2\nif x ==1:\n    print(\"x is 1\")",
      "x", "1", "if", "(x == 1)", "print", "x is 1", "x", "2", "if", "(x == 1)", "print", "x is 1")]
        public void ParseAssignment_ValidInput_ReturnsCorrectResult(string input, params string[] expectedNodes)
        {
            var lexer = new Lexer(input);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.Parse();

            int index = 0;
            foreach (var node in ast)
            {
                if (node is AssignmentNode assignmentNode)
                {
                    Assert.AreEqual(expectedNodes[index++], assignmentNode.VariableName);
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(assignmentNode.Value));
                }
                else if (node is BinaryOperationNode binaryOpNode)
                {
                    Assert.AreEqual(expectedNodes[index++], binaryOpNode.Operator);
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(binaryOpNode.Left));
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(binaryOpNode.Right));
                }
                else if (node is IntLiteralNode intLiteralNode)
                {
                    Assert.AreEqual(expectedNodes[index++], intLiteralNode.Value.ToString());
                }
                else if (node is VariableNode variableNode)
                {
                    Assert.AreEqual(expectedNodes[index++], variableNode.Name);
                }
                else if (node is IfStatementNode ifStatementNode)
                {
                    Assert.AreEqual("if", expectedNodes[index++]); // Check if it's an if statement
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(ifStatementNode.Condition)); // Check the condition
                    foreach (var bodyNode in ifStatementNode.Body)
                    {
                        // You may need to implement additional checks here depending on your requirements
                        // For example, check for PrintStatementNode within the body
                    }
                }
                else if (node is PrintStatementNode printStatementNode)
                {
                    Assert.AreEqual("print", expectedNodes[index++]); // Check if it's a print statement
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(printStatementNode.Expression)); // Check the expression to be printed
                }
                else
                {
                    throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
                }
            }
        }

        // Theses where to hard to "do" and maintain
        // [TestCase("testinputs/firstfunc.py", "a", "1", "def", "addOne", "val" )]
        [TestCase("testinputs/stringliteraltoConsole.py", "temp", "this is a straight up string", "print", "temp")] // print string
        [TestCase("testinputs/ifAndPrint.py", "a", "56", "b", "9", "d", "4", "x", "1", "if", "(x == 1)", "print", "x is 1", "if", "(x == 1)", "x", "(((a / d) - b) + x)", "print", "x")] // file has an if and end result x should be 2
        [TestCase("testinputs/simpleAssignment.py", "a", "2", "b", "3", "c", "(a + b)")] // Assume the file contains "a = 2\nb = 3\nc = a + b"
        public void ParseAST_ValidInputFromFile_ReturnsCorrectResult(string relativePath, params string[] expectedNodes)
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
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            TestContext.WriteLine(parser.PrintAST(ast));


            int index = 0;
            foreach (var node in ast)
            {
                if (node is AssignmentNode assignmentNode)
                {
                    Assert.AreEqual(expectedNodes[index++], assignmentNode.VariableName);
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(assignmentNode.Value));
                }
                else if (node is BinaryOperationNode binaryOpNode)
                {
                    Assert.AreEqual(expectedNodes[index++], binaryOpNode.Operator);
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(binaryOpNode.Left));
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(binaryOpNode.Right));
                }
                else if (node is IntLiteralNode intLiteralNode)
                {
                    Assert.AreEqual(expectedNodes[index++], intLiteralNode.Value.ToString());
                }
                else if (node is VariableNode variableNode)
                {
                    Assert.AreEqual(expectedNodes[index++], variableNode.Name);
                }
                else if (node is StringLiteralNode stringLiteralNode)
                {
                    Assert.AreEqual(expectedNodes[index++], stringLiteralNode.Value);
                }
                else if (node is IfStatementNode ifStatementNode)
                {
                    Assert.AreEqual("if", expectedNodes[index++]); // Check if it's an if statement
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(ifStatementNode.Condition)); // Check the condition
                    foreach (var bodyNode in ifStatementNode.Body)
                    {
                        if (bodyNode is PrintStatementNode printNode)
                        {
                            Assert.AreEqual("print", expectedNodes[index++]); // Check if it's a print statement
                            Assert.AreEqual(expectedNodes[index++], GetValueAsString(printNode.Expression)); // Check the expression to be printed
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported node type within if statement body: {bodyNode.GetType().Name}");
                        }
                    }
                }
                else if (node is PrintStatementNode printStatementNode)
                {
                    Assert.AreEqual("print", expectedNodes[index++]); // Check if it's a print statement
                    Assert.AreEqual(expectedNodes[index++], GetValueAsString(printStatementNode.Expression)); // Check the expression to be printed
                }
                else if (node is FunctionDefinitionNode funcDefNode)
                {
                    Assert.AreEqual("def", expectedNodes[index++]); // Check if it's a function definition
                    Assert.AreEqual(expectedNodes[index++], funcDefNode.FunctionName); // Check the function name
                    CollectionAssert.AreEqual(expectedNodes.Skip(index).Take(funcDefNode.Parameters.Count).ToArray(), funcDefNode.Parameters); // Check the function parameters
                    index += funcDefNode.Parameters.Count;
                    foreach (var bodyNode in funcDefNode.Body)
                    {
                        if (bodyNode is ReturnStatementNode returnNode)
                        {
                            Assert.AreEqual("return", expectedNodes[index++]); // Check if it's a return statement
                            Assert.AreEqual(expectedNodes[index++], GetValueAsString(returnNode.ReturnValue)); // Check the return value
                        }
                        else if (bodyNode is PrintStatementNode printNode)
                        {
                            Assert.AreEqual("print", expectedNodes[index++]); // Check if it's a print statement
                            Assert.AreEqual(expectedNodes[index++], GetValueAsString(printNode.Expression)); // Check the expression to be printed
                        }
                        else
                        {
                            throw new NotSupportedException($"Unsupported node type within function body: {bodyNode.GetType().Name}");
                        }
                    }
                }
                else
                {
                    throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
                }
            }
        }


        [TestCase("testinputs/firstfunc.py", @"
[Depth 0] Assignment: a =
    Int Literal: 1
[Depth 0] Function Definition: addOne(val)
    Body:
        [Depth 2] Return Statement:
            Binary Operation: +
                Left:
                    Variable: val
                Right:
                    Int Literal: 1
[Depth 0] Assignment: a =
    Function Call: addOne
        Arguments:
            Variable: a
[Depth 0] Print Statement:
    Variable: a
[Depth 0] Function Definition: multipleParams(a, b, c, d)
    Body:
        [Depth 2] Return Statement:
            Binary Operation: +
                Left:
                    Binary Operation: +
                        Left:
                            Binary Operation: +
                                Left:
                                    Variable: a
                                Right:
                                    Variable: b
                        Right:
                            Variable: c
                Right:
                    Variable: d
[Depth 0] Print Statement:
    Variable: a
")]
        [TestCase("testinputs/simpleAssignment.py", @"
[Depth 0] Assignment: a =
    Int Literal: 2
[Depth 0] Assignment: b =
    Int Literal: 3
[Depth 0] Assignment: c =
    Binary Operation: +
        Left:
            Variable: a
        Right:
            Variable: b
")]
        public void ParseAST_ValidInputFromFile_MatchesExpectedAstString(string relativePath, string expectedAst)
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
            var parser = new Parser(tokens);
            var ast = parser.Parse();

            string actualAst = parser.PrintAST(ast).Trim();
            TestContext.WriteLine(actualAst);

            Assert.AreEqual(expectedAst.Trim(), actualAst, "AST did not match expected structure.");
        }

    }
}

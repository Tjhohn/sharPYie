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
        [TestCase("x = 1", "x", "1")]
        [TestCase("a = 2", "a", "2")]
        [TestCase("a = 2\ny=57", "a", "2", "y", "57")]
        [TestCase("d = 5 / 1", "d", "(5 / 1)", "5", "1")]
        [TestCase("a = 2\nb = 3\nc = a + b", "a", "2", "b", "3", "c", "(a + b)")]    
        [TestCase("a = 5\nb = 5\nc = 1\nd = a / b - c", "a", "5", "b", "5","c","1", "d","((a / b) - c)")]
        public void ParseAssignment_ValidInput_ReturnsCorrectResult(string input, params string[] expectedNodes)
        {
            var lexer = new Lexer(input);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.Parse();

            //Assert.AreEqual(expectedNodes.Length / 2, ast.Count); // Check if the number of assignments matches

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
                else
                {
                    throw new NotSupportedException($"Unsupported node type: {node.GetType().Name}");
                }
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
    }
}

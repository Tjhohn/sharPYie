using sharPYieLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sharPYieTest
{
    [TestFixture]
    public class InterpreterTests
    {
        [TestCase("x = 1", 1)]
        [TestCase("a = 2", 2)]
        [TestCase("a = 2\nb = 3\nc = a + b", 5)]
        [TestCase("d = 5 / 1", 5)]
        [TestCase("a = 5\nb = 5\nc = 1\nd = a / b - c", 0)]
        public void InterpretAST_ValidInput_ReturnsCorrectResult(string input, int expectedResult)
        {
            var lexer = new Lexer(input);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            var ast = parser.Parse();
            var interpreter = new Interpreter();

            interpreter.Interpret(ast);

            // Now, we need to retrieve the value of the last assignment node (if any) and assert it
            if (ast.Count > 0 && ast[ast.Count - 1] is AssignmentNode lastAssignment)
            {
                Assert.AreEqual(expectedResult, interpreter.GetVariableValue(lastAssignment.VariableName));
            }
            else
            {
                // If there are no assignment nodes, then we can't check the result directly
                Assert.Fail("No assignment node found in the AST.");
            }
        }

        [TestCase("testinputs/simpleAssignment.py", 5)] // Assume the file contains "a = 2\nb = 3\nc = a + b"
        public void InterpretAST_ValidInputFromFile_ReturnsCorrectResult(string relativePath, int expectedResult)
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
            var interpreter = new Interpreter();

            interpreter.Interpret(ast);

            // Get the value of the last assignment and verify against expected result
            var lastAssignment = (AssignmentNode)ast[ast.Count - 1];
            int result = interpreter.GetVariableValue(lastAssignment.VariableName);
            Assert.AreEqual(expectedResult, result);
        }
    }
}

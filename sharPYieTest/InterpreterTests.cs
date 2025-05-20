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

        [TestCase("testinputs/stringliteraltoConsole.py", "this is a straight up string", "this is a straight up string")] // print string
        [TestCase("testinputs/ifAndPrint.py", 6, "x is 1\n6")] // file has an if and end result x should be 2
        [TestCase("testinputs/simpleAssignment.py", 5, "")] // Assume the file contains "a = 2\nb = 3\nc = a + b"
        [TestCase("testinputs/firstfunc.py", 2, "2\n2")] // Assume the file contains "a = 2\nb = 3\nc = a + b"
        [TestCase("testinputs/multipleParams.py", 470, "470")]
        [TestCase("testinputs/concatStrings.py", "weird", "qwertyweird")]
        [TestCase("testinputs/basicScope.py", 8, "10\n8")]
        public void InterpretAST_ValidInputFromFile_ReturnsCorrectResult(string relativePath, object expectedResult, string expectedString)
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


            using (StringWriter sw = new StringWriter())
            {
                Console.SetOut(sw);

                interpreter.Interpret(ast);

                // Get the printed output, remove /r to hopefully be consistent across os's but not really a focus
                string printedOutput = sw.ToString().Replace("\r\n", "\n").Replace("\n\r", "\n").Trim();

                // Verify the printed output matches the expected result
                Assert.AreEqual(expectedString, printedOutput);
            }

            // Get the value of the last assignment and verify against expected result if exists
            var assignmentNodeExists = ast.Any(node => node is AssignmentNode);

            if (assignmentNodeExists)
            {
                // Get the value of the last assignment and verify against expected result
                var lastAssignment = (AssignmentNode)ast.Last(node => node is AssignmentNode);
                var result = interpreter.GetVariableValue(lastAssignment.VariableName);
                Assert.AreEqual(expectedResult, result);
            }
        }
    }
}

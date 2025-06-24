using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using static sharPYieLib.Interpreter;
namespace sharPYieLib
{
    public class Interpreter
    {
        private readonly Dictionary<string, PyBaseObject> globals;
        private readonly Dictionary<string, PyBaseObject> locals;
        private readonly Dictionary<string, FunctionDefinitionNode> functions = new();

        private static readonly Dictionary<string, Func<List<PyBaseObject>, PyBaseObject>> pyBuiltins = new()
        {
            ["len"] = args => args.Count == 1 ? args[0].Len() : throw new Exception("len() takes 1 argument"),
            ["int"] = args => args.Count == 1 && args[0] is PyStr s ? new PyInt(int.Parse(s.Value)) : throw new Exception("int() invalid argument"),
            ["str"] = args => args.Count == 1 ? new PyStr(args[0].Str()) : throw new Exception("str() takes 1 argument"),
            ["type"] = args => args.Count == 1 ? new PyStr(args[0].TypeName) : throw new Exception("type() takes 1 argument"),
            ["object"] = args => new PyObject(),
            ["range"] = args =>
            {
                int start = 0, stop, step = 1;

                if (args.Count == 1)
                {
                    if (args[0] is not PyInt s)
                        throw new Exception("range() argument must be int");
                    stop = s.Value;
                }
                else if (args.Count == 2)
                {
                    if (args[0] is not PyInt s1 || args[1] is not PyInt s2)
                        throw new Exception("range() arguments must be int");
                    start = s1.Value;
                    stop = s2.Value;
                }
                else if (args.Count == 3)
                {
                    if (args[0] is not PyInt s1 || args[1] is not PyInt s2 || args[2] is not PyInt s3)
                        throw new Exception("range() arguments must be int");
                    start = s1.Value;
                    stop = s2.Value;
                    step = s3.Value;
                }
                else
                {
                    throw new Exception("range() expects 1 to 3 arguments");
                }

                if (step == 0)
                    throw new Exception("range() step argument must not be zero");

                var items = new List<PyBaseObject>();
                for (int i = start; step > 0 ? i < stop : i > stop; i += step)
                    items.Add(new PyInt(i));

                return new PyList(items);
            }
        };

        //private static readonly Dictionary<string, Func<List<object>, object>> builtins = new()
        //{
        //    ["len"] = args => args.Count == 1
        //        ? args[0] is string s ? s.Length
        //        : args[0] is IEnumerable<object> e ? e.Count()
        //        : throw new Exception("len() argument must be iterable")
        //        : throw new Exception("len() takes exactly one argument"),

        //    ["int"] = args => args.Count == 1 ? int.Parse(args[0].ToString()!) : throw new Exception("int() takes exactly one argument"),

        //    ["str"] = args => args.Count == 1 ? args[0].ToString()! : throw new Exception("str() takes exactly one argument"),

        //    ["type"] = args => args.Count == 1 ? args[0].GetType().Name.ToLower() : throw new Exception("type() takes exactly one argument"),

        //    ["range"] = args =>
        //    {
        //        int start = 0, stop = 0, step = 1;

        //        if (args.Count == 1)
        //            stop = Convert.ToInt32(args[0]);
        //        else if (args.Count == 2)
        //        {
        //            start = Convert.ToInt32(args[0]);
        //            stop = Convert.ToInt32(args[1]);
        //        }
        //        else if (args.Count == 3)
        //        {
        //            start = Convert.ToInt32(args[0]);
        //            stop = Convert.ToInt32(args[1]);
        //            step = Convert.ToInt32(args[2]);
        //        }
        //        else throw new Exception("range() expects 1 to 3 arguments");

        //        var result = new List<object>();
        //        for (int i = start; step > 0 ? i < stop : i > stop; i += step)
        //            result.Add(i);
        //        return result;
        //    }
        //};

        //private static readonly Dictionary<string, Func<List<object>, object>> listBuiltins = new()
        //{
        //    ["append"] = args =>
        //    {
        //        if (args.Count != 2 || args[0] is not List<object> list)
        //            throw new Exception("append expects a list and a value");
        //        list.Add(args[1]);
        //        return null!;
        //    },

        //    ["pop"] = args =>
        //    {
        //        if (args.Count != 1 || args[0] is not List<object> list)
        //            throw new Exception("pop expects a list");
        //        if (list.Count == 0)
        //            throw new Exception("pop from empty list");
        //        var last = list[^1];
        //        list.RemoveAt(list.Count - 1);
        //        return last;
        //    }
        //    // Add more as needed: insert, remove, clear, index...
        //};

        public Interpreter(Dictionary<string, PyBaseObject>? globals = null, Dictionary<string, PyBaseObject>? locals = null, Dictionary<string, FunctionDefinitionNode>? functions = null)
        {
            this.globals = globals ?? new Dictionary<string, PyBaseObject>();
            this.locals = locals ?? new Dictionary<string, PyBaseObject>();
            this.functions = functions ?? new Dictionary<string, FunctionDefinitionNode>();
        }

        public StepResult Interpret(List<AstNode> ast)
        {
            foreach (var node in ast)
            {
                var result = Walk(node);
                if (result.HasReturned) return result;
            }
            return StepResult.None();
        }

        public PyBaseObject GetVariableValue(string name)
        {
            if (locals.TryGetValue(name, out var val)) return val;
            if (globals.TryGetValue(name, out val)) return val;
            throw new Exception($"Variable '{name}' not found.");
        }

        private bool IsTruthy(PyBaseObject? value)
        {
            return value switch
            {
                null => false,
                PyNone => false,
                PyBool b => b.Value,
                PyInt i => i.Value != 0,
                PyStr s => s.Value.Length > 0,
                PyList l => l.Items.Count > 0,
                _ => true
            };
        }

        private StepResult Walk(AstNode node)
        {
            if (node is AssignmentNode assignmentNode)
            {
                // Evaluate the value assigned to the variable
                PyBaseObject value = EvaluateExpression(assignmentNode.Value);
                // update scope
                if (locals.ContainsKey(assignmentNode.VariableName))
                    locals[assignmentNode.VariableName] = value;
                else
                    globals[assignmentNode.VariableName] = value;
                return StepResult.None();
            }
            else if (node is IfStatementNode ifStatementNode)
            {
                // Evaluate the condition
                var conditionResult = EvaluateExpression(ifStatementNode.Condition);
                if (IsTruthy(conditionResult))
                {
                    // Execute the body of the if statement
                    foreach (var statement in ifStatementNode.Body)
                    {
                        var result = Walk(statement);
                        if (result.HasReturned)
                        {
                            return result;
                        }
                    }
                }
                else if (ifStatementNode.ElseBody != null)
                {
                    foreach (var stmt in ifStatementNode.ElseBody)
                    {
                        var result = Walk(stmt);
                        if (result.HasReturned)
                            return result;
                    }
                }
                return StepResult.None();
            }
            else if (node is PrintStatementNode printStatementNode)
            {
                PyBaseObject expressionValue = EvaluateExpression(printStatementNode.Expression);
                // Print the value
                if (expressionValue is PyList pyList)
                {
                    Console.WriteLine(pyList.Repr());
                }
                else
                {
                    Console.WriteLine(expressionValue.Str());
                }
                return StepResult.None();
            }
            else if (node is FunctionDefinitionNode funcNode)
            {
                functions[funcNode.FunctionName] = funcNode;
                return StepResult.None();
            }
            else if (node is ReturnStatementNode returnNode)
            {
                //handle if a function returns null, may need to figure out something as pythons "None" is not "exactly" null
                PyBaseObject? value = returnNode.ReturnValue != null ? EvaluateExpression(returnNode.ReturnValue) : null;
                return StepResult.Return(value);
            }
            else if (node is FunctionCallNode callNode)
            {
                EvaluateExpression(callNode); // Execute for side effects, discard result
                return StepResult.None();
            }
            else
            {
                throw new ArgumentException($"Unsupported node type: {node.GetType().Name}");
            }
        }

        private PyBaseObject EvaluateExpression(AstNode node)
        {
            // Handle evaluation of expressions and return their values
            if (node is IntLiteralNode intLiteralNode)
            {
                return new PyInt(intLiteralNode.Value);
            }
            else if (node is StringLiteralNode stringLiteralNode)
            {

                return new PyStr(stringLiteralNode.Value);
            }
            else if (node is BinaryOperationNode binaryOperationNode)
            {
                PyBaseObject leftValue = EvaluateExpression(binaryOperationNode.Left);
                PyBaseObject rightValue = EvaluateExpression(binaryOperationNode.Right);
                return EvaluateBinaryOperation(leftValue, binaryOperationNode.Operator, rightValue);
            }
            else if (node is UnaryOperationNode unaryOpNode)
            {
                var operand = EvaluateExpression(unaryOpNode.Operand);
                return EvaluateUnaryOperation(unaryOpNode.Operator, operand);
            }
            else if (node is VariableNode variableNode)
            {
                return GetVariableValue(variableNode.Name);
            }
            else if (node is ListLiteralNode listNode)
            {
                return new PyList(listNode.Elements.Select(EvaluateExpression).ToList());
            }
            else if (node is FunctionCallNode callNode)
            {
                var args = callNode.Arguments.Select(EvaluateExpression).ToList();
                if (pyBuiltins.TryGetValue(callNode.FunctionName, out var builtin))
                {
                    return builtin(args);
                }

                // Check for list methods
                if (args.Count > 0 && args[0] is PyBaseObject obj)
                {
                    try
                    {
                        var method = obj.GetAttr(callNode.FunctionName);
                        return method.Call(args.Skip(1).ToList());  // method is expected to be callable
                    }
                    catch (Exception)
                    {
                        // Fall through to function table below
                    }
                }

                if (!functions.TryGetValue(callNode.FunctionName, out var function))
                    throw new ArgumentException($"Function '{callNode.FunctionName}' is not defined.");

                if (function.Parameters.Count != callNode.Arguments.Count)
                    throw new ArgumentException($"Function '{callNode.FunctionName}' expects {function.Parameters.Count} arguments but got {callNode.Arguments.Count}.");


                var localScope = new Dictionary<string, PyBaseObject>();
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    var argValue = EvaluateExpression(callNode.Arguments[i]);
                    localScope[function.Parameters[i]] = argValue;
                }

                var functionInterpreter = new Interpreter(globals, localScope, functions);
                var result = functionInterpreter.Interpret(function.Body);
                return result.HasReturned ? result.ReturnValue : null;
            }
            else if (node is NoneLiteralNode)
            {
                return PyNone.Instance;  // See step below
            }
            else if (node is IndexAccessNode indexNode)
            {
                var target = EvaluateExpression(indexNode.Target);
                var index = EvaluateExpression(indexNode.Index);

                if (target is PyList pyList && index is PyInt i)
                {
                    if (i.Value < 0 || i.Value >= pyList.Items.Count)
                        throw new IndexOutOfRangeException($"Index {i.Value} out of bounds for list of size {pyList.Items.Count}");
                    return pyList.Items[i.Value];
                }
                else
                {
                    throw new ArgumentException("Attempted index access on non-list object");
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported expression type: {node.GetType().Name}");
            }
        }

        private PyBaseObject EvaluateBinaryOperation(PyBaseObject left, string op, PyBaseObject right)
        {
            return op switch
            {
                "+" => left.Add(right),
                "-" => left.Subtract(right),
                "*" => left.Multiply(right),
                "/" => left.Divide(right),
                "==" => left.Eq(right),
                "!=" => new PyBool(!((PyBool)left.Eq(right)).Value),
                "<" => left.LessThan(right),
                "<=" => left.LessThanOrEqual(right),
                ">" => left.GreaterThan(right),
                ">=" => left.GreaterThanOrEqual(right),
                "and" => new PyBool(IsTruthy(left) && IsTruthy(right)),
                "or" => new PyBool(IsTruthy(left) || IsTruthy(right)),
                _ => throw new ArgumentException($"Unsupported binary operator: {op}")
            };
        }

        private PyBaseObject EvaluateUnaryOperation(string op, PyBaseObject operand)
        {
            if (op == "-")
            {
                if (operand is PyInt i) return new PyInt(-i.Value);
                if (operand is PyFloat f) return new PyFloat(-f.Value);
            }
            else if (op == "not")
            {
                return new PyBool(!IsTruthy(operand));
            }

            throw new Exception($"Unsupported unary operator {op} for type {operand.TypeName}");
        }

        public class StepResult
        {
            public bool HasReturned { get; }
            public PyBaseObject? ReturnValue { get; }

            private StepResult(bool hasReturned, PyBaseObject? value)
            {
                HasReturned = hasReturned;
                ReturnValue = value;
            }

            public static StepResult None() => new StepResult(false, PyNone.Instance);
            public static StepResult Return(PyBaseObject value) => new StepResult(true, value);
        }
    }
}
using System;
using System.Collections.Generic;
using static sharPYieLib.Interpreter;

namespace sharPYieLib
{
    public class Interpreter
    {
        private readonly Dictionary<string, object> globals;
        private readonly Dictionary<string, object> locals;
        private readonly Dictionary<string, FunctionDefinitionNode> functions = new();

        public Interpreter(Dictionary<string, object>? globals = null, Dictionary<string, object>? locals = null, Dictionary<string, FunctionDefinitionNode>? functions = null)
        {
            this.globals = globals ?? new Dictionary<string, object>();
            this.locals = locals ?? new Dictionary<string, object>();
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

        public object GetVariableValue(string variableName)
        {
            if (locals.TryGetValue(variableName, out var value)) return value;
            if (globals.TryGetValue(variableName, out value)) return value;
            throw new KeyNotFoundException($"Variable '{variableName}' not found in scope.");
        }

        private StepResult Walk(AstNode node)
        {
            if (node is AssignmentNode assignmentNode)
            {
                // Evaluate the value assigned to the variable
                object value = EvaluateExpression(assignmentNode.Value);
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
                int conditionValue = (int)EvaluateExpression(ifStatementNode.Condition);
                if (conditionValue == 1)
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
                return StepResult.None();
            }
            else if (node is PrintStatementNode printStatementNode)
            {
                object expressionValue = EvaluateExpression(printStatementNode.Expression);
                // Print the value
                Console.WriteLine(expressionValue);
                return StepResult.None();
            }
            else if (node is FunctionDefinitionNode funcNode)
            {
                functions[funcNode.FunctionName] = funcNode;
                return StepResult.None();
            }
            else if (node is ReturnStatementNode returnNode)
            {
                object value = EvaluateExpression(returnNode.ReturnValue);
                return StepResult.Return(value);
            }
            else
            {
                throw new ArgumentException($"Unsupported node type: {node.GetType().Name}");
            }
        }

        private object EvaluateExpression(AstNode node)
        {
            // Handle evaluation of expressions and return their values
            if (node is IntLiteralNode intLiteralNode)
            {
                return intLiteralNode.Value;
            }
            else if (node is StringLiteralNode stringLiteralNode)
            {

                return stringLiteralNode.Value;
            }
            else if (node is BinaryOperationNode binaryOperationNode)
            {
                object leftValue = EvaluateExpression(binaryOperationNode.Left);
                object rightValue = EvaluateExpression(binaryOperationNode.Right);
                return EvaluateBinaryOperation(leftValue, binaryOperationNode.Operator, rightValue);
            }
            else if (node is VariableNode variableNode)
            {
                return GetVariableValue(variableNode.Name);
            }
            else if (node is FunctionCallNode callNode)
            {
                if (!functions.TryGetValue(callNode.FunctionName, out var function))
                    throw new ArgumentException($"Function '{callNode.FunctionName}' is not defined.");

                if (function.Parameters.Count != callNode.Arguments.Count)
                    throw new ArgumentException($"Function '{callNode.FunctionName}' expects {function.Parameters.Count} arguments but got {callNode.Arguments.Count}.");


                var localScope = new Dictionary<string, object>();
                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    var argValue = EvaluateExpression(callNode.Arguments[i]);
                    localScope[function.Parameters[i]] = argValue;
                }

                var functionInterpreter = new Interpreter(globals, localScope, functions);
                var result = functionInterpreter.Interpret(function.Body);
                return result.HasReturned ? result.ReturnValue : null;
            }
            else
            {
                throw new ArgumentException($"Unsupported expression type: {node.GetType().Name}");
            }
        }

        private object EvaluateBinaryOperation(object left, string op, object right)
        {
            if (left is int lInt && right is int rInt)
            {
                return op switch
                {
                    "+" => lInt + rInt,
                    "-" => lInt - rInt,
                    "*" => lInt * rInt,
                    "/" => rInt == 0 ? throw new DivideByZeroException() : lInt / rInt,
                    "==" => lInt == rInt ? 1 : 0,
                    _ => throw new ArgumentException($"Unsupported operator for integers: {op}")
                };
            }
            else if (left is string lStr && right is string rStr)
            {
                if (op == "+") return lStr + rStr;
                if (op == "==") return lStr == rStr ? 1 : 0;
                throw new ArgumentException($"Unsupported string operator: {op}");
            }
            else
            {
                throw new ArgumentException($"Type mismatch or unsupported operand types for '{op}': {left.GetType().Name}, {right.GetType().Name}");
            }
        }

        public class StepResult
        {
            public bool HasReturned { get; }
            public object? ReturnValue { get; }

            private StepResult(bool hasReturned, object? value)
            {
                HasReturned = hasReturned;
                ReturnValue = value;
            }

            public static StepResult None() => new StepResult(false, null);
            public static StepResult Return(object value) => new StepResult(true, value);
        }
    }
}
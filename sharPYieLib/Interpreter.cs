using System;
using System.Collections.Generic;

namespace sharPYieLib
{
    public class Interpreter
    {
        private readonly Dictionary<string, object> environment;

        public Interpreter()
        {
            environment = new Dictionary<string, object>();
        }

        public void Interpret(List<AstNode> ast)
        {
            foreach (var node in ast)
            {
                Walk(node);
            }
        }

        public object GetVariableValue(string variableName)
        {
            if (environment.TryGetValue(variableName, out object value))
            {
                return value;
            }
            else
            {
                throw new KeyNotFoundException($"Variable '{variableName}' not found in the environment.");
            }
        }

        private void Walk(AstNode node)
        {
            if (node is AssignmentNode assignmentNode)
            {
                // Evaluate the value assigned to the variable
                object value = EvaluateExpression(assignmentNode.Value);
                // Update the environment with the variable assignment
                environment[assignmentNode.VariableName] = value;
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
                        Walk(statement);
                    }
                }
            }
            else if (node is PrintStatementNode printStatementNode)
            {
                // Evaluate the expression to print
                object expressionValue = EvaluateExpression(printStatementNode.Expression);
                // Print the value
                Console.WriteLine(expressionValue);
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
                int leftValue = (int)EvaluateExpression(binaryOperationNode.Left);
                int rightValue = (int)EvaluateExpression(binaryOperationNode.Right);
                return EvaluateBinaryOperation(leftValue, binaryOperationNode.Operator, rightValue);
            }
            else if (node is VariableNode variableNode)
            {
                // Look up the value of the variable in the environment
                if (environment.TryGetValue(variableNode.Name, out object value))
                {
                    return value;
                }
                else
                {
                    throw new ArgumentException($"Variable '{variableNode.Name}' is not defined.");
                }
            }
            else
            {
                throw new ArgumentException($"Unsupported expression type: {node.GetType().Name}");
            }
        }

        private int EvaluateBinaryOperation(int left, string op, int right)
        {
            switch (op)
            {
                case "+":
                    return left + right;
                case "-":
                    return left - right;
                case "*":
                    return left * right;
                case "/":
                    if (right == 0)
                        throw new DivideByZeroException();
                    return left / right;
                case "==":
                    return left == right ? 1 : 0;
                default:
                    throw new ArgumentException($"Unsupported operator: {op}");
            }
        }
    }
}
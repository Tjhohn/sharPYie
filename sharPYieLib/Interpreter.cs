using System;
using System.Collections.Generic;

namespace sharPYieLib
{
    public class Interpreter
    {
        private readonly Dictionary<string, int> environment;

        public Interpreter()
        {
            environment = new Dictionary<string, int>();
        }

        public void Interpret(List<AstNode> ast)
        {
            foreach (var node in ast)
            {
                Walk(node);
            }
        }

        public int GetVariableValue(string variableName)
        {
            if (environment.TryGetValue(variableName, out int value))
            {
                return value;
            }
            else
            {
                throw new KeyNotFoundException($"Variable '{variableName}' not found in the environment.");
            }
        }

        private int Walk(AstNode node)
        {
            if (node is IntLiteralNode intLiteralNode)
            {
                return intLiteralNode.Value;
            }
            else if (node is BinaryOperationNode binaryOperationNode)
            {
                int leftValue = Walk(binaryOperationNode.Left);
                int rightValue = Walk(binaryOperationNode.Right);
                return EvaluateBinaryOperation(leftValue, binaryOperationNode.Operator, rightValue);
            }
            else if (node is AssignmentNode assignmentNode)
            {
                // Evaluate the value assigned to the variable
                int value = Walk(assignmentNode.Value);
                // Update the environment with the variable assignment
                environment[assignmentNode.VariableName] = value;
                // Return the assigned value
                return value;
            }
            else if (node is VariableNode variableNode)
            {
                // Look up the value of the variable in the environment
                if (environment.TryGetValue(variableNode.Name, out int value))
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
                default:
                    throw new ArgumentException("Unsupported operator");
            }
        }
    }
}
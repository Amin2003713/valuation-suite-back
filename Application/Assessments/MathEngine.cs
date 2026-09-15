using System.Linq.Expressions;
using Domain.Common;
using Application.Assessments;

namespace Application.Assessments;

public class MathEngine : IMathEngine
{
    public double Evaluate(string expression, Dictionary<string, double> variables)
    {
        var sanitized = SanitizeExpression(expression);
        var postfix = ToPostfix(sanitized);
        return EvaluatePostfix(postfix, variables);
    }

    public bool ValidateExpression(string expression, out string? error)
    {
        try
        {
            var sanitized = SanitizeExpression(expression);
            var postfix = ToPostfix(sanitized);
            EvaluatePostfix(postfix, new Dictionary<string, double>());
            error = null;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string SanitizeExpression(string expr)
    {
        // Remove whitespace and validate
        var clean = expr.Replace(" ", "").Replace("\t", "").Replace("\n", "");
        foreach (var ch in clean)
        {
            if (!"0123456789+-*/().^abcdefghijklmnopqrstuvwxyz_".Contains(ch))
                throw new InvalidOperationException($"Invalid character: {ch}");
        }
        return clean;
    }

    private static List<string> ToPostfix(string expression)
    {
        var output = new List<string>();
        var operators = new Stack<string>();
        var i = 0;

        while (i < expression.Length)
        {
            var ch = expression[i];

            if (char.IsLetter(ch) || ch == '_')
            {
                var start = i;
                while (i < expression.Length && (char.IsLetterOrDigit(expression[i]) || expression[i] == '_'))
                    i++;
                output.Add(expression[start..i]);
                continue;
            }

            if (char.IsDigit(ch))
            {
                var start = i;
                while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                    i++;
                output.Add(expression[start..i]);
                continue;
            }

            if (ch == '(')
            {
                operators.Push(ch.ToString());
                i++;
                continue;
            }

            if (ch == ')')
            {
                while (operators.Count > 0 && operators.Peek() != "(")
                    output.Add(operators.Pop());
                operators.Pop(); // Remove '('
                i++;
                continue;
            }

            // Operator
            var prec = Precedence(ch);
            while (operators.Count > 0 && Precedence(operators.Peek()[0]) >= prec)
                output.Add(operators.Pop());
            operators.Push(ch.ToString());
            i++;
        }

        while (operators.Count > 0)
            output.Add(operators.Pop());

        return output;
    }

    private static int Precedence(char op) => op switch
    {
        '+' or '-' => 1,
        '*' or '/' => 2,
        '^' => 3,
        _ => 0
    };

    private static double EvaluatePostfix(List<string> postfix, Dictionary<string, double> variables)
    {
        var stack = new Stack<double>();
        foreach (var token in postfix)
        {
            if (double.TryParse(token, out var num))
            {
                stack.Push(num);
            }
            else if (token == "+")
            {
                var b = stack.Pop(); var a = stack.Pop(); stack.Push(a + b);
            }
            else if (token == "-")
            {
                var b = stack.Pop(); var a = stack.Pop(); stack.Push(a - b);
            }
            else if (token == "*")
            {
                var b = stack.Pop(); var a = stack.Pop(); stack.Push(a * b);
            }
            else if (token == "/")
            {
                var b = stack.Pop(); var a = stack.Pop();
                if (b == 0) throw new DivideByZeroException("Division by zero");
                stack.Push(a / b);
            }
            else if (token == "^")
            {
                var b = stack.Pop(); var a = stack.Pop(); stack.Push(Math.Pow(a, b));
            }
            else
            {
                // Variable
                if (variables.TryGetValue(token, out var val))
                    stack.Push(val);
                else
                    throw new InvalidOperationException($"Unknown variable: {token}");
            }
        }

        return stack.Pop();
    }
}

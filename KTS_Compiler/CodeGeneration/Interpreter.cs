using System;
using System.Collections.Generic;

namespace KTS_Compiler
{
    /*public sealed class GenericVoid
    {
        private GenericVoid()
        {
            throw new InvalidOperationException("Void can't be instantiated.");
        }
    }

    public class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<GenericVoid>
    {
        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error.Message);
            }
        }

        private void Execute(Stmt statement)
        {
            statement.Accept(this);
        }

        private void ExecuteBlock(List<Stmt> statements, NoxEnvironment environment)
        {
            var previous = Environment;

            try
            {
                Environment = environment;

                foreach (var statement in statements)
                {
                    Execute(statement);
                }
            }
            finally
            {
                Environment = previous;
            }
        }

        public object VisitBinaryExpression(Expr.Binary exp)
        {
            Object left = Evaluate(exp.Left);
            Object right = Evaluate(exp.Right);

            switch (exp.Operator.Type)
            {
                case TokenType.MINUS:
                    {
                        CheckNumberOperands(exp.Operator, left, right);
                        return (double)left - (double)right;
                    }
                case TokenType.MULT:
                    {
                        CheckNumberOperands(exp.Operator, left, right);
                        return (double)left * (double)right;
                    }
                case TokenType.DIV:
                    {
                        CheckNumberOperands(exp.Operator, left, right);
                        return (double)left / (double)right;
                    }
                case TokenType.PLUS:
                    {
                        if (left is double l && right is double r)
                        {
                            return l + r;
                        }
                        else if (left is string ls && right is double rs)
                        {
                            return ls + rs;
                        }

                        throw new Exception($"{exp.Operator}, Operands must both be numbers or strings.");
                    }

                case TokenType.GREATER:
                    {
                        CheckNumberOperands(exp.Operator, left, right);
                        return (double)left > (double)right;
                    }
                case TokenType.GREATER_EQUAL:
                    {
                        CheckNumberOperands(exp.Operator, left, right);
                        return (double)left >= (double)right;
                    }
                case TokenType.LESS:
                    {
                        CheckNumberOperands(exp.Operator, left, right);
                        return (double)left < (double)right;
                    }
                case TokenType.LESS_EQUAL:
                    {
                        CheckNumberOperands(exp.Operator, left, right);
                        return (double)left <= (double)right;
                    }

                case TokenType.NOT_EQUAL:
                    {
                        return !IsEqual(left, right);
                    }
                case TokenType.EQUAL:
                    {
                        return IsEqual(left, right);
                    }
            }

            return null;
        }

        public object VisitGroupingExpression(Expr.Grouping exp)
        {
            return Evaluate(exp.Expression);
        }

        public object VisitLiteralExpression(Expr.Literal exp)
        {
            return exp.Value;
        }

        public object VisitTernaryExpression(Expr.Ternary exp)
        {
            object condition = Evaluate(exp.Condition);
            object tr = Evaluate(exp.True);
            object fl = Evaluate(exp.False);

            if (IsTruthy(condition))
            {
                return tr;
            }
            else return fl;
        }

        public object VisitUnaryExpression(Expr.Unary exp)
        {
            object right = Evaluate(exp.Right);

            switch (exp.Operator.Type)
            {
                case TokenType.MINUS:
                    {
                        CheckNumberOperand(exp.Operator, right);
                        return -(double)right;
                    }
                case TokenType.NOT: return !IsTruthy(right);
            }

            return null;
        }

        private string Stringify(object obj)
        {
            if (obj == null) return "null";

            if (obj is double)
            {
                var text = obj.ToString();

                if (text.EndsWith(".0"))
                {
                    text = text[0..^2];
                }

                return text;
            }

            return obj.ToString();
        }

        private void CheckNumberOperand(Token op, object operand)
        {
            if (operand is double) return;

            throw new Exception($"{op}, Operand must be a number.");
        }

        private void CheckNumberOperands(Token op, object left, object right)
        {
            if (left is double && right is double) return;

            throw new Exception($" {op} Operands must be a numbers.");
        }

        private bool IsEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null) return false;

            return a.Equals(b);
        }

        private bool IsTruthy(object obj)
        {
            if (obj == null) return false;
            if (obj is bool t) return t;

            return true;
        }

        private object Evaluate(Expr exp)
        {
            return exp.Accept(this);
        }

        public GenericVoid VisitBlockStatement(Stmt.Block statement)
        {
            ExecuteBlock(statement.Statements, new NoxEnvironment(Environment));

            return null;
        }

        public GenericVoid VisitPrintStatement(Stmt.Print statement)
        {
            var value = Evaluate(statement.Expression);

            Console.WriteLine(Stringify(value));

            return null;
        }

        public GenericVoid VisitExpressionStatement(Stmt.ExpressionStatement statement)
        {
            Evaluate(statement.Expression);

            return null;
        }

        public GenericVoid VisitVariableStatement(Stmt.Var statement)
        {
            object value = null;

            if (statement.Initializer != null)
            {
                value = Evaluate(statement.Initializer);
            }

            Environment.Define(statement.Name, value);
            return null;
        }

        public object VisitVariableExpression(Expr.Variable exp)
        {
            return Environment[exp.Name];
        }

        public object VisitAssignmentExpression(Expr.Assign exp)
        {
            var value = Evaluate(exp.Value);
            Environment.Assign(exp.Name, value);
            return value;
        }
    }*/
}

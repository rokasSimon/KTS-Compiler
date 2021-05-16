using System;
using System.Text;

namespace KTS_Compiler
{
    public class AstPrinter : Expr.IVisitor<string>
    {
        public string Print(Expr exp)
        {
            return exp.Accept(this);
        }

        public string VisitBinaryExpression(Expr.Binary exp)
        {
            return Parenthesize(exp.Operator.Source, exp.Left, exp.Right);
        }

        public string VisitGroupingExpression(Expr.Grouping exp)
        {
            return Parenthesize("group", exp.Expression);
        }

        public string VisitLiteralExpression(Expr.Literal exp)
        {
            if (exp.Value == null)
            {
                return "null";
            }

            return exp.Value.ToString();
        }

        public string VisitUnaryExpression(Expr.Unary exp)
        {
            return Parenthesize(exp.Operator.Source, exp.Right);
        }

        private string Parenthesize(string name, params Expr[] expres)
        {
            var output = new StringBuilder();

            output.Append("(").Append(name);

            foreach (var expression in expres)
            {
                output.Append(" ").Append(expression.Accept(this));
            }

            output.Append(")");

            return output.ToString();
        }

        public string VisitVariableExpression(Expr.Variable exp)
        {
            throw new NotImplementedException();
        }

        public string VisitAssignmentExpression(Expr.Assign exp)
        {
            throw new NotImplementedException();
        }

        public string VisitArrayInitializer(Expr.ArrayInitializer exp)
        {
            throw new NotImplementedException();
        }

        public string VisitLogicalExpression(Expr.Logical exp)
        {
            throw new NotImplementedException();
        }

        public string VisitCastExpression(Expr.Cast exp)
        {
            throw new NotImplementedException();
        }

        public string VisitIndexAccessExpression(Expr.IndexAccess exp)
        {
            throw new NotImplementedException();
        }

        public string VisitInvokeExpression(Expr.Invoke exp)
        {
            throw new NotImplementedException();
        }

        public string VisitRangeExpression(Expr.Range exp)
        {
            throw new NotImplementedException();
        }
    }
}

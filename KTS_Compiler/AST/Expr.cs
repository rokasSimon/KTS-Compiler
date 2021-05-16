using System.Collections.Generic;

namespace KTS_Compiler
{
    public abstract class Expr
    {
        public interface IVisitor<T>
        {
            public T VisitBinaryExpression(Expr.Binary exp);
            public T VisitGroupingExpression(Expr.Grouping exp);
            public T VisitLiteralExpression(Expr.Literal exp);
            public T VisitUnaryExpression(Expr.Unary exp);
            public T VisitVariableExpression(Expr.Variable exp);
            public T VisitAssignmentExpression(Expr.Assign exp);
            public T VisitArrayInitializer(Expr.ArrayInitializer exp);
            public T VisitLogicalExpression(Expr.Logical exp);
            public T VisitCastExpression(Expr.Cast exp);
            public T VisitIndexAccessExpression(Expr.IndexAccess exp);
            public T VisitInvokeExpression(Expr.Invoke exp);
            public T VisitRangeExpression(Expr.Range exp);
        }

        public abstract T Accept<T>(IVisitor<T> visitor);

        public class Binary : Expr
        {
            public Expr Left { get; }
            public Token Operator { get; }
            public Expr Right { get; }

            public Binary(Expr left, Token op, Expr right)
            {
                Left = left;
                Operator = op;
                Right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitBinaryExpression(this);
            }
        }

        public class Grouping : Expr
        {
            public Expr Expression { get; }

            public Grouping(Expr exp)
            {
                Expression = exp;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitGroupingExpression(this);
            }
        }

        public class Literal : Expr
        {
            public TypeEnum Type { get; set; }
            public object Value { get; }

            public Literal(TypeEnum type, object value)
            {
                Type = type;
                Value = value;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitLiteralExpression(this);
            }
        }

        public class Unary : Expr
        {
            public Token Operator { get; }
            public Expr Right { get; }

            public Unary(Token op, Expr right)
            {
                Operator = op;
                Right = right;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitUnaryExpression(this);
            }
        }

        public class Logical : Expr
        {
            public Expr Left { get; set; }
            public Token Operator { get; set; }
            public Expr Right { get; set; }

            public Logical(Expr l, Token op, Expr r)
            {
                Left = l;
                Operator = op;
                Right = r;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitLogicalExpression(this);
            }
        }

        public class Variable : Expr
        {
            public Token Name { get; }

            public Variable(Token name)
            {
                Name = name;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitVariableExpression(this);
            }
        }

        public class Assign : Expr
        {
            public Expr Var { get; }
            public Expr Value { get; }

            public Assign(Expr variable, Expr value)
            {
                Var = variable;
                Value = value;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitAssignmentExpression(this);
            }
        }

        public class ArrayInitializer : Expr
        {
            public List<Expr> DimensionSizes { get; set; }

            public ArrayInitializer(List<Expr> dimensions)
            {
                DimensionSizes = dimensions;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitArrayInitializer(this);
            }
        }

        public class Cast : Expr
        {
            public TypeSpecifier TargetType { get; set; }
            public Expr Expression { get; set; }

            public Cast(TypeSpecifier target, Expr expr)
            {
                TargetType = target;
                Expression = expr;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitCastExpression(this);
            }
        }

        public class IndexAccess : Expr
        {
            public Token Identifier { get; set; }
            public List<Expr> Expressions { get; set; }

            public IndexAccess(Token ident, List<Expr> expr)
            {
                Identifier = ident;
                Expressions = expr;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitIndexAccessExpression(this);
            }
        }

        public class Invoke : Expr
        {
            public Token Identifier { get; set; }
            public List<Expr> Arguments { get; set; }

            public Invoke(Token identifier, List<Expr> args)
            {
                Identifier = identifier;
                Arguments = args;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitInvokeExpression(this);
            }
        }

        public class Range : Expr
        {
            public bool InclusiveStart { get; set; }
            public bool InclusiveEnd { get; set; }
            public Expr Start { get; set; }
            public Expr End { get; set; }

            public Range(bool istart, bool iend, Expr start, Expr end)
            {
                InclusiveStart = istart;
                InclusiveEnd = iend;
                Start = start;
                End = end;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitRangeExpression(this);
            }
        }
    }
}

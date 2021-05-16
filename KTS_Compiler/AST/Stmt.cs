using System.Collections.Generic;

namespace KTS_Compiler
{
    public abstract class Stmt
    {
        public interface IVisitor<T>
        {
            public T VisitBlockStatement(BlockStatement statement);
            public T VisitFunctionDeclaration(FunctionDeclaration statement);
            public T VisitWhileStatement(While statement);
            public T VisitForStatement(For statement);
            public T VisitDeclarationStatement(VariableDeclaration statement);
            public T VisitReturnStatement(Return statement);
            public T VisitExpressionStatement(ExpressionStatement statement);
            public T VisitIfStatement(If statement);
        }

        public abstract T Accept<T>(IVisitor<T> visitor);

        public class BlockStatement : Stmt
        {
            public List<Stmt> Statements { get; set; }

            public BlockStatement(List<Stmt> statements)
            {
                Statements = statements;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitBlockStatement(this);
            }
        }

        public class ExpressionStatement : Stmt
        {
            public Expr Expression { get; }

            public ExpressionStatement(Expr expression)
            {
                Expression = expression;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitExpressionStatement(this);
            }
        }

        public class FunctionDeclaration : Stmt
        {
            public TypeSpecifier ReturnType { get; set; }
            public Token Identifier { get; set; }
            public List<Parameter> Parameters { get; set; }
            public Stmt FunctionBody { get; set; }

            public FunctionDeclaration(TypeSpecifier returnType, Token identifier, List<Parameter> parameters, Stmt functionBody)
            {
                ReturnType = returnType;
                Identifier = identifier;
                Parameters = parameters;
                FunctionBody = functionBody;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitFunctionDeclaration(this);
            }
        }

        public class VariableDeclaration : Stmt
        {
            public TypeSpecifier TypeSpecifier { get; set; }
            public Token Identifier { get; set; }
            public Binding Binding { get; set; }
            public Expr Initializer { get; set; }

            public VariableDeclaration(TypeSpecifier type, Token ident, Binding binding, Expr init)
            {
                TypeSpecifier = type;
                Identifier = ident;
                Binding = binding;
                Initializer = init;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitDeclarationStatement(this);
            }
        }

        public class Return : Stmt
        {
            public Expr Value { get; set; }

            public Return(Expr expression)
            {
                Value = expression;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitReturnStatement(this);
            }
        }

        public class For : Stmt
        {
            public TypeSpecifier TypeSpecifier { get; set; }
            public Token Identifier { get; set; }
            public Expr.Range RangeExpression { get; set; }
            public Stmt Body { get; set; }

            public For(TypeSpecifier type, Token ident, Expr.Range range, Stmt body)
            {
                TypeSpecifier = type;
                Identifier = ident;
                RangeExpression = range;
                Body = body;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitForStatement(this);
            }
        }

        public class While : Stmt
        {
            public Expr Condition { get; set; }
            public Stmt Body { get; set; }

            public While(Expr condition, Stmt body)
            {
                Condition = condition;
                Body = body;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitWhileStatement(this);
            }
        }

        public class If : Stmt
        {
            public Expr Condition { get; set; }
            public Stmt MainBlock { get; set; }
            public Stmt ElseBlock { get; set; }

            public If(Expr condition, Stmt main, Stmt el)
            {
                Condition = condition;
                MainBlock = main;
                ElseBlock = el;
            }

            public override T Accept<T>(IVisitor<T> visitor)
            {
                return visitor.VisitIfStatement(this);
            }
        }
    }
}

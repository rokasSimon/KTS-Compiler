using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KTS_Compiler.Expr;

namespace KTS_Compiler
{
    public class ASTTypeChecker : Expr.IVisitor<TypeSpecifier>, Stmt.IVisitor<bool>
    {
        private readonly List<Stmt> Declarations;
        private readonly Dictionary<string, TypeSpecifier> LocalTypes;
        private readonly Dictionary<string, FunctionSymbol> FunctionTable;
        private FunctionSymbol CurrentFunction { get; set; }
        public bool Failed { get; private set; }

        public ASTTypeChecker(List<Stmt> functionDeclarations)
        {
            Declarations = functionDeclarations;
            LocalTypes = new Dictionary<string, TypeSpecifier>();
            FunctionTable = new Dictionary<string, FunctionSymbol>();
            CurrentFunction = null;
            Failed = false;
        }

        public void ExecuteTypeCheck()
        {
            foreach (var functionDeclaration in Declarations)
            {
                Stmt.FunctionDeclaration func = (Stmt.FunctionDeclaration)functionDeclaration;
                
                if (FunctionTable.ContainsKey(func.Identifier.Source))
                {
                    Console.WriteLine($"There already exists a '{func.Identifier.Source}' function declaration");
                }
                else
                {
                    FunctionTable.Add(func.Identifier.Source, new FunctionSymbol { ReturnType = func.ReturnType, Parameters = func.Parameters });
                }
            }

            foreach (var statement in Declarations)
            {
                var success = Examine(statement);

                if (!success)
                {
                    Failed = true;
                }
            }
        }

        private bool Examine(Stmt statement)
        {
            return statement.Accept(this);
        }

        private TypeSpecifier Examine(Expr expression)
        {
            return expression.Accept(this);
        }

        public TypeSpecifier VisitArrayInitializer(ArrayInitializer exp)
        {
            foreach (var expr in exp.DimensionSizes)
            {
                var ts = Examine(expr);

                if (!ts.IsInt())
                {
                    Console.WriteLine($"Array initializer dimensions must be integers");
                    return TypeSpecifier.Null;
                }
            }

            return new TypeSpecifier { Type = TypeEnum.ARRAY, Dimensions = exp.DimensionSizes.Count };
        }

        public TypeSpecifier VisitAssignmentExpression(Assign exp)
        {
            TypeSpecifier variable = Examine(exp.Var);
            TypeSpecifier value = Examine(exp.Value);

            if (value.ImplicitCastableTo(variable))
            {
                return variable;
            }

            if (variable != value)
            {
                Console.WriteLine($"Trying to assign type '{value}' to type '{variable}'");
                return TypeSpecifier.Null;
            }

            return variable;
        }

        public TypeSpecifier VisitBinaryExpression(Binary exp)
        {
            TypeSpecifier left = Examine(exp.Left);
            TypeSpecifier right = Examine(exp.Right);

            switch (exp.Operator.Type)
            {
                case TokenType.PLUS:
                case TokenType.MINUS:
                case TokenType.DIV:
                case TokenType.MULT:
                case TokenType.PERCENT:
                    {
                        if (right.ImplicitCastableTo(left))
                        {
                            return left;
                        }
                        return TypeSpecifier.Null;
                    }
                case TokenType.GREATER:
                case TokenType.GREATER_EQUAL:
                case TokenType.LESS:
                case TokenType.LESS_EQUAL:
                case TokenType.EQUAL:
                case TokenType.NOT_EQUAL:
                    {
                        if (right.ImplicitCastableTo(left))
                        {
                            return new TypeSpecifier { Type = TypeEnum.BOOL, Dimensions = 0 };
                        }

                        return TypeSpecifier.Null;
                    }
                default:
                    {
                        Console.WriteLine($"'{left}' is not implicitly castable to '{right}' at line: {exp.Operator.Line}");
                        return TypeSpecifier.Null;
                    }
            }
        }

        public bool VisitBlockStatement(Stmt.BlockStatement statement)
        {
            bool result = true;

            foreach (var stmt in statement.Statements)
            {
                var success = Examine(stmt);

                if (!success)
                {
                    result = false;
                }
            }

            return result;
        }

        public TypeSpecifier VisitCastExpression(Cast exp)
        {
            TypeSpecifier expr = Examine(exp.Expression);

            if (!expr.CastableTo(exp.TargetType))
            {
                Console.WriteLine($"Can't cast type {expr} to type {exp.TargetType}");
                return TypeSpecifier.Null;
            }

            return exp.TargetType;
        }

        public bool VisitDeclarationStatement(Stmt.VariableDeclaration statement)
        {
            bool result = true;

            if (LocalTypes.ContainsKey(statement.Identifier.Source))
            {
                result = false;
                Console.WriteLine($"'{statement.Identifier.Source}' variable name already used");
            }

            LocalTypes.Add(statement.Identifier.Source, statement.TypeSpecifier);

            if (statement.Binding != null)
            {
                foreach (var functionName in statement.Binding.Identifiers)
                {
                    if (!FunctionTable.ContainsKey(functionName))
                    {
                        Console.WriteLine($"Undeclared function '{functionName}' used in binding on line: {statement.Identifier.Line}");
                        result = false;
                    }
                    else
                    {
                        FunctionSymbol fs = FunctionTable[functionName];

                        if (fs.Parameters.Count > 1 || (fs.Parameters.Count == 1 && fs.Parameters[0].Type != statement.TypeSpecifier))
                        {
                            Console.WriteLine($"Mismatched function parameters and binding variable on line: {statement.Binding.Type.Line}");
                            result = false;
                        }
                    }
                }
            }

            if (statement.Initializer != null)
            {
                TypeSpecifier ts = Examine(statement.Initializer);

                if (ts.ImplicitCastableTo(statement.TypeSpecifier))
                {
                    return result;
                }

                if (ts != statement.TypeSpecifier)
                {
                    Console.WriteLine($"Trying to assign type '{ts}' to a variable of type '{statement.TypeSpecifier}'");
                    result = false;
                }
            }

            return result;
        }

        public bool VisitExpressionStatement(Stmt.ExpressionStatement statement)
        {
            TypeSpecifier ts = Examine(statement.Expression);

            if (ts.Type == TypeEnum.UNKNOWN)
            {
                return false;
            }

            return true;
        }

        public bool VisitForStatement(Stmt.For statement)
        {
            bool result = true;

            if (!statement.TypeSpecifier.IsInt())
            {
                result = false;
                Console.WriteLine("For statement loop variable isn't integer");
            }

            if (LocalTypes.ContainsKey(statement.Identifier.Source))
            {
                result = false;
                Console.WriteLine("For statement variable identifier already used");
            }

            var sg = Examine(statement.RangeExpression.Start);
            if (!sg.IsInt())
            {
                result = false;
                Console.WriteLine("Range start expression is not an integer");
            }

            var eg = Examine(statement.RangeExpression.End);
            if (!eg.IsInt())
            {
                result = false;
                Console.WriteLine("Range end expression is not an integer");
            }

            bool bg = Examine(statement.Body);
            if (!bg)
            {
                result = false;
            }

            return result;
        }

        public bool VisitFunctionDeclaration(Stmt.FunctionDeclaration statement)
        {
            LocalTypes.Clear();
            foreach (var parameter in statement.Parameters)
            {
                LocalTypes.Add(parameter.Identifier, parameter.Type);
            }

            CurrentFunction = FunctionTable[statement.Identifier.Source];

            return Examine(statement.FunctionBody);
        }

        public TypeSpecifier VisitGroupingExpression(Grouping exp)
        {
            return Examine(exp.Expression);
        }

        public bool VisitIfStatement(Stmt.If statement)
        {
            bool result = true;

            var cg = Examine(statement.Condition);
            if (cg.Type != TypeEnum.BOOL)
            {
                result = false;
                Console.WriteLine("Not a boolean value in if condition");
            }

            bool ms = Examine(statement.MainBlock);
            if (!ms)
            {
                result = false;
                Console.WriteLine("If statement main block is bad");
            }

            if (statement.ElseBlock != null)
            {
                bool es = Examine(statement.ElseBlock);

                if (!es)
                {
                    result = false;
                    Console.WriteLine("If statement else block is bad");
                }
            }

            return result;
        }

        public TypeSpecifier VisitIndexAccessExpression(IndexAccess exp)
        {
            if (!LocalTypes.ContainsKey(exp.Identifier.Source))
            {
                Console.WriteLine($"Trying access non-existant array '{exp.Identifier}'");
                return TypeSpecifier.Null;
            }
            else
            {
                TypeSpecifier ts = LocalTypes[exp.Identifier.Source];

                if (ts.Dimensions != exp.Expressions.Count)
                {
                    Console.WriteLine($"Mismatched dimensions when accessing array at line: {exp.Identifier.Line}");
                    return TypeSpecifier.Null;
                }

                for (int i = 0; i < ts.Dimensions; i++)
                {
                    var expr = Examine(exp.Expressions[i]);

                    if (!expr.IsInt())
                    {
                        Console.WriteLine($"Non-integer array index access at line: {exp.Identifier.Line}");
                        return TypeSpecifier.Null;
                    }
                }

                return new TypeSpecifier { Type = ts.Type, Dimensions = 0 };
            }
        }

        public TypeSpecifier VisitInvokeExpression(Invoke exp)
        {
            if (!FunctionTable.ContainsKey(exp.Identifier.Source))
            {
                Console.WriteLine($"No function by name '{exp.Identifier.Source}' exists");
                return new TypeSpecifier { Type = TypeEnum.UNKNOWN };
            }
            else
            {
                var function = FunctionTable[exp.Identifier.Source];

                if (exp.Arguments.Count != function.Parameters.Count)
                {
                    Console.WriteLine($"Mismatched number of arguments for '{exp.Identifier.Source}' call at line: {exp.Identifier.Line}");
                    return TypeSpecifier.Null;
                }

                for (int i = 0; i < exp.Arguments.Count; i++)
                {
                    TypeSpecifier argType = Examine(exp.Arguments[i]);
                    TypeSpecifier paramType = function.Parameters[i].Type;

                    if (argType.Type != paramType.Type)
                    {
                        Console.WriteLine($"Mismatched argument types for '{exp.Identifier.Source}' call at line: {exp.Identifier.Line}");
                        return TypeSpecifier.Null;
                    }
                }

                return function.ReturnType;
            }
        }

        public TypeSpecifier VisitLiteralExpression(Literal exp)
        {
            return new TypeSpecifier { Type = exp.Type, Dimensions = 0 };
        }

        public TypeSpecifier VisitLogicalExpression(Logical exp)
        {
            var tleft = Examine(exp.Left);
            var tright = Examine(exp.Right);

            if (tleft.Type != TypeEnum.BOOL || tright.Type != TypeEnum.BOOL)
            {
                Console.WriteLine($"Not a boolean on either side of a logical operator on line: {exp.Operator.Line}");
                return new TypeSpecifier { Type = TypeEnum.UNKNOWN };
            }

            return new TypeSpecifier { Type = TypeEnum.BOOL };
        }

        public TypeSpecifier VisitRangeExpression(Expr.Range exp)
        {
            return new TypeSpecifier { Type = TypeEnum.UNKNOWN };
        }

        public bool VisitReturnStatement(Stmt.Return statement)
        {
            if (statement.Value == null)
            {
                if (CurrentFunction.ReturnType.Type == TypeEnum.VOID)
                {
                    return true;
                }

                return false;
            }

            var sr = Examine(statement.Value);
            if (sr.ImplicitCastableTo(CurrentFunction.ReturnType))
            {
                return true;
            }

            if (sr != CurrentFunction.ReturnType)
            {
                Console.WriteLine("Mismatched return type");
                return false;
            }

            return true;
        }

        public TypeSpecifier VisitUnaryExpression(Unary exp)
        {
            var ts = Examine(exp.Right);

            if (ts.Type == TypeEnum.BOOL && exp.Operator.Type == TokenType.NOT)
            {
                return new TypeSpecifier { Type = TypeEnum.BOOL };
            }
            else if (ts.IsInt() && exp.Operator.Type == TokenType.MINUS || ts.IsFloat() && exp.Operator.Type == TokenType.MINUS)
            {
                return ts;
            }
            else
            {
                Console.WriteLine($"Mismatched unary operator at line: {exp.Operator.Line}");
                return new TypeSpecifier { Type = TypeEnum.UNKNOWN };
            }
        }

        public TypeSpecifier VisitVariableExpression(Variable exp)
        {
            if (LocalTypes.ContainsKey(exp.Name.Source))
            {
                return LocalTypes[exp.Name.Source];
            }
            else
            {
                Console.WriteLine($"Undeclared variable '{exp.Name.Source}' used at line: {exp.Name.Line}");
                return new TypeSpecifier { Type = TypeEnum.UNKNOWN };
            }
        }

        public bool VisitWhileStatement(Stmt.While statement)
        {
            bool result = true;

            var sc = Examine(statement.Condition);
            if (sc.Type != TypeEnum.BOOL)
            {
                result = false;
                Console.WriteLine("Not a boolean condition in while loop");
            }

            bool sb = Examine(statement.Body);
            if (!sb)
            {
                result = false;
                Console.WriteLine("Bad while loop body");
            }

            return result;
        }
    }
}

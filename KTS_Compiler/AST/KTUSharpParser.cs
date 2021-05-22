using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace KTS_Compiler
{
    public class KTUSharpParser
    {
        private int Current { get; set; }
        private List<Token> Tokens { get; set; }
        private List<string> Errors { get; set; }

        private bool AtEnd => Tokens[Current].Type == TokenType.EOF;
        private Token Lookahead => Tokens[Current];
        private Token Previous => Tokens[Current - 1];

        public KTUSharpParser(List<Token> tokens)
        {
            Errors = new List<string>();
            Tokens = tokens;
            Current = 0;
        }

        public List<Stmt> ParseTokens()
        {
            var functionDeclarations = new List<Stmt>();

            while (!AtEnd)
            {
                Stmt statement = FunctionDeclaration();

                if (statement == null)
                    break;

                functionDeclarations.Add(statement);
            }

            foreach (var error in Errors)
            {
                Console.WriteLine(error);
            }

            return functionDeclarations;
        }

        private Stmt FunctionDeclaration()
        {
            try
            {
                TypeSpecifier typeSpecifier = TypeSpec("Function declarations must begin with a type specifier");
                Token name = Consume(TokenType.IDENTIFIER, "Functions must have names");
                var paramList = ParameterList(name.Source);
                var functBody = Block();

                return new Stmt.FunctionDeclaration(typeSpecifier, name, paramList, functBody);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private TypeSpecifier TypeSpec(string error)
        {
            Token specifier = Consume(TokenType.TYPE_SPECIFIER, error);
            TypeEnum type = TypeSpecifier.FromString(specifier.Value.ToString());

            if (type == TypeEnum.UNKNOWN)
            {
                throw new Exception("何???");
            }

            var typeSpec = type == TypeEnum.STRING
                         ? new TypeSpecifier { Dimensions = 1, Type = TypeEnum.I8 }
                         : new TypeSpecifier { Dimensions = 0, Type = type };

            while (Match(TokenType.LEFT_BRACKET))
            {
                Consume(TokenType.RIGHT_BRACKET, "Unclosed array type specifier");
                typeSpec.Dimensions++;
            }

            return typeSpec;
        }

        private List<Parameter> ParameterList(string functionName)
        {
            Consume(TokenType.LEFT_PARENTH, $"Missing opening parenthesis after '{functionName}'");

            if (Match(TokenType.RIGHT_PARENTH))
            {
                return new List<Parameter>();
            }

            var paramList = new List<Parameter>();

            while (true)
            {
                bool refpass = Match(TokenType.REF);
                TypeSpecifier tspec = TypeSpec("Function parameter list must have parameter types");
                if (tspec.Type == TypeEnum.VOID)
                {
                    throw Error("Function parameters cannot have 'void' type");
                }
                Token name = Consume(TokenType.IDENTIFIER, "No name given for function parameter");

                paramList.Add(new Parameter { Reference = refpass, Type = tspec, Identifier = name.Source });

                if (Match(TokenType.RIGHT_PARENTH))
                {
                    return paramList;
                }

                Consume(TokenType.COMMA, $"Unfinished parameter list in '{functionName}' function header");
            }
        }

        private Stmt Block()
        {
            Consume(TokenType.LEFT_BRACE, "Blocks start with '{'");

            var statements = new List<Stmt>();

            while (true)
            {
                if (NextIsType(TokenType.TYPE_SPECIFIER)) statements.Add(VariableDeclaration());
                else if (Match(TokenType.FOR)) statements.Add(ForStatement());
                else if (Match(TokenType.WHILE)) statements.Add(WhileStatement());
                else if (Match(TokenType.IF)) statements.Add(IfStatement());
                else if (Match(TokenType.RETURN)) statements.Add(ReturnStatement());
                else
                {
                    statements.Add(ExpressionStatement());
                }
                
                if (Match(TokenType.RIGHT_BRACE))
                {
                    return new Stmt.BlockStatement(statements);
                }

                if (AtEnd)
                {
                    throw Error($"Unclosed block");
                }
            }
        }

        private Stmt VariableDeclaration()
        {
            TypeSpecifier type = TypeSpec("");
            Token identifier = Consume(TokenType.IDENTIFIER, "No identifier for variable declaration");
            Binding binding = null;
            Expr initializer = null;

            if (Match(TokenType.ON))
            {
                binding = Binding();
            }
            if (Match(TokenType.ASSIGN))
            {
                initializer = VariableInitializer();
            }

            Consume(TokenType.SEMICOLON, "Variable declarations end with a ';'");

            return new Stmt.VariableDeclaration(type, identifier, binding, initializer);
        }

        private Binding Binding()
        {
            var binding = new Binding();

            Consume(TokenType.LESS, "No '<' at binding type declaration");

            if (Match(TokenType.ANY, TokenType.READ, TokenType.WRITE))
            {
                binding.Type = Previous;
            }
            else
            {
                throw Error("Given token does not match any binding types");
            }

            Consume(TokenType.GREATER, "No '>' at binding type declaration");

            binding.Identifiers = new List<string>();

            Token firstFunc = Consume(TokenType.IDENTIFIER, "Bindings must have at least one identifier");
            binding.Identifiers.Add(firstFunc.Source);

            while (true)
            {
                if (Match(TokenType.COMMA))
                {
                    Token bindFunction = Consume(TokenType.IDENTIFIER, "No additional identifier given after comma");
                    binding.Identifiers.Add(bindFunction.Source);
                }
                else if (NextAnyOf(TokenType.ASSIGN, TokenType.SEMICOLON)) return binding;
                else throw Error("Unfinished binding function list");
            }
        }

        private Expr VariableInitializer()
        {
            Expr initializer;

            if (Match(TokenType.NEW))
            {
                initializer = ArrayInitializer();
            }
            else
            {
                initializer = Expression();
            }

            return initializer;
        }

        private Expr ArrayInitializer()
        {
            var dim = new List<Expr>();

            while (true)
            {
                Consume(TokenType.LEFT_BRACKET, "No opening array bracket");

                dim.Add(Expression());

                Consume(TokenType.RIGHT_BRACKET, "No closing array bracket");

                if (NextIsType(TokenType.SEMICOLON))
                {
                    return new Expr.ArrayInitializer(dim);
                }
            }
        }

        private Stmt ForStatement()
        {
            Consume(TokenType.LEFT_PARENTH, "Expect '(' after 'for'");
            TypeSpecifier type = TypeSpec("Must declare loop variable type");
            Token identifier = Consume(TokenType.IDENTIFIER, "Expect identifier after type specifier");
            Consume(TokenType.IN, "Expect 'in' after loop variable identifier");

            bool istart;
            bool iend;

            if (Match(TokenType.LEFT_BRACKET)) istart = true;
            else if (Match(TokenType.LEFT_PARENTH)) istart = false;
            else throw Error("Expected '[' or '(' at range start");

            Expr start = Expression();

            Consume(TokenType.RANGE, "Expect '..' after range start expression");

            Expr end = Expression();

            if (Match(TokenType.RIGHT_BRACKET)) iend = true;
            else if (Match(TokenType.RIGHT_PARENTH)) iend = false;
            else throw Error("Expected ']' or ')' at range end");

            Consume(TokenType.RIGHT_PARENTH, "Expect ')' to finish for loop header");

            var block = Block();

            return new Stmt.For(type, identifier, new Expr.Range(istart, iend, start, end), block);
        }

        private Stmt WhileStatement()
        {
            Consume(TokenType.LEFT_PARENTH, "Expect '(' after 'while'");
            var expr = LogicalOr();
            Consume(TokenType.RIGHT_PARENTH, "Expect ')' after boolean expression");

            var block = Block();

            return new Stmt.While(expr, block);
        }

        private Stmt IfStatement()
        {
            Consume(TokenType.LEFT_PARENTH, "Expect '(' after 'if'");
            var cond = LogicalOr();
            Consume(TokenType.RIGHT_PARENTH, "Expect ')' after boolean expression");

            Stmt mainblock = Block();
            Stmt elseblock = null;

            if (Match(TokenType.ELSE))
            {
                if (Match(TokenType.IF))
                {
                    elseblock = IfStatement();
                }
                else
                {
                    elseblock = Block();
                }
            }

            return new Stmt.If(cond, mainblock, elseblock);
        }

        private Stmt ReturnStatement()
        {
            if (Match(TokenType.SEMICOLON))
            {
                return new Stmt.Return(null);
            }

            Expr returnExpression = Expression();

            Consume(TokenType.SEMICOLON, "No ';' after return expression");

            return new Stmt.Return(returnExpression);
        }

        private Stmt ExpressionStatement()
        {
            var expr = Expression();
            Consume(TokenType.SEMICOLON, "A ';' must proceed an expression");
            return new Stmt.ExpressionStatement(expr);
        }

        private Expr Expression()
        {
            return Assignment();
        }

        private Expr Assignment()
        {
            var expr = LogicalOr();

            if (Match(TokenType.ASSIGN))
            {
                if (expr is Expr.Variable || expr is Expr.IndexAccess)
                {
                    Expr value = Assignment();

                    return new Expr.Assign(expr, value);
                }

                throw Error("Trying to assign rvalue to rvalue");
            }

            return expr;
        }

        private Expr LogicalOr()
        {
            var expr = LogicalAnd();

            while (Match(TokenType.OR))
            {
                Token op = Previous;
                Expr right = LogicalAnd();

                if (expr is Expr.Literal l && right is Expr.Literal r)
                {
                    var typel = new TypeSpecifier { Type = l.Type, Dimensions = 0 };
                    var typer = new TypeSpecifier { Type = r.Type, Dimensions = 0 };

                    if (typel.Type == TypeEnum.BOOL && typer.Type == TypeEnum.BOOL)
                    {
                        return new Expr.Literal(TypeEnum.BOOL, (bool)l.Value || (bool)r.Value);
                    }
                    else
                    {
                        throw Error($"'{typer}' not implicitly castable to '{typel}'");
                    }
                }

                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr LogicalAnd()
        {
            var expr = Equality();

            while (Match(TokenType.AND))
            {
                Token op = Previous;
                Expr right = Equality();

                if (expr is Expr.Literal l && right is Expr.Literal r)
                {
                    var typel = new TypeSpecifier { Type = l.Type, Dimensions = 0 };
                    var typer = new TypeSpecifier { Type = r.Type, Dimensions = 0 };

                    if (typel.Type == TypeEnum.BOOL && typer.Type == TypeEnum.BOOL)
                    {
                        return new Expr.Literal(TypeEnum.BOOL, (bool)l.Value && (bool)r.Value);
                    }
                    else
                    {
                        throw Error($"'{typer}' not implicitly castable to '{typel}'");
                    }
                }

                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr Equality()
        {
            var expr = Relational();

            while (Match(TokenType.NOT_EQUAL, TokenType.EQUAL))
            {
                Token op = Previous;
                Expr right = Relational();

                if (expr is Expr.Literal l && right is Expr.Literal r)
                {
                    var typel = new TypeSpecifier { Type = l.Type, Dimensions = 0 };
                    var typer = new TypeSpecifier { Type = r.Type, Dimensions = 0 };

                    if (typer.ImplicitCastableTo(typel))
                    {
                        if (op.Type == TokenType.NOT_EQUAL)
                        {
                            return new Expr.Literal(TypeEnum.BOOL, !l.Value.Equals(r.Value));
                        }
                        else
                        {
                            return new Expr.Literal(TypeEnum.BOOL, l.Value.Equals(r.Value));
                        }
                    }
                    else
                    {
                        throw Error($"'{typer}' not implicitly castable to '{typel}'");
                    }
                }

                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Relational()
        {
            var expr = Additive();

            while (Match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token op = Previous;
                Expr right = Additive();

                if (expr is Expr.Literal l && right is Expr.Literal r)
                {
                    var typel = new TypeSpecifier { Type = l.Type, Dimensions = 0 };
                    var typer = new TypeSpecifier { Type = r.Type, Dimensions = 0 };

                    if (typer.ImplicitCastableTo(typel))
                    {
                        if (typel.Type == TypeEnum.BOOL)
                        {
                            throw Error($"Boolean values are not comparable with '{op.Type}'");
                        }

                        if (typel.IsInt())
                        {
                            return CompareLiterals<long>(op.Type, l.Value, r.Value);
                        }
                        else if (typel.IsFloat())
                        {
                            return CompareLiterals<double>(op.Type, l.Value, r.Value);
                        }
                        else
                        {
                            throw Error("Something wrong with literal comparison");
                        }
                    }
                    else
                    {
                        throw Error($"'{typer}' not implicitly castable to '{typel}'");
                    }
                }

                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Additive()
        {
            var expr = Multiplicative();

            while (Match(TokenType.MINUS, TokenType.PLUS))
            {
                Token op = Previous;
                Expr right = Multiplicative();

                if (expr is Expr.Literal l && right is Expr.Literal r)
                {
                    var typel = new TypeSpecifier { Type = l.Type, Dimensions = 0 };
                    var typer = new TypeSpecifier { Type = r.Type, Dimensions = 0 };

                    if (typer.ImplicitCastableTo(typel))
                    {
                        if (typel.IsInt())
                        {
                            long one = (long)l.Value;
                            long two = (long)r.Value;

                            if (op.Type == TokenType.MINUS)
                            {
                                return new Expr.Literal(typel.Type, one - two);
                            }
                            else
                            {
                                return new Expr.Literal(typel.Type, one + two);
                            }
                        }
                        else if (typel.IsFloat())
                        {
                            double one = (double)l.Value;
                            double two = (double)r.Value;

                            if (op.Type == TokenType.MINUS)
                            {
                                return new Expr.Literal(typel.Type, one - two);
                            }
                            else
                            {
                                return new Expr.Literal(typel.Type, one + two);
                            }
                        }
                        else
                        {
                            throw Error("Something wrong on Additive");
                        }
                    }
                    else
                    {
                        throw Error($"'{typer}' not implicitly castable to '{typel}'");
                    }
                }

                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Multiplicative()
        {
            var expr = Cast();

            while (Match(TokenType.DIV, TokenType.MULT))
            {
                Token op = Previous;
                Expr right = Cast();

                if (expr is Expr.Literal l && right is Expr.Literal r)
                {
                    var typel = new TypeSpecifier { Type = l.Type, Dimensions = 0 };
                    var typer = new TypeSpecifier { Type = r.Type, Dimensions = 0 };

                    if (typer.ImplicitCastableTo(typel))
                    {
                        if (typel.IsInt())
                        {
                            long one = (long)l.Value;
                            long two = (long)r.Value;

                            if (op.Type == TokenType.DIV)
                            {
                                return new Expr.Literal(typel.Type, one / two);
                            }
                            else
                            {
                                return new Expr.Literal(typel.Type, one * two);
                            }
                        }
                        else if (typel.IsFloat())
                        {
                            double one = (double)l.Value;
                            double two = (double)r.Value;

                            if (op.Type == TokenType.DIV)
                            {
                                return new Expr.Literal(typel.Type, one / two);
                            }
                            else
                            {
                                return new Expr.Literal(typel.Type, one * two);
                            }
                        }
                        else
                        {
                            throw Error("Something wrong on Multiplicative");
                        }
                    }
                    else
                    {
                        throw Error($"'{typer}' not implicitly castable to '{typel}'");
                    }
                }

                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr Cast()
        {
            if (Match(TokenType.CAST))
            {
                Consume(TokenType.LESS, "No '<' before cast type");

                var typeSpecifier = TypeSpec("No cast target type");

                Consume(TokenType.GREATER, "No '>' after cast type");
                Consume(TokenType.LEFT_PARENTH, "No '(' before cast expression");

                Expr inner = Cast();

                Consume(TokenType.RIGHT_PARENTH, "No ')' after cast expression");

                return new Expr.Cast(typeSpecifier, inner);
            }

            return Unary();
        }

        private Expr Unary()
        {
            if (Match(TokenType.NOT, TokenType.MINUS))
            {
                Token op = Previous;
                Expr right = Cast();

                if (right is Expr.Literal lit)
                {
                    var type = new TypeSpecifier { Type = lit.Type, Dimensions = 0 };

                    if (op.Type == TokenType.NOT)
                    {
                        if (type.IsBool())
                        {
                            bool value = (bool)lit.Value;

                            return new Expr.Literal(lit.Type, !value);
                        }
                        else
                        {
                            throw Error("Trying to invert a non-boolean value");
                        }
                    }
                    else
                    {
                        if (type.IsInt())
                        {
                            long value = (long)lit.Value;

                            return new Expr.Literal(lit.Type, -value);
                        }
                        else if (type.IsFloat())
                        {
                            double value = (double)lit.Value;

                            return new Expr.Literal(lit.Type, -value);
                        }
                        else
                        {
                            throw Error("Trying to negate a non-numeric value");
                        }
                    }
                }

                return new Expr.Unary(op, right);
            }

            return Invoke();
        }

        private Expr Invoke()
        {
            Expr expr = Primary();

            if (Match(TokenType.LEFT_PARENTH))
            {
                if (expr is Expr.Variable function)
                {
                    var arguments = new List<Expr>();

                    if (Match(TokenType.RIGHT_PARENTH))
                    {
                        return new Expr.Invoke(function.Name, arguments);
                    }

                    while (true)
                    {
                        arguments.Add(Expression());

                        if (Match(TokenType.RIGHT_PARENTH))
                        {
                            return new Expr.Invoke(function.Name, arguments);
                        }

                        Consume(TokenType.COMMA, "Expected ',' after argument and no ')' was found");
                    }
                }
                else
                {
                    throw Error("Calling a function on a non-variable");
                }
            }

            return expr;
        }

        private Expr Primary()
        {
            if (Match(TokenType.TRUE)) return new Expr.Literal(TypeEnum.BOOL, true);
            if (Match(TokenType.FALSE)) return new Expr.Literal(TypeEnum.BOOL, false);
            if (Match(TokenType.INTEGER))
            {
                long num = (long)Previous.Value;

                return new Expr.Literal(TypeSpecifier.IntFromConst(num), num);
            }
            if (Match(TokenType.FLOAT))
            {
                double num = (double)Previous.Value;

                return new Expr.Literal(TypeSpecifier.FloatFromConst(num), num);
            }
            if (Match(TokenType.STRING))
            {
                string value = (string)Previous.Value;

                return new Expr.Literal(TypeEnum.STRING, Regex.Unescape(value));
            }
            if (Match(TokenType.CHAR)) return new Expr.Literal(TypeEnum.I8, Previous.Value);
            if (Match(TokenType.IDENTIFIER))
            {
                Token identifier = Previous;

                if (NextIsType(TokenType.LEFT_BRACKET))
                {
                    List<Expr> expressions = new List<Expr>();

                    while (Match(TokenType.LEFT_BRACKET))
                    {
                        var expr = Expression();
                        Consume(TokenType.RIGHT_BRACKET, "Expect ']' after array access expression");

                        expressions.Add(expr);
                    }

                    return new Expr.IndexAccess(identifier, expressions);
                }

                return new Expr.Variable(identifier);
            }
            if (Match(TokenType.LEFT_PARENTH))
            {
                var expr = Expression();
                Consume(TokenType.RIGHT_PARENTH, "Expect ')' after grouping expression");
                return new Expr.Grouping(expr);
            }

            throw Error("Expect expression");
        }

        private bool Match(params TokenType[] tokens)
        {
            foreach (var token in tokens)
            {
                if (NextIsType(token))
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }

        private Token Consume(TokenType token, string message)
        {
            if (NextIsType(token)) return Advance();

            Errors.Add($"Line: {Lookahead.Line} | Error: {message}");
            throw new Exception();
        }

        private Exception Error(string message)
        {
            Errors.Add($"Line: {Lookahead.Line} | Error: {message}");
            return new Exception();
        }

        private bool NextIsType(TokenType token)
        {
            if (AtEnd) return false;

            return Lookahead.Type == token;
        }

        private bool NextAnyOf(params TokenType[] tokens)
        {
            if (AtEnd) return false;

            foreach (var token in tokens)
            {
                if (Lookahead.Type == token) return true;
            }

            return false;
        }

        private Token Advance()
        {
            if (!AtEnd) Current++;

            return Previous;
        }

        private void Synchronize()
        {
            Advance();

            while (!AtEnd)
            {
                if (Previous.Type == TokenType.SEMICOLON) return;

                switch (Lookahead.Type)
                {
                    case TokenType.FOR:
                    case TokenType.WHILE:
                    case TokenType.IF:
                    case TokenType.ELSE:
                    case TokenType.RETURN:
                        return;
                }

                Advance();
            }
        }

        private static Expr.Literal CompareLiterals<T>(TokenType op, object left, object right) where T : IComparable<T>
        {
            T l = (T)left;
            T r = (T)right;

            return op switch
            {
                TokenType.LESS => new Expr.Literal(TypeEnum.BOOL, l.CompareTo(r) < 0),
                TokenType.LESS_EQUAL => new Expr.Literal(TypeEnum.BOOL, l.CompareTo(r) <= 0),
                TokenType.GREATER => new Expr.Literal(TypeEnum.BOOL, l.CompareTo(r) > 0),
                TokenType.GREATER_EQUAL => new Expr.Literal(TypeEnum.BOOL, l.CompareTo(r) >= 0),
                _ => throw new Exception()
            };
        }
    }

    class ConstantSymbol
    {
        public TypeSpecifier Type { get; set; }
        public object Value { get; set; }
    }
}

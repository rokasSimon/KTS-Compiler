using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace KTS_Compiler.CodeGeneration
{
    public class CodeGenerator : Expr.IVisitor<TypeSpecifier>, Stmt.IVisitor<Stmt>
    {
        private readonly LLVMModuleRef _module;
        private readonly LLVMBuilderRef _builder;
        private readonly Dictionary<string, LLVMSymbol> _namedValues = new Dictionary<string, LLVMSymbol>();
        private readonly Dictionary<string, FunctionSymbol> _functions = new Dictionary<string, FunctionSymbol>();
        private readonly Stack<LLVMValueRef> _valueStack = new Stack<LLVMValueRef>();
        private FunctionSymbol currentFunction;

        private readonly LLVMBool _lFalse = new LLVMBool(0);
        private readonly LLVMBool _lTrue = new LLVMBool(1);
        private readonly LLVMValueRef _lNull = new LLVMValueRef(IntPtr.Zero);

        public CodeGenerator(string sourceFile)
        {
            _module = LLVM.ModuleCreateWithName(sourceFile);
            _builder = LLVM.CreateBuilder();

            var printf = PrintfPrototype(_module);
            var gets = GetsPrototype(_module);

            _functions.Add("printf", new FunctionSymbol
            {
                ReturnType = new TypeSpecifier { Dimensions = 0, Type = TypeEnum.I32 },
                Parameters = new List<Parameter>
                {
                    new Parameter { Identifier = "printf", Reference = false, Type = new TypeSpecifier { Dimensions = 1, Type = TypeEnum.I8 }}
                },
                VarArgs = true
            });
            _functions.Add("gets", new FunctionSymbol
            {
                ReturnType = new TypeSpecifier { Dimensions = 1, Type = TypeEnum.I8 },
                Parameters = new List<Parameter>
                {
                    new Parameter { Identifier = "gets", Reference = false, Type = new TypeSpecifier { Dimensions = 1, Type = TypeEnum.I8 }}
                },
                VarArgs = false
            });
        }

        public void GenerateBitcode(List<Stmt> statements)
        {
            foreach (var funcdecl in statements)
            {
                Stmt.FunctionDeclaration func = (Stmt.FunctionDeclaration)funcdecl;

                FunctionPrototype(func);
            }

            foreach (var funcdecl in statements)
            {
                Visit(funcdecl);
            }

            LLVM.VerifyModule(_module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string error);
            if (error != null)
            {
                Console.WriteLine(error);
            }

            //LLVM.LinkInMCJIT();
            LLVM.WriteBitcodeToFile(_module, "test.bc");

            LLVM.DumpModule(_module);
            LLVM.DisposeBuilder(_builder);
        }

        private TypeSpecifier Visit(Expr expression)
        {
            return expression.Accept(this);
        }

        private Stmt Visit(Stmt statement)
        {
            return statement.Accept(this);
        }

        public TypeSpecifier VisitLiteralExpression(Expr.Literal exp)
        {
            LLVMValueRef outval;

            if (exp.Type == TypeEnum.I8)
            {
                ulong b = Convert.ToUInt64(exp.Value);
                if (b < 0)
                {
                    outval = LLVM.ConstInt(LLVM.Int8Type(), b, _lTrue);
                }
                else
                {
                    outval = LLVM.ConstInt(LLVM.Int8Type(), b, _lFalse);
                }
            }
            else if (exp.Type == TypeEnum.I16)
            {
                ulong b = Convert.ToUInt64(exp.Value);
                if (b < 0)
                {
                    outval = LLVM.ConstInt(LLVM.Int16Type(), b, _lTrue);
                }
                else
                {
                    outval = LLVM.ConstInt(LLVM.Int16Type(), b, _lFalse);
                }
            }
            else if (exp.Type == TypeEnum.I32)
            {
                ulong b = Convert.ToUInt64(exp.Value);
                if (b < 0)
                {
                    outval = LLVM.ConstInt(LLVM.Int32Type(), b, _lTrue);
                }
                else
                {
                    outval = LLVM.ConstInt(LLVM.Int32Type(), b, _lFalse);
                }
            }
            else if (exp.Type == TypeEnum.I64)
            {
                ulong b = Convert.ToUInt64(exp.Value);
                if (b < 0)
                {
                    outval = LLVM.ConstInt(LLVM.Int64Type(), b, _lTrue);
                }
                else
                {
                    outval = LLVM.ConstInt(LLVM.Int64Type(), b, _lFalse);
                }
            }
            else if (exp.Type == TypeEnum.F32)
            {
                double d = Convert.ToDouble(exp.Value);
                outval = LLVM.ConstReal(LLVM.FloatType(), d);
            }
            else if (exp.Type == TypeEnum.F64)
            {
                double d = Convert.ToDouble(exp.Value);
                outval = LLVM.ConstReal(LLVM.DoubleType(), d);
            }
            else if (exp.Type == TypeEnum.BOOL)
            {
                bool b = Convert.ToBoolean(exp.Value);
                if (b)
                {
                    outval = LLVM.ConstInt(LLVM.Int1Type(), 1, _lFalse);
                }
                else
                {
                    outval = LLVM.ConstInt(LLVM.Int1Type(), 0, _lTrue);
                }
            }
            else if (exp.Type == TypeEnum.STRING)
            {
                string str = exp.Value.ToString();

                outval = LLVM.BuildGlobalStringPtr(_builder, str, "");
                //outval = LLVM.ConstString(str, (uint)(str.Length + 1), _lFalse);
            }
            else
            {
                throw new Exception("Unknown literal");
            }

            _valueStack.Push(outval);
            return new TypeSpecifier { Type = exp.Type, Dimensions = 0 };
        }

        public TypeSpecifier VisitVariableExpression(Expr.Variable exp)
        {
            if (_namedValues.ContainsKey(exp.Name.Source))
            {
                //_valueStack.Push(_namedValues[exp.Name.Source].Value);
                var variable = _namedValues[exp.Name.Source];
                var value = variable.Value;
                var loadinst = LLVM.BuildLoad(_builder, value, exp.Name.Source);
                _valueStack.Push(loadinst);

                if (variable.Binding != null)
                {
                    HandleBinding(variable, true);
                }
            }
            else
            {
                throw new Exception("Unknown variable name");
            }

            return _namedValues[exp.Name.Source].KtsType;
        }

        public TypeSpecifier VisitBinaryExpression(Expr.Binary exp)
        {
            TypeSpecifier leftType = Visit(exp.Left);
            TypeSpecifier rightType = Visit(exp.Right);

            LLVMValueRef r = _valueStack.Pop();
            LLVMValueRef l = _valueStack.Pop();

            LLVMValueRef outval;
            TypeSpecifier rettype;

            LLVMValueRef casted = CheckedCast(r, rightType, leftType);

            switch (exp.Operator.Type)
            {
                case TokenType.PLUS:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildAdd(_builder, l, casted, "addtmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFAdd(_builder, l, casted, "addftmp");
                    }
                    rettype = leftType;
                    break;
                case TokenType.MINUS:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildSub(_builder, l, casted, "subtmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFSub(_builder, l, casted, "subftmp");
                    }
                    rettype = leftType;
                    break;
                case TokenType.MULT:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildMul(_builder, l, casted, "multmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFMul(_builder, l, casted, "mulftmp");
                    }
                    rettype = leftType;
                    break;
                case TokenType.DIV:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildSDiv(_builder, l, casted, "divtmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFDiv(_builder, l, casted, "divftmp");
                    }
                    rettype = leftType;
                    break;
                case TokenType.LESS:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntSLT, l, casted, "cmpiltmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealOLT, l, casted, "cmpfltmp");
                    }
                    rettype = new TypeSpecifier { Type = TypeEnum.BOOL, Dimensions = 0 };
                    break;
                case TokenType.LESS_EQUAL:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntSLE, l, casted, "cmpiletmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealOLE, l, casted, "cmpfletmp");
                    }
                    rettype = new TypeSpecifier { Type = TypeEnum.BOOL, Dimensions = 0 };
                    break;
                case TokenType.GREATER:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntSGT, l, casted, "cmpigtmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealOGT, l, casted, "cmpfgtmp");
                    }
                    rettype = new TypeSpecifier { Type = TypeEnum.BOOL, Dimensions = 0 };
                    break;
                case TokenType.GREATER_EQUAL:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntSGE, l, casted, "cmpigetmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealOGE, l, casted, "cmpfgetmp");
                    }
                    rettype = new TypeSpecifier { Type = TypeEnum.BOOL, Dimensions = 0 };
                    break;
                case TokenType.EQUAL:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntEQ, l, casted, "cmpieqtmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealOEQ, l, casted, "cmpfeqtmp");
                    }
                    rettype = new TypeSpecifier { Type = TypeEnum.BOOL, Dimensions = 0 };
                    break;
                case TokenType.NOT_EQUAL:
                    if (leftType.IsInt())
                    {
                        outval = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntNE, l, casted, "cmpinetmp");
                    }
                    else
                    {
                        outval = LLVM.BuildFCmp(_builder, LLVMRealPredicate.LLVMRealONE, l, casted, "cmpfnetmp");
                    }
                    rettype = new TypeSpecifier { Type = TypeEnum.BOOL, Dimensions = 0 };
                    break;
                default: throw new Exception("No binary op matched");
            }

            _valueStack.Push(outval);
            return rettype;
        }

        public TypeSpecifier VisitInvokeExpression(Expr.Invoke exp)
        {
            LLVMValueRef callfunc = LLVM.GetNamedFunction(_module, exp.Identifier.Source);
            if (callfunc.Pointer == IntPtr.Zero)
            {
                throw new Exception("Unknown function referenced");
            }

            uint argc = (uint)exp.Arguments.Count;
            var argv = new LLVMValueRef[argc];

            for (int i = 0; i < argc; i++)
            {
                if (exp.Arguments[i] is Expr.Variable var)
                {
                    var name = var.Name.Source;
                    var variable = _namedValues[name];
                    var argtype = variable.KtsType;

                    if (argtype.Dimensions > 0)
                    {
                        LLVMValueRef[] indices = new LLVMValueRef[2];
                        indices[0] = LLVM.ConstInt(LLVM.Int64Type(), 0, _lFalse);
                        indices[1] = LLVM.ConstInt(LLVM.Int64Type(), 0, _lFalse);

                        var address = LLVM.BuildInBoundsGEP(_builder, variable.Value, indices, "");
                        argv[i] = address;

                        if (variable.Binding != null)
                        {
                            HandleBinding(variable, true);
                        }
                    }
                    else
                    {
                        Visit(exp.Arguments[i]);
                        argv[i] = _valueStack.Pop();
                    }
                }
                else
                {
                    TypeSpecifier type = Visit(exp.Arguments[i]);
                    argv[i] = _valueStack.Pop();
                }
            }

            _valueStack.Push(LLVM.BuildCall(_builder, callfunc, argv, ""));

            var funcSignature = _functions[exp.Identifier.Source];

            return funcSignature.ReturnType;
        }

        public void FunctionPrototype(Stmt.FunctionDeclaration func)
        {
            uint argc = (uint)func.Parameters.Count;
            var argv = new LLVMTypeRef[argc];

            var function = LLVM.GetNamedFunction(_module, func.Identifier.Source);

            if (function.Pointer != IntPtr.Zero)
            {
                if (LLVM.CountBasicBlocks(function) != 0)
                {
                    throw new Exception("redefinition of function.");
                }

                if (LLVM.CountParams(function) != argc)
                {
                    throw new Exception("redefinition of function with different # args");
                }
            }
            else
            {
                for (int i = 0; i < argc; i++)
                {
                    argv[i] = GetParameterType(func.Parameters[i].Type);
                }

                function = LLVM.AddFunction(_module, func.Identifier.Source, LLVM.FunctionType(GetParameterType(func.ReturnType), argv, false));
                LLVM.SetLinkage(function, LLVMLinkage.LLVMExternalLinkage);
            }

            for (int i = 0; i < argc; i++)
            {
                string argName = func.Parameters[i].Identifier;

                LLVMValueRef param = LLVM.GetParam(function, (uint)i);
                LLVM.SetValueName(param, argName);

                //_namedValues[argName] = new LLVMSymbol { Binding = null, IsFunction = false, KtsType = stmt.Parameters[i].Type, Value = param };
            }

            _functions.Add(func.Identifier.Source, new FunctionSymbol { ReturnType = func.ReturnType, Parameters = func.Parameters });
        }

        public Stmt VisitFunctionDeclaration(Stmt.FunctionDeclaration stmt)
        {
            currentFunction = new FunctionSymbol { Parameters = stmt.Parameters, ReturnType = stmt.ReturnType };

            uint argc = (uint)stmt.Parameters.Count;
            //var argv = new LLVMTypeRef[argc];
            var function = LLVM.GetNamedFunction(_module, stmt.Identifier.Source);

            if (function.Pointer != IntPtr.Zero)
            {
                if (LLVM.CountBasicBlocks(function) != 0)
                {
                    throw new Exception("redefinition of function.");
                }

                if (LLVM.CountParams(function) != argc)
                {
                    throw new Exception("redefinition of function with different # args");
                }
            }

            #region FunctionBody

            _namedValues.Clear();
            LLVM.PositionBuilderAtEnd(_builder, LLVM.AppendBasicBlock(function, "entry"));
            var args = LLVM.GetParams(function);
            for (int i = 0; i < args.Length; i++)
            {
                var name = stmt.Parameters[i].Identifier;
                var type = stmt.Parameters[i].Type;

                var alloca = EntryAllocation(function, type, name);
                LLVM.BuildStore(_builder, args[i], alloca);
                bool decayed = false;

                if (type.Dimensions != 0)
                {
                    decayed = true;
                }

                _namedValues.Add(name, new LLVMSymbol { Binding = null, IsDecayed = decayed, KtsType = type, Value = alloca });
            }

            try
            {
                Visit(stmt.FunctionBody);

                if (stmt.ReturnType.Type == TypeEnum.VOID)
                {
                    Stmt.BlockStatement block = (Stmt.BlockStatement)stmt.FunctionBody;

                    var length = block.Statements.Count;
                    if (block.Statements[length - 1] is not Stmt.Return)
                    {
                        LLVM.BuildRetVoid(_builder);
                    }
                }
            }
            catch
            {
                LLVM.DeleteFunction(function);
                throw;
            }

            LLVM.VerifyFunction(function, LLVMVerifierFailureAction.LLVMPrintMessageAction);
            _valueStack.Push(function);

            #endregion

            return stmt;
        }

        public TypeSpecifier VisitGroupingExpression(Expr.Grouping exp)
        {
            return Visit(exp.Expression);
        }

        public TypeSpecifier VisitUnaryExpression(Expr.Unary exp)
        {
            var type = Visit(exp.Right);
            var val = _valueStack.Pop();

            switch (exp.Operator.Type)
            {
                case TokenType.MINUS:
                    if (type.IsInt())
                    {
                        _valueStack.Push(LLVM.BuildNeg(_builder, val, "negitmp"));
                    }
                    else
                    {
                        _valueStack.Push(LLVM.BuildFNeg(_builder, val, "negftmp"));
                    }
                    return type;
                case TokenType.NOT:
                    _valueStack.Push(LLVM.BuildNot(_builder, val, "nottmp"));
                    return type;
                default: throw new Exception("In unary");
            }
        }

        public TypeSpecifier VisitLogicalExpression(Expr.Logical exp)
        {
            var ltype = Visit(exp.Left);
            var rtype = Visit(exp.Right);

            var rvalue = _valueStack.Pop();
            var lvalue = _valueStack.Pop();

            switch (exp.Operator.Type)
            {
                case TokenType.AND:
                    _valueStack.Push(LLVM.BuildAnd(_builder, lvalue, rvalue, "andtmp"));
                    return ltype;
                case TokenType.OR:
                    _valueStack.Push(LLVM.BuildOr(_builder, lvalue, rvalue, "ortmp"));
                    return rtype;
                default: throw new Exception("In logical");
            }
        }

        public TypeSpecifier VisitCastExpression(Expr.Cast exp)
        {
            var type = Visit(exp.Expression);
            var target = exp.TargetType;
            var value = _valueStack.Pop();

            _valueStack.Push(CheckedCast(value, type, target));
            return target;
        }

        public TypeSpecifier VisitIndexAccessExpression(Expr.IndexAccess exp)
        {
            var name = exp.Identifier.Source;
            var variable = _namedValues[name];
            var vartype = new TypeSpecifier { Dimensions = 0, Type = variable.KtsType.Type };

            if (variable.IsDecayed)
            {
                LLVMValueRef[] indices = new LLVMValueRef[exp.Expressions.Count];

                for (int i = 0; i < indices.Length; i++)
                {
                    var exprType = Visit(exp.Expressions[i]);
                    var exprVal = _valueStack.Pop();

                    var casted = CheckedCast(exprVal, exprType, new TypeSpecifier { Dimensions = 0, Type = TypeEnum.I64 });
                    indices[i] = casted;
                }

                var load = LLVM.BuildLoad(_builder, variable.Value, "");
                var address = LLVM.BuildInBoundsGEP(_builder, load, indices, "");
                var loadsec = LLVM.BuildLoad(_builder, address, "");
                _valueStack.Push(loadsec);

                return vartype;
            }
            else
            {
                LLVMValueRef[] indices = new LLVMValueRef[exp.Expressions.Count + 1];

                indices[0] = LLVM.ConstInt(LLVM.Int64Type(), 0, _lFalse);

                for (int i = 1; i < indices.Length; i++)
                {
                    var exprType = Visit(exp.Expressions[i - 1]);
                    var exprVal = _valueStack.Pop();

                    var casted = CheckedCast(exprVal, exprType, new TypeSpecifier { Dimensions = 0, Type = TypeEnum.I64 });
                    indices[i] = casted;
                }

                var address = LLVM.BuildInBoundsGEP(_builder, variable.Value, indices, "");
                var load = LLVM.BuildLoad(_builder, address, "");
                _valueStack.Push(load);

                if (variable.Binding != null)
                {
                    HandleBinding(variable, true);
                }

                return vartype;
            }

            /*LLVMValueRef[] indices = new LLVMValueRef[exp.Expressions.Count + 1];

            indices[0] = LLVM.ConstInt(LLVM.Int64Type(), 0, _lFalse);

            for (int i = 1; i < indices.Length; i++)
            {
                var exprType = Visit(exp.Expressions[i - 1]);
                var exprVal = _valueStack.Pop();

                var casted = CheckedCast(exprVal, exprType, new TypeSpecifier { Dimensions = 0, Type = TypeEnum.I64 });
                indices[i] = casted;
            }

            var address = LLVM.BuildInBoundsGEP(_builder, variable.Value, indices, "");
            var load = LLVM.BuildLoad(_builder, address, "");
            _valueStack.Push(load);

            return vartype;*/
        }

        public TypeSpecifier VisitRangeExpression(Expr.Range exp)
        {
            var left = Visit(exp.Start);
            var lvalue = _valueStack.Pop();

            var right = Visit(exp.End);
            var rvalue = _valueStack.Pop();

            if (exp.InclusiveStart)
            {
                _valueStack.Push(lvalue);
            }
            else
            {
                var plusone = LLVM.BuildAdd(_builder, lvalue, LLVM.ConstInt(LLVMPrimitiveType(left.Type), 1, _lFalse), "exclstart");
                _valueStack.Push(plusone);
            }

            if (exp.InclusiveEnd)
            {
                _valueStack.Push(CheckedCast(rvalue, right, left));
            }
            else
            {
                var casted = CheckedCast(rvalue, right, left);
                var minusone = LLVM.BuildSub(_builder, casted, LLVM.ConstInt(LLVMPrimitiveType(left.Type), 1, _lFalse), "exclend");
                _valueStack.Push(minusone);
            }

            return left;
        }

        public Stmt VisitBlockStatement(Stmt.BlockStatement statement)
        {
            foreach (var item in statement.Statements)
            {
                Visit(item);
            }

            return null;
        }

        public Stmt VisitWhileStatement(Stmt.While statement)
        {
            var function = LLVM.GetInsertBlock(_builder).GetBasicBlockParent();
            var loop = LLVM.AppendBasicBlock(function, "whileloop");
            LLVM.BuildBr(_builder, loop);
            LLVM.PositionBuilderAtEnd(_builder, loop);

            Visit(statement.Body);
            var condType = Visit(statement.Condition);
            var condValue = _valueStack.Pop();
            var endCondition = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntNE, condValue, LLVM.ConstInt(LLVMPrimitiveType(condType.Type), 0, _lFalse), "endcond");

            var afterLoop = LLVM.AppendBasicBlock(function, "afterloop");
            LLVM.BuildCondBr(_builder, endCondition, loop, afterLoop);
            LLVM.PositionBuilderAtEnd(_builder, afterLoop);

            return null;
        }

        public Stmt VisitForStatement(Stmt.For statement)
        {
            var function = LLVM.GetInsertBlock(_builder).GetBasicBlockParent();
            var alloca = EntryAllocation(function, statement.TypeSpecifier, statement.Identifier.Source);

            var stepType = Visit(statement.RangeExpression);
            var endn = _valueStack.Pop();
            var startn = _valueStack.Pop();
            var end = CheckedCast(endn, stepType, statement.TypeSpecifier);
            var start = CheckedCast(startn, stepType, statement.TypeSpecifier);

            LLVM.BuildStore(_builder, start, alloca);
            var loop = LLVM.AppendBasicBlock(function, "forloop");
            LLVM.BuildBr(_builder, loop);
            LLVM.PositionBuilderAtEnd(_builder, loop);

            if (_namedValues.ContainsKey(statement.Identifier.Source))
            {
                throw new Exception("For loop variable already defined");
            }
            else
            {
                _namedValues.Add(statement.Identifier.Source, new LLVMSymbol { KtsType = statement.TypeSpecifier, Value = alloca });
            }

            Visit(statement.Body);
            
            var stepVal = LLVM.ConstInt(LLVMPrimitiveType(statement.TypeSpecifier.Type), 1, _lFalse);

            var curVal = LLVM.BuildLoad(_builder, alloca, statement.Identifier.Source);
            var nextVal = LLVM.BuildAdd(_builder, curVal, stepVal, "nextvar");
            LLVM.BuildStore(_builder, nextVal, alloca);

            var endCondition = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntSLT, curVal, end, "loopcond");
            var after = LLVM.AppendBasicBlock(function, "afterloop");
            LLVM.BuildCondBr(_builder, endCondition, loop, after);

            LLVM.PositionBuilderAtEnd(_builder, after);

            return null;
        }

        public Stmt VisitDeclarationStatement(Stmt.VariableDeclaration statement)
        {
            var function = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(_builder));
            var type = statement.TypeSpecifier;
            var name = statement.Identifier.Source;

            if (_namedValues.ContainsKey(name))
            {
                throw new Exception("Variable name already declared");
            }

            LLVMValueRef alloca;

            if (type.Dimensions == 0)
            {
                alloca = EntryAllocation(function, type, name);
            }
            else
            {
                Expr.ArrayInitializer init = (Expr.ArrayInitializer)statement.Initializer;

                alloca = AllocateArray(function, type, name, init.DimensionSizes);
            }

            _namedValues.Add(name, new LLVMSymbol { Binding = statement.Binding, IsDecayed = false, KtsType = type, Value = alloca });

            if (statement.Initializer == null)
            {
                if (type.Dimensions == 0)
                {
                    if (type.IsFloat())
                    {
                        LLVM.BuildStore(_builder, LLVM.ConstReal(LLVMPrimitiveType(type.Type), 0), alloca);
                    }
                    else
                    {
                        LLVM.BuildStore(_builder, LLVM.ConstInt(LLVMPrimitiveType(type.Type), 0, _lFalse), alloca);
                    }
                }
                else
                {
                    throw new Exception("Uninitialized array");
                }
            }
            else
            {
                if (type.Dimensions == 0)
                {
                    var initType = Visit(statement.Initializer);
                    var initValue = _valueStack.Pop();

                    var casted = CheckedCast(initValue, initType, type);
                    LLVM.BuildStore(_builder, casted, alloca);
                }
            }

            return null;
        }

        public TypeSpecifier VisitAssignmentExpression(Expr.Assign exp)
        {
            if (exp.Var is Expr.Variable exprname)
            {
                var name = exprname.Name.Source;
                var variable = _namedValues[name];

                var type = Visit(exp.Value);
                var value = _valueStack.Pop();

                var casted = CheckedCast(value, type, variable.KtsType);

                var store = LLVM.BuildStore(_builder, casted, variable.Value);
                _valueStack.Push(store);

                if (variable.Binding != null)
                {
                    HandleBinding(variable, false);
                }

                return variable.KtsType;
            }
            else if (exp.Var is Expr.IndexAccess exprindex)
            {
                var name = exprindex.Identifier.Source;
                var variable = _namedValues[name];
                var vartype = new TypeSpecifier { Dimensions = 0, Type = variable.KtsType.Type };

                if (variable.IsDecayed)
                {
                    LLVMValueRef[] indices = new LLVMValueRef[exprindex.Expressions.Count];

                    for (int i = 0; i < indices.Length; i++)
                    {
                        var indexType = Visit(exprindex.Expressions[i]);
                        var indexVal = _valueStack.Pop();

                        var integerIndex = CheckedCast(indexVal, indexType, new TypeSpecifier { Dimensions = 0, Type = TypeEnum.I64 });
                        indices[i] = integerIndex;
                    }

                    var load = LLVM.BuildLoad(_builder, variable.Value, "");
                    var address = LLVM.BuildInBoundsGEP(_builder, load, indices, "");

                    var exprType = Visit(exp.Value);
                    var exprVal = _valueStack.Pop();

                    var casted = CheckedCast(exprVal, exprType, vartype);

                    LLVMValueRef store = LLVM.BuildStore(_builder, casted, address);
                    _valueStack.Push(store);

                    return vartype;
                }
                else
                {
                    LLVMValueRef[] indices = new LLVMValueRef[exprindex.Expressions.Count + 1];

                    indices[0] = LLVM.ConstInt(LLVM.Int64Type(), 0, _lFalse);

                    for (int i = 1; i < indices.Length; i++)
                    {
                        var indexType = Visit(exprindex.Expressions[i - 1]);
                        var indexVal = _valueStack.Pop();

                        var integerIndex = CheckedCast(indexVal, indexType, new TypeSpecifier { Dimensions = 0, Type = TypeEnum.I64 });
                        indices[i] = integerIndex;
                    }

                    var address = LLVM.BuildInBoundsGEP(_builder, variable.Value, indices, "");

                    var exprType = Visit(exp.Value);
                    var exprVal = _valueStack.Pop();

                    var casted = CheckedCast(exprVal, exprType, vartype);

                    LLVMValueRef store = LLVM.BuildStore(_builder, casted, address);
                    _valueStack.Push(store);

                    if (variable.Binding != null)
                    {
                        HandleBinding(variable, false);
                    }

                    return vartype;
                }
            }
            else
            {
                throw new Exception("Should be either index access or primitive assignment");
            }
        }

        public TypeSpecifier VisitArrayInitializer(Expr.ArrayInitializer exp)
        {
            throw new NotImplementedException();
        }

        public Stmt VisitReturnStatement(Stmt.Return statement)
        {
            if (currentFunction.ReturnType.Type == TypeEnum.VOID)
            {
                LLVM.BuildRetVoid(_builder);
            }
            else
            {
                var type = Visit(statement.Value);
                var value = _valueStack.Pop();

                var casted = CheckedCast(value, type, currentFunction.ReturnType);
                LLVM.BuildRet(_builder, casted);
            }

            return null;
        }

        public Stmt VisitExpressionStatement(Stmt.ExpressionStatement statement)
        {
            Visit(statement.Expression);
            _valueStack.Pop();

            return null;
        }

        public Stmt VisitIfStatement(Stmt.If statement)
        {
            Visit(statement.Condition);
            var conditionValue = _valueStack.Pop();
            var condition = LLVM.BuildICmp(_builder, LLVMIntPredicate.LLVMIntNE, conditionValue, LLVM.ConstInt(LLVM.Int1Type(), 0, _lFalse), "cond");

            LLVMValueRef func = LLVM.GetBasicBlockParent(LLVM.GetInsertBlock(_builder));

            LLVMBasicBlockRef thenb = LLVM.AppendBasicBlock(func, "then");
            //LLVMBasicBlockRef elseb = LLVM.AppendBasicBlock(func, "else");
            LLVMBasicBlockRef merge = LLVM.AppendBasicBlock(func, "merge");

            if (statement.ElseBlock != null)
            {
                LLVMBasicBlockRef elseb = LLVM.AppendBasicBlock(func, "else");

                LLVM.BuildCondBr(_builder, condition, thenb, elseb);
                LLVM.PositionBuilderAtEnd(_builder, thenb);

                Visit(statement.MainBlock);

                LLVM.BuildBr(_builder, merge);
                thenb = LLVM.GetInsertBlock(_builder);

                LLVM.PositionBuilderAtEnd(_builder, elseb);

                Visit(statement.ElseBlock);

                LLVM.BuildBr(_builder, merge);
                elseb = LLVM.GetInsertBlock(_builder);

                LLVM.PositionBuilderAtEnd(_builder, merge);

                return null;
            }
            else
            {
                LLVM.BuildCondBr(_builder, condition, thenb, merge);
                LLVM.PositionBuilderAtEnd(_builder, thenb);

                Visit(statement.MainBlock);

                LLVM.BuildBr(_builder, merge);
                thenb = LLVM.GetInsertBlock(_builder);

                //LLVM.PositionBuilderAtEnd(_builder, elseb);

                //Visit(statement.ElseBlock);

                //LLVM.BuildBr(_builder, merge);
                //elseb = LLVM.GetInsertBlock(_builder);

                LLVM.PositionBuilderAtEnd(_builder, merge);

                return null;
            }

            /*LLVM.BuildCondBr(_builder, condition, thenb, elseb);
            LLVM.PositionBuilderAtEnd(_builder, thenb);

            Visit(statement.MainBlock);

            LLVM.BuildBr(_builder, merge);
            thenb = LLVM.GetInsertBlock(_builder);

            LLVM.PositionBuilderAtEnd(_builder, elseb);

            Visit(statement.ElseBlock);

            LLVM.BuildBr(_builder, merge);
            elseb = LLVM.GetInsertBlock(_builder);

            LLVM.PositionBuilderAtEnd(_builder, merge);

            return null;*/
        }

        private void HandleBinding(LLVMSymbol var, bool fromLoad)
        {
            if (fromLoad && var.Binding.Type.Type == TokenType.READ || // read
                !fromLoad && var.Binding.Type.Type == TokenType.WRITE || // write
                var.Binding.Type.Type == TokenType.ANY) // any
            {
                for (int i = 0; i < var.Binding.Identifiers.Count; i++)
                {
                    string funcName = var.Binding.Identifiers[i];
                    LLVMValueRef func = LLVM.GetNamedFunction(_module, funcName);
                    LLVMValueRef[] param = func.GetParams();

                    if (param.Length == 0)
                    {
                        LLVM.BuildCall(_builder, func, Array.Empty<LLVMValueRef>(), "");
                        return;
                    }
                    else if (param.Length == 1)
                    {
                        LLVMValueRef[] args = new LLVMValueRef[1];

                        if (var.KtsType.Dimensions > 0)
                        {
                            LLVMValueRef[] indices = new LLVMValueRef[2];
                            indices[0] = LLVM.ConstInt(LLVM.Int64Type(), 0, _lFalse);
                            indices[1] = LLVM.ConstInt(LLVM.Int64Type(), 0, _lFalse);

                            var address = LLVM.BuildInBoundsGEP(_builder, var.Value, indices, "");
                            args[0] = address;
                        }
                        else
                        {
                            args[0] = LLVM.BuildLoad(_builder, var.Value, "");
                        }

                        LLVM.BuildCall(_builder, func, args, "");
                    }
                    else
                    {
                        throw new Exception("Too many parameters in binding function");
                    }
                }
            }
        }

        private LLVMValueRef EntryAllocation(LLVMValueRef function, TypeSpecifier type, string name)
        {
            var entryBlock = function.GetEntryBasicBlock();
            var firstInstruction = entryBlock.GetFirstInstruction();

            if (firstInstruction.Pointer == IntPtr.Zero)
            {
                LLVM.PositionBuilderAtEnd(_builder, function.GetLastBasicBlock());
                var alloca = LLVM.BuildAlloca(_builder, GetParameterType(type), name);
                return alloca;
            }
            else
            {
                LLVM.PositionBuilderBefore(_builder, function.GetEntryBasicBlock().GetFirstInstruction());
                var alloca = LLVM.BuildAlloca(_builder, GetParameterType(type), name);
                LLVM.PositionBuilderAtEnd(_builder, function.GetLastBasicBlock());
                return alloca;
            }
        }

        private LLVMValueRef AllocateArray(LLVMValueRef function, TypeSpecifier type, string name, List<Expr> dimensions)
        {
            var entryBlock = function.GetEntryBasicBlock();
            var firstInstruction = entryBlock.GetFirstInstruction();

            LLVMTypeRef arrtype = LLVMPrimitiveType(type.Type);

            for (int i = dimensions.Count - 1; i >= 0; i--)
            {
                Expr.Literal lit = (Expr.Literal)dimensions[i];
                //long l = Convert.ToInt64(lit.Value);
                uint size = Convert.ToUInt32(lit.Value);

                arrtype = LLVM.ArrayType(arrtype, size);
            }

            if (firstInstruction.Pointer == IntPtr.Zero)
            {
                LLVM.PositionBuilderAtEnd(_builder, function.GetLastBasicBlock());
                var arrayAlloc = LLVM.BuildAlloca(_builder, arrtype, name);
                return arrayAlloc;
            }
            else
            {
                LLVM.PositionBuilderBefore(_builder, firstInstruction);
                var arrayAlloc = LLVM.BuildAlloca(_builder, arrtype, name);
                LLVM.PositionBuilderAtEnd(_builder, function.GetLastBasicBlock());
                return arrayAlloc;
            }
        }

        private LLVMValueRef CheckedCast(LLVMValueRef value, TypeSpecifier type, TypeSpecifier target)
        {
            if (type == target)
            {
                return value;
            }

            if (target.Dimensions == 0)
            {
                if (type.IsInt())
                {
                    if (target.IsFloat())
                    {
                        return IFPCast(value, target);
                    }
                    else if (Lesser(type, target))
                    {
                        return IntUpcast(value, target);
                    }
                    else
                    {
                        return IntDowncast(value, target);
                    }
                }
                
                if (type.IsFloat())
                {
                    if (target.IsInt())
                    {
                        return FPICast(value, target);
                    }
                    else if (Lesser(type, target))
                    {
                        return FloatUpcast(value, target);
                    }
                    else
                    {
                        return FloatDowncast(value, target);
                    }
                }

                throw new Exception("Didn't cast?");
            }
            else
            {
                throw new Exception("Implicit cast on arrays not allowed");
            }
        }

        private LLVMValueRef IntDowncast(LLVMValueRef value, TypeSpecifier target)
        {
            return LLVM.BuildTrunc(_builder, value, LLVMPrimitiveType(target.Type), "");
        }

        private LLVMValueRef FloatDowncast(LLVMValueRef value, TypeSpecifier target)
        {
            return LLVM.BuildFPTrunc(_builder, value, LLVMPrimitiveType(target.Type), "");
        }

        private LLVMValueRef IntUpcast(LLVMValueRef value, TypeSpecifier target)
        {
            return LLVM.BuildSExt(_builder, value, LLVMPrimitiveType(target.Type), "");
        }

        private LLVMValueRef FloatUpcast(LLVMValueRef value, TypeSpecifier target)
        {
            return LLVM.BuildFPExt(_builder, value, LLVMPrimitiveType(target.Type), "");
        }

        private LLVMValueRef IFPCast(LLVMValueRef value, TypeSpecifier target)
        {
            return LLVM.BuildSIToFP(_builder, value, LLVMPrimitiveType(target.Type), "");
        }

        private LLVMValueRef FPICast(LLVMValueRef value, TypeSpecifier target)
        {
            return LLVM.BuildFPToSI(_builder, value, LLVMPrimitiveType(target.Type), "");
        }

        private static LLVMTypeRef LLVMPrimitiveType(TypeEnum type) => type switch
        {
                TypeEnum.I8 => LLVM.Int8Type(),
                TypeEnum.I16 => LLVM.Int16Type(),
                TypeEnum.I32 => LLVM.Int32Type(),
                TypeEnum.I64 => LLVM.Int64Type(),
                TypeEnum.F32 => LLVM.FloatType(),
                TypeEnum.F64 => LLVM.DoubleType(),
                TypeEnum.BOOL => LLVM.Int1Type(),
                _ => throw new Exception("Bad type exchange")
        };

        private static LLVMTypeRef GetParameterType(TypeSpecifier kts)
        {
            if (kts.Type == TypeEnum.VOID)
            {
                return LLVM.VoidType();
            }

            if (kts.Dimensions == 0)
            {
                return LLVMPrimitiveType(kts.Type);
            }
            else
            {
                return LLVMArrayPtr(kts);
            }
        }

        private static LLVMTypeRef LLVMArrayPtr(TypeSpecifier type)
        {
            LLVMTypeRef curType = LLVM.PointerType(LLVMPrimitiveType(type.Type), 0u);

            for (int i = 1; i < type.Dimensions; i++)
            {
                curType = LLVM.PointerType(curType, 0u);
            }

            return curType;
        }

        private static bool Lesser(TypeSpecifier left, TypeSpecifier right)
        {
            return left.Type.CompareTo(right.Type) < 0;
        }

        private static bool Greater(TypeSpecifier left, TypeSpecifier right)
        {
            return left.Type.CompareTo(right.Type) > 0;
        }

        private static LLVMValueRef PrintfPrototype(LLVMModuleRef module)
        {
            var print_type = LLVM.FunctionType(LLVM.Int32Type(), new[] { LLVM.PointerType(LLVM.Int8Type(), 0u) }, true);
            var printf = LLVM.AddFunction(module, "printf", print_type);

            return printf;
        }

        private static LLVMValueRef MainPrototype(LLVMModuleRef module)
        {
            var main_type = LLVM.FunctionType(LLVM.Int32Type(), Array.Empty<LLVMTypeRef>(), false);
            var main = LLVM.AddFunction(module, "main", main_type);

            return main;
        }

        private static LLVMValueRef GetsPrototype(LLVMModuleRef module)
        {
            var gets_type = LLVM.FunctionType(LLVM.PointerType(LLVM.Int8Type(), 0u), new[] { LLVM.PointerType(LLVM.Int8Type(), 0u) }, false);
            var gets = LLVM.AddFunction(module, "gets", gets_type);

            return gets;
        }
    }
}

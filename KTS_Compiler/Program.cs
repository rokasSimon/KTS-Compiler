using System;
using LLVMSharp;

namespace KTS_Compiler
{
    class Program
    {
        private static LLVMValueRef PrintfPrototype(LLVMModuleRef module)
        {
            var print_type = LLVM.FunctionType(LLVM.Int32Type(), new[] { LLVM.PointerType(LLVM.Int8Type(), 0u) }, false);
            var printf = LLVM.AddFunction(module, "printf", print_type);

            return printf;
        }

        private static LLVMValueRef MainPrototype(LLVMModuleRef module)
        {
            var main_type = LLVM.FunctionType(LLVM.Int32Type(), Array.Empty<LLVMTypeRef>(), false);
            var main = LLVM.AddFunction(module, "main", main_type);

            return main;
        }

        static void Main(string[] args)
        {
            /*var module = LLVM.ModuleCreateWithName("example");
            var builder = LLVM.CreateBuilder();
            var printf = PrintfPrototype(module);
            var main = MainPrototype(module);

            var block = LLVM.AppendBasicBlock(main, "main");
            LLVM.PositionBuilderAtEnd(builder, block);
            //LLVM.InsertIntoBuilder(builder, block);
            var hello = LLVM.BuildGlobalStringPtr(builder, "Hello World!\n", "");
            LLVM.BuildCall(builder, printf, new[] { hello }, "");
            var zero = LLVM.ConstInt(LLVM.Int32Type(), 0, new LLVMBool(0));
            var ret = LLVM.BuildRet(builder, zero);

            LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string error);
            if (error != null)
            {
                Console.WriteLine(error);
            }

            LLVM.LinkInMCJIT();
            LLVM.WriteBitcodeToFile(module, "test.bc");

            LLVM.DumpModule(module);
            LLVM.DisposeBuilder(builder);*/

            KTUSharpScanner scanner = new KTUSharpScanner("test.kts");
            var tokens = scanner.ScanTokens();
            KTUSharpParser parser = new KTUSharpParser(tokens);
            var statements = parser.ParseTokens();
            ASTTypeChecker typeChecker = new ASTTypeChecker(statements);
            typeChecker.ExecuteTypeCheck();

            if (typeChecker.Failed) return;
        }
    }
}

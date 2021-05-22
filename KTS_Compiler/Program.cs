using System;
using System.Diagnostics;
using LLVMSharp;

namespace KTS_Compiler
{
    class Program
    {

        // clang -o <exe name> <bitcode file> / generates executable
        static void Main(string[] args)
        {
            /*var module = LLVM.ModuleCreateWithName("example");
            var builder = LLVM.CreateBuilder();
            var printf = PrintfPrototype(module);
            var main = MainPrototype(module);
            var gets = GetsPrototype(module);

            var block = LLVM.AppendBasicBlock(main, "main");
            LLVM.PositionBuilderAtEnd(builder, block);
            //LLVM.InsertIntoBuilder(builder, block);
            //var hello = LLVM.BuildGlobalStringPtr(builder, "Hello World!\n", "");
            var readline = LLVM.BuildArrayMalloc(builder, LLVM.Int8Type(), LLVM.ConstInt(LLVM.Int32Type(), 10, new LLVMBool(0)), "");
            LLVM.BuildCall(builder, gets, new[] { readline }, "");
            LLVM.BuildCall(builder, printf, new[] { readline }, "");
            LLVM.BuildFree(builder, readline);
            var zero = LLVM.ConstInt(LLVM.Int32Type(), 5, new LLVMBool(1));
            

            LLVM.PositionBuilderBefore(builder, main.GetEntryBasicBlock().GetFirstInstruction());
            var intalloc = LLVM.BuildAlloca(builder, LLVM.Int32Type(), "theallocated");

            LLVM.PositionBuilderAtEnd(builder, main.GetLastBasicBlock());
            var ret = LLVM.BuildRet(builder, zero);*/


            string file = "test.kts";

            KTUSharpScanner scanner = new KTUSharpScanner(file);
            var tokens = scanner.ScanTokens();
            KTUSharpParser parser = new KTUSharpParser(tokens);
            var statements = parser.ParseTokens();
            ASTTypeChecker typeChecker = new ASTTypeChecker(statements);
            typeChecker.ExecuteTypeCheck();

            if (typeChecker.Failed) return;

            CodeGeneration.CodeGenerator gen = new CodeGeneration.CodeGenerator(file);
            gen.GenerateBitcode(statements);

            //string command = $@"clang -o {file[0..^4]} {file}";
            //System.Diagnostics.Process.Start("CMD.exe", command);
        }
    }
}

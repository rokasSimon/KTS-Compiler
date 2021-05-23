using System;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using LLVMSharp;

namespace KTS_Compiler
{
    class Program
    {
        // clang -o <exe name> <bitcode file> / generates executable
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("| Usage: kts <source filepath> |");
                return;
            }

            string file = args[1];

            var fileinfo = new FileInfo(file);

            if (fileinfo.Exists)
            {
                if (Path.GetExtension(file) == ".kts")
                {
                    KTUSharpScanner scanner = new KTUSharpScanner(file);
                    var tokens = scanner.ScanTokens();
                    KTUSharpParser parser = new KTUSharpParser(tokens);
                    var statements = parser.ParseTokens();
                    ASTTypeChecker typeChecker = new ASTTypeChecker(statements);
                    typeChecker.ExecuteTypeCheck();

                    if (typeChecker.Failed) return;

                    try
                    {
                        CodeGeneration.CodeGenerator gen = new CodeGeneration.CodeGenerator(file);
                        gen.GenerateBitcode(statements);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Crashed from bitcode generation.");
                        Console.WriteLine(e);
                    }

                    var fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                    var exeFile = fileWithoutExt + ".exe";
                    var bitcodeFile = fileWithoutExt + ".bc";

                    var powershell = PowerShell.Create();
                    using var runspace = RunspaceFactory.CreateRunspace();
                    runspace.Open();

                    powershell.Runspace = runspace;
                    string command = $@"clang -o {exeFile} {bitcodeFile}";
                    powershell.AddScript(command);
                    var psout = powershell.Invoke();
                    if (powershell.Streams.Error.Count == 0)
                    {
                        var psOutString = new StringBuilder().AppendJoin(Environment.NewLine, psout);
                        Console.WriteLine(psOutString);
                    }
                    else
                    {
                        foreach (var err in powershell.Streams.Error)
                        {
                            Console.WriteLine(err.ToString());
                        }
                    }
                }
                else
                {
                    Console.WriteLine("| File does not have a .kts extension |");
                }
            }
            else
            {
                Console.WriteLine("| Bad source file path given | Maybe use absolute path? |");
            }
        }
    }
}

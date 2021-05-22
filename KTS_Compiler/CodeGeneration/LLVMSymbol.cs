using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace KTS_Compiler.CodeGeneration
{
    public class LLVMSymbol
    {
        public LLVMValueRef Value { get; set; }
        public TypeSpecifier KtsType { get; set; }
        public bool IsFunction { get; set; }
        public Binding Binding { get; set; }
    }
}

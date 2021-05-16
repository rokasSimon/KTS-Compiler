using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KTS_Compiler
{
    public class FunctionSymbol
    {
        public TypeSpecifier ReturnType { get; set; }
        public List<Parameter> Parameters { get; set; }
    }
}

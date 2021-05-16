using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KTS_Compiler
{
    public class Binding
    {
        public Token Type { get; set; }
        public List<string> Identifiers { get; set; }
    }
}

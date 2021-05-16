using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KTS_Compiler
{
    public class Parameter
    {
        public bool Reference { get; set; }
        public TypeSpecifier Type { get; set; }
        public string Identifier { get; set; }

        public override string ToString()
        {
            string output = "";

            if (Reference)
            {
                output += "ref ";
            }

            output += Type;
            output += " ";
            output += Identifier;

            return output;
        }
    }
}

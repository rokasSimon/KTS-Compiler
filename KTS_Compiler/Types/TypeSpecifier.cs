using System;

namespace KTS_Compiler
{
    public enum TypeEnum
    {
        I8, I16, I32, I64,
        F32, F64,
        BOOL, STRING, VOID,
        ARRAY,
        POINTER,
        UNKNOWN
    }

    public class TypeSpecifier
    {
        public TypeEnum Type { get; set; }
        public int Dimensions { get; set; } // Non-array = 0

        public static TypeEnum FromString(string type) => type switch
        {
            "i8" => TypeEnum.I8,
            "i16" => TypeEnum.I16,
            "i32" => TypeEnum.I32,
            "i64" => TypeEnum.I64,
            "f32" => TypeEnum.F32,
            "f64" => TypeEnum.F64,
            "bool" => TypeEnum.BOOL,
            "string" => TypeEnum.STRING,
            "void" => TypeEnum.VOID,
            _ => TypeEnum.UNKNOWN
        };

        public static TypeEnum IntFromConst(long i) => i switch
        {
            var x when x <= byte.MaxValue && x >= byte.MinValue => TypeEnum.I8,
            var x when x <= short.MaxValue && x >= short.MinValue => TypeEnum.I16,
            var x when x <= int.MaxValue && x >= int.MinValue => TypeEnum.I32,
            var x when x <= long.MaxValue && x >= long.MaxValue => TypeEnum.I64,
            _ => throw new ArgumentException("Value too large or too small to parse")
        };

        public static TypeEnum FloatFromConst(double d) => d switch
        {
            var x when x <= Single.MaxValue && x >= Single.MinValue => TypeEnum.I16,
            var x when x <= Double.MaxValue && x >= Double.MinValue => TypeEnum.I32,
            _ => throw new ArgumentException("Value too large or too small to parse")
        };

        public static TypeSpecifier Null => new TypeSpecifier { Type = TypeEnum.UNKNOWN };

        public bool IsInt() => Type switch
        {
            TypeEnum.I8 or TypeEnum.I16 or TypeEnum.I32 or TypeEnum.I64 => true,
            _ => false
        };

        public bool IsFloat() => Type switch
        {
            TypeEnum.F32 or TypeEnum.F64 => true,
            _ => false
        };

        public bool IsBool()
        {
            return Type == TypeEnum.BOOL;
        }

        private bool ToInt() => Type switch
        {
            TypeEnum.I8 or TypeEnum.I16 or TypeEnum.I32 or TypeEnum.I64 => true,
            TypeEnum.F32 or TypeEnum.F64 => true,
            _ => false
        };

        private bool ToFloat() => Type switch
        {
            TypeEnum.I8 or TypeEnum.I16 or TypeEnum.I32 or TypeEnum.I64 => true,
            TypeEnum.F32 or TypeEnum.F64 => true,
            _ => false
        };

        private bool ToBool() => Type switch
        {
            TypeEnum.BOOL => true,
            _ => false
        };

        public bool CastableTo(TypeSpecifier other)
        {
            if (Dimensions != 0 || other.Dimensions != 0) return false;

            return other.Type switch
            {
                TypeEnum.I8 or TypeEnum.I16 or TypeEnum.I32 or TypeEnum.I64 => ToInt(),
                TypeEnum.F32 or TypeEnum.F64 => ToFloat(),
                TypeEnum.BOOL => ToBool(),
                _ => false,
            };
        }

        public bool ImplicitCastableTo(TypeSpecifier other)
        {
            if (Dimensions != 0 || other.Dimensions != 0) return false;

            return other.Type switch
            {
                TypeEnum.I8 => Type == TypeEnum.I8,
                TypeEnum.I16 => Type == TypeEnum.I8 || Type == TypeEnum.I16,
                TypeEnum.I32 => Type == TypeEnum.I8 || Type == TypeEnum.I16 || Type == TypeEnum.I32,
                TypeEnum.I64 => Type == TypeEnum.I8 || Type == TypeEnum.I16 || Type == TypeEnum.I32 || Type == TypeEnum.I64,
                TypeEnum.F32 => Type == TypeEnum.I8 || Type == TypeEnum.I16 || Type == TypeEnum.I32 || Type == TypeEnum.F32,
                TypeEnum.F64 => Type == TypeEnum.I8 || Type == TypeEnum.I16 || Type == TypeEnum.I32 || Type == TypeEnum.F32 || Type == TypeEnum.F64,
                TypeEnum.BOOL => Type == TypeEnum.BOOL,
                TypeEnum.STRING => Type == TypeEnum.STRING,
                _ => false
            };
        }

        public override string ToString()
        {
            string output = Type.ToString();

            for (int i = 0; i < Dimensions; i++)
            {
                output += "[]";
            }

            return output;
        }

        public static bool operator ==(TypeSpecifier first, TypeSpecifier other)
        {
            if (first.Type == TypeEnum.ARRAY || other.Type == TypeEnum.ARRAY)
            {
                return first.Dimensions == other.Dimensions;
            }

            if (first.Type == TypeEnum.STRING && other.Type == TypeEnum.I8 && other.Dimensions == 1
                || first.Type == TypeEnum.I8 && first.Dimensions == 1 && other.Type == TypeEnum.STRING)
            {
                return true;
            }

            return first.Type == other.Type && first.Dimensions == other.Dimensions;
        }

        public static bool operator !=(TypeSpecifier first, TypeSpecifier other)
        {
            return !(first == other);
        }

        public override bool Equals(object obj)
        {
            if (obj is TypeSpecifier ts)
            {
                return ts == this;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
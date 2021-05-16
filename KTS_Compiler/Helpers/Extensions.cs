namespace KTS_Compiler.Extensions
{
    public static class Extensions
    {
        public static bool IsDigit(this char c)
        {
            return c >= '0' && c <= '9';
        }

        public static bool IsAlpha(this char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   c == '_';
        }

        public static bool IsAlphaNumeric(this char c)
        {
            return c.IsDigit() || c.IsAlpha();
        }

        public static string RangeSubstring(this string value, int start, int end)
        {
            return value.Substring(start, end - start);
        }
    }
}

namespace BugByte;

internal static class Lexer
{
    internal static Queue<Token> LexFile(string filename)
    {
        var lines = File.ReadAllLines(filename);
        var words = new Queue<Token>();
        var lineNr = 1;
        foreach (var line in lines)
        {
            var lineSegment = LineSegment.From(line);
            while (lineSegment is not EmptyLineSegment)
            {
                if (lineSegment.Value.StartsWith(LineComment))
                {
                    break;
                }

                string word;

                if (lineSegment.Value.StartsWith(StringLiteral) || lineSegment.Value.StartsWith(NullTerminatedStringLiteral))
                {
                    var endQuoteIndex = lineSegment.Value.IndexOf(StringLiteral, lineSegment.Value.StartsWith(StringLiteral) ? StringLiteral.Length : NullTerminatedStringLiteral.Length);
                    if (endQuoteIndex is -1)
                    {
                        throw new Exception($"Missing end quote for string literal `{lineSegment}` @ {filename}:{lineNr}:{lineSegment.Offset}");
                    }
                    word = lineSegment.Value[..(endQuoteIndex + 1)];
                }
                else if (SpecialSeparatorSymbols.Contains(lineSegment.Value[0]))
                {
                    word = lineSegment.Value[0].ToString();
                }
                else
                {
                    var split = lineSegment.Value.Split(' ', 2);
                    word = split[0];
                    for (var index = 0; index < word.Length; index++)
                    {
                        if (SpecialSeparatorSymbols.Contains(word[index]))
                        {
                            word = word[..index];
                            break;
                        }
                    }
                }

                words.Enqueue(new Token(filename, word, lineNr, lineSegment.Offset));

                lineSegment = lineSegment.Without(word);
            }
            lineNr++;
        }
        return words;
    }

    internal const string LineComment = "#";
    internal const string StringLiteral = "\"";
    internal const string NullTerminatedStringLiteral = "0\"";
    internal static char[] SpecialSeparatorSymbols = new char[] { ':', ';', '?', '[', ']', '(', ')' };
}

internal record Token(string Filename, string Value, int Line, int Column)
{
    public override string ToString() => $"`{Value}` @ {Filename}:{Line}:{Column}";
};

public static class Tokens
{
    public static bool IsReserved(string token)
    {
        if (IsReserved(typeof(DataType), token))
        {
            return true;
        }
        if (IsReserved(typeof(Operator), token))
        {
            return true;
        }
        if (IsReserved(typeof(Keyword), token))
        {
            return true;
        }
        return false;
    }

    private static bool IsReserved(Type type, string token)
    {
        var reserved = type.GetFields();
        foreach (var field in reserved)
        {
            if (field.GetValue(null)?.ToString() == token)
            {
                return true;
            }
        }
        return false;
    }

    public static class DataType
    {
        public const string Number = "int";
        public const string Boolean = "bool";
        public const string Pointer = "ptr";
        public const string String = "str";
        public const string NullTerminatedString = "0str";
    }

    public static class Operator
    {
        public const string Add = "+";
        public const string Subtract = "-";
        public const string Multiply = "*";
        public const string Divide = "/";
        public const string Modulo = "%";

        public const string Equal = "=";
        public const string NotEqual = "!=";
        public const string LessThan = "<";
        public const string LessThanOrEqual = "<=";
        public const string GreaterThan = ">";
        public const string GreaterThanOrEqual = ">=";
        public const string StringEqual = "==";
        public const string StringNotEqual = "!==";

        public const string And = "&";
        public const string Or = "|";
        public const string Xor = "^";

        public const string ShiftLeft = "<<";
        public const string ShiftRight = ">>";
    }

    public static class Keyword
    {
        public const string Duplicate = "dup";
        public const string Drop = "drop";
        public const string Swap = "swap";
        public const string Over = "over";

        public const string Load = "load";
        public const string LoadByte = "load-byte";
        public const string Store = "store";

        public const string Print = "print";
        public const string PrintChar = "printc";
        public const string PrintString = "prints";

        public const string Syscall0 = "syscall0";
        public const string Syscall1 = "syscall1";
        public const string Syscall2 = "syscall2";
        public const string Syscall3 = "syscall3";
        public const string Syscall4 = "syscall4";
        public const string Syscall5 = "syscall5";
        public const string Syscall6 = "syscall6";

        public const string Inspect = "inspect";

        public const string Exit = "exit";

        public const string Yes = "yes";
        public const string No = "no";

        public const string Allocate = "alloc";

        public const string Branch = "?";

        public const string Loop = "while";

        public const string PinStackElements = "using";
        public const string Include = "include";

        public const string Cast = "as";

        public const string Struct = "struct";
        public const string ConstantDefinition = "aka";
    }
}

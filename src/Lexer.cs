namespace BugByte;

internal static class Lexer
{
    internal static SourceCode LexFile(string filename) => LexString(File.ReadAllLines(filename));

    internal static SourceCode LexString(string[] lines, string? filename = null)
    {
        var words = new Queue<Token>();
        var lineNr = 1;
        foreach (var line in lines)
        {
            var lineSegment = LineSegment.From(line);
            while (lineSegment is not EmptyLineSegment)
            {
                var nextWord = lineSegment.FindNextWord();
                if (nextWord is LineComment)
                {
                    break;
                }
                words.Enqueue(new(filename ?? string.Empty, nextWord, lineNr, lineSegment.Offset));
                lineSegment = lineSegment.Without(nextWord.Value);
            }
            lineNr++;
        }
        return new(words);
    }
}

internal record Token(string Filename, IWord Word, int Line, int Column)
{
    public static Token OnlyValue(string value) => new("", LineSegment.From(value).FindNextWord(), 0, 0);

    public override string ToString() => $"`{Word}` @ {Filename}:{Line}:{Column}";
};

public static class Tokens
{
    public const string BlockStart = ":";
    public const string BlockEnd = ";";
    public static bool IsReserved(string token)
    {
        if (IsReserved(typeof(Primitive), token))
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

    public static class Primitive
    {
        public const string Number = "int";
        public const string Boolean = "bool";
        public const string Pointer = "ptr";

        public static bool TryParsePrimitive(string token, out Primitives dataType)
        {
            if (token == Number)
            {
                dataType = Primitives.Number;
                return true;
            }
            if (token == Boolean)
            {
                dataType = Primitives.Number;
                return true;
            }
            if (token == Pointer)
            {
                dataType = Primitives.Pointer;
                return true;
            }
            dataType = Primitives.Unknown;
            return false;
        }
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
        public const string FunctionParametersStart = "(";
        public const string FunctionParametersEnd = ")";
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

        public const string Repeat = "repeat";
    }
}

namespace BugByte;

public interface IWord
{
    string Value { get; }
}

public static class LineExtensions
{
    public static IWord FindNextWord(this ILineSegment line)
    {
        if (line is EmptyLineSegment)
        {
            return new Word(string.Empty);
        }
        else if (IsLineComment(line.Value))
        {
            return new LineComment();
        }
        else if (IsStringLiteral(line.Value))
        {
            return new StringLiteralWord(line.Value);
        }
        else if (IsZeroTerminatedStringLiteral(line.Value))
        {
            return new ZeroTerminatedStringLiteralWord(line.Value);
        }
        else if (IsSpecialCharacter(line.Value))
        {
            return new SpecialCharacter(line.Value);
        }

        return new Word(line.Value.Split(WordSeparators, 2)[0]);
    }

    private static bool IsLineComment(string line) => line.StartsWith(LineCommentSymbol);
    private static bool IsStringLiteral(string line) => line.StartsWith(StringLiteralDefinition.StartSymbol);
    private static bool IsZeroTerminatedStringLiteral(string line) => line.StartsWith(ZeroTerminatedStringLiteralDefinition.StartSymbol);
    private static bool IsSpecialCharacter(string line) => SpecialSeparatorSymbols.Contains(line[0]);

    internal const string LineCommentSymbol = "#";

    internal static char[] SpecialSeparatorSymbols = [':', ';', '?', '[', ']', '(', ')'];
    internal static char[] WordSeparators = [' ', .. SpecialSeparatorSymbols];
}

static class StringLiteralDefinition
{
    public static string StartSymbol => "\"";
    public static string EndSymbol => "\"";
}

static class ZeroTerminatedStringLiteralDefinition
{
    public static string StartSymbol => "0\"";
    public static string EndSymbol => "\"";
}


public class Word : IWord
{
    public string Value { get; }

    public Word(string value) => Value = value;

    public override string ToString() => Value;
}

public class SpecialCharacter : IWord
{
    public string Value { get; }

    public SpecialCharacter(string value) => Value = value[0].ToString();

    public override string ToString() => Value;
}

public class StringLiteralWord : WrappedWord
{
    public StringLiteralWord(string value)
        : base(StringLiteralDefinition.StartSymbol, StringLiteralDefinition.EndSymbol, value) { }
}

public class ZeroTerminatedStringLiteralWord : WrappedWord
{
    public ZeroTerminatedStringLiteralWord(string value)
        : base(ZeroTerminatedStringLiteralDefinition.StartSymbol, ZeroTerminatedStringLiteralDefinition.EndSymbol, value) { }
}

public abstract class WrappedWord : IWord
{
    public string StartSymbol { get; }
    public string EndSymbol { get; }
    public string Value { get; }

    public string InnerValue => Value[StartSymbol.Length..^EndSymbol.Length];

    protected WrappedWord(string start, string end, string value)
    {
        StartSymbol = start;
        EndSymbol = end;
        if (!value.StartsWith(start))
        {
            throw new Exception($"WrappedWord must start with {start}");
        }
        var indexOfEnd = value.IndexOf(end, start.Length);
        if (indexOfEnd is -1)
        {
            throw new Exception($"WrappedWord must end with {end}");
        }
        Value = value[..(indexOfEnd + end.Length)];
    }

    public override string ToString() => Value;
}

public class LineComment : IWord
{
    public string Value => throw new NotImplementedException();
}

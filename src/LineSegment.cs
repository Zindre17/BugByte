namespace BugByte;

public interface ILineSegment
{
    public string Value { get; }
    public int Offset { get; }
}

public class LineSegment : ILineSegment
{
    public string Value { get; }
    public int Offset { get; }

    private LineSegment(string value, int offset)
    {
        Value = value;
        Offset = offset;
    }

    public static ILineSegment From(string value, int atOffset = 1)
    {
        if (atOffset < 1)
        {
            throw new ArgumentException("Lines start at index 1.");
        }
        if (string.IsNullOrWhiteSpace(value))
        {
            return new EmptyLineSegment();
        }
        var originalLength = value.Length;
        var newValue = value.TrimStart();
        var newOffset = atOffset + (originalLength - newValue.Length);
        return new LineSegment(newValue, newOffset);
    }

    public override string ToString() => Value;
}

public class EmptyLineSegment : ILineSegment
{
    public string Value => throw new NotImplementedException();
    public int Offset => throw new NotImplementedException();
}

public static class ILineSegmentExtensions
{
    public static ILineSegment Without(this ILineSegment segment, string value)
    {
        if (!segment.Value.StartsWith(value))
        {
            throw new ArgumentException($"LineSegment does not start with `{value}`");
        }
        return LineSegment.From(segment.Value[value.Length..], segment.Offset + value.Length);
    }
}


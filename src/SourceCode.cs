namespace BugByte;

internal class SourceCode
{
    private int currentPosition = 0;
    private readonly Token[] tokens;

    public SourceCode(Token[] tokens) => this.tokens = tokens;
    public SourceCode(IEnumerable<Token> tokens) => this.tokens = tokens.ToArray();

    public bool HasNextToken() => HasRemainingTokens(1);
    public Token MoveNext() => HasNextToken() ? tokens[currentPosition++] : throw new EndOfCodeException();
    public Token PeekNextToken() => PeekNthToken(1);
    public Token PeekNthToken(int count) => HasRemainingTokens(count) ? tokens[currentPosition + count - 1] : throw new EndOfCodeException();

    internal Token CurrentToken() => currentPosition is 0 ? throw new NoCurrentTokenYetException() : tokens[currentPosition - 1];

    internal bool HasRemainingTokens(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than 0");
        }

        return currentPosition + (count - 1) < tokens.Length;
    }
}

internal class EndOfCodeException : Exception
{
    public EndOfCodeException() : base("End of file reached") { }
}

internal class NoCurrentTokenYetException : Exception
{
    public NoCurrentTokenYetException() : base("No token in focus yet") { }
}

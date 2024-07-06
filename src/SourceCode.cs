namespace BugByte;

internal class SourceCode
{
    private int currentPosition = 0;
    private readonly Token[] tokens;

    public SourceCode(Token[] tokens) => this.tokens = tokens;
    public SourceCode(IEnumerable<Token> tokens) => this.tokens = tokens.ToArray();

    public bool HasNextToken()
        => HasRemainingTokens(1);

    public Token MoveNext()
        => HasNextToken() ? tokens[currentPosition++]
        : throw new EndOfCodeException();

    public Token PeekNextToken()
        => PeekNthToken(1);

    public Token PeekNthToken(int count)
        => HasRemainingTokens(count) ? tokens[CurrentIndex + count]
        : throw new EndOfCodeException();

    internal Token CurrentToken()
        => HasEnumerationStarted() ? tokens[CurrentIndex]
        : throw new NoCurrentTokenYetException();

    internal SourceCode GetCodeUntil(string tokenValue)
        => GetCodeUntil(token => token.Word.Value == tokenValue);

    internal SourceCode GetCodeUntilAny(params string[] tokenValues)
        => GetCodeUntil(token => tokenValues.Contains(token.Word.Value));

    internal SourceCode GetCodeUntil(Func<Token, bool> predicate)
    {
        var startIndex = NextIndex;
        bool predicateSucceded = false;
        while (HasNextToken())
        {
            var token = MoveNext();
            if (predicate(token))
            {
                predicateSucceded = true;
                break;
            }
        }

        if (!predicateSucceded)
        {
            throw new Exception("Search did not terminate before end of code.");
        }

        return new(tokens[startIndex..CurrentIndex]);
    }

    internal bool HasRemainingTokens(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than 0");
        }

        return CurrentIndex + count < tokens.Length;
    }

    private int CurrentIndex => currentPosition - 1;
    private int NextIndex => currentPosition;
    public bool HasEnumerationStarted() => currentPosition > 0;
    public void Reset() => currentPosition = 0;
}

internal class SourceCodeBuilder
{
    private readonly List<Token> tokens = [];

    public void Add(Token token) => tokens.Add(token);
    public SourceCode Build() => new(tokens);
}

internal class EndOfCodeException() : Exception("End of file reached")
{
}

internal class NoCurrentTokenYetException() : Exception("No token in focus yet")
{
}

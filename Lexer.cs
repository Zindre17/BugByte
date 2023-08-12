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
            var remainingLine = line.TrimStart();
            var currentColumn = line.Length - remainingLine.Length + 1; // index starts at 1
            while (remainingLine.Length > 0)
            {
                if (remainingLine.StartsWith(LineComment))
                {
                    break;
                }

                string word;

                if (remainingLine.StartsWith(StringLiteral) || remainingLine.StartsWith(NullTerminatedStringLiteral))
                {
                    var endQuoteIndex = remainingLine.IndexOf(StringLiteral, remainingLine.StartsWith(StringLiteral) ? StringLiteral.Length : NullTerminatedStringLiteral.Length);
                    if (endQuoteIndex is -1)
                    {
                        throw new Exception($"Missing end quote for string literal `{remainingLine}` @ {filename}:{lineNr}:{currentColumn}");
                    }
                    word = remainingLine[..(endQuoteIndex + 1)];
                }
                else if (SpecialSeparatorSymbols.Contains(remainingLine[0]))
                {
                    word = remainingLine[0].ToString();
                }
                else
                {
                    var split = remainingLine.Split(' ', 2);
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

                words.Enqueue(new Token(filename, word, lineNr, currentColumn));

                if (remainingLine.Length > word.Length)
                {
                    remainingLine = remainingLine[word.Length..].TrimStart();
                    currentColumn = line.Length - remainingLine.Length + 1;
                }
                else
                {
                    remainingLine = "";
                }
            }
            lineNr++;
        }
        return words;
    }

    internal const string LineComment = "#";
    internal const string StringLiteral = "\"";
    internal const string NullTerminatedStringLiteral = "0\"";
    internal static char[] SpecialSeparatorSymbols = new char[] { ':', ';', '?', '[', ']' };
}

internal record Token(string Filename, string Value, int Line, int Column)
{
    public override string ToString() => $"`{Value}` @ {Filename}:{Line}:{Column}";
};

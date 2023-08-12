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
                string word;
                if (remainingLine.StartsWith("#"))
                {
                    break;
                }
                if (remainingLine.StartsWith('"') || remainingLine.StartsWith("0\""))
                {
                    var endQuoteIndex = remainingLine.IndexOf('"', remainingLine.StartsWith("\"") ? 1 : 2);
                    if (endQuoteIndex == -1)
                    {
                        throw new Exception($"Missing end quote for string literal `{remainingLine}` @ {filename}:{lineNr}:{currentColumn}");
                    }
                    word = remainingLine[..(endQuoteIndex + 1)];
                }
                else
                {
                    var split = remainingLine.Split(' ', 2);
                    word = split[0];
                }
                if (word.Length > 6 && remainingLine.StartsWith("alloc["))
                {
                    words.Enqueue(new Token(filename, word[..6], lineNr, currentColumn));
                    word = word[6..];
                    currentColumn += 6;
                    remainingLine = remainingLine[6..];
                }
                if (word.Length > 4 && word.StartsWith("yes:"))
                {
                    words.Enqueue(new Token(filename, word[..4], lineNr, currentColumn));
                    word = word[4..];
                    currentColumn += 4;
                    remainingLine = remainingLine[4..];
                }
                else if (word.Length > 3 && word.StartsWith("no:"))
                {
                    words.Enqueue(new Token(filename, word[..3], lineNr, currentColumn));
                    word = word[3..];
                    currentColumn += 3;
                    remainingLine = remainingLine[3..];
                }

                if (word.Length > 1 && (word.EndsWith(";") || word.EndsWith("?") || word.EndsWith("]") || (word.EndsWith(":") && !word.StartsWith("yes:") && !word.StartsWith("no:"))))
                {
                    words.Enqueue(new Token(filename, word[..^1], lineNr, currentColumn));
                    words.Enqueue(new Token(filename, word[^1..], lineNr, currentColumn + word.Length - 1));
                }
                else
                {
                    words.Enqueue(new Token(filename, word, lineNr, currentColumn));
                }

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
}

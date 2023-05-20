if (args.Length == 0)
{
    Console.WriteLine("Please provide a file name to lex");
    return;
}
if (args.Length > 1)
{
    Console.WriteLine("Please provide only one file name");
    return;
}

var fileName = args[0];

var words = LexProgram(fileName);

foreach (var word in words)
{
    Console.WriteLine($"{fileName}:{word.Line}:{word.Column} - {word.Value}");
}


List<Token> LexProgram(string filename)
{
    var lines = File.ReadAllLines(filename);
    var words = new List<Token>();
    var lineNr = 1;
    foreach (var line in lines)
    {
        var remainigLine = line.TrimStart();
        var currentColumn = line.Length - remainigLine.Length + 1; // index starts at 1
        while (remainigLine.Length > 0)
        {
            var split = remainigLine.Split(' ', 2);
            words.Add(new Token(split[0], lineNr, currentColumn));
            if (split.Length > 1)
            {
                currentColumn += split[0].Length + 1; // +1 for the space
                remainigLine = split[1].TrimStart();
                currentColumn += split[1].Length - remainigLine.Length;
            }
            else
            {
                remainigLine = "";
            }
        }
        lineNr++;
    }
    return words;
}

record Token(string Value, int Line, int Column);
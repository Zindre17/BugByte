using System.Diagnostics;

var fileName = "file.txt";

var stopWatch = new Stopwatch();
stopWatch.Start();
var lines = File.ReadAllLines(fileName);
Console.WriteLine($"Read {lines.Length} lines in {stopWatch.ElapsedMilliseconds}ms");
stopWatch.Restart();

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
Console.WriteLine($"Processed {lines.Length} lines in {stopWatch.ElapsedMilliseconds}ms");

foreach (var word in words)
{
    Console.WriteLine($"{fileName}:{word.Line}:{word.Column} - {word.Value}");
}

record Token(string Value, int Line, int Column);
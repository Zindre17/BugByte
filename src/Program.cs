using System.Diagnostics;
using System.Text;
using BugByte;
using static BugByte.Lexer;
using static BugByte.Parser;

if (args.Length is 0)
{
    Console.WriteLine("Please provide a file name to lex");
    return;
}
if (args.Length > 1)
{
    Console.WriteLine("Please provide only one file name");
    return;
}

var path = args[0];
if (!Path.Exists(path))
{
    throw new Exception("File does not exist.");
}

var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new Exception("Failed to get current directory.");
var fileName = Path.GetFileName(path);

Directory.SetCurrentDirectory(directory);

var words = LexFile(fileName);

var meta = new GlobalContext();
var context = MetaEvaluate(words, meta, new(), null, out var remainingWords, out _);
var program = ParseProgram(new(remainingWords), meta, context);
var typeStack = new TypeStack();
var runtimePins = new Dictionary<string, Stack<Primitives>>();
foreach (var item in program)
{
    item.TypeCheck(typeStack, runtimePins);
}
if (typeStack.Count > 0)
{
    throw new Exception($"The program must have an empty stack at the end. Got {typeStack.Count} items on the stack.");
}

var assembler = new Assembler();
assembler.Assemble(program, meta);

Directory.CreateDirectory("./output");
File.WriteAllLines($"./output/{fileName.Split(".")[0]}.asm", assembler.Assembly);

Console.WriteLine("Compiled successfully.");

var fileNameWithoutExtension = "./output/" + fileName.Split(".")[0];
if (!RunExternalCommand("fasm", $"{fileNameWithoutExtension}.asm {fileNameWithoutExtension}"))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Failed to assemble.");
    Console.ResetColor();
    Environment.Exit(1);
}

if (!RunExternalCommand("chmod", $"+x {fileNameWithoutExtension}"))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine("Failed to make executable.");
    Console.ResetColor();
    Environment.Exit(1);
}

RunExternalCommand(fileNameWithoutExtension, "");

static bool RunExternalCommand(string command, string arguments, bool printInfo = true)
{
    var startInfo = new ProcessStartInfo()
    {
        FileName = command,
        Arguments = arguments,
    };
    try
    {
        if (printInfo)
        {
            Console.WriteLine($"\nRunning command: {command} {arguments}");
        }
        var process = Process.Start(startInfo);
        if (process is null)
        {
            if (printInfo)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Failed to start external process.");
                Console.ResetColor();
            }
            return false;
        }
        process.WaitForExit();
        if (process.ExitCode is not 0)
        {
            if (printInfo)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Command failed. Error({(sbyte)process.ExitCode})");
                Console.ResetColor();
            }
            return false;
        }
        return true;
    }
    catch (System.ComponentModel.Win32Exception e)
    {
        if (e.HResult is -2147467259)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine($"Command not found: {command}. Make sure you have all the dependencies installed.");
            Console.ResetColor();
            return false;
        }
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"{e.GetType()}({e.HResult}): {e.Message}");
        Console.ResetColor();
        return false;
    }
    catch (Exception e)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"{e.GetType()}({e.HResult}): {e.Message}");
        Console.ResetColor();
        return false;
    }
}

class TypeStack : Stack<(TypingType, Token)>
{
    public TypeStack() : base() { }

    // NOTE: To create a copy of a stack, we need to do it twice to get the elements back in order.
    public TypeStack(IEnumerable<(TypingType, Token)> collection) : base(new Stack<(TypingType, Token)>(collection)) { }

    internal (TypeStackDiff, string?) Diff(TypeStack other)
    {
        if (Count != other.Count)
        {
            return (TypeStackDiff.SizeDifference, this.ToString() + "\n\n" + other.ToString());
        }
        var stringBuilder = new StringBuilder();
        var result = TypeStackDiff.Equal;
        for (var i = 0; i < Count; i++)
        {
            var (type, token) = this.ElementAt(i);
            var (otherType, otherToken) = other.ElementAt(i);
            stringBuilder.AppendLine($"{i}: {type} ({token.Filename}:{token.Line}:{token.Column}) | {otherType} ({otherToken.Filename}:{otherToken.Line}:{otherToken.Column}))");
            if (type != otherType && !(type.IsPointer() && otherType.IsPointer()))
            {
                result = TypeStackDiff.TypeDifference;
            }
        }
        return (result, result is TypeStackDiff.Equal ? null : stringBuilder.ToString());
    }

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        foreach (var (type, token) in this)
        {
            stringBuilder.AppendLine($"{type} ({token.Filename}:{token.Line}:{token.Column})");
        }
        return stringBuilder.ToString();
    }
}

enum TypeStackDiff
{
    SizeDifference,
    TypeDifference,
    Equal,

}

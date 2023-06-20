using System.Diagnostics;

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

try
{
    var program = ParseProgram(words);
    GenerateAsembly(program, fileName);
}
catch (Exception e)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine("Failed to compile.");
    Console.ResetColor();
    Environment.Exit(1);
}

Console.WriteLine("Compiled successfully.");

var fileNameWithoutExtension = fileName.Split(".")[0];
ProcessStartInfo startInfo = new()
{
    FileName = "fasm",
    Arguments = $"{fileNameWithoutExtension}.asm {fileNameWithoutExtension}",
};
var process = Process.Start(startInfo);
process?.WaitForExit();

Console.WriteLine("Assembled successfully.");

startInfo = new()
{
    FileName = "chmod",
    Arguments = $"+x {fileNameWithoutExtension}",
};
process = Process.Start(startInfo);
process?.WaitForExit();

Console.WriteLine("Made executable successfully.");

startInfo = new()
{
    FileName = fileNameWithoutExtension,
};
process = Process.Start(startInfo);
process?.WaitForExit();

Console.WriteLine("Executed successfully.");

static void GenerateAsembly(ParsedProgram program, string filename)
{
    var assembly = new List<string>
    {
        "format ELF64 executable 3",
        "segment readable executable",
        "entry start",
        
        // TODO: Implement this yourself / understand what it does
        "print:"                               ,
        "  mov     r9, -3689348814741910323" ,
        "  sub     rsp, 40"                  ,
        "  mov     BYTE [rsp+31], 10"        ,
        "  lea     rcx, [rsp+30]"            ,
        ".L2:"                                 ,
        "  mov     rax, rdi"                 ,
        "  lea     r8, [rsp+32]"             ,
        "  mul     r9"                       ,
        "  mov     rax, rdi"                 ,
        "  sub     r8, rcx"                  ,
        "  shr     rdx, 3"                   ,
        "  lea     rsi, [rdx+rdx*4]"         ,
        "  add     rsi, rsi"                 ,
        "  sub     rax, rsi"                 ,
        "  add     eax, 48"                  ,
        "  mov     BYTE [rcx], al"           ,
        "  mov     rax, rdi"                 ,
        "  mov     rdi, rdx"                 ,
        "  mov     rdx, rcx"                 ,
        "  sub     rcx, 1"                   ,
        "  cmp     rax, 9"                   ,
        "  ja      .L2"                      ,
        "  lea     rax, [rsp+32]"            ,
        "  mov     edi, 1"                   ,
        "  sub     rdx, rax"                 ,
        "  xor     eax, eax"                 ,
        "  lea     rsi, [rsp+32+rdx]"        ,
        "  mov     rdx, r8"                  ,
        "  mov     rax, 1"                   ,
        "  syscall"                          ,
        "  add     rsp, 40"                  ,
        "  ret"                              ,

        "start:"
    };

    foreach (var operation in program.Operations)
    {
        if (operation.Type is TokenType.Number)
        {
            assembly.Add($"  mov rax, {operation.Value!.Number}");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is TokenType.Operator)
        {
            if (operation.Token.Value is "+")
            {
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {operation.Value!.Number}");
                assembly.Add($"  add rax, rbx");
                assembly.Add($"  push rax");
            }
            else
            {
                throw new Exception($"Unknown operator `{operation.Token.Value}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
            }
        }
        else
        {
            throw new Exception($"Unknown operation `{operation.Type}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
        }
    }

    // TODO: only temporary to print the item at the top of the stack
    assembly.Add("  pop rdi");
    assembly.Add("  call print");

    // EXIT syscall
    assembly.Add("  mov rax, 60");
    assembly.Add("  mov rdi, 0");
    assembly.Add("  syscall");

    File.WriteAllLines($"{filename.Split(".")[0]}.asm", assembly);
}

static ParsedProgram ParseProgram(List<Token> tokens)
{
    var program = new ParsedProgram(new List<Operation>());
    var tokenQueue = new Queue<Token>(tokens);
    while (tokenQueue.Count > 0)
    {
        var token = tokenQueue.Dequeue();
        if (int.TryParse(token.Value, out var value))
        {
            program.Operations.Add(new Operation(TokenType.Number, token, new Value(Number: value)));
        }
        else if (token.Value is "+")
        {
            var nextToken = tokenQueue.Dequeue();
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after +, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(TokenType.Operator, token, new Value(Number: operand)));
        }
        else
        {
            throw new Exception($"Unknown token `{token.Value}` @ {token.Filename}:{token.Line}:{token.Column}");
        }
    }
    return program;
}

static List<Token> LexProgram(string filename)
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
            words.Add(new Token(filename, split[0], lineNr, currentColumn));
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

record Token(string Filename, string Value, int Line, int Column);

enum TokenType
{
    Number,
    Operator,
}

record Value(int? Number = null, string? Word = null);

record Operation(TokenType Type, Token Token, Value? Value = null);

record ParsedProgram(List<Operation> Operations);

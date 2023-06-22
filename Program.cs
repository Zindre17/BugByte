using System.Diagnostics;

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

RunExternalCommand($"./{fileNameWithoutExtension}", "", false);

static bool RunExternalCommand(string command, string arguments, bool printInfo = true)
{
    var startInfo = new ProcessStartInfo()
    {
        FileName = command,
        Arguments = arguments,
    };
    try
    {
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
        if (printInfo)
        {
            Console.WriteLine($"Running command: {command} {arguments}");
        }
        process.WaitForExit();
        if (process.ExitCode is not 0)
        {
            if (printInfo)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"Command failed. Error({process.ExitCode}): {process.StandardError.ReadToEnd()}");
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

    var stringLiterals = new List<string>();
    foreach (var operation in program.Operations)
    {
        if (operation.Type is TokenType.Number)
        {
            var number = operation.Data?.Number
                ?? throw new Exception($"Operation was of type number but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            assembly.Add($"  mov rax, {number}");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is TokenType.Operator)
        {
            var op = operation.Data?.Operator
                ?? throw new Exception($"Operation was of type operator but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            var number = operation.Data.Number
                ?? throw new Exception($"Operation was of type operator but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            if (operation.Token.Value is "+")
            {
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  add rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (operation.Token.Value is "-")
            {
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  sub rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (operation.Token.Value is "*")
            {
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  mul rbx");
                assembly.Add($"  push rax");
            }
            else if (operation.Token.Value is "/")
            {
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  div rbx");
                assembly.Add($"  push rax");
                // NOTE: this ignores the remainder
            }
            else
            {
                throw new Exception($"Unknown operator `{operation.Token.Value}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
            }
        }
        else if (operation.Type is TokenType.Keyword)
        {
            var keyword = operation.Data?.Keyword
                ?? throw new Exception($"Operation was of type keyword but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            if (keyword is Keyword.Print)
            {
                assembly.Add("  pop rdi");
                assembly.Add("  call print");
            }
            else if (keyword is Keyword.PrintString)
            {
                assembly.Add("  pop rsi");
                assembly.Add("  pop rdx");
                assembly.Add("  mov rdi, 1");
                assembly.Add("  mov rax, 1");
                assembly.Add("  syscall");
            }
            else
            {
                throw new Exception($"Unknown keyword `{operation.Token.Value}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
            }
        }
        else if (operation.Type is TokenType.String)
        {
            var text = operation.Data?.Text
                ?? throw new Exception($"Operation was of type string but has no value. Probably a bug in the parser.");

            assembly.Add($"  mov rax, {text.Length}");
            assembly.Add($"  push rax");
            assembly.Add($"  push string_{stringLiterals.Count}");

            stringLiterals.Add(text);
        }
        else
        {
            throw new Exception($"Unknown operation `{operation.Type}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
        }
    }

    // EXIT syscall
    assembly.Add("  mov rax, 60");
    assembly.Add("  mov rdi, 0");
    assembly.Add("  syscall");

    // NOTE: removed the 'writeable' flag since these are constant string literals
    assembly.Add("segment readable");
    for (var i = 0; i < stringLiterals.Count; i++)
    {
        // TODO: Do this cleaner. This is potentially wasteful (emtpy "" at the end).
        var stringLiteral = stringLiterals[i]
            .Replace("\\r", "\", 13, \"")
            .Replace("\\n", "\", 10, \"")
            .Replace("\\t", "\", 9, \"");
        assembly.Add($"string_{i}: db \"{stringLiteral}\"");
    }

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
            program.Operations.Add(new Operation(TokenType.Number, token, new Meta(Number: value)));
        }
        else if (token.Value is "+")
        {
            if (tokenQueue.Count is 0)
            {
                throw new Exception($"Expected number after +, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var nextToken = tokenQueue.Dequeue();
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after +, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(TokenType.Operator, token, new Meta(Number: operand, Operator: Operator.Add)));
        }
        else if (token.Value is "-")
        {
            if (tokenQueue.Count is 0)
            {
                throw new Exception($"Expected number after -, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var nextToken = tokenQueue.Dequeue();
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after -, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(TokenType.Operator, token, new Meta(Number: operand, Operator: Operator.Subtract)));
        }
        else if (token.Value is "*")
        {
            if (tokenQueue.Count is 0)
            {
                throw new Exception($"Expected number after *, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var nextToken = tokenQueue.Dequeue();
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after *, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(TokenType.Operator, token, new Meta(Number: operand, Operator: Operator.Multiply)));
        }
        else if (token.Value is "/")
        {
            if (tokenQueue.Count == 0)
            {
                throw new Exception($"Expected number after /, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var nextToken = tokenQueue.Dequeue();
            if (!int.TryParse(nextToken.Value, out var operand))
            {
                throw new Exception($"Expected number after /, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
            }

            program.Operations.Add(new Operation(TokenType.Operator, token, new Meta(Number: operand, Operator: Operator.Divide)));
        }
        else if (token.Value is "print")
        {
            program.Operations.Add(new Operation(TokenType.Keyword, token, new Meta(Keyword: Keyword.Print)));
        }
        else if (token.Value is "prints")
        {
            program.Operations.Add(new Operation(TokenType.Keyword, token, new Meta(Keyword: Keyword.PrintString)));
        }
        else if (token.Value.StartsWith('"') && token.Value.EndsWith('"'))
        {
            program.Operations.Add(new Operation(TokenType.String, token, new Meta(Text: token.Value[1..^1])));
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
        var remainingLine = line.TrimStart();
        var currentColumn = line.Length - remainingLine.Length + 1; // index starts at 1
        while (remainingLine.Length > 0)
        {
            if (remainingLine.StartsWith('"'))
            {
                var endQuoteIndex = remainingLine.IndexOf('"', 1);
                if (endQuoteIndex == -1)
                {
                    throw new Exception($"Missing end quote for string literal `{remainingLine}` @ {filename}:{lineNr}:{currentColumn}");
                }
                var stringLiteral = remainingLine[..(endQuoteIndex + 1)];
                words.Add(new Token(filename, stringLiteral, lineNr, currentColumn));
                remainingLine = remainingLine[(endQuoteIndex + 2)..].TrimStart();
            }
            else
            {
                var split = remainingLine.Split(' ', 2);
                words.Add(new Token(filename, split[0], lineNr, currentColumn));
                if (split.Length > 1)
                {
                    currentColumn += split[0].Length + 1; // +1 for the space
                    remainingLine = split[1].TrimStart();
                    currentColumn += split[1].Length - remainingLine.Length;
                }
                else
                {
                    remainingLine = "";
                }
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
    Keyword,
    String,
}

enum Operator
{
    Add,
    Subtract,
    Multiply,
    Divide,
}

enum Keyword
{
    Print,
    PrintString,
}

record Meta(int? Number = null, string? Text = null, Keyword? Keyword = null, Operator? Operator = null);

record Operation(TokenType Type, Token Token, Meta? Data = null);

record ParsedProgram(List<Operation> Operations);

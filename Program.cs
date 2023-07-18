using System.Diagnostics;
using System.Text;

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
    var startBlock = GroupBlock(null, words, new());
    var program = ParseProgram(startBlock, new(), new());
    var typeStack = TypeCheckProgram(program, new(), new());
    if (typeStack.Count > 0)
    {
        throw new Exception($"The program must have an empty stack at the end. Got {typeStack.Count} items on the stack.");
    }
    var flattendProgram = FlattenProgram(program);
    var assembly = GenerateAssembly(flattendProgram);
    Directory.CreateDirectory(Directory.GetParent($"./output/{fileName.Split(".")[0]}")?.FullName ?? throw new Exception("Failed to create output directory."));
    File.WriteAllLines($"./output/{fileName.Split(".")[0]}.asm", assembly);
}
catch (Exception e)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.Error.WriteLine(e.Message);
    Console.Error.WriteLine("Failed to compile.");
    Console.ResetColor();
    throw;
}

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

RunExternalCommand($"./{fileNameWithoutExtension}", "", false);

static ParsedProgram FlattenProgram(ParsedProgram program)
{
    var flattenedProgram = new ParsedProgram(new(), new());
    foreach (var operation in program.Operations)
    {
        if (operation.Type is OperationType.Loop)
        {
            flattenedProgram.Operations.AddRange(FlattenProgram(program.NestedPrograms.Dequeue()).Operations);
            flattenedProgram.Operations.AddRange(FlattenProgram(program.NestedPrograms.Dequeue()).Operations);
        }
        else if (operation.Type is OperationType.Branch)
        {
            var count = operation.Data?.Number ?? throw new Exception("Branch operation must have a branch count.");
            for (var i = 0; i < count; i++)
            {
                flattenedProgram.Operations.AddRange(FlattenProgram(program.NestedPrograms.Dequeue()).Operations);
            }
        }
        else if (operation.Type is OperationType.UsingBlock)
        {
            flattenedProgram.Operations.AddRange(FlattenProgram(program.NestedPrograms.Dequeue()).Operations);
        }
        else if (operation.Type is OperationType.Inline)
        {
            flattenedProgram.Operations.AddRange(FlattenProgram(program.NestedPrograms.Dequeue()).Operations);
        }
        else
        {
            flattenedProgram.Operations.Add(operation);
        }
    }
    return flattenedProgram;
}

static Block GroupBlock(Token? last, Queue<Token> tokens, Dictionary<string, Block> functions, string? expectedClosingTag = null)
{
    var block = new Block(new(), new(), functions);
    if (tokens.Count is 0)
    {
        if (last is null)
        {
            throw new Exception("Empty program.");
        }
        throw new Exception($"Empty block after {last}.");
    }
    while (tokens.Count > 0)
    {
        var token = tokens.Dequeue();
        if (expectedClosingTag is not null && (token.Value == expectedClosingTag))
        {
            if (block.Tokens.Count is 0)
            {
                throw new Exception($"Empty block after {last}");
            }
            block.Tokens.Enqueue(token);
            return block;
        }
        else if (token.Value is "?")
        {
            block.Tokens.Enqueue(token);
            var missingBranch = "";
            if (tokens.Count is 0)
            {
                throw new Exception($"Missing branch after {token}");
            }
            if (tokens.Peek().Value is "yes:" or "no:" && tokens.Count > 0)
            {
                missingBranch = tokens.Peek().Value is "yes:" ? "no:" : "yes:";
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, ";"));
            }
            if (tokens.Count is 0)
            {
                continue;
            }
            if (tokens.Peek().Value == missingBranch && tokens.Count > 0)
            {
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, ";"));
            }
        }
        else if (token.Value is "while")
        {
            block.Tokens.Enqueue(token);
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, ":"));
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, ";"));
        }
        else if (token.Value is "using")
        {
            block.Tokens.Enqueue(token);
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, ":"));
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, block.Functions, ";"));
        }
        else if (token.Value.EndsWith("()"))
        {
            var functionName = token.Value[..^2];
            if (tokens.Count is 0 || tokens.Dequeue().Value is not ":")
            {
                throw new Exception($"Missing ':' after function declaration {functionName}.");
            }
            var functionBlock = GroupBlock(token, tokens, block.Functions, ";");
            if (!block.Functions.TryAdd(functionName, functionBlock))
            {
                throw new Exception($"Duplicate function {functionName}.");
            }
        }
        else
        {
            block.Tokens.Enqueue(token);
        }
    }
    if (expectedClosingTag is not null)
    {
        var openingToken = block.Tokens.Peek();
        throw new Exception($"Unclosed block at @ {openingToken.Filename}:{openingToken.Line}:{openingToken.Column}");
    }
    return block;
}

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

static List<string> GenerateAssembly(ParsedProgram program)
{
    const int TempStackCapacity = 100;

    var pinnedStackItems = new List<string>();
    var memories = new List<(string, int)>();
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

        "start:",
        ";-- setup temp stack pointers --",
        "  mov r15, temp_stack; top of stack",
        "  mov r14, temp_stack; bottom of stack",
    };

    var stringLiterals = new List<string>();
    foreach (var operation in program.Operations)
    {
        if (operation.Type is OperationType.PushNumber)
        {
            var number = operation.Data?.Number
                ?? throw new Exception($"Operation was of type number but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            assembly.Add($";-- number --");
            assembly.Add($"  mov rax, {number}");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.PushString)
        {
            var text = operation.Data?.Text
                ?? throw new Exception($"Operation was of type string but has no value. Probably a bug in the parser.");

            assembly.Add($";-- string --");
            assembly.Add($"  mov rax, {text.Length - text.Count(c => c is '\\')}");
            assembly.Add($"  push rax");
            assembly.Add($"  push string_{stringLiterals.Count}");

            stringLiterals.Add(text);
        }
        else if (operation.Type is OperationType.PushZeroString)
        {
            var text = operation.Data?.Text
                ?? throw new Exception($"Operation was of type push zero terminated string, but has no value. Probably a bug in the parser.");

            assembly.Add($";-- push zero terminated string --");
            assembly.Add($"  push string_{stringLiterals.Count}");

            stringLiterals.Add(text);
        }
        else if (operation.Type is OperationType.PushBool)
        {
            var boolean = operation.Data?.Bool
                ?? throw new Exception($"Operation was of type boolean but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            assembly.Add(";-- boolean --");
            assembly.Add($"  mov rax, {(boolean ? 1 : 0)}");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.PushDuplicate)
        {
            assembly.Add(";-- duplicate --");
            assembly.Add($"  pop rax");
            assembly.Add($"  push rax");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.Drop)
        {
            assembly.Add(";-- drop --");
            assembly.Add($"  pop rax");
        }
        else if (operation.Type is OperationType.Over)
        {
            assembly.Add(";-- over --");
            assembly.Add($"  pop rax");
            assembly.Add($"  pop rbx");
            assembly.Add($"  push rbx");
            assembly.Add($"  push rax");
            assembly.Add($"  push rbx");
        }
        else if (operation.Type is OperationType.Swap)
        {
            assembly.Add(";-- swap --");
            assembly.Add($"  pop rax");
            assembly.Add($"  pop rbx");
            assembly.Add($"  push rax");
            assembly.Add($"  push rbx");
        }
        else if (operation.Type is OperationType.Operator)
        {
            var op = operation.Data?.Operator
                ?? throw new Exception($"Operation was of type operator but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            if (op is Operator.StringEqual)
            {
                assembly.Add(";-- string equal --");
                assembly.Add($"  pop rcx");
                assembly.Add($"  pop r8");
                assembly.Add($"  pop rdx");
                assembly.Add($"  pop r9");
                assembly.Add($"  cmp r8, r9");
                assembly.Add($"  jne .string_not_equal");
                assembly.Add($"  cmp r8, 0");
                assembly.Add($"  je .string_equal");
                assembly.Add($".string_check_loop:");
                assembly.Add($"  mov al, [rcx]");
                assembly.Add($"  cmp al, [rdx]");
                assembly.Add($"  jne .string_not_equal");
                assembly.Add($"  dec r8");
                assembly.Add($"  cmp r8, 0");
                assembly.Add($"  je .string_equal");
                assembly.Add($"  inc rcx");
                assembly.Add($"  inc rdx");
                assembly.Add($"  jmp .string_check_loop");
                assembly.Add($".string_equal:");
                assembly.Add($"  mov rax, 1");
                assembly.Add($"  push rax");
                assembly.Add($"  jmp .string_equal_end");
                assembly.Add($".string_not_equal:");
                assembly.Add($"  mov rax, 0");
                assembly.Add($"  push rax");
                assembly.Add($".string_equal_end:");
                continue;
            }
            else if (op is Operator.Add)
            {
                assembly.Add(";-- add --");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  add rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Subtract)
            {
                assembly.Add(";-- subtract --");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  sub rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Multiply)
            {
                assembly.Add(";-- multiply --");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  mul rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Divide)
            {
                assembly.Add(";-- divide --");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  div rbx");
                assembly.Add($"  push rax");
                // TODO: merge this with modulo
            }
            else if (op is Operator.Modulo)
            {
                assembly.Add(";-- modulo --");
                assembly.Add($"  xor rdx, rdx");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  div rbx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.Not)
            {
                assembly.Add(";-- not --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, 0");
                assembly.Add($"  cmove rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.Equal)
            {
                assembly.Add(";-- equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, rbx");
                assembly.Add($"  cmove rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.NotEqual)
            {
                assembly.Add(";-- not equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, rbx");
                assembly.Add($"  cmovne rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.LessThan)
            {
                assembly.Add(";-- less than --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, rbx");
                assembly.Add($"  cmovl rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.LessThanOrEqual)
            {
                assembly.Add(";-- less than or equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, rbx");
                assembly.Add($"  cmovle rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.GreaterThan)
            {
                assembly.Add(";-- greater than --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, rbx");
                assembly.Add($"  cmovg rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.GreaterThanOrEqual)
            {
                assembly.Add(";-- greater than or equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, rbx");
                assembly.Add($"  cmovge rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else
            {
                throw new Exception($"Unknown operator {op} `{operation.Token.Value}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
            }
        }
        else if (operation.Type is OperationType.Print)
        {
            assembly.Add(";-- print --");
            assembly.Add("  pop rdi");
            assembly.Add("  call print");
        }
        else if (operation.Type is OperationType.PrintString)
        {
            assembly.Add(";-- print string --");
            assembly.Add("  pop rsi");
            assembly.Add("  pop rdx");
            assembly.Add("  mov rdi, 1");
            assembly.Add("  mov rax, 1");
            assembly.Add("  syscall");
        }
        else if (operation.Type is OperationType.AllocateMemory)
        {
            var size = operation.Data?.Number
                ?? throw new Exception("AllocateMemory has no size. Probably a bug in the parser.");
            var name = operation.Data?.Text
                ?? throw new Exception("AllocateMemory has no name. Probably a bug in the parser.");

            memories.Add((name, size));
        }
        else if (operation.Type is OperationType.PushMemory)
        {
            var name = operation.Data?.Text
                ?? throw new Exception("PushMemory has no name. Probably a bug in the parser.");
            assembly.Add($";-- push memory {name} --");
            assembly.Add($"  mov rax, {name}");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.StoreMemory)
        {
            assembly.Add(";-- store memory --");
            assembly.Add("  pop rbx");
            assembly.Add("  pop rax");
            assembly.Add("  mov [rbx], rax");
        }
        else if (operation.Type is OperationType.LoadMemory)
        {
            assembly.Add($";-- load memory --");
            assembly.Add($"  pop rax");
            assembly.Add($"  mov rax, [rax]");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.LoadByte)
        {
            assembly.Add($";-- load byte --");
            assembly.Add($"  pop rbx");
            assembly.Add($"  xor rax, rax");
            assembly.Add($"  mov al, BYTE [rbx]");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.Cast)
        {

        }
        else if (operation.Type is OperationType.Syscall)
        {
            var version = operation.Data?.Number
                ?? throw new Exception("Syscall has no version. Probably a bug in the parser.");

            assembly.Add($";-- syscall {version} --");
            assembly.Add("  pop rax");
            if (version > 0)
            {
                assembly.Add("  pop rdi");
            }
            if (version > 1)
            {
                assembly.Add("  pop rsi");
            }
            if (version > 2)
            {
                assembly.Add("  pop rdx");
            }
            if (version > 3)
            {
                assembly.Add("  pop r10");
            }
            if (version > 4)
            {
                assembly.Add("  pop r8");
            }
            if (version > 5)
            {
                assembly.Add("  pop r9");
            }
            assembly.Add("  syscall");
            assembly.Add("  push rax");
        }
        else if (operation.Type is OperationType.JumpIfZero)
        {
            var endLabel = operation.Data?.Text
                ?? throw new Exception("If-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add(";-- jump if zero --");
            assembly.Add($"  pop rax");
            assembly.Add($"  cmp rax, 0");
            assembly.Add($"  jz {endLabel}");
        }
        else if (operation.Type is OperationType.JumpIfNotZero)
        {
            var endLabel = operation.Data?.Text
                ?? throw new Exception("If-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add(";-- jump if not zero --");
            assembly.Add($"  pop rax");
            assembly.Add($"  cmp rax, 0");
            assembly.Add($"  jnz {endLabel}");
        }
        else if (operation.Type is OperationType.Label)
        {
            var endLabel = operation.Data?.Text
                ?? throw new Exception("Block-end-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add($"{endLabel}:");
        }
        else if (operation.Type is OperationType.Jump)
        {
            var label = operation.Data?.Text
                ?? throw new Exception("Jump-keyword has no jump label. Probably a bug in the parser.");
            assembly.Add(";-- jump --");
            assembly.Add($"  jmp {label}");
        }
        else if (operation.Type is OperationType.PinStackElement)
        {
            var index = pinnedStackItems.Count;
            if (index is TempStackCapacity)
            {
                throw new Exception($"Too many pinned stack items. Max is {TempStackCapacity}.");
            }
            pinnedStackItems.Add(operation.Token.Value);
            assembly.Add(";-- pin stack element --");
            assembly.Add($"  pop rax");
            assembly.Add($"  mov [r15], rax");
            assembly.Add($"  add r15, 8");
        }
        else if (operation.Type is OperationType.PushPinnedStackItem)
        {
            var label = operation.Token.Value;
            var index = pinnedStackItems.IndexOf(label);
            if (index == -1)
            {
                throw new Exception($"Unknown pinned stack item `{label}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
            }
            assembly.Add(";-- push pinned stack item --");
            assembly.Add($"  mov rax, [r14 + {index * 8}]");
            assembly.Add($"  push rax");
        }
        else if (operation.Type is OperationType.UnpinStackElement)
        {
            pinnedStackItems.RemoveAt(pinnedStackItems.Count - 1);
            assembly.Add(";-- unpin stack element --");
            assembly.Add($"  sub r15, 8");
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
            .Replace("\0", "\", 0, \"")
            .Replace("\\r", "\", 13, \"")
            .Replace("\\n", "\", 10, \"")
            .Replace("\\t", "\", 9, \"");
        assembly.Add($"string_{i}: db \"{stringLiteral}\"");
    }

    assembly.Add("segment readable writeable");
    assembly.Add($"temp_stack: rq {TempStackCapacity}");
    foreach (var (name, size) in memories)
    {
        assembly.Add($"{name}: rb {size}");
    }
    return assembly;
}

static TypeStack TypeCheckProgram(ParsedProgram program, TypeStack typeStack, Dictionary<string, DataType> pinnedStackItems)
{
    var nestedIndex = 0;
    for (var i = 0; i < program.Operations.Count; i++)
    {
        var operation = program.Operations[i];
        var token = operation.Token;

        if (operation.Type is OperationType.PushBool)
        {
            // TODO: introduce a new type for booleans
            typeStack.Push((DataType.Number, token));
        }
        else if (operation.Type is OperationType.PushString)
        {
            typeStack.Push((DataType.Number, token));
            typeStack.Push((DataType.Pointer, token));
        }
        else if (operation.Type is OperationType.PushZeroString)
        {
            typeStack.Push((DataType.Pointer, token));
        }
        else if (operation.Type is OperationType.PushNumber)
        {
            typeStack.Push((DataType.Number, token));
        }
        else if (operation.Type is OperationType.PushPinnedStackItem)
        {
            if (!pinnedStackItems.TryGetValue(token.Value, out var dataType))
            {
                throw new Exception($"Unknown pinned stack item {token}");
            }
            typeStack.Push((dataType, token));
        }
        else if (operation.Type is OperationType.PushDuplicate)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, _) = typeStack.Peek();
            typeStack.Push((top, token));
        }
        else if (operation.Type is OperationType.PushMemory)
        {
            typeStack.Push((DataType.Pointer, token));
        }
        else if (operation.Type is OperationType.PinStackElement)
        {
            pinnedStackItems[operation.Token.Value] = typeStack.Pop().Item1;
        }
        else if (operation.Type is OperationType.UnpinStackElement)
        {
            pinnedStackItems.Remove(operation.Token.Value);
        }
        else if (operation.Type is OperationType.Drop)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            typeStack.Pop();
        }
        else if (operation.Type is OperationType.Over)
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"{operation.Type} expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var top = typeStack.Pop();
            var (prev, _) = typeStack.Peek();
            typeStack.Push(top);
            typeStack.Push((prev, token));
        }
        else if (operation.Type is OperationType.Swap)
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"{operation.Type} expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, _) = typeStack.Pop();
            var (prev, _) = typeStack.Pop();
            typeStack.Push((top, token));
            typeStack.Push((prev, token));
        }
        else if (operation.Type is OperationType.Jump)
        {
            // does not affect the stack
        }
        else if (operation.Type is OperationType.JumpIfNotZero or OperationType.JumpIfZero)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            // TODO: replace with a new type for booleans
            if (top is not DataType.Number)
            {
                throw new Exception($"{operation.Type} expects a number on the stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
        }
        else if (operation.Type is OperationType.AllocateMemory)
        {
            // does not affect the stack
        }
        else if (operation.Type is OperationType.Print)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expected value on stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number and not DataType.Pointer)
            {
                throw new Exception($"{operation.Type} expected number or pointer on stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
        }
        else if (operation.Type is OperationType.PrintString)
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"{operation.Type} expected at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            var (nextTop, nextTopToken) = typeStack.Pop();
            if (top is not DataType.Pointer)
            {
                throw new Exception($"{operation.Type} expected pointer on top of stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
            if (nextTop is not DataType.Number)
            {
                throw new Exception($"{operation.Type} expected number as second element ont the stack, but got `{nextTop}` @ {nextTopToken.Filename}:{nextTopToken.Line}:{nextTopToken.Column}");
            }
        }
        else if (operation.Type is OperationType.Operator)
        {
            var op = operation.Data?.Operator ??
                throw new Exception("Operation is missing operator type. Probably a bug in the parser.");


            if (op is Operator.Not)
            {
                if (typeStack.Count is 0)
                {
                    throw new Exception($"Operator `{op}` expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
                }
                var (topType, topToken) = typeStack.Pop();
                // TODO: replace with a new type for booleans
                if (topType is not DataType.Number)
                {
                    throw new Exception($"Operator `{op}` expects a number on the stack, but got `{topType}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
                }
                // TODO: replace with a new type for booleans
                typeStack.Push((DataType.Number, token));
                continue;
            }

            if (op is Operator.StringEqual)
            {
                if (typeStack.Count < 4)
                {
                    throw new Exception($"Operator {op} expects at least 4 elements on the stack, but got {typeStack.Count}.");
                }
                var (type1, _) = typeStack.Pop();
                if (type1 is not DataType.Pointer)
                {
                    throw new Exception($"Operator {op} expects a pointer to a string on top of the stack, but got {type1}.");
                }
                var (type2, _) = typeStack.Pop();
                if (type2 is not DataType.Number)
                {
                    throw new Exception($"Operator {op} expects a number as the second element on the stack, but got {type2}.");
                }
                var (type3, _) = typeStack.Pop();
                if (type3 is not DataType.Pointer)
                {
                    throw new Exception($"Operator {op} expects a pointer to a string as the third element on the stack, but got {type3}.");
                }
                var (type4, _) = typeStack.Pop();
                if (type4 is not DataType.Number)
                {
                    throw new Exception($"Operator {op} expects a number as the fourth element on the stack, but got {type4}.");
                }
                // TODO: replace with a new type for booleans
                typeStack.Push((DataType.Number, token));
                continue;
            }

            if (typeStack.Count < 2)
            {
                throw new Exception($"Operator `{op}` expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }

            if (op is Operator.Equal or Operator.NotEqual or Operator.LessThan or Operator.GreaterThan or Operator.LessThanOrEqual or Operator.GreaterThanOrEqual)
            {
                typeStack.Pop();
                typeStack.Pop();
                // TODO: replace with a new type for booleans
                typeStack.Push((DataType.Number, token));
            }
            else if (op is Operator.Add)
            {
                var (type, _) = typeStack.Pop();
                var (nextType, _) = typeStack.Pop();

                if (type is DataType.Pointer && nextType is DataType.Pointer)
                {
                    throw new Exception($"Pointer + Pointer is not allowed @ {token.Filename}:{token.Line}:{token.Column}");
                }

                if (type is DataType.Pointer || nextType is DataType.Pointer)
                {
                    typeStack.Push((DataType.Pointer, token));
                }
                else
                {
                    typeStack.Push((DataType.Number, token));
                }
            }
            else if (op is Operator.Subtract)
            {
                var (type, _) = typeStack.Pop();
                var (nextType, _) = typeStack.Pop();
                if (type is DataType.Pointer && nextType is DataType.Pointer)
                {
                    // NOTE: pointer - pointer is a number, and not a pointer
                    typeStack.Push((DataType.Number, token));
                }
                else if (type is DataType.Pointer || nextType is DataType.Pointer)
                {
                    typeStack.Push((DataType.Pointer, token));
                }
                else
                {
                    typeStack.Push((DataType.Number, token));
                }
            }
            else if (op is Operator.Multiply or Operator.Divide or Operator.Modulo)
            {
                var (top, topToken) = typeStack.Pop();
                var (nextTop, nextTopToken) = typeStack.Pop();
                if (top is DataType.Pointer || nextTop is DataType.Pointer)
                {
                    throw new Exception($"Operator `{op}` does not support pointers @ {token.Filename}:{token.Line}:{token.Column}");
                }
                typeStack.Push((DataType.Number, token));
            }
        }
        else if (operation.Type is OperationType.StoreMemory)
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"{operation.Type} expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            var (nextTop, nextTopToken) = typeStack.Pop();
            if (top is not DataType.Pointer)
            {
                throw new Exception($"{operation.Type} expects a pointer as second element ont the stack, but got {top}");
            }
        }
        else if (operation.Type is OperationType.LoadMemory)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expects at least one value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Pointer)
            {
                throw new Exception($"{operation.Type} expects pointer on top of stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
            // TODO: replace this with the actual type if possible
            typeStack.Push((DataType.Number, token));
        }
        else if (operation.Type is OperationType.LoadByte)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expects at least one value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Pointer)
            {
                throw new Exception($"{operation.Type} expects pointer on top of stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
            // TODO: replace this with the actual type if possible
            typeStack.Push((DataType.Number, token));
        }
        else if (operation.Type is OperationType.Cast)
        {
            var type = operation.Data?.Type ?? throw new Exception($"{operation.Type} expects type as metadata. Probably a bug in the parser. @ {token.Filename}:{token.Line}:{token.Column}");
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expects at least one value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            typeStack.Pop();
            typeStack.Push((type, token));
        }
        else if (operation.Type is OperationType.Syscall)
        {
            var arguments = operation.Data?.Number ??
                throw new Exception($"{operation.Type} expects argument count as metadata. Probably a bug in the parser. @ {token.Filename}:{token.Line}:{token.Column}");

            if (typeStack.Count < (arguments + 1))
            {
                throw new Exception($"{operation.Type} expects at least {arguments + 1} value(s) on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }

            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"{operation.Type} expects number on top of stack, but got `{top}` from {topToken}.");
            }

            for (var j = 0; j < arguments; j++)
            {
                typeStack.Pop();
            }

            typeStack.Push((DataType.Number, token));
        }
        else if (operation.Type is OperationType.Branch)
        {
            var count = operation.Data?.Number ??
                throw new Exception($"{operation.Type} expects argument count as metadata. Probably a bug in the parser. @ {token.Filename}:{token.Line}:{token.Column}");

            if (program.NestedPrograms.Count < count + nestedIndex)
            {
                throw new Exception($"{operation.Type} expects at least {count} nested program(s), but got {program.NestedPrograms.Count - nestedIndex} @ {token.Filename}:{token.Line}:{token.Column}");
            }

            if (count is 1)
            {
                // NOTE: Must not alter the stack (typewise nor size wise)
                var branch = program.NestedPrograms.ElementAt(nestedIndex++);
                var branchStack = TypeCheckProgram(branch, new(typeStack), pinnedStackItems);
                var (diffResult, stackDump) = typeStack.Diff(branchStack);
                if (diffResult is not TypeStackDiff.Equal)
                {
                    throw new Exception($"Single branch `?` block must leave the stack unchanged, but it left it in a different state @ {token.Filename}:{token.Line}:{token.Column}\n{stackDump}");
                }
            }
            else
            {
                // NOTE: each branch must leave the stack in the same state
                var branch = program.NestedPrograms.ElementAt(nestedIndex++);
                var branchStack = TypeCheckProgram(branch, new(typeStack), pinnedStackItems);
                var branchToken = branch.Operations.First().Token;
                for (var j = 1; j < count; j++)
                {
                    var otherBranch = program.NestedPrograms.ElementAt(nestedIndex++);
                    var otherBranchStack = TypeCheckProgram(otherBranch, new(typeStack), pinnedStackItems);
                    var otherBranchToken = otherBranch.Operations.First().Token;
                    var (diff, stackDump) = branchStack.Diff(otherBranchStack);
                    if (diff is TypeStackDiff.SizeDifference)
                    {
                        throw new Exception($"Branches starting at {branchToken.Filename}:{branchToken.Line}:{branchToken.Column} and {otherBranchToken.Filename}:{otherBranchToken.Line}:{otherBranchToken.Column} have different stack sizes.");
                    }
                    else if (diff is TypeStackDiff.TypeDifference)
                    {
                        throw new Exception($"Branches starting at {branchToken.Filename}:{branchToken.Line}:{branchToken.Column} and {otherBranchToken.Filename}:{otherBranchToken.Line}:{otherBranchToken.Column} have diverging stack types:\n{stackDump}");
                    }
                }
                typeStack = branchStack;
            }
        }
        else if (operation.Type is OperationType.Loop)
        {
            if (program.NestedPrograms.Count < 2)
            {
                throw new Exception($"Loop expects two nested programs, but got {program.NestedPrograms.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }

            var condition = program.NestedPrograms.ElementAt(nestedIndex++);
            var conditionStack = TypeCheckProgram(condition, new(typeStack), pinnedStackItems);
            if ((conditionStack.Count - typeStack.Count) is not 0)
            {
                throw new Exception($"Expected condition to produce no values (conditional jump consumes produced bool), but got {conditionStack.Count - typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }

            var whileBlock = program.NestedPrograms.ElementAt(nestedIndex++);
            var whileStack = TypeCheckProgram(whileBlock, new(conditionStack), pinnedStackItems);
            var (diff, stackDump) = conditionStack.Diff(whileStack);
            if (diff is TypeStackDiff.SizeDifference)
            {
                throw new Exception($"Loop must not alter stack size. Size diff: {whileStack.Count - conditionStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            else if (diff is TypeStackDiff.TypeDifference)
            {
                throw new Exception($"Stack types diverged before and after loop:\n{stackDump}\n@ {token.Filename}:{token.Line}:{token.Column}");
            }
            typeStack = whileStack;
        }
        else if (operation.Type is OperationType.Label)
        {
            // Does not affect typestack
        }
        else if (operation.Type is OperationType.UsingBlock)
        {
            var usingBlock = program.NestedPrograms.ElementAt(nestedIndex++);
            var usingStack = TypeCheckProgram(usingBlock, new(typeStack), pinnedStackItems);
            typeStack = usingStack;
        }
        else if (operation.Type is OperationType.Inline)
        {
            var functionBlock = program.NestedPrograms.ElementAt(nestedIndex++);
            var functionStack = TypeCheckProgram(functionBlock, new(typeStack), pinnedStackItems);
            typeStack = functionStack;
        }
        else if (operation.Type is OperationType.Inspect)
        {
            Console.WriteLine(typeStack);
            break;
        }
        else
        {
            throw new Exception($"Unknown operation `{operation.Type}` @ {token.Filename}:{token.Line}:{token.Column}");
        }
    }
    return typeStack;
}

// TODO: Allocate memory relative to the current scope from a pool
// TODO: Deallocate memory at the end of scope
static ParsedProgram ParseProgram(Block block, Dictionary<string, Token> memories, Dictionary<string, Token>? pinnedStackItems = null)
{
    var keywords = new string[]
    {
        "dup",
        "swap",
        "drop",
        "over",
        "?",
        "no:",
        "yes:",
        "while",
        "+",
        "-",
        "*",
        "/",
        "%",
        "<",
        "<=",
        ">",
        ">=",
        "print",
        "prints",
        "=",
        "!=",
        "==",
        "using",
    };
    var inclusions = new Dictionary<string, Token>();
    var operations = new List<Operation>();
    var tokens = block.Tokens;
    var program = new ParsedProgram(operations, new());
    while (tokens.Count > 0)
    {
        var token = tokens.Dequeue();
        if (pinnedStackItems is not null && pinnedStackItems.TryGetValue(token.Value, out var tuple))
        {
            operations.Add(new Operation(OperationType.PushPinnedStackItem, token, new Meta(Text: token.Value)));
        }
        else if (block.Functions.TryGetValue(token.Value, out var functionBlock))
        {
            var parsedFunction = ParseProgram(functionBlock, new());
            program.NestedPrograms.Enqueue(parsedFunction);
            operations.Add(new Operation(OperationType.Inline, token));
        }
        else if (memories.ContainsKey(token.Value))
        {
            operations.Add(new Operation(OperationType.PushMemory, token, new Meta(Text: token.Value)));
        }
        else if (int.TryParse(token.Value, out var value))
        {
            operations.Add(new Operation(OperationType.PushNumber, token, new Meta(Number: value)));
        }
        else if (token.Value is "+")
        {
            ParseOperator(token, Operator.Add);
        }
        else if (token.Value is "-")
        {
            ParseOperator(token, Operator.Subtract);
        }
        else if (token.Value is "*")
        {
            ParseOperator(token, Operator.Multiply);
        }
        else if (token.Value is "/")
        {
            ParseOperator(token, Operator.Divide);
        }
        else if (token.Value is "%")
        {
            ParseOperator(token, Operator.Modulo);
        }
        else if (token.Value is "=")
        {
            ParseOperator(token, Operator.Equal);
        }
        else if (token.Value is "==")
        {
            var nextToken = GetNextToken($"Expected string literal after `==`, but got nothing.");
            if (!IsString(nextToken, out var str))
            {
                throw new Exception($"Expected string literal after `==`, but got {nextToken}");
            }
            operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: str)));
            operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.StringEqual)));
        }
        else if (token.Value is "!==")
        {
            var nextToken = GetNextToken($"Expected string literal after `!==`, but got nothing.");
            if (!IsString(nextToken, out var str))
            {
                throw new Exception($"Expected string literal after `!==`, but got {nextToken}");
            }

            operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: str)));
            operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.StringEqual)));
            operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.Not)));
        }
        else if (token.Value is "!=")
        {
            ParseOperator(token, Operator.NotEqual);
        }
        else if (token.Value is "<")
        {
            ParseOperator(token, Operator.LessThan);
        }
        else if (token.Value is "<=")
        {
            ParseOperator(token, Operator.LessThanOrEqual);
        }
        else if (token.Value is ">")
        {
            ParseOperator(token, Operator.GreaterThan);
        }
        else if (token.Value is ">=")
        {
            ParseOperator(token, Operator.GreaterThanOrEqual);
        }
        else if (token.Value is "dup")
        {
            operations.Add(new Operation(OperationType.PushDuplicate, token));
        }
        else if (token.Value is "drop")
        {
            operations.Add(new Operation(OperationType.Drop, token));
        }
        else if (token.Value is "over")
        {
            operations.Add(new Operation(OperationType.Over, token));
        }
        else if (token.Value is "swap")
        {
            operations.Add(new Operation(OperationType.Swap, token));
        }
        else if (token.Value is "print")
        {
            operations.Add(new Operation(OperationType.Print, token));
        }
        else if (token.Value is "prints")
        {
            operations.Add(new Operation(OperationType.PrintString, token));
        }
        else if (token.Value is "alloc[")
        {
            var nextToken = GetNextToken($"Expected number after `alloc[`, but got nothing.");
            if (!int.TryParse(nextToken.Value, out var number))
            {
                throw new Exception($"Expected a number after `alloc[` but got {token}");
            }
            var endToken = GetNextToken($"Expected `]` after `alloc[` but got nothing.");
            if (endToken.Value is not "]")
            {
                throw new Exception($"Unclosed `alloc[` @ {token.Filename}:{token.Line}:{token.Column}");
            }

            var name = GetNextToken($"Expected name after `alloc[{number}]` but got nothing.");
            if (keywords.Contains(name.Value))
            {
                throw new Exception($"`{name.Value}` is a keyword and cannot be used as a memory name @ {name.Filename}:{name.Line}:{name.Column}");
            }
            if (!memories.TryAdd(name.Value, name))
            {
                throw new Exception($"`{name.Value}` is already allocated at {memories[name.Value]}");
            }

            operations.Add(new Operation(OperationType.AllocateMemory, token, new Meta(Number: number, Text: name.Value)));
        }
        else if (token.Value is "store")
        {
            operations.Add(new Operation(OperationType.StoreMemory, token));
        }
        else if (token.Value is "load")
        {
            operations.Add(new Operation(OperationType.LoadMemory, token));
        }
        else if (token.Value is "load-byte")
        {
            operations.Add(new Operation(OperationType.LoadByte, token));
        }
        else if (token.Value is "(ptr)")
        {
            operations.Add(new Operation(OperationType.Cast, token, new Meta(Type: DataType.Pointer)));
        }
        else if (token.Value is "syscall0")
        {
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 0)));
        }
        else if (token.Value is "syscall1")
        {
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 1)));
        }
        else if (token.Value is "syscall2")
        {
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 2)));
        }
        else if (token.Value is "syscall3")
        {
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 3)));
        }
        else if (token.Value is "syscall4")
        {
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 4)));
        }
        else if (token.Value is "syscall5")
        {
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 5)));
        }
        else if (token.Value is "syscall6")
        {
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 6)));
        }
        else if (IsZeroTerminatedString(token, out var zerostr))
        {
            operations.Add(new Operation(OperationType.PushZeroString, token, new Meta(Text: zerostr)));
        }
        else if (IsString(token, out var str))
        {
            operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: str)));
        }
        else if (token.Value is "yes")
        {
            operations.Add(new Operation(OperationType.PushBool, token, new Meta(Bool: true)));
        }
        else if (token.Value is "no")
        {
            operations.Add(new Operation(OperationType.PushBool, token, new Meta(Bool: false)));
        }
        else if (token.Value is ":")
        {
            if (tokens.Count > 0)
            {
                throw new Exception($"Expected nothing after `:`, but got `{tokens.Peek().Value}` @ {tokens.Peek().Filename}:{tokens.Peek().Line}:{tokens.Peek().Column}");
            }
        }
        else if (token.Value is ";")
        {
            if (tokens.Count > 0)
            {
                throw new Exception($"Expected nothing after `;`, but got `{tokens.Peek().Value}` @ {tokens.Peek().Filename}:{tokens.Peek().Line}:{tokens.Peek().Column}");
            }
        }
        else if (token.Value is "yes:" or "no:")
        {

        }
        else if (token.Value is "?")
        {
            if (block.NestedBlocks.Count is 0)
            {
                throw new Exception($"Expected at least one branch block after ?, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var branch1 = block.NestedBlocks.Dequeue();
            var firstBranch1Token = branch1.Tokens.Peek()
                ?? throw new Exception($"Expected yes: or no: after ?, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            if (firstBranch1Token.Value is not "yes:" and not "no:")
            {
                throw new Exception($"Expected yes: or no: after ?, but got `{firstBranch1Token.Value}` @ {firstBranch1Token.Filename}:{firstBranch1Token.Line}:{firstBranch1Token.Column}");
            }

            var branch1Program = ParseProgram(branch1, memories, pinnedStackItems);

            var endLabel = $"end_if_{token.Line}_{token.Column}";
            var expectedBranch2Token = firstBranch1Token.Value is "yes:" ? "no:" : "yes:";

            if (block.NestedBlocks.Count is 0 || block.NestedBlocks.Peek().Tokens.Peek().Value != expectedBranch2Token)
            {
                operations.Add(new Operation(firstBranch1Token.Value is "yes:" ? OperationType.JumpIfZero : OperationType.JumpIfNotZero, token, new Meta(Text: endLabel)));
                operations.Add(new Operation(OperationType.Branch, token, new Meta(Number: 1)));
                program.NestedPrograms.Enqueue(branch1Program);
                operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
                continue;
            }

            var branch2 = block.NestedBlocks.Dequeue();
            var firstBranch2Token = branch2.Tokens.Peek();
            var branch2Program = ParseProgram(branch2, memories, pinnedStackItems);

            ParsedProgram yesBlock;
            ParsedProgram noBlock;
            Token yesToken;
            Token noToken;
            if (firstBranch1Token.Value is "yes:")
            {
                yesBlock = branch1Program;
                noBlock = branch2Program;
                yesToken = firstBranch1Token;
                noToken = firstBranch2Token;
            }
            else
            {
                yesBlock = branch2Program;
                noBlock = branch1Program;
                yesToken = firstBranch2Token;
                noToken = firstBranch1Token;
            }
            var startYesBlockLabel = $"start_yes_branch_{yesToken.Line}_{yesToken.Column}";
            operations.Add(new Operation(OperationType.JumpIfNotZero, token, new Meta(Text: startYesBlockLabel)));
            operations.Add(new Operation(OperationType.Branch, token, new Meta(Number: 2)));

            noBlock.Operations.Add(new Operation(OperationType.Jump, token, new Meta(Text: endLabel)));
            program.NestedPrograms.Enqueue(noBlock);

            yesBlock.Operations.Insert(0, new Operation(OperationType.Label, token, new Meta(Text: startYesBlockLabel)));
            program.NestedPrograms.Enqueue(yesBlock);

            operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
        }
        else if (token.Value is "while")
        {
            if (block.NestedBlocks.Count < 2)
            {
                throw new Exception($"Expected at least two blocks after while, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var conditionBlock = block.NestedBlocks.Dequeue();
            var whileBlock = block.NestedBlocks.Dequeue();

            var whileStartLabel = $"while_{token.Line}_{token.Column}";
            var whileEndLabel = $"end_while_{token.Line}_{token.Column}";
            operations.Add(new Operation(OperationType.Label, token, new Meta(Text: whileStartLabel)));
            operations.Add(new Operation(OperationType.Loop, token));

            var conditionProgram = ParseProgram(conditionBlock, memories, pinnedStackItems);
            conditionProgram.Operations.Add(new Operation(OperationType.JumpIfZero, token, new Meta(Text: whileEndLabel)));
            program.NestedPrograms.Enqueue(conditionProgram);

            var whileProgram = ParseProgram(whileBlock, memories, pinnedStackItems);
            program.NestedPrograms.Enqueue(whileProgram);
            operations.Add(new Operation(OperationType.Jump, token, new Meta(Text: whileStartLabel)));
            operations.Add(new Operation(OperationType.Label, token, new Meta(Text: whileEndLabel)));
        }
        else if (token.Value is "using")
        {
            if (block.NestedBlocks.Count < 2)
            {
                throw new Exception($"Expected at least two blocks after using, but got {block.NestedBlocks.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var assignmentBlock = block.NestedBlocks.Dequeue();
            if (assignmentBlock.Tokens.Count is 0)
            {
                throw new Exception($"Expected at least one assignment, but got none @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var assignments = new List<Token>();
            while (assignmentBlock.Tokens.Count > 0)
            {
                var assignment = assignmentBlock.Tokens.Dequeue();
                if (assignment.Value is ":")
                {
                    break;
                }
                if (keywords.Contains(assignment.Value))
                {
                    throw new Exception($"Expected identifier, but got an existing keyword {token}.");
                }
                assignments.Add(assignment);
            }
            if (assignments.Count is 0)
            {
                throw new Exception($"Expected at least one assignment, but got none @ {token.Filename}:{token.Line}:{token.Column}");
            }

            pinnedStackItems ??= new Dictionary<string, Token>();
            for (var i = assignments.Count - 1; i >= 0; i--)
            {
                if (!pinnedStackItems.TryAdd(assignments[i].Value, assignments[i]))
                {
                    var existing = pinnedStackItems[assignments[i].Value];
                    throw new Exception($"Cannot pin {assignments[i]} because it is already pinned @ {existing.Filename}:{existing.Line}:{existing.Column}");
                };
                operations.Add(new Operation(OperationType.PinStackElement, assignments[i]));
            }
            operations.Add(new Operation(OperationType.UsingBlock, token));
            var consumingBlock = block.NestedBlocks.Dequeue();
            var consumingProgram = ParseProgram(consumingBlock, memories, pinnedStackItems);
            program.NestedPrograms.Enqueue(consumingProgram);

            foreach (var assignment in assignments)
            {
                operations.Add(new Operation(OperationType.UnpinStackElement, assignment));
                pinnedStackItems.Remove(assignment.Value);
            }
        }
        else if (token.Value is "include")
        {
            var includePath = block.Tokens.Dequeue();
            if (!IsString(includePath, out var path))
            {
                throw new Exception($"Expected string after include, but got {includePath} @ {includePath.Filename}:{includePath.Line}:{includePath.Column}");
            }
            var fullPath = Path.GetFullPath(path);
            Console.WriteLine($"Including {fullPath}");
            if (inclusions.TryGetValue(fullPath, out var existingInclude))
            {
                throw new Exception($"Cannot include {path} because it is already included at {existingInclude}.");
            }
            inclusions.Add(fullPath, includePath);
            var words = LexProgram(path);
            var blocks = GroupBlock(null, words, new());

            // Extract only functions for now
            foreach (var (key, function) in blocks.Functions)
            {
                if (block.Functions.ContainsKey(key))
                {
                    continue;
                }
                block.Functions.Add(key, function);
            }
        }
        else if (token.Value is "inspect")
        {
            operations.Add(new Operation(OperationType.Inspect, token));
        }
        else
        {
            throw new Exception($"Unknown token {token}");
        }
    }

    return program;

    bool IsString(Token token, out string value)
    {
        if (token.Value.StartsWith("\"") && token.Value.EndsWith("\""))
        {
            value = token.Value[1..^1];
            return true;
        }
        value = "";
        return false;
    }

    bool IsZeroTerminatedString(Token token, out string value)
    {
        if (token.Value.StartsWith("0\"") && token.Value.EndsWith("\""))
        {
            value = token.Value[2..^1] + '\0';
            return true;
        }
        value = "";
        return false;
    }

    void ParseOperator(Token token, Operator operatorType)
    {
        var nextToken = GetNextToken($"Expected number after `{token.Value}`, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        var prasedImmediate = ParseProgram(new Block(new Queue<Token>(new[] { nextToken }), new(), block.Functions), memories, pinnedStackItems);
        operations.AddRange(prasedImmediate.Operations);
        operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: operatorType)));
    }

    Token GetNextToken(string failedMessage)
    {
        if (tokens.Count == 0)
        {
            throw new Exception(failedMessage);
        }
        return tokens?.Dequeue()
            ?? throw new Exception("Unexpected block start");
    }
}

static Queue<Token> LexProgram(string filename)
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

enum DataType
{
    Number,
    Pointer,
}

record Token(string Filename, string Value, int Line, int Column)
{
    public override string ToString() => $"`{Value}` @ {Filename}:{Line}:{Column}";
};

enum Operator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,

    Not,

    Equal,
    NotEqual,
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    StringEqual,
}

enum OperationType
{
    Print,
    PrintString,
    JumpIfZero,
    JumpIfNotZero,
    Label,
    Jump,
    Drop,
    Over,
    Swap,
    PushNumber,
    PushString,
    PushZeroString,
    PushBool,
    PushDuplicate,
    PushPinnedStackItem,
    Operator,
    UsingBlock,
    PinStackElement,
    UnpinStackElement,
    Syscall,
    AllocateMemory,
    PushMemory,
    StoreMemory,
    LoadMemory,
    LoadByte,
    Cast,
    Branch,
    Loop,
    Inline,
    Inspect,
}

record Meta(int? Number = null, string? Text = null, Operator? Operator = null, bool? Bool = null, DataType? Type = null);

record Operation(OperationType Type, Token Token, Meta? Data = null);

record ParsedProgram(List<Operation> Operations, Queue<ParsedProgram> NestedPrograms);

record Block(Queue<Token> Tokens, Queue<Block> NestedBlocks, Dictionary<string, Block> Functions);
class TypeStack : Stack<(DataType, Token)>
{
    public TypeStack() : base() { }

    // NOTE: To create a copy of a stack, we need to do it twice to get the elements back in order.
    public TypeStack(IEnumerable<(DataType, Token)> collection) : base(new Stack<(DataType, Token)>(collection)) { }

    internal (TypeStackDiff, string?) Diff(TypeStack other)
    {
        if (Count != other.Count)
        {
            return (TypeStackDiff.SizeDifference, null);
        }
        var stringBuilder = new StringBuilder();
        var result = TypeStackDiff.Equal;
        for (var i = 0; i < Count; i++)
        {
            var (type, token) = this.ElementAt(i);
            var (otherType, otherToken) = other.ElementAt(i);
            stringBuilder.AppendLine($"{i}: {type} ({token.Filename}:{token.Line}:{token.Column}) | {otherType} ({otherToken.Filename}:{otherToken.Line}:{otherToken.Column}))");
            if (type != otherType)
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

record PinnedStackItem(Token Token, DataType Type);

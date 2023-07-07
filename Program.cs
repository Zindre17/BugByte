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
    var startBlock = GroupBlock(null, words);
    var (program, typeStack) = ParseProgram(startBlock, new());
    if (typeStack.Count > 0)
    {
        throw new Exception($"The program must have an empty stack at the end. Got {typeStack.Count} items on the stack.");
    }
    GenerateAsembly(program, fileName);
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

static Block GroupBlock(Token? last, Queue<Token> tokens, string? expectedClosingTag = null)
{
    var block = new Block(new(), new());
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
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, ";"));
            }
            if (tokens.Count is 0)
            {
                continue;
            }
            if (tokens.Peek().Value == missingBranch && tokens.Count > 0)
            {
                block.NestedBlocks.Enqueue(GroupBlock(token, tokens, ";"));
            }
        }
        else if (token.Value is "while")
        {
            block.Tokens.Enqueue(token);
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, ":"));
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, ";"));
        }
        else if (token.Value is "using")
        {
            block.Tokens.Enqueue(token);
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, ":"));
            block.NestedBlocks.Enqueue(GroupBlock(token, tokens, ";"));
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

static void GenerateAsembly(ParsedProgram program, string filename)
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

            var number = operation.Data.Number
                ?? throw new Exception($"Operation was of type operator but has no value. Probably a bug in the parser. @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");

            if (op is Operator.Add)
            {
                assembly.Add(";-- add --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  add rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Subtract)
            {
                assembly.Add(";-- subtract --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  sub rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Multiply)
            {
                assembly.Add(";-- multiply --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  mul rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Divide)
            {
                assembly.Add(";-- divide --");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  div rbx");
                assembly.Add($"  push rax");
                // TODO: merge this with modulo
            }
            else if (op is Operator.Modulo)
            {
                assembly.Add(";-- modulo --");
                assembly.Add($"  xor rdx, rdx");
                assembly.Add($"  pop rax");
                assembly.Add($"  mov rbx, {number}");
                assembly.Add($"  div rbx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.Equal)
            {
                assembly.Add(";-- equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmove rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.NotEqual)
            {
                assembly.Add(";-- not equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovne rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.LessThan)
            {
                assembly.Add(";-- less than --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovl rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.LessThanOrEqual)
            {
                assembly.Add(";-- less than or equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovle rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.GreaterThan)
            {
                assembly.Add(";-- greater than --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
                assembly.Add($"  cmovg rdx, rcx");
                assembly.Add($"  push rdx");
            }
            else if (op is Operator.GreaterThanOrEqual)
            {
                assembly.Add(";-- greater than or equal --");
                assembly.Add($"  mov rcx, 1");
                assembly.Add($"  mov rdx, 0");
                assembly.Add($"  pop rax");
                assembly.Add($"  cmp rax, {number}");
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
            assembly.Add("  pop rax");
            assembly.Add("  pop rbx");
            assembly.Add("  mov [rbx], rax");
        }
        else if (operation.Type is OperationType.LoadMemory)
        {
            assembly.Add($";-- load memory --");
            assembly.Add($"  pop rax");
            assembly.Add($"  mov rax, [rax]");
            assembly.Add($"  push rax");
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
            var label = operation.Data?.Text
                ?? throw new Exception("Pin-stack-element-keyword has no name. Probably a bug in the parser.");
            var index = pinnedStackItems.Count;
            if (index is TempStackCapacity)
            {
                throw new Exception($"Too many pinned stack items. Max is {TempStackCapacity}.");
            }
            pinnedStackItems.Add(label);
            assembly.Add(";-- pin stack element --");
            assembly.Add($"  pop rax");
            assembly.Add($"  mov [r15], rax");
            assembly.Add($"  add r15, 8");
        }
        else if (operation.Type is OperationType.PushPinnedStackItem)
        {
            var label = operation.Data?.Text
                ?? throw new Exception("Push-pinned-stack-item-keyword has no name. Probably a bug in the parser.");

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

    File.WriteAllLines($"{filename.Split(".")[0]}.asm", assembly);
}

static (ParsedProgram, TypeStack) ParseProgram(Block block, TypeStack typeStack, Dictionary<string, (DataType, Token)>? pinnedStackItems = null)
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
    var operations = new List<Operation>();
    var memories = new Dictionary<string, Token>();
    var tokens = block.Tokens;
    while (tokens.Count > 0)
    {
        var token = tokens.Dequeue();
        if (pinnedStackItems is not null && pinnedStackItems.TryGetValue(token.Value, out var tuple))
        {
            operations.Add(new Operation(OperationType.PushPinnedStackItem, token, new Meta(Text: token.Value)));
            typeStack.Push((tuple.Item1, token));
        }
        else if (memories.ContainsKey(token.Value))
        {
            operations.Add(new Operation(OperationType.PushMemory, token, new Meta(Text: token.Value)));
            typeStack.Push((DataType.Pointer, token));
        }
        else if (int.TryParse(token.Value, out var value))
        {
            operations.Add(new Operation(OperationType.PushNumber, token, new Meta(Number: value)));
            typeStack.Push((DataType.Number, token));
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
            if (typeStack.Count < 2)
            {
                throw new Exception($"`==` expects at least two elements on the stack, but got {typeStack.Count}.");
            }
            var (type1, _) = typeStack.Pop();
            if (type1 is not DataType.Pointer)
            {
                throw new Exception($"`==` expects a pointer to a string on top of the stack, but got {type1}.");
            }
            var (type2, _) = typeStack.Pop();
            if (type2 is not DataType.Number)
            {
                throw new Exception($"`==` expects a number as the second element on the stack, but got {type2}.");
            }
            var nextToken = GetNextToken($"Expected string literal after `==`, but got nothing.");
            if (!IsString(nextToken, out var str))
            {
                throw new Exception($"Expected string literal after `==`, but got {nextToken}");
            }

            operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: str)));
            operations.Add(new Operation(OperationType.Operator, token, new Meta(Operator: Operator.StringEqual)));
            typeStack.Push((DataType.Number, token));
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
            if (typeStack.Count is 0)
            {
                throw new Exception($"`dup` expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            operations.Add(new Operation(OperationType.PushDuplicate, token));
            var (prevValue, _) = typeStack.Peek();
            typeStack.Push((prevValue, token));
        }
        else if (token.Value is "drop")
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"`drop` expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            typeStack.Pop();
            operations.Add(new Operation(OperationType.Drop, token));
        }
        else if (token.Value is "over")
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"`over` expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var top = typeStack.Pop();
            var (prev, _) = typeStack.Peek();
            typeStack.Push(top);
            typeStack.Push((prev, token));
            operations.Add(new Operation(OperationType.Over, token));
        }
        else if (token.Value is "swap")
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"`swap` expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, _) = typeStack.Pop();
            var (prev, _) = typeStack.Pop();
            typeStack.Push((top, token));
            typeStack.Push((prev, token));
            operations.Add(new Operation(OperationType.Swap, token));
        }
        else if (token.Value is "print")
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"`print` expected value on stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number and not DataType.Pointer)
            {
                throw new Exception($"`print` expected number or pointer on stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
            operations.Add(new Operation(OperationType.Print, token));
        }
        else if (token.Value is "prints")
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"`prints` expected at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            var (nextTop, nextTopToken) = typeStack.Pop();
            if (top is not DataType.Pointer)
            {
                throw new Exception($"`prints` expected pointer on top of stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
            if (nextTop is not DataType.Number)
            {
                throw new Exception($"`prints` expected number as second element ont the stack, but got `{nextTop}` @ {nextTopToken.Filename}:{nextTopToken.Line}:{nextTopToken.Column}");
            }
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
            if (typeStack.Count < 2)
            {
                throw new Exception($"`store` expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            var (nextTop, nextTopToken) = typeStack.Pop();
            if (nextTop is not DataType.Pointer)
            {
                throw new Exception($"`store` expects a pointer as second element ont the stack, but got `{nextTop}` @ {nextTopToken.Filename}:{nextTopToken.Line}:{nextTopToken.Column}");
            }
            operations.Add(new Operation(OperationType.StoreMemory, token));
        }
        else if (token.Value is "load")
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"`load` expects at least one value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Pointer)
            {
                throw new Exception($"`load` expects pointer on top of stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
            operations.Add(new Operation(OperationType.LoadMemory, token));
            typeStack.Push((DataType.Number, token));
        }
        else if (token.Value is "syscall0")
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"`syscall0` expects at least one value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`syscall0` expects number on top of stack, but got `{top}` from {topToken}.");
            }
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 0)));
            typeStack.Push((DataType.Number, token));
        }
        else if (token.Value is "syscall1")
        {
            if (typeStack.Count < 2)
            {
                throw new Exception($"`syscall1` expects at least two values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`syscall1` expects number on top of stack, but got `{top}` from {topToken}.");
            }
            typeStack.Pop();
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 1)));
            typeStack.Push((DataType.Number, token));
        }
        else if (token.Value is "syscall2")
        {
            if (typeStack.Count < 3)
            {
                throw new Exception($"`syscall2` expects at least three values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`syscall2` expects number on top of stack, but got `{top}` from {topToken}.");
            }
            typeStack.Pop();
            typeStack.Pop();
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 2)));
            typeStack.Push((DataType.Number, token));
        }
        else if (token.Value is "syscall3")
        {
            if (typeStack.Count < 4)
            {
                throw new Exception($"`syscall3` expects at least four values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`syscall3` expects number on top of stack, but got `{top}` from {topToken}.");
            }
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 3)));
            typeStack.Push((DataType.Number, token));
        }
        else if (token.Value is "syscall4")
        {
            if (typeStack.Count < 5)
            {
                throw new Exception($"`syscall4` expects at least four values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`syscall4` expects number on top of stack, but got `{top}` from {topToken}.");
            }
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 4)));
            typeStack.Push((DataType.Number, token));
        }
        else if (token.Value is "syscall5")
        {
            if (typeStack.Count < 6)
            {
                throw new Exception($"`syscall5` expects at least four values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`syscall5` expects number on top of stack, but got `{top}` from {topToken}.");
            }
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 5)));
            typeStack.Push((DataType.Number, token));
        }
        else if (token.Value is "syscall6")
        {
            if (typeStack.Count < 7)
            {
                throw new Exception($"`syscall6` expects at least four values on the stack, but got {typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`syscall6` expects number on top of stack, but got `{top}` from {topToken}.");
            }
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            typeStack.Pop();
            operations.Add(new Operation(OperationType.Syscall, token, new Meta(Number: 6)));
            typeStack.Push((DataType.Number, token));
        }
        else if (IsZeroTerminatedString(token, out var zerostr))
        {
            typeStack.Push((DataType.Pointer, token));
            operations.Add(new Operation(OperationType.PushZeroString, token, new Meta(Text: zerostr)));
        }
        else if (IsString(token, out var str))
        {
            typeStack.Push((DataType.Number, token));
            typeStack.Push((DataType.Pointer, token));
            operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: str)));
        }
        else if (token.Value is "yes")
        {
            typeStack.Push((DataType.Number, token));
            operations.Add(new Operation(OperationType.PushBool, token, new Meta(Bool: true)));
        }
        else if (token.Value is "no")
        {
            typeStack.Push((DataType.Number, token));
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
            if (typeStack.Count is 0)
            {
                throw new Exception($"`?` expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"`?` expects a number on the stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }

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

            var (branch1Program, branch1Stack) = ParseProgram(branch1, new(typeStack));

            var endLabel = $"end_if_{token.Line}_{token.Column}";
            if (block.NestedBlocks.Count is 0)
            {
                var (diffResult, stackDump) = typeStack.Diff(branch1Stack);
                if (diffResult is not TypeStackDiff.Equal)
                {
                    throw new Exception($"Single branch `?` block must leave the stack unchanged, but it left it in a different state @ {token.Filename}:{token.Line}:{token.Column}\n{stackDump}");
                }
                operations.Add(new Operation(firstBranch1Token.Value is "yes:" ? OperationType.JumpIfZero : OperationType.JumpIfNotZero, token, new Meta(Text: endLabel)));
                operations.AddRange(branch1Program.Operations);
                operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
                typeStack = branch1Stack;
                continue;
            }

            var expectedBranch2Token = firstBranch1Token.Value is "yes:" ? "no:" : "yes:";
            var branch2 = block.NestedBlocks.Peek();
            var firstBranch2Token = branch2.Tokens.Peek();
            if (firstBranch2Token?.Value != expectedBranch2Token)
            {
                operations.Add(new Operation(firstBranch1Token.Value is "yes:" ? OperationType.JumpIfZero : OperationType.JumpIfNotZero, token, new Meta(Text: endLabel)));
                operations.AddRange(branch1Program.Operations);
                operations.Add(new Operation(OperationType.Label, token, new Meta(Text: endLabel)));
                typeStack = branch1Stack;
                continue;
            }
            block.NestedBlocks.Dequeue();
            var (branch2Program, branch2Stack) = ParseProgram(branch2, new(typeStack));
            var (diff, msg) = branch1Stack.Diff(branch2Stack);
            if (diff is TypeStackDiff.SizeDifference)
            {
                throw new Exception($"Branches starting at {firstBranch1Token.Filename}:{firstBranch1Token.Line}:{firstBranch1Token.Column} and {firstBranch2Token.Filename}:{firstBranch2Token.Line}:{firstBranch2Token.Column} have different stack sizes.");
            }
            else if (diff is TypeStackDiff.TypeDifference)
            {
                throw new Exception($"Branches starting at {firstBranch1Token.Filename}:{firstBranch1Token.Line}:{firstBranch1Token.Column} and {firstBranch2Token.Filename}:{firstBranch2Token.Line}:{firstBranch2Token.Column} have diverging stack types:\n{msg}");
            }

            typeStack = branch2Stack;

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
            operations.AddRange(noBlock.Operations);
            operations.Add(new Operation(OperationType.Jump, token, new Meta(Text: endLabel)));
            operations.Add(new Operation(OperationType.Label, token, new Meta(Text: startYesBlockLabel)));
            operations.AddRange(yesBlock.Operations);
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

            var (conditionProgram, conditionStack) = ParseProgram(conditionBlock, new TypeStack(typeStack));
            if ((conditionStack.Count - typeStack.Count) is not 1)
            {
                throw new Exception($"Expected condition to produce a single value, but got {conditionStack.Count - typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = conditionStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"Expected condition to be a bool, but got {top} from {topToken.Filename}:{topToken.Line}:{topToken.Column}");
            }
            operations.AddRange(conditionProgram.Operations);
            operations.Add(new Operation(OperationType.JumpIfZero, token, new Meta(Text: whileEndLabel)));

            var (whileProgram, whileStack) = ParseProgram(whileBlock, new TypeStack(conditionStack));
            if (whileStack.Count != typeStack.Count)
            {
                throw new Exception($"Expected while block to produce 0 values, but got {whileStack.Count - typeStack.Count} @ {token.Filename}:{token.Line}:{token.Column}");
            }
            typeStack = whileStack;
            operations.AddRange(whileProgram.Operations);
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

            pinnedStackItems ??= new Dictionary<string, (DataType, Token)>();
            for (var i = assignments.Count - 1; i >= 0; i--)
            {
                if (!pinnedStackItems.TryAdd(assignments[i].Value, (typeStack.Pop().Item1, assignments[i])))
                {
                    var (_, existing) = pinnedStackItems[assignments[i].Value];
                    throw new Exception($"Cannot pin {assignments[i]} because it is already pinned @ {existing.Filename}:{existing.Line}:{existing.Column}");
                };
                operations.Add(new Operation(OperationType.PinStackElement, assignments[i], new Meta(Text: assignments[i].Value)));
            }

            var consumingBlock = block.NestedBlocks.Dequeue();

            var (consumingProgram, consumingStack) = ParseProgram(consumingBlock, new TypeStack(typeStack), pinnedStackItems);
            operations.AddRange(consumingProgram.Operations);
            foreach (var assignment in assignments)
            {
                operations.Add(new Operation(OperationType.UnpinStackElement, assignment));
                pinnedStackItems.Remove(assignment.Value);
            }
            typeStack = consumingStack;
        }
        else
        {
            throw new Exception($"Unknown token {token}");
        }
    }

    return (new ParsedProgram(operations), typeStack);

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
        if (typeStack.Count is 0)
        {
            throw new Exception($"`{token.Value}` expected number on stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        }

        var nextToken = GetNextToken($"Expected number after `{token.Value}`, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        if (!int.TryParse(nextToken.Value, out var operand))
        {
            throw new Exception($"Expected number after `{token.Value}`, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
        }

        var (type, _) = typeStack.Pop();
        if (type is not DataType.Number)
        {
            if (operatorType is Operator.Add or Operator.Subtract && type is DataType.Pointer)
            {
                operations.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: operatorType)));
                typeStack.Push((type, token));
                return;
            }
            throw new Exception($"`{token.Value}` expected number on stack, but got `{type}` @ {token.Filename}:{token.Line}:{token.Column}");
        }

        operations?.Add(new Operation(OperationType.Operator, token, new Meta(Number: operand, Operator: operatorType)));
        typeStack.Push((DataType.Number, token));
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
}

record Meta(int? Number = null, string? Text = null, Operator? Operator = null, bool? Bool = null);

record Operation(OperationType Type, Token Token, Meta? Data = null);

record ParsedProgram(List<Operation> Operations);

record Block(Queue<Token> Tokens, Queue<Block> NestedBlocks);

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
}

enum TypeStackDiff
{
    SizeDifference,
    TypeDifference,
    Equal,

}

record PinnedStackItem(Token Token, DataType Type);

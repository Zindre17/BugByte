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

var fileName = args[0];

var words = LexFile(fileName);

try
{
    var startBlock = GroupBlock(null, words, new(), new(), new(), new() { [Path.GetFullPath(fileName)] = new Token(fileName, "entrypoint", 0, 0) });
    var program = ParseProgram(startBlock, new());
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

RunExternalCommand($"./{fileNameWithoutExtension}", "");

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
            else if (op is Operator.Xor)
            {
                assembly.Add(";-- xor --");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  xor rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.Or)
            {
                assembly.Add(";-- or --");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  or rax, rbx");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.And)
            {
                assembly.Add(";-- and --");
                assembly.Add($"  pop rbx");
                assembly.Add($"  pop rax");
                assembly.Add($"  and rax, rbx");
                assembly.Add($"  push rax");
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
            else if (op is Operator.LeftShift)
            {
                assembly.Add(";-- left shift --");
                assembly.Add($"  pop rcx");
                assembly.Add($"  pop rax");
                assembly.Add($"  shl rax, cl");
                assembly.Add($"  push rax");
            }
            else if (op is Operator.RightShift)
            {
                assembly.Add(";-- right shift --");
                assembly.Add($"  pop rcx");
                assembly.Add($"  pop rax");
                assembly.Add($"  shr rax, cl");
                assembly.Add($"  push rax");
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
                ?? throw new Exception("Label operation contained no label. Probably a bug in the parser.");

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
            var index = pinnedStackItems.LastIndexOf(label);
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
        else if (operation.Type is OperationType.UpdatePinnedStackElement)
        {
            var label = operation.Token.Value;
            var index = pinnedStackItems.LastIndexOf(label);
            if (index == -1)
            {
                throw new Exception($"Unknown pinned stack item `{label}` @ {operation.Token.Filename}:{operation.Token.Line}:{operation.Token.Column}");
            }
            assembly.Add(";-- update pinned stack element --");
            assembly.Add($"  pop rax");
            assembly.Add($"  mov [r14 + {index * 8}], rax");
        }
        else if (operation.Type is OperationType.Exit)
        {
            assembly.Add($";-- exit --");
            assembly.Add($"  pop rdi");
            assembly.Add($"  mov rax, 60");
            assembly.Add($"  syscall");
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

static TypeStack TypeCheckProgram(ParsedProgram program, TypeStack typeStack, Dictionary<string, Stack<DataType>> pinnedStackItems)
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
            typeStack.Push((dataType.Peek(), token));
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
            if (pinnedStackItems.ContainsKey(operation.Token.Value))
            {
                pinnedStackItems[operation.Token.Value].Push(typeStack.Pop().Item1);
            }
            else
            {
                var stack = new Stack<DataType>();
                stack.Push(typeStack.Pop().Item1);
                pinnedStackItems[operation.Token.Value] = stack;

            }
        }
        else if (operation.Type is OperationType.UnpinStackElement)
        {
            var stack = pinnedStackItems[operation.Token.Value];
            if (stack.Count is 1)
            {
                pinnedStackItems.Remove(operation.Token.Value);
            }
            else
            {
                stack.Pop();
            }
        }
        else if (operation.Type is OperationType.UpdatePinnedStackElement)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"{operation.Type} expects a value on the stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
            }
            typeStack.Pop();
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
                    throw new Exception($"Operator {op} expects at least 4 elements on the stack, but got {typeStack.Count} at {token}.");
                }
                var (type1, _) = typeStack.Pop();
                if (type1 is not DataType.Pointer)
                {
                    throw new Exception($"Operator {op} expects a pointer to a string on top of the stack, but got {type1} at {token}.");
                }
                var (type2, _) = typeStack.Pop();
                if (type2 is not DataType.Number)
                {
                    throw new Exception($"Operator {op} expects a number as the second element on the stack, but got {type2} at {token}.");
                }
                var (type3, _) = typeStack.Pop();
                if (type3 is not DataType.Pointer)
                {
                    throw new Exception($"Operator {op} expects a pointer to a string as the third element on the stack, but got {type3} at {token}.");
                }
                var (type4, _) = typeStack.Pop();
                if (type4 is not DataType.Number)
                {
                    throw new Exception($"Operator {op} expects a number as the fourth element on the stack, but got {type4} at {token}.");
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
            else if (op is Operator.Multiply or Operator.Divide or Operator.Modulo or Operator.And or Operator.Or or Operator.Xor or Operator.LeftShift or Operator.RightShift)
            {
                var (top, topToken) = typeStack.Pop();
                var (nextTop, nextTopToken) = typeStack.Pop();
                if (top is DataType.Pointer || nextTop is DataType.Pointer)
                {
                    throw new Exception($"Operator `{op}` does not support pointers @ {token.Filename}:{token.Line}:{token.Column}");
                }
                typeStack.Push((DataType.Number, token));
            }
            else
            {
                throw new Exception($"Unknown operator {op} {token}");
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
        else if (operation.Type is OperationType.Exit)
        {
            if (typeStack.Count is 0)
            {
                throw new Exception($"Exit expects a value on the stack @ {token.Filename}:{token.Line}:{token.Column}");
            }
            var (top, topToken) = typeStack.Pop();
            if (top is not DataType.Number)
            {
                throw new Exception($"Exit expects a number on top of the stack, but got {topToken}");
            }
        }
        else
        {
            throw new Exception($"Unknown operation `{operation.Type}` @ {token.Filename}:{token.Line}:{token.Column}");
        }
    }
    return typeStack;
}

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

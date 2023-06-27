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
    var startBlock = GroupBlock(words);
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

static Block GroupBlock(Queue<Token> tokens, string? expectedClosingTag = null)
{
    var block = new Block(new(), new());
    while (tokens.Count > 0)
    {
        var token = tokens.Dequeue();
        if (expectedClosingTag is not null && (token.Value == expectedClosingTag))
        {
            if (block.Tokens.Count is < 2)
            {
                var openingToken = block.Tokens.Peek()
                    ?? throw new Exception("Start of block is a nested block/branch. This is most likely a bug.");

                throw new Exception($"Empty block @ {openingToken.Filename}:{openingToken.Line}:{openingToken.Column}");
            }
            block.Tokens.Enqueue(token);
            return block;
        }
        else if (token.Value is "?")
        {
            block.Tokens.Enqueue(token);
            var missingBranch = "";
            if (tokens.Peek().Value is "yes:" or "no:" && tokens.Count > 0)
            {
                missingBranch = tokens.Peek().Value is "yes:" ? "no:" : "yes:";
                block.NestedBlocks.Enqueue(GroupBlock(tokens, ";"));
            }
            if (tokens.Peek().Value == missingBranch && tokens.Count > 0)
            {
                block.NestedBlocks.Enqueue(GroupBlock(tokens, ";"));
            }
        }
        else if (token.Value is "while")
        {
            block.Tokens.Enqueue(token);
            block.NestedBlocks.Enqueue(GroupBlock(tokens, ":"));
            block.NestedBlocks.Enqueue(GroupBlock(tokens, ";"));
        }
        else
        {
            block.Tokens.Enqueue(token);
        }
    }
    if (expectedClosingTag is not null)
    {
        var openingToken = block.Tokens.Peek()
            ?? throw new Exception("Start of block is a nested block. This is most likely a bug.");
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

static (ParsedProgram, TypeStack) ParseProgram(Block block, TypeStack typeStack)
{
    var operations = new List<Operation>();
    var tokens = block.Tokens;
    while (tokens.Count > 0)
    {
        var token = tokens.Dequeue();
        if (int.TryParse(token.Value, out var value))
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
            if (top is not DataType.Number)
            {
                throw new Exception($"`print` expected number on stack, but got `{top}` @ {topToken.Filename}:{topToken.Line}:{topToken.Column}");
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
        else if (token.Value.StartsWith('"') && token.Value.EndsWith('"'))
        {
            typeStack.Push((DataType.Number, token));
            typeStack.Push((DataType.Pointer, token));
            operations.Add(new Operation(OperationType.PushString, token, new Meta(Text: token.Value[1..^1])));
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
        else
        {
            throw new Exception($"Unknown token `{token.Value}` @ {token.Filename}:{token.Line}:{token.Column}");
        }
    }

    return (new ParsedProgram(operations), typeStack);

    void ParseOperator(Token token, Operator operatorType)
    {
        if (typeStack.Count is 0)
        {
            throw new Exception($"`{token.Value}` expected number on stack, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        }
        var (type, _) = typeStack.Pop();
        if (type is not DataType.Number)
        {
            throw new Exception($"`{token.Value}` expected number on stack, but got `{type}` @ {token.Filename}:{token.Line}:{token.Column}");
        }
        var nextToken = GetNextToken($"Expected number after `{token.Value}`, but got nothing @ {token.Filename}:{token.Line}:{token.Column}");
        if (!int.TryParse(nextToken.Value, out var operand))
        {
            throw new Exception($"Expected number after `{token.Value}`, but got `{nextToken.Value}` @ {nextToken.Filename}:{nextToken.Line}:{nextToken.Column}");
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
            if (remainingLine.StartsWith('"'))
            {
                var endQuoteIndex = remainingLine.IndexOf('"', 1);
                if (endQuoteIndex == -1)
                {
                    throw new Exception($"Missing end quote for string literal `{remainingLine}` @ {filename}:{lineNr}:{currentColumn}");
                }
                var stringLiteral = remainingLine[..(endQuoteIndex + 1)];
                words.Enqueue(new Token(filename, stringLiteral, lineNr, currentColumn));
                currentColumn += stringLiteral.Length + 1;
                remainingLine = remainingLine[(endQuoteIndex + 2)..].TrimStart();
            }
            else
            {
                var split = remainingLine.Split(' ', 2);
                words.Enqueue(new Token(filename, split[0], lineNr, currentColumn));
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

enum DataType
{
    Number,
    Pointer,
}

record Token(string Filename, string Value, int Line, int Column);

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
    PushBool,
    PushDuplicate,
    Operator,
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

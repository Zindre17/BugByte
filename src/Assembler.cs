namespace BugByte;

internal interface IAssemblyContext
{
    void AddExecution(params string[] assembly);
    void AddMemory(MemoryAllocationType allocation);
    void AddString(string label, string value);
}

internal class Assembler : IAssemblyContext
{
    private readonly HashSet<string> strings = [];
    private readonly Dictionary<string, int> memories = [];
    private readonly List<string> execution = [];

    internal List<string> Assemble(IEnumerable<IProgramPiece> programPieces)
    {
        ProcessInstructions(programPieces);

        var assembledMemories = memories.Select(kvp => $"{kvp.Key}: rb {kvp.Value}");

        List<string> assembly = [
            format,
            ..entry,
            ";-- setup temp stack pointers --",
            "  mov r15, temp_stack; top of stack",
            "  mov r14, temp_stack; bottom of stack",
            ..execution,
            ..exit,
            ..functionsSection,
            ..stringsSection,
            ..strings,
            ..dataSection,
            $"temp_stack: rq {TempStackCapacity}",
            ..assembledMemories,
        ];

        return assembly;
    }

    private void ProcessInstructions(IEnumerable<IProgramPiece> programPieces)
    {
        execution.Clear();
        foreach (var programPiece in programPieces)
        {
            programPiece.Assemble(this);
        }
    }

    public void AddExecution(string[] assembly)
    {
        execution.AddRange(assembly);
    }

    public void AddString(string label, string value)
    {
        strings.Add(ToAssemblyDataString(label, value));
    }

    public void AddMemory(MemoryAllocationType allocation)
    {
        memories[allocation.GetAssemblyLabel()] = allocation.GetSize();
    }

    private const int TempStackCapacity = 1024;

    private static string ToAssemblyDataString(string label, string value)
    {
        var stringLiteral = value
           .Replace("\0", "\", 0, \"")
           .Replace("\\r", "\", 13, \"")
           .Replace("\\n", "\", 10, \"")
           .Replace("\\t", "\", 9, \"");
        return $"{label}: db \"{stringLiteral}\"";
    }

    private const string format = "format ELF64 executable 3";

    private static readonly string[] entry = [
        "segment readable executable",
        "entry start",
        "start:"
    ];

    private static readonly string[] exit = [
        ";-- Exit (success) --",
        "  mov rax, 60",
        "  mov rdi, 0",
        "  syscall"
    ];

    private static readonly string[] stringsSection = [
        ";-- Strings --",
        "segment readable",
    ];

    private static readonly string[] dataSection = [
        ";-- Data --",
        "segment readable writeable",
    ];

    private static readonly string[] functionsSection = [
        ";-- Functions --",
        "segment readable executable",
        "print:"                             ,
        "  mov     r9, -3689348814741910323" ,
        "  sub     rsp, 40"                  ,
        "  mov     BYTE [rsp+31], 10"        ,
        "  lea     rcx, [rsp+30]"            ,
        ".L2:"                               ,
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
    ];
}

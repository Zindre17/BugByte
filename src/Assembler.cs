namespace BugByte;

internal interface IAssemblyContext
{
    void AddExecution(params string[] assembly);
    void AddMemory(MemoryAllocationType allocation);
    void AddString(string label, string value);
}

internal class Assembler : IAssemblyContext
{
    public Assembler()
    {
        Assembly =
        [
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
        ];
    }

    public List<string> Assembly { get; init; }
    private readonly HashSet<string> strings = [];
    private readonly Dictionary<string, int> memories = [];

    internal void Assemble(IEnumerable<IProgramPiece> programPieces)
    {
        foreach (var programPiece in programPieces)
        {
            programPiece.Assemble(this);
        }

        // Exit
        Assembly.Add("  mov rax, 60");
        Assembly.Add("  mov rdi, 0");
        Assembly.Add("  syscall");

        Assembly.Add("segment readable");
        Assembly.AddRange(strings);

        Assembly.Add("segment readable writeable");
        Assembly.Add($"temp_stack: rq {TempStackCapacity}");

        foreach (var (name, size) in memories)
        {
            Assembly.Add($"{name}: rb {size}");
        }
    }

    public void AddExecution(string[] assembly)
    {
        Assembly.AddRange(assembly);
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
}

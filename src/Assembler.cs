namespace BugByte;

public class Assembler
{
    public Assembler()
    {
        Assembly = new()
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
    }

    public List<string> Assembly { get; init; }

    internal void Assemble(IEnumerable<IProgramPiece> programPieces, GlobalContext meta)
    {
        foreach (var programPiece in programPieces)
        {
            Add(programPiece.Assemble());
        }

        // Exit
        Assembly.Add("  mov rax, 60");
        Assembly.Add("  mov rdi, 0");
        Assembly.Add("  syscall");

        AddMeta(meta);
    }

    public void Add(string[] assembly)
    {
        Assembly.AddRange(assembly);
    }

    private const int TempStackCapacity = 1024;

    internal void AddMeta(GlobalContext meta)
    {
        Assembly.Add("segment readable");
        foreach (var literal in meta.StringLiterals.Values)
        {
            Assembly.Add(ToAssemblyDataString($"s{literal.Index}", literal.Value));
        }
        foreach (var literal in meta.NullTerminatedStringLiterals.Values)
        {
            Assembly.Add(ToAssemblyDataString($"ns{literal.Index}", literal.Value + "\0"));
        }

        Assembly.Add("segment readable writeable");
        Assembly.Add($"temp_stack: rq {TempStackCapacity}");

        foreach (var (name, size) in meta.Memory)
        {
            Assembly.Add($"{name}: rb {size}");
        }
    }

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

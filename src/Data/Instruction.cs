
namespace BugByte;

internal record Instruction(Token Token, string[] Assembly, Action<TypeStack> TypeChecker) : IProgramPiece
{
    public Instruction(Token token, string[] assembly, Contract contract) : this(token, assembly, stack => contract.TypeCheck(token, stack)) { }

    public string[] Assemble() => Assembly;
    public void TypeCheck(TypeStack stack) => TypeChecker(stack);
}

internal interface ITypeCheckable
{
    void TypeCheck(TypeStack currentStack);
}

internal interface IAssemblable
{
    string[] Assemble();
}

internal interface IProgramPiece : ITypeCheckable, IAssemblable
{
    Token Token { get; }
}

internal static class Instructions
{
    internal static Instruction Cast(Token token, Primitives pointer)
    {
        return new(token, [], stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception("Stack is empty.");
            }
            stack.Pop();
            stack.Push((pointer, token));
        });
    }

    internal static Instruction Drop(Token token)
    {
        var assembly = new[]{
          ";-- Drop --",
          "  pop rax",
        };
        return new(token, assembly, stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception($"Stack is empty ({token})");
            }
            stack.Pop();
        });
    }

    internal static Instruction Duplicate(Token token)
    {
        var assembly = new[]{
          ";-- Duplicate --",
          "  pop rax",
          "  push rax",
          "  push rax",
        };
        return new(token, assembly, stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception($"Stack is empty ({token})");
            }
            var (a, oldToken) = stack.Pop();
            stack.Push((a, oldToken));
            stack.Push((a, oldToken));
        });
    }

    internal static Instruction Exit(Token token)
    {
        var assembly = new[]{
            $";-- exit --",
            $"  pop rdi",
            $"  mov rax, 60",
            $"  syscall",
        };
        return new(token, assembly, Contract.Consumer(Primitives.Number));
    }

    internal static Instruction Jump(Token token, string startLabel)
    {
        var assembly = new[]{
            ";-- jump --",
            $"  jmp {startLabel}",
        };
        return new(token, assembly, stack => { });
    }

    internal static Instruction JumpIfNotZero(Token token, string endLabel)
    {
        var assembly = new[]{
            ";-- jump if not zero --",
            $"  pop rax",
            $"  cmp rax, 0",
            $"  jnz {endLabel}",
        };
        return new(token, assembly, Contract.Consumer(Primitives.Number));
    }

    internal static Instruction JumpIfZero(Token token, string label)
    {
        var assembly = new[]{
            ";-- jump if zero --",
            $"  pop rax",
            $"  cmp rax, 0",
            $"  jz {label}",
        };
        return new(token, assembly, Contract.Consumer(Primitives.Number));
    }

    internal static Instruction Load(Token token)
    {
        var assembly = new[]{
            ";-- Load --",
            $"  pop rax",
            $"  mov rax, [rax]",
            $"  push rax",
        };
        return new(token, assembly, stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception("Stack is empty.");
            }
            var (top, _) = stack.Pop();
            if (top is not Primitives.Pointer)
            {
                throw new Exception($"Expected pointer on the stack, but got {top} ({token})");
            }
            stack.Push((Primitives.Number, token));
        });
    }

    internal static Instruction LoadByte(Token token)
    {
        var assembly = new[]{
            $";-- load byte --",
            $"  pop rbx",
            $"  xor rax, rax",
            $"  mov al, BYTE [rbx]",
            $"  push rax",
         };
        return new(token, assembly, stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception($"Stack is empty ({token})");
            }
            var (top, origin) = stack.Pop();
            if (top is not Primitives.Pointer)
            {
                throw new Exception($"Expected pointer on the stack, but got {top} from {origin}");
            }
            stack.Push((Primitives.Number, token));
        });
    }

    internal static Instruction Over(Token token)
    {
        var assembly = new[]{
          ";-- Over --",
          "  pop rax",
          "  pop rbx",
          "  push rbx",
          "  push rax",
          "  push rbx",
        };
        return new(token, assembly, stack =>
        {
            if (stack.Count < 2)
            {
                throw new Exception($"Stack needs to contain at least two elements, but got {stack.Count}.");
            }
            var (a, at) = stack.Pop();
            var (b, bt) = stack.Pop();
            stack.Push((b, bt));
            stack.Push((a, at));
            stack.Push((b, token));
        });
    }

    internal static Instruction PinStackItem(PinnedStackItem item)
    {
        var assembly = new[]{
            ";-- pin stack element --",
            $"  pop rax",
            $"  mov [r14 + {item.Index * 8}], rax",
            // $"  add r15, 8",
        };
        return new(item.Token, assembly, stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception($"Stack is empty {item.Token}");
            }
            var (type, origin) = stack.Pop();
            item.Type = type;
        });
    }

    internal static Instruction Print(Token token)
    {
        var assembly = new[]{
          ";-- Print --",
          "  pop rdi",
          "  call print",
        };
        return new(token, assembly, stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception("Stack is empty.");
            }
            stack.Pop();
        });
    }

    internal static Instruction PrintChar(Token token)
    {
        var assembly = new[]{
            $";-- print byte --",
            $"  mov rsi, rsp",
            $"  mov rdx, 1",
            $"  mov rdi, 1",
            $"  mov rax, 1",
            $"  syscall",
            $"  pop rax",
        };
        return new(token, assembly, Contract.Consumer(Primitives.Number));
    }

    internal static Instruction PrintString(Token token)
    {
        var assembly = new[]{
            ";-- print string --",
            "  pop rsi",
            "  pop rdx",
            "  mov rdi, 1",
            "  mov rax, 1",
            "  syscall",
        };
        return new(token, assembly, Contract.Consumer(Primitives.Number, Primitives.Pointer));
    }

    internal static Instruction PushMemoryPointer(Token token, string label)
    {
        var assembly = new[]{
            ";-- push memory pointer --",
            $"  mov rax, {label}",
            $"  push rax",
        };
        return new(token, assembly, Contract.Producer(Primitives.Pointer));
    }

    internal static Instruction PushPinnedStackItem(PinnedStackItem item)
    {
        var assembly = new[]{
            ";-- push pinned stack item --",
            $"  mov rax, [r14 + {item.Index * 8}]",
            $"  push rax",
        };
        return new(item.Token, assembly, stack =>
        {
            stack.Push((item.Type, item.Token));
        });
    }

    internal static Instruction Store(Token token)
    {
        var assembly = new[]{
            ";-- Store --",
            $"  pop rax",
            $"  pop rbx",
            $"  mov [rax], rbx",
        };
        return new(token, assembly, stack =>
        {
            if (stack.Count < 2)
            {
                throw new Exception($"Stack needs to contain at least two elements, but got {stack.Count}.");
            }
            var (a, _) = stack.Pop();
            stack.Pop();
            if (a is not Primitives.Pointer)
            {
                throw new Exception($"Expected pointer on the stack, but got {a}.");
            }
        });
    }

    internal static Instruction StructFieldOffset(Token token, int offset)
    {
        var offsetInstruction = Literal.Number(token, offset);
        var addInstruction = Operations.Add(token);
        return new(token, [.. offsetInstruction.Assemble(), .. addInstruction.Assemble()], stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception("Expected at least one item on the stack, but it was empty.");
            }
            var (top, _) = stack.Peek();
            if (top is not Primitives.Pointer)
            {
                throw new Exception($"Expected pointer on the stack, but got {top}.");
            }
            offsetInstruction.TypeCheck(stack);
            addInstruction.TypeCheck(stack);
        });
    }

    internal static Instruction Swap(Token token)
    {
        var assembly = new[]{
          ";-- Swap --",
          "  pop rax",
          "  pop rbx",
          "  push rax",
          "  push rbx",
        };
        return new(token, assembly, stack =>
        {
            if (stack.Count < 2)
            {
                throw new Exception($"Swap needs at least two elements on the stack, but got {stack.Count}.");
            }
            var a = stack.Pop();
            var b = stack.Pop();
            stack.Push(a);
            stack.Push(b);
        });
    }

    internal static Instruction Syscall(int version, Token token)
    {
        var assembly = new List<string>
        {
            $";-- syscall {version} --",
            "  pop rax"
        };
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

        return new(token, [.. assembly], stack =>
        {
            if (stack.Count < version + 1)
            {
                throw new Exception($"Syscall{version} needs at least {version + 1} elements on the stack, but got {stack.Count}.");
            }
            for (var i = 0; i <= version; i++)
            {
                stack.Pop();
            }
            stack.Push((Primitives.Number, token));
        });
    }

    internal static Instruction UnpinStackItem(PinnedStackItem item)
    {
        var assembly = new[]{
            ";-- unpin stack element --",
            // $"  sub r15, 8",
        };
        return new(item.Token, assembly, stack => { });
    }

    internal static Instruction UpdatePinnedStackElement(PinnedStackItem item)
    {
        var assembly = new[]{
            ";-- update pinned stack element --",
            $"  pop rax",
            $"  mov [r14 + {item.Index * 8}], rax",
        };
        return new(item.Token, assembly, stack =>
        {
            if (stack.Count is 0)
            {
                throw new Exception("Stack is empty.");
            }
            stack.Pop();
        });
    }

    public static class Boolean
    {
        public static Instruction Yes(Token token)
        {
            var assembly = new[]{
                ";-- Yes --",
                "  mov rax, 1",
                "  push rax",
            };
            return new(token, assembly, Contract.Producer(Primitives.Number));
        }

        public static Instruction No(Token token)
        {
            var assembly = new[]{
                ";-- No --",
                "  mov rax, 0",
                "  push rax",
            };
            return new(token, assembly, Contract.Producer(Primitives.Number));
        }
    }

    public static class Literal
    {
        public static Instruction String(Token token, StringLiteral str)
        {
            var size = str.Value.Length - str.Value.Count(c => c is '\\');
            var assembly = new[]{
                $";-- String --",
                $"  mov rax, {size}",
                $"  push rax",
                $"  mov rax, s{str.Index}",
                $"  push rax",
            };
            return new(token, assembly, Contract.Producer(Primitives.Number, Primitives.Pointer));
        }

        internal static Instruction NullTerminatedString(Token token, int index)
        {
            var assembly = new[]{
                $";-- Null terminated string --",
                $"  mov rax, ns{index}",
                $"  push rax",
            };
            return new(token, assembly, Contract.Producer(Primitives.Pointer));
        }

        internal static Instruction Number(Token token, long number)
        {
            var assembly = new[]{
                $";-- Number --",
                $"  mov rax, {number}",
                $"  push rax",
            };
            return new(token, assembly, Contract.Producer(Primitives.Number));
        }
    }

    public static class Operations
    {
        private static Contract CommonOperatorContract => new([Primitives.Number, Primitives.Number], [Primitives.Number]);

        public static Instruction Add(Token token)
        {
            var assembly = new[]{
                ";-- Add --",
                "  pop rbx",
                "  pop rax",
                "  add rax, rbx",
                "  push rax"
            };
            return new(token, assembly, stack =>
            {
                if (stack.Count < 2)
                {
                    throw new Exception($"Add needs at least two elements on the stack, but got {stack.Count}.");
                }
                var (a, _) = stack.Pop();
                var (b, _) = stack.Pop();
                if (a is not Primitives.Number && b is not Primitives.Number)
                {
                    throw new Exception($"Expected at least one of the top two elements on the stack to be {Primitives.Number}, but got {a} and {b}.");
                }
                if (a is Primitives.Pointer || b is Primitives.Pointer)
                {
                    stack.Push((Primitives.Pointer, token));
                }
                else
                {
                    stack.Push((Primitives.Number, token));
                }
            });
        }

        public static Instruction Subtract(Token token)
        {
            var assembly = new[]{
                ";-- Subtract --",
                "  pop rbx",
                "  pop rax",
                "  sub rax, rbx",
                "  push rax"
            };
            return new(token, assembly, stack =>
            {
                if (stack.Count < 2)
                {
                    throw new Exception($"Add needs at least two elements on the stack, but got {stack.Count}.");
                }
                var (a, _) = stack.Pop();
                var (b, _) = stack.Pop();

                if (a is Primitives.Pointer && b is Primitives.Pointer)
                {
                    stack.Push((Primitives.Number, token));
                }
                else if (a is Primitives.Pointer || b is Primitives.Pointer)
                {
                    stack.Push((Primitives.Pointer, token));
                }
                else
                {
                    stack.Push((Primitives.Number, token));
                }
            });
        }

        internal static Instruction And(Token token)
        {
            var assembly = new[]{
                ";-- and --",
                $"  pop rax",
                $"  pop rbx",
                $"  and rax, rbx",
                $"  push rax",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction Divide(Token token)
        {
            var assembly = new[]{
                ";-- Divide --",
                "  pop rbx",
                "  pop rax",
                "  div rbx",
                "  push rax"
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction Equal(Token token)
        {
            var assembly = new[]{
                ";-- equal --",
                $"  mov rcx, 1",
                $"  mov rdx, 0",
                $"  pop rbx",
                $"  pop rax",
                $"  cmp rax, rbx",
                $"  cmove rdx, rcx",
                $"  push rdx",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction GreaterThan(Token token)
        {
            var assembly = new[]{
                ";-- greater than --",
                $"  mov rcx, 1",
                $"  mov rdx, 0",
                $"  pop rbx",
                $"  pop rax",
                $"  cmp rax, rbx",
                $"  cmovg rdx, rcx",
                $"  push rdx",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction GreaterThanOrEqual(Token token)
        {
            var assembly = new[]{
                ";-- greater than or equal --",
                $"  mov rcx, 1",
                $"  mov rdx, 0",
                $"  pop rbx",
                $"  pop rax",
                $"  cmp rax, rbx",
                $"  cmovge rdx, rcx",
                $"  push rdx",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction LessThan(Token token)
        {
            var assembly = new[]{
                ";-- less than --",
                $"  mov rcx, 1",
                $"  mov rdx, 0",
                $"  pop rbx",
                $"  pop rax",
                $"  cmp rax, rbx",
                $"  cmovl rdx, rcx",
                $"  push rdx",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction LessThanOrEqual(Token token)
        {
            var assembly = new[]{
                ";-- less than or equal --",
                $"  mov rcx, 1",
                $"  mov rdx, 0",
                $"  pop rbx",
                $"  pop rax",
                $"  cmp rax, rbx",
                $"  cmovle rdx, rcx",
                $"  push rdx",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction Modulo(Token token)
        {
            var assembly = new[]{
                ";-- Modulo --",
                "  xor rdx, rdx",
                "  pop rbx",
                "  pop rax",
                "  div rbx",
                "  push rdx"
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction Multiply(Token token)
        {
            var assembly = new[]{
                ";-- Multiply --",
                "  pop rbx",
                "  pop rax",
                "  mul rbx",
                "  push rax"
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction NotEqual(Token token)
        {
            var assembly = new[]{
                ";-- not equal --",
                $"  mov rcx, 1",
                $"  mov rdx, 0",
                $"  pop rbx",
                $"  pop rax",
                $"  cmp rax, rbx",
                $"  cmovne rdx, rcx",
                $"  push rdx",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction Or(Token token)
        {
            var assembly = new[]{
                ";-- or --",
                $"  pop rax",
                $"  pop rbx",
                $"  or rax, rbx",
                $"  push rax",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction ShiftLeft(Token token)
        {
            var assembly = new[]{
                ";-- shift left --",
                $"  pop rcx",
                $"  pop rax",
                $"  shl rax, cl",
                $"  push rax",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction ShiftRight(Token token)
        {
            var assembly = new[]{
                ";-- shift right --",
                $"  pop rcx",
                $"  pop rax",
                $"  shr rax, cl",
                $"  push rax",
            };
            return new(token, assembly, CommonOperatorContract);
        }

        internal static Instruction StringEqual(Token token)
        {
            var assembly = new[]{
                ";-- string equal --",
                $"  pop rcx",
                $"  pop r8",
                $"  pop rdx",
                $"  pop r9",
                $"  cmp r8, r9",
                $"  jne .string_not_equal",
                $"  cmp r8, 0",
                $"  je .string_equal",
                $".string_check_loop:",
                $"  mov al, [rcx]",
                $"  cmp al, [rdx]",
                $"  jne .string_not_equal",
                $"  dec r8",
                $"  cmp r8, 0",
                $"  je .string_equal",
                $"  inc rcx",
                $"  inc rdx",
                $"  jmp .string_check_loop",
                $".string_equal:",
                $"  mov rax, 1",
                $"  push rax",
                $"  jmp .string_equal_end",
                $".string_not_equal:",
                $"  mov rax, 0",
                $"  push rax",
                $".string_equal_end:",
            };
            return new(token, assembly, Contract.Consumer(Primitives.Number, Primitives.Pointer, Primitives.Number, Primitives.Pointer) with { Out = [Primitives.Number] });
        }

        internal static IProgramPiece Xor(Token token)
        {
            throw new NotImplementedException();
        }
    }
}

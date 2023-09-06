namespace BugByte;

internal record Branching(Token Token, List<IProgramPiece>? YesBranch, List<IProgramPiece>? NoBranch) : IProgramPiece
{
    private static string EndLabel => $"eb_{Guid.NewGuid():N}";

    public string[] Assemble()
    {
        var assembly = new List<string>();
        var endLabel = EndLabel;
        var noLabel = $"nb_{Guid.NewGuid():N}";
        if (YesBranch is null)
        {
            assembly.AddRange(Instructions.JumpIfNotZero(Token, endLabel).Assemble());
            assembly.AddRange(NoBranch?.SelectMany(i => i.Assemble()) ?? throw new Exception($"Branching has no branches. {Token}"));
        }
        else if (NoBranch is null)
        {
            assembly.AddRange(Instructions.JumpIfZero(Token, endLabel).Assemble());
            assembly.AddRange(YesBranch.SelectMany(i => i.Assemble()));
        }
        else
        {
            assembly.AddRange(Instructions.JumpIfZero(Token, noLabel).Assemble());
            assembly.AddRange(YesBranch.SelectMany(i => i.Assemble()));
            assembly.AddRange(Instructions.Jump(Token, endLabel).Assemble());
            assembly.Add($"{noLabel}:");
            assembly.AddRange(NoBranch.SelectMany(i => i.Assemble()));
        }
        assembly.Add($"{endLabel}:");

        return assembly.ToArray();
    }

    public void TypeCheck(TypeStack currentStack)
    {
        if (currentStack.Count is 0)
        {
            throw new Exception($"Expected at least one element on the stack, but got nothing.");
        }
        currentStack.Pop();
        var cloneStack = new TypeStack(currentStack);
        if (YesBranch is null)
        {
            foreach (var piece in NoBranch!)
            {
                piece.TypeCheck(currentStack);
            }
            var (diff, msg) = currentStack.Diff(cloneStack);
            if (diff is not TypeStackDiff.Equal)
            {
                throw new Exception($"Single branched `?` ({NoBranch.First().Token}) cannot alter stack. Size {currentStack.Count} vs {cloneStack.Count}. Diff: {msg}");
            }
        }
        else if (NoBranch is null)
        {
            foreach (var piece in YesBranch)
            {
                piece.TypeCheck(currentStack);
            }
            var (diff, msg) = currentStack.Diff(cloneStack);
            if (diff is not TypeStackDiff.Equal)
            {
                throw new Exception($"Single branched `?` ({YesBranch.First().Token}) cannot alter stack. Size {currentStack.Count} vs {cloneStack.Count}. Diff: {msg}");
            }
        }
        else
        {
            var otherCurrentStack = new TypeStack(currentStack);
            foreach (var piece in YesBranch)
            {
                piece.TypeCheck(currentStack);
            }
            foreach (var piece in NoBranch)
            {
                piece.TypeCheck(otherCurrentStack);
            }
            var (diff, msg) = currentStack.Diff(otherCurrentStack);
            if (diff is not TypeStackDiff.Equal)
            {
                throw new Exception($"Branches must produce the same stack. Size {currentStack.Count} vs {otherCurrentStack.Count}. Diff: {msg}");
            }
        }
    }
}

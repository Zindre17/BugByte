namespace BugByte;

internal record Branching(Token Token, List<IProgramPiece>? YesBranch, List<IProgramPiece>? NoBranch) : IProgramPiece
{
    private static string EndLabel => $"eb_{Guid.NewGuid():N}";

    public void Assemble(IAssemblyContext context)
    {
        var endLabel = EndLabel;
        var noLabel = $"nb_{Guid.NewGuid():N}";
        if (YesBranch is null)
        {
            Instructions.JumpIfNotZero(Token, endLabel).Assemble(context);
            if (NoBranch is null)
            {
                throw new Exception($"Branching has no branches. {Token}");
            }
            NoBranch.ForEach(i => i.Assemble(context));
        }
        else if (NoBranch is null)
        {
            Instructions.JumpIfZero(Token, endLabel).Assemble(context);
            YesBranch.ForEach(i => i.Assemble(context));
        }
        else
        {
            Instructions.JumpIfZero(Token, noLabel).Assemble(context);
            YesBranch.ForEach(i => i.Assemble(context));
            Instructions.Jump(Token, endLabel).Assemble(context);
            context.AddExecution($"{noLabel}:");
            NoBranch.ForEach(i => i.Assemble(context));
        }
        context.AddExecution($"{endLabel}:");
    }

    public void TypeCheck(TypeStack currentStack, Dictionary<string, Stack<Primitives>> runtimePins)
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
                piece.TypeCheck(currentStack, runtimePins);
            }
            var (diff, msg) = currentStack.Diff(cloneStack);
            if (diff is not TypeStackDiff.Equal)
            {
                throw new Exception($"Single branched `?` ({NoBranch.First().Token}) cannot alter stack. Size {currentStack.Count} vs {cloneStack.Count}.\nDiff:\n{msg}");
            }
        }
        else if (NoBranch is null)
        {
            foreach (var piece in YesBranch)
            {
                piece.TypeCheck(currentStack, runtimePins);
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
                piece.TypeCheck(currentStack, runtimePins);
            }
            foreach (var piece in NoBranch)
            {
                piece.TypeCheck(otherCurrentStack, runtimePins);
            }
            var (diff, msg) = currentStack.Diff(otherCurrentStack);
            if (diff is not TypeStackDiff.Equal)
            {
                throw new Exception($"Branches from {Token} must produce the same stack. Size {currentStack.Count}(yes) vs {otherCurrentStack.Count}(no). Diff: {msg}");
            }
        }
    }
}

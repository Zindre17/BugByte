namespace BugByte;

internal record Loop(Token Token, PinnedStackItem Iterator, List<IProgramPiece> Condition, List<IProgramPiece> Body) : IProgramPiece
{
    private static string StartLabel => $"ls_{Guid.NewGuid():N}";
    private static string EndLabel => $"le_{Guid.NewGuid():N}";

    public string[] Assemble()
    {
        var assembly = new List<string>();

        var startLabel = StartLabel;
        var endLabel = EndLabel;

        assembly.AddRange(Instructions.PinStackItem(Iterator).Assemble());

        assembly.Add($"{startLabel}:");

        assembly.AddRange(Instructions.PushPinnedStackItem(Iterator).Assemble());
        assembly.AddRange(Condition.SelectMany(i => i.Assemble()));
        assembly.AddRange(Instructions.JumpIfZero(Token, endLabel).Assemble());

        assembly.AddRange(Body.SelectMany(i => i.Assemble()));
        assembly.AddRange(Instructions.UpdatePinnedStackElement(Iterator).Assemble());
        assembly.AddRange(Instructions.Jump(Token, startLabel).Assemble());

        assembly.Add($"{endLabel}:");

        assembly.AddRange(Instructions.PushPinnedStackItem(Iterator).Assemble());
        assembly.AddRange(Instructions.UnpinStackItem(Iterator).Assemble());

        return assembly.ToArray();
    }

    public void TypeCheck(TypeStack currentStack)
    {
        if (currentStack.Count is 0)
        {
            throw new Exception($"Expected at least one element on the stack, but got nothing.");
        }
        var prevStack = new TypeStack(currentStack);
        Instructions.PinStackItem(Iterator).TypeCheck(currentStack);

        var cloneStack = new TypeStack(currentStack);

        Instructions.PushPinnedStackItem(Iterator).TypeCheck(currentStack);
        foreach (var piece in Condition)
        {
            piece.TypeCheck(currentStack);
        }
        Instructions.JumpIfZero(Token, EndLabel).TypeCheck(currentStack);
        var (diff, msg) = currentStack.Diff(cloneStack);
        if (diff is not TypeStackDiff.Equal)
        {
            throw new Exception($"Loop condition ({Token})cannot alter stack. Size {currentStack.Count} vs {cloneStack.Count}.\nDiff:\n{msg}");
        }

        foreach (var piece in Body)
        {
            piece.TypeCheck(currentStack);
        }
        (diff, msg) = currentStack.Diff(prevStack);
        if (diff is not TypeStackDiff.Equal)
        {
            throw new Exception($"Loop body ({Token}) cannot alter stack. Size {currentStack.Count} vs {prevStack.Count}.\nDiff:\n{msg}");
        }
        Instructions.UpdatePinnedStackElement(Iterator).TypeCheck(currentStack);

        Instructions.PushPinnedStackItem(Iterator).TypeCheck(currentStack);
    }
}

namespace BugByte;

internal record Loop(Token Token, IPinnedStackItem Iterator, List<IProgramPiece> Condition, List<IProgramPiece> Body) : IProgramPiece
{
    private static string StartLabel => $"ls_{Guid.NewGuid():N}";
    private static string EndLabel => $"le_{Guid.NewGuid():N}";

    public void Assemble(IAssemblyContext context)
    {
        var startLabel = StartLabel;
        var endLabel = EndLabel;

        Instructions.PinStackItem(Iterator).Assemble(context);

        context.AddExecution($"{startLabel}:");

        Instructions.PushPinnedStackItem(Iterator).Assemble(context);
        Condition.ForEach(i => i.Assemble(context));
        Instructions.JumpIfZero(Token, endLabel).Assemble(context);

        Body.ForEach(i => i.Assemble(context));
        Instructions.UpdatePinnedStackElement(Iterator).Assemble(context);
        Instructions.Jump(Token, startLabel).Assemble(context);

        context.AddExecution($"{endLabel}:");

        Instructions.PushPinnedStackItem(Iterator).Assemble(context);
        Instructions.UnpinStackItem(Iterator).Assemble(context);
    }

    public void TypeCheck(TypeStack currentStack, Dictionary<string, Stack<Primitives>> runtimePins)
    {
        if (currentStack.Count is 0)
        {
            throw new Exception($"Expected at least one element on the stack, but got nothing.");
        }
        var prevStack = new TypeStack(currentStack);
        Instructions.PinStackItem(Iterator).TypeCheck(currentStack, runtimePins);

        var cloneStack = new TypeStack(currentStack);

        Instructions.PushPinnedStackItem(Iterator).TypeCheck(currentStack, runtimePins);
        foreach (var piece in Condition)
        {
            piece.TypeCheck(currentStack, runtimePins);
        }
        Instructions.JumpIfZero(Token, EndLabel).TypeCheck(currentStack, runtimePins);
        var (diff, msg) = currentStack.Diff(cloneStack);
        if (diff is not TypeStackDiff.Equal)
        {
            throw new Exception($"Loop condition ({Token})cannot alter stack. Size {currentStack.Count} vs {cloneStack.Count}.\nDiff:\n{msg}");
        }

        foreach (var piece in Body)
        {
            piece.TypeCheck(currentStack, runtimePins);
        }
        (diff, msg) = currentStack.Diff(prevStack);
        if (diff is not TypeStackDiff.Equal)
        {
            throw new Exception($"Loop body ({Token}) cannot alter stack. Size {currentStack.Count} vs {prevStack.Count}.\nDiff:\n{msg}");
        }
        Instructions.UpdatePinnedStackElement(Iterator).TypeCheck(currentStack, runtimePins);

        Instructions.PushPinnedStackItem(Iterator).TypeCheck(currentStack, runtimePins);
    }
}

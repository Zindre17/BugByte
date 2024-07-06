namespace BugByte;

internal record Contract(TypingType[] In, TypingType[] Out)
{
    public Contract(Primitives[] @in, Primitives[] @out) : this(@in.Select(Typing.Create).ToArray(), @out.Select(Typing.Create).ToArray()) { }

    public static Contract Producer(params Primitives[] outTypes) => new([], outTypes.Select(Typing.Create).ToArray());
    public static Contract Producer(params TypingType[] outTypes) => new([], outTypes);
    public static Contract Consumer(params Primitives[] inTypes) => new(inTypes.Select(Typing.Create).ToArray(), []);
    public static Contract Consumer(params TypingType[] inTypes) => new(inTypes, []);

    public void TypeCheck(Token token, TypeStack stack)
    {
        if (stack.Count < In.Decompose().Length)
        {
            throw new Exception($"Not enough elements on the stack ({token}). Expected {In.Length} elements, but got {stack.Count}.");
        }
        foreach (var expected in In.Decompose().Reverse())
        {
            var (actual, actualToken) = stack.Pop();
            if (actual != expected && !(actual.IsPointer() && expected.IsPointer()))
            {
                throw new Exception($"Type mismatch: `{actual}`({actualToken}) != `{expected}` for {token}\n{stack}");
            }
        }
        foreach (var type in Out.Decompose())
        {
            stack.Push((type, token));
        }
    }

    public bool IsEmpty => In.Length is 0 && Out.Length is 0;

    public Contract JoinInto(Contract next)
    {
        var _in = In.ToList();
        var _out = Out.ToList();
        if (next.IsEmpty)
        {
            return new Contract([.. _in], [.. _out]);
        }
        else if (next.In.Length is 0)
        {
            _out.AddRange(next.Out);
            return new Contract([.. _in], [.. _out]);
        }
        else
        {
            var nextIns = new Stack<TypingType>(next.In);
            var prevOuts = new Stack<TypingType>(_out);
            while (nextIns.Count > 0)
            {
                var nextIn = nextIns.Pop();
                if (prevOuts.Count is 0)
                {
                    _in.Insert(0, nextIn);
                }
                else
                {
                    var prevOut = prevOuts.Pop();
                    if (nextIn != prevOut)
                    {
                        throw new Exception($"Type mismatch: `{nextIn}` != `{prevOut}`");
                    }
                }
            }

            return new Contract([.. _in], [.. prevOuts.Reverse(), .. next.Out]);
        }
    }

    public virtual bool Equals(Contract? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        var ins = In.Decompose();
        var otherIns = other.In.Decompose();
        var outs = Out.Decompose();
        var otherOuts = other.Out.Decompose();
        if (ins.Length != otherIns.Length || outs.Length != otherOuts.Length)
        {
            return false;
        }
        return Enumerable.Range(0, ins.Length).All(i => ins[i] == otherIns[i])
            && Enumerable.Range(0, outs.Length).All(i => outs[i] == otherOuts[i]);
    }
}

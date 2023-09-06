namespace BugByte;

internal record Contract(DataType[] In, DataType[] Out)
{
    public Contract() : this(Array.Empty<DataType>(), Array.Empty<DataType>()) { }

    public static Contract Producer(params DataType[] outTypes) => new(Array.Empty<DataType>(), outTypes);
    public static Contract Consumer(params DataType[] inTypes) => new(inTypes, Array.Empty<DataType>());

    public void TypeCheck(Token token, TypeStack stack)
    {
        if (stack.Count < In.Length)
        {
            throw new Exception($"Not enough elements on the stack. Expected {In.Length} elements, but got {stack.Count}.");
        }
        for (var index = In.Length - 1; index >= 0; index--)
        {
            var expected = In[index];
            var (actual, actualToken) = stack.Pop();
            if (actual != expected)
            {
                throw new Exception($"Type mismatch: `{actual}`({actualToken}) != `{expected}` for {token}\n{stack}");
            }
        }
        foreach (var type in Out)
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
            return new Contract(_in.ToArray(), _out.ToArray());
        }
        else if (next.In.Length is 0)
        {
            _out.AddRange(next.Out);
            return new Contract(_in.ToArray(), _out.ToArray());
        }
        else
        {
            var nextIns = new Stack<DataType>(next.In);
            var prevOuts = new Stack<DataType>(_out);
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

            return new Contract(_in.ToArray(), prevOuts.Reverse().Concat(next.Out).ToArray());
        }
    }
}

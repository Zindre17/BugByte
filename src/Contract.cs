namespace BugByte;

internal record Contract(DataType[] In, DataType[] Out)
{
    public Contract() : this(Array.Empty<DataType>(), Array.Empty<DataType>()) { }

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

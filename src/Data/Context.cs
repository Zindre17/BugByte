namespace BugByte;

internal record Context
{
    public Context(Context parent)
    {
        Functions = new(parent.Functions);
        Constants = new(parent.Constants);
        Structures = new(parent.Structures);
        Name = "";
        Memory = new();
        Parent = parent;
    }

    private Context? Parent { get; } = null;

    public string Name { get; set; } = "";

    public Dictionary<string, Token> Memory { get; } = new();
    private Dictionary<string, FunctionMeta> Functions { get; } = new();
    private Dictionary<string, Constant> Constants { get; } = new();
    private Dictionary<string, Structure> Structures { get; } = new();

    public void AddFunction(FunctionMeta function)
    {
        if (!Functions.TryAdd(function.Name.Value, function))
        {
            throw new Exception($"Duplicate function {function.Name} and {Functions[function.Name.Value].Name}.");
        }
    }

    public void AddConstant(Constant constant)
    {
        if (!Constants.TryAdd(constant.Name, constant))
        {
            throw new Exception($"Duplicate constant {constant.Name} at {constant.Token} and {Constants[constant.Name].Token}");
        }
    }

    public void AddStructure(Structure structure)
    {
        if (!Structures.TryAdd(structure.Name, structure))
        {
            throw new Exception($"Duplicate structure {structure.Name} at {structure.Token} and {Structures[structure.Name].Token}.");
        }
    }

    public void Merge(Context other)
    {
        foreach (var (_, value) in other.Functions)
        {
            AddFunction(value);
        }
        foreach (var (_, value) in other.Constants)
        {
            AddConstant(value);
        }
        foreach (var (_, value) in other.Structures)
        {
            AddStructure(value);
        }
    }

    private string GenerateMemoryLabel(string label)
    {
        return $"{Name.Replace('-', '_')}_{label}";
    }

    internal string AddMemory(Token label)
    {
        if (Memory.TryGetValue(label.Value, out var token))
        {
            if (token != label)
            {
                throw new Exception($"Duplicate memory label {label} and {token}.");
            }
        }
        Memory.TryAdd(label.Value, label);
        return GenerateMemoryLabel(label.Value);
    }

    internal bool TryGetMemory(Token label, out string memory)
    {
        if (Memory.ContainsKey(label.Value))
        {
            memory = GenerateMemoryLabel(label.Value);
            return true;
        }
        memory = "";
        return false;
    }

    internal bool TryGetFunction(string value, out FunctionMeta func)
    {
        if (Functions.TryGetValue(value, out var function))
        {
            func = function;
            return true;
        }
        if (Parent is not null)
        {
            return Parent.TryGetFunction(value, out func);
        }
        func = null!;
        return false;
    }

    internal bool TryGetConstant(string value, out Constant constant)
    {
        if (Constants.TryGetValue(value, out var constant2))
        {
            constant = constant2;
            return true;
        }
        if (Parent is not null)
        {
            return Parent.TryGetConstant(value, out constant);
        }
        constant = null!;
        return false;
    }

    internal bool TryGetStructure(string value, out Structure structure)
    {
        if (Structures.TryGetValue(value, out var structure2))
        {
            structure = structure2;
            return true;
        }
        if (Parent is not null)
        {
            return Parent.TryGetStructure(value, out structure);
        }
        structure = null!;
        return false;
    }
}

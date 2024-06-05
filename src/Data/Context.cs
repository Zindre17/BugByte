namespace BugByte;

internal record Context
{
    public Context(Context parent)
    {
        Functions = new(parent.Functions);
        Constants = new(parent.Constants);
        Structures = new(parent.Structures);
        Name = "";
        Memory = [];
        Parent = parent;
    }

    private Context? Parent { get; } = null;

    public string Name { get; set; } = "";

    public Dictionary<string, Token> Memory { get; } = [];
    private Dictionary<string, FunctionMeta> Functions { get; } = [];
    private Dictionary<string, Constant> Constants { get; } = [];
    private Dictionary<string, Structure> Structures { get; } = [];
    private static readonly Dictionary<string, Structure> builtInStructures = new()
    {
        [Structure.String.Name] = Structure.String,
        [Structure.ZeroTerminatedString.Name] = Structure.ZeroTerminatedString,
    };

    public bool IsReserved(string name)
    {
        if (Memory.ContainsKey(name) || Functions.ContainsKey(name) || Constants.ContainsKey(name))
        {
            return true;
        }

        if (Parent is null)
        {
            return Tokens.IsReserved(name);
        }
        return false;
    }

    public void AddFunction(FunctionMeta function)
    {
        if (!Functions.TryAdd(function.Name.Word.Value, function))
        {
            throw new Exception($"Duplicate function {function.Name} and {Functions[function.Name.Word.Value].Name}.");
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
        if (builtInStructures.TryGetValue(structure.Name, out var builtIn))
        {
            throw new Exception($"Cannot redefine built-in structure {structure.Name} at {structure.Token}.");
        }
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
        return $"{Name.Replace('-', '_')}_{label.Replace('-', '_')}";
    }

    internal string AddMemory(Token label)
    {
        if (Memory.TryGetValue(label.Word.Value, out var token))
        {
            if (token != label)
            {
                throw new Exception($"Duplicate memory label {label} and {token}.");
            }
            return GenerateMemoryLabel(label.Word.Value);
        }

        if (IsReserved(label.Word.Value))
        {
            throw new Exception($"Cannot use reserved keyword {label} as a memory label.");
        }

        Memory.TryAdd(label.Word.Value, label);
        return GenerateMemoryLabel(label.Word.Value);
    }

    internal bool TryGetMemory(Token label, out string memory)
    {
        if (Memory.ContainsKey(label.Word.Value))
        {
            memory = GenerateMemoryLabel(label.Word.Value);
            return true;
        }
        else if (Parent is not null)
        {
            return Parent.TryGetMemory(label, out memory);
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
        if (builtInStructures.TryGetValue(value, out var builtIn))
        {
            structure = builtIn;
            return true;
        }
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

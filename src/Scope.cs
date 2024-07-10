namespace BugByte;

interface IScope
{
    Definitions Definitions { get; }
    Dictionary<string, IScopedPin> Pins { get; }
};

interface IScopedPin
{
    IScope Scope { get; }
    string Name { get; }
    IPinnedStackItem GetPinInfo();
    void Unpin();
}

internal record struct ScopedPinData(IScope Scope, IPinnedStackItem Item) : IScopedPin
{
    public readonly string Name => Item.Token.Word.Value;

    public readonly IPinnedStackItem GetPinInfo() => Item;

    public readonly void Unpin()
    {
        Scope.Pins.Remove(Name);
        Item.Unpin();
    }
}

internal record struct GlobalScope(Definitions Definitions, Dictionary<string, IScopedPin> Pins) : IScope;
internal record struct NestedScope(string Name, Definitions Definitions, Dictionary<string, IScopedPin> Pins, IScope Parent) : IScope;

internal static class Scope
{
    public static IScope Create() => new GlobalScope(new("global"), []);

    public static IScope Create(IScope parentScope, string name) => new NestedScope(name, new(name), [], parentScope);

    public static string GetScopeName(this IScope scope) => scope switch
    {
        GlobalScope => "global",
        NestedScope nested => $"{GetScopeName(nested.Parent)}_{nested.Name}",
        _ => throw new Exception("Unknown scope type."),
    };

    public static IScopedPin Pin(this IScope scope, Token token, TypingType type)
    {
        if (scope.Pins.ContainsKey(token.Word.Value))
        {
            throw new Exception($"Already exists a pin with the same name in the current sopce {scope.GetScopeName()}");
        }
        var pin = new ScopedPinData(scope, PinnedStackItem.Create(token, type));
        scope.Pins.Add(token.Word.Value, pin);
        return pin;
    }

    public static bool TryGetPin(this IScope scope, string name, out IScopedPin pinnedStackItem)
    {
        if (scope.Pins.TryGetValue(name, out pinnedStackItem!))
        {
            return true;
        }
        return scope switch
        {
            GlobalScope => false,
            NestedScope ns => ns.Parent.TryGetPin(name, out pinnedStackItem),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }

    public static bool TryGetMemory(this IScope scope, string name, out MemoryAllocationType memoryAllocation)
    {
        if (scope.Definitions.TryGetMemory(name, out memoryAllocation))
        {
            return true;
        }
        return scope switch
        {
            GlobalScope => false,
            NestedScope ns => ns.Parent.TryGetMemory(name, out memoryAllocation),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }

    public static bool TryGetConstant(this IScope scope, string name, out IIdentifiedDefinition<Constant> constantDefinition)
    {
        if (scope.Definitions.TryGetConstant(name, out constantDefinition))
        {
            return true;
        }
        return scope switch
        {
            GlobalScope => false,
            NestedScope ns => ns.Parent.TryGetConstant(name, out constantDefinition),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }

    public static bool TryGetFunction(this IScope scope, string name, out IIdentifiedDefinition<Function> functionDefinition)
    {
        if (scope.Definitions.TryGetFunction(name, out functionDefinition))
        {
            return true;
        }
        return scope switch
        {
            GlobalScope => false,
            NestedScope ns => ns.Parent.TryGetFunction(name, out functionDefinition),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }

    public static bool TryGetStructure(this IScope scope, string name, out IIdentifiedDefinition<Structure> structureDefinition)
    {
        if (scope.Definitions.TryGetStructure(name, out structureDefinition))
        {
            return true;
        }
        return scope switch
        {
            GlobalScope => false,
            NestedScope ns => ns.Parent.TryGetStructure(name, out structureDefinition),
            _ => throw new ArgumentOutOfRangeException(nameof(scope))
        };
    }
}

namespace BugByte;

internal static class Typing
{
    internal static TypingType Create(Primitives dataType) => new PrimitiveType(dataType);
    internal static TypingType Create(Structure structure) => structure.Fields.Count is 1 ? new PrimitiveType(structure.Fields.First().Value.Type) : new ComplexType(structure);
    internal static TypingType CreatePointer(TypingType innerType) => new PointerType(innerType);

    internal static bool IsPointer(this TypingType type) => type switch
    {
        PointerType _ => true,
        PrimitiveType primitive => primitive.DataType is Primitives.Pointer,
        _ => false,
    };

    internal static TypingType GetInnerType(this TypingType type) => type switch
    {
        PointerType pointer => pointer.InnerType,
        PrimitiveType primitive => primitive.DataType switch
        {
            Primitives.Pointer => Create(Primitives.Number),
            _ => throw new Exception("Not a pointer."),
        },
        _ => throw new Exception("Not a pointer."),
    };

    internal static int GetSize(this TypingType type) => type switch
    {
        PrimitiveType primitive => primitive.DataType switch
        {
            Primitives.Number => 8,
            Primitives.Pointer => 8,
            Primitives.Runtime => 8,
            _ => throw new Exception($"Unknown data type {primitive.DataType}."),
        },
        ComplexType complex => complex.Structure.Size,
        _ => throw new Exception("Unknown type."),
    };


    internal static StructureField GetField(this TypingType type, string fieldName) => type switch
    {
        ComplexType complex => complex.Structure.Fields[fieldName],
        _ => throw new Exception("Primitives have no fields."),
    };

    internal static TypingType Add(this TypingType type, TypingType other)
    {
        if (type is ComplexType || other is ComplexType)
        {
            throw new Exception("Expected both types to be primitive.");
        }

        if (type.IsPointer() && other.IsPointer())
        {
            throw new Exception("Cannot add two pointers.");
        }

        if (type.IsPointer() || other.IsPointer())
        {
            // We don't know what type of pointer this is anymore.
            return Create(Primitives.Pointer);
        }
        else
        {
            return Create(Primitives.Number);
        }
    }

    internal static TypingType Subtract(this TypingType type, TypingType other)
    {
        if (type is ComplexType || other is ComplexType)
        {
            throw new Exception("Expected both types to be primitive.");
        }

        if (type.IsPointer() && other.IsPointer())
        {
            return Create(Primitives.Number);
        }
        else if (type.IsPointer() || other.IsPointer())
        {
            return Create(Primitives.Pointer);
        }
        else
        {
            return Create(Primitives.Number);
        }
    }

    internal static TypingType[] Decompose(this TypingType type) => type switch
    {
        ComplexType complex => complex.Structure.Fields.Values.Select(f => Create(f.Type)).ToArray(),
        PrimitiveType primitive => [primitive],
        PointerType pointer => [pointer],
        _ => throw new Exception("Unknown type."),
    };

    internal static TypingType[] Decompose(this IEnumerable<TypingType> types) => [.. types.SelectMany(Decompose)];

    internal static Primitives[] ToPrimitives(this TypingType type) => type switch
    {
        PrimitiveType primitive => [primitive.DataType],
        PointerType pointer => [Primitives.Pointer],
        ComplexType complex => complex.Structure.Fields.Values
            .OrderBy(f => f.Offset)
            .Select(f => f.Type)
            .ToArray(),
        _ => throw new Exception("Unknown type."),
    };
}

internal abstract record TypingType;

internal record PrimitiveType(Primitives DataType) : TypingType;
internal record ComplexType(Structure Structure) : TypingType;
internal record PointerType(TypingType InnerType) : TypingType;

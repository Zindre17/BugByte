namespace BugByte;

internal static class Parameter
{
    internal static ParameterType Create(TypingType typing) => new AnonymousParameter(typing);
    internal static ParameterType Create(Token nameToken, TypingType typing) => new NamedParameter(nameToken, typing);

    internal static bool IsNamed(this ParameterType parameter) => parameter is NamedParameter;

    internal static Token GetNameToken(this ParameterType parameter) => parameter switch
    {
        NamedParameter namedParameter => namedParameter.Name,
        _ => throw new Exception("No name for this parameter."),
    };
}

internal abstract record ParameterType(TypingType Typing);
internal record NamedParameter(Token Name, TypingType Typing) : ParameterType(Typing);
internal record AnonymousParameter(TypingType Typing) : ParameterType(Typing);

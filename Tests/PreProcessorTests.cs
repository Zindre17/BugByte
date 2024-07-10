namespace Tests;

[TestClass]
public class PreProcessorTests
{
    private const string testScope = "test";

    [TestMethod]
    public void Process_EmptyCode_ReturnsEmptyDefinitions()
    {
        var code = new SourceCode([]);
        var result = PreProcessor.Process(code, testScope);
        Assert.AreEqual(0, result.Scope.Definitions.Count);
        Assert.IsFalse(result.RemainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_StructDefinition_ReturnsStructDefinition()
    {
        var (remainingCode, scope) = PreProcessor.Process(Lexer.LexString(structDefinitonString), testScope);
        scope.TryGetStructure("point", out var structDefinition);

        Assert.AreEqual(1, scope.Definitions.Count);
        AssertStructure("point", 2, structDefinition);
        Assert.IsFalse(remainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_ConstantDefinitions_ReturnsConstantDefinitions()
    {
        var (remainingCode, scope) = PreProcessor.Process(Lexer.LexString(constantDefinitionString), testScope);
        scope.TryGetConstant("four", out var fourConstant);
        scope.TryGetConstant("prompt", out var promptConstant);

        Assert.AreEqual(2, scope.Definitions.Count);
        AssertConstant("four", Typing.Create(Primitives.Number), fourConstant);
        AssertConstant("prompt", Typing.Create(Structure.String), promptConstant);
        Assert.IsFalse(remainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_FunctionDefinition_ReturnsFunctionDefinition()
    {
        var (remainingCode, scope) = PreProcessor.Process(Lexer.LexString(functionDefinitionString), testScope);
        scope.TryGetFunction("add", out var functionDefinition);

        Assert.AreEqual(1, scope.Definitions.Count);
        AssertFunction("add", new Contract([Primitives.Number, Primitives.Number], [Primitives.Number]), false, functionDefinition);
        Assert.IsFalse(remainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_ProgramWithRemainingCode_ReturnsRemainigCode()
    {
        var (remainingCode, _) = PreProcessor.Process(Lexer.LexString([.. functionDefinitionString, " 1 2 add "]), testScope);

        Assert.IsTrue(remainingCode.HasNextToken());
        Assert.AreEqual("1", remainingCode.MoveNext().Word.Value);
        Assert.AreEqual("2", remainingCode.MoveNext().Word.Value);
        Assert.AreEqual("add", remainingCode.MoveNext().Word.Value);
    }

    private static void AssertFunction(string expectedName, Contract expectedContract, bool expectedAutoUsing, IIdentifiedDefinition<Function> definition)
    {
        var function = definition.Parse(Scope.Create());

        Assert.AreEqual(expectedName, function.Token.Word.Value);
        Assert.AreEqual(expectedContract, function.Contract);
        Assert.AreEqual(expectedAutoUsing, function.AutoUsings);
    }

    private static void AssertStructure(string expectedName, int expectedFieldCount, IIdentifiedDefinition<Structure> definition)
    {
        Assert.AreEqual(expectedName, definition.Token.Word.Value);
        var structure = definition.Parse(Scope.Create());
        Assert.AreEqual(expectedFieldCount, structure.Fields.Count);
    }

    private static void AssertConstant(string expectedName, TypingType expectedType, IIdentifiedDefinition<Constant> definition)
    {
        Assert.AreEqual(expectedName, definition.Token.Word.Value);
        var constant = definition.Parse(Scope.Create());
        Assert.AreEqual(Contract.Producer(expectedType), constant.Contract);
    }

    private static readonly string[] structDefinitonString = [
        "struct point :",
        "    x int",
        "    y int",
        ";"
    ];

    private static readonly string[] constantDefinitionString = [
        "aka four 4",
        "aka prompt \"Enter a number:\""
    ];

    private static readonly string[] functionDefinitionString = [
        "add(int int) int : + ;",
    ];
}

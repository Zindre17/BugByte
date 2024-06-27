namespace Tests;

[TestClass]
public class PreProcessorTests
{
    [TestMethod]
    public void Process_EmptyCode_ReturnsEmptyDefinitions()
    {
        var code = new SourceCode([]);
        var result = PreProcessor.Process(code);
        Assert.AreEqual(0, result.Definitions.Count);
        Assert.IsFalse(result.RemainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_StructDefinition_ReturnsStructDefinition()
    {
        var (remainingCode, definition) = PreProcessor.Process(Lexer.LexString(structDefinitonString));
        var structDefinition = definition.Get("point");

        Assert.AreEqual(1, definition.Count);
        AssertDefinition("point", 4, structDefinition);
        Assert.IsFalse(remainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_ConstantDefinitions_ReturnsConstantDefinitions()
    {
        var (remainingCode, definition) = PreProcessor.Process(Lexer.LexString(constantDefinitionString));
        var fourConstant = definition.Get("four");
        var promptConstant = definition.Get("prompt");

        Assert.AreEqual(2, definition.Count);
        AssertDefinition("four", 1, fourConstant);
        AssertDefinition("prompt", 1, promptConstant);
        Assert.IsFalse(remainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_FunctionDefinition_ReturnsFunctionDefinition()
    {
        var (remainingCode, definition) = PreProcessor.Process(Lexer.LexString(functionDefinitionString));
        var functionDefinition = definition.Get("add");

        Assert.AreEqual(1, definition.Count);
        AssertDefinition("add", 1, functionDefinition);
        Assert.IsFalse(remainingCode.HasNextToken());
    }

    [TestMethod]
    public void Process_ProgramWithRemainingCode_ReturnsRemainigCode()
    {
        var (remainingCode, _) = PreProcessor.Process(Lexer.LexString([.. functionDefinitionString, " 1 2 add "]));

        Assert.IsTrue(remainingCode.HasNextToken());
        Assert.AreEqual("1", remainingCode.MoveNext().Word.Value);
        Assert.AreEqual("2", remainingCode.MoveNext().Word.Value);
        Assert.AreEqual("add", remainingCode.MoveNext().Word.Value);
    }

    private static void AssertDefinition(string expectedName, int expectedCodeLength, IDefinition definition)
    {
        Assert.AreEqual(expectedName, definition.Name.Word.Value);
        Assert.IsTrue(definition.Code.HasRemainingTokens(expectedCodeLength));
        Assert.IsFalse(definition.Code.HasRemainingTokens(expectedCodeLength + 1));
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

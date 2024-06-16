namespace Tests;

[TestClass]
public class SourceCodeTests
{
    private readonly Token firstToken = Token.OnlyValue("a");
    private readonly Token secondToken = Token.OnlyValue("b");
    private readonly Token thirdToken = Token.OnlyValue("c");

    [TestMethod]
    public void HasNextToken_InitializedNonEmptySourceCode_True()
    {
        var sourceCode = new SourceCode([firstToken]);

        Assert.IsTrue(sourceCode.HasNextToken());
    }

    [TestMethod]
    public void HasNextToken_NonEmptySourcePartiallyConsumed_True()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        SourceCode.MoveNext();
        SourceCode.MoveNext();

        Assert.IsTrue(SourceCode.HasNextToken());
    }

    [TestMethod]
    public void HasNextToken_NonEmptySourceCodeFullyConsumed_False()
    {
        var SourceCode = new SourceCode([firstToken, secondToken]);

        SourceCode.MoveNext();
        SourceCode.MoveNext();

        Assert.IsFalse(SourceCode.HasNextToken());
    }


    [TestMethod]
    public void HasNextToken_EmptySourceCode_False()
    {
        var SourceCode = new SourceCode([firstToken]);

        Assert.IsTrue(SourceCode.HasNextToken());
    }

    [TestMethod]
    public void MoveNext_InitializedNonEmptySourceCode_FirstToken()
    {
        var SourceCode = new SourceCode([firstToken]);

        Assert.AreEqual(firstToken, SourceCode.MoveNext());
    }

    [TestMethod]
    public void MoveNext_EmptySourceCode_ThrowsException()
    {
        var SourceCode = new SourceCode([]);

        Assert.ThrowsException<EndOfCodeException>(SourceCode.MoveNext);
    }

    [TestMethod]
    public void MoveNext_InitializedNonEmptySourceCode_CanBeCalledTheSameAmountOfTimesAsElements()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        SourceCode.MoveNext();
        SourceCode.MoveNext();
        SourceCode.MoveNext();

        Assert.ThrowsException<EndOfCodeException>(SourceCode.MoveNext);
    }

    [TestMethod]
    public void MoveNext_InitializedNonEmptySourceCode_OrderOfTokensIsPreserved()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.AreEqual(firstToken, SourceCode.MoveNext());
        Assert.AreEqual(secondToken, SourceCode.MoveNext());
        Assert.AreEqual(thirdToken, SourceCode.MoveNext());
    }

    [TestMethod]
    public void PeekNextToken_EmptySourceCode_ThrowsException()
    {
        var SourceCode = new SourceCode([]);

        Assert.ThrowsException<EndOfCodeException>(SourceCode.PeekNextToken);
    }

    [TestMethod]
    public void PeekNextToken_NonEmptySourceCode_FirstToken()
    {
        var SourceCode = new SourceCode([firstToken]);

        Assert.AreEqual(firstToken, SourceCode.PeekNextToken());
    }

    [TestMethod]
    public void PeekNextToken_NonEmptySourceCode_CanBeCalledMultipleTimes()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.AreEqual(firstToken, SourceCode.PeekNextToken());
        Assert.AreEqual(firstToken, SourceCode.PeekNextToken());
        Assert.AreEqual(firstToken, SourceCode.PeekNextToken());
    }

    [TestMethod]
    public void CurrentToken_EmptySourceCode_ThrowsException()
    {
        var SourceCode = new SourceCode([]);

        Assert.ThrowsException<NoCurrentTokenYetException>(SourceCode.CurrentToken);
    }

    [TestMethod]
    public void CurrentToken_NonEmptySourceCodeNotYetMovedToNext_ThrowsException()
    {
        var SourceCode = new SourceCode([firstToken]);

        Assert.ThrowsException<NoCurrentTokenYetException>(SourceCode.CurrentToken);
    }

    [TestMethod]
    public void CurrentToken_NonEmptySourceCodeAfterMoveNext_FirstToken()
    {
        var SourceCode = new SourceCode([firstToken]);

        SourceCode.MoveNext();

        Assert.AreEqual(firstToken, SourceCode.CurrentToken());
    }

    [TestMethod]
    public void HasRemainingTokens_NonEmptySourceCode_EnoughTokens()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.IsTrue(SourceCode.HasRemainingTokens(2));
    }

    [TestMethod]
    public void HasRemainingTokens_NonEmptySourceCode_NotEnoughTokens()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.IsFalse(SourceCode.HasRemainingTokens(4));
    }

    [TestMethod]
    public void HasRemainingTokens_NonEmptySourceCode_ExactAmountOfTokens()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.IsTrue(SourceCode.HasRemainingTokens(3));
    }

    [TestMethod]
    public void HasRemainingTokens_NonEmptySourceCode_ExactAmountOfTokensAfterMoveNext()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        SourceCode.MoveNext();

        Assert.IsTrue(SourceCode.HasRemainingTokens(2));
    }

    [TestMethod]
    public void PeekNthToken_NonEmptySourceCode_GettingCorrctToken()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.AreEqual(secondToken, SourceCode.PeekNthToken(2));
    }

    [TestMethod]
    public void PeekNthToken_NonEmptySourceCode_GettingCorrctTokenAfterMoveNext()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        SourceCode.MoveNext();

        Assert.AreEqual(thirdToken, SourceCode.PeekNthToken(2));
    }

    [TestMethod]
    public void PeekNthToken_NonEmptySourceCode_NotEnoughTokens_ThrowsException()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.ThrowsException<EndOfCodeException>(() => SourceCode.PeekNthToken(4));
    }

    [TestMethod]
    public void PeekNthToken_NonEmptySourceCode_ExactAmountOfTokens()
    {
        var SourceCode = new SourceCode([firstToken, secondToken, thirdToken]);

        Assert.AreEqual(thirdToken, SourceCode.PeekNthToken(3));
    }

    [TestMethod]
    public void PeekNthToken_EmptySourceCode_ThrowsException()
    {
        var SourceCode = new SourceCode([]);

        Assert.ThrowsException<EndOfCodeException>(() => SourceCode.PeekNthToken(1));
    }
}


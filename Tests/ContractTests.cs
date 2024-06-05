namespace Tests;

[TestClass]
public class ContractTests
{
    [TestMethod]
    public void JoinInto_NoIns()
    {
        var a = Contract.Producer(Primitives.Number);
        var b = Contract.Producer(Primitives.Pointer);

        var r = a.JoinInto(b);

        Assert.AreEqual(0, r.In.Length);
        Assert.AreEqual(2, r.Out.Length);
        Assert.AreEqual(Primitives.Number, r.Out[0]);
        Assert.AreEqual(Primitives.Pointer, r.Out[1]);
    }

    [TestMethod]
    public void JoinInto_Empty()
    {
        var a = new Contract([Primitives.Number], [Primitives.Number]);
        var b = new Contract();

        var r = a.JoinInto(b);

        Assert.AreEqual(1, r.In.Length);
        Assert.AreEqual(1, r.Out.Length);
        Assert.AreEqual(Primitives.Number, r.In[0]);
        Assert.AreEqual(Primitives.Number, r.Out[0]);
    }

    [TestMethod]
    public void JoinInto_ExactInOutMatch()
    {
        var a = new Contract([Primitives.Number, Primitives.Pointer], [Primitives.Number]);
        var b = new Contract([Primitives.Number], [Primitives.Pointer]);

        var r = a.JoinInto(b);

        Assert.AreEqual(2, r.In.Length);
        Assert.AreEqual(Primitives.Number, r.In[0]);
        Assert.AreEqual(Primitives.Pointer, r.In[1]);
        Assert.AreEqual(1, r.Out.Length);
        Assert.AreEqual(Primitives.Pointer, r.Out[0]);
    }

    [TestMethod]
    public void JoinInto_InsufficientIn()
    {
        var a = new Contract([Primitives.Number, Primitives.Pointer], [Primitives.Number]);
        var b = new Contract([.. Structure.String.Decompose(), Primitives.Number], [Primitives.Pointer]);

        var r = a.JoinInto(b);

        Assert.AreEqual(4, r.In.Length);
        Assert.AreEqual(Primitives.Number, r.In[0]);
        Assert.AreEqual(Primitives.Pointer, r.In[1]);
        Assert.AreEqual(Primitives.Number, r.In[2]);
        Assert.AreEqual(Primitives.Pointer, r.In[3]);
        Assert.AreEqual(1, r.Out.Length);
        Assert.AreEqual(Primitives.Pointer, r.Out[0]);
    }

    [TestMethod]
    public void JoinInto_ExcessOuts()
    {
        var a = new Contract([Primitives.Pointer], [Primitives.Pointer, .. Structure.String.Decompose(), Primitives.Number]);
        var b = new Contract([Primitives.Number], [.. Structure.ZeroTerminatedString.Decompose()]);

        var r = a.JoinInto(b);

        Assert.AreEqual(1, r.In.Length);
        Assert.AreEqual(Primitives.Pointer, r.In[0]);
        Assert.AreEqual(4, r.Out.Length);
        Assert.AreEqual(Primitives.Pointer, r.Out[0]);
        Assert.AreEqual(Primitives.Number, r.Out[1]);
        Assert.AreEqual(Primitives.Pointer, r.Out[2]);
        Assert.AreEqual(Primitives.Pointer, r.Out[3]);
    }

    [TestMethod]
    public void JoinInto_Incompatible()
    {
        var a = Contract.Producer(Primitives.Pointer);
        var b = Contract.Consumer(Primitives.Number);

        Assert.ThrowsException<Exception>(() => a.JoinInto(b));
    }
}

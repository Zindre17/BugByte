namespace Tests;

[TestClass]
public class ContractTests
{
    [TestMethod]
    public void JoinInto_NoIns()
    {
        var a = Contract.Producer(DataType.Number);
        var b = Contract.Producer(DataType.Pointer);

        var r = a.JoinInto(b);

        Assert.AreEqual(0, r.In.Length);
        Assert.AreEqual(2, r.Out.Length);
        Assert.AreEqual(DataType.Number, r.Out[0]);
        Assert.AreEqual(DataType.Pointer, r.Out[1]);
    }

    [TestMethod]
    public void JoinInto_Empty()
    {
        var a = new Contract([DataType.Number], [DataType.Number]);
        var b = new Contract();

        var r = a.JoinInto(b);

        Assert.AreEqual(1, r.In.Length);
        Assert.AreEqual(1, r.Out.Length);
        Assert.AreEqual(DataType.Number, r.In[0]);
        Assert.AreEqual(DataType.Number, r.Out[0]);
    }

    [TestMethod]
    public void JoinInto_ExactInOutMatch()
    {
        var a = new Contract([DataType.Number, DataType.Pointer], [DataType.Number]);
        var b = new Contract([DataType.Number], [DataType.Pointer]);

        var r = a.JoinInto(b);

        Assert.AreEqual(2, r.In.Length);
        Assert.AreEqual(DataType.Number, r.In[0]);
        Assert.AreEqual(DataType.Pointer, r.In[1]);
        Assert.AreEqual(1, r.Out.Length);
        Assert.AreEqual(DataType.Pointer, r.Out[0]);
    }

    [TestMethod]
    public void JoinInto_InsufficientIn()
    {
        var a = new Contract([DataType.Number, DataType.Pointer], [DataType.Number]);
        var b = new Contract([DataType.String, DataType.Number], [DataType.Pointer]);

        var r = a.JoinInto(b);

        Assert.AreEqual(3, r.In.Length);
        Assert.AreEqual(DataType.String, r.In[0]);
        Assert.AreEqual(DataType.Number, r.In[1]);
        Assert.AreEqual(DataType.Pointer, r.In[2]);
        Assert.AreEqual(1, r.Out.Length);
        Assert.AreEqual(DataType.Pointer, r.Out[0]);
    }

    [TestMethod]
    public void JoinInto_ExcessOuts()
    {
        var a = new Contract([DataType.Pointer], [DataType.Pointer, DataType.String, DataType.Number]);
        var b = new Contract([DataType.Number], [DataType.ZeroTerminatedString]);

        var r = a.JoinInto(b);

        Assert.AreEqual(1, r.In.Length);
        Assert.AreEqual(DataType.Pointer, r.In[0]);
        Assert.AreEqual(3, r.Out.Length);
        Assert.AreEqual(DataType.Pointer, r.Out[0]);
        Assert.AreEqual(DataType.String, r.Out[1]);
        Assert.AreEqual(DataType.ZeroTerminatedString, r.Out[2]);
    }

    [TestMethod]
    public void JoinInto_Incompatible()
    {
        var a = Contract.Producer(DataType.Pointer);
        var b = Contract.Consumer(DataType.Number);

        Assert.ThrowsException<Exception>(() => a.JoinInto(b));
    }
}

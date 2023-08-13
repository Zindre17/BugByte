namespace Tests;

[TestClass]
public class ContractTests
{
    [TestMethod]
    public void JoinInto_NoIns()
    {
        var a = new Contract(Array.Empty<DataType>(), new DataType[] { DataType.Number });
        var b = new Contract(Array.Empty<DataType>(), new DataType[] { DataType.Pointer });

        var r = a.JoinInto(b);

        Assert.AreEqual(0, r.In.Length);
        Assert.AreEqual(2, r.Out.Length);
        Assert.AreEqual(DataType.Number, r.Out[0]);
        Assert.AreEqual(DataType.Pointer, r.Out[1]);
    }

    [TestMethod]
    public void JoinInto_Empty()
    {
        var a = new Contract(new DataType[] { DataType.Number }, new DataType[] { DataType.Number });
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
        var a = new Contract(new DataType[] { DataType.Number, DataType.Pointer }, new DataType[] { DataType.Number });
        var b = new Contract(new DataType[] { DataType.Number }, new DataType[] { DataType.Pointer });

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
        var a = new Contract(new DataType[] { DataType.Number, DataType.Pointer }, new DataType[] { DataType.Number });
        var b = new Contract(new DataType[] { DataType.String, DataType.Number }, new DataType[] { DataType.Pointer });

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
        var a = new Contract(new DataType[] { DataType.Pointer }, new DataType[] { DataType.Pointer, DataType.String, DataType.Number });
        var b = new Contract(new DataType[] { DataType.Number }, new DataType[] { DataType.ZeroTerminatedString });

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
        var a = new Contract(Array.Empty<DataType>(), new DataType[] { DataType.Pointer });
        var b = new Contract(new DataType[] { DataType.Number }, Array.Empty<DataType>());

        Assert.ThrowsException<Exception>(() => a.JoinInto(b));
    }
}

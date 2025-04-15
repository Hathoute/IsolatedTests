namespace Tests;

using IsolatedTests.Attribute;
using TestDependency;

[IsolatedTest]
[TestClass]
public class IsolatedDependencyTest1 {

    [TestMethod]
    public void TestNumberGeneratorShouldYieldOne() {
        Assert.AreEqual(1, MyNumberGenerator.GetNextNumber());
    }
    
}

[IsolatedTest]
[TestClass]
public class IsolatedDependencyTest2 {
    
    [TestMethod]
    public void TestNumberGeneratorShouldYieldOne() {
        Assert.AreEqual(1, MyNumberGenerator.GetNextNumber());
    }
    
}
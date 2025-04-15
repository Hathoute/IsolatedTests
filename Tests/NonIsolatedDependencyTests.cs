namespace Tests;

using TestDependency;

[TestClass]
public class NonIsolatedDependencyTest1 {

    [TestMethod]
    public void TestNumberGeneratorShouldYieldOne() {
        Assert.AreEqual(1, MyNumberGenerator.GetNextNumber());
    }
    
}

[TestClass]
public class NonIsolatedDependencyTest2 {
    
    [TestMethod]
    public void TestNumberGeneratorShouldYieldTwo() {
        Assert.AreEqual(2, MyNumberGenerator.GetNextNumber());
    }
    
}
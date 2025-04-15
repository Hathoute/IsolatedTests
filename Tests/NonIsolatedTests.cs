// Explicitly state that we do not want to parallelize these tests, since we rely on order of tests here
// https://learn.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?pivots=mstest
// and https://stackoverflow.com/questions/2255284/how-does-mstest-determine-the-order-in-which-to-run-test-methods
[assembly: DoNotParallelize]
namespace Tests;

[TestClass]
public class NonIsolatedTest1 {

    [TestMethod]
    public void TestSharedStateShouldNotBeInitialized() {
        Assert.IsFalse(SharedState.Instance.IsInitialized);
        SharedState.Instance.IsInitialized = true;
    }
}

[TestClass]
public class NonIsolatedTest2 {

    [TestMethod]
    public void TestSharedStateShouldHaveBeenInitialized() {
        Assert.IsTrue(SharedState.Instance.IsInitialized);
    }
    
}
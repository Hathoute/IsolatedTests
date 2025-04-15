using IsolatedTests.Attribute;

namespace Tests;

[IsolatedTest]
[TestClass]
public class IsolatedTest1 {

    [TestMethod]
    public void TestSharedStateShouldNotBeInitialized() {
        lock (SharedState.Instance) {
            Assert.IsFalse(SharedState.Instance.IsInitialized);
            SharedState.Instance.IsInitialized = true;
        }
    }
    
}

[IsolatedTest]
[TestClass]
public class IsolatedTest2 {
    
    [TestMethod]
    public void TestSharedStateShouldNotBeInitialized() {
        lock (SharedState.Instance) {
            Assert.IsFalse(SharedState.Instance.IsInitialized);
            SharedState.Instance.IsInitialized = true;
        }
    }
    
}
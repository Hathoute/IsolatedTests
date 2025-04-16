using System.Reflection;
using IsolatedTests;

namespace Tests;

[TestClass]
public class TestClassInterceptorTests {

    private const string ValidTestClassCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestClassInterceptorTests;

[TestClass]
public class ValidTestClass {

    [TestMethod]
    public void TestMethod() {
        Assert.AreEqual(2 + 2, 4);
    }

    [TestMethod]
    public async void TestAsyncMethod() {
        await Task.Delay(1);
        Assert.AreEqual(2 + 2, 4);
    }
}";
    
    private const string InvalidTestClassesCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestClassInterceptorTests;

[TestClass]
public class InvalidTestMethodWithReturn {

    [TestMethod]
    public int TestMethodWithReturn() {
        Assert.AreEqual(2 + 2, 4);
        return 1;
    }

}

[TestClass]
public class InvalidTestMethodWithParameters {

    [TestMethod]
    public void TestMethodWithParameters(int param) {
        Assert.AreEqual(2 + 2, 4);
    }

}";
    
    private static readonly Assembly ValidTestClassAssembly = TestHelpers.CompileCode(ValidTestClassCode);
    private static readonly Assembly InvalidTestClassAssembly = TestHelpers.CompileCode(InvalidTestClassesCode);

    [TestMethod]
    public void TestShouldAcceptValidClass() {
        var type = ValidTestClassAssembly.GetType("TestClassInterceptorTests.ValidTestClass");
        Assert.IsNotNull(type);
        
        try {
            _ = new TestClassInterceptor(type);
        }
        catch (Exception ex) {
            Assert.Fail("Expected no exception, but got: " + ex.Message);
        }
    }

    [TestMethod]
    public void TestShouldNotAcceptMethodWithReturn() {
        var type = InvalidTestClassAssembly.GetType("TestClassInterceptorTests.InvalidTestMethodWithReturn");
        Assert.IsNotNull(type);
        
        var exception = Assert.ThrowsException<InvalidOperationException>(() => _ = new TestClassInterceptor(type));
        Assert.AreEqual("Isolated test TestMethodWithReturn should either return void or a Task (async)", exception.Message);
    }

    [TestMethod]
    public void TestShouldNotAcceptMethodWithParameters() {
        var type = InvalidTestClassAssembly.GetType("TestClassInterceptorTests.InvalidTestMethodWithParameters");
        Assert.IsNotNull(type);
        
        var exception = Assert.ThrowsException<NotSupportedException>(() => _ = new TestClassInterceptor(type));
        Assert.AreEqual("Isolated test TestMethodWithParameters should not have parameters", exception.Message);
    }
}
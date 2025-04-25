using System.Reflection;
using IsolatedTests;

namespace Tests;

[TestClass]
public class TestClassInterceptorTests {

    private const string ValidTestClassCodeTemplate = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestClassInterceptorTests;

[TestClass]
public class ValidTestClass_{0} {{

    [TestMethod]
    public void TestMethod() {{
        Assert.AreEqual(2 + 2, 4);
    }}

    [TestMethod]
    public async void TestAsyncMethod() {{
        await Task.Delay(1);
        Assert.AreEqual(2 + 2, 4);
    }}
}}";
    
    private const string InvalidTestClassesCodeTemplate = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestClassInterceptorTests;

[TestClass]
public class InvalidTestMethodWithReturn_{0} {{

    [TestMethod]
    public int TestMethodWithReturn() {{
        Assert.AreEqual(2 + 2, 4);
        return 1;
    }}

}}

[TestClass]
public class InvalidTestMethodWithParameters_{0} {{

    [TestMethod]
    public void TestMethodWithParameters(int param) {{
        Assert.AreEqual(2 + 2, 4);
    }}

}}";
    
    private const string UnsupportedTestClassesCodeTemplate = @"
using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestClassInterceptorTests;

//[UnsupportedTestAttribute]
public class UnsupportedTest_{0} {{

    //[UnsupportedTestAttribute]
    public void TestMethod() {{
        Assert.AreEqual(2 + 2, 4);
    }}

}}";
    

    private static int _currentClassId;

    private static Tuple<Assembly, int> TestClassAssemblyProvider(string codeTemplate) {
        var classId = ++_currentClassId;
        var code = string.Format(codeTemplate, classId);
        var assembly = TestHelpers.CompileCode(code);
        return new Tuple<Assembly, int>(assembly, classId);
    }
    
    [TestMethod]
    public void TestShouldAcceptValidClass() {
        var (assembly, classId) = TestClassAssemblyProvider(ValidTestClassCodeTemplate);
        var typeName = $"TestClassInterceptorTests.ValidTestClass_{classId}";
        var type = assembly.GetType(typeName);
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
        var (assembly, classId) = TestClassAssemblyProvider(InvalidTestClassesCodeTemplate);
        var typeName = $"TestClassInterceptorTests.InvalidTestMethodWithReturn_{classId}";
        var type = assembly.GetType(typeName);
        Assert.IsNotNull(type);
        
        var exception = Assert.ThrowsException<InvalidOperationException>(() => _ = new TestClassInterceptor(type));
        Assert.AreEqual("Isolated test TestMethodWithReturn should either return void or a Task (async)", exception.Message);
    }

    [TestMethod]
    public void TestShouldNotAcceptMethodWithParameters() {
        var (assembly, classId) = TestClassAssemblyProvider(InvalidTestClassesCodeTemplate);
        var typeName = $"TestClassInterceptorTests.InvalidTestMethodWithParameters_{classId}";
        var type = assembly.GetType(typeName);
        Assert.IsNotNull(type);
        
        var exception = Assert.ThrowsException<NotSupportedException>(() => _ = new TestClassInterceptor(type));
        Assert.AreEqual("Isolated test TestMethodWithParameters should not have parameters", exception.Message);
    }

    [TestMethod]
    public void TestDisposeOfObjectsShouldReturnTrueWhenNoTestMethodFound() {
        var (assembly, classId) = TestClassAssemblyProvider(UnsupportedTestClassesCodeTemplate);
        var typeName = $"TestClassInterceptorTests.UnsupportedTest_{classId}";
        var type = assembly.GetType(typeName);
        Assert.IsNotNull(type);
        
        var interceptor = new TestClassInterceptor(type);
        Assert.IsTrue(interceptor.DisposeOfObjects());
    }

    [TestMethod]
    public void TestDisposeOfObjectsShouldReturnFalseWhenNotInitialized() {
        var (assembly, classId) = TestClassAssemblyProvider(ValidTestClassCodeTemplate);
        var typeName = $"TestClassInterceptorTests.ValidTestClass_{classId}";
        var type = assembly.GetType(typeName);
        Assert.IsNotNull(type);
        
        var interceptor = new TestClassInterceptor(type);
        Assert.IsFalse(interceptor.DisposeOfObjects());
    }
}
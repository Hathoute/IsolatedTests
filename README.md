# IsolatedTests

**IsolatedTests** is a .NET library that enables test class isolation in your test suites using a simple attribute-based approach. It supports .NET 6, .NET 7, .NET 8 and .NET 9.


## Features

- **Isolated Test Classes** – Ensure test classes are isolated from each other to avoid shared state issues.
- **Simple Initialization** – Minimal setup using a `[ModuleInitializer]`.
- **Seamless Integration** – Use `[IsolatedTest]` to mark classes that should be isolated.


## Getting Started

### Installation

Add the `IsolatedTests` package to your test project:

```bash
dotnet add package IsolatedTests
```

- Make sure your test project targets `.NET 6+`.
- Make sure to disable **Optimization** or specifically **JIT inlining** so that [test methods are not potentially inlined](https://github.com/Hathoute/UnsafeCLR?tab=readme-ov-file#limitations).


###  Setup

Initialize `IsolatedTests` once at module startup using the `[ModuleInitializer]` attribute:

```csharp
using System.Runtime.CompilerServices;
using IsolatedTests;

public static class TestSetup {
    [ModuleInitializer]
    internal static void ModuleInitializer() {
        TestIsolator.ModuleInitializer();
    }
}
```

This ensures that isolation is injected before any tests run.


### Usage

To isolate a test class, simply decorate it with the `[IsolatedTest]` attribute:

```csharp
using IsolatedTests.Attribute;

public class MySharedState {
    private static MySharedState? _instance;
    internal static MySharedState Instance => _instance ??= new MySharedState();

    internal bool IsInitialized {
        get;
        set;
    }
}

[TestClass]
[IsolatedTest]
public class MyIsolatedTest1 {
    [TestMethod]
    public void Test1() {
        lock (MySharedState.Instance) {
            Assert.IsFalse(MySharedState.Instance.IsInitialized);
            MySharedState.Instance.IsInitialized = true;
        }
    }
}

[TestClass]
[IsolatedTest]
public class MyIsolatedTest2 {
    [TestMethod]
    public async Task Test2() {
        await Task.Delay(1000)
            .ContinueWith(t => {
                lock (MySharedState.Instance) {
                    Assert.IsFalse(MySharedState.Instance.IsInitialized);
                    MySharedState.Instance.IsInitialized = true;
                }
            });
    }
}
```

Only test classes marked with `[IsolatedTest]` will be isolated. Regular test classes continue to run as usual.


## How does it work?

`IsolatedTests` is powered by [UnsafeCLR](https://github.com/Hathoute/UnsafeCLR) (thus the non-support for net6.0 ARM64), a runtime method manipulation library.

Here’s what happens under the hood:

1. `UnsafeCLR` is used to dynamically edit the implementation of your test methods at runtime. This allows `IsolatedTests` to intercept calls to test methods.

2. Once a test method is intercepted, `IsolatedTests` loads the test assembly (if not previously done for this class) into a [completely separate, isolated assembly context](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext).

3. The intercepted method call is then re-routed to execute inside this new context, providing full isolation from other tests.

This approach ensures that static state, global configuration, or other test-related side effects do not bleed between test classes.


## Compatibility

-  .NET 6 ([**ARM64** is not supported](https://github.com/Hathoute/UnsafeCLR?tab=readme-ov-file#limitations))
-  .NET 7
-  .NET 8
-  .NET 9
-  .NET 10


## License

See [LICENSE](LICENSE)


## Contributing

Pull requests are welcome! For major changes, please open an issue first to discuss what you'd like to change.


## Feedback

Found an issue or have a feature request? [Open an issue](https://github.com/Hathoute/IsolatedTests/issues).



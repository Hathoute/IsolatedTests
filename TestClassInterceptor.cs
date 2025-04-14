using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;
using NLog;
using UnsafeCLR;

namespace IsolatedTests;

internal class TestClassInterceptor {
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private static readonly string[] TestAttributes = {
        "Xunit.FactAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute"
    };

    private static readonly string[] OtherAttributes = {
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute",
        "Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute"
    };
    
    private static Dictionary<Type, TestClassInterceptor> registeredInterceptors = new ();
    
    private static MethodInfo GetMethodInfo<T1, T2>(Action<T1, T2> action) => action.Method;
    
    private readonly Type _executingContextTestType;
    private WeakReference _weakTestAssemblyReference;
    private WeakReference _weakTestAssembly;
    private WeakReference _weakTestType;
    private bool _loaded;

    private int _disposedObjects;
    private int _testMethodCount;
    
    private readonly Dictionary<string, MethodInfo> _interceptorMethods;
    private readonly Dictionary<string, MethodReplacement> _methodReplacements;
    private readonly List<Tuple<WeakReference, object>> _objectToProxy;

    internal TestClassInterceptor(Type executingContextTestType) {
        registeredInterceptors.Add(executingContextTestType, this);
        
        _executingContextTestType = executingContextTestType;
        _interceptorMethods = new Dictionary<string, MethodInfo>();
        _methodReplacements = new Dictionary<string, MethodReplacement>();
        _objectToProxy = new List<Tuple<WeakReference, object>>();
        PrepareIsolatedTestClass();
    }
    
    private void PrepareIsolatedTestClass() {
        var testMethods = _executingContextTestType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => HasAnyAttribute(m, TestAttributes))
            .ToList();
        var otherMethods = _executingContextTestType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => HasAnyAttribute(m, OtherAttributes));
        
        _testMethodCount = testMethods.Count;
        
        foreach (var fact in testMethods.Concat(otherMethods)) {
            PrepareIsolatedTestMethod(fact);
        }
    }

    private void PrepareIsolatedTestMethod(MethodInfo testMethod) {
        if (testMethod.GetParameters().Length > 0) {
            throw new NotSupportedException($"Isolated test {testMethod.Name} should not have parameters.");
        }

        var interceptor = CreateInterceptorForTestMethod(testMethod.Name);
        // Keep a reference to the interceptor method so that it won't be potentially garbage collected.
        // TODO: find out if DynamicMethods can be garbage collected
        _interceptorMethods.Add(testMethod.Name, interceptor);
        
        var replacement = CLRHelper.ReplaceInstanceMethod(_executingContextTestType, testMethod, interceptor);
        _methodReplacements.Add(testMethod.Name, replacement);
    }

    internal bool DisposeOfObjects() {
        Logger.Trace("Begin DisposeOfObjects for isolated type {0}", _executingContextTestType.FullName);
        var collectedObjects = _objectToProxy.Where(x => !x.Item1.IsAlive)
            .ToList();
        
        if (Logger.IsTraceEnabled) {
            Logger.Trace("{0} objects were collected", collectedObjects.Count);
        }
        
        foreach (var tuple in collectedObjects) {
            if (tuple.Item2 is IDisposable isolatedInstance) {
                isolatedInstance.Dispose();
            }

            _objectToProxy.Remove(tuple);
            _disposedObjects++;
        }

        Logger.Trace("Disposed Objects: {0}, Test Method Count: {1}", _disposedObjects, _testMethodCount);
        if (_disposedObjects != _testMethodCount) {
            return false;
        }

        if (collectedObjects.Count > 0) {
            Logger.Warn(
                "Disposed objects matched test method count, but there are still {0} collected objects remaining. Will not unload the AssemblyLoadContext.",
                collectedObjects.Count);
            return false;
        }
            
        if (_weakTestAssemblyReference.Target is not TestAssemblyLoadContext assemblyLoadContext) {
            throw new InvalidOperationException("TestAssemblyLoadContext was null before");
        }
        
        Logger.Debug("Unloading assembly load context for isolated type {0}", _executingContextTestType.FullName);
        assemblyLoadContext.Unload();
        return true;
    }

    private void LoadIsolatedAssembly() {
        var loadContext = new TestAssemblyLoadContext(".");
        var assemblyPath = _executingContextTestType.Assembly.Location;
        var assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        if (assembly is null) {
            throw new InvalidOperationException($"Failed to load assembly '{assemblyPath}'");
        }

        if (assembly == _executingContextTestType.Assembly) {
            throw new InvalidOperationException("Loaded assembly is not isolated");
        }

        var testType = assembly.GetType(_executingContextTestType.FullName!);
        if (testType is null) {
            throw new InvalidOperationException($"Type '{testType}' was not found in the isolated assembly '{assembly.FullName}'");
        }
        
        _weakTestAssemblyReference = new WeakReference(loadContext);
        _weakTestAssembly = new WeakReference(assembly);
        _weakTestType = new WeakReference(testType);
        _loaded = true;
    }

    private void RunTestMethod(object original, string testMethodName) {
        if (!_loaded) {
            LoadIsolatedAssembly();
        }
        
        var isolatedInstance = GetIsolatedInstance(original);
        var isolatedType = _weakTestType.Target as Type;

        var isolatedTestMethod = isolatedType.GetMethod(testMethodName, BindingFlags.Public | BindingFlags.Instance);
        isolatedTestMethod.Invoke(isolatedInstance, null);
    }

    private object GetIsolatedInstance(object original) {
        var existing = _objectToProxy.FirstOrDefault(o => o.Item1.IsAlive && ReferenceEquals(o.Item1.Target, original));
        if (existing is not null) {
            return existing;
        }

        var instance = CreateNewIsolatedInstance();
        _objectToProxy.Add(new Tuple<WeakReference, object>(new WeakReference(original), instance));
        return instance;
    }

    private object CreateNewIsolatedInstance() {
        if (_weakTestAssembly.Target is not Assembly assembly) {
            throw new InvalidOperationException(
                $"Isolated assembly for {_executingContextTestType.Assembly.FullName} was null");
        }
        
        if (_weakTestType.Target is not Type testType) {
            throw new InvalidOperationException(
                $"Isolated type for {_executingContextTestType.FullName} was null");
        }
        
        var instance = assembly.CreateInstance(testType.FullName!);
        if (instance is null) {
            throw new InvalidOperationException(
                $"Could not create instance of isolated type {testType.FullName}");
        }

        return instance;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static void TestMethod(object instance, string testMethodName) {
        var type = instance.GetType();
        var interceptor = registeredInterceptors[type];
        interceptor.RunTestMethod(instance, testMethodName);
    }

    private static bool HasAnyAttribute(MethodInfo methodInfo, string[] attributes) {
        return methodInfo.GetCustomAttributes()
            .Select(a => a.GetType())
            .Any(t => attributes.Contains(t.FullName));
    }

    private static DynamicMethod CreateInterceptorForTestMethod(string testMethodName) {
        var dynamicMethod = new DynamicMethod("Interceptor_" + testMethodName, 
            typeof(void),
            new []{typeof (object)});
        
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, testMethodName);
        il.EmitCall(OpCodes.Call, GetMethodInfo<object, string>(TestMethod), null);
        il.Emit(OpCodes.Ret);
        return dynamicMethod;
    }
}
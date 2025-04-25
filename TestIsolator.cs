using System.Reflection;
using System.Runtime.CompilerServices;
using IsolatedTests.Attribute;

[assembly: InternalsVisibleTo("Tests")]
namespace IsolatedTests;

public static class TestIsolator {

    private const string LoadedEnvVariableName = "ISOLATED_TESTS_LOADED";
    
    public static void ModuleInitializer(bool collectibleAssemblies = true) {
        var variable = Environment.GetEnvironmentVariable(LoadedEnvVariableName);
        if (variable != null) {
            // This assembly is being run as an isolated test, we already did
            // the generation before.
            return;
        }
        
        Environment.SetEnvironmentVariable(LoadedEnvVariableName, "true");
        
        // Find all classes meant to run in isolated mode
        var callingAssembly = Assembly.GetCallingAssembly();
        var testInterceptors = callingAssembly.GetTypes()
            .Where(t => t.IsClass && t.GetCustomAttributes(typeof(IsolatedTestAttribute), true).Length > 0)
            .Select(t => new TestClassInterceptor(t, collectibleAssemblies))
            .ToList();

        var watcher = new InterceptorWatcher(testInterceptors);
        watcher.Start();
    }
}
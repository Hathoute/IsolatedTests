using System.Reflection;
using System.Runtime.Loader;
using NLog;

namespace IsolatedTests;

internal class TestAssemblyLoadContext : AssemblyLoadContext {
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    private readonly AssemblyDependencyResolver _resolver;

    internal TestAssemblyLoadContext(string baseAssemblyPath, bool isCollectible) : base(isCollectible) {
        _resolver = new AssemblyDependencyResolver(baseAssemblyPath);
    }
    
    // Resolver of the locations of the assemblies that are dependencies of the
    // main plugin assembly.

    // The Load method override causes all the dependencies present in the plugin's binary directory to get loaded
    // into the HostAssemblyLoadContext together with the plugin assembly itself.
    // NOTE: The Interface assembly must not be present in the plugin's binary directory, otherwise we would
    // end up with the assembly being loaded twice. Once in the default context and once in the HostAssemblyLoadContext.
    // The types present on the host and plugin side would then not match even though they would have the same names.
    protected override Assembly Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath == null) {
            return null;
        }

        Logger.Info($"Loading assembly {assemblyPath} into the TestAssemblyLoadContext");
        return LoadFromAssemblyPath(assemblyPath);

    }
}
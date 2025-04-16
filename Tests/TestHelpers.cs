using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NLog;

namespace Tests;

public static class TestHelpers {
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static Assembly CompileCode(string code) {
        // Parse syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var dotnetReference = GetGlobalReferences();
        var references = new[] {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location), // MSTest
            MetadataReference.CreateFromFile(typeof(TestClassAttribute).Assembly.Location), // MSTest attributes
        };

        // Define compilation options
        var compilation = CSharpCompilation.Create(
            assemblyName: $"TestAssembly_{Random.Shared.NextInt64()}",
            syntaxTrees: new[] { syntaxTree },
            references: dotnetReference.Concat(references),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success) {
            Logger.Error("Failed to compile provided code");
            foreach (var diag in result.Diagnostics)
                Logger.Error(diag);
            
            throw new InvalidOperationException("Failed to compile provided code");
        }

        ms.Seek(0, SeekOrigin.Begin);
        return AssemblyLoadContext.Default.LoadFromStream(ms);
    }
    
    private static IEnumerable<MetadataReference> GetGlobalReferences() {
        //The location of the .NET assemblies
        var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        /* 
            * Adding some necessary .NET assemblies
            * These assemblies couldn't be loaded correctly via the same construction as above,
            * in specific the System.Runtime.
            */
        return new [] {
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "mscorlib.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Core.dll")),
            MetadataReference.CreateFromFile(Path.Combine(assemblyPath, "System.Runtime.dll"))
        };
    }
}
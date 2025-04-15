using System.Runtime.CompilerServices;
using IsolatedTests;
using NLog;

namespace Tests;

internal static class TestInitializer {

    static TestInitializer(){
        LogManager.Setup().LoadConfiguration(builder => {
            builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToConsole();
        });
    }

    [ModuleInitializer]
    internal static void ModuleInitializer() {
        TestIsolator.ModuleInitializer();
    }
    
}
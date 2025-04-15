namespace Tests;

internal class SharedState {

    private static SharedState? _instance;
    internal static SharedState Instance => _instance ??= new SharedState();

    internal bool IsInitialized {
        get;
        set;
    }
}
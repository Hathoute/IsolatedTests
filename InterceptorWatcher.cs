using NLog;

namespace IsolatedTests;

internal class InterceptorWatcher {
    
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const int CheckPeriodMillis = 2000;
    
    private readonly List<TestClassInterceptor> _interceptors;
    private Timer _timer;

    internal InterceptorWatcher(IEnumerable<TestClassInterceptor> interceptors) {
        _interceptors = new List<TestClassInterceptor>(interceptors);
    }

    public void Start() {
        _timer = new Timer(Callback, null, CheckPeriodMillis, 0); 
    }

    private void Callback(object? o) {
        Logger.Trace("Starting Callback");
        
        var disposedCount = _interceptors.RemoveAll(i => i.DisposeOfObjects());
        if (disposedCount > 0) {
            Logger.Debug("Disposed of {0} interceptors.", disposedCount);
        }

        if (_interceptors.Count > 0) {
            _timer.Change(CheckPeriodMillis, 0);
        }
    }
}
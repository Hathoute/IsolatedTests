namespace TestDependency;

public static class MyNumberGenerator {

    private static int _currentNumber;

    public static int GetNextNumber() {
        return ++_currentNumber;
    }
    
}
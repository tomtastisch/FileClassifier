
namespace FileTypeDetectionLib.Tests.Support;

internal static class TestGuard
{
    public static T Unbox<T>(object? value) where T : struct
    {
        if (value is T typed)
        {
            return typed;
        }

        throw new InvalidOperationException($"Expected value of type {typeof(T).Name}.");
    }

    public static T NotNull<T>(T? value) where T : class
    {
        return value ?? throw new InvalidOperationException($"Expected non-null {typeof(T).Name}.");
    }
}

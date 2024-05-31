namespace System.Linq;

public static class LinqExtensions
{
    public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
    {
        foreach (var value in values)
        {
            action(value);
        }
    }
}

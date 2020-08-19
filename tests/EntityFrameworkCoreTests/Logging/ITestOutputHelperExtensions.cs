using Xunit.Abstractions;

namespace EntityFrameworkCoreTests.Logging
{
    public static class ITestOutputHelperExtensions
    {
        public static ILineWritable AsLineWriter(this ITestOutputHelper source) => new LineWritableTestOutputHelper(source);
    }
}

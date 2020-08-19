using Xunit.Abstractions;

namespace EntityFrameworkCoreTests.Logging
{
    class LineWritableTestOutputHelper : ITestOutputHelper, ILineWritable
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public LineWritableTestOutputHelper(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public void WriteLine(string message)
        {
            _testOutputHelper.WriteLine(message);
        }

        public void WriteLine(string format, params object[] args)
        {
            _testOutputHelper.WriteLine(format, args);
        }
    }
}

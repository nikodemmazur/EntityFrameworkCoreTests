using EntityFrameworkCoreTests.TestHelpers;
using Xunit.Abstractions;

namespace EntityFrameworkCoreTests.Tests
{
    public class ConfiguringRelationships : TestClassBase
    {
        private readonly ITestOutputHelper _testOutput;

        public ConfiguringRelationships(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }


    }
}

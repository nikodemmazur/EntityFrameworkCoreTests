using Microsoft.Extensions.Configuration;
using System.IO;
using System.Reflection;

namespace EntityFrameworkCoreTests.Helpers
{
    public static class AppSettingsLoader
    {
        public static IConfigurationRoot GetConfiguration()
        {
            var exeAssemPath = Assembly.GetExecutingAssembly().Location;
            var basePath = Path.GetDirectoryName(exeAssemPath);
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
            return builder.Build();
        }
    }
}

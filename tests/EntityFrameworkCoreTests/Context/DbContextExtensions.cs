using EntityFrameworkCoreTests.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace EntityFrameworkCoreTests.Context
{
    public static class DbContextExtensions
    {
        public static T StartLogging<T>(this T source, ILineWritable sink) where T : DbContext
        {
            var loggerFactory = source.GetService<ILoggerFactory>();
            loggerFactory.AddProvider(new LineWriterLoggerProvider(sink));
            return source;
        }
    }
}

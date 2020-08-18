using EntityFrameworkCoreTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;
using System.Diagnostics;

namespace EntityFrameworkCoreTests.Db
{
    public static class SqlDb
    {
        /// <summary>
        /// Creates <see cref="DbContextOptions"/> using <paramref name="source"/> as the connection string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DbContextOptions<T> AsSqlConnectionString<T>(this string source) where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();
            builder.UseSqlServer(source);
            return builder.Options;
        }

        /// <summary>
        /// Creates the connection string by examining the caller.
        /// </summary>
        /// <param name="baseConnStr">The base connection string.</param>
        /// <param name="stackFrameLevel"></param>
        /// <param name="withMethodName">Include the method name also.</param>
        /// <returns></returns>
        private static string CreateConnectionStringFrom(string baseConnStr, int stackFrameLevel = 1, bool withMethodName = true)
        {
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(stackFrameLevel)?.GetMethod() ?? throw new InvalidOperationException("Cannot obtain info about the caller.");
            var className = method.DeclaringType?.Name ?? throw new InvalidOperationException("Cannot obtain info about the caller.");
            var methodName = method.Name;

            return CreateConnectionStringFrom(baseConnStr, className, withMethodName ? methodName : string.Empty);
        }

        /// <summary>
        /// Creates the connection string by formatting <paramref name="baseConnStr"/>, <paramref name="className"/> and <paramref name="methodName"/>.
        /// </summary>
        /// <param name="baseConnStr"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static string CreateConnectionStringFrom(string baseConnStr, string className, string methodName = "")
        {
            var suffix = $"-{className}" + (string.IsNullOrEmpty(methodName) ? string.Empty : $"-{methodName}");

            var builder = new SqlConnectionStringBuilder(baseConnStr);
            builder.InitialCatalog += suffix;

            return builder.ToString();
        }

        /// <summary>
        /// Creates the connection string from the default connection string, the caller type name and method name.
        /// </summary>
        /// <returns></returns>
        public static string CreateConnectionString()
        {
            var defConn = AppSettingsLoader.GetConfiguration().GetConnectionString("DefaultConnection");
            return CreateConnectionStringFrom(defConn, 2, true);
        }
    }
}

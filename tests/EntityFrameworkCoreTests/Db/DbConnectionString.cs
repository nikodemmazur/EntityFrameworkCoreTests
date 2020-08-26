using EntityFrameworkCoreTests.Loaders;
using Microsoft.Extensions.Configuration;
using System;
using System.Data.SqlClient;

namespace EntityFrameworkCoreTests.Db
{
    /// <summary>
    /// Holds the db connection string. The name is created by the adopted convention.
    /// </summary>
    public class DbConnectionString : IEquatable<DbConnectionString?>
    {
        private DbConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
        }

        public string ConnectionString { get; }

        /// <summary>
        /// Creates the connection string by formatting <paramref name="baseConnStr"/>, <paramref name="className"/> and <paramref name="methodName"/>.
        /// </summary>
        /// <param name="baseConnStr"></param>
        /// <param name="className"></param>
        /// <param name="methodName"></param>
        /// <returns></returns>
        private static DbConnectionString Create(string baseConnStr, string className, string methodName = "")
        {
            var suffix = $"-{className}" + (string.IsNullOrEmpty(methodName) ? string.Empty : $"-{methodName}");

            var builder = new SqlConnectionStringBuilder(baseConnStr);
            builder.InitialCatalog += suffix;

            return new DbConnectionString(builder.ToString());
        }

        /// <summary>
        /// Creates the connection string from the default connection string, the class name and method name.
        /// </summary>
        /// <param name="stackDepth"></param>
        /// <returns></returns>
        public static DbConnectionString Create(string className, string methodName = "")
        {
            var defConn = AppSettingsLoader.GetConfiguration().GetConnectionString("DefaultConnection");
            return Create(defConn, className, methodName);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DbConnectionString);
        }

        public bool Equals(DbConnectionString? other)
        {
            return other != null &&
                   ConnectionString == other.ConnectionString;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ConnectionString);
        }
    }
}

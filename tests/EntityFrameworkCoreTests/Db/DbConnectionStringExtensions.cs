using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCoreTests.Db
{
    public static class DbConnectionStringExtensions
    {
        /// <summary>
        /// Creates <see cref="DbContextOptions"/> using <paramref name="source"/> as the connection string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DbContextOptions<T> AsSqlConnectionString<T>(this DbConnectionString source) where T : DbContext
        {
            var builder = new DbContextOptionsBuilder<T>();
            builder.UseSqlServer(source.ConnectionString);
            return builder.Options;
        }
    }
}

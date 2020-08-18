using Microsoft.EntityFrameworkCore;
using System;

namespace EntityFrameworkCoreTests.Context
{
    public static class DbContextOptionsExtensions
    {
        /// <summary>
        /// Creates <see cref="DbContext"/> from <see cref="DbContextOptions"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T ToDbContext<T>(this DbContextOptions<T> source) where T : DbContext
        {
            return (T)Activator.CreateInstance(typeof(T), source)!;
        }

        /// <summary>
        /// Ensures deletion and then creation of the connected db.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static DbContextOptions<T> EnsureDb<T>(this DbContextOptions<T> source) where T : DbContext
        {
            using var context = source.ToDbContext();
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            return source;
        }
    }
}

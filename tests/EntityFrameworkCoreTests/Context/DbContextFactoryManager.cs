using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;

namespace EntityFrameworkCoreTests.Context
{
    public class DbContextFactoryManager<T> where T : DbContext
    {
        private static readonly Lazy<DbContextFactoryManager<T>> _lazyInstance =
            new Lazy<DbContextFactoryManager<T>>(() => new DbContextFactoryManager<T>(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

        private readonly ConcurrentDictionary<string, DbContextFactory<T>> _dbCtxFacts =
            new ConcurrentDictionary<string, DbContextFactory<T>>();

        private DbContextFactoryManager() { }

        public static DbContextFactoryManager<T> Instance => _lazyInstance.Value;

        public DbContextFactory<T> GetDbContextFactory(string name) => 
            _dbCtxFacts.GetOrAdd(name, new DbContextFactory<T>());
    }
}

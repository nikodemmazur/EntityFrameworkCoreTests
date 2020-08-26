using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Logging;
using Microsoft.EntityFrameworkCore;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFrameworkCoreTests.Context
{
    public class BookStoreContextFactory
    {
        private static readonly Lazy<BookStoreContextFactory> _lazyInstance =
                new Lazy<BookStoreContextFactory>(() => new BookStoreContextFactory());
        private readonly ConcurrentDictionary<DbConnectionString, DbContextOptions<BookStoreContext>> _initedDb =
            new ConcurrentDictionary<DbConnectionString, DbContextOptions<BookStoreContext>>();

        public void InitDb(DbConnectionString dbConnectionString, string fileToSeedDbWith)
        {
            if (!_initedDb.TryAdd(dbConnectionString, dbConnectionString.AsSqlConnectionString<BookStoreContext>()))
                return; // Db already initialized.
            _initedDb[dbConnectionString].EnsureDb().SeedWith(fileToSeedDbWith);
        }

        public void InitDb(IEnumerable<DbConnectionString> dbConnectionStrings, string fileToSeedDbWith) =>
            dbConnectionStrings.ForEach(dbcs => InitDb(dbcs, fileToSeedDbWith));

        public async Task InitDbAsync(DbConnectionString dbConnectionString, string fileToSeedDbWith) =>
            await Task.Run(() => InitDb(dbConnectionString, fileToSeedDbWith));

        public async Task InitDbAsync(IEnumerable<DbConnectionString> dbConnectionStrings, string fileToSeedDbWith) =>
            await Task.WhenAll(dbConnectionStrings.Select(dbcs => InitDbAsync(dbcs, fileToSeedDbWith)).ToArray());

        public BookStoreContext Create(DbConnectionString dbConnectionString, ILineWritable? logSink = null)
        {
            if (!_initedDb.ContainsKey(dbConnectionString))
                throw new InvalidOperationException("Db not initialized.");
            var ctx = _initedDb[dbConnectionString].BuildDbContext();
            if (logSink != null)
                ctx.StartLogging(logSink);
            return ctx;
        }

        public static BookStoreContextFactory Instance => _lazyInstance.Value;
    }
}

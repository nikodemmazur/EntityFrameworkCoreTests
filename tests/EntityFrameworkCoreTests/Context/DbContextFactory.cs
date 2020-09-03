using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Logging;
using Microsoft.EntityFrameworkCore;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFrameworkCoreTests.Context
{
    public class DbContextFactory<T> where T : DbContext
    {
        private readonly ConcurrentDictionary<DbConnectionString, DbContextOptions<T>> _initedDb =
            new ConcurrentDictionary<DbConnectionString, DbContextOptions<T>>();

        private ImmutableList<Action<DbContextOptions<T>>> _actions = ImmutableList.Create<Action<DbContextOptions<T>>>();

        public void InitDb(DbConnectionString dbConnectionString)
        {
            if (!_initedDb.TryAdd(dbConnectionString, dbConnectionString.AsSqlConnectionString<T>()))
                return; // Db already initialized.
            _initedDb[dbConnectionString].EnsureDb();
            _actions.ForEach(a => a.Invoke(_initedDb[dbConnectionString]));
        }

        public void InitDb(IEnumerable<DbConnectionString> dbConnectionStrings) =>
            dbConnectionStrings.ForEach(dbcs => InitDb(dbcs));

        public async Task InitDbAsync(DbConnectionString dbConnectionString) =>
            await Task.Run(() => InitDb(dbConnectionString));

        public async Task InitDbAsync(IEnumerable<DbConnectionString> dbConnectionStrings) =>
            await Task.WhenAll(dbConnectionStrings.Select(dbcs => InitDbAsync(dbcs)).ToArray());

        public T Create(DbConnectionString dbConnectionString, ILineWritable? logSink = null)
        {
            if (!_initedDb.ContainsKey(dbConnectionString))
                throw new InvalidOperationException("Db not initialized.");
            var ctx = _initedDb[dbConnectionString].BuildDbContext();
            if (logSink != null)
                ctx.StartLogging(logSink);
            return ctx;
        }

        public void RegisterOnInit(Action<DbContextOptions<T>> action)
        {
            _actions = _actions.Add(action);
        }

        public void ClearRegistration()
        {
            _actions = ImmutableList.Create<Action<DbContextOptions<T>>>();
        }
    }
}

using EntityFrameworkCoreTests.Context;
using EntityFrameworkCoreTests.Db;
using EntityFrameworkCoreTests.Logging;
using EntityFrameworkCoreTests.TestHelpers;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable disable

namespace EntityFrameworkCoreTests.Tests
{
    public class ConfiguringNonrelationalProperties : TestClassBase
    {
        private readonly ITestOutputHelper _testOutput;

        private BookStoreContext CreateBookStoreContext()
        {
            var dbConnectionString = DbConnectionString.Create(GetType().Name, GetCallerName(1));
            return BookStoreContextFactory.Instance.Create(dbConnectionString, _testOutput.AsLineWriter());
        }

        public ConfiguringNonrelationalProperties(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;

            var dbConnectionStrings = ListFactMethodNames().Select(str => DbConnectionString.Create(GetType().Name, str));
            BookStoreContextFactory.Instance.InitDbAsync(dbConnectionStrings, @"TestData\RawTestData1.json").Wait();
        }

        [Fact]
        public void ContextRepresentsSqlDb()
        {
            using var context = CreateBookStoreContext();

            context.Database.IsSqlServer().Should().BeTrue($"because in {nameof(DbContextOptionsBuilder)}," +
                $" the UseSqlServer extension method has been used");
        }

        /// <summary>
        /// Extra nested class to let Xunit initialize Db in parallel.
        /// </summary>
        public class ShadowPropertyTests : TestClassBase
        {
            private readonly ITestOutputHelper _testOutput;

            public ShadowPropertyTests(ITestOutputHelper testOutput)
            {
                _testOutput = testOutput;
            }

            /// <summary>
            /// Helper entity class for <see cref="BookEntityImplicitlyContainsPriceOfferIdAsTheForeignKey"/>.
            /// </summary>
            public class Principal
            {
                public int PrincipalId { get; set; }
                public string StringColumn { get; set; }
            }

            /// <summary>
            /// Helper entity class for <see cref="BookEntityImplicitlyContainsPriceOfferIdAsTheForeignKey"/>.
            /// </summary>
            public class Dependent
            {
                public int DependentId { get; set; }

                [Required]
                public Principal NavigationProp { get; set; }
            }

            /// <summary>
            /// Helper db context class for <see cref="BookEntityImplicitlyContainsPriceOfferIdAsTheForeignKey"/>.
            /// </summary>
            public class PrincipalDependentContext : DbContext
            {
                public DbSet<Dependent> Dependents { get; set; }

                public PrincipalDependentContext(DbContextOptions<PrincipalDependentContext> options) : base(options) { }
            }

            [Fact]
            public void DependentEntityContainsPrincipalEntityIdAsTheShadowProperty()
            {
                var dbConnectionString = DbConnectionString.Create(nameof(ConfiguringNonrelationalProperties), GetCallerName());
                using var context = 
                    dbConnectionString
                        .AsSqlConnectionString<PrincipalDependentContext>()
                        .EnsureDb()
                        .BuildDbContext()
                        .StartLogging(_testOutput.AsLineWriter());

                Func<object> func = () => 
                    context
                        .Entry(new Dependent())
                        .Property($"{nameof(Dependent.NavigationProp)}{nameof(Principal.PrincipalId)}")
                        .CurrentValue;

                FluentActions
                    .Invoking(func)
                    .Should()
                    .NotThrow($"because EF Core should have created the shadow property representing {nameof(Principal.PrincipalId)} " +
                        $"to be able to link {nameof(Dependent)} to {nameof(Principal)}");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SFCCTools.Core.Migrations;
using Xunit;

namespace SFCCTools.UnitTests.Migrations
{
    public class TestMigrationContext
    {
        /// <summary>
        /// Migration differences should be the missing migrations in the target
        /// migration context
        ///
        /// Parents must be equal
        /// </summary>
        [Fact]
        public void TestDifferenceIsMissingMigrations()
        {
            var migrationsA = "a,b,c,d";
            var migrationsB = "a,b";

            var contextA = MigrationContext.FromCommaSeparatedStrings("a", migrationsA, "");
            var contextB = MigrationContext.FromCommaSeparatedStrings("b", migrationsB, "");

            var difference = contextA.Difference(contextB);

            var expectedDifference = new List<Migration>()
            {
                new Migration() {Id = "c", ParentId = "b"},
                new Migration() {Id = "d", ParentId = "c"}
            };
            Assert.Equal(expectedDifference, difference.Migrations);
        }
        
        [Fact]
        public void TestDifferenceIsMissingMigrations2()
        {
            var migrationsA = "a,b,c,d";
            var migrationsB = "a,b";

            var contextA = MigrationContext.FromCommaSeparatedStrings("a", migrationsA, "");
            var contextB = MigrationContext.FromCommaSeparatedStrings("b", migrationsB, "");

            var difference = contextA.Difference(contextB);

            // intentionally invalid migrations path
            var expectedDifference = new List<Migration>()
            {
                new Migration() {Id = "c", ParentId = "b"},
                new Migration() {Id = "d", ParentId = "a"}
            };
            Assert.NotEqual(expectedDifference, difference.Migrations);
            
            expectedDifference = new List<Migration>()
            {
                new Migration() {Id = "something else", ParentId = "b"},
                new Migration() {Id = "d", ParentId = "c"}
            };
            Assert.NotEqual(expectedDifference, difference.Migrations);
        }
        
        /// <summary>
        /// An unknown migration in the target context means, for instance, the instance
        /// has had migrations applied that are not in the current context (i.e. the source code).
        /// This means we cannot apply the current migrations context and implies an error that should be resolved
        /// by the developer by resetting to the most recent common migration (or changing branches, etc)
        ///
        /// Common errors include applying a migration from a branch
        /// </summary>
        [Fact]
        public void TestThrowsExceptionForUnknownMigration()
        {
            var migrationsA = "a,b,c,d";
            var migrationsB = "a,b,x,y,z";

            var contextA = MigrationContext.FromCommaSeparatedStrings("a", migrationsA, "");
            var contextB = MigrationContext.FromCommaSeparatedStrings("b", migrationsB, "");

            Assert.Throws<MigrationException>(() =>
            {
                var difference = contextA.Difference(contextB);
            });
            try
            {
                var difference2 = contextA.Difference(contextB);
                Assert.True(false); // should never reach
            }
            catch (MigrationException e)
            {
                Assert.True(e.RelatedMigrations.Count == 3);
                Assert.Contains(e.RelatedMigrations, migration => migration.Id == "x");
                Assert.Contains(e.RelatedMigrations, migration => migration.Id == "y");
                Assert.Contains(e.RelatedMigrations, migration => migration.Id == "z");
            }
        }
        
        /// <summary>
        /// Migrations applied out of order also require manual intervention, such as resetting to the
        /// most recent common migration    
        /// </summary>
        [Fact]
        public void TestThrowsExceptionForOutOfOrderMigrations()
        {
            var migrationsA = "a,b,c,d";
            var migrationsB = "b,a,c";

            var contextA = MigrationContext.FromCommaSeparatedStrings("a", migrationsA, "");
            var contextB = MigrationContext.FromCommaSeparatedStrings("a", migrationsB, "");

            Assert.Throws<MigrationException>(() =>
            {
                var difference = contextA.Difference(contextB);
            });
        }
        
        /// <summary>
        /// Hotfixes don't have parents so the order doesn't matter
        /// Difference should be strictly the missing items in the order they appear
        /// in the source Migration Context
        /// </summary>
        [Fact]
        public void TestDifferenceIsMissingHotfixes()
        {
            var migrationsA = "a,b,c,d";
            var migrationsB = "a,b";
            var hotfixesA = "x,y,z";
            var hotfixesB = "m,x,n";

            var contextA = MigrationContext.FromCommaSeparatedStrings("a", migrationsA, hotfixesA);
            var contextB = MigrationContext.FromCommaSeparatedStrings("b", migrationsB, hotfixesB);

            var difference = contextA.Difference(contextB);

            // intentionally invalid migrations path
            var expectedDifference = new List<Migration>()
            {
                new Migration() {Id = "y"},
                new Migration() {Id = "z"}
            };
            Assert.Equal(expectedDifference, difference.Hotfixes);
        }
    }
}
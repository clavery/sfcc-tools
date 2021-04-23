using System;
using System.Collections.Generic;
using System.Linq;

namespace SFCCTools.Core.Migrations
{
    public class MigrationContext
    {
        public IList<Migration> Migrations { get; private set; }
        public IList<Migration> Hotfixes { get; private set; }

        public Migration CurrentMigration { get; private set; }

        /// <summary>
        /// Does this migration context contain all information
        /// </summary>
        public bool IsComplete { get; private set; }

        public MigrationContext()
        {
            Migrations = new List<Migration>();
            Hotfixes = new List<Migration>();
        }

        /// <summary>
        /// Create a MigrationContext from the given migrations.xml and hotfixes.xml
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static MigrationContext FromMigrationsFile(string migrationsFilename, string hotfixesFilename)
        {
            return new MigrationContext()
            {
                IsComplete = true
            };
        }

        /// <summary>
        /// Creates a MigrationContext from the given comma separated strings (i.e. global preferences)
        ///
        /// This context will only have the migration structure and so can only be used to
        /// compare to a full migration context
        /// </summary>
        /// <param name="currentMigration"></param>
        /// <param name="migrationsPath"></param>
        /// <param name="hotfixPath"></param>
        /// <returns></returns>
        public static MigrationContext FromCommaSeparatedStrings(string currentMigration, string migrationsPath,
            string hotfixPath)
        {
            var migrationsIds = migrationsPath.Split(",");
            var migrations = new List<Migration>();
            string last = null;
            foreach (var id in migrationsIds)
            {
                var migration = new Migration()
                {
                    Id = id,
                    ParentId = last
                };
                last = id;
                migrations.Add(migration);
            }

            var current = migrations.First(m => m.Id == currentMigration);

            List<Migration> hotfixes;
            if (!string.IsNullOrEmpty(hotfixPath))
            {
                var hotfixIds = hotfixPath.Split(",");
                hotfixes = hotfixIds.Select(h => new Migration()
                {
                    Id = h
                }).ToList();
            }
            else
            {
                hotfixes = new List<Migration>();
            }

            return new MigrationContext()
            {
                IsComplete = false,
                CurrentMigration = current,
                Migrations = migrations,
                Hotfixes = hotfixes
            };
        }

        /// <summary>
        /// Creates a MigrationContext by comparing with another context returning
        /// the outstanding migrations and hotfixes in this context.
        ///
        /// Raises exceptions if the other context contains migrations not referenced in or
        /// out of order from this context. For this reason this object should normally be a
        /// migrations context from the project and `other` should usually be from an instance
        /// </summary>
        /// <param name="other">The other context to compare to. i.e. the instance</param>
        /// <returns>A new context containing the difference between this and the other; suitable for applying</returns>
        public MigrationContext Difference(MigrationContext other)
        {
            var intersection = this.Migrations.Intersect(other.Migrations).ToList();

            if (intersection.Count != other.Migrations.Count)
            {
                // if the intersection of the context (as compared by id and parent) is not equal to other
                // then other must have migrations we don't know about; meaning un-migratable
                var unrecognizedMigrations = other.Migrations.Except(this.Migrations);
                throw new MigrationException(
                    $"Target migrations context has unrecognized migrations: {String.Join(",", unrecognizedMigrations.Select(m => m.Id))}",
                    relatedMigrations: unrecognizedMigrations.ToList());
            }

            var difference = this.Migrations.Except(other.Migrations).ToList();

            var hotfixDifference = this.Hotfixes.Except(other.Hotfixes).ToList();
            return new MigrationContext()
            {
                Migrations = difference,
                Hotfixes = hotfixDifference
            };
        }
    }
}
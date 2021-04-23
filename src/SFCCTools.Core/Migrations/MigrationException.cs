using System;
using System.Collections;
using System.Collections.Generic;

namespace SFCCTools.Core.Migrations
{
    public class MigrationException : Exception
    {
        public IList<Migration> RelatedMigrations;
        
        public MigrationException(string message, IList<Migration> relatedMigrations = null) : base(message)
        {
            RelatedMigrations = relatedMigrations;
        }
    }
}
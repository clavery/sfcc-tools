using System;

#nullable enable
namespace SFCCTools.Core.Migrations
{
    public class Migration : IEquatable<Migration>
    {
        public Migration()
        {
        }

        public string Id { get; set; }
        public string? Description { get; set; }
        public string? ParentId { get; set; }
        public string? Location { get; set; }

        /// <summary>
        /// Equality of a migration can either be a reference equality or
        /// a comparison of the migrations Id and the parent path
        ///
        /// If the ID is the same but the parent path differences then they cannot
        /// be considered the same as they are not from the same migration context (i.e.
        /// we may be comparing to a context with migrations out of order from the current code version).
        ///
        /// Hotfixes will not have a parent so equal Ids are always the same hotfix.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Migration? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Equals(ParentId, other.ParentId);
        }

        /// <summary>
        /// Similar to the equality logic above a migration is defined by it's Id
        /// AND it's parent
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, ParentId);
        }
    }
}
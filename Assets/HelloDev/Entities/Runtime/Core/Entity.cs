using System;

namespace HelloDev.Entities
{
    // An entity is just a typed integer. Keeping it a struct avoids heap allocation
    // and makes it safe to pass around to Jobs later.
    public readonly struct Entity : IEquatable<Entity>
    {
        public readonly int Id;
        public readonly int Generation; // which "lifetime" of this ID slot we're in

        public Entity(int id, int generation)
        {
            Id = id;
            Generation = generation;
        }

        public bool IsNull => Id < 0;

        public static readonly Entity Null = new(-1, -1);

        public bool Equals(Entity other) => Id == other.Id && Generation == other.Generation;
        public override bool Equals(object obj) => obj is Entity other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Id, Generation);
        public static bool operator ==(Entity a, Entity b) => a.Equals(b);
        public static bool operator !=(Entity a, Entity b) => !a.Equals(b);
        public override string ToString() => $"Entity({Id}, gen={Generation})";
    }
}
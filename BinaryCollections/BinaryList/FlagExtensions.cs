namespace BinaryCollections.BinaryList
{
    public static class FlagExtensions
    {
        internal static DuplicateDensity Modify(this ref DuplicateDensity flags, DuplicateDensity add = DuplicateDensity.None, DuplicateDensity remove = DuplicateDensity.None)
        {
            flags = (flags | add) & ~remove;
            return flags;
        }

        internal static DuplicateDensity Modify(DuplicateDensity flags, DuplicateDensity add = DuplicateDensity.None, DuplicateDensity remove = DuplicateDensity.None)
        {
            return flags.Modify(add, remove);
        }

        internal static DuplicateDensity Add(this ref DuplicateDensity flags, DuplicateDensity add)
        {
            return flags |= add;
        }

        internal static DuplicateDensity Add(DuplicateDensity flags, DuplicateDensity add)
        {
            return flags.Add(add);
        }

        internal static DuplicateDensity Remove(this ref DuplicateDensity flags, DuplicateDensity remove)
        {
            return flags &= ~remove;
        }

        internal static DuplicateDensity Remove(DuplicateDensity flags, DuplicateDensity remove)
        {
            return flags.Remove(remove);
        }

        public static bool HasTheFlag(this DuplicateDensity left, DuplicateDensity right)
        {
            return (left & right) == right;
        }

        public static bool HasAnyFlag(this DuplicateDensity left, DuplicateDensity right)
        {
            return (left & right) != 0;
        }

        internal static DuplicateDensity CheckFor(this ref DuplicateDensity flags, int count = 0)
        {
            if (count < 2)
                flags.Remove(DuplicateDensity.Possible | DuplicateDensity.Full);

            return flags;
        }

        internal static DuplicateDensity CheckFor(DuplicateDensity flags, int count = 0)
        {
            return flags.CheckFor(count);
        }
    }
}
namespace BinaryCollections.BinaryList
{
    internal sealed class OrdinalStringComparer : Comparer<string>
    {
        public override int Compare(string? x, string? y) => string.CompareOrdinal(x, y);
    }
}
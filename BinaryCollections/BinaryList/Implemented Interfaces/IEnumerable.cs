using System.Collections;

namespace BinaryCollections.BinaryList
{
    partial class BinaryList<TValue> : IEnumerable<TValue>
    {
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

namespace BinaryCollections.BinaryList
{
    public interface IInformativeComparer<T> : IComparer<T>
    {
        /// <summary>
        /// Gets a value indicating whether a return value of 0 from the <see cref="IComparer{TValue}.Compare"/> method) <br/>
        /// signifies absolute identity of the elements (full agreement with <see cref="EqualityComparer{T}.Equals"/>).
        /// Allows safely skipping additional hash code checks.
        /// </summary>
        bool CorrespondsToEquals { get; }
    }
}

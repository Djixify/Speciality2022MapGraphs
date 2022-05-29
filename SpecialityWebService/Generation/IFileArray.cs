using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpecialityWebService.Generation
{
    public interface IFileArray<T> where T : IFileItem<T>
    {
        /// <summary>
        /// File where each object index is stored at a constant step-size with the offset in the datafile at each entry
        /// </summary>
        public string IndexFile { get; }
        /// <summary>
        /// Datafile with the actual objects, found using the index file above
        /// </summary>
        public string DataFile { get; }
        /// <summary>
        /// Returns the number of objects in the file
        /// </summary>
        public int Count { get; }
        /// <summary>
        /// Gets the item at index i
        /// </summary>
        /// <param name="i">index to get</param>
        /// <returns>The object at the index</returns>
        public T this[int i] { get; }
        /// <summary>
        /// Write the new item to the index and datafile
        /// </summary>
        /// <param name="item">The item to store</param>
        /// <returns>The index the object was written to</returns>
        public int Add(T item);
        /// <summary>
        /// Removes the specified item index from index file (effectively invalidating the data entry)
        /// </summary>
        /// <param name="i">Index to remove</param>
        /// <param name="clean">If the datafile should remove unused indices, can be expensive</param>
        public void Remove(int i, bool clean);
    }
}

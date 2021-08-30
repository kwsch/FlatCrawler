using System.Collections.Generic;

namespace FlatCrawler.Lib
{
    public interface IFieldNode
    {
        /// <summary>
        /// Gets the raw offset of the Field within the data table.
        /// </summary>
        /// <param name="fieldIndex">Field Index</param>
        /// <returns>For struct values, this will be the location of the data. For reference values, this is the pointer to the object's data table.</returns>
        int GetFieldOffset(int fieldIndex);

        /// <summary>
        /// Gets the raw offset of a Field's Object data table, by treating the Field Offset as a relative pointer.
        /// </summary>
        /// <param name="fieldIndex">Field Index</param>
        /// <param name="data">Full FlatBuffer reference.</param>
        /// <returns>Reference value data table offset.</returns>
        int GetReferenceOffset(int fieldIndex, byte[] data);

        FlatBufferNode? GetField(int fieldIndex) => AllFields[fieldIndex];
        IReadOnlyList<FlatBufferNode?> AllFields { get; }
    }
}

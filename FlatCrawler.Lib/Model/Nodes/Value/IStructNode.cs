using System;

namespace FlatCrawler.Lib;

/// <summary>
/// FlatBuffer node that contains an integral numeric type / floating point type value.
/// </summary>
public interface IStructNode
{
    /// <summary> The type of the node's value. </summary>
    TypeCode Type { get; }
}

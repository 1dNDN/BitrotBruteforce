using System.Collections;
using System.Text;

namespace Bruteforce.TorrentWrapper.BEncoding;

/// <summary>
///     The BEncoded list.
/// </summary>
public class BEncodedList : BEncodedValue, IList<BEncodedValue>, IEnumerable
{
    /// <summary>
    ///     The list.
    /// </summary>
    private readonly List<BEncodedValue> _list;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BEncodedList" /> class.
    /// </summary>
    public BEncodedList()
        : this(new List<BEncodedValue>())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BEncodedList" /> class.
    /// </summary>
    /// <param name="capacity">The capacity.</param>
    public BEncodedList(int capacity)
        : this(new List<BEncodedValue>(capacity))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BEncodedList" /> class.
    /// </summary>
    /// <param name="list">The list.</param>
    public BEncodedList(IEnumerable<BEncodedValue> list)
    {
        _list = new List<BEncodedValue>(list);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BEncodedList" /> class.
    /// </summary>
    /// <param name="value">The value.</param>
    public BEncodedList(List<BEncodedValue> value)
    {
        _list = value;
    }

    /// <summary>
    ///     Gets the number of elements contained in the <see cref="System.Collections.Generic.ICollection`1" />.
    /// </summary>
    /// <value>The number of elements contained in the <see cref="System.Collections.Generic.ICollection`1" />.</value>
    public int Count => _list.Count;

    /// <summary>
    ///     Gets a value indicating whether the <see cref="System.Collections.Generic.ICollection`1" /> is read-only.
    /// </summary>
    /// <value>true if the <see cref="System.Collections.Generic.ICollection`1" /> is read-only; otherwise, false.</value>
    public bool IsReadOnly => false;

    /// <summary>
    ///     Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns>The BEncoded value.</returns>
    public BEncodedValue this[int index]
    {
        get => _list[index];

        set => _list[index] = value;
    }

    /// <summary>
    ///     Adds an item to the <see cref="System.Collections.Generic.ICollection`1" />.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="System.Collections.Generic.ICollection`1" />.</param>
    public void Add(BEncodedValue item) =>
        _list.Add(item);

    /// <summary>
    ///     Removes all items from the <see cref="System.Collections.Generic.ICollection`1" />.
    /// </summary>
    public void Clear() =>
        _list.Clear();

    /// <summary>
    ///     Determines whether the <see cref="System.Collections.Generic.ICollection`1" /> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="System.Collections.Generic.ICollection`1" />.</param>
    /// <returns>
    ///     true if <paramref name="item" /> is found in the <see cref="System.Collections.Generic.ICollection`1" />;
    ///     otherwise, false.
    /// </returns>
    public bool Contains(BEncodedValue item) =>
        _list.Contains(item);

    /// <summary>
    ///     Copies the automatic.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <param name="arrayIndex">Index of the array.</param>
    public void CopyTo(BEncodedValue[] array, int arrayIndex) =>
        _list.CopyTo(array, arrayIndex);

    /// <summary>
    ///     Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>
    ///     A <see cref="System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
    /// </returns>
    public IEnumerator<BEncodedValue> GetEnumerator() =>
        _list.GetEnumerator();

    /// <summary>
    ///     Returns an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>
    ///     An <see cref="System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
    /// </returns>
    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    ///     Determines the index of a specific item in the <see cref="System.Collections.Generic.IList`1" />.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="System.Collections.Generic.IList`1" />.</param>
    /// <returns>
    ///     The index of <paramref name="item" /> if found in the list; otherwise, -1.
    /// </returns>
    public int IndexOf(BEncodedValue item) =>
        _list.IndexOf(item);

    /// <summary>
    ///     Inserts an item to the <see cref="System.Collections.Generic.IList`1" /> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
    /// <param name="item">The object to insert into the <see cref="System.Collections.Generic.IList`1" />.</param>
    public void Insert(int index, BEncodedValue item) =>
        _list.Insert(index, item);

    /// <summary>
    ///     Removes the first occurrence of a specific object from the <see cref="System.Collections.Generic.ICollection`1" />.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="System.Collections.Generic.ICollection`1" />.</param>
    /// <returns>
    ///     true if <paramref name="item" /> was successfully removed from the
    ///     <see cref="System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if
    ///     <paramref name="item" /> is not found in the original <see cref="System.Collections.Generic.ICollection`1" />.
    /// </returns>
    public bool Remove(BEncodedValue item) =>
        _list.Remove(item);

    /// <summary>
    ///     Removes the <see cref="System.Collections.Generic.IList`1" /> item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index) =>
        _list.RemoveAt(index);

    /// <summary>
    ///     Adds the range.
    /// </summary>
    /// <param name="collection">The collection.</param>
    public void AddRange(IEnumerable<BEncodedValue> collection) =>
        _list.AddRange(collection);

    /// <summary>
    ///     Encodes the list to a byte[]
    /// </summary>
    /// <param name="buffer">The buffer to encode the list to</param>
    /// <param name="offset">The offset to start writing the data at</param>
    /// <returns>The number of bytes encoded.</returns>
    public override int Encode(byte[] buffer, int offset)
    {
        var written = 0;

        buffer[offset] = (byte)'l'; // lists start with l

        written++;

        for (var i = 0; i < _list.Count; i++)
            written += _list[i].Encode(buffer, offset + written);

        buffer[offset + written] = (byte)'e'; // lists end with e

        written++;

        return written;
    }

    /// <summary>
    ///     Determines whether the specified <see cref="object" />, is equal to this instance.
    /// </summary>
    /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
    /// <returns>
    ///     <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object obj)
    {
        var other = obj as BEncodedList;

        if (other == null)
            return false;

        for (var i = 0; i < _list.Count; i++)
            if (!_list[i].Equals(other._list[i]))
                return false;

        return true;
    }

    /// <summary>
    ///     Returns a hash code for this instance.
    /// </summary>
    /// <returns>
    ///     A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
    /// </returns>
    public override int GetHashCode()
    {
        var result = 0;

        for (var i = 0; i < _list.Count; i++)
            result ^= _list[i].GetHashCode();

        return result;
    }

    /// <summary>
    ///     Returns the size of the list in bytes
    /// </summary>
    /// <returns>The length in bytes.</returns>
    public override int LengthInBytes()
    {
        var length = 0;

        length += 1; // Lists start with 'l'

        for (var i = 0; i < _list.Count; i++)
            length += _list[i].LengthInBytes();

        length += 1; // Lists end with 'e'

        return length;
    }

    /// <summary>
    ///     Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString() =>
        Encoding.UTF8.GetString(Encode());

    /// <summary>
    ///     Decodes a BEncodedList from the given StreamReader
    /// </summary>
    /// <param name="reader">The reader.</param>
    internal override void DecodeInternal(RawReader reader)
    {
        if (reader.ReadByte() != 'l')
            throw new ArgumentException("Invalid data found. Aborting");

        while (reader.PeekByte() != -1 &&
               reader.PeekByte() != 'e')
            _list.Add(Decode(reader));

        if (reader.ReadByte() != 'e')
            throw new ArgumentException("Invalid data found. Aborting");
    }
}

namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.Globalization;

    using JetBrains.Annotations;

    /// <summary>
    /// A class encapsulating a <see cref="CultureInfo"/>, usable as a key to a dictionary to allow also indexing a <c>null</c> <see cref="CultureInfo"/>.
    /// </summary>
    public class CultureKey : IComparable<CultureKey>, IEquatable<CultureKey>, IComparable
    {
        [NotNull]
        public static readonly CultureKey Neutral = new CultureKey((CultureInfo)null);

        public CultureKey([CanBeNull] string cultureName)
        {
            Culture = cultureName?.ToCulture();
        }

        public CultureKey([CanBeNull] CultureInfo culture)
        {
            Culture = culture;
        }

        [CanBeNull]
        public CultureInfo Culture { get; }

        public override string ToString()
        {
            return ToString(string.Empty);
        }

        [NotNull]
        public string ToString([NotNull] string neutralCultureKey)
        {
            return Culture != null ? "." + Culture.Name : neutralCultureKey;
        }

        #region IComparable/IEquatable implementation

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Culture?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([CanBeNull] object obj)
        {
            return Equals(obj as CultureKey);
        }

        /// <summary>
        /// Determines whether the specified <see cref="CultureKey"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="CultureKey"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="CultureKey"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals([CanBeNull] CultureKey other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left is null)
                return false;
            if (right is null)
                return false;

            return Equals(left.Culture, right.Culture);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            return !InternalEquals(left, right);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        public int CompareTo([CanBeNull] CultureKey other)
        {
            return Compare(this, other);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj" /> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj" />. Greater than zero This instance follows <paramref name="obj" /> in the sort order.
        /// </returns>
        public int CompareTo([CanBeNull] object obj)
        {
            return Compare(this, obj as CultureKey);
        }

        private static int Compare([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (left is null)
                return -1;
            if (right is null)
                return 1;

            return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Implements the operator &gt;.
        /// </summary>
        public static bool operator >([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            return Compare(left, right) > 0;
        }
        /// <summary>
        /// Implements the operator &lt;.
        /// </summary>
        public static bool operator <([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            return Compare(left, right) < 0;
        }
        /// <summary>
        /// Implements the operator &gt;=.
        /// </summary>
        public static bool operator >=([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            return Compare(left, right) >= 0;
        }
        /// <summary>
        /// Implements the operator &lt;=.
        /// </summary>
        public static bool operator <=([CanBeNull] CultureKey left, [CanBeNull] CultureKey right)
        {
            return Compare(left, right) <= 0;
        }

        #endregion

        [NotNull]
        public static implicit operator CultureKey([CanBeNull] CultureInfo culture)
        {
            return new CultureKey(culture);
        }

        [NotNull]
        public static CultureKey Parse([CanBeNull] object item)
        {
            if (item == null)
                return new CultureKey(string.Empty);

            switch (item)
            {
                case string stringValue:
                    return new CultureKey(stringValue);

                case CultureInfo cultureInfo:
                    return new CultureKey(cultureInfo);

                case CultureKey cultureKey:
                    return cultureKey;

                default:
                    throw new InvalidOperationException("Unable to cast object to culture key: " + item);
            }
        }
    }
}

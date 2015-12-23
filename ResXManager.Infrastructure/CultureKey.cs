namespace tomenglertde.ResXManager.Infrastructure
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;

    using TomsToolbox.Core;

    /// <summary>
    /// A class encapsulating a <see cref="CultureInfo"/>, usable as a key to a dictionary to allow also indexing a <c>null</c> <see cref="CultureInfo"/>.
    /// </summary>
    public class CultureKey : IComparable<CultureKey>, IEquatable<CultureKey>, IComparable
    {
        private readonly CultureInfo _culture;

        public static readonly CultureKey Neutral = new CultureKey((CultureInfo)null);

        public CultureKey(string cultureName)
        {
            _culture = cultureName.ToCulture();
        }

        public CultureKey(CultureInfo culture)
        {
            _culture = culture;
        }

        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
        }

        public override string ToString()
        {
            return ToString(string.Empty);
        }

        public string ToString(string neutralCultureKey)
        {
            Contract.Requires(neutralCultureKey != null);
            Contract.Ensures(Contract.Result<string>() != null);

            return _culture.Maybe().Return(c => "." + c.Name) ?? neutralCultureKey;
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
            return _culture.Maybe().Return(l => l.GetHashCode());
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as CultureKey);
        }

        /// <summary>
        /// Determines whether the specified <see cref="CultureKey"/> is equal to this instance.
        /// </summary>
        /// <param name="other">The <see cref="CultureKey"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="CultureKey"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(CultureKey other)
        {
            return InternalEquals(this, other);
        }

        private static bool InternalEquals(CultureKey left, CultureKey right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null))
                return false;
            if (ReferenceEquals(right, null))
                return false;

            return Equals(left._culture, right._culture);
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(CultureKey left, CultureKey right)
        {
            return InternalEquals(left, right);
        }
        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(CultureKey left, CultureKey right)
        {
            return !InternalEquals(left, right);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        public int CompareTo(CultureKey other)
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
        public int CompareTo(object obj)
        {
            return Compare(this, obj as CultureKey);
        }

        private static int Compare(CultureKey left, CultureKey right)
        {
            if (ReferenceEquals(left, right))
                return 0;
            if (ReferenceEquals(left, null))
                return -1;
            if (ReferenceEquals(right, null))
                return 1;

            return string.Compare(left.ToString(), right.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Implements the operator >.
        /// </summary>
        public static bool operator >(CultureKey left, CultureKey right)
        {
            return Compare(left, right) > 0;
        }
        /// <summary>
        /// Implements the operator <.
        /// </summary>
        public static bool operator <(CultureKey left, CultureKey right)
        {
            return Compare(left, right) < 0;
        }
        /// <summary>
        /// Implements the operator >=.
        /// </summary>
        public static bool operator >=(CultureKey left, CultureKey right)
        {
            return Compare(left, right) >= 0;
        }
        /// <summary>
        /// Implements the operator <=.
        /// </summary>
        public static bool operator <=(CultureKey left, CultureKey right)
        {
            return Compare(left, right) <= 0;
        }

        #endregion

        public static implicit operator CultureKey(CultureInfo culture)
        {
            Contract.Ensures(Contract.Result<CultureKey>() != null);
            
            return new CultureKey(culture);
        }
    }
}

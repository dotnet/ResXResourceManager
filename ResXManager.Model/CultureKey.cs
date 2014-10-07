namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Globalization;

    /// <summary>
    /// A class encapsulating a <see cref="CultureInfo"/>, usable as a key to a dictionary to allow also indexing a <c>null</c> <see cref="CultureInfo"/>.
    /// </summary>
    public class CultureKey : IEquatable<CultureKey>
    {
        private readonly CultureInfo _culture;

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
            return _culture.Maybe().Return(c => "." + c.Name, neutralCultureKey);
        }

        #region IEquatable implementation

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

        #endregion
    }
}

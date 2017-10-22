namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    [Serializable]
    public class ImportException : Exception
    {
        public ImportException()
        {
        }

        public ImportException([CanBeNull] string message) : base(message)
        {
        }

        public ImportException([CanBeNull] string message, Exception inner) : base(message, inner)
        {
        }

        protected ImportException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Contract.Requires(info != null);
        }
    }
}
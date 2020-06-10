namespace ResXManager.Model
{
    using System;
    using System.Runtime.Serialization;

    using JetBrains.Annotations;

    [Serializable]
    public class ImportException : Exception
    {
        public ImportException()
        {
        }

        public ImportException(string? message) : base(message)
        {
        }

        public ImportException(string? message, Exception? inner) : base(message, inner)
        {
        }

        protected ImportException([NotNull] SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
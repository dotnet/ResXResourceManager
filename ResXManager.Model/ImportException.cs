namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class ImportException : Exception
    {
        public ImportException()
        {
        }

        public ImportException(string message) : base(message)
        {
        }

        public ImportException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ImportException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
        }
    }
}
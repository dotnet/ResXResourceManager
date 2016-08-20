namespace tomenglertde.ResXManager.Model
{
    using System;

    [Flags]
    public enum ResourceLoadOptions
    {
        /// <summary>
        /// Just load the files.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Start to find code references after load.
        /// </summary>
        FindCodeReferences = 1
    }
}
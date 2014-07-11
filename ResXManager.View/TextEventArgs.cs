namespace tomenglertde.ResXManager.View
{
    using System;

    /// <summary>
    /// Event args for events that deal with text, e.g. text changed or text received.
    /// </summary>
    public class TextEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextEventArgs"/> class.
        /// </summary>
        /// <param name="text">The text associated with the event.</param>
        public TextEventArgs(string text)
        {
            Text = text;
        }

        /// <summary>
        /// Gets the text associated with the event.
        /// </summary>
        public string Text
        {
            get;
            private set;
        }
    }
}

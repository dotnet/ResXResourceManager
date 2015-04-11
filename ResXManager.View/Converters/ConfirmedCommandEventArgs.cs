namespace tomenglertde.ResXManager.View.Converters
{
    using System.ComponentModel;

    public class ConfirmedCommandEventArgs : CancelEventArgs
    {
        public object Parameter
        {
            get;
            set;
        }
    }
}

namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Windows.Input;

    public class NullCommand : ICommand
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ICommand Default = new NullCommand();

        private NullCommand()
        {
        }

        public void Execute(object parameter)
        {
        }

        public bool CanExecute(object parameter)
        {
            return false;
        }

        public event EventHandler CanExecuteChanged 
        {
            add { }
            remove { }
        }
    }
}

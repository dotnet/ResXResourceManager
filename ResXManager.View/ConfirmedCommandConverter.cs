namespace tomenglertde.ResXManager.View
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows.Data;
    using System.Windows.Input;

    public class ConfirmedCommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var command = value as ICommand;

            return command == null ? value : new CommandProxy(this, command);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public event EventHandler<CancelEventArgs> Executing;

        private bool QueryCancelExecution()
        {
            var e = new CancelEventArgs();

            var handler = Executing;
            if (handler != null) 
                handler(this, e);

            return e.Cancel;
        }

        class CommandProxy : ICommand
        {
            private readonly ConfirmedCommandConverter _owner;
            private readonly ICommand _command;

            public CommandProxy(ConfirmedCommandConverter owner, ICommand command)
            {
                Contract.Requires(owner != null);
                Contract.Requires(command != null);
                _owner = owner;
                _command = command;
            }

            public void Execute(object parameter)
            {
                if (_owner.QueryCancelExecution())
                    return;

                _command.Execute(parameter);
            }

            public bool CanExecute(object parameter)
            {
                return _command.CanExecute(parameter);
            }

            public event EventHandler CanExecuteChanged
            {
                add { _command.CanExecuteChanged += value; }
                remove { _command.CanExecuteChanged -= value; }
            }

            [ContractInvariantMethod]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
            private void ObjectInvariant()
            {
                Contract.Invariant(_owner != null);
                Contract.Invariant(_command != null);
            }
        }
    }
}

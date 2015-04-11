namespace tomenglertde.ResXManager.View.Converters
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Windows;
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

        public event EventHandler<ConfirmedCommandEventArgs> Executing;

        public string Query
        {
            get;
            set;
        }

        private void QueryCancelExecution(ConfirmedCommandEventArgs e)
        {
            Contract.Requires(e != null);

            if (!string.IsNullOrEmpty(Query))
            {
                if (MessageBox.Show(Query, Properties.Resources.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            var handler = Executing;
            if (handler != null)
                handler(this, e);
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
                var args = new ConfirmedCommandEventArgs { Parameter = parameter };
                
                _owner.QueryCancelExecution(args);

                if (args.Cancel)
                    return;

                _command.Execute(args.Parameter);
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

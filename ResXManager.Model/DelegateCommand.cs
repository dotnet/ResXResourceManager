namespace tomenglertde.ResXManager.Model
{
    using System;
    using System.Windows;
    using System.Windows.Input;

    /// <summary>
    /// A simple, straight forward delegate command implementation. For usage see MVVM concepts.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// <para/>
        /// No callback is initially set, so they must be set via the property setters. This usage generates easy readable code even if the delegates are inlined.
        /// </summary>
        /// <example>
        /// public ICommand DeleteCommand
        /// {
        ///     get
        ///     {
        ///         return new DelegateCommand
        ///         {
        ///             CanExecuteCallback = delegate
        ///             {
        ///                 return IsSomethingSelected();
        ///             },
        ///             ExecuteCallback = delegate
        ///             {
        ///                 if (IsSomehingSelected())
        ///                 {
        ///                     DelteTheSelection();
        ///                 }
        ///             }
        ///         };
        ///     }
        /// }
        /// </example>
        public DelegateCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class with the execute callback.
        /// <para/>
        /// This version generates more compact code; not recommended for inlined delegates.
        /// </summary>
        /// <param name="executeCallback">The default execute callback.</param>
        /// <example>
        /// public ICommand AboutCommand
        /// {
        ///     get
        ///     {
        ///         return new DelegateCommand(() => ShowAboutBox());
        ///     }
        /// }
        /// </example>
        public DelegateCommand(Action executeCallback)
            : this(null, executeCallback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        /// <param name="canExecuteCallback">The default can execute callback.</param>
        /// <param name="executeCallback">The default execute callback.</param>
        /// <example>
        /// public ICommand EditCommand
        /// {
        ///     get
        ///     {
        ///         return new DelegateCommand(CanEdit, Edit);
        ///     }
        /// }
        /// 
        /// public bool CanEdit(object param)
        /// {
        ///     .....
        /// </example>
        public DelegateCommand(Func<bool> canExecuteCallback, Action executeCallback)
        {
            if (canExecuteCallback != null)
                CanExecuteCallback = () =>
                {
                    try
                    {
                        return canExecuteCallback();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                        return false;
                    }
                };


            if (executeCallback != null)
                ExecuteCallback = () =>
                {
                    try
                    {
                        executeCallback();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                };
        }


        /// <summary>
        /// Gets or sets the predicate to handle the ICommand.CanExecute method.
        /// If unset, ICommand.CanExecute will always return true if ExecuteCallback is set.
        /// </summary>
        public Func<bool> CanExecuteCallback
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the action to handle the ICommand.Execute method.
        /// If unset, ICommand.CanExecute will always return false.
        /// </summary>
        public Action ExecuteCallback
        {
            get;
            set;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        /// <remarks>
        /// The event is forwarded to the <see cref="CommandManager"/>, so visuals bound to the delegate command will be updated
        /// in sync with the system. To explicitly refresh all visuals call CommandManager.InvalidateRequerySuggested();
        /// </remarks>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public bool CanExecute(object parameter)
        {
            if (ExecuteCallback == null)
            {
                return false;
            }

            return CanExecuteCallback == null || CanExecuteCallback();
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public void Execute(object parameter)
        {
            if (ExecuteCallback != null)
            {
                ExecuteCallback();
            }
        }
    }

    /// <summary>
    /// A simple, straight forward delegate command implementation. For usage see MVVM concepts.
    /// </summary>
    public class DelegateCommand<T> : ICommand
    {
        public DelegateCommand()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class with the execute callback.
        /// <para/>
        /// This version generates more compact code; not recommended for inlined delegates.
        /// </summary>
        /// <param name="executeCallback">The default execute callback.</param>
        /// <example>
        /// public ICommand AboutCommand
        /// {
        ///     get
        ///     {
        ///         return new DelegateCommand(() => ShowAboutBox());
        ///     }
        /// }
        /// </example>
        public DelegateCommand(Action<T> executeCallback)
            : this(null, executeCallback)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        /// <param name="canExecuteCallback">The default can execute callback.</param>
        /// <param name="executeCallback">The default execute callback.</param>
        /// <example>
        /// public ICommand EditCommand
        /// {
        ///     get
        ///     {
        ///         return new DelegateCommand{T}(CanEdit, Edit);
        ///     }
        /// }
        /// 
        /// public bool CanEdit(T param)
        /// {
        ///     .....
        /// </example>
        public DelegateCommand(Func<T, bool> canExecuteCallback, Action<T> executeCallback)
        {
            if (canExecuteCallback != null)
                CanExecuteCallback = parameter =>
                {
                    try
                    {
                        return canExecuteCallback(parameter);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                        return false;
                    }
                };


            if (executeCallback != null)
                ExecuteCallback = parameter =>
                {
                    try
                    {
                        executeCallback(parameter);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                };
        }


        /// <summary>
        /// Gets or sets the predicate to handle the ICommand.CanExecute method.
        /// If unset, ICommand.CanExecute will always return true if ExecuteCallback is set.
        /// </summary>
        public Func<T, bool> CanExecuteCallback
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the action to handle the ICommand.Execute method.
        /// If unset, ICommand.CanExecute will always return false.
        /// </summary>
        public Action<T> ExecuteCallback
        {
            get;
            set;
        }

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        /// <remarks>
        /// The event is forwarded to the <see cref="CommandManager"/>, so visuals bound to the delegate command will be updated
        /// in sync with the system. To explicitly refresh all visuals call CommandManager.InvalidateRequerySuggested();
        /// </remarks>
        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>
        /// true if this command can be executed; otherwise, false.
        /// </returns>
        public bool CanExecute(object parameter)
        {
            if (ExecuteCallback == null)
            {
                return false;
            }

            return CanExecuteCallback == null || CanExecuteCallback(SafeCast(parameter));
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public void Execute(object parameter)
        {
            if (ExecuteCallback != null)
            {
                ExecuteCallback(SafeCast(parameter));
            }
        }

        private static T SafeCast(object parameter)
        {
            return parameter == null ? default(T) : (T)parameter;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VMBase.ViewModel
{
	/// <summary>
	/// ICommand implemenation to be used with MVVM design model
	/// </summary>
	/// <typeparam name="T">input type</typeparam>
	public class RelayCommand<T> : ICommand
	{
		#region members
		/// <summary>
		/// command execution to be carried out
		/// </summary>
		private readonly Action<T> _execute;

		/// <summary>
		/// determines that if a command can be carried out  
		/// </summary>
		private readonly Predicate<T> _canExecute;
		public bool IsSelfRequery { get; private set; }

		#endregion members

		#region constructors
		/// <summary>
		/// Create a relay command
		/// </summary>
		/// <param name="execute"></param>
		/// <param name="canExecute"></param>
		/// <param name="enableSelfRequery">disabled, does not affect function</param>
		/// <exception cref="ArgumentNullException">execute cannot be null</exception>
		public RelayCommand(Action<T> execute, Predicate<T> canExecute, bool enableSelfRequery = false)
		{
			// command action cannot be null, otherwise command is meaningless
			if (null == execute)
			{
				throw new ArgumentNullException("execute cannot be null");
			}
			this._execute = execute;
			this._canExecute = canExecute;
			this.IsSelfRequery = false; // disable ability to use requery without CommandManager for now
		}

		/// <summary>
		/// Create a relay command without predicate check
		/// </summary>
		/// <param name="execute"></param>
		/// <param name="enableSelfRequery"></param>
		/// <exception cref="ArgumentNullException">execute cannot be null</exception>
		public RelayCommand(Action<T> execute, bool enableSelfRequery = false)
			: this(execute, null, enableSelfRequery)
		{ }

		#endregion constructors

		#region ICommand implement

		public bool CanExecute(object parameter)
		{
			// may require thread check here to ensure threadsafty
			return (null == this._canExecute) ? true : this._canExecute((T)parameter);
		}

		public event EventHandler CanExecuteChanged
		{
			// attach to command manager,  should modify if we need custom commandmanager
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameter)
		{
			this._execute((T)parameter);
		}

		#endregion ICommand implement

	}

	/// <summary>
	/// ICommand implemenation to be used with MVVM design model
	/// </summary>
	public class RelayCommand : ICommand
	{
		#region members
		/// <summary>
		/// command execution to be carried out
		/// </summary>
		private readonly Action _execute;

		/// <summary>
		/// determines that if a command can be carried out  
		/// </summary>
		private readonly Predicate<object> _canExecute;
		public bool IsSelfRequery { get; private set; }

		#endregion members

		#region constructors
		/// <summary>
		/// Create a relay command
		/// </summary>
		/// <param name="execute"></param>
		/// <param name="canExecute"></param>
		/// <param name="enableSelfRequery">disabled, does not affect function</param>
		/// <exception cref="ArgumentNullException">execute cannot be null</exception>
		public RelayCommand(Action execute, Predicate<object> canExecute, bool enableSelfRequery = false)
		{
			// command action cannot be null, otherwise command is meaningless
			if (null == execute)
			{
				throw new ArgumentNullException("execute cannot be null");
			}
			this._execute = execute;
			this._canExecute = canExecute;
			this.IsSelfRequery = false; // disable ability to use requery without CommandManager for now
		}

		/// <summary>
		/// Create a relay command without predicate check
		/// </summary>
		/// <param name="execute"></param>
		/// <param name="enableSelfRequery"></param>
		/// <exception cref="ArgumentNullException">execute cannot be null</exception>
		public RelayCommand(Action execute, bool enableSelfRequery = false)
			: this(execute, null, enableSelfRequery)
		{ }

		#endregion constructors

		#region ICommand implement

		public bool CanExecute(object parameter)
		{
			// may require thread check here to ensure threadsafty
			return (null == this._canExecute) ? true : this._canExecute(parameter);
		}

		public event EventHandler CanExecuteChanged
		{
			// attach to command manager,  should modify if we need custom commandmanager
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		public void Execute(object parameter)
		{
			this._execute();
		}

		#endregion ICommand implement

	}

}

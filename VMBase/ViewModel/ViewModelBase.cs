using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace VMBase.ViewModel
{
    /// <summary>
    /// Base for ViewModel implementation.  Implements INotifyPropertyChanged and IDisposable
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        #region constructor
        /// <summary>
        /// Default constructor that will be called by the inherited class
        /// </summary>
        protected ViewModelBase()
        {

        }

        #endregion constructor

        /// <summary>
        /// Implements INotifyProperty.PropertyChanged event.  Subscribes to this event
        /// to be notified when a property is modified.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// flag to enable exception checking if the property name being supplied to raise event is valid
        /// </summary>
        internal bool ThrowOnInvlaidPropertyName = false;

        /// <summary>
        /// Enables debugging of property name.  
        /// </summary>
        /// <param name="propertyName"></param>
        [Conditional("Debug")]
        [DebuggerStepThrough]
        private void VerifyPropertyName(string propertyName)
        {
            if (null == TypeDescriptor.GetProperties(this)[propertyName])
            {
                if (this.ThrowOnInvlaidPropertyName)
                {
                    throw new ArgumentException("Invalid PropertyName: " + propertyName);
                }
                else
                {
                    Debug.Fail(propertyName);
                }
            }
        }

        /// <summary>
        /// Execute all handlers subscribed to the event <see cref="PropertyChanged"/>
        /// </summary>
        /// <param name="propertyName"></param>
        public void RaisePropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            if (null != this.PropertyChanged)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool _disposed = false;

        /// <summary>
        /// Implements IDisposable's dispose. Calls <see cref="OnDispose"/> the first time this
        /// method is called.
        /// </summary>
        public void Dispose()
        {
            if (!this._disposed)
            {
                this.OnDispose();
                this._disposed = true;
            }
        }

        /// <summary>
        /// For subclasses that needs to specify resources to dispose, overriding this method
        /// to provide the necessaries steps.  This method will be called once by <see cref="Dispose"/>
        /// </summary>
        protected virtual void OnDispose()
        { }

        /// <summary>
        /// Sets a member with value while calling RaisePropertyChanged with propertyName
        /// <para>
        /// <code>
        /// this._member = value;<br />
        /// this.RaisePropertyChanged("Member");
        /// </code>
        /// is the same as using
        /// <code>
        /// this.SetAndraise(out this._member, "Member", value);
        /// </code>
        /// </para>
        /// </summary>
        /// <typeparam name="T">type of member</typeparam>
        /// <param name="member">reference to a value to be changed</param>
        /// <param name="propertyName">name of property to be raised</param>
        /// <param name="value">the new value to be assigned to member</param>
        protected void SetAndRaise<T>(out T member, string propertyName, T value)
        {
            member = value;
            this.RaisePropertyChanged(propertyName);
        }

        /// <summary>
        /// Raise property change only if value being set differs from original
        /// </summary>
        /// <typeparam name="T">IComparable values</typeparam>
        /// <param name="member">reference to a value to be changed</param>
        /// <param name="propertyName">name of property to be raised</param>
        /// <param name="value">the new value to be assigned to member</param>
        protected void SetAndRaiseOnNotEqual<T>(ref T member, string propertyName, T value)
            where T : IComparable
        {
            if((member!=null && member.CompareTo(value)!=0) || (member== null && value !=null))
            {
                SetAndRaise(out member, propertyName, value);
            }
        }

    }



}
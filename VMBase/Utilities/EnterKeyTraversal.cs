using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VMBase.Utilities
{
    /// <summary>
    /// Attached event that will let enter key entry move focus to next focus object
    /// </summary>
    public static class EnterKeyTraversal
    {
        /// <summary>
        /// Attached property that is used to attach the event handler to a dependency object
        /// </summary>
        public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
            "Attach",
            typeof(bool),
            typeof(EnterKeyTraversal),
            new FrameworkPropertyMetadata(AttachPropertyChangedCallback)
            );

        // need these methods to act as Setter and Geter for Property "Attach"
        // otherwise the property cannot be accessed by Style setter
        #region Property "Attach" setter and getter

        /// <summary>
        /// Get value of Attach from a dependency object
        /// </summary>
        /// <param name="d">object to get value from</param>
        /// <returns>value of Attached, if not assigned, defaults to false</returns>
        public static bool GetAttach(DependencyObject d)
        {
            return (bool)(d.GetValue(AttachProperty) ?? false);
        }

        /// <summary>
        /// Set value of Attach to an dependency object
        /// </summary>
        /// <param name="d">object to set value to</param>
        /// <param name="value">enable or disable attach</param>
        public static void SetAttach(DependencyObject d, bool value)
        {
            d.SetValue(AttachProperty, value);
        }

        #endregion Attach Property setter and getter

        /// <summary>
        /// Attach/Detach handlers from the dependency object depending on the value of Attach
        /// </summary>
        /// <param name="d">object to be set to, needs to be framework element for handler to affect</param>
        /// <param name="e">event parameter</param>
        private static void AttachPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var fe = d as FrameworkElement;

            d.SetValue(AttachProperty, e.NewValue);

            if (null == fe)
            {
                return;
            }

            if ((bool)e.NewValue && (bool)e.NewValue != (bool)e.OldValue)
            {
                AttachPropertyAttachHandlers(fe);
            }
            else
            {
                AttachPropertyDetachHandlers(fe);
            }

        }

        /// <summary>
        /// Events to handle
        /// </summary>
        /// <param name="fe">element to handle from</param>
        private static void AttachPropertyAttachHandlers(FrameworkElement fe)
        {
            fe.Unloaded += AttachProperty_Unloaded;
            fe.PreviewKeyDown += AttachProperty_PreviewKeyDown;
        }

        /// <summary>
        /// Detach events
        /// </summary>
        /// <param name="fe">element to detach from</param>
        private static void AttachPropertyDetachHandlers(FrameworkElement fe)
        {
            fe.Unloaded -= AttachProperty_Unloaded;
            fe.PreviewKeyDown -= AttachProperty_PreviewKeyDown;
        }

        /// <summary>
        /// Handler for Unloaded event,  will perform detach action.
        /// </summary>
        /// <param name="sender">element that sent the event</param>
        /// <param name="e">event arguement</param>
        private static void AttachProperty_Unloaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            if (null == fe)
            {
                return;
            }
            AttachPropertyDetachHandlers(fe);
        }

        /// <summary>
        /// Handler for PreviewkeyDown event,  will check if enter key was hit.  If hit,  it will
        /// direct to next focusable element instead of passing the enter key to element
        /// </summary>
        /// <param name="sender">object that triggered event</param>
        /// <param name="e">event args</param>
        private static void AttachProperty_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var fe = e.OriginalSource as FrameworkElement;
                if (null != fe)
                {
                    fe.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                }
            }
        }

    }
}

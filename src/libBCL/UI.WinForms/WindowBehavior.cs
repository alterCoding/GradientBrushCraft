using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace AltCoD.UI.WinForms
{
	using UI.Win32.Windows;

    public abstract class WindowBehavior
    {
		protected WindowBehavior(IntPtr wnd) { Target = wnd; }

        /// <summary>
        /// the target window handle
        /// </summary>
        public IntPtr Target { get; }

		public abstract void HandleWindowProc(ref Message m);
    }

	/// <summary>
	/// Helper to be used when a parent window wants its child windows to be centered at activation
	/// </summary>
	/// <remarks>
	/// This helper may be useless at 1st glance when the child window is a Form since we can use the 
	/// <see cref="Form.StartPosition"/> property, but if the parent window property is undefined at child level, the
	/// setwindowpos() (according to the StartPosition property) will be eaten ... whereas _this helper will use the
	/// Form owner (defined at construction time), thus enforcing that any child is shown centered. In another words,
	/// _this behavior takes precedence over the forms properties
	/// </remarks>
	public sealed class CenterChildWindowBehavior : WindowBehavior
    {
		/// <summary>
		/// NOTE: must not be called from From constructor (as the Handle is not ready)
		/// </summary>
		/// <param name="owner">the parent window</param>
		public CenterChildWindowBehavior(Form owner) 
			: base(owner.Handle) 
		{
			owner.FormClosing += (s, e) => _closing = true;
		}

        /// <summary>
        /// To be called from the parent window proc method (WndProc). Keep in mind that the window proc may be called
        /// early (meaning when Handle is still not created, thus _this instance should not be available yet)
        /// </summary>
        /// <param name="m"></param>
        public override void HandleWindowProc(ref Message m)
        {
            // handle WM_ACTIVATE/deactivated
			// when a child window is activated, a deactivate message is sent to the parent
			//
			// NOTE: we enforce that the owner isn't in closing state (when the form is closing, the deactivation event
			//is dispatched and the lParam would contain the PARENT wnd ... thus we wouldn't want to raise a spurious
			//SETWINDOWPOS for the parent, which would be very unfriendly)

            if (m.Msg == Native.WM_ACTIVATE)
            {
				IntPtr child_wnd = m.LParam;
				if (child_wnd == IntPtr.Zero) return;

				var this_state = (int)m.WParam == Native.WA_INACTIVE ? WindowState.deactivated : WindowState.activated;

				if (this_state == WindowState.deactivated && !_closing)
				{
                    //one could imagine to not taking precedence over the Form.StartPosition property
                    //var child = Control.FromHandle(m.LParam);
                    //if (child is Form form && form.StartPosition != FormStartPosition.CenterParent) return;

                    if (!_childStates.ContainsKey(child_wnd))
                    {
						//do it once
                        Native.CenterWindow(Target, m.LParam);
                    }

                    _childStates[child_wnd] = WindowState.activated;
                }
                else if (this_state == WindowState.activated)
                {
                    _childStates[child_wnd] = WindowState.deactivated;
                }
            }
        }

		private bool _closing;

		private enum WindowState { undefined, activated, deactivated }

		/// <summary>
		/// we keep child states because we couldn't find a reliable way to know if a child window has been activated
		/// for the first time or a subsequent time. <br/>
		/// Motivation: we don't want to (re)center a window again, in case of deactivation/reactivation of the 
		/// window (only relevant for non-modal windows)
		/// </summary>
		private readonly Dictionary<IntPtr, WindowState> _childStates = new Dictionary<IntPtr, WindowState>();
    }

}

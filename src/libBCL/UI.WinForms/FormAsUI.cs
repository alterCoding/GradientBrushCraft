using System;
using System.Windows.Forms;

namespace AltCoD.UI.WinForms
{
    using AltCoD.BCL;

    /// <summary>
    /// Abstract UI implementation for target of type Forms.MessageBox
    /// </summary>
    internal class FormAsUI : IUITextContract
    {
        #region interface IIOstreamUI

        public void Error(string message, string header)
        {
            MessageBox.Show(message, header, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void Info(string message, string header = "")
        {
            MessageBox.Show(message, header, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void Out(string message)
        {
            MessageBox.Show(message);
        }
        public void Out(string tag, string message, string header)
        {
            MessageBox.Show(message, string.Concat(header, " [", tag, "]"));
        }

        public MessageAction Show(string message, MessageAction action, string header)
        {
            var result = MessageBox.Show(message, header, mapButtons(action));
            return mapResult(result);
        }

        public MessageAction Show(string message, MessageAction action, string header, MessageContext context, string prompt)
        {
            if (!string.IsNullOrWhiteSpace(prompt)) message = string.Concat(message, Environment.NewLine, prompt);

            var result = MessageBox.Show(message, header, mapButtons(action), mapContext(context));
            return mapResult(result);
        }

        #endregion

        private static MessageBoxButtons mapButtons(MessageAction buttons)
        {
            if (buttons == MessageAction.ok) return MessageBoxButtons.OK;
            else if (buttons == MessageActions.YesNo) return MessageBoxButtons.YesNo;
            else if (buttons == MessageActions.YesNoCancel) return MessageBoxButtons.YesNoCancel;
            else if (buttons == MessageActions.OkCancel) return MessageBoxButtons.OKCancel;
            else if (buttons == MessageActions.RetryCancel) return MessageBoxButtons.RetryCancel;
            else if (buttons == MessageActions.AbortRetryIgnore) return MessageBoxButtons.AbortRetryIgnore;

            else throw new ArgumentException(
                $"unable to map {nameof(MessageAction)}.{buttons} to {nameof(MessageBoxButtons)}");
        }

        private static MessageAction mapResult(DialogResult result)
        {
            if (result == DialogResult.OK) return MessageAction.ok;
            else if (result == DialogResult.Yes) return MessageAction.yes;
            else if (result == DialogResult.No) return MessageAction.no;
            else if (result == DialogResult.Cancel) return MessageAction.cancel;
            else if (result == DialogResult.Abort) return MessageAction.abort;
            else if (result == DialogResult.Ignore) return MessageAction.ignore;
            else if (result == DialogResult.Retry) return MessageAction.retry;

            else throw new ArgumentException(
                $"unable to map {nameof(DialogResult)}.{result} to {nameof(MessageAction)}");
        }

        private static MessageBoxIcon mapContext(MessageContext context)
        {
            if (context == MessageContext.envIssue) return MessageBoxIcon.Error;
            else if (context == MessageContext.envQuestion) return MessageBoxIcon.Question;
            else return MessageBoxIcon.None;
        }
    }
}

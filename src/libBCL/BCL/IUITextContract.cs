using System;

namespace AltCoD.BCL
{
    internal enum MessageType
    {
        logInfo,
        logError
    }

    [Flags]
    public enum MessageAction
    {
        /// <summary>
        /// DialogResult.None
        /// </summary>
        none = 0,
        /// <summary>
        /// DialogResult.OK
        /// </summary>
        ok = 0x1,
        /// <summary>
        /// DialogResult.Cancel
        /// </summary>
        cancel = 0x2,
        /// <summary>
        /// DialogResult.Abort
        /// </summary>
        abort = 0x4,
        /// <summary>
        /// DialogResult.Retry
        /// </summary>
        retry = 0x8,
        /// <summary>
        /// DialogResult.Ignore
        /// </summary>
        ignore = 0x10,
        /// <summary>
        /// DialogResult.Yes
        /// </summary>
        yes = 0x20,
        /// <summary>
        /// DialogResult.No
        /// </summary>
        no = 0x40,
        help = 0x1000,
        custom1 = 0x10000,
        custom2 = 0x20000,
        custom3 = 0x40000
    }

    public enum MessageContext
    {
        none,
        envQuestion,
        envIssue
    }

    public static class MessageActions
    {
        public const MessageAction YesNo = MessageAction.yes | MessageAction.no;
        public const MessageAction OkCancel = MessageAction.ok | MessageAction.cancel;
        public const MessageAction AbortRetryIgnore = MessageAction.abort | MessageAction.retry | MessageAction.ignore;
        public const MessageAction YesNoCancel = MessageAction.yes | MessageAction.no | MessageAction.cancel;
        public const MessageAction RetryCancel = MessageAction.retry | MessageAction.cancel;
    }

    /// <summary>
    /// An interface to abtract stream such as informational outputs to target (console or message-box)
    /// </summary>
    public interface IUITextContract
    {
        void Out(string message);
        void Out(string tag, string message, string header = "");
        void Error(string message, string header = "");
        void Info(string message, string header = "");

        MessageAction Show(string message, MessageAction action, string header = "");

        MessageAction Show(string message, MessageAction action, string header = "",
            MessageContext context = MessageContext.none, string prompt = "");
    }
}

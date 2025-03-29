using System;
using System.Windows.Forms;

namespace AltCoD.UI.WinForms
{
    using Win32.Windows;

    public static class TextBoxBaseExtensions
    {
        /// <summary>
        /// Alternative to the <see cref="TextBoxBase.Cut"/> method, this latter copying the selection to the clipboard.
        /// The <see cref="DeleteSelection(TextBoxBase)"/> method remove the selected text w/o polluting the clipboard
        /// </summary>
        /// <param name="textbox"></param>
        public static void DeleteSelection(this TextBoxBase textbox)
        {
            //WM_CLEAR suits better than the WM_CUT message (implemented by the regular TextBoxBase.Cut() method)
            //NOTE:the TextBoxBase.Clear() method doesn't clear the selection ... but erase ALL
            //
            Native.SendMessage(textbox.Handle, Native.WM_CLEAR, 0, 0);
        }
    }

    public readonly struct TextBoxContent
    {
        public enum ContentType { text, rtf };

        public static TextBoxContent RTF(string content) => new TextBoxContent(content, ContentType.rtf);
        public static TextBoxContent Text(string content) => new TextBoxContent(content, ContentType.text);

        public static readonly TextBoxContent Empty = new TextBoxContent(string.Empty, ContentType.text);

        public TextBoxContent(string content, ContentType type)
        {
            Content = type;
            Value = content;
        }

        public ContentType Content { get; }
        public string Value { get; }
        public bool IsEmpty => string.IsNullOrEmpty(Value);
    }
}

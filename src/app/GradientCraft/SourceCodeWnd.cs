using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;

namespace AltCoD.GradientCraft
{
    using UI.WinForms;

    /// <summary>
    /// A modal window to display a C# source code content <br/>
    /// Main feature: syntax coloring. Refer to <see cref="SourceCodeParser"/> for usage
    /// </summary>
    public partial class SourceCodeWnd : Form
    {
        public SourceCodeWnd(Action explain)
        {
            InitializeComponent();

            btnClose.Click += (s, e) => Close();

            btnCopy.Click += onCopySourceCode;

            btnInfo.Click += (s, e) =>
            {
                string info =
@"The provided source code reflects the parameterized shape and gradient brush, as defined in the main window application.
It's only pseudo code manually assembled from the application underlying properties and attributes, but it should be operating for testing or educational purpose.
It contains in a single code block both the initialization and the drawing stuffs, which is not the best coding practise indeed.
In real effective source code, we obviously wouldn't want to instantiate and initialize all static or pseudo constant graphics primitives at each call 
(referring to GraphicsPath, Brush or Pen instances, Bitmap and so on ...).
As long as Size or colors don't change, they can be instantiated once.";

                MessageBox.Show(this, info, "Source code sample context ... what is this fucking source code",
                    MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            };

            btnExplain.Click += (s, e) => explain();

            radCopyText.Checked = true;

            _parser = new SourceCodeParser(editSource);
        }

        public string SourceCode
        {
            get => _sourceCode;
            set
            {
                _sourceCode = value;
                editSource.Text = value;
                _parser.Parse();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            m_wndBehavior = new CenterChildWindowBehavior(this);

            base.OnLoad(e);
        }

        protected override void WndProc(ref Message m)
        {
            m_wndBehavior?.HandleWindowProc(ref m);

            base.WndProc(ref m);
        }

        private void onCopySourceCode(object sender, EventArgs e)
        {
            if (radCopyText.Checked) Clipboard.SetText(_sourceCode);
            else if(radCopyRTF.Checked) Clipboard.SetData(DataFormats.Rtf, editSource.Rtf);
        }

        /// <summary>
        /// naïve parsing (for reminder) for a single keyword (no true use case) <br/>
        /// should be superceeded by regular expression matching
        /// </summary>
        /// <param name="keyword"></param>
        private void highlightKeyword(string keyword)
        {
            int actual = editSource.SelectionStart;

            int position = 0;
            while((position = editSource.Text.IndexOf(keyword, position)) != -1)
            {
                editSource.Select(position, keyword.Length);
                editSource.SelectionColor = Color.Red;
                position += keyword.Length;
            }

            editSource.Select(actual, 0);
        }

        private readonly SourceCodeParser _parser;
        /// <summary>
        /// the original content
        /// </summary>
        private string _sourceCode;

        private CenterChildWindowBehavior m_wndBehavior;
    }
}

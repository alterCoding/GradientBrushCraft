using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace AltCoD.GradientCraft
{
    using UI.WinForms;

    /// <summary>
    /// Display an informational text block which summarizes the GDI api key points to instantiate the gradient as
    /// defined in the application-ui. <br/>
    /// Basic informal text coloration is provided through a few tokens (see the <see cref="InfoContent"/> property).
    /// Tokens delimit the fields to be styled, they are removed from the text when done. See <see cref="GenericParser"/>
    /// for the rules
    /// </summary>
    public partial class GradientInfoWnd : Form
    {
        /// <summary>
        /// </summary>
        /// <param name="doNotClose">[TRUE] remove the button to close the tool window (but it can still be closed 
        /// using the usual command of the title bar)</param>
        private GradientInfoWnd(bool doNotClose, TextBoxContent content, IGradientChangesSource source)
        {
            _doNotClose = doNotClose;

            InitializeComponent();

            _parser = new GenericParser(editContent);

            if (_doNotClose) btnClose.Visible = false;
            btnClose.Click += (s, e) => { if (!_doNotClose) Close(); };

            chkWordWrap.CheckedChanged += (s, e) => editContent.WordWrap = (s as CheckBox).Checked;

            InfoContent = content;

            if (source != null)
            {
                _source = source;
                source.OnGradientChange += onGradientChange;
            }
        }

        public GradientInfoWnd(bool doNotClose, TextBoxContent content)
            : this(doNotClose, content, null)
        { }
        public GradientInfoWnd(bool doNotClose, IGradientChangesSource source)
            : this(doNotClose, source.GetExplanation(), source)
        { }

        public TextBoxContent GetInfoContent(TextBoxContent.ContentType type)
        {
            if (type == TextBoxContent.ContentType.rtf) return TextBoxContent.RTF(editContent.Rtf);
            else if (type == TextBoxContent.ContentType.text) return TextBoxContent.Text(editContent.Text);
            else return TextBoxContent.Empty; 
        }

        /// <summary>
        /// GET/SET the formatted or unformatted data content. <br/>
        /// When unformatted data is provided, the expected format is: <br/>
        /// - &lt;type-name&gt;<br/>
        /// - {property-name} <br/>
        /// - all but those tokens will be output as is
        /// </summary>
        public TextBoxContent InfoContent
        {
            set
            {
                if(value.Content == TextBoxContent.ContentType.text)
                {
                    _content = value.Value;

                    editContent.Text = _content;
                    _parser.Parse();
                }
                else if(value.Content == TextBoxContent.ContentType.rtf)
                {
                    editContent.Rtf = value.Value;
                    _content = editContent.Text;
                }

                //the ReadOnly property cannot be set to TRUE otherwise the content cannot be editable (cut/clear)
                //even programmatically. 
                //the SelectionProtected property can be used instead to mimic the readonly behavior
                editContent.SelectAll();
                editContent.SelectionProtected = true;
                //however, the content may be appended/edited from the end (i.e only the 'value' content is protected)
                editContent.Select(0, 0);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            if (_source != null) _source.OnGradientChange -= onGradientChange;
        }

        private bool isRelevant(GradientChangeEvent change)
        {
            //any simple property value change doesn't actually immediately update the info block
            return change.ChangeType == GradientChangeEvent.EventType.gradientType ||
                    change.ChangeType == GradientChangeEvent.EventType.addRemoveProperty;
        }

        private void onGradientChange(object sender, GradientChangeEvent e)
        {
            if (isRelevant(e))
            {
                //update now
                InfoContent = _source.GetExplanation();
            }
            else
            {
                //deferring updates ... assuming a lot of notifications are expected to come

                //NOTE: to need to MT access management, as the active job is done in main thread

                _changesPending++;

                if (_changesPending == 1)
                {
                    //no changes pending, start a task that will pulse a single update at expiration
                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        _source.BeginInvoke(new Action(pulseChangesPending), Type.EmptyTypes);
                    });
                }
            }
        }

        private void pulseChangesPending()
        {
            //MUST be called in main thread
            _changesPending = 0;
            InfoContent = _source.GetExplanation();
        }

        private readonly GenericParser _parser;

        private int _changesPending;

        //original content
        private string _content;

        //when one wants a live update of the content (may be null)
        private readonly IGradientChangesSource _source;

        private readonly bool _doNotClose;
    }
}

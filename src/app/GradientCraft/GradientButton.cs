using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AltCoD.GradientCraft
{
    using UI.WinForms;
    using BCL.Drawing;

    /// <summary>
    /// A vanilla button with a flat rendering based on a 2-colors gradient
    /// </summary>
    /// <remarks>
    /// A button refinement ... just for applying a gradient background ... common forms controls are soooo limited 
    /// when it comes to cope with non-stock behavior/rendering
    /// </remarks>
    class GradientButton : Button
    {
        public GradientButton()
        {
            FlatStyle = FlatStyle.Flat;

            //I'm unsatisfied with the behavior of the DefaultValue attribute for properties (setter no called)
            Color1 = SystemColors.Control;
            Color2 = SystemColors.ButtonFace;
            BorderColor = SystemColors.ControlDark;
        }

        /// <summary>
        /// ForeColor is overriden because the class attempts to define an accurate default value which suits a decent 
        /// contrast between the <see cref="Color1"/> and <see cref="Color2"/> properties <br/>
        /// If a value is explicitely provided with, the default value assignment should be discarded
        /// </summary>
        public override Color ForeColor 
        { 
            get => base.ForeColor;
            set
            {
                _foreColor = value;
                base.ForeColor = value;
                defineColors();
            }
        }

        public Color Color1
        {
            get => _color1;
            set
            {
                if (_color1 == value) return;

                _color1 = value;
                _foreColor = Color.Empty;
                defineColors();
                updateGradient();
            }
        }

        public Color Color2
        {
            get => _color2;
            set
            {
                if (_color2 == value) return;

                _color2 = value;
                _foreColor = Color.Empty;
                defineColors();
                updateGradient();
            }
        }

        public Color BorderColor
        {
            get => _borderColor;

            set
            {
                if (_borderColor == value) return;

                _borderColor = value;
                defineColors();

                FlatAppearance.BorderColor = value;
                _borderPen?.Dispose();
                _borderPen = null;

                Invalidate();
            }
        }

        public Blend GradientBlend
        {
            get => _gradientBlend;
            set
            {
                _gradientBlend = value;

                updateGradient();
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;

            g.FillRectangle(backgroundBrush, _rcBounds);

            var bounds = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width - 1, ClientRectangle.Height/2);
            if (_isPressed) bounds.Offset(0, ClientRectangle.Height / 2);
            g.FillRectangle(_gradientBrushHighLight, bounds);

            if (Focused)
            {
                g.DrawRectangle(SystemPens.Highlight, _rcBounds);
                g.DrawRectangle(SystemPens.Highlight, Rectangle.Inflate(_rcBounds, -1, -1));
            }
            else
            {
                if (_isHotState) g.DrawRectangle(SystemPens.Highlight, _rcBounds);
                else g.DrawRectangle(borderPen, _rcBounds);
            }

            TextRenderer.DrawText(g, Text, Font, _rcBounds, _actualForeColor, Color.Transparent,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        #region overriden handlers

        protected override void OnCreateControl()
        {
            //useless as we don't change any stock properties, but to be thought about otherwise
            //SuspendLayout();

            _rcBounds = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width - 1, ClientRectangle.Height - 1);

            defineColors(); //be sure that effective properties are fully defined
 
            base.OnCreateControl();
            //ResumeLayout();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            defineColors();
            update();
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHotState = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHotState = false;
            Invalidate();
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            updateGradient(); //gradient bounds need to be updated
        }
        protected override void OnKeyDown(KeyEventArgs kevent)
        {
            base.OnKeyDown(kevent);
            _isPressed = kevent.KeyCode == Keys.Enter || kevent.KeyCode == Keys.Space;
        }
        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            base.OnMouseDown(mevent);
            _isPressed = true;
        }
        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            base.OnMouseUp(mevent);
            _isPressed = false;
        }
        protected override void OnKeyUp(KeyEventArgs kevent)
        {
            base.OnKeyUp(kevent);
            _isPressed = false;
        }

        #endregion

        private void update()
        {
            updateGradient();
            _borderPen?.Dispose();
            _borderPen = null;
        }

        /// <summary>
        /// colors attributes are duplicated to cope with the dual state (enabled/disabled) of the control
        /// </summary>
        private void defineColors()
        {
            if (Enabled)
            {
                _actualColor1 = _color1;
                _actualColor2 = _color2;

                if (_foreColor.IsEmpty)
                {
                    Color color = RGB.FindBestContrastColor(_actualColor1, _actualColor2);

                    base.ForeColor = color;
                    _actualForeColor = color;
                }
                else
                {
                    _actualForeColor = _foreColor;
                }

                _actualBorderColor = _borderColor;
            }
            else
            {
                _actualColor1 = _color1.ToGrayScale();
                _actualColor2 = _color2.ToGrayScale();
                _actualForeColor = ForeColor.ToGrayScale(lowContrast: true);
                _actualBorderColor = _borderColor.ToGrayScale(lowContrast:true);
            }
        }

        private void updateGradient()
        {
            _gradientBrush?.Dispose();
            _gradientBrush = null;
            _gradientBrushHot?.Dispose();
            _gradientBrushHot = null;
            _gradientBrushHighLight?.Dispose();
            _gradientBrushHighLight = null;

            Invalidate();
        }

        /// <summary>
        /// the backround brush as such is available in different brushes depending on the actual control state
        /// </summary>
        private Brush backgroundBrush
        {
            get
            {
                if (_gradientBrush == null)
                {
                    _gradientBrush = new LinearGradientBrush(ClientRectangle, 
                        _actualColor1, _actualColor2, LinearGradientMode.Horizontal);
                    
                    //when mouse hovering
                    _gradientBrushHot = new LinearGradientBrush(ClientRectangle, 
                        ControlPaint.Light(_actualColor1), ControlPaint.Light(_actualColor2), LinearGradientMode.Horizontal);

                    //a glassy effect
                    var rc = new Rectangle(ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Width, ClientRectangle.Height / 2);
                    _gradientBrushHighLight = new LinearGradientBrush(rc,
                        Color.FromArgb(150, Color.White), Color.FromArgb(50, Color.White), LinearGradientMode.Vertical);

                    if (_gradientBlend != null)
                    {
                        _gradientBrush.Blend = _gradientBlend;
                        _gradientBrushHot.Blend = _gradientBlend;
                    }
                }

                return _isHotState || _isPressed ? _gradientBrushHot : _gradientBrush;
            }
        }

        private Pen borderPen
        {
            get
            {
                if(_borderPen == null) _borderPen = new Pen(_actualBorderColor);

                return _borderPen;
            }
        }

        /** the actual main brush (enabled or disabled state) */
        private LinearGradientBrush _gradientBrush;
        /** transient brush when hovering */
        private LinearGradientBrush _gradientBrushHot;
        /** an additional partial transparent brush to provide glassy effect */
        private LinearGradientBrush _gradientBrushHighLight;

        private Blend _gradientBlend;

        private Color _color1;
        private Color _actualColor1;
        private Color _color2;
        private Color _actualColor2;
        private Color _foreColor;
        private Color _actualForeColor;
        private Color _borderColor;
        private Color _actualBorderColor;
        private Pen _borderPen;

        private Rectangle _rcBounds;
        /** mouse hovering */
        private bool _isHotState;
        private bool _isPressed;
    }
}

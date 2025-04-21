using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace AltCoD.GradientCraft
{
    using UI.WinForms;
    using UI.Win32.Windows;
    using BCL.Drawing;
    using BCL.Reflection;

    using static FormattableString;

    /// <summary>
    /// The application single form  <br/>
    /// - renders a shape (as the GraphicsPath) that is filled with a PathGradientBrush instance and 1-px outlined <br/>
    /// - the brush may optionally use a Blend entity (referring to the 'blend' checkbox/panel) <br/>
    /// - rendering may be optionnaly processed with transparent colors over a background texture (aka the overlaying way)
    ///  (referring to the 'overlay' checkbox/panel) <br/>
    /// - a few parameters are provided with UI controls : the 2-colors of the gradient, the outline color, a limited 
    /// edition of the Blend property (3 position/factors), the gradient center point along with its x/y scaling <br/>
    /// - NOTE: the multicolors gradient isn't supported yet <br/>
    /// </summary>
    /// @internal l'implémentation du multiple-couleurs impliquera l'utilisation de la property InterpolationColors, qui
    /// est antagoniste avec celle des properties Blend et SurroundColors (donc prévoir un mode "multiple colors blending"
    /// et un mode "intensity fading")
    public partial class GradientForm : Form, IGradientChangesSource
    {
        #region initialization

        public GradientForm()
        {
            InitializeComponent();

            this.Text = "Interactive GDI+ Gradient Brushes playground";

            _paneGradientOrigin = _gradientPane.Location;

            listLinearModes.DataSource = Enum.GetValues(typeof(LinearGradientMode));
            listLinearModes.SelectedIndexChanged += (s, e) => linearGradientMode = (LinearGradientMode)(s as ListBox).SelectedItem;

            listLinearFallOff.DataSource = Enum.GetValues(typeof(LinearGradientFallOff));
            listLinearFallOff.SelectedIndexChanged += (s, e) => linearGradientFallOff = (LinearGradientFallOff)(s as ListBox).SelectedItem;
            linearGradientFallOff = LinearGradientFallOff.regular;

            listShapes.DataSource = Enum.GetValues(typeof(ShapeType));
            listShapes.SelectedIndexChanged += (s, e) => shapeType = (ShapeType)(s as ListBox).SelectedItem;

            listTextures.DataSource = Enum.GetValues(typeof(HatchStyle));
            listTextures.SelectedIndexChanged += (s, e) => textureType = (HatchStyle)(s as ListBox).SelectedItem;
            textureType = HatchStyle.LargeCheckerBoard;

            _gradientPane.MouseUp += (s, e) => onGradientMouseUp(e);
            _gradientPane.SizeChanged += (s, e) => onGradientPaneResize();
            //pane sizing (match a 100% zoom) ---
            _gradientPane.ClientSize = Size.Ceiling(_shapeMaxSize + _gradientPane.Padding.Size);
            _centerGradient = new PointF(_gradientPane.Width / 2f, _gradientPane.Height / 2f);

            initializeBlendElements(_trackBlend1, _blend1Caption, null, 1);
            initializeBlendElements(_trackBlend2, _blend2Caption, _position2, 2);
            initializeBlendElements(_trackBlend3, _blend3Caption, _position3, 3);

            _gradientPane.Paint += drawGradient;

            _zoom.ValueChanged += (s, e) => onZoom();
            _zoom.Value = 100;

            _focusScaleX.ValueChanged += (s, e) => onFocusScaleChange();
            _focusScaleY.ValueChanged += (s, e) => onFocusScaleChange();

            numLinearFocus.ValueChanged += (s, e) => onLinearFallOffChange(VirtualPropertyName._linearFallFocus);

            updateCaptions();

            bindColorProperty(editPaneShapeColor, btnPaneShapeColor, nameof(shapePaneColor), ref _shapePaneColorProperty, SystemColors.ControlDark);
            bindColorProperty(editOuterColor, btnOuterColor, nameof(outerColor), ref _outerColorProperty, SystemColors.ControlDarkDark);

            bindColorProperty(editCenterColor, btnCenterColor, nameof(centerColor), ref _centerColorProperty, Color.White);
            bindColorProperty(editGradientColor, btnGradientColor, nameof(gradientColor), ref _gradientColorProperty, Color.Blue);

            bindColorProperty(editOuterRingColor, btnOuterRing, nameof(outerRingColor), ref _outerRingColorProperty, Color.LightGray);
            bindColorProperty(editTextureColor, btnTextureColor, nameof(textureColor), ref _textureColorProperty, Color.Yellow);
            bindColorProperty(editBackColor, btnBackColor, nameof(backgroundColor), ref _backColorProperty, _gradientColor);

            chkUseOverlay.CheckedChanged += (s, e) => { var chk = s as CheckBox; useOverlay = chk.Checked; };
            chkUseBlend.CheckedChanged += (s, e) => { var chk = s as CheckBox; useBlend = chk.Checked; };
            chkOuterRing.CheckedChanged += (s, e) => { var chk = s as CheckBox; haveOuterRing = chk.Checked; };

            radioLinearGradient.CheckedChanged += (s, e) =>
            {
                var radio = s as RadioButton;
                if (radio.Checked) gradientType = GradientType.linear;
            };
            radioPathGradient.CheckedChanged += (s, e) =>
            {
                var radio = s as RadioButton;
                if (radio.Checked) gradientType = GradientType.path;
            };

            trackTransparency.ValueChanged += (s, e) =>
            {
                var slider = s as TrackBar;
                txtTransparency.Text = $"Main transparency: {slider.Value}";
                overlayAlpha = slider.Value;
            };
            trackOuterTransparency.ValueChanged += (s, e) =>
            {
                var slider = s as TrackBar;
                txtOuterTransparency.Text = $"Outer transparency: {slider.Value}";
                overlayOuterAlpha = slider.Value;
            };
            overlayAlpha = 100;
            overlayOuterAlpha = 150;

            trackLinearScale.ValueChanged += (s, e) => onLinearFallOffChange(VirtualPropertyName._linearFallScale);
            trackLinearScale.Value = 30; //0.3

            useOverlay = false;
            useBlend = true;
            gradientType = GradientType.path;
            linearGradientMode = LinearGradientMode.Horizontal;

            btnAbout.Click += (s, e) => showAboutWindow();
            btnClose.Click += (s, e) => Close();
            btnShowCode.Click += (s, e) => showSourceCode();
            btnExplain.Click += onBtnExplain;
            btnCenterPath.Click += (s, e) => { centerGradient = new PointF(_gradientPane.Width / 2f, _gradientPane.Height / 2f); };

            updateGradient();
        }

        /// <summary>
        /// link up together the controls that are involved into a blend parameterizaton part (i.e a position, a factor,
        /// a slider to adjust the factor, a numericupdown to adjust the position, a label info)
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="info"></param>
        /// <param name="position"></param>
        /// <param name="index">the 1-indexed of the blend parameter</param>
        private void initializeBlendElements(TrackBar bar, Label info, NumericUpDown position, int index)
        {
            if (position != null)
            {
                position.Value = (decimal)_blendData[0][index - 1];
                position.Tag = index;
                position.ValueChanged += (s, e) => onPositionChange(s as NumericUpDown);
            }

            info.Tag = index;

            bar.Scroll += (s, e) => onScrollBlend(s as TrackBar, info);
            bar.Tag = index;
            bar.Value = (int)Math.Round(_blendData[1][index - 1] * 100);
        }

        private void bindColorProperty(MaskedTextBox editBox, Button button, string propName, ref ColorProperty property, Color initValue)
        {
            editBox.Mask = "FFAAAAAAh";
            //cette stupide MaskedTextBox ignore la property Handled de KeyPressEventArgs si on utilise la méthode 
            //normale qui est d'utiliser l'event KeyPress ... donc on utilise KeyDown
            //editOuterColor.KeyPress += (s, e) => handleHexaOrDecimalCharInput(e);
            editBox.KeyDown += (s, e) => handleColorCharInput(e);
            editBox.TypeValidationCompleted += (s, e) => validateColorInput(s as MaskedTextBox, e);
            editBox.ValidatingType = typeof(ColorValueInput);

            //bind the property member 
            property = new ColorProperty(this, propName, editBox);
            editBox.Tag = property;
            property.SetValue(initValue);

            button.Tag = property;
            button.Click += onSelectColor;
        }

        protected override void OnLoad(EventArgs e)
        {
            _wndBehavior = new CenterChildWindowBehavior(this);

            base.OnLoad(e);
        }

        #endregion

        #region interface IGradientChangesSource

        /// <summary>
        /// event raised when (some) ui controls are updated
        /// </summary>
        public event EventHandler<GradientChangeEvent> OnGradientChange;

        TextBoxContent IGradientChangesSource.GetExplanation() => TextBoxContent.Text(updateExplanation());

        #endregion

        #region ui handlers

        private void showAboutWindow()
        {
            var wnd = new AboutBox();
            wnd.ShowDialog(this);
        }

        private void onGradientMouseUp(MouseEventArgs e)
        {
            if (_gradientType == GradientType.path)
                centerGradient = e.Location;
        }

        private void onGradientPaneResize()
        {
            _shapeBitmap?.Dispose();
            _shapeBitmap = new Bitmap(_gradientPane.ClientSize.Width, _gradientPane.ClientSize.Height);
        }

        private void onScrollBlend(TrackBar bar, Label caption)
        {
            _blendData[1][(int)bar.Tag - 1] = bar.Value / 100f;

            if (useBlend)
            {
                assignBlend();
                _gradientPane.Invalidate();
            }

            updateCaption(caption);
        }

        private void onPositionChange(NumericUpDown value)
        {
            int position = (int)value.Tag - 1; //to 0-indexed
            _blendData[0][position] = (float)value.Value;

            if (useBlend)
            {
                assignBlend();
                _gradientPane.Invalidate();
            }

            updateCaptions();
        }

        private void onZoom()
        {
            //current zoom [0.0 - 1.0]
            float scale = (float)_zoom.Value / 100f;

            //the max reserved room size for the drawing (pseudo const as long as padding doesn't change)
            Size pane_maxsize = Size.Ceiling(_shapeMaxSize) + _gradientPane.Padding.Size;

            //the shape new bounds
            SizeF shape_size = new SizeF(_shapeMaxSize.Width * scale, _shapeMaxSize.Height * scale);

            //the new pane size
            Size size = Size.Ceiling(shape_size + _gradientPane.Padding.Size); //new pane size

            //track the center according to the zoom change -----
            float dx = (float)size.Width / _gradientPane.ClientSize.Width;
            float dy = (float)size.Height / _gradientPane.ClientSize.Height;
            _centerGradient.X *= dx;
            _centerGradient.Y *= dy;

            _shapeSize = shape_size;
            _gradientPane.ClientSize = size;

            //anchor the shape to the center of the reserved room size
            _gradientPane.Location = Point.Ceiling(new PointF(
                _paneGradientOrigin.X + (1 - scale) * (pane_maxsize.Width / 2f),
                _paneGradientOrigin.Y + (1 - scale) * (pane_maxsize.Height / 2)));

            //recreate the whole stuff ellipse-path and gradient
            updateGradient();
        }

        private void onFocusScaleChange()
        {
            var scales = new PointF((float)_focusScaleX.Value, (float)_focusScaleY.Value);

            getPathBrush().FocusScales = scales;
            _gradientPane.Invalidate();

            var change = GradientChangeEvent.PropertyValue(_gradientBrush, nameof(PathGradientBrush.FocusScales), scales);
            OnGradientChange?.Invoke(this, change);
        }

        private void onLinearFallOffChange(string propname)
        {
            if (propname == VirtualPropertyName._linearFallFocus)
            {
                float value = (float)numLinearFocus.Value;
                OnGradientChange?.Invoke(this, GradientChangeEvent.PropertyValue(propname, value));
            }

            if (propname == VirtualPropertyName._linearFallScale)
            {
                float value = trackLinearScale.Value / 100f;
                OnGradientChange?.Invoke(this, GradientChangeEvent.PropertyValue(propname, value));

                txtLinearScale.Text = $"Scale: {value:f2}";
            }

            if (_linearFallOff != LinearGradientFallOff.regular)
            {
                applyGradientFallOff(getLinearBrush(), _linearFallOff);

                _gradientPane.Invalidate();
            }
        }

        /// <summary>
        /// Button click handler <br/>
        /// Open a color picker for a given property color target.
        /// The property must be available through a ColorProperty instance, which is attached to the Button Tag property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onSelectColor(object sender, EventArgs e)
        {
            var prop = (sender as Button).Tag as ColorProperty;
            //retrieve the color actual value
            Color color = prop.GetValue();
            colorDlg.Color = color;

            var result = colorDlg.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (prop.SetValue(colorDlg.Color))
                    _gradientPane.Invalidate();
            }
        }

        private void onSelectShapeType(object sender, EventArgs e)
        {
            ShapeType type = (ShapeType)listShapes.SelectedItem;
            _shapeType = type;
        }

        private void handleHexaOrDecimalCharInput(KeyPressEventArgs e) => e.Handled = !Uri.IsHexDigit(e.KeyChar);
        private void handleColorCharInput(KeyEventArgs e)
        {
            //ok to move
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) return;

            e.SuppressKeyPress = !Uri.IsHexDigit((char)e.KeyValue);
        }

        private void validateColorInput(MaskedTextBox source, TypeValidationEventArgs e)
        {
            if (e.IsValidInput)
            {
                //call the property setter to update the color which should have been attached to the editbox through
                //the general purpose Tag property ---

                var property = (ColorProperty)source.Tag;
                var color = ((ColorValueInput)e.ReturnValue).Value;

                property.SetValue(color, updateUI: false);
            }
        }

        private void onBtnExplain(object sender, EventArgs e)
        {
            if (_gradientInfoWnd != null)
            {
                //relocate if one has lost the window ...
                Native.CenterWindow(Handle, _gradientInfoWnd.Handle);

                //if behavior is unfriendly, no need to click on that button
            }

            //open a new or force-update the current window
            showExplanation(modal: false);
        }


        #endregion


        #region gradient

        private GraphicsPath createPath(RectangleF bounds)
        {
            //one says that winding mode is a bit less worst than the default mode for text rendering (I can't really 
            //agree with this assertion ... I need to use overlay method to get a good rendering)
            var fill = shapeType == ShapeType.text ? FillMode.Winding : FillMode.Alternate;
            var path = new GraphicsPath(fill);

            if (_shapeType == ShapeType.ellipse)
            {
                path.AddEllipse(bounds);
            }
            else if (_shapeType == ShapeType.rectangle)
            {
                path.AddRectangle(bounds);
            }
            else if (_shapeType == ShapeType.text)
            {
                string text = "ABCDEFGHIJKLMNOPQRSTUVWYZ";
                var fmt = new StringFormat(StringFormat.GenericDefault) 
                {
                    LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center 
                };
                path.AddString(text, new FontFamily("Arial"), (int)FontStyle.Bold, (float)_zoom.Value / 2, bounds, fmt);
            }
            else if(_shapeType == ShapeType.polygon)
            {
                path.AddPolygon(new[] {
                    new PointF(bounds.Left, bounds.Bottom), new PointF(bounds.Right, bounds.Bottom), new PointF(bounds.Left + bounds.Width/2f, bounds.Top)
                });
            }

            return path;
        }

        /// <summary>
        /// create or recreate the gradient brush according to the actual properties
        /// </summary>
        private void createGradient()
        {
            //the extent of the actual shape size
            var bounds = new RectangleF(_gradientPane.Padding.Left, _gradientPane.Padding.Top, _shapeSize.Width, _shapeSize.Height);

            _shapePath?.Dispose();
            _shapePath = createPath(bounds);

            Brush brush = null;

            if (_gradientType == GradientType.path)
            {
                var gradient = new PathGradientBrush(_shapePath)
                {
                    CenterColor = centerColor,
                    SurroundColors = new Color[] { _useOverlay ? Color.Transparent : gradientColor },
                    CenterPoint = _centerGradient,
                    FocusScales = new PointF((float)_focusScaleX.Value, (float)_focusScaleY.Value)
                };

                brush = gradient;
            }
            else if(_gradientType == GradientType.linear)
            {
                Color start = _useOverlay ? Color.Transparent : gradientColor;
                var gradient = new LinearGradientBrush(bounds, start, centerColor, linearGradientMode);
                applyGradientFallOff(gradient, linearGradientFallOff);

                brush = gradient;
            }

            _gradientBrush?.Dispose();
            _gradientBrush = brush;

            assignBlend();
        }

        private void assignBlend()
        {
            Blend blend = null;

            // Create custom blend with dynamic values from trackbars
            if (useBlend) blend = new Blend { Positions = _blendData[0], Factors = _blendData[1] };

            if (blend != null && _gradientBrush != null)
            {
                //if we are using blend w/o any valid gradient instance ... it's a bug or an useless initialization
                setBlend(blend);
            }
            else if (_gradientBrush != null)
            {
                //if we have a running gradient instance, we must invalidate the active blend
                setBlend(new Blend());
            }
            
            btnAbout.GradientBlend = blend;
        }

        #endregion

        private void updateCaption(Label caption)
        {
            int index = (int)caption.Tag -1;
            caption.Text = $"Blend {index+1}: Position [{_blendData[0][index]:f2} - {_blendData[0][index+1]:f2}] Scale {_blendData[1][index]:f2}";
        }

        private void updateCaptions()
        {
            updateCaption(_blend1Caption);
            updateCaption(_blend2Caption);
            updateCaption(_blend3Caption);
        }


        #region source code topic

        /// <summary>
        /// generate a source-oriented summary info from the current gradient setup
        /// </summary>
        /// <returns></returns>
        private string updateExplanation()
        {
            var info = new StringBuilder();

            string classtype = string.Empty;

            if (gradientType == GradientType.linear) classtype = nameof(LinearGradientBrush);
            else if (gradientType == GradientType.path) classtype = nameof(PathGradientBrush);

            string shape_color = $"0x{gradientColor.ToArgb():X08}";

            info.AppendLine($"The current gradient setup is achieved by instantiation of the class <{classtype}>.");

            if(gradientType == GradientType.linear)
            {
                info.AppendLine($@"Using the gradient direction <{nameof(LinearGradientMode)}>.{{{_linearMode}}}.");
            }
                
            if (useBlend && (gradientType == GradientType.path || linearGradientFallOff == LinearGradientFallOff.regular))
            {
                string color1 = gradientType == GradientType.path ? "outer" : "start";
                string color2 = gradientType == GradientType.path ? "center" : "end";

                info.AppendLine(
$@"
A custom <Blend> instance is provided with the <{classtype}>.{{Blend}} property.
The <Blend>.{{Positions}} and <Blend>.{{Factors}} properties have to be supplied to state the colors distribution. 
@3 blends leveraging effects are actually parameterized in this demo, knowing that the last position (1.0), is hidden and its factor/scale is decently set to the fixed value (1.0)
Reminder: The position (0.0) reflects the {color1} gradient part, while the position (1.0) reflects the {color2} gradient part.
{{Positions[i]}} and {{Factors[i]}} values are scaled in the range [0.0 - 1.0].
");
            }
            else if(gradientType == GradientType.linear && linearGradientFallOff != LinearGradientFallOff.regular)
            {
                string method = linearGradientFallOff.ToString();
                if (linearGradientFallOff == LinearGradientFallOff.bell) method = nameof(LinearGradientBrush.SetSigmaBellShape);
                else if (linearGradientFallOff == LinearGradientFallOff.triangular) method = nameof(LinearGradientBrush.SetBlendTriangularShape);

                info.AppendLine(Invariant(
$@"
Any custom/user <Blend> instance can't be provided with the <{classtype}>.{{Blend}} property, since the blend configuration has been set the pre-defined color distribution, achievable with the method <{nameof(LinearGradientBrush)}>.{{{method}}}.
This pre-defined distribution is parameterized by its {{focus:}}{(float)numLinearFocus.Value:f2} and {{scale:}}{trackLinearScale.Value/100f:f2} arguments (in the range [0.0 - 1.0]).
"));
            }
            else if(!useBlend && (gradientType == GradientType.path || linearGradientFallOff == LinearGradientFallOff.regular))
            {
                info.AppendLine(
$@"
Any custom/user <Blend> instance hasn't been provided with the <{classtype}>.{{Blend}} property, thus a default decent colors distribution is used.
");
            }

            if(gradientType == GradientType.path)
            {
                if(_focusScaleX.Value != 0 || _focusScaleY.Value != 0)
                {
                    info.AppendLine(
$@"The gradient center is adjusted with explicit focus scale values, referring to the {{{nameof(PathGradientBrush.FocusScales)}}}:({(float)_focusScaleX.Value:f2}, {(float)_focusScaleY.Value:f2}) property
");
                }
                if(Math.Abs(_gradientPane.Width /2f - _centerGradient.X) > 0.01 || Math.Abs(_gradientPane.Height/2f - _centerGradient.Y) > 0.01)
                {
                    info.AppendLine(
$@"The gradient center has been adjusted through the {{{nameof(PathGradientBrush.CenterPoint)}}} property
");
                }
            }

            if(useOverlay)
            {
                info.AppendLine(
$@"Using the {{Overlay}} option.
The {{overlay}} has nothing to do with the properties of the <Brush> classes. It's an application option that enables us to experiment several ways of draw/rendering processing.
Basically, we invariably apply a gradient <Brush> onto a shape <GraphicsPath> (or draw a primitive with a Brush).
We can do this, simply by filling with a brush based on a solid color (meaning a color(s) change implies a new gradient instance or at least a property change).
Alternatively, we can draw using (at least) two layers. 
 - The @1st or the low layer is the shape as itself, and the shape is filled with its own color. Here Color:{shape_color}.
 - The @2nd or the up layer is the gradient where the main color is {{Color.Transparent}}.
With such an approach, we could apply the same gradient on top of several shapes, each one with its own background.
The actual sample is a bit more complicated as the shape is filled with a texture <{nameof(HatchStyle)}>.{{{textureType}}}, which is paint with a variable colors and transparency.
");

                if(haveOuterRing)
                {
                    info.AppendLine(
$@"Drawing an {{""outer ring""}}.
The sample fills an additional outer ring with its own color and transparency. It has not any special didactic interest to process with that.
It might be used to casually play with GDI to try to render some user UI controls (using <GraphicsPath>) instead of fixed size images.
");
                }
            }

            return info.ToString();
        }

        /// <summary>
        /// Display OR update a <see cref="GradientInfoWnd"/> instance according to the current gradient setup
        /// </summary>
        /// <param name="modal">[true] a new instance is always raised and modal <br/>
        /// [false] actual instance (if any) is updated or a new modeless instance is created</param>
        private void showExplanation(bool modal)
        {
            GradientInfoWnd wnd = null;

            //the info block to be displayed or updated
            string content = updateExplanation();

            if (modal || _gradientInfoWnd == null)
            {
                //modal (we need a new window each time)
                //OR 1st time non-modal window (or has been closed prior)

                wnd = new GradientInfoWnd(doNotClose: !modal, this) { Text = "Current configuration brush ..." };

                if (!modal)
                {
                    wnd.FormClosed += (s, e) => _gradientInfoWnd = null;
                }
            }
            else
            {
                //current (non-modal) window (cannot be null)
                wnd = _gradientInfoWnd;
                //update content (may be useless .. since it should be up to date due to live-update ... but to be sure)
                wnd.InfoContent = TextBoxContent.Text(content);
            }

            if(modal)
            {
                wnd.ShowDialog(this);
            }
            else if (_gradientInfoWnd == null)
            {
                _gradientInfoWnd = wnd;
                wnd.Show(this);
            }
        }

        /// <summary>
        /// Show a dialog with the gradient pseudo source code (see the <see cref="SourceCodeWnd"/> form help message)
        /// </summary>
        private void showSourceCode()
        {
            var builder = new StringBuilder();

            builder.AppendLine(
@"
using System.Drawing.Drawing2D;
using System.Drawing;

/**
 * To be integrated into an initializing and a painting methods
 * (either by overriding the Control.OnPaint(), either by handling the Control.Paint event).
 * That means we expect a PaintEventArgs (referring to the hereafter <paintEvent> argument, which
 * provides with a Graphics instance.
 */");
            builder.AppendLine(Invariant(
$@"
//----------------------------------------shape path ----------------------------------------------

{(_shapeType != ShapeType.text ? "var path = new GraphicsPath();" : "var path = new GraphicsPath(FillMode.Winding);")}
var bounds = new RectangleF({_gradientPane.Padding.Left}, {_gradientPane.Padding.Top}, {_shapeSize.Width:f1}f, {_shapeSize.Height:f1}f);
"));
            if (_shapeType == ShapeType.text)
            {
                builder.AppendLine(
$@"var textfmt = new StringFormat(StringFormat.GenericDefault) 
 {{
    LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center 
 }};");
            }

            appendShapeToPath("path", "bounds", builder, shapeType);

            builder.AppendLine(
@"
//----------------------------------------gradient brush ------------------------------------------
");
            string gradient_color = $"Color.FromArgb(unchecked((int)0x{gradientColor.ToArgb():X08}))";
            string center_color = $"Color.FromArgb(unchecked((int)0x{centerColor.ToArgb():X08}))";
            string outer_color = $"Color.FromArgb(unchecked((int)0x{outerColor.ToArgb():X08}))";

            if (_gradientType == GradientType.linear)
            {
                builder.AppendLine(
$@"Color startColor = {(_useOverlay ? "Color.Transparent" : gradient_color)};
Color endColor = {center_color};

var brush = new LinearGradientBrush(bounds, startColor, endColor, LinearGradientMode.{linearGradientMode});
");
                var gradient = getLinearBrush();

                if (_linearFallOff == LinearGradientFallOff.bell)
                {
                    builder.AppendLine(Invariant(
$@"brush.SetSigmaBellShape(focus:{(float)numLinearFocus.Value:f2}f, scale:{trackLinearScale.Value / 100f:f2}f);
"));
                }
                else if (_linearFallOff == LinearGradientFallOff.triangular)
                {
                    builder.AppendLine(Invariant(
$@"brush.SetBlendTriangularShape(focus:{(float)numLinearFocus.Value:f2}f, scale:{trackLinearScale.Value / 100f:f2}f);
"));
                }
            }
            else if (_gradientType == GradientType.path)
            {
                builder.AppendLine(Invariant(
$@"var brush = new PathGradientBrush(path);

Color gradientColor = {(_useOverlay ? "Color.Transparent" : gradient_color)};
Color centerColor = {center_color};
brush.CenterColor = centerColor;
brush.SurroundColors = new Color[] {{ gradientColor }}; 
brush.CenterPoint = new PointF({_centerGradient.X:f1}f, {_centerGradient.Y:f1}f);
brush.FocusScales = new PointF({(float)_focusScaleX.Value:f2}f, {(float)_focusScaleY.Value:f2}f);
"));
            }

            if (_useBlend &&
                ((gradientType == GradientType.linear && _linearFallOff == LinearGradientFallOff.regular) ||
                (gradientType == GradientType.path)))
            {
                builder.AppendLine(Invariant(
$@"var blend = new Blend()
{{
    Positions = new float[] {{ {string.Join(", ", _blendData[0].Select(p => Invariant($"{p:f2}f")))} }},
    Factors = new float[] {{ {string.Join(", ", _blendData[1].Select(p => Invariant($"{p:f2}f")))} }}
}};
brush.Blend = blend;
"));
            }

            builder.AppendLine(
$@"//------------------------------------------------ Drawing ----------------------------------------
");
            builder.AppendLine(
$@"//create a bitmap for double buffering purpose (to be cached somewhere) 
var bitmap = new Bitmap({_gradientPane.ClientSize.Width}, {_gradientPane.ClientSize.Height});
var g = Graphics.FromImage(bitmap);

g.SmoothingMode = SmoothingMode.AntiAlias;
g.Clear(SystemColors.Control);
");
            bool outline_pen_is_defined = false;

            if (_useOverlay)
            {
                builder.AppendLine(
$@"//overlay painting ----------------------------

//the hatched brush for the background layer
var hatchColor = Color.FromArgb(unchecked((int)0x{_textureColor.ToArgb():X08}));
var backColor = Color.FromArgb(unchecked((int)0x{_backgroundColor.ToArgb():X08}));
//the shape solid brush for the overlay layer 
var overlayColor = {gradient_color};
using(var hatchBrush = new HatchBrush(HatchStyle.{textureType}, hatchColor, backColor))
using(var overlayBrush = new SolidBrush(overlayColor))
{{
    g.FillPath(hatchBrush, path);
    g.FillPath(overlayBrush, path);
}}
");
                if (haveOuterRing)
                {
                    builder.AppendLine(Invariant(
$@"//optional outer ring painting ---------------

float ringSize = {_shapeSize.Width / 10:f1}f;
var ringBounds = RectangleF.Inflate(path.GetBounds(), -ringSize, -ringSize);
var ringPath = new GraphicsPath();"));
                    appendShapeToPath("ringPath", "ringBounds", builder, shapeType);

                    builder.AppendLine(
$@"
g.SetClip(ringPath, CombineMode.Exclude);
var outerRingColor = Color.FromArgb(unchecked((int)0x{outerRingColor.ToArgb():X08}));
using(var outerRingBrush = new SolidBrush(outerRingColor))
{{
    g.FillPath(outerRingBrush, path);
}}

g.ResetClip();
");
                    outline_pen_is_defined = true;

                    builder.AppendLine(
$@"var outlinePen = new Pen({outer_color});
g.DrawPath(outlinePen, ringPath);
");
                }
            }

            builder.AppendLine(
@"//main step: applying the gradient as such
g.FillPath(brush, path);
");
            if (!outline_pen_is_defined)
            {
                outline_pen_is_defined = true;
                builder.AppendLine(
$"var outlinePen = new Pen({outer_color});");
            }

            builder.AppendLine(
$@"//minor: apply an outline
g.DrawPath(outlinePen, path);
 
//Final step
//flushing the bitmap on main graphics target
paintEvent.Graphics.DrawImage(bitmap, new Point(0, 0));
bitmap.Dispose();

outlinePen.Dispose();
g.Dispose();
");
            var wnd = new SourceCodeWnd(() => showExplanation(modal: true))
            {
                SourceCode = builder.ToString()
            };
            wnd.ShowDialog(this);

            void appendShapeToPath(string apath, string abounds, StringBuilder abuilder, ShapeType ashape)
            {
                if (ashape == ShapeType.ellipse) builder.AppendLine(
$"{apath}.AddEllipse({abounds});");
                else if (ashape == ShapeType.rectangle) builder.AppendLine(
$"{apath}.AddRectangle({abounds});");
                else if (ashape== ShapeType.polygon) builder.AppendLine(
$@"path.AddPolygon(new[] {{
    new PointF({abounds}.Left, {abounds}.Bottom), 
    new PointF({abounds}.Right, {abounds}.Bottom), 
    new PointF({abounds}.Left + {abounds}.Width/2f, {abounds}.Top)
}});");
                else if (ashape == ShapeType.text) builder.AppendLine(
$@"{apath}.AddString(""ABCDEF"", new FontFamily(""Arial""), (int)FontStyle.Bold, 20f, {abounds}, textfmt);");
            }
        }

        #endregion

        #region drawing

        private void drawGradient(object sender, PaintEventArgs e)
        {
            var g = Graphics.FromImage(_shapeBitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(_shapePaneColor);

            //when using overlay
            //- we fill the background with a texture brush
            //- we fill the shape with the color and A channel
            //- we optionally fill the outer ring
            //- and finally we apply on top the transparent gradient
            //
            //when using simple drawing
            //- we fill the shape with the colored opaque gradient

            if (useOverlay)
            {
                g.FillPath(textureBrush, _shapePath);
                g.FillPath(gradientBrushOverlay, _shapePath);

                if (haveOuterRing)
                {
                    float size = _shapeSize.Width / 10;
                    var bounds = RectangleF.Inflate(_shapePath.GetBounds(), -size, -size);

                    var inner = createPath(bounds);
                    g.SetClip(inner, CombineMode.Exclude);
                    g.FillPath(outerRingBrush, _shapePath);
                    g.ResetClip();

                    g.DrawPath(outlinePen, inner);
                }
            }

            g.FillPath(_gradientBrush, _shapePath);
            g.DrawPath(outlinePen, _shapePath);

            e.Graphics.DrawImage(_shapeBitmap, new Point(0, 0));
            g.Dispose();
        }

        /// <summary>
        /// recreate the whole gradient entity
        /// </summary>
        private void updateGradient()
        {
            createGradient();
            _gradientPane.Invalidate();
        }

        #endregion

        #region gradient properties

        private Color centerColor
        {
            get => _centerColor;
            set
            {
                if (_centerColor == value) return;

                _centerColor = value;
                btnAbout.Color2 = value;

                if (_gradientBrush != null)
                {
                    setGradientColors(gradientColor, value);
                    _gradientPane.Invalidate();

                    string property = string.Empty;
                    if (_gradientType == GradientType.path) property = nameof(PathGradientBrush.CenterColor);
                    else if (_gradientType == GradientType.linear) property = nameof(LinearGradientBrush.LinearColors);

                    var change = GradientChangeEvent.PropertyValue(_gradientBrush, property, value);
                    OnGradientChange?.Invoke(this, change);
                }
            }
        }

        private PointF centerGradient
        {
            get => _centerGradient;
            set
            {
                if (_centerGradient == value) return;

                _centerGradient = value;

                if (gradientType == GradientType.path)
                {
                    getPathBrush().CenterPoint = value;
                    _gradientPane.Invalidate();

                    var change = GradientChangeEvent.PropertyValue(_gradientBrush, nameof(PathGradientBrush.CenterPoint), value);
                    OnGradientChange?.Invoke(this, change);
                }
            }
        }

        private Color gradientColor
        {
            get
            {
                if (_useOverlay) return Color.FromArgb(overlayAlpha, _gradientColor);
                else return _gradientColor;
            }
            set
            {
                if (_gradientColor == value) return;

                _gradientColor = value;
                btnAbout.Color1 = value;

                _gradientBrushOverlay?.Dispose();
                _gradientBrushOverlay = null;

                if(_gradientBrush != null)
                {
                    setGradientColors(value, centerColor);
                    _gradientPane.Invalidate();

                    string property = string.Empty;
                    if (_gradientType == GradientType.path) property = nameof(PathGradientBrush.SurroundColors);
                    else if (_gradientType == GradientType.linear) property = nameof(LinearGradientBrush.LinearColors);

                    var change = GradientChangeEvent.PropertyValue(_gradientBrush, property, value);
                    OnGradientChange?.Invoke(this, change);
                }
            }
        }

        private bool useBlend
        {
            get => _useBlend;
            set
            {
                if (chkUseBlend.Checked != value) chkUseBlend.Checked = value;

                if (_useBlend == value) return;

                _useBlend = value;

                assignBlend();
                _gradientPane.Invalidate();

                paneBlend.Enabled = value;

                OnGradientChange?.Invoke(this, GradientChangeEvent.Property<Blend>(_gradientBrush, VirtualPropertyName._blend));
            }
        }

        private LinearGradientMode linearGradientMode
        {
            get => _linearMode;
            set
            {
                listLinearModes.SelectedItem = value;

                if (_linearMode == value) return;

                var change = GradientChangeEvent.PropertyValue(VirtualPropertyName._linearMode, value);
                OnGradientChange?.Invoke(this, change);

                _linearMode = value;
                updateGradient();
            }
        }

        private LinearGradientFallOff linearGradientFallOff
        {
            get => _linearFallOff;
            set
            {
                //enforce UI consistency -----------------

                listLinearFallOff.SelectedItem = value;

                if (value == LinearGradientFallOff.regular)
                {
                    //one can apply a custom blend again
                    chkUseBlend.Enabled = true;
                    numLinearFocus.Enabled = false;
                    trackLinearScale.Enabled = false;
                }
                else
                {
                    //specifying a special distribution is conflicting with a user custom blend
                    chkUseBlend.Enabled = false;
                    useBlend = false;
                    numLinearFocus.Enabled = true;
                    trackLinearScale.Enabled = true;
                }

                if (_linearFallOff == value) return;

                _linearFallOff = value;

                var change = GradientChangeEvent.PropertyValue(VirtualPropertyName._linearFallOffDistrib, value);
                OnGradientChange?.Invoke(this, change);

                if(_linearFallOff == LinearGradientFallOff.regular)
                {
                    //create or recreate the linear gradient
                    updateGradient();
                }
                else
                {
                    applyGradientFallOff(getLinearBrush(), value);
                    _gradientPane.Invalidate();
                }
            }
        }

        private GradientType gradientType
        {
            get => _gradientType;
            set
            {
                //update UI if needed, at least enforce consistency --------

                if(value == GradientType.linear)
                {
                    _focusScaleX.Enabled = false;
                    _focusScaleY.Enabled = false;
                    radioPathGradient.Checked = false;

                    radioLinearGradient.Checked = true;
                    listLinearModes.Enabled = true;
                    listLinearFallOff.Enabled = true;
                    txtColor2.Text = "End color";
                    txtColor1.Text = "Start color";

                    //enforce ui consistency (re-apply)
                    linearGradientFallOff = _linearFallOff;
                }
                else if(value == GradientType.path)
                {

                    radioLinearGradient.Checked = false;
                    listLinearModes.Enabled = false;
                    listLinearFallOff.Enabled = false;
                    trackLinearScale.Enabled = false;
                    numLinearFocus.Enabled = false;

                    _focusScaleX.Enabled = true;
                    _focusScaleY.Enabled = true;
                    radioPathGradient.Checked = true;
                    chkUseBlend.Enabled = true;
                    txtColor2.Text = "Center color";
                    txtColor1.Text = "Outer color";
                }

                if (_gradientType == value) return;

                _gradientType = value;
                updateGradient();

                OnGradientChange?.Invoke(this, GradientChangeEvent.OfType(_gradientBrush));
            }
        }

        private void setBlend(Blend blend)
        {
            if (_gradientType == GradientType.linear)
            {
                //if an explicit blend distribution has been set up, we mustn't overwrite it by another blend entity
                if(_linearFallOff == LinearGradientFallOff.regular)
                    getLinearBrush().Blend = blend;
            }
            else if (_gradientType == GradientType.path) 
                getPathBrush().Blend = blend;
        }

        private void setGradientColors(Color c1, Color c2)
        {
            if (_gradientType == GradientType.linear)
            {
                getLinearBrush().LinearColors = new[] { c1, c2 };
            }
            else if (_gradientType == GradientType.path)
            {
                getPathBrush().CenterColor = c2;

                //when using overlay, color is transparent, thus no need to update it
                getPathBrush().SurroundColors = new Color[] { _useOverlay ? Color.Transparent : c1 };
            }
        }

        private void applyGradientFallOff(LinearGradientBrush gradient, LinearGradientFallOff falloff)
        {
            float scale = trackLinearScale.Value / 100f;
            float focus = (float)numLinearFocus.Value;

            if (falloff == LinearGradientFallOff.bell) gradient.SetSigmaBellShape(focus, scale);
            else if (falloff == LinearGradientFallOff.triangular) gradient.SetBlendTriangularShape(focus, scale);
        }

        #endregion

        #region form implementation

        protected override void WndProc(ref Message m)
        {
            _wndBehavior?.HandleWindowProc(ref m);
            base.WndProc(ref m);
        }

        #endregion


        #region other drawing properties

        private Pen outlinePen
        {
            get
            {
                if (_outlinePen == null) _outlinePen = new Pen(_outerColor);
                return _outlinePen;
            }
        }

        private Color shapePaneColor
        {
            get => _shapePaneColor;
            set
            {
                if (_shapePaneColor == value) return;

                _shapePaneColor = value;
                _gradientPane.Invalidate();
            }
        }

        private Color outerColor
        {
            get => _outerColor;
            set
            {
                if (_outerColor == value) return;

                _outerColor = value;
                _outlinePen = null;
                //gradient doesn't need to be updated as outlining is just part of drawing (but not gradient related to)
                _gradientPane.Invalidate();

                btnAbout.BorderColor = value;
            }
        }

        private Color outerRingColor
        {
            get => Color.FromArgb(overlayOuterAlpha, _outerRingColor);

            set
            {
                if (_outerRingColor == value) return;

                _outerRingColor = value;

                _outerBrush?.Dispose();
                _outerBrush = null;

                if (!_useOverlay) return;

                _gradientPane.Invalidate();
            }
        }

        #endregion

        #region overlay drawing

        private bool useOverlay
        {
            get => _useOverlay;
            set
            {
                if (chkUseOverlay.Checked != value) chkUseOverlay.Checked = value;

                paneOverlay.Enabled = value;

                if (_useOverlay == value) return;

                _useOverlay = value;

                updateGradient();

                OnGradientChange?.Invoke(this, GradientChangeEvent.PropertyValue(VirtualPropertyName._useOverlay, value));
            }
        }

        private bool haveOuterRing
        {
            get => _haveOuterRing;
            set
            {
                if (chkOuterRing.Checked != value) chkOuterRing.Checked = value;

                _haveOuterRing = value;

                _gradientPane.Invalidate();

                OnGradientChange?.Invoke(this, GradientChangeEvent.PropertyValue(VirtualPropertyName._haveOuterRing, value));
            }
        }

        private Brush gradientBrushOverlay
        {
            get
            {
                if (_gradientBrushOverlay == null) _gradientBrushOverlay = new SolidBrush(gradientColor);
                return _gradientBrushOverlay;
            }
        }

        private Brush outerRingBrush
        {
            get
            {
                if (_outerBrush == null) _outerBrush = new SolidBrush(outerRingColor);
                return _outerBrush;
            }
        }

        private Color textureColor
        {
            get => _textureColor;
            set
            {
                if (_textureColor == value) return;

                _textureColor = value;

                _textureBrush?.Dispose();
                _textureBrush = null;

                if (_useOverlay) _gradientPane.Invalidate();
            }
        }

        private Color backgroundColor
        {
            get => _backgroundColor;
            set
            {
                if (_backgroundColor == value) return;

                _backgroundColor = value;

                _textureBrush?.Dispose();
                _textureBrush = null;

                if (_useOverlay) _gradientPane.Invalidate();
            }
        }

        /**
         * background brush when using overlay drawing
         */
        private Brush textureBrush
        {
            get
            {
                if (_textureBrush == null) _textureBrush = new HatchBrush(textureType, _textureColor, _backgroundColor);
                return _textureBrush;
            }
        }

        private HatchStyle textureType
        {
            get => _textureType;
            set
            {
                listTextures.SelectedItem = value;

                if (_textureType == value) return;

                _textureType = value;

                OnGradientChange?.Invoke(this, GradientChangeEvent.PropertyValue(nameof(textureType), value));

                _textureBrush?.Dispose();
                _textureBrush = null;

                if (_useOverlay) _gradientPane.Invalidate();
            }
        }

        private int overlayAlpha
        {
            get => _overlayAlpha;
            set
            {
                trackTransparency.Value = value;

                if (_overlayAlpha == value) return;
                _overlayAlpha = value;

                _gradientBrushOverlay?.Dispose();
                _gradientBrushOverlay = null;

                if (_useOverlay) _gradientPane.Invalidate();

                var change = GradientChangeEvent.PropertyValue(VirtualPropertyName._overlayChannelA, value);
                OnGradientChange?.Invoke(this, change);
            }
        }

        private int overlayOuterAlpha
        {
            get => _overlayOuterAlpha;
            set
            {
                trackOuterTransparency.Value = value;

                if (_overlayOuterAlpha == value) return;
                _overlayOuterAlpha = value;

                _outerBrush?.Dispose();
                _outerBrush = null;

                if (_useOverlay) _gradientPane.Invalidate();

                var change = GradientChangeEvent.PropertyValue(VirtualPropertyName._overlayRingChannelA, value);
                OnGradientChange?.Invoke(this, change);
            }
        }

        #endregion

        private ShapeType shapeType
        {
            get => _shapeType;
            set
            {
                listShapes.SelectedItem = value;

                if (_shapeType == value) return;

                _shapeType = value;

                //when using PathGradient, the rendering w/o overlay is often full of defects
                //(some parts of letters are not filled-in)
                if (value == ShapeType.text && _gradientType == GradientType.path && !_useOverlay)
                    useOverlay = true; //update not needed (already just done)
                else
                    updateGradient();
            }
        }


        private PathGradientBrush getPathBrush()
        {
            if (_gradientType != GradientType.path)
                throw new InvalidCastException($"Invalid attempt to get a PathGradientBrush from {_gradientType}");

            if(_gradientBrush == null)
                throw new NullReferenceException($"Invalid attempt to get a PathGradientBrush from a NULL brush");

            return (PathGradientBrush)_gradientBrush;
        }

        private LinearGradientBrush getLinearBrush()
        {
            if (_gradientType != GradientType.linear)
                throw new InvalidCastException($"Invalid attempt to get a LinearGradientBrush from {gradientType}");

            if(_gradientBrush == null)
                throw new NullReferenceException($"Invalid attempt to get a LinearGradientBrush from a NULL brush");

            return (LinearGradientBrush)_gradientBrush;
        }


        private ShapeType _shapeType;

        /** the max reserved area for the shape drawing */
        private static readonly SizeF _shapeMaxSize = new SizeF(300, 300);

        /** the actual shape size (according to the zoom value) */
        private SizeF _shapeSize = _shapeMaxSize;

        /** the initial location of the drawing pane, as the base origin for the shape location */
        private readonly Point _paneGradientOrigin;

        /** double buffering purpose */
        private Bitmap _shapeBitmap;

        private GradientType _gradientType;
        private Brush _gradientBrush;

        private LinearGradientFallOff _linearFallOff;
        private LinearGradientMode _linearMode;

        /** to be persisted beyound the gradient instance due to zoom tracking needs (PathGradientBrush only) */
        private PointF _centerGradient;

        private GraphicsPath _shapePath;

        private bool _useOverlay;
        private bool _haveOuterRing;

        private bool _useBlend;

        /** 
         * Blend properties: [0,] positions, [1,] factors 
         * MSDN is not very verbose on the Blend.Factors and Positions properties ...
         * When blend is applied on an ellipse, positon:0f is the outer diameter and position:1f is the center
         * factor:0 means no center color, factor:1 means only center color
         */
        private readonly float[][] _blendData = { new [] { 0f, 0.3f, 0.6f, 1f }, new [] { 0.2f, 0.6f, 0.8f, 1f } };

        /** used when painting with overlay */
        private SolidBrush _gradientBrushOverlay;
        private Color _gradientColor;
        private readonly ColorProperty _gradientColorProperty;

        /** shape panel */
        private Color _shapePaneColor;
        private readonly ColorProperty _shapePaneColorProperty;

        /** shape solid outline */
        private Color _outerColor;
        private readonly ColorProperty _outerColorProperty;
        private Pen _outlinePen;

        /** gradient center color */
        private Color _centerColor;
        private readonly ColorProperty _centerColorProperty;

        /** attributes used when painting with overlay */
        private Brush _outerBrush;
        private Color _outerRingColor;
        private readonly ColorProperty _outerRingColorProperty;
        private Brush _textureBrush;
        private Color _textureColor;
        private readonly ColorProperty _textureColorProperty;
        private Color _backgroundColor;
        private readonly ColorProperty _backColorProperty;
        private int _overlayAlpha;
        private int _overlayOuterAlpha = 255;
        private HatchStyle _textureType;

        private CenterChildWindowBehavior _wndBehavior;

        private GradientInfoWnd _gradientInfoWnd;

        enum ShapeType
        {
            ellipse,
            rectangle,
            text,
            polygon
        }
        enum GradientType
        {
            path,
            linear
        }
        /// <summary>
        /// The linear gradient distribution types
        /// </summary>
        enum LinearGradientFallOff
        {
            regular,
            bell,
            triangular
        }

        /// <summary>
        /// A few properties are considered as overkill because they don't need special behavior, nevertheless it's
        /// useful to uniquely name them ... somewhere <br/>
        /// We more or less don't care of the exact naming, we just need to refer to an unique string
        /// </summary>
        static class VirtualPropertyName
        {
            public static readonly string _linearFallFocus = $"{nameof(LinearGradientFallOff)}.focus";
            public static readonly string _linearFallScale = $"{nameof(LinearGradientFallOff)}.scale";
            public static readonly string _linearFallOffDistrib = nameof(LinearGradientFallOff);
            public static readonly string _linearMode = nameof(LinearGradientMode);

            /// <summary>
            /// LinearGradienBrush or PathGradientBrush have got both a Blend property but unrelated to any common
            /// ancestor or interface
            /// </summary>
            public static readonly string _blend = "blend.isused";

            public static readonly string _useOverlay = "overlay.isused";
            public static readonly string _overlayChannelA = "overlay.Achannel";
            public static readonly string _overlayRingChannelA = "overlay.ringAChannel";
            public static readonly string _haveOuterRing = "overlay.haveOuterRing";
        }

        /// <summary>
        /// Helper mediator between a color property member and the UI
        /// </summary>
        class ColorProperty
        {
            /// <summary>
            /// </summary>
            /// <param name="target">the property target instance</param>
            /// <param name="property">the color property name</param>
            /// <param name="ui">the editbox where the color may be edited</param>
            public ColorProperty(GradientForm target, string property, MaskedTextBox ui)
            {
                _owner = target;
                _ui = ui;
                _get = PropertyAccessor<GradientForm>.Getter<Color>(property);
                _set = PropertyAccessor<GradientForm>.Setter<Color>(property);
            }

            public Color GetValue() => get();
            
            public bool SetValue(Color color, bool updateUI = true)
            {
                //actually update the property value
                Color old = get();
                set(color);

                if (get() == old) return false;

                if (updateUI)
                {
                    //set full solid color (must be kept consistent with the mask format)
                    string value = $"FF{(color.ToArgb() & 0x00ffffff):X06}h";
                    _ui.Text = value;
                }

                return true;
            }

            private Color get() => _get(_owner);
            private void set(Color c) => _set(_owner, c);

            private readonly Func<GradientForm, Color> _get;
            private readonly Action<GradientForm, Color> _set;

            private readonly GradientForm _owner;
            private readonly MaskedTextBox _ui;
        }
    }
}

using System.Drawing;
using System.Linq;
using System;
using Unknown6656.Common;
using System.Collections.Generic;
//using System.Windows.Forms;
using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Imaging;
using System.Globalization;

namespace Unknown6656.Controls.Console
{
    using Console = System.Console;


    public abstract class AutosizableControl
        : Control
    {
        private bool _Autosize = true;


        public bool Autosize
        {
            get => _Autosize;
            set
            {
                if (!Equals(_Autosize, value))
                {
                    _Autosize = value;

                    AutosizeChanged?.Invoke(this, value);
                }
            }
        }

        public event ControlEventHandler<bool>? AutosizeChanged;


        protected AutosizableControl(ControlHost host)
            : base(host)
        {
        }
    }

    public class Label
        : AutosizableControl
    {
        protected override bool UseDefaultTextRenderer { get; } = true;


        public Label(ControlHost host)
            : base(host)
        {
            BorderStyle = BorderStyle.None;
            FocusBehaviour = FocusBehaviour.NonFocusable;
            BorderStyleChanged += LabelLayoutChanged;
            AutosizeChanged += LabelLayoutChanged;
            TextChanged += LabelLayoutChanged;
        }

        private void LabelLayoutChanged<T>(Control sender, T _)
        {
            if (Autosize)
            {
                int b = BorderStyle == BorderStyle.None ? 0 : 2;

                Size = new Size(Math.Max(2, (Text?.Trim()?.Length ?? 0) + b), 1 + b);
            }
        }

        protected override void InternalRender(RenderInformation render_information)
        {
        }
    }

    public class Button
        : Label
    {
        public event ControlEventHandler? Clicked;


        public Button(ControlHost host)
            : base(host)
        {
            FocusBehaviour = FocusBehaviour.Focusable;
            BorderStyle = BorderStyle.Rounded;
            KeyPress += Button_KeyPress;
        }

        private void Button_KeyPress(Control sender, ConsoleKeyInfo key, ref bool handled)
        {
            if (Host.KeyMapping.GetHandledAction(key) == KeyAction.EnterFocusedElement)
            {
                handled = true;

                Clicked?.Invoke(this);
            }
        }
    }

    public class TextBox
        : AutosizableControl
    {
        protected override bool UseDefaultTextRenderer { get; } = false;

        private bool _cursorback = false;
        private Range _Selection = ^0..;


        public Range Selection
        {
            get => _Selection;
            set
            {
                if (!Equals(_Selection, value))
                {
                    _Selection = value;

                    SelectionChanged?.Invoke(this, value);
                }
            }
        }

        public int SelectionLength
        {
            get => Selection.GetOffsetAndLength(TextLength).Length;
            set => Selection = new Range(Selection.Start, Selection.Start.GetOffset(TextLength) + value);
        }

        public int CursorPosition
        {
            get => Selection.Start.GetOffset(TextLength);
            set => Selection = new Range(value, value);
        }


        public event EventHandler<Range>? SelectionChanged;


        public TextBox(ControlHost host)
            : base(host)
        {
            BorderStyle = BorderStyle.TextBox;
            FocusBehaviour = FocusBehaviour.Focusable;
            FocusedStyle = FocusedStyle.RenderAsNormal;
            SelectionChanged += (_, _) => RequestRender();
            BorderStyleChanged += TextBoxLayoutChanged;
            AutosizeChanged += TextBoxLayoutChanged;
            TextChanged += TextBox_TextChanged;
            KeyPress += TextBox_KeyPress;
            host.BlinkEvent += (_, _) => RequestRender();
        }

        private void TextBox_KeyPress(Control sender, ConsoleKeyInfo key, ref bool handled)
        {
            switch (Host.KeyMapping.GetHandledAction(key))
            {
                case KeyAction.ScrollLeft or KeyAction.CursorLeft:
                    CursorPosition = Math.Max(CursorPosition - 1, 0);
                    handled = true;

                    break;
                case KeyAction.CursorRight or KeyAction.ScrollRight:
                    CursorPosition = Math.Min(CursorPosition + 1, TextLength);
                    handled = true;

                    break;
                case KeyAction.SelectLeft:
                    if (_cursorback)
                        Selection = Math.Max(CursorPosition - 1, 0)..(SelectionLength + CursorPosition);
                    else if (SelectionLength > 0)
                        --SelectionLength;
                    else
                    {
                        Selection = Math.Max(CursorPosition - 1, 0)..CursorPosition;
                        _cursorback = true;
                    }

                    handled = true;

                    break;
                case KeyAction.SelectRight:
                    SelectionLength = Math.Min(SelectionLength + 1, TextLength - CursorPosition);
                    _cursorback = true;
                    handled = true;

                    break;
            }

            if (SelectionLength == 0)
                _cursorback = false;
        }

        private void TextBox_TextChanged(Control sender, string? new_text)
        {
            Selection = ^0..;
            TextBoxLayoutChanged(sender, new_text);
        }

        private void TextBoxLayoutChanged<T>(Control sender, T _)
        {
            if (Autosize)
            {
                int b = BorderStyle == BorderStyle.None ? 1 : 3;

                Size = new Size(Math.Max(3, (Text?.Length ?? 0) + b), b);
            }
        }

        protected override void InternalRender(RenderInformation render_information)
        {
            int x = render_information.ControlRenderArea.X;
            int y = render_information.ControlRenderArea.Y;
            int b = BorderStyle == BorderStyle.None ? 0 : 1;
            int s = CursorPosition;
            int e = s + SelectionLength;
            string txt = (render_information.SanitizedString ?? "") + ' ';

            Console.SetCursorPosition(x + b, y + b);

            if (!IsFocused)
            {
                Console.Write(txt[..s]);
                ConsoleExtensions.WriteUnderlined(txt[s..e]);
                Console.Write(txt[e..]);
            }
            else if (s == e)
            {
                Console.Write(txt[..s]);

                if (render_information.BlinkState)
                    Console.Write("\x1b[7m");

                Console.Write(txt[s]);
                Console.Write("\x1b[27m");
                Console.Write(txt[(s + 1)..]);
            }
            else
            {
                Console.Write(txt[..s]);

                if (!_cursorback || render_information.BlinkState)
                    Console.Write("\x1b[7m");

                Console.Write(txt[s]);

                if (s < e)
                {
                    Console.Write("\x1b[7m");
                    Console.Write(txt[(s + 1)..e]);
                }

                if (!_cursorback && render_information.BlinkState)
                {
                    Console.Write("\x1b[7m");
                    Console.Write(txt[e]);
                }

                if (e < txt.Length)
                {
                    Console.Write("\x1b[27m");
                    Console.Write(txt[(e + 1)..]);
                }
            }

            // ConsoleExtensions.RGBForegroundColor = render_information.Foreground;
            // ConsoleExtensions.RGBBackgroundColor = render_information.Background;
        }
    }

    public class CheckBox
        : AutosizableControl
    {
        private bool _ischecked = default;


        protected override bool UseDefaultTextRenderer { get; } = false;

        public bool IsChecked
        {
            get => _ischecked;
            set
            {
                if (!Equals(_ischecked, value))
                {
                    _ischecked = value;

                    CheckedChanged?.Invoke(this, value);
                }
            }
        }


        public event ControlEventHandler<bool>? CheckedChanged;


        public CheckBox(ControlHost host)
            : base(host)
        {
            MinimumSize = new Size(4, 1);
            FocusedStyle = FocusedStyle.ReverseColors;
            BorderStyle = BorderStyle.None;
            BorderStyleChanged += CheckBoxLayoutChanged;
            AutosizeChanged += CheckBoxLayoutChanged;
            TextChanged += CheckBoxLayoutChanged;
            KeyPress += CheckBox_KeyPress;
            CheckedChanged += (_, _) => RequestRender();
        }

        private void CheckBoxLayoutChanged<T>(Control sender, T _)
        {
            if (Autosize)
            {
                int b = BorderStyle == BorderStyle.None ? 1 : 3;

                Size = new Size((Text?.Length ?? 0) + b + 4, b);
            }
        }

        private void CheckBox_KeyPress(Control sender, ConsoleKeyInfo key, ref bool handled)
        {
            if (Host.KeyMapping.GetHandledAction(key) == KeyAction.EnterFocusedElement)
            {
                handled = true;
                IsChecked ^= true;
            }
        }
        
        protected override void InternalRender(RenderInformation render_information)
        {
            int x = render_information.ControlRenderArea.X;
            int y = render_information.ControlRenderArea.Y;
            int b = BorderStyle == BorderStyle.None ? 0 : 1;

            Console.SetCursorPosition(x + b, y + b);
            Console.Write($"[{(IsChecked ? 'X' : ' ')}] {render_information.SanitizedString}");
        }
    }

    public class OptionBox
        : AutosizableControl
    {
        private int _selectedoptionindex = -1;
        private string[]? _options = null;


        protected override bool UseDefaultTextRenderer { get; } = true;

        public string[]? Options
        {
            get => _options;
            set
            {
                if (!Equals(_options, value))
                {
                    _options = value;

                    OptionsChanged?.Invoke(this, value);

                    if (SelectedOptionIndex >= (value?.Length ?? 0))
                        SelectedOptionIndex = value is null ? -1 : 0;
                }
            }
        }

        public int OptionsCount => _options?.Length ?? 0;

        public int SelectedOptionIndex
        {
            get => _selectedoptionindex;
            set
            {
                if (!Equals(_selectedoptionindex, value))
                {
                    int len = OptionsCount;

                    if (value >= len || (value < 0 && len > 0))
                        throw new ArgumentOutOfRangeException(nameof(value));

                    _selectedoptionindex = value;

                    SelectedOptionIndexChanged?.Invoke(this, value);

                    SelectedOption = Options?[SelectedOptionIndex];
                }
            }
        }

        public string? SelectedOption
        {
            get => SelectedOptionIndex < 0 ? null : Options?[SelectedOptionIndex];
            set
            {
                if (Options is null && value is null)
                    return;
                else if (Options?.Contains(value) ?? false)
                    SelectedOptionIndex = Options.WithIndex().FirstOrDefault(t => t.Item == value).Index;
                else
                    throw new KeyNotFoundException($"The option '{value}' could not be found among the selectable options.");
            }
        }


        public event ControlEventHandler<string?>? SelectedOptionChanged;
        public event ControlEventHandler<int>? SelectedOptionIndexChanged;
        public event ControlEventHandler<string[]?>? OptionsChanged;


        public OptionBox(ControlHost host)
            : base(host)
        {
            FocusedStyle = FocusedStyle.ReverseColors;
            BorderStyle = BorderStyle.None;
            BorderStyleChanged += OptionBoxLayoutChanged;
            AutosizeChanged += OptionBoxLayoutChanged;
            TextChanged += OptionBoxLayoutChanged;
            OptionsChanged += OptionBoxLayoutChanged;
            SelectedOptionIndexChanged += (_, _) => RequestRender();
            KeyPress += OptionBox_KeyPress;
        }

        private void OptionBoxLayoutChanged<T>(Control sender, T _)
        {
            if (Autosize)
            {
                int b = BorderStyle == BorderStyle.None ? 1 : 3;

                Size = new Size((Options?.Select(o => o.Length)?.Max() ?? 0) + b + 4, b + (string.IsNullOrWhiteSpace(Text) ? 0 : 2));
            }
        }

        private void OptionBox_KeyPress(Control sender, ConsoleKeyInfo key, ref bool handled)
        {
            switch (Host.KeyMapping.GetHandledAction(key))
            {
                case KeyAction.CursorLeft or KeyAction.ScrollLeft:
                    if (SelectedOptionIndex > 0)
                        --SelectedOptionIndex;

                    handled = true;

                    return;
                case KeyAction.CursorRight or KeyAction.ScrollRight:
                    if (SelectedOptionIndex < OptionsCount - 1)
                        ++SelectedOptionIndex;

                    handled = true;

                    return;
            }
        }

        protected override void InternalRender(RenderInformation render_information)
        {
            int b = BorderStyle == BorderStyle.None ? 0 : 1;
            int idx = SelectedOptionIndex;

            Console.SetCursorPosition(render_information.ContentRenderArea.X, render_information.ContentRenderArea.Y);
            Console.Write($"{(idx <= 0 ? ' ' : '◄')} {(SelectedOption ?? "").PadRight(Width - (2 * b) - 4)} {(idx < OptionsCount - 1 ? '►' : ' ')}");
        }
    }

    public class StackPanel
        : ContainerControl
    {
        private Orientation _Orientation = Orientation.Vertical;


        public bool Autosize { set; get; } = true;

        public Orientation Orientation
        {
            get => _Orientation;
            set
            {
                if (!Equals(_Orientation, value))
                {
                    _Orientation = value;

                    OrientationChanged?.Invoke(this, value);
                    UpdateSize();
                }
            }
        }


        public event EventHandler<Orientation>? OrientationChanged;


        public StackPanel(ControlHost host)
            : base(host)
        {
            BorderStyle = BorderStyle.None;
            BorderStyleChanged += (_, _) => UpdateSize();
            ChildCollectionChanged += (_, _) => UpdateSize();
            ChildAbsolutePositionChanged += (_, _) => UpdateSize();
            ChildSizeChanged += (_, _) => UpdateSize();
        }

        private void UpdateSize()
        {
            if (Autosize)
            {
                (int w, int h) = BorderStyle == BorderStyle.None ? (0, 0) : (2, 2);

                foreach (Control child in Children)
                {
                    (w, h, child.Position) = Orientation switch
                    {
                        Orientation.Vertical => (Math.Max(w, child.Width), h + child.Height, new Point(0, h)),
                        Orientation.Horizontal => (w + child.Width, Math.Max(h, child.Height), new Point(w, 0)),
                        _ => throw new InvalidOperationException("The stack panel's orientation property has an invalid value.")
                    };
                }

                Size = new Size(w, h);
            }
        }
    }

    public class ProgressBar
        : Control
    {
        private bool _displaypercentage = false;
        private Scalar _value = Scalar.Zero;


        public Scalar Value
        {
            get => _value;
            set
            {
                if (!Equals(_value, value))
                {
                    _value = value.Clamp();

                    ValueChanged?.Invoke(this, value);
                }
            }
        }

        public bool DisplayPercentage
        {
            get => _displaypercentage;
            set
            {
                if (!Equals(_displaypercentage, value))
                {
                    _displaypercentage = value;

                    DisplayPercentageChanged?.Invoke(this, value);
                }
            }
        }

        protected override bool UseDefaultTextRenderer { get; } = true;


        public event ControlEventHandler<bool>? DisplayPercentageChanged;
        public event ControlEventHandler<Scalar>? ValueChanged;


        public ProgressBar(ControlHost host)
            : base(host)
        {
            BorderStyle = BorderStyle.Rounded;
            FocusBehaviour = FocusBehaviour.NonFocusable;
            Size = new Size(12, 3);
            ValueChanged += (_, _) => RequestRender();
            DisplayPercentageChanged += (_, _) => RequestRender();
        }

        protected override void InternalRender(RenderInformation render_information)
        {
            Rectangle area = render_information.ContentRenderArea;
            int bar_w = (int)(Value * area.Width);
            string line = new string('█', bar_w) + new string('░', area.Width - bar_w);

            for (int i = 0; i < area.Height; ++i)
                ConsoleExtensions.Write(line, (area.X, area.Y + i));

            if (DisplayPercentage && area.Width > 3)
            {
                Scalar v = Value * 100;
                string s = area.Width switch
                {
                    4 => $"{v,3:F0}%",
                    5 => $"{v,3:F0} %",
                    6 => $"{v,5:F1}%",
                    int i when i % 2 == 1 => $"{v,5:F1} %",
                    int i when i % 2 == 0 => $"{v,6:F2} %",
                };
                int x = (area.Width - s.Length) / 2;

                Console.SetCursorPosition(area.X + x, area.Y + (area.Height / 2));

                for (int i = 0; i < s.Length; ++i)
                {
                    char c = s[i];

                    if (i <= bar_w - x)
                        ConsoleExtensions.WriteInverted(c.ToString());
                    else if (c == ' ')
                        Console.Write('░');
                    else
                        Console.Write(c);
                }
            }
        }
    }

    public sealed class ColorPicker
        : Control
    {
        private static readonly BitmapChannel[] _channels = { BitmapChannel.R, BitmapChannel.G, BitmapChannel.B };
        private RGBAColor _PickedColor = RGBAColor.Red;
        private int _selectedchannelindex = 0;
        private int _selectedhexindex = 0;


        protected override bool UseDefaultTextRenderer { get; } = true;

        public RGBAColor PickedColor
        {
            get => _PickedColor;
            set
            {
                if (!Equals(_PickedColor, value))
                {
                    _PickedColor = value;

                    PickedColorChanged?.Invoke(this, value);
                }
            }
        }

        private int SelectedChannelIndex
        {
            get => _selectedchannelindex;
            set
            {
                if (!Equals(_selectedchannelindex, value))
                {
                    _selectedchannelindex = value;

                    SelectedChannelIndexChanged?.Invoke(this, value);
                }
            }
        }

        private int SelectedHexIndex
        {
            get => _selectedhexindex;
            set
            {
                if (!Equals(_selectedhexindex, value))
                {
                    _selectedhexindex = value;

                    SelectedHexIndexChanged?.Invoke(this, value);
                }
            }
        }


        private event ControlEventHandler<int>? SelectedChannelIndexChanged;
        private event ControlEventHandler<int>? SelectedHexIndexChanged;
        public event ControlEventHandler<RGBAColor>? PickedColorChanged;


        public ColorPicker(ControlHost host)
            : base(host)
        {
            Size = new Size(18, 6);
            MaximumSize = Size;
            MinimumSize = Size;
            CanChangeText = false;
            BorderStyle = BorderStyle.Rounded;
            FocusedStyle = FocusedStyle.RenderAsNormal;
            PickedColorChanged += (_, _) => RequestRender();
            SelectedHexIndexChanged += (_, _) => RequestRender();
            SelectedChannelIndexChanged += (_, _) => RequestRender();
            KeyPress += ColorPicker_KeyPress;
        }

        private void ColorPicker_KeyPress(Control sender, ConsoleKeyInfo key, ref bool handled)
        {
            RGBAColor col = PickedColor;

            switch (Host.KeyMapping.GetHandledAction(key))
            {
                case KeyAction.CursorUp or KeyAction.ScrollUp:
                    SelectedChannelIndex = (SelectedChannelIndex + _channels.Length) % (_channels.Length + 1);
                    handled = true;

                    break;
                case KeyAction.CursorDown or KeyAction.ScrollDown:
                    SelectedChannelIndex = (SelectedChannelIndex + 1) % (_channels.Length + 1);
                    handled = true;

                    break;
                case KeyAction.CursorRight or KeyAction.ScrollRight when SelectedChannelIndex == _channels.Length:
                    SelectedHexIndex = Math.Min(5, SelectedHexIndex + 1);
                    handled = true;

                    break;
                case KeyAction.CursorLeft or KeyAction.ScrollLeft when SelectedChannelIndex == _channels.Length:
                    SelectedHexIndex = Math.Max(0, SelectedHexIndex - 1);
                    handled = true;

                    break;
                case KeyAction.CursorLeft or KeyAction.ScrollLeft:
                    {
                        if (col[_channels[SelectedChannelIndex]] is byte b && b > 0)
                            col[_channels[SelectedChannelIndex]] = (byte)(b - 1);

                        handled = true;
                    }
                    break;
                case KeyAction.CursorRight or KeyAction.ScrollRight:
                    {
                        if (col[_channels[SelectedChannelIndex]] is byte b && b < 255)
                            col[_channels[SelectedChannelIndex]] = (byte)(b + 1);

                        handled = true;
                    }
                    break;
                default:
                    char c = char.ToLower(key.KeyChar);

                    if (key.Key == ConsoleKey.Delete || key.Key == ConsoleKey.Backspace)
                        c = '0';

                    if ("0123456789abcdef".Contains(c))
                    {
                        uint nibble = byte.Parse(c.ToString(), NumberStyles.HexNumber);
                        int idx = SelectedHexIndex;
                        int bit = 20 - (4 * idx);

                        col.ARGBu = (col.ARGBu & ~(0xfu << bit)) | (nibble << bit) | 0xff000000u;
                        SelectedHexIndex = key.Key switch {
                            ConsoleKey.Delete => idx,
                            ConsoleKey.Backspace => Math.Max(0, idx - 1),
                            _ => Math.Min(5, idx + 1)
                        };
                        handled = true;
                    }

                    break;
            }

            PickedColor = col;
        }

        protected override void InternalRender(RenderInformation render_information)
        {
            int x = render_information.ContentRenderArea.X;
            int y = render_information.ContentRenderArea.Y;
            int w = render_information.ContentRenderArea.Width;
            int h = render_information.ContentRenderArea.Height;
            RGBAColor col = PickedColor;
            string hex = $"#{col & 0xffffff:x6}";
            bool foc = IsFocused;

            ConsoleExtensions.RGBForegroundColor = col;

            for (int i = 0; i < 3; ++i)
                ConsoleExtensions.Write(new string('█', 5), (x + 11, y + i));

            ConsoleExtensions.RGBForegroundColor = render_information.Foreground;

            for (int c = 0; c < _channels.Length; ++c)
            {
                byte v = col[_channels[c]];

                Console.SetCursorPosition(x + 1, y + c);
                Console.Write($"{_channels[c]}: ");

                if (c == SelectedChannelIndex && foc)
                    ConsoleExtensions.WriteInverted($"{(v > 0 ? '◄' : ' ')}{v,3}{(v < 255 ? '►' : ' ')}");
                else
                    Console.Write($"{v,4}");
            }

            if (SelectedChannelIndex == _channels.Length && foc)
            {
                int idx = SelectedHexIndex + 1;

                Console.SetCursorPosition(x, y + _channels.Length);
                Console.Write(hex[..idx]);
                ConsoleExtensions.WriteInverted(hex[idx].ToString());
                Console.Write(hex[(idx + 1)..]);
            }
            else
                ConsoleExtensions.Write(hex, (x, y + _channels.Length));

            /*
+----------------+
| R: <xxx>  #####|
| G: <xxx>  #####|
| B: <xxx>  #####|
|#xxxxxxxx       |
+----------------+ 18x6
             */
        }
    }

    public class ScrollPanel
        : ContainerControl
    {
        private ScrollbarVisibility _HorizontalScrollbar = ScrollbarVisibility.Visible;
        private ScrollbarVisibility _VerticalScrollbar = ScrollbarVisibility.Visible;


        public ScrollbarVisibility HorizontalScrollbar
        {
            get => _HorizontalScrollbar;
            set
            {
                if (!Equals(_HorizontalScrollbar, value))
                {
                    _HorizontalScrollbar = value;

                    HorizontalScrollbarChanged?.Invoke(this, value);
                }
            }
        }

        public ScrollbarVisibility VerticalScrollbar
        {
            get => _VerticalScrollbar;
            set
            {
                if (!Equals(_VerticalScrollbar, value))
                {
                    _VerticalScrollbar = value;

                    VerticalScrollbarChanged?.Invoke(this, value);
                }
            }
        }


        public event EventHandler<ScrollbarVisibility>? VerticalScrollbarChanged;
        public event EventHandler<ScrollbarVisibility>? HorizontalScrollbarChanged;


        public ScrollPanel(ControlHost host)
            : base(host)
        {
            BorderStyle = BorderStyle.Thin;
            KeyPress += ScrollPanel_KeyPress;
        }

        private void ScrollPanel_KeyPress(Control sender, ConsoleKeyInfo key, ref bool handled)
        {
            // TODO : handle pageup/pagedown
        }
    }

    // TODO : radiobutton [?]
    // TODO : numeric up/down
    // TODO : calendar picker


    public enum Orientation
    {
        Horizontal,
        Vertical,
    }

    public enum ScrollbarVisibility
    {
        Visible,
        Auto,
        Hidden,
    }
}

using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Text;
using System;

using Unknown6656.Generics;
using Unknown6656.Imaging;
using Unknown6656.Common;


namespace Unknown6656.Controls.Console
{
    using Console = System.Console;


    public delegate void ControlEventHandler(Control sender);

    public delegate void ControlEventHandler<Data>(Control sender, [MaybeNull] Data new_value);

    public delegate void ControlEventHandler2<Data>(Control sender, [MaybeNull] Data old_value, [MaybeNull] Data new_value);

    public delegate void KeyPressEventHandler(Control sender, ConsoleKeyInfo key, ref bool handled);

    public readonly struct ConsoleKeyShortcut
    {
        public readonly ConsoleKey Key { get; }
        public readonly ConsoleModifiers Modifiers { get; }


        public ConsoleKeyShortcut(ConsoleKey key, ConsoleModifiers modifiers)
        {
            Key = key;
            Modifiers = modifiers;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (ConsoleModifiers m in Enum.GetValues(typeof(ConsoleModifiers)))
                if (Modifiers.HasFlag(m))
                    sb.Append(m + " + ");

            return sb.Append(Key).ToString();
        }

        public bool Handles(ConsoleKeyInfo keyinfo) => Key == keyinfo.Key && Modifiers == keyinfo.Modifiers;

        public static implicit operator ConsoleKeyShortcut(ConsoleKey key) => new ConsoleKeyShortcut(key, default);

        public static implicit operator ConsoleKeyShortcut((ConsoleModifiers modifiers, ConsoleKey key) keycut) => new ConsoleKeyShortcut(keycut.key, keycut.modifiers);

        public static implicit operator ConsoleKeyShortcut((ConsoleKey key, ConsoleModifiers modifiers) keycut) => new ConsoleKeyShortcut(keycut.key, keycut.modifiers);

        public static ConsoleKeyShortcut operator +(ConsoleModifiers modifiers, ConsoleKeyShortcut keycut) => keycut + modifiers;

        public static ConsoleKeyShortcut operator +(ConsoleKeyShortcut keycut, ConsoleModifiers modifiers) => new ConsoleKeyShortcut(keycut.Key, keycut.Modifiers | modifiers);
    }

    public sealed class ConsoleKeyMap
    {
        public Dictionary<KeyAction, ConsoleKeyShortcut> KeyboardShortcuts { get; } = new Dictionary<KeyAction, ConsoleKeyShortcut>
        {
            [KeyAction.EnterFocusedElement] = ConsoleKey.Enter,
            [KeyAction.FocusNextSibling] = ConsoleKey.Tab,
            [KeyAction.FocusPreviousSibling] = (ConsoleModifiers.Shift, ConsoleKey.Tab),
            [KeyAction.FocusParent] = ConsoleKey.Escape,
            [KeyAction.ScrollLeft] = ConsoleKey.Home,
            [KeyAction.ScrollRight] = ConsoleKey.End,
            [KeyAction.ScrollUp] = ConsoleKey.PageUp,
            [KeyAction.ScrollDown] = ConsoleKey.PageDown,
            [KeyAction.Quit] = (ConsoleModifiers.Control, ConsoleKey.Q),
            [KeyAction.CursorUp] = ConsoleKey.UpArrow,
            [KeyAction.CursorDown] = ConsoleKey.DownArrow,
            [KeyAction.CursorLeft] = ConsoleKey.LeftArrow,
            [KeyAction.CursorRight] = ConsoleKey.RightArrow,
            [KeyAction.SelectLeft] = (ConsoleModifiers.Control, ConsoleKey.LeftArrow),
            [KeyAction.SelectRight] = (ConsoleModifiers.Control, ConsoleKey.RightArrow),
        };

        public ConsoleKeyShortcut this[KeyAction index]
        {
            get => KeyboardShortcuts[index];
            set => KeyboardShortcuts[index] = value;
        }


        public KeyAction? GetHandledAction(ConsoleKeyInfo keyinfo)
        {
            foreach (KeyValuePair<KeyAction, ConsoleKeyShortcut> kvp in KeyboardShortcuts)
                if (kvp.Value.Handles(keyinfo))
                    return kvp.Key;

            return null;
        }
    }

    public enum KeyAction
    {
        EnterFocusedElement,
        FocusNextSibling,
        FocusPreviousSibling,
        FocusParent,
        ScrollLeft,
        ScrollRight,
        ScrollUp,
        ScrollDown,
        CursorUp,
        CursorDown,
        CursorLeft,
        CursorRight,
        SelectLeft,
        SelectRight,
        Quit,
    }

    public sealed class ControlHost
        : ContainerControl
    {
        private const int INTERVAL_EXIT = 250;
        private const int INTERVAL_WINDOWSIZE = 100;
        private const int INTERVAL_KEYSUSPEND = 50;
        private const int INTERVAL_RENDER = 10;
        private const int INTERVAL_BLINK = 400;

        private readonly ConcurrentQueue<Control> _renderqueue = new ConcurrentQueue<Control>();
        private volatile bool _running;
        private ConsoleState? _old_state;
        private Task? _keyboard_watcher;
        private Task? _resize_watcher;
        private Task? _render_task;
        private Task? _blink_task;
        private Control? _FocusedControl = null;
        private RenderPolicy _renderpolicy = RenderPolicy.Async;
        private (int w, int h) _max_size;
        internal bool _blinkstate;
        private int _yoffs;


        public bool IsRunning => _running;

        public bool KeyListenerSuspended { private set; get; }

        public ConsoleKeyMap KeyMapping { get; } = new ConsoleKeyMap();

        public Control? FocusedControl
        {
            get => _FocusedControl;
            private set
            {
                Control? old = _FocusedControl;

                _FocusedControl = value;

                if (!Equals(old, value))
                {
                    old?.TriggerFocus(false);
                    value?.TriggerFocus(true);
                    FocusedControlChanged?.Invoke(this, value);
                }
            }
        }

        public override Size Size
        {
            get => base.Size;
            set
            {
                base.Size = value;

                Console.WindowWidth = value.Width;
                Console.WindowHeight = value.Height;
            }
        }

        public RenderPolicy RenderPolicy
        {
            get => _renderpolicy;
            set
            {
                RenderPolicy old = _renderpolicy;
                _renderpolicy = value;

                if (!Equals(old, value))
                    RenderPolicyChanged?.Invoke(this, value);
            }
        }


        public event EventHandler<RenderPolicy>? RenderPolicyChanged;
        public event ControlEventHandler<ConsoleKeyInfo>? KeyInputRecieved;
        public event ControlEventHandler<Control?>? FocusedControlChanged;
        internal event ControlEventHandler<bool>? BlinkEvent;


        public ControlHost()
            : base(null!)
        {
            Host = this;
            Position = Point.Empty;
            Foreground = 0xffff;
            Background = 0xf000;
            BorderStyle = BorderStyle.Double;
            KeyInputRecieved += ControlHost_KeyInputRecieved;
        }

        public void SuspendKeyListener() => KeyListenerSuspended = true;

        public void UnsuspendKeyListener() => KeyListenerSuspended = false;

        private void ControlHost_KeyInputRecieved(Control sender, ConsoleKeyInfo key)
        {
            bool handled = false;

            FocusedControl?.OnKeyPress(key, ref handled);

            if (!handled)
                OnKeyPress(key, ref handled);

            // TODO : ?????
        }

        private async Task KeyboardTask()
        {
            while (_running)
                if (Console.KeyAvailable && !KeyListenerSuspended && KeyInputRecieved is { })
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    KeyInputRecieved?.Invoke(this, key);
                }
                else
                    await Task.Delay(INTERVAL_KEYSUSPEND);
        }

        private async Task ResizeTask()
        {
            while (_running)
            {
                Size size = new Size(Console.WindowWidth, Console.WindowHeight);

                if (size != Size)
                {
                    _max_size.w = Math.Max(_max_size.w, size.Width);
                    _max_size.h = Math.Max(_max_size.h, size.Height);

                    TryDo(() => base.Size = size);
                }

                await Task.Delay(INTERVAL_WINDOWSIZE);
            }
        }

        private async Task BlinkTask()
        {
            while (_running)
            {
                _blinkstate ^= true;

                BlinkEvent?.Invoke(this, _blinkstate);

                await Task.Delay(INTERVAL_BLINK);
            }
        }

        private void TryDo(Action act)
        {
            try
            {
                act();
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine("[RENDER ERROR]");

                // TODO : print msg?
            }
        }

        private async Task RenderTask()
        {
            while (_running)
            {
                if (!_renderqueue.TryDequeue(out Control? control))
                    control = null;

                if (control is null)
                    await Task.Delay(INTERVAL_RENDER);
                else
                {
                    // TODO : optimize

                    if (control.Parent is { } p && _renderqueue.Contains(p))
                        continue;
                    else
                        TryDo(control.RenderCallback);
                }
            }
        }

        public void RequestFocus(Control control)
        {
            if (control.Host != this)
                throw new ArgumentException("This control host can only request the focus for a control managed by this host.", nameof(control));
            else if (control.FocusBehaviour != FocusBehaviour.NonFocusable)
                FocusedControl = control;

            // TODO : throw error otherwise?
        }

        public void RequestRender(Control control)
        {
            if (control.Host != this)
                throw new ArgumentException("This control host can only request a render task for a control managed by this host.", nameof(control));
            else if (control == this)
                _renderqueue.Clear();

            _renderqueue.Enqueue(control);
        }

        public void Run()
        {
            if (!_running)
            {
                _old_state = new ConsoleState
                {
                    Background = Console.BackgroundColor,
                    Foreground = Console.ForegroundColor,
                    InputEncoding = Console.InputEncoding,
                    OutputEncoding = Console.OutputEncoding,
                    CursorVisible = Console.CursorVisible,
                    CursorSize = Console.CursorSize,
                    CursorX = Console.CursorLeft,
                    CursorY = Console.CursorTop,
                    Mode = ConsoleExtensions.IsWindowsConsole ? ConsoleExtensions.STDINConsoleMode : default,
                };
                _yoffs = _old_state.CursorY + 1;
                KeyListenerSuspended = false;
                _running = true;

                Console.CancelKeyPress += Console_CancelKeyPress;
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
                Console.CursorSize = 1;
                Console.CursorVisible = false;
                Console.SetCursorPosition(0, _yoffs);

                if (ConsoleExtensions.IsWindowsConsole)
                    ConsoleExtensions.STDINConsoleMode = (ConsoleExtensions.STDINConsoleMode & ~ConsoleMode.ENABLE_QUICK_EDIT_MODE) | ConsoleMode.ENABLE_EXTENDED_FLAGS;

                _renderqueue.Clear();
                _render_task = Task.Factory.StartNew(RenderTask);
                _resize_watcher = Task.Factory.StartNew(ResizeTask);
                _keyboard_watcher = Task.Factory.StartNew(KeyboardTask);
                _blink_task = Task.Factory.StartNew(BlinkTask);
                FocusedControl = this;

                while (_running)
                    Thread.Sleep(INTERVAL_EXIT);
            }
        }

        public void Shutdown()
        {
            if (_running)
            {
                _render_task?.Dispose();
                _render_task = null;
                _resize_watcher?.Dispose();
                _resize_watcher = null;
                _keyboard_watcher?.Dispose();
                _keyboard_watcher = null;
                _blink_task?.Dispose();
                _blink_task = null;
                KeyListenerSuspended = false;
                _running = false;

                Console.CancelKeyPress -= Console_CancelKeyPress;

                if (_old_state is { })
                {
                    Console.BackgroundColor = _old_state.Background;
                    Console.ForegroundColor = _old_state.Foreground;
                    Console.InputEncoding = _old_state.InputEncoding ?? Encoding.Default;
                    Console.OutputEncoding = _old_state.OutputEncoding ?? Encoding.Default;

                    if (ConsoleExtensions.IsWindowsConsole)
                        ConsoleExtensions.STDINConsoleMode = _old_state.Mode | ConsoleMode.ENABLE_QUICK_EDIT_MODE | ConsoleMode.ENABLE_EXTENDED_FLAGS;

                    Clear(new RenderInformation(BoundingBox, BoundingBox, null, false, default, default));

                    if (_old_state.CursorX < Width - 1)
                        Console.SetCursorPosition(_old_state.CursorX + 1, _old_state.CursorY);
                    else
                        Console.SetCursorPosition(0, _yoffs);

                    Console.WriteLine();
                    Console.CursorSize = _old_state.CursorSize;
                    Console.CursorVisible = _old_state.CursorVisible;
                }
                else
                {
                    Console.WriteLine();
                    Console.CursorVisible = true;
                }

                _old_state = null;
            }
        }

        private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e) => Shutdown();


        private sealed class ConsoleState
        {
            public ConsoleMode Mode { get; set; }
            public ConsoleColor Background { set; get; }
            public ConsoleColor Foreground { set; get; }
            public Encoding? OutputEncoding { set; get; }
            public Encoding? InputEncoding { set; get; }
            public bool CursorVisible { set; get; }
            public int CursorSize { set; get; }
            public int CursorX { set; get; }
            public int CursorY { set; get; }
        }
    }

    public abstract class Control
    {
        #region FIELDS

        private int _relativezindex = 0;
        private int _relativetabindex = 0;
        private ScrollBarInformation _scrollbars = new ScrollBarInformation(null, null);
        private FocusedStyle _focusedstyle = FocusedStyle.ReverseColors;
        private FocusBehaviour _focusbehaviour = FocusBehaviour.Focusable;
        private ControlVisiblity _visiblity = ControlVisiblity.Visible;
        private BorderStyle _borderstyle = BorderStyle.Thin;
        private RGBAColor _background = 0xf000;
        private RGBAColor _foreground = 0xffff;
        private bool _isvisible = true;
        private Size _clientsize = Size.Empty;
        private Size _size = Size.Empty;
        private Size _minimumsize = new Size(2, 2);
        private Size _maximumsize = new Size(1000, 1000);
        private Point _position = Point.Empty;
        private Point _absoluteposition = Point.Empty;
        private ContainerControl? _parent = null;
        private Rectangle _renderarea = Rectangle.Empty;
        private string? _text = null;

        #endregion
        #region PROPERTIES

        public ControlHost Host { get; internal set; }

        public bool CanChangeText { get; protected set; } = true;

        protected ScrollBarInformation Scrollbars
        {
            get => _scrollbars;
            set
            {
                if (!Equals(_scrollbars, value))
                {
                    if ((value.Horizontal.HasValue || value.Vertical.HasValue) && BorderStyle == BorderStyle.None)
                        ; // TODO : throw ?

                    _scrollbars = value;

                    ScrollbarsChanged?.Invoke(this, value);
                }
            }
        }

        protected abstract bool UseDefaultTextRenderer { get; }

        public FocusBehaviour FocusBehaviour
        {
            get => _focusbehaviour;
            protected set
            {
                if (!Equals(_focusbehaviour, value))
                {
                    if (this is ContainerControl && value == FocusBehaviour.NonFocusable)
                        throw new ArgumentException($"A container control cannot have have a focus behaviour of the value '{value}'.", nameof(value));

                    _focusbehaviour = value;
                }
            }
        }

        public FocusedStyle FocusedStyle
        {
            get => _focusedstyle;
            protected set
            {
                if (!Equals(_focusedstyle, value))
                {
                    _focusedstyle = value;

                    FocusedStyleChanged?.Invoke(this, value);
                }
            }
        }

        public bool IsFocused => Equals(Host.FocusedControl, this);

        public string? Text
        {
            get => _text;
            set
            {
                if (!Equals(_text, value))
                {
                    if (!CanChangeText)
                        throw new InvalidOperationException("The text of this control cannot be changed.");

                    _text = value;

                    UpdateClientArea();
                    TextChanged?.Invoke(this, value);
                }
            }
        }

        public int TextLength => Text?.Length ?? 0;

        public int RelativeZIndex
        {
            get => _relativezindex;
            set
            {
                if (!Equals(_relativezindex, value))
                {
                    _relativezindex = value;

                    RelativeZIndexChanged?.Invoke(this, value);
                }
            }
        }

        public int RelativeTabIndex
        {
            get => _relativetabindex;
            set
            {
                if (!Equals(_relativetabindex, value))
                {
                    _relativetabindex = value;

                    RelativeTabIndexChanged?.Invoke(this, value);
                }
            }
        }

        public RGBAColor Background
        {
            get => _background;
            set
            {
                if (!Equals(_background, value))
                {
                    _background = value;

                    BackgroundChanged?.Invoke(this, value);
                }
            }
        }

        public RGBAColor Foreground
        {
            get => _foreground;
            set
            {
                if (!Equals(_foreground, value))
                {
                    _foreground = value;

                    ForegroundChanged?.Invoke(this, value);
                }
            }
        }

        public BorderStyle BorderStyle
        {
            get => _borderstyle;
            set
            {
                if (!Equals(_borderstyle, value))
                {
                    _borderstyle = value;

                    UpdateClientArea();
                    BorderStyleChanged?.Invoke(this, value);
                }
            }
        }

        public Point Position
        {
            get => _position;
            set
            {
                if (!Equals(_position, value))
                {
                    _position = value;

                    UpdateAbsolutePosition();
                    PositionChanged?.Invoke(this, value);
                }
            }
        }

        public ContainerControl? Parent
        {
            get => _parent;
            set
            {
                if (!Equals(_parent, value))
                {
                    if (Host != value?.Host && value?.Host is { })
                        throw new ArgumentException("The parent control must have the same control host as the child control.", nameof(value));

                    ContainerControl? old = _parent;

                    UpdateParentHandlers(false);

                    _parent = value;

                    UpdateParentHandlers(true);
                    ParentChanged?.Invoke(this, old, value);
                }
            }
        }

        public Point AbsolutePosition
        {
            get => _absoluteposition;
            private set
            {
                _absoluteposition = value;

                UpdateRenderableClientArea();
                AbsolutePositionChanged?.Invoke(this, value);
            }
        }

        public ControlVisiblity Visiblity
        {
            get => _visiblity;
            set
            {
                if (!Equals(_visiblity, value))
                {
                    _visiblity = value;
                    IsVisible = value == ControlVisiblity.Visible && (Parent?.IsVisible ?? true);

                    VisiblityChanged?.Invoke(this, value);
                }
            }
        }

        public bool IsVisible
        {
            get => _isvisible;
            private set
            {
                _isvisible = value;

                IsVisibleChanged?.Invoke(this, value);
            }
        }

        public virtual Size Size
        {
            get => _size;
            set
            {
                if (!Equals(_size, value))
                {
                    value.Width = Math.Min(MaximumSize.Width, Math.Max(value.Width, MinimumSize.Width));
                    value.Height = Math.Min(MaximumSize.Height, Math.Max(value.Height, MinimumSize.Height));

                    _size = value;

                    UpdateClientArea();
                    SizeChanged?.Invoke(this, value);
                }
            }
        }

        public Size MinimumSize
        {
            get => _minimumsize;
            set
            {
                if (!Equals(_minimumsize, value))
                {
                    bool border = BorderStyle != BorderStyle.None;
                    int min_height = string.IsNullOrWhiteSpace(Text) ? (border ? 2 : 1) : (border ? 4 : 2);

                    value = new Size(Math.Max(value.Width, 2), Math.Max(value.Height, min_height));

                    _minimumsize = value;

                    MinimumSizeChanged?.Invoke(this, value);

                    Size = new Size(Math.Max(Width, value.Width), Math.Max(Height, value.Height));
                }
            }
        }

        public Size MaximumSize
        {
            get => _maximumsize;
            set
            {
                if (!Equals(_maximumsize, value))
                {
                    if (value.Width > 1000 || value.Height > 1000)
                        throw new ArgumentOutOfRangeException(nameof(value));

                    _maximumsize = value;

                    MaximumSizeChanged?.Invoke(this, value);

                    Width = Math.Min(Width, value.Width);
                    Height = Math.Min(Height, value.Height);
                }
            }
        }

        public event EventHandler<Size>? MaximumSizeChanged;


        public event EventHandler<Size>? MinimumSizeChanged;


        public int Width
        {
            get => Size.Width;
            set => Size = new Size(value, Height);
        }

        public int Height
        {
            get => Size.Height;
            set => Size = new Size(Width, value);
        }

        public int Top
        {
            get => Position.Y;
            set => Position = new Point(Left, value);
        }

        public int Left
        {
            get => Position.X;
            set => Position = new Point(value, Top);
        }

        public int Bottom => Top + Height;

        public int Right => Left + Width;

        public Size ClientSize
        {
            get => _clientsize;
            private set
            {
                _clientsize = value;

                UpdateRenderableClientArea();
                ClientSizeChanged?.Invoke(this, value);
            }
        }

        // protected Size BorderSize => BorderStyle == BorderStyle.None

        protected Rectangle BoundingBox => new Rectangle(AbsolutePosition, Size);

        public Control[] Siblings => Parent is { Children: IEnumerable<Control> ch } ? ch.ToArrayWhere(c => c != this) : Array.Empty<Control>();

        protected Rectangle RenderableAbsoluteClientArea
        {
            get => _renderarea;
            private set
            {
                _renderarea = value;

                RenderableAbsoluteClientAreaChanged?.Invoke(this, value);
            }
        }

        // TODO : min size, max size

        #endregion
        #region EVENTS

        public event KeyPressEventHandler? KeyPress;
        public event ControlEventHandler<int>? RelativeTabIndexChanged;
        public event ControlEventHandler<int>? RelativeZIndexChanged;
        public event ControlEventHandler<bool>? IsVisibleChanged;
        public event ControlEventHandler<string?>? TextChanged;
        public event ControlEventHandler<ControlVisiblity>? VisiblityChanged;
        public event ControlEventHandler<BorderStyle>? BorderStyleChanged;
        public event ControlEventHandler<FocusedStyle>? FocusedStyleChanged;
        public event ControlEventHandler<RGBAColor>? BackgroundChanged;
        public event ControlEventHandler<RGBAColor>? ForegroundChanged;
        public event ControlEventHandler2<ContainerControl?>? ParentChanged;
        public event ControlEventHandler<Point>? PositionChanged;
        public event ControlEventHandler<Point>? AbsolutePositionChanged;
        public event ControlEventHandler<Size>? SizeChanged;
        public event ControlEventHandler<Size>? ClientSizeChanged;
        public event ControlEventHandler? FocusRequested;
        public event ControlEventHandler? FocusAcquired;
        public event ControlEventHandler? FocusLost;
        public event ControlEventHandler? RenderRequested;
        public event ControlEventHandler? BeforeRendering;
        public event ControlEventHandler? AfterRendering;
        public event ControlEventHandler<ScrollBarInformation>? ScrollbarsChanged;
        protected event ControlEventHandler<Rectangle>? RenderableAbsoluteClientAreaChanged;

        #endregion

        public Control(ControlHost host)
        {
            Host = host;

            IsVisibleChanged += (_, _) => RequestRender();
            BackgroundChanged += (_, _) => RequestRender();
            ForegroundChanged += (_, _) => RequestRender();
            BorderStyleChanged += (_, _) => RequestRender();
            RenderableAbsoluteClientAreaChanged += (_, _) => RequestRender();
            FocusedStyleChanged += (_, _) => RequestRender();
            FocusAcquired += _ => RequestRender();
        }

        public override string ToString() => $"({Left},{Top}: {Width}x{Height}, {(IsFocused ? 'F' : FocusBehaviour switch { FocusBehaviour.Focusable => 'f', FocusBehaviour.PassFocusThrough => 'p', _ => 'n' })}) {GetType().Name}: {Text}";

        #region PARENT HANDLERS

        private void UpdateParentHandlers(bool subscribe)
        {
            if (Parent is { } parent)
                if (subscribe)
                {
                    parent.IsVisibleChanged += Parent_OnIsVisibleChanged;
                    parent.AbsolutePositionChanged += Parent_OnAbsolutePositionChanged;
                    parent.RenderableAbsoluteClientAreaChanged += Parent_OnRenderableAbsoluteClientAreaChanged;
                }
                else
                {
                    parent.IsVisibleChanged -= Parent_OnIsVisibleChanged;
                    parent.AbsolutePositionChanged -= Parent_OnAbsolutePositionChanged;
                    parent.RenderableAbsoluteClientAreaChanged -= Parent_OnRenderableAbsoluteClientAreaChanged;
                }

            Parent_OnRenderableAbsoluteClientAreaChanged(this, default);
            Parent_OnAbsolutePositionChanged(this, default);
            Parent_OnIsVisibleChanged(this, default);

            // TODO : other handlers?
        }

        private void Parent_OnRenderableAbsoluteClientAreaChanged(Control? sender, Rectangle _) => UpdateRenderableClientArea();

        private void Parent_OnAbsolutePositionChanged(Control? sender, Point _) => UpdateAbsolutePosition();

        private void Parent_OnIsVisibleChanged(Control? sender, bool _) => IsVisible = Visiblity == ControlVisiblity.Visible && (Parent?.IsVisible ?? true);

        #endregion
        #region LAYOUT HANDLERS

        private void UpdateAbsolutePosition()
        {
            Point p = Position;

            p.Offset(Parent?.AbsolutePosition ?? Point.Empty);

            AbsolutePosition = p;
        }

        private void UpdateClientArea()
        {
            MinimumSize = _minimumsize; // update min size

            Size size = Size;
            int b = BorderStyle == BorderStyle.None ? 0 : 2;
            int h = string.IsNullOrWhiteSpace(Text) ? 2 : 0;

            ClientSize = new Size(size.Width - b, size.Height - b - h);
        }

        private void UpdateRenderableClientArea()
        {
            Rectangle area = BoundingBox;
            Rectangle isect = Parent?.RenderableAbsoluteClientArea ?? area;

            isect.Intersect(area);

            RenderableAbsoluteClientArea = isect;
        }

        internal void TriggerFocus(bool aquired) => (aquired ? FocusAcquired : FocusLost)?.Invoke(this);

        internal void OnKeyPress(ConsoleKeyInfo key, ref bool handled)
        {
            bool backwards = false;
            Control[] focusable_children = this is ContainerControl { Children: var cs } ? (from c in cs
                                                                                            where c.FocusBehaviour != FocusBehaviour.NonFocusable
                                                                                            orderby c.RelativeTabIndex ascending
                                                                                            select c).ToArray() : Array.Empty<Control>();

            switch (Host.KeyMapping.GetHandledAction(key))
            {
                case KeyAction.Quit:
                    handled = true;
                    Host.Shutdown();

                    return;
                case KeyAction.EnterFocusedElement when IsFocused && focusable_children.Length > 0:
                    handled = true;
                    focusable_children[0].RequestFocus();

                    return;
                case KeyAction.FocusPreviousSibling:
                    backwards = true;

                    goto case KeyAction.FocusNextSibling;
                case KeyAction.FocusNextSibling:
                    if (focusable_children.Length > 0)
                    {
                        int idx = (from t in focusable_children.WithIndex()
                                   where t.Item.IsFocused
                                   select t.Index).ToList() is { Count: > 0 } l ? (l[0] + (backwards ? -1 : 1) + focusable_children.Length) % focusable_children.Length : 0;

                        handled = true;
                        focusable_children[idx].RequestFocus();

                        return;
                    }
                    else
                        break;
                case KeyAction.FocusParent when IsFocused:
                    {
                        Control @this = this;
                        Control? parent = null;

                        do
                            if ((parent = @this.Parent) is { })
                                if (ReferenceEquals(parent, @this))
                                    return;
                                else if (parent.FocusBehaviour == FocusBehaviour.PassFocusThrough)
                                    @this = parent;
                                else
                                {
                                    parent.RequestFocus();

                                    handled = true;

                                    return;
                                }
                        while (parent is { });
                    }

                    break;
                case KeyAction.ScrollLeft:
                case KeyAction.ScrollRight:
                case KeyAction.ScrollUp:
                case KeyAction.ScrollDown:
                    // TODO

                    break;
                case null:
                default:
                    break;
            }

            foreach (Delegate? del in KeyPress?.GetInvocationList() ?? Array.Empty<Delegate>())
                if (handled)
                    return;
                else
                {
                    object[] args = { this, key, handled };

                    del?.DynamicInvoke(args);

                    handled = (bool)args[2];
                }

            if (Parent is { } && Parent != this)
                Parent.OnKeyPress(key, ref handled);
        }

        #endregion
        #region RENDER METHODS

        public void RequestFocus()
        {
            FocusRequested?.Invoke(this);
            Host?.RequestFocus(this);
        }

        public void RequestRender()
        {
            RenderRequested?.Invoke(this);

            if (Host?.RenderPolicy == RenderPolicy.Sync)
                RenderCallback();
            else
                Host?.RequestRender(this);
        }

        internal protected void RenderCallback()
        {
            if (IsVisible)
            {
                BeforeRendering?.Invoke(this);

                RGBAColor old_fg = ConsoleExtensions.RGBForegroundColor;
                RGBAColor old_bg = ConsoleExtensions.RGBBackgroundColor;
                (RGBAColor fg, RGBAColor bg, bool sw_border) = FocusedStyle switch
                {
                    FocusedStyle.ReverseColors when IsFocused => (Background, Foreground, false),
                    FocusedStyle.ReverseBorderColorsOnly when IsFocused => (Foreground, Background, true),
                    _ => (Foreground, Background, false),
                };

                try
                {
                    Rectangle render_area = RenderableAbsoluteClientArea;
                    string? text = string.IsNullOrWhiteSpace(Text) ? null : Regex.Replace(Text.Trim(), @"[\s\x00-\x20\x7f-\xa0]", " ");
                    int b = BorderStyle == BorderStyle.None ? 0 : 1;

                    if (text is { Length: int l } && l > Width - 2 && l > 3)
                        text = Width < 5 ? new string('.', Width - 2) : text[..(Width - 5)] + "...";

                    Rectangle content = new Rectangle(AbsolutePosition.X + b, AbsolutePosition.Y + b + (text is null ? 0 : 2), render_area.Width - 2 * b, render_area.Height - (2 * b) - (text is null ? 0 : 2));
                    RenderInformation info = new RenderInformation(render_area, content, text, Host._blinkstate, fg, bg);
                    ConsoleExtensions.RGBForegroundColor = fg;
                    ConsoleExtensions.RGBBackgroundColor = bg;

                    Clear(info);

                    if (sw_border)
                    {
                        ConsoleExtensions.RGBForegroundColor = bg;
                        ConsoleExtensions.RGBBackgroundColor = fg;
                    }

                    RenderBorders(info);

                    ConsoleExtensions.RGBForegroundColor = fg;
                    ConsoleExtensions.RGBBackgroundColor = bg;

                    if (UseDefaultTextRenderer)
                        RenderText(info);

                    InternalRender(info);
                }
                catch (Exception ex)
                {
                    DrawRenderError(ex);
                }

                Console.SetCursorPosition(AbsolutePosition.X, AbsolutePosition.Y);
                ConsoleExtensions.RGBForegroundColor = old_fg;
                ConsoleExtensions.RGBBackgroundColor = old_bg;
                Console.Write("\x1b[?25l");

                AfterRendering?.Invoke(this);
            }
        }

        private void RenderText(RenderInformation render_information)
        {
            if (render_information.SanitizedString is string text)
            {
                int b = BorderStyle == BorderStyle.None ? 0 : 1;

                Console.SetCursorPosition(AbsolutePosition.X + b, AbsolutePosition.Y + b);
                Console.Write(text);
            }
        }

        protected void Clear(RenderInformation render_information)
        {
            Rectangle area = render_information.ControlRenderArea;
            string line = new string(' ', area.Width);

            for (int yoffs = 0, h = area.Height, x = area.X, y = area.Y; yoffs < h; ++yoffs)
            {
                Console.SetCursorPosition(x, y + yoffs);
                Console.Write(line);
            }
        }

        protected virtual void RenderBorders(RenderInformation render_information)
        {
            int w = Width;
            int x = AbsolutePosition.X;
            int y = AbsolutePosition.Y;
            Rectangle area = render_information.ControlRenderArea;
            string box = BorderStyle switch
            {
                // TODO : change horizontal scroll bar style!

                BorderStyle.Thin => "┌┐└┘├┤──││◄►▲▼▓▓░░",

                BorderStyle.Thick => "┏┓┗┛┣┫━━┃┃◄►▲▼▓▓░░",

                BorderStyle.Double => "╔╗╚╝╠╣══║║◄►▲▼▓▓░░",

                BorderStyle.TextBox => "┌┐╘╛├┤─═││◄►▲▼▓▓░░",

                BorderStyle.Rounded => "╭╮╰╯├┤──││◄►▲▼▓▓░░",

                BorderStyle.ASCII => "..\'\'++--||<>^v@@-:",
                _ => "                "
            };
            /* box[ 0] = top left
             * box[ 1] = top right
             * box[ 2] = bottom left
             * box[ 3] = bottom right
             * box[ 4] = left cross
             * box[ 5] = right cross
             * box[ 6] = horizontal (top)
             * box[ 7] = horizontal (bottom)
             * box[ 8] = vertical (left)
             * box[ 9] = vertical (right)
             * box[10] = scroll left
             * box[11] = scroll right
             * box[12] = scroll up
             * box[13] = scroll down
             * box[14] = scroll bar (hor.)
             * box[15] = scroll bar (ver.)
             * box[16] = scroll background (hor.)
             * box[17] = scroll background (ver.)
             */

            if (w > 0 && BorderStyle != BorderStyle.None)
            {
                ConsoleExtensions.RGBForegroundColor = render_information.Foreground;
                ConsoleExtensions.RGBBackgroundColor = render_information.Background;
                bool hastxt = !string.IsNullOrWhiteSpace(Text);

                for (int yoffs = 0, h = Height; yoffs < h; ++yoffs)
                {
                    Console.SetCursorPosition(x, y + yoffs);

                    if (w == 1)
                        Console.Write(box[7]);
                    else
                        Console.Write(yoffs switch
                        {
                            0 => $"{box[0]}{new string(box[6], w - 2)}{box[1]}",
                            _ when yoffs == h - 1 => $"{box[2]}{new string(box[7], w - 2)}{box[3]}",
                            2 when hastxt => $"{box[4]}{new string(box[6], w - 2)}{box[5]}",
                            _ => $"{box[8]}{new string(' ', w - 2)}{box[9]}"
                        });
                }

                if (Scrollbars.Horizontal is (double pos_h, double wdh))
                {
                    int length = area.Width - 4;
                    int width = Math.Max((int)(length * wdh), 1);
                    int pre = (int)((length - width) * pos_h);
                    int post = length - width - pre;

                    Console.SetCursorPosition(x + 1, Height - 1);
                    Console.Write($"{box[10]}{new string(box[16], pre)}{new string(box[14], width)}{new string(box[16], post)}{box[11]}");
                }

                if (Scrollbars.Vertical is (double pos_v, double hgt))
                {
                    int y_offs = hastxt ? 1 : 3;
                    int length = area.Height - y_offs - 3;
                    int height = Math.Max((int)(length * hgt), 1);
                    int pre = (int)((length - height) * pos_v);

                    Console.SetCursorPosition(Width - 1, y + y_offs);
                    ConsoleExtensions.WriteVertical($"{box[12]}{new string(box[17], pre)}{new string(box[15], height)}{new string(box[17], length - height - pre)}{box[13]}");
                }
            }
        }

        private void DrawRenderError(Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Red;

            Rectangle area = RenderableAbsoluteClientArea;
            string line = new string('░', area.Width);
            int x = area.X;
            int y = area.Y;

            for (int yoffs = 0, h = area.Height; yoffs < h; ++yoffs)
            {
                Console.SetCursorPosition(x, y + yoffs);
                Console.Write(line);
            }

            Console.SetCursorPosition(x, y);
            Console.Write("[ERROR]");
            Debug.Print(ex.Message);
            Debug.Print(ex.StackTrace);
        }

        // TODO : draw drop shadow

        protected abstract void InternalRender(RenderInformation render_information);


        protected sealed class RenderInformation
        {
            public bool BlinkState { get; }
            public Rectangle ContentRenderArea { get; }
            public Rectangle ControlRenderArea { get; }
            public string? SanitizedString { get; }
            public RGBAColor Foreground { get; }
            public RGBAColor Background { get; }


            public RenderInformation(Rectangle control, Rectangle content, string? txt, bool blink, RGBAColor fg, RGBAColor bg)
            {
                ControlRenderArea = control;
                ContentRenderArea = content;
                SanitizedString = txt;
                BlinkState = blink;
                Foreground = fg;
                Background = bg;
            }
        }

        #endregion
    }

    public class ContainerControl
        : Control
    {
        private readonly HashSet<Control> _children = new HashSet<Control>();


        protected override bool UseDefaultTextRenderer { get; } = true;

        public int ChildrenCount => _children.Count;

        public Control[] Children => _children.ToArray();


        public event ControlEventHandler<Control[]>? ChildCollectionChanged;
        public event ControlEventHandler<Control>? ChildControlRemoved;
        public event ControlEventHandler<Control>? ChildControlAdded;


        public event ControlEventHandler<int>? ChildRelativeTabIndexChanged;
        public event ControlEventHandler<int>? ChildRelativeZIndexChanged;
        public event ControlEventHandler<bool>? ChildIsVisibleChanged;
        public event ControlEventHandler<string?>? ChildTitleChanged;
        public event ControlEventHandler<ControlVisiblity>? ChildVisiblityChanged;
        public event ControlEventHandler<BorderStyle>? ChildBorderStyleChanged;
        public event ControlEventHandler<RGBAColor>? ChildBackgroundChanged;
        public event ControlEventHandler<RGBAColor>? ChildForegroundChanged;
        public event ControlEventHandler<Point>? ChildPositionChanged;
        public event ControlEventHandler<Point>? ChildAbsolutePositionChanged;
        public event ControlEventHandler<Size>? ChildSizeChanged;
        public event ControlEventHandler<Size>? ChildClientSizeChanged;
        public event ControlEventHandler? ChildFocusRequested;
        public event ControlEventHandler? ChildFocusAcquired;
        public event ControlEventHandler? ChildFocusLost;


        public ContainerControl(ControlHost host)
            : base(host)
        {
            FocusedStyle = FocusedStyle.RenderAsNormal;
            FocusBehaviour = FocusBehaviour.PassFocusThrough;
            ChildCollectionChanged += (_, _) => RequestRender();
            FocusAcquired += ContainerControl_FocusAcquired;
            ChildRelativeZIndexChanged += Child_Invalidated;
            ChildAbsolutePositionChanged += Child_Invalidated;
            ChildSizeChanged += Child_Invalidated;
            ChildFocusAcquired += Child_FocusChanged;
            ChildFocusLost += Child_FocusChanged;
        }

        private void ContainerControl_FocusAcquired(Control sender)
        {
            if ((from c in _children
                 where c.FocusBehaviour != FocusBehaviour.NonFocusable
                 orderby c.RelativeTabIndex ascending
                 select c).FirstOrDefault() is Control first)
                first.RequestFocus();
        }

        private void RegisterHandlers(Control? control, bool subsribe)
        {
            if (control is { })
                if (subsribe)
                {
                    control.ParentChanged += Child_ParentChanged;
                    control.RelativeTabIndexChanged += ChildRelativeTabIndexChanged;
                    control.RelativeZIndexChanged += ChildRelativeZIndexChanged;
                    control.IsVisibleChanged += ChildIsVisibleChanged;
                    control.TextChanged += ChildTitleChanged;
                    control.VisiblityChanged += ChildVisiblityChanged;
                    control.BorderStyleChanged += ChildBorderStyleChanged;
                    control.BackgroundChanged += ChildBackgroundChanged;
                    control.ForegroundChanged += ChildForegroundChanged;
                    control.PositionChanged += ChildPositionChanged;
                    control.AbsolutePositionChanged += ChildAbsolutePositionChanged;
                    control.SizeChanged += ChildSizeChanged;
                    control.ClientSizeChanged += ChildClientSizeChanged;
                    control.FocusRequested += ChildFocusRequested;
                    control.FocusAcquired += ChildFocusAcquired;
                    control.FocusLost += ChildFocusLost;
                }
                else
                {
                    control.ParentChanged -= Child_ParentChanged;
                    control.RelativeTabIndexChanged -= ChildRelativeTabIndexChanged;
                    control.RelativeZIndexChanged -= ChildRelativeZIndexChanged;
                    control.IsVisibleChanged -= ChildIsVisibleChanged;
                    control.TextChanged -= ChildTitleChanged;
                    control.VisiblityChanged -= ChildVisiblityChanged;
                    control.BorderStyleChanged -= ChildBorderStyleChanged;
                    control.BackgroundChanged -= ChildBackgroundChanged;
                    control.ForegroundChanged -= ChildForegroundChanged;
                    control.PositionChanged -= ChildPositionChanged;
                    control.AbsolutePositionChanged -= ChildAbsolutePositionChanged;
                    control.SizeChanged -= ChildSizeChanged;
                    control.ClientSizeChanged -= ChildClientSizeChanged;
                    control.FocusRequested -= ChildFocusRequested;
                    control.FocusAcquired -= ChildFocusAcquired;
                    control.FocusLost -= ChildFocusLost;
                }

            Child_Invalidated<bool>(this, false);
            Child_FocusChanged(this);
        }

        private void Child_Invalidated<T>(Control sender, T _) => RequestRender();

        private void Child_ParentChanged(Control sender, ContainerControl? old_parent, ContainerControl? new_parent)
        {
            if (ReferenceEquals(old_parent, this) && !ReferenceEquals(new_parent, this))
                RemoveChild(sender);
        }

        private void Child_FocusChanged(Control sender) => RequestRender();

        public T AddNewChild<T>()
            where T : Control // , new(ControlHost)
        {
            try
            {
                if (typeof(T).GetConstructor(new[] { typeof(ControlHost) })?.Invoke(new object[] { Host }) is T control)
                    return AddChild<T>(control);
            }
            catch
            {
            }

            throw new ArgumentException($"The given type '{typeof(T)}' does not have a constructor accepting a single argument of the type '{typeof(ControlHost)}'.", nameof(T));
        }

        public T AddNewChild<T>(Action<T> callback)
            where T : Control // , new(ControlHost)
        {
            T control = AddNewChild<T>();

            callback(control);

            return control;
        }

        public T AddChild<T>(T control)
            where T : Control
        {
            if (control is null)
                throw new ArgumentNullException(nameof(control));
            else if (control.Host != Host)
                throw new ArgumentException("The children's control host must be the same as the parent's one.");

            if (!HasChild(control))
            {
                _children.Add(control);
                ChildControlAdded?.Invoke(this, control);
                ChildCollectionChanged?.Invoke(this, Children);
                RegisterHandlers(control, true);
            }

            control!.Parent = this;

            return control;
        }

        public bool RemoveChild<T>(T control)
            where T : Control
        {
            if (control is { } && HasChild(control))
            {
                control.Parent = null;
                _children.Remove(control);
                ChildControlRemoved?.Invoke(this, control);
                ChildCollectionChanged?.Invoke(this, Children);
                RegisterHandlers(control, false);

                return true;
            }

            return false;
        }

        public void RemoveAllChildren()
        {
            foreach (Control? control in Children)
                RemoveChild(control);
        }

        public bool HasChild<T>(T control) where T : Control => control.Host == Host && _children.Contains(control);

        protected override void InternalRender(RenderInformation render_information)
        {
            foreach (Control control in _children.OrderBy(c => c.RelativeZIndex))
                if (Host.RenderPolicy == RenderPolicy.Sync)
                    control.RenderCallback();
                else
                    control.RequestRender();
        }
    }

    public readonly struct ScrollBarInformation
    {
        public readonly (double Position, double Width)? Horizontal { get; }
        public readonly (double Position, double Height)? Vertical { get; }


        public ScrollBarInformation((double Position, double Width)? horizontal, (double Position, double Height)? vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }
    }

    public enum BorderStyle
    {
        None,
        Thin,
        Thick,
        Double,
        TextBox,
        ASCII,
        Rounded,
    }

    public enum ControlVisiblity
    {
        Hidden,
        Visible,
    }

    public enum RenderPolicy
    {
        Async,
        Sync
    }

    public enum FocusBehaviour
    {
        NonFocusable,
        PassFocusThrough,
        Focusable,
    }

    public enum FocusedStyle
    {
        ReverseColors,
        RenderAsNormal,
        ReverseBorderColorsOnly,
        // TODO : ?
    }
}

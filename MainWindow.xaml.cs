using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using static emoji_picker_wpf.NativeMethods;

namespace emoji_picker_wpf
{
    public partial class MainWindow : Window
    {
        private AppConfig _config = AppConfig.Load();

        private IntPtr _previousWindow;
        public IReadOnlyList<KeyValuePair<string, Emoji>> Emojis { get; private set; } = Array.Empty<KeyValuePair<string, Emoji>>();
        public IReadOnlyDictionary<string, Emoji> EmojiDict { get; private set; } = new Dictionary<string, Emoji>();
        public ObservableCollection<KeyValuePair<string, Emoji>> FilteredEmojis { get; private set; } = new();

        public void ForceForeground()
        {
            IntPtr targetHwnd = new WindowInteropHelper(this).Handle;
            IntPtr foregroundHwnd = GetForegroundWindow();
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundHwnd, IntPtr.Zero);
            uint appThreadId = GetCurrentThreadId();
            if (foregroundThreadId != appThreadId)
            {
                AttachThreadInput(appThreadId, foregroundThreadId, true);
                SetForegroundWindow(targetHwnd);
                this.Activate();
                AttachThreadInput(appThreadId, foregroundThreadId, false);
            }
        }

        public void EnableBlur()
        {
            WindowInteropHelper windowHelper = new WindowInteropHelper(this);
            AccentPolicy accent = new AccentPolicy
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = 0x01000000
            };

            int accentStructSize = Marshal.SizeOf(accent);
            nint accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            WindowCompositionAttributeData data = new WindowCompositionAttributeData
            {
                Attribute = 19, // WCA_ACCENT_POLICY
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        public MainWindow()
        {
            InitializeComponent();
            _previousWindow = GetForegroundWindow();

            DataContext = this;
            searchBox.Focus();

            Loaded += async (s, e) =>
            {
                nint hwnd = new WindowInteropHelper(this).Handle;
                GetCursorPos(out POINT cursor);
                MoveWindow(hwnd, cursor.X, cursor.Y, (int)this.ActualWidth, (int)this.ActualHeight, true);

                if (_config.BackdropMode == BackdropMode.Extend)
                {
                    MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                    DwmExtendFrameIntoClientArea(hwnd, ref margins);
                }
                else if (_config.BackdropMode == BackdropMode.Acrylic)
                {
                    EnableBlur();
                }

                int dark = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

                int captionColor = 0x00202020;
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

                await Task.Run(() =>
                {
                    EmojiDict = JsonConvert.DeserializeObject<Dictionary<string, Emoji>>(resources.dataByEmoji)!;
                    Emojis = new List<KeyValuePair<string, Emoji>>(EmojiDict.ToList());
                });

                foreach (KeyValuePair<string, Emoji> kvp in Emojis)
                    FilteredEmojis.Add(kvp);

                ForceForeground();
                this.Topmost = true;
            };
        }

        private void InsertEmoji(string emoji)
        {
            SetForegroundWindow(_previousWindow);
            System.Windows.Forms.SendKeys.SendWait(emoji);
            this.Close();
        }

        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilteredEmojis.Clear();
            string search = searchBox.Text?.Trim().ToLower() ?? "";

            Dictionary<string, Emoji> priorityKeys = new();
            if (searchBox.Text == "v") priorityKeys["✌️"] = EmojiDict["✌️"];
            if (searchBox.Text == "b") priorityKeys["💔"] = EmojiDict["💔"];
            if (searchBox.Text == "w") priorityKeys["🥀"] = EmojiDict["🥀"];

            foreach (KeyValuePair<string, Emoji> kvp in priorityKeys) FilteredEmojis.Add(kvp);

            foreach (KeyValuePair<string, Emoji> kvp in string.IsNullOrWhiteSpace(search)
                     ? Emojis
                     : Emojis.Where(k => k.Value.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            {
                if (!priorityKeys.ContainsKey(kvp.Key)) FilteredEmojis.Add(kvp);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_ERASEBKGND) { handled = true; return new IntPtr(1); }
            return IntPtr.Zero;
        }

        private void emojiView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (emojiView.SelectedItem is KeyValuePair<string, Emoji> kvp)
                InsertEmoji(kvp.Key);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close();
            if (e.Key == Key.Enter && emojiView.Items.Count > 0)
                if (emojiView.Items[0] is KeyValuePair<string, Emoji> kvp)
                    InsertEmoji(kvp.Key);
        }
    }
}
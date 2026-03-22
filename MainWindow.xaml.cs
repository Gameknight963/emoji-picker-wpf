using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;


namespace emoji_picker_wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();
        
        // balls
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

        private IntPtr _previousWindow;

        public IReadOnlyList<KeyValuePair<string, Emoji>> Emojis { get; private set; } = Array.Empty<KeyValuePair<string, Emoji>>();
        public IReadOnlyDictionary<string, Emoji> EmojiDict { get; private set; } = new Dictionary<string, Emoji>();

        public ObservableCollection<KeyValuePair<string, Emoji>> FilteredEmojis { get; private set; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        }
        public enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }
        public enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;


        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);

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
                Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
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
            FilteredEmojis = new ObservableCollection<KeyValuePair<string, Emoji>>();
            DataContext = this;
            searchBox.Focus();

            Loaded += async (s, e) =>
            {
                WindowInteropHelper windowHelper = new WindowInteropHelper(this);
                MARGINS margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
                DwmExtendFrameIntoClientArea(windowHelper.Handle, ref margins);
                EnableBlur();
                var hwnd = new WindowInteropHelper(this).Handle;

                // Dark title bar
                int dark = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int));

                // Or set a specific caption color (COLORREF = 0x00BBGGRR)
                int color = 0x00202020; // dark gray
                DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref color, sizeof(int));

                await Task.Run(() =>
                {
                    EmojiDict = JsonConvert.DeserializeObject<Dictionary<string, Emoji>>(resources.dataByEmoji)!;
                    Emojis = new List<KeyValuePair<string, Emoji>>(EmojiDict.ToList());
                });

                foreach (KeyValuePair<string, Emoji> kvp in Emojis)
                {
                    FilteredEmojis.Add(kvp);
                }

                ForceForeground();
                this.Topmost = true;
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
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
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source.AddHook(WndProc);
        }

        private const int WM_ERASEBKGND = 0x0014;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_ERASEBKGND)
            {
                handled = true;
                return new IntPtr(1);
            }
            return IntPtr.Zero;
        }

        private void emojiView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (emojiView.SelectedItem is KeyValuePair<string, Emoji> kvp)
            {
                InsertEmoji(kvp.Key);
                this.Close();
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape) this.Close();
            if (e.Key == Key.Enter)
            {
                if (emojiView.Items.Count == 0) return;
                if (emojiView.Items[0] is KeyValuePair<string, Emoji> kvp)
                {
                    InsertEmoji(kvp.Key);
                    this.Close();
                }
            }
        }
    }
}
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

        public MainWindow()
        {
            InitializeComponent();
            _previousWindow = GetForegroundWindow();
            FilteredEmojis = new ObservableCollection<KeyValuePair<string, Emoji>>();
            DataContext = this;
            searchBox.Focus();

            Loaded += async (s, e) =>
            {
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
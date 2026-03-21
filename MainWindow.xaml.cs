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

        public IReadOnlyList<KeyValuePair<string, Emoji>> Emojis { get; private set; }
        public IReadOnlyDictionary<string, Emoji> EmojiDict { get; private set; }

        public ObservableCollection<KeyValuePair<string, Emoji>> FilteredEmojis { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            _previousWindow = GetForegroundWindow();
            EmojiDict = JsonConvert.DeserializeObject<Dictionary<string, Emoji>>(resources.dataByEmoji)!;
            Emojis = new List<KeyValuePair<string, Emoji>>(EmojiDict.ToList());
            FilteredEmojis = new ObservableCollection<KeyValuePair<string, Emoji>>(Emojis);
            DataContext = this;
            if (Emojis == null || EmojiDict == null)
                throw new InvalidOperationException("Error loading resources");

            Loaded += (s, e) =>
            {
                ForceForeground();
                searchBox.Focus();
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

            foreach (var kvp in string.IsNullOrWhiteSpace(searchBox.Text)
                     ? Emojis
                     : Emojis.Where(kvp => kvp.Value.Name.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase)))
            {
                FilteredEmojis.Add(kvp);
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
        }
    }
}
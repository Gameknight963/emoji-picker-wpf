using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


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
            searchBox.Focus();
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
    }
}
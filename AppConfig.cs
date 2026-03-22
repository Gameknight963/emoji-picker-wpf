using IniParser;
using IniParser.Model;
using System.IO;

namespace emoji_picker_wpf
{
    public enum BackdropMode
    {
        None,
        Acrylic,
        Extend
    }

    public class AppConfig
    {

        public BackdropMode BackdropMode { get; set; } = BackdropMode.Acrylic;

        public static AppConfig Load()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            if (!File.Exists(path)) return new AppConfig();
            try
            {
                var parser = new FileIniDataParser();
                IniData data = parser.ReadFile(path);
                Enum.TryParse(data["Settings"]["BackdropMode"], out BackdropMode mode);
                return new AppConfig { BackdropMode = mode };
            }
            catch { return new AppConfig(); }
        }
    }
}

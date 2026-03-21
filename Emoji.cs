using Newtonsoft.Json;

namespace emoji_picker_wpf
{
    public class Emoji
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";
        [JsonProperty("slug")]
        public string Slug { get; set; } = "";
        [JsonProperty("group")]
        public string Group { get; set; } = "";
        [JsonProperty("emoji_version")]
        public string EmojiVersion { get; set; } = "";
        [JsonProperty("unicode_version")]
        public string UnicodeVersion { get; set; } = "";
        [JsonProperty("skin_tone_support")]
        public bool SkinToneSupport { get; set; }
    }
}

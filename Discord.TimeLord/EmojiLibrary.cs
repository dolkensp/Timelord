using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord.TimeLord
{
    public class EmojiLibrary : Emoji
    {
        public EmojiLibrary(String shortcode) : base(EmojiOne.EmojiOne.ShortnameToUnicode(shortcode)) { }
        public EmojiLibrary(String shortcode, params String[] timezones) : base(EmojiOne.EmojiOne.ShortnameToUnicode(shortcode))
        {
            this.TimeZones = timezones;
            this.PrimaryZones = timezones;
        }

        public String[] TimeZones { get; set; } = new String[] { };
        public String[] PrimaryZones { get; set; } = new String[] { };
        public String[] SecondaryZones { get; set; } = new String[] { };

        public static EmojiLibrary DELETE = new EmojiLibrary(":x:");

        public static EmojiLibrary FLAG_AU = new EmojiLibrary(":flag_au:", "AEST", "AEDT", "ACST", "ACDT", "AWST", "AWDT")
        {
            PrimaryZones = new[] { "AEST", "ACST", "AWST" },
            SecondaryZones = new[] { "AEDT", "ACDT", "AWDT" },
        };
        public static EmojiLibrary FLAG_HK = new EmojiLibrary(":flag_hk:", "HKT");
        public static EmojiLibrary FLAG_US = new EmojiLibrary(":flag_us:", "CST", "EST", "MST", "PST", "PT", "MT", "CT", "ET", "MDT", "PDT", "EDT", "CDT")
        {
            PrimaryZones = new[] { "PT", "MT", "CT", "ET" },
        };
        public static EmojiLibrary FLAG_GB = new EmojiLibrary(":flag_gb:", "GMT", "BST", "UTC")
        {
            PrimaryZones = new[] { "GMT" },
            SecondaryZones = new[] { "BST" },
        };
        public static EmojiLibrary FLAG_EU = new EmojiLibrary(":flag_eu:", "GMT", "CET", "EET", "BST", "CEST", "EEST")
        {
            PrimaryZones = new[] { "CET", "EET", "GMT" },
            SecondaryZones = new[] { "CEST", "EEST", "BST" },
        };
        public static EmojiLibrary FLAG_DE = new EmojiLibrary(":flag_de:", "CET");
        public static EmojiLibrary FLAG_FR = new EmojiLibrary(":flag_fr:", "CET");
        public static EmojiLibrary FLAG_IE = new EmojiLibrary(":flag_ie:", "IST");
    }
}

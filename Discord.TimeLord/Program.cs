using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.TimeLord
{
    [Flags]
    public enum BotPermission
    {
        /// <summary>
        /// Allows creation of instant invites
        /// </summary>
        CREATE_INSTANT_INVITE = 0x00000001,
        /// <summary>
        /// Allows kicking members
        /// </summary>
        KICK_MEMBERS = 0x00000002,
        /// <summary>
        /// Allows banning members
        /// </summary>
        BAN_MEMBERS = 0x00000004,
        /// <summary>
        /// Allows all permissions and bypasses channel permission overwrites
        /// </summary>
        ADMINISTRATOR = 0x00000008,
        /// <summary>
        /// Allows management and editing of channels
        /// </summary>
        MANAGE_CHANNELS = 0x00000010,
        /// <summary>
        /// Allows management and editing of the guild
        /// </summary>
        MANAGE_GUILD = 0x00000020,
        /// <summary>
        /// Allows for the addition of reactions to messages
        /// </summary>
        ADD_REACTIONS = 0x00000040,
        /// <summary>
        /// Allows for viewing of audit logs
        /// </summary>
        VIEW_AUDIT_LOG = 0x00000080,
        /// <summary>
        /// Allows reading messages in a channel. The channel will not appear for users without this permission
        /// </summary>
        READ_MESSAGES = 0x00000400,
        /// <summary>
        /// Allows for sending messages in a channel
        /// </summary>
        SEND_MESSAGES = 0x00000800,
        /// <summary>
        /// Allows for sending of /tts messages
        /// </summary>
        SEND_TTS_MESSAGES = 0x00001000,
        /// <summary>
        /// Allows for deletion of other users messages
        /// </summary>
        MANAGE_MESSAGES = 0x00002000,
        /// <summary>
        /// Links sent by this user will be auto-embedded
        /// </summary>
        EMBED_LINKS = 0x00004000,
        /// <summary>
        /// Allows for uploading images and files
        /// </summary>
        ATTACH_FILES = 0x00008000,
        /// <summary>
        /// Allows for reading of message history
        /// </summary>
        READ_MESSAGE_HISTORY = 0x00010000,
        /// <summary>
        /// Allows for using the @everyone tag to notify all users in a channel, and the @here tag to notify all online users in a channel
        /// </summary>
        MENTION_EVERYONE = 0x00020000,
        /// <summary>
        /// Allows the usage of custom emojis from other servers
        /// </summary>
        USE_EXTERNAL_EMOJIS = 0x00040000,
        /// <summary>
        /// Allows for joining of a voice channel
        /// </summary>
        CONNECT = 0x00100000,
        /// <summary>
        /// Allows for speaking in a voice channel
        /// </summary>
        SPEAK = 0x00200000,
        /// <summary>
        /// Allows for muting members in a voice channel
        /// </summary>
        MUTE_MEMBERS = 0x00400000,
        /// <summary>
        /// Allows for deafening of members in a voice channel
        /// </summary>
        DEAFEN_MEMBERS = 0x00800000,
        /// <summary>
        /// Allows for moving of members between voice channels
        /// </summary>
        MOVE_MEMBERS = 0x01000000,
        /// <summary>
        /// Allows for using voice-activity-detection in a voice channel
        /// </summary>
        USE_VAD = 0x02000000,
        /// <summary>
        /// Allows for modification of own nickname
        /// </summary>
        CHANGE_NICKNAME = 0x04000000,
        /// <summary>
        /// Allows for modification of other users nicknames
        /// </summary>
        MANAGE_NICKNAMES = 0x08000000,
        /// <summary>
        /// Allows management and editing of roles
        /// </summary>
        MANAGE_ROLES = 0x10000000,
        /// <summary>
        /// Allows management and editing of webhooks
        /// </summary>
        MANAGE_WEBHOOKS = 0x20000000,
        /// <summary>
        /// Allows management and editing of emojis
        /// </summary>
        MANAGE_EMOJIS = 0x40000000,
    }

    class Program
    {
        /// <summary>
        /// Primary Supplied Timezones
        /// </summary>
        public static Emoji[] PRIMARY_ZONES   = new[] { EmojiLibrary.FLAG_US, EmojiLibrary.FLAG_EU };
        /// <summary>
        /// Secondary Timezone Suggestions
        /// </summary>
        public static Emoji[] SECONDARY_ZONES = new[] { EmojiLibrary.FLAG_AU, EmojiLibrary.FLAG_HK };

        static void Main(String[] args) => new Program().RunAsync(args).GetAwaiter().GetResult();

        private DiscordSocketClient _discord;
        private SocketGuild _socketGuild;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource { };

        public String CreateInviteUrl(UInt64 clientId, BotPermission permissions)
        {
            return $"https://discordapp.com/api/oauth2/authorize?client_id={clientId}&scope=bot&permissions={(Int32)permissions}";
        }

        public async Task RunAsync(String[] args)
        {
            var countryCodes = TimeZoneNames.TZNames.GetCountryNames("en-GB");
            foreach (var countryCode in countryCodes.Keys)
            {
                var shortcode = $":flag_{countryCode.ToLowerInvariant()}:";
                var emojicode = EmojiOne.EmojiOne.ShortnameToUnicode(shortcode);

                if (emojicode != shortcode)
                {
                    var timezoneNames = TimeZoneNames.TZNames.GetTimeZoneIdsForCountry(countryCode, DateTimeOffset.UtcNow);

                    foreach (var timezone in timezoneNames)
                    {
                        var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                        var abbreviations = TimeZoneNames.TZNames.GetAbbreviationsForTimeZone(timezone, "en-GB");
                    }
                }
            }

            using (this._discord = new DiscordSocketClient { })
            {
                this._discord.Connected += Discord_OnConnectedAsync;
                this._discord.MessageReceived += Discord_OnMessageReceivedAsync;
                this._discord.ReactionAdded += Discord_OnReactionAddedAsync;

                await this._discord.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["Token.Bot"]);

                await this._discord.StartAsync();

                this._tokenSource.Token.WaitHandle.WaitOne();
            }
        }

        private async Task Discord_OnReactionAddedAsync(Cacheable<IUserMessage, UInt64> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await cachedMessage.GetOrDownloadAsync();

            if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

            if (reaction.Emote.Name == EmojiLibrary.DELETE.Name)
            {
                if (message.Author.Id == this._discord.CurrentUser.Id)
                {
                    await message.DeleteAsync();
                }
            }

            if (message.Author.Id == this._discord.CurrentUser.Id)
            {
                var shortcode = EmojiOne.EmojiOne.ToShort(reaction.Emote.Name);
                var timeZones = TimeZoneNames.TZNames.GetTimeZoneIdsForCountry(shortcode.Substring(6, 2), DateTimeOffset.UtcNow);
                var zones = SECONDARY_ZONES.Where(z => reaction.Emote.Name == z.Name);

                await message.ModifyAsync((Action<MessageProperties>)((mp) =>
                {
                    var embed = message.Embeds.FirstOrDefault();
                    var author = embed.Author.Value;
                    var footer = embed.Footer.Value;

                    var fields = new List<EmbedFieldBuilder>(embed.Fields.Select(f => new EmbedFieldBuilder { Name = f.Name, Value = f.Value, IsInline = f.Inline }));

                    zones = Enumerable.Where<Emoji>(zones, (Func<Emoji, bool>)(z => (bool)!fields.Where((Func<EmbedFieldBuilder, bool>)(f => (bool)(f.Name == z.Name))).Any()));

                    foreach (var zone in zones)
                    {
                        fields.Add(new EmbedFieldBuilder { Name = $"{zone.Name}", Value = "1700", IsInline = false });
                    }

                    mp.Embed = new Optional<Embed>(new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = author.IconUrl,
                            Name = author.Name,
                            Url = author.Url,
                        },
                        Color = embed.Color,
                        Description = embed.Description,
                        Fields = fields,
                        Footer = new EmbedFooterBuilder
                        {
                            IconUrl = footer.IconUrl,
                            Text = footer.Text,
                        },
                        Timestamp = embed.Timestamp,
                        Title = embed.Title,
                        Url = embed.Url,
                    });
                }));
            }
            else
            {
                // TODO: New Embed
            }

            Console.WriteLine(reaction.Emote.Name);
        }

        private IEnumerable<DateTime> ExtractDates(String content, TimeZoneInfo defaultTimeZone)
        {
            if (false)
            {
                yield return DateTime.Now;
            }
        }

        private Regex LongTime = new Regex(@"\b([0-2]?[0-9]):?([0-5][0-9])\b(?:|[a-z]{3-4})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private Regex ShortTime = new Regex(@"\b(1?[0-9]) ?([ap]m)\b(?:|[a-z]{3-4})", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private IEnumerable<TimeSpan> ExtractTimes(String content, TimeZoneInfo defaultTimeZone)
        {
            var match = this.LongTime.Match(content);

            if (match.Success)
            {
                foreach (Match capture in match.Captures)
                {
                    yield return TimeSpan.Parse($"{capture.Groups[1].Value}:{capture.Groups[2].Value}");
                }
            }

            match = this.ShortTime.Match(content);

            if (match.Success)
            {
                foreach (Match capture in match.Captures)
                {
                    var timeZone = this.ExtractTimeZone(capture.Groups[3].Value, defaultTimeZone);
                    var hour = Int32.Parse(capture.Groups[1].Value);
                    
                    if ((capture.Groups[2].Value.ToLowerInvariant() == "pm") && (hour < 12)) hour += 12;

                    var timeSpan = TimeSpan.Parse($"{hour}:00");

                    yield return TimeZoneInfo.ConvertTimeToUtc(DateTime.Today + timeSpan, timeZone).TimeOfDay;
                }
            }
        }

        private Regex ShortRegion = new Regex(@"\b\([A-Z]\)\b", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        private TimeZoneInfo ExtractTimeZone(String content, TimeZoneInfo defaultTimeZone)
        {
            return TimeZoneInfo.Local;
        }

        private async Task Discord_OnMessageReceivedAsync(SocketMessage arg)
        {
            if (arg.Author.IsBot) return;
            if (arg.Author.Id == this._discord.CurrentUser.Id) return;
            if (!arg.Content.Contains("GMT")) return;
            // if (arg.Channel.Id != 266427401305587712UL) return;

            var message = arg as IUserMessage;
            var author = arg as IGuildUser;

            var nickname = author.Nickname ?? author.Username;
            if (String.IsNullOrWhiteSpace(nickname)) nickname = author.Username;

            var defaultTimeZone = this.ExtractTimeZone(nickname, TimeZoneInfo.Utc);

            var mentionedDates = this.ExtractDates(arg.Content, TimeZoneInfo.Utc).Distinct().ToArray();
            var mentionedTimes = this.ExtractTimes(arg.Content, TimeZoneInfo.Utc).Distinct().ToArray();

            if (mentionedDates.Any() || mentionedTimes.Any())
            {
                if (arg.MentionedUsers.Where(u => u.Id == this._discord.CurrentUser.Id).Any())
                {
                    var fields = new List<EmbedFieldBuilder> { };

                    foreach (var zone in PRIMARY_ZONES)
                    {
                        fields.Add(new EmbedFieldBuilder { Name = $"{zone.Name}", Value = "1700" });
                    }

                    var response = await arg.Channel.SendMessageAsync(String.Empty, false, new EmbedBuilder
                    {
                        Author = new EmbedAuthorBuilder
                        {
                            IconUrl = arg.Author.GetAvatarUrl(ImageFormat.Auto, 128),
                            Name = (arg.Author as SocketGuildUser)?.Nickname ?? arg.Author.Username,
                        },
                        Description = message.Content,
                        Color = new HSLColor
                        {
                            Hue = (arg.Author.Id) % 240,
                            Saturation = ((arg.Author.Id / 240f) % 30) * 2 + 90,
                            Luminosity = ((arg.Author.Id / 7200f) % 30) * 2 + 90,
                        },
                        Timestamp = DateTime.Now.AddDays(5).AddHours(5),
                        Footer = new EmbedFooterBuilder
                        {
                            Text = $"{message.Id}",
                        },
                        Fields = fields,
                    });

                    await message.DeleteAsync();

                    await response.AddReactionAsync(EmojiLibrary.DELETE);

                    foreach (var zone in SECONDARY_ZONES)
                    {
                        await response.AddReactionAsync(zone);
                    }
                }
                else
                {
                    foreach (var zone in PRIMARY_ZONES)
                    {
                        await message.AddReactionAsync(zone);
                    }
                }
            }
        }

        private async Task Discord_OnConnectedAsync()
        {
            // this._socketGuild = this._discord.GetGuild(UInt64.Parse(ConfigurationManager.AppSettings["Target.GuildId"]));

            await Task.CompletedTask;
        }
    }

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

    //public class Timezone
    //{
    //    public Emoji Emoji { get; private set; }
    //    public String ShortName { get; private set; }
    //    public String LongName { get; private set; }
        
    //    public static Timezone AEST = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "AEST", LongName = "Australian Eastern Standard Time", IsDaylightSavingTime = false };
    //    public static Timezone AEDT = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "AEDT", LongName = "Australian Eastern Daylight Time", IsDaylightSavingTime = true };
    //    // public static Timezone ACST = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "ACST", LongName = "Australian Central Standard Time" };
    //    // public static Timezone ACDT = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "ACDT", LongName = "Australian Central Daylight Time" };
    //    // public static Timezone AWST = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "AWST", LongName = "Australian Western Standard Time" };
    //    // public static Timezone AWDT = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "AWDT", LongName = "Australian Western Daylight Time" };
    //    // public static Timezone AET = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "AET", LongName = "Australian Eastern Time" };
    //    // public static Timezone ACT = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "ACT", LongName = "Australian Central Time" };
    //    // public static Timezone AWT = new Timezone { Emoji = EmojiLibrary.FLAG_AU, ShortName = "AWT", LongName = "Australian Western Time" };

    //    public static Timezone HKT = new Timezone { Emoji = EmojiLibrary.FLAG_HK, ShortName = "HKT" };

    //    public static Timezone BST = new Timezone { Emoji = EmojiLibrary.FLAG_GB, ShortName = "BST", LongName = "British Summer Time" };
    //    public static Timezone GMT = new Timezone { Emoji = EmojiLibrary.FLAG_GB, ShortName = "GMT", LongName = "Greenwich Mean Time" };
    //    public static Timezone IST = new Timezone { Emoji = EmojiLibrary.FLAG_IE, ShortName = "IST", LongName = "Irish Standard Time" };

    //    public static Timezone CET  = new Timezone { Emoji = EmojiLibrary.FLAG_DE, ShortName = "CET",  LongName = "Central European Summer Time" };
    //    public static Timezone CEST = new Timezone { Emoji = EmojiLibrary.FLAG_DE, ShortName = "CEST", LongName = "Central European Time" };
    //    public static Timezone CET =  new Timezone { Emoji = EmojiLibrary.FLAG_FR, ShortName = "CET", LongName = "Central European Summer Time" };
    //    public static Timezone CEST = new Timezone { Emoji = EmojiLibrary.FLAG_FR, ShortName = "CEST", LongName = "Central European Time" };
    //}
}
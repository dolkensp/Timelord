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

        private Regex _longTime = new Regex(@"\b([0-2]?[0-9]):?([0-5][0-9])\b(?:|[a-z]{3-4})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private Regex _shortTime = new Regex(@"\b(1?[0-9]) ?([ap]m)\b(?:|[a-z]{3-4})", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        private Regex _shortRegion = new Regex(@"\b\([A-Z]\)\b", RegexOptions.Multiline | RegexOptions.IgnoreCase);

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

        private IEnumerable<TimeSpan> ExtractTimes(String content, TimeZoneInfo defaultTimeZone)
        {
            var match = this._longTime.Match(content);

            if (match.Success)
            {
                foreach (Match capture in match.Captures)
                {
                    yield return TimeSpan.Parse($"{capture.Groups[1].Value}:{capture.Groups[2].Value}");
                }
            }

            match = this._shortTime.Match(content);

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
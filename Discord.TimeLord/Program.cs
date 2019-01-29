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
      public static Dictionary<string, TimeZoneWrapper> PrimaryTimezones = new Dictionary<string, TimeZoneWrapper>()
      {
         { "AUS Eastern Standard Time", new TimeZoneWrapper(EmojiLibrary.FLAG_AU, "AUS Eastern Standard Time") },
         { "Pacific Standard Time", new TimeZoneWrapper(EmojiLibrary.FLAG_US, "Pacific Standard Time") },
         { "Central European Standard Time", new TimeZoneWrapper(EmojiLibrary.FLAG_EU, "Central European Standard Time") }
      };

      public static Dictionary<string, TimeZoneWrapper> SecondaryTimezones = new Dictionary<string, TimeZoneWrapper>()
      {
      };

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
                  // var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                  // var abbreviations = TimeZoneNames.TZNames.GetAbbreviationsForTimeZone(timezone, "en-GB");
               }
            }
         }

         using (this._discord = new DiscordSocketClient { })
         {
            this._discord.Connected += Discord_OnConnectedAsync;
            this._discord.MessageReceived += Discord_OnMessageReceivedAsync;
            this._discord.ReactionAdded += Discord_OnReactionAddedAsync;
            this._discord.ReactionsCleared += Discord_OnReactionsClearedAsync;
            this._discord.ReactionRemoved += Discord_OnReactionRemovedAsync;

            await this._discord.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["Token.Bot"]);

            await this._discord.StartAsync();

            this._tokenSource.Token.WaitHandle.WaitOne();
         }
      }

      private async Task Discord_OnReactionRemovedAsync(Cacheable<IUserMessage, UInt64> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
      {
         // Ignore bots
         if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

         var message = await cachedMessage.GetOrDownloadAsync();

         if (message.Author.Id == this._discord.CurrentUser.Id)
         {

         }

         await Task.CompletedTask;
      }

      private async Task Discord_OnReactionsClearedAsync(Cacheable<IUserMessage, UInt64> cachedMessage, ISocketMessageChannel channel)
      {
         await Task.CompletedTask;
      }

      private async Task Discord_OnReactionAddedAsync(Cacheable<IUserMessage, UInt64> cachedMessage, ISocketMessageChannel channel, SocketReaction reaction)
      {
         if (!reaction.User.IsSpecified) return;

         // Ignore bots
         if (reaction.User.IsSpecified && reaction.User.Value.IsBot) return;

         var message = await cachedMessage.GetOrDownloadAsync();

         if (reaction.Emote.Name == EmojiLibrary.DELETE.Name)
         {
            if (message.Author.Id == this._discord.CurrentUser.Id)
            {
               await message.DeleteAsync();
            }
         }
         else if (message.Author.Id == this._discord.CurrentUser.Id)
         {
            var shortcode = EmojiOne.EmojiOne.ToShort(reaction.Emote.Name);
            var timeZones = TimeZoneNames.TZNames.GetTimeZoneIdsForCountry(shortcode.Substring(6, 2), DateTimeOffset.UtcNow);
            var zones = SecondaryTimezones.Where(z => reaction.Emote.Name == z.Value.Emoji.Name);

            await message.ModifyAsync((mp) =>
            {
               var embed = message.Embeds.FirstOrDefault();
               var embedAuthor = embed.Author.Value;
               var embedFooter = embed.Footer.Value;

               var fields = new List<EmbedFieldBuilder>(embed.Fields.Select(f => new EmbedFieldBuilder { Name = f.Name, Value = f.Value, IsInline = f.Inline }));

               zones = zones.Where(z => !fields.Where(f => f.Name == z.Value.Emoji.Name).Any());

               foreach (var zone in zones)
               {
                  fields.Add(new EmbedFieldBuilder { Name = $"{zone.Value.Emoji.Name}", Value = ConvertToTime(message, zone.Value.Info), IsInline = false });
               }

               mp.Embed = new Optional<Embed>(new EmbedBuilder
               {
                  Author = new EmbedAuthorBuilder
                  {
                     IconUrl = embedAuthor.IconUrl,
                     Name = embedAuthor.Name,
                     Url = embedAuthor.Url,
                  },
                  Color = embed.Color,
                  Description = embed.Description,
                  Fields = fields,
                  Footer = new EmbedFooterBuilder
                  {
                     IconUrl = embedFooter.IconUrl,
                     Text = embedFooter.Text,
                  },
                  Timestamp = embed.Timestamp,
                  Title = embed.Title,
                  Url = embed.Url,
               }.Build());
            });

            await message.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
         }
         else
         {
            var author = message.Author as IGuildUser;

            var response = await this.RespondToAsync(message, author);

            await message.DeleteAsync();

            await this.ReactToAsync(response);
         }
      }

      private IEnumerable<DateTime> ExtractDates(String content, TimeZoneInfo defaultTimeZone)
      {
         if (false)
         {
            yield return DateTime.Now;
         }
      }

      private IEnumerable<DateTime> ExtractTimes(String content, TimeZoneInfo defaultTimeZone)
      {
         var match = this._longTime.Match(content);

         if (match.Success)
         {
            foreach (Match capture in match.Captures)
            {
               yield return DateTime.Parse($"{capture.Groups[1].Value}:{capture.Groups[2].Value}");
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

               yield return TimeZoneInfo.ConvertTimeToUtc(DateTime.Today + timeSpan, timeZone);
            }
         }
      }

      private TimeZoneInfo ExtractTimeZone(String content, TimeZoneInfo defaultTimeZone)
      {
         return TimeZoneInfo.Local;
      }

      private async Task Discord_OnMessageReceivedAsync(SocketMessage socketMessage)
      {
         if (socketMessage.Author.IsBot) return;
         if (socketMessage.Author.Id == this._discord.CurrentUser.Id) return;
         if (!socketMessage.Content.Contains("GMT")) return;
         // if (arg.Channel.Id != 266427401305587712UL) return;

         var message = socketMessage as IUserMessage;
         var author = socketMessage.Author as IGuildUser;

         var nickname = author.Nickname ?? author.Username;
         if (String.IsNullOrWhiteSpace(nickname)) nickname = author.Username;

         var defaultTimeZone = this.ExtractTimeZone(nickname, TimeZoneInfo.Utc);

         var mentionedDates = this.ExtractDates(socketMessage.Content, TimeZoneInfo.Utc).Distinct().ToArray();
         var mentionedTimes = this.ExtractTimes(socketMessage.Content, TimeZoneInfo.Utc).Distinct().ToArray();

         if (mentionedDates.Any() || mentionedTimes.Any())
         {
            if (socketMessage.MentionedUsers.Where(u => u.Id == this._discord.CurrentUser.Id).Any())
            {
               var response = await this.RespondToAsync(message, author);

               await message.DeleteAsync();

               await this.ReactToAsync(response);
            }
            else
            {
               foreach (var zone in PrimaryTimezones)
               {
                  await message.AddReactionAsync(zone.Value.Emoji);
               }
            }
         }
      }

      private async Task ReactToAsync(IUserMessage response)
      {
         await response.AddReactionAsync(EmojiLibrary.DELETE);

         foreach (var zone in SecondaryTimezones)
         {
            await response.AddReactionAsync(zone.Value.Emoji);
         }
      }

      private async Task<IUserMessage> RespondToAsync(IUserMessage message, IGuildUser author)
      {
         var fields = new List<EmbedFieldBuilder> { };

         foreach (var zone in PrimaryTimezones)
         {
            fields.Add(new EmbedFieldBuilder { Name = $"{zone.Value.Emoji.Name}", Value = ConvertToTime(message, zone.Value.Info).ToString("HH:MM")});
         }

         var response = await message.Channel.SendMessageAsync(String.Empty, false, new EmbedBuilder
         {
            Author = new EmbedAuthorBuilder
            {
               IconUrl = author.GetAvatarUrl(ImageFormat.Auto, 128),
               Name = author.Nickname ?? author.Username,
            },
            Description = message.Content,
            Color = new HSLColor
            {
               Hue = (author.Id) % 240,
               Saturation = ((author.Id / 240f) % 30) * 2 + 90,
               Luminosity = ((author.Id / 7200f) % 30) * 2 + 90,
            },
            Timestamp = DateTime.Now.AddDays(5).AddHours(5),
            Footer = new EmbedFooterBuilder
            {
               Text = $"{message.Id}",
            },
            Fields = fields,
         }.Build());

         return response;
      }

      private async Task Discord_OnConnectedAsync()
      {
         // this._socketGuild = this._discord.GetGuild(UInt64.Parse(ConfigurationManager.AppSettings["Target.GuildId"]));

         await Task.CompletedTask;
      }

      private DateTime ConvertToTime(IUserMessage message, TimeZoneInfo destinationTimeZone)
      {
         var sourceTimezone = this.ExtractTimeZone(message.Content, TimeZoneInfo.Utc);

         var time = this.ExtractTimes(message.Content, sourceTimezone).First();

         return TimeZoneInfo.ConvertTime(time, sourceTimezone, destinationTimeZone);
      }
   }

   public class TimeZoneWrapper
   {
      public Emoji Emoji { get; private set; }

      public TimeZoneInfo Info { get; private set; }

      public TimeZoneWrapper(Emoji emoji, String timeZoneInfoId)
      {
         Emoji = emoji;
         Info = TimeZoneInfo.FindSystemTimeZoneById(timeZoneInfoId);

      }
   }
}
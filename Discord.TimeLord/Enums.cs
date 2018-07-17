using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
}

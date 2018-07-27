using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace FishBot
{
    public class AuditLog
    {
        private DiscordSocketClient _client;

        public static string FilePath { get; } = "Logs/" + DateTime.Now.ToString("M.d.yy") + ".log";

        public void Mount(DiscordSocketClient c)
        {
            _client = c;
            _client.MessageUpdated += MessageUpdated;
            _client.MessageDeleted += MessageDeleted;

            _client.GuildMemberUpdated += UserUpdated;
            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;

            //TODO: Add support for Bans and Role Changes (Perms)
        }


        public static void EnsureExists()
        {
            if (!Directory.Exists("Logs"))
            {
                Program.Print("No Logs folder found!", ConsoleColor.DarkRed);
                Directory.CreateDirectory("Logs");
            }

            if (!File.Exists(FilePath))
            {
                Program.Print("No AuditLog file found!", ConsoleColor.DarkRed);
                File.Create(FilePath);

                Program.Print("AuditLog file created at: " + FilePath, ConsoleColor.DarkGreen);
            }
            else
                Program.Print("AuditLog file found at: " + FilePath + "!", ConsoleColor.DarkGreen);
        }

        #region Events

        public static void AddMessageEvent(SocketMessage msg)
            => File.AppendAllText(FilePath, $"{DateTime.Now} {msg.Author}: \"{msg.Content}\" {Environment.NewLine}");
        public static void AddMessageEditedEvent(SocketMessage before, SocketMessage after)
            => File.AppendAllText(FilePath, $"{after.Timestamp.DateTime} {after.Author}: \"{before.Content}\" => \"{after.Content}\" {Environment.NewLine}");
        public static void AddMessageDeletedEvent(SocketMessage msg)
            => File.AppendAllText(FilePath, $"{msg.Timestamp.DateTime} {msg.Author}: (-) \"{msg.Content}\" {Environment.NewLine}");
        public static void AddUserNicknameUpdatedEvent(SocketGuildUser before, SocketGuildUser after)
            => File.AppendAllText(FilePath, $"{DateTime.Now} User {before.Username} changed \"{before.Nickname}\" => \"{after.Nickname}\" {Environment.NewLine}");
        public static void AddUserJoinedEvent(SocketGuildUser user)
            => File.AppendAllText(FilePath, $"{DateTime.Now} {user.Username} \"{user.Nickname}\" has joined the Guild! {Environment.NewLine}");
        public static void AddUserLeftEvent(SocketGuildUser user)
            => File.AppendAllText(FilePath, $"{DateTime.Now} {user.Username} (\"{user.Nickname}\") has left the Guild! {Environment.NewLine}");
        public static void AddCommandResultEvent(IResult res)
            => File.AppendAllText(FilePath, res.IsSuccess
                    ? $"{DateTime.Now} The previous command was successfully evaluated! {Environment.NewLine}"
                    : $"{DateTime.Now} The previous command failed with ErrorReason: \"{res.ErrorReason}\" {Environment.NewLine}");

        #endregion

        #region MessageEvents
        private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
        {
            var message = await before.GetOrDownloadAsync() as SocketMessage;

            if (message != null)
            {
                string outText = after.Author + ": \"" + message.Content + "\" => \"" + after.Content + "\"";
                await Program.Log(new LogMessage(LogSeverity.Verbose, "", outText));

                AddMessageEditedEvent(message, after);
            }

        }
        private static async Task MessageDeleted(Cacheable<IMessage, ulong> msg, ISocketMessageChannel socketMessageChannel)
        {
            var message = await msg.GetOrDownloadAsync() as SocketMessage;

            if (message != null)
            {
                string outText = message.Author + ": (-) \"" + message.Content + "\"";
                await Program.Log(new LogMessage(LogSeverity.Verbose, "", outText));

                AddMessageDeletedEvent(message);
            }
        }
        #endregion

        #region UserEvents
        private static async Task UserUpdated(SocketUser socketUser, SocketUser user)
        {
            SocketGuildUser before = socketUser as SocketGuildUser;
            SocketGuildUser after = user as SocketGuildUser;

            if (before != null && after != null)
            {
                if (before.Nickname != after.Nickname)
                {
                    string outText = "User " + before.Username + " changed \"" + before.Nickname + "\" => \"" + after.Nickname + "\"";
                    await Program.Log(new LogMessage(LogSeverity.Verbose, "", outText));

                    AddUserNicknameUpdatedEvent(before, after);
                }
            }
        }

        private static async Task UserJoined(SocketGuildUser user)
        {
            await Program.Log(new LogMessage(LogSeverity.Verbose, "", $"user.Username + (\" + {user.Nickname} \") has joined Guild"));
            AddUserJoinedEvent(user);
        }

        private static async Task UserLeft(SocketGuildUser user)
        {
            await Program.Log(new LogMessage(LogSeverity.Verbose, "", user.Username + "(\"" + user.Nickname + "\") has left the Guild"));
            AddUserJoinedEvent(user);
        }

        #endregion

    }
}
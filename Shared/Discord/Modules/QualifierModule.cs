#pragma warning disable 1998
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TournamentAssistantShared.BeatSaver;
using TournamentAssistantShared.Discord.Helpers;
using TournamentAssistantShared.Discord.Services;
using TournamentAssistantShared.Models;
using TournamentAssistantShared.Models.Packets;
using static TournamentAssistantShared.Models.GameplayModifiers;
using static TournamentAssistantShared.Models.PlayerSpecificSettings;
using static TournamentAssistantShared.SharedConstructs;

namespace TournamentAssistantShared.Discord.Modules
{
    public class QualifierModule : ModuleBase<SocketCommandContext>
    {
        private static Random random = new Random();

        public DatabaseService DatabaseService { get; set; }
        public ScoresaberService ScoresaberService { get; set; }
        public SystemServerService ServerService { get; set; }

        private bool IsAdmin()
        {
            return ((IGuildUser)Context.User).GuildPermissions.Has(GuildPermission.Administrator);
        }

        private GameplayParameters FindSong(List<GameplayParameters> songPool, string levelId, string characteristic, int beatmapDifficulty, int gameOptions, int playerOptions)
        {
            return songPool.FirstOrDefault(x => x.Beatmap.LevelId == levelId && x.Beatmap.Characteristic.SerializedName == characteristic && x.Beatmap.Difficulty == (BeatmapDifficulty)beatmapDifficulty && x.GameplayModifiers.Options == (GameOptions)gameOptions && x.PlayerSettings.Options == (PlayerOptions)playerOptions);
        }

        private List<GameplayParameters> RemoveSong(List<GameplayParameters> songPool, string levelId, string characteristic, int beatmapDifficulty, int gameOptions, int playerOptions)
        {
            songPool.RemoveAll(x => x.Beatmap.LevelId == levelId && x.Beatmap.Characteristic.SerializedName == characteristic && x.Beatmap.Difficulty == (BeatmapDifficulty)beatmapDifficulty && x.GameplayModifiers.Options == (GameOptions)gameOptions && x.PlayerSettings.Options == (PlayerOptions)playerOptions);
            return songPool;
        }

        private bool SongExists(List<GameplayParameters> songPool, string levelId, string characteristic, int beatmapDifficulty, int gameOptions, int playerOptions)
        {
            return FindSong(songPool, levelId, characteristic, beatmapDifficulty, gameOptions, playerOptions) != null;
        }

        [Command("创建赛事")]
        [Summary("在服务器中创建预选赛")]
        [RequireContext(ContextType.Guild)]
        public async Task CreateEventAsync([Remainder] string paramString)
        {
            if (IsAdmin())
            {
                var name = paramString.ParseArgs("名称");
                var hostServer = paramString.ParseArgs("服务器");
                var notificationChannelId = paramString.ParseArgs("频道");
                
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(hostServer))
                {
                    await ReplyAsync(embed: "用法: `创建赛事 -名称 \"赛事名\" -服务器 \"[服务器地址]:[端口号]\"`\n想要找到可用的服务器使用 `列出服务器` 指令\n你也可以提前加上设置项。比如在命令里添加`-对选手隐藏得分`".ErrorEmbed());
                }
                else
                {
                    var server = ServerService.GetServer();
                    if (server == null)
                    {
                        await ReplyAsync(embed: "服务器不在线，所以不能创建比赛".ErrorEmbed());
                    }
                    else
                    {
                        var host = server.State.KnownHosts.FirstOrDefault(x => $"{x.Address}:{x.Port}" == hostServer);

                        QualifierEvent.EventSettings settings = QualifierEvent.EventSettings.None;

                        //Parse any desired options
                        foreach (QualifierEvent.EventSettings o in Enum.GetValues(typeof(QualifierEvent.EventSettings)))
                        {
                            if (paramString.ParseArgs(o.ToString()) == "true") settings = (settings | o);
                        }

                        var response = await server.SendCreateQualifierEvent(host, DatabaseService.DatabaseContext.ConvertDatabaseToModel(null, new Database.Event
                        {
                            EventId = Guid.NewGuid().ToString(),
                            GuildId = Context.Guild.Id,
                            GuildName = Context.Guild.Name,
                            Name = name,
                            InfoChannelId = ulong.Parse(notificationChannelId ?? "0"),
                            Flags = (int)settings
                        }));
                        if (response.Type == Response.ResponseType.Success)
                        {
                            await ReplyAsync(embed: response.Message.SuccessEmbed());
                        }
                        else if (response.Type == Response.ResponseType.Fail)
                        {
                            await ReplyAsync(embed: response.Message.ErrorEmbed());
                        }
                    }
                }
            }
            else await ReplyAsync(embed: "你没有足够的权限使用该命令".ErrorEmbed());
        }

        [Command("计分频道")]
        [Summary("设置赛事的计分频道")]
        [RequireContext(ContextType.Guild)]
        public async Task SetScoreChannelAsync(IGuildChannel channel, [Remainder] string paramString)
        {
            if (IsAdmin())
            {
                var eventId = paramString.ParseArgs("赛事");

                if (string.IsNullOrEmpty(eventId))
                {
                    await ReplyAsync(embed: "用法: `计分频道 #频道 -赛事 \"[赛事ID]\"`\n赛事ID可以通过`赛事列表`命令查找".ErrorEmbed());
                }
                else
                {
                    var server = ServerService.GetServer();
                    if (server == null)
                    {
                        await ReplyAsync(embed: "服务器不在线，所以不能创建赛事".ErrorEmbed());
                    }
                    else
                    {
                        var knownPairs = await HostScraper.ScrapeHosts(server.State.KnownHosts, $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0);
                        var targetPair = knownPairs.FirstOrDefault(x => x.Value.Events.Any(y => y.EventId.ToString() == eventId));

                        if (targetPair.Key != null)
                        {
                            var targetEvent = targetPair.Value.Events.First(x => x.EventId.ToString() == eventId);
                            targetEvent.InfoChannel = new Models.Discord.Channel
                            {
                                Id = channel?.Id ?? 0,
                                Name = channel?.Name ?? ""
                            };

                            var response = await server.SendUpdateQualifierEvent(targetPair.Key, targetEvent);
                            if (response.Type == Response.ResponseType.Success)
                            {
                                await ReplyAsync(embed: response.Message.SuccessEmbed());
                            }
                            else if (response.Type == Response.ResponseType.Fail)
                            {
                                await ReplyAsync(embed: response.Message.ErrorEmbed());
                            }
                        }
                        else await ReplyAsync(embed: "Could not find an event with that ID".ErrorEmbed());
                    }
                }
            }
            else await ReplyAsync(embed: "你没有足够的权限使用该命令".ErrorEmbed());
        }

        [Command("添加歌曲")]
        [Summary("向赛事中添加歌曲")]
        [RequireContext(ContextType.Guild)]
        public async Task AddSongAsync([Remainder] string paramString = null)
        {
            if (IsAdmin())
            {
                var eventId = paramString.ParseArgs("赛事");
                var songId = paramString.ParseArgs("歌曲");

                if (string.IsNullOrEmpty(eventId) || string.IsNullOrEmpty(songId))
                {
                    await ReplyAsync(embed: ("用法: `添加歌曲 -赛事 \"[赛事ID]\" -歌曲 [链接/key]`\n" +
                        "赛事ID可以通过`赛事列表`命令查找\n" +
                        "可选参数: `-难度 [Easy/Normal/Hard/Expert/ExpertPlus]`, `-谱型 [例如: onesaber]`, `-[修改项]` (例如: 不死模式为 `-nofail`)").ErrorEmbed());
                    return;
                }

                //Parse the difficulty input, either as an int or a string
                BeatmapDifficulty difficulty = BeatmapDifficulty.ExpertPlus;

                string difficultyArg = paramString.ParseArgs("难度");
                if (difficultyArg != null)
                {
                    //If the enum conversion doesn't succeed, try it as an int
                    if (!Enum.TryParse(difficultyArg, true, out difficulty))
                    {
                        await ReplyAsync(embed: "请检查难度参数".ErrorEmbed());
                        return;
                    }
                }

                string characteristic = paramString.ParseArgs("谱型");
                characteristic = characteristic ?? "Standard";

                GameOptions gameOptions = GameOptions.None;
                PlayerOptions playerOptions = PlayerOptions.None;

                //Load up the GameOptions and PlayerOptions
                foreach (GameOptions o in Enum.GetValues(typeof(GameOptions)))
                {
                    if (paramString.ParseArgs(o.ToString()) == "true") gameOptions = (gameOptions | o);
                }

                foreach (PlayerOptions o in Enum.GetValues(typeof(PlayerOptions)))
                {
                    if (paramString.ParseArgs(o.ToString()) == "true") playerOptions = (playerOptions | o);
                }

                //Sanitize input
                if (songId.StartsWith("https://beatsaver.com/") || songId.StartsWith("https://bsaber.com/"))
                {
                    //Strip off the trailing slash if there is one
                    if (songId.EndsWith("/")) songId = songId.Substring(0, songId.Length - 1);

                    //Strip off the beginning of the url to leave the id
                    songId = songId.Substring(songId.LastIndexOf("/") + 1);
                }

                if (songId.Contains("&"))
                {
                    songId = songId.Substring(0, songId.IndexOf("&"));
                }

                var server = ServerService.GetServer();
                if (server == null)
                {
                    await ReplyAsync(embed: "服务器不在线，所以不能添加歌曲".ErrorEmbed());
                }
                else
                {
                    //Get the hash for the song
                    var hash = BeatSaverDownloader.GetHashFromID(songId);
                    var knownPairs = await HostScraper.ScrapeHosts(server.State.KnownHosts, $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0);
                    var targetPair = knownPairs.FirstOrDefault(x => x.Value.Events.Any(y => y.EventId.ToString() == eventId));
                    var targetEvent = targetPair.Value.Events.FirstOrDefault(x => x.EventId.ToString() == eventId);
                    var songPool = targetEvent.QualifierMaps.ToList();

                    if (OstHelper.IsOst(hash))
                    {
                        if (!SongExists(songPool, hash, characteristic, (int)difficulty, (int)gameOptions, (int)playerOptions))
                        {
                            GameplayParameters parameters = new GameplayParameters
                            {
                                Beatmap = new Beatmap
                                {
                                    Name = OstHelper.GetOstSongNameFromLevelId(hash),
                                    LevelId = hash,
                                    Characteristic = new Characteristic
                                    {
                                        SerializedName = characteristic
                                    },
                                    Difficulty = difficulty
                                },
                                GameplayModifiers = new GameplayModifiers
                                {
                                    Options = gameOptions
                                },
                                PlayerSettings = new PlayerSpecificSettings
                                {
                                    Options = playerOptions
                                }
                            };

                            songPool.Add(parameters);
                            targetEvent.QualifierMaps = songPool.ToArray();

                            var response = await server.SendUpdateQualifierEvent(targetPair.Key, targetEvent);
                            if (response.Type == Response.ResponseType.Success)
                            {
                                await ReplyAsync(embed: ($"已添加: {parameters.Beatmap.Name} ({difficulty}) ({characteristic})" +
                                    $"{(gameOptions != GameOptions.None ? $" 附加游戏参数: ({gameOptions})" : "")}" +
                                    $"{(playerOptions != PlayerOptions.None ? $" 附加玩家参数: ({playerOptions})" : "!")}").SuccessEmbed());
                            }
                            else if (response.Type == Response.ResponseType.Fail)
                            {
                                await ReplyAsync(embed: response.Message.ErrorEmbed());
                            }
                        }
                        else await ReplyAsync(embed: "歌曲已存在于数据库".ErrorEmbed());
                    }
                    else
                    {
                        var songInfo = await BeatSaverDownloader.GetSongInfo(songId);
                        string songName = songInfo.Name;

                        if (!songInfo.HasDifficulty(characteristic, difficulty))
                        {
                            BeatmapDifficulty nextBestDifficulty = songInfo.GetClosestDifficultyPreferLower(characteristic, difficulty);

                            if (SongExists(songPool, hash, characteristic, (int)nextBestDifficulty, (int)gameOptions, (int)playerOptions))
                            {
                                await ReplyAsync(embed: $"{songName} 不存在 {difficulty} 难度, 而且 {nextBestDifficulty} 已存在于赛事".ErrorEmbed());
                            }
                            else
                            {
                                GameplayParameters parameters = new GameplayParameters
                                {
                                    Beatmap = new Beatmap
                                    {
                                        Name = songName,
                                        LevelId = $"custom_level_{hash.ToUpper()}",
                                        Characteristic = new Characteristic
                                        {
                                            SerializedName = characteristic
                                        },
                                        Difficulty = nextBestDifficulty
                                    },
                                    GameplayModifiers = new GameplayModifiers
                                    {
                                        Options = gameOptions
                                    },
                                    PlayerSettings = new PlayerSpecificSettings
                                    {
                                        Options = playerOptions
                                    }
                                };

                                songPool.Add(parameters);
                                targetEvent.QualifierMaps = songPool.ToArray();

                                var response = await server.SendUpdateQualifierEvent(targetPair.Key, targetEvent);
                                if (response.Type == Response.ResponseType.Success)
                                {
                                    await ReplyAsync(embed: ($"{songName} 不存在 {difficulty} 难度, 使用 {nextBestDifficulty} 难度代替。\n" +
                                        $"已添加至歌曲列表" +
                                        $"{(gameOptions != GameOptions.None ? $" 附加游戏参数: ({gameOptions})" : "")}" +
                                        $"{(playerOptions != PlayerOptions.None ? $" 附加玩家参数: ({playerOptions})" : "!")}").SuccessEmbed());
                                }
                                else if (response.Type == Response.ResponseType.Fail)
                                {
                                    await ReplyAsync(embed: response.Message.ErrorEmbed());
                                }
                            }
                        }
                        else
                        {
                            GameplayParameters parameters = new GameplayParameters
                            {
                                Beatmap = new Beatmap
                                {
                                    Name = songName,
                                    LevelId = $"custom_level_{hash.ToUpper()}",
                                    Characteristic = new Characteristic
                                    {
                                        SerializedName = characteristic
                                    },
                                    Difficulty = difficulty
                                },
                                GameplayModifiers = new GameplayModifiers
                                {
                                    Options = gameOptions
                                },
                                PlayerSettings = new PlayerSpecificSettings
                                {
                                    Options = playerOptions
                                }
                            };

                            songPool.Add(parameters);
                            targetEvent.QualifierMaps = songPool.ToArray();

                            var response = await server.SendUpdateQualifierEvent(targetPair.Key, targetEvent);
                            if (response.Type == Response.ResponseType.Success)
                            {
                                await ReplyAsync(embed: ($"{songName} ({difficulty}) ({characteristic}) 已下载并添加至歌曲列表" +
                                    $"{(gameOptions != GameOptions.None ? $" 附加游戏参数: ({gameOptions})" : "")}" +
                                    $"{(playerOptions != PlayerOptions.None ? $" 附加玩家参数: ({playerOptions})" : "!")}").SuccessEmbed());
                            }
                            else if (response.Type == Response.ResponseType.Fail)
                            {
                                await ReplyAsync(embed: response.Message.ErrorEmbed());
                            }
                        }
                    }
                }
            }
            else await ReplyAsync(embed: "你没有足够的权限使用该命令".ErrorEmbed());
        }

        [Command("歌曲列表")]
        [Summary("显示赛事的歌曲列表")]
        [RequireContext(ContextType.Guild)]
        public async Task ListSongsAsync([Remainder] string paramString = null)
        {
            var server = ServerService.GetServer();
            if (server == null)
            {
                await ReplyAsync(embed: "服务器不在线，所以不能获取赛事信息".ErrorEmbed());
            }
            else
            {
                var eventId = paramString.ParseArgs("赛事");

                if (string.IsNullOrEmpty(eventId))
                {
                    await ReplyAsync(embed: ("用法: `歌曲列表 -赛事 \"[赛事ID]\"`\n" +
                        "赛事ID可以通过`赛事列表`命令查找\n").ErrorEmbed());
                    return;
                }

                var knownPairs = await HostScraper.ScrapeHosts(server.State.KnownHosts, $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0);
                var targetPair = knownPairs.FirstOrDefault(x => x.Value.Events.Any(y => y.EventId.ToString() == eventId));
                var targetEvent = targetPair.Value.Events.FirstOrDefault(x => x.EventId.ToString() == eventId);
                var songPool = targetEvent.QualifierMaps.ToList();

                var builder = new EmbedBuilder();
                builder.Title = "<:page_with_curl:735592941338361897> 歌曲列表";
                builder.Color = new Color(random.Next(255), random.Next(255), random.Next(255));

                var titleField = new EmbedFieldBuilder();
                titleField.Name = "曲名";
                titleField.Value = "```";
                titleField.IsInline = true;

                var difficultyField = new EmbedFieldBuilder();
                difficultyField.Name = "难度";
                difficultyField.Value = "```";
                difficultyField.IsInline = true;

                var modifierField = new EmbedFieldBuilder();
                modifierField.Name = "修改项";
                modifierField.Value = "```";
                modifierField.IsInline = true;

                foreach (var song in songPool)
                {
                    titleField.Value += $"\n{song.Beatmap.Name}";
                    difficultyField.Value += $"\n{song.Beatmap.Difficulty}";
                    modifierField.Value += $"\n{song.GameplayModifiers.Options}";
                }

                titleField.Value += "```";
                difficultyField.Value += "```";
                modifierField.Value += "```";

                builder.AddField(titleField);
                builder.AddField(difficultyField);
                builder.AddField(modifierField);

                await ReplyAsync(embed: builder.Build());
            }
        }

        [Command("删除歌曲")]
        [Summary("从赛事中删除歌曲")]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveSongAsync([Remainder] string paramString = null)
        {
            if (IsAdmin())
            {
                var server = ServerService.GetServer();
                if (server == null)
                {
                    await ReplyAsync(embed: "服务器不在线，所以不能获取赛事信息".ErrorEmbed());
                }
                else
                {
                    var eventId = paramString.ParseArgs("赛事");
                    var songId = paramString.ParseArgs("歌曲");

                    if (string.IsNullOrEmpty(eventId))
                    {
                        await ReplyAsync(embed: ("用法: `删除歌曲 -赛事 \"[赛事ID]\" -歌曲 [链接/key]`\n" +
                            "赛事ID可以通过`赛事列表`命令查找\n" +
                            "注意: 你可能需要在命令种包含难度或者修改项信息来确保你删除的是正确的歌曲").ErrorEmbed());
                        return;
                    }

                    var knownPairs = await HostScraper.ScrapeHosts(server.State.KnownHosts, $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0);
                    var targetPair = knownPairs.FirstOrDefault(x => x.Value.Events.Any(y => y.EventId.ToString() == eventId));
                    var targetEvent = targetPair.Value.Events.FirstOrDefault(x => x.EventId.ToString() == eventId);
                    var songPool = targetEvent.QualifierMaps.ToList();

                    //Parse the difficulty input, either as an int or a string
                    BeatmapDifficulty difficulty = BeatmapDifficulty.ExpertPlus;

                    string difficultyArg = paramString.ParseArgs("难度");
                    if (difficultyArg != null)
                    {
                        //If the enum conversion doesn't succeed, try it as an int
                        if (!Enum.TryParse(difficultyArg, true, out difficulty))
                        {
                            await ReplyAsync(embed: ("请检查难度参数\n" +
                            "用法: `删除歌曲 [歌曲ID] [难度]`").ErrorEmbed());
                            return;
                        }
                    }

                    string characteristic = paramString.ParseArgs("谱型");
                    characteristic = characteristic ?? "Standard";

                    GameOptions gameOptions = GameOptions.None;
                    PlayerOptions playerOptions = PlayerOptions.None;

                    //Load up the GameOptions and PlayerOptions
                    foreach (GameOptions o in Enum.GetValues(typeof(GameOptions)))
                    {
                        if (paramString.ParseArgs(o.ToString()) == "true") gameOptions = (gameOptions | o);
                    }

                    foreach (PlayerOptions o in Enum.GetValues(typeof(PlayerOptions)))
                    {
                        if (paramString.ParseArgs(o.ToString()) == "true") playerOptions = (playerOptions | o);
                    }

                    //Sanitize input
                    if (songId.StartsWith("https://beatsaver.com/") || songId.StartsWith("https://bsaber.com/"))
                    {
                        //Strip off the trailing slash if there is one
                        if (songId.EndsWith("/")) songId = songId.Substring(0, songId.Length - 1);

                        //Strip off the beginning of the url to leave the id
                        songId = songId.Substring(songId.LastIndexOf("/") + 1);
                    }

                    if (songId.Contains("&"))
                    {
                        songId = songId.Substring(0, songId.IndexOf("&"));
                    }

                    //Get the hash for the song
                    var hash = BeatSaverDownloader.GetHashFromID(songId);

                    var song = FindSong(songPool, $"custom_level_{hash.ToUpper()}", characteristic, (int)difficulty, (int)gameOptions, (int)playerOptions);
                    if (song != null)
                    {
                        targetEvent.QualifierMaps = RemoveSong(songPool, $"custom_level_{hash.ToUpper()}", characteristic, (int)difficulty, (int)gameOptions, (int)playerOptions).ToArray();

                        var response = await server.SendUpdateQualifierEvent(targetPair.Key, targetEvent);
                        if (response.Type == Response.ResponseType.Success)
                        {
                            await ReplyAsync(embed: ($"已从歌曲列表中删除 {song.Beatmap.Name} ({difficulty}) ({characteristic}) " +
                                $"{(gameOptions != GameOptions.None ? $" 附加游戏参数: ({gameOptions})" : "")}" +
                                $"{(playerOptions != PlayerOptions.None ? $" 附加玩家参数: ({playerOptions})" : "!")}").SuccessEmbed());
                        }
                        else if (response.Type == Response.ResponseType.Fail)
                        {
                            await ReplyAsync(embed: response.Message.ErrorEmbed());
                        }
                    }
                    else await ReplyAsync(embed: $"指定歌曲没有相对应 难度/谱型/游戏选项/玩家选项 ({difficulty} {characteristic} {gameOptions} {playerOptions})".ErrorEmbed());
                }                
            }
        }

        [Command("结束赛事")]
        [Summary("结束赛事")]
        [RequireContext(ContextType.Guild)]
        public async Task EndEventAsync([Remainder] string paramString = null)
        {
            if (IsAdmin())
            {
                //Make server backup
                /*Logger.Warning($"BACKING UP DATABASE...");
                File.Copy("BotDatabase.db", $"EventDatabase_bak_{DateTime.Now.Day}_{DateTime.Now.Hour}_{DateTime.Now.Minute}_{DateTime.Now.Second}.db");
                Logger.Success("Database backed up succsessfully.");*/

                var server = ServerService.GetServer();
                if (server == null)
                {
                    await ReplyAsync(embed: "服务器不在线，所以不能结束赛事信息".ErrorEmbed());
                }
                else
                {
                    var eventId = paramString.ParseArgs("赛事");

                    if (string.IsNullOrEmpty(eventId))
                    {
                        await ReplyAsync(embed: ("用法: `结束赛事 -赛事 \"[赛事ID]\"`\n" +
                            "赛事ID可以通过`赛事列表`命令查找").ErrorEmbed());
                        return;
                    }

                    var knownPairs = await HostScraper.ScrapeHosts(server.State.KnownHosts, $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0);
                    var targetPair = knownPairs.FirstOrDefault(x => x.Value.Events.Any(y => y.EventId.ToString() == eventId));
                    var targetEvent = targetPair.Value.Events.FirstOrDefault(x => x.EventId.ToString() == eventId);

                    var response = await server.SendDeleteQualifierEvent(targetPair.Key, targetEvent);
                    if (response.Type == Response.ResponseType.Success)
                    {
                        await ReplyAsync(embed: response.Message.SuccessEmbed());
                    }
                    else if (response.Type == Response.ResponseType.Fail)
                    {
                        await ReplyAsync(embed: response.Message.ErrorEmbed());
                    }
                }
            }
            else await ReplyAsync(embed: "你没有足够的权限使用该命令".ErrorEmbed());
        }

        [Command("列出赛事")]
        [Summary("列出所有赛事")]
        [RequireContext(ContextType.Guild)]
        public async Task ListEventsAsync()
        {
            if (IsAdmin())
            {
                var server = ServerService.GetServer();
                if (server == null)
                {
                    await ReplyAsync(embed: "服务器不在线，所以不能获取赛事信息".ErrorEmbed());
                }
                else
                {
                    var knownEvents = (await HostScraper.ScrapeHosts(server.State.KnownHosts, $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0)).Select(x => x.Value).Where(x => x.Events != null).SelectMany(x => x.Events);

                    var builder = new EmbedBuilder();
                    builder.Title = "<:page_with_curl:735592941338361897> 赛事";
                    builder.Color = new Color(random.Next(255), random.Next(255), random.Next(255));

                    foreach (var @event in knownEvents)
                    {
                        builder.AddField(@event.Name, $"```fix\n{@event.EventId}```\n" +
                            $"```css\n({@event.Guild.Name})```", true);
                    }

                    await ReplyAsync(embed: builder.Build());
                }
            }
            else await ReplyAsync(embed: "你没有足够的权限使用该命令".ErrorEmbed());
        }

        [Command("列出服务器")]
        [Summary("列出所有服务器")]
        [RequireContext(ContextType.Guild)]
        public async Task ListHostsAsync()
        {
            if (IsAdmin())
            {
                var server = ServerService.GetServer();
                if (server == null)
                {
                    await ReplyAsync(embed: "服务器不在线，所以不能获取服务器信息".ErrorEmbed());
                }
                else
                {
                    var builder = new EmbedBuilder();
                    builder.Title = "<:page_with_curl:735592941338361897> 服务器";
                    builder.Color = new Color(random.Next(255), random.Next(255), random.Next(255));

                    foreach (var host in server.State.KnownHosts)
                    {
                        builder.AddField(host.Name, $"```\n{host.Address}:{host.Port}```", true);
                    }

                    await ReplyAsync(embed: builder.Build());
                }
            }
            else await ReplyAsync(embed: "你没有足够的权限使用该命令".ErrorEmbed());
        }

        [Command("排行榜")]
        [Alias("排行榜")]
        [Summary("显示赛事排行榜")]
        [RequireContext(ContextType.Guild)]
        public async Task LeaderboardsAsync([Remainder] string paramString)
        {
            if (IsAdmin())
            {
                var server = ServerService.GetServer();
                if (server == null)
                {
                    await ReplyAsync(embed: "服务器不在线，所以不能获取服务器信息".ErrorEmbed());
                }
                else
                {
                    var eventId = paramString.ParseArgs("赛事");
                    var knownPairs = await HostScraper.ScrapeHosts(server.State.KnownHosts, $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0);
                    var targetPair = knownPairs.FirstOrDefault(x => x.Value.Events.Any(y => y.EventId.ToString() == eventId));
                    var targetEvent = targetPair.Value.Events.FirstOrDefault(x => x.EventId.ToString() == eventId);

                    var builder = new EmbedBuilder();
                    builder.Title = "<:page_with_curl:735592941338361897> 排行榜";
                    builder.Color = new Color(random.Next(255), random.Next(255), random.Next(255));

                    var playerNames = new List<string>();
                    var playerScores = new List<string>();

                    foreach (var map in targetEvent.QualifierMaps)
                    {
                        var scores = (await HostScraper.RequestResponse(targetPair.Key, new Packet(new ScoreRequest
                        {
                            EventId = Guid.Parse(eventId),
                            Parameters = map
                        }), typeof(ScoreRequestResponse), $"{server.CoreServer.Address}:{server.CoreServer.Port}", 0)).SpecificPacket as ScoreRequestResponse;

                        builder.AddField(map.Beatmap.Name, $"```\n{string.Join("\n", scores.Scores.Select(x => $"{x.Username} {x._Score} {(x.FullCombo ? "FC" : "")}\n"))}```", true);
                    }

                    await ReplyAsync(embed: builder.Build());
                }
            }
            else await ReplyAsync(embed: "你没有足够的权限使用该命令".ErrorEmbed());
        }
    }
}

#pragma warning disable 1998
using TournamentAssistantShared.Discord.Services;
using Discord;
using Discord.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using TournamentAssistantShared.SimpleJSON;

/**
 * Created by Moon on 5/18/2019
 * A Discord.NET module for basic bot functionality, not necessarily relating to Beat Saber
 */

namespace TournamentAssistantShared.Discord.Modules
{
    public class GenericModule : ModuleBase<SocketCommandContext>
    {
        public MessageUpdateService MessageUpdateService { get; set; }
        public DatabaseService DatabaseService { get; set; }
        public CommandService CommandService { get; set; }

        private static Random random = new Random();

        private bool IsAdmin()
        {
            return ((IGuildUser)Context.User).GuildPermissions.Has(GuildPermission.Administrator);
        }

        [Command("测试")]
        [Summary("就是测试而已")]
        public async Task Test([Remainder] string args = null)
        {
            if (IsAdmin()) await Task.Delay(0);
        }

        [Command("模组列表")]
        [Summary("列出所有机器人模组")]
        public async Task ListModules()
        {
            var reply = string.Empty;
            CommandService.Modules.Select(x => x.Name).ForEach(x => reply += $"{x}\n");
            await ReplyAsync(reply);
        }

        [Command("帮助")]
        [Summary("显示帮助信息")]
        public async Task HelpAsync()
        {
            var builder = new EmbedBuilder();
            builder.Title = "<:page_with_curl:735592941338361897> 命令";
            builder.Color = new Color(random.Next(255), random.Next(255), random.Next(255));
            Dictionary<string, string> moduleNameTranslation = new Dictionary<string, string>();
            moduleNameTranslation.Add("GenericModule", "通用模块");
            moduleNameTranslation.Add("PictureModule", "图片模块");
            moduleNameTranslation.Add("QualifierModule", "预选赛模块");

            foreach (var module in CommandService.Modules)
            {
                //Skip if the module has no commands
                if (module.Commands.Count <= 0) continue;

                builder.AddField(moduleNameTranslation[module.Name], $"```\n{string.Join("\n", module.Commands.Select(x => x.Name))}```", true);
            }

            await ReplyAsync(embed: builder.Build());
        }
    }
}

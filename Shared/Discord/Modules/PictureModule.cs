using TournamentAssistantShared.Discord.Services;
using Discord;
using Discord.Commands;
using System.IO;
using System.Threading.Tasks;

namespace TournamentAssistantShared.Discord.Modules
{
    public class PictureModule : ModuleBase<SocketCommandContext>
    {
        public PictureService PictureService { get; set; }

        [Command("猫")]
        public async Task CatAsync()
        {
            var stream = await PictureService.GetCatPictureAsync();
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "cat.png");
        }

        [Command("猫娘")]
        public async Task NekoAsync()
        {
            var stream = await PictureService.GetNekoStreamAsync(PictureService.NekoType.Neko);
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "neko.png");
        }

        [Command("猫娘涩图")]
        [RequireNsfw]
        public async Task NekoLewdAsync()
        {
            var stream = await PictureService.GetNekoStreamAsync(PictureService.NekoType.NekoLewd);
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "nekolewd.png");
        }

        [Command("猫娘动图")]
        public async Task NekoGifAsync()
        {
            var gifLink = await PictureService.GetNekoGifAsync();

            var builder = new EmbedBuilder();
            builder.WithImageUrl(gifLink);

            await ReplyAsync("", false, builder.Build());
        }

        [Command("猫娘涩动图")]
        [RequireNsfw]
        public async Task NekoLewdGifAsync()
        {
            var gifLink = await PictureService.GetNekoLewdGifAsync();

            var builder = new EmbedBuilder();
            builder.WithImageUrl(gifLink);

            await ReplyAsync("", false, builder.Build());
        }

        [Command("涩图")]
        [RequireNsfw]
        public async Task LewdAsync()
        {
            var stream = await PictureService.GetNekoStreamAsync(PictureService.NekoType.Hentai);
            stream.Seek(0, SeekOrigin.Begin);
            await Context.Channel.SendFileAsync(stream, "lewd.png");
        }

        [Command("涩动图")]
        [RequireNsfw]
        public async Task LewdGifAsync()
        {
            var gifLink = await PictureService.GetLewdGifAsync();

            var builder = new EmbedBuilder();
            builder.WithImageUrl(gifLink);

            await ReplyAsync("", false, builder.Build());
        }
    }
}

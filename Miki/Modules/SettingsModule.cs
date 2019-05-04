﻿using Microsoft.EntityFrameworkCore;
using Miki.Bot.Models;
using Miki.Cache;
using Miki.Discord;
using Miki.Discord.Common;
using Miki.Discord.Rest;
using Miki.Framework;
using Miki.Framework.Commands;
using Miki.Framework.Commands.Attributes;
using Miki.Framework.Commands.Localization;
using Miki.Framework.Events;
using Miki.Framework.Events.Attributes;
using Miki.Localization;
using Miki.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Miki.Modules
{
	public enum LevelNotificationsSetting
	{
		RewardsOnly = 0,
		All = 1,
		None = 2
	}

    public enum AchievementNotificationSetting
    {
        All = 0,
        None = 1
    }

	[Module("settings")]
	internal class SettingsModule
	{
        private readonly IDictionary<DatabaseSettingId, Enum> _settingOptions = new Dictionary<DatabaseSettingId, Enum>()
        {
            {DatabaseSettingId.LevelUps, (LevelNotificationsSetting)0 },
            {DatabaseSettingId.Achievements, (AchievementNotificationSetting)0 }
        };

        [Command("setnotifications")]
        public async Task SetupNotifications(IContext e)
        {
            if (!e.GetArgumentPack().Take(out string enumString))
            {
                // TODO (Veld) : Handle error.
            }

            if (!enumString.TryFromEnum<DatabaseSettingId>(out var value))
            {
                await Utils.ErrorEmbedResource(e, new LanguageResource(
                    "error_notifications_setting_not_found",
                    string.Join(", ", Enum.GetNames(typeof(DatabaseSettingId))
                        .Select(x => $"`{x}`"))))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }

            if (!_settingOptions.TryGetValue(value, out var @enum))
            {
                return;
            }

            if (!e.GetArgumentPack().Take(out string enumValue))
            {
            }

            if (!Enum.TryParse(@enum.GetType(), enumValue, true, out var type))
            {
                await Utils.ErrorEmbedResource(e, new LanguageResource(
                    "error_notifications_type_not_found",
                    enumValue,
                    value.ToString(),
                    string.Join(", ", Enum.GetNames(@enum.GetType())
                        .Select(x => $"`{x}`"))))
                    .ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
                return;
            }


            var context = e.GetService<MikiDbContext>();

            var channels = new List<IDiscordTextChannel> { (e.GetChannel() as IDiscordTextChannel) };

            if (e.GetArgumentPack().CanTake)
            {
                if (e.GetArgumentPack().Take(out string attr))
                {
                    if (attr.StartsWith("-g"))
                    {
                        channels = (await e.GetGuild().GetChannelsAsync())
                            .Where(x => x.Type == ChannelType.GUILDTEXT)
                            .Select(x => x as IDiscordTextChannel)
                            .ToList();
                    }
                }
            }

            foreach (var c in channels)
            {
                await Setting.UpdateAsync(context, c.Id, value, (int)type);
            }
            await context.SaveChangesAsync();

            await Utils.SuccessEmbed(e, e.GetLocale().GetString("notifications_update_success"))
                .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

        [Command("showmodule")]
		public async Task ConfigAsync(IContext e)
		{
            var cache = e.GetService<ICacheClient>();
            var db = e.GetService<DbContext>();

            string args = e.GetArgumentPack().Pack.TakeAll();
            //Module module = null;//e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules.FirstOrDefault(x => x.Name.ToLower() == args.ToLower());

			//if (module != null)
			//{
			//	EmbedBuilder embed = new EmbedBuilder();

			//	embed.Title = (args.ToUpper());

			//	string content = "";

			//	//foreach (CommandEvent ev in module.Events.OrderBy((x) => x.Name))
			//	//{
			//	//	content += (await ev.IsEnabledAsync(e) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
			//	//}

			//	embed.AddInlineField("Events", content);

			//	content = "";

			//	//foreach (BaseService ev in module.Services.OrderBy((x) => x.Name))
			//	//{
			//	//	content += (await ev.IsEnabledAsync(e) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>") + " " + ev.Name + "\n";
			//	//}

			//	if (!string.IsNullOrEmpty(content))
			//		embed.AddInlineField("Services", content);

   //             await embed.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
			//}
		}

		[Command("showmodules")]
		public async Task ShowModulesAsync(IContext e)
		{
            var cache = e.GetService<ICacheClient>();
            var db = e.GetService<DbContext>();

            List<string> modules = new List<string>();
            //SimpleCommandHandler commandHandler = null;//            e.EventSystem.GetCommandHandler<SimpleCommandHandler>();
			//EventAccessibility userEventAccessibility = await commandHandler.GetUserAccessibility(e);

			//foreach (CommandEvent ev in commandHandler.Commands)
			//{
			//	if (userEventAccessibility >= ev.Accessibility)
			//	{
			//		if (ev.Module != null && !modules.Contains(ev.Module.Name.ToUpper()))
			//		{
			//			modules.Add(ev.Module.Name.ToUpper());
			//		}
			//	}
			//}

			modules.Sort();

			string firstColumn = "", secondColumn = "";

			for (int i = 0; i < modules.Count(); i++)
			{
                string output = "";//$"{(await e.EventSystem.GetCommandHandler<SimpleCommandHandler>().Modules[i].IsEnabled(cache, db, e.GetChannel().Id) ? "<:iconenabled:341251534522286080>" : "<:icondisabled:341251533754728458>")} {modules[i]}\n";
				if (i < modules.Count() / 2 + 1)
				{
					firstColumn += output;
				}
				else
				{
					secondColumn += output;
				}
			}

            await new EmbedBuilder()
				.SetTitle($"Module Status for '{e.GetChannel().Name}'")
				.AddInlineField("Column 1", firstColumn)
				.AddInlineField("Column 2", secondColumn)
				.ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}

        [Command("setlocale")]
        public async Task SetLocale(IContext e)
        {
            var localization = e.GetService<LocalizationPipelineStage>();

            string localeName = e.GetArgumentPack().Pack.TakeAll() ?? "";

            if (!localization.LocaleNames.TryGetValue(localeName, out string langId))
            {
                await e.ErrorEmbedResource(
                    "error_language_invalid",
                    localeName,
                    e.GetPrefixMatch()
                ).ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
            }

            await localization.SetLocaleForChannelAsync(e, (long)e.GetChannel().Id, langId);

            await e.SuccessEmbed(
                    e.GetLocale()
                    .GetString(
                        "localization_set", 
                        $"`{localeName}`"))
                .QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
        }

		[Command("setprefix")]
		public async Task PrefixAsync(IContext e)
		{
            var prefixMiddleware = e.GetService<PipelineStageTrigger>();

            if (!e.GetArgumentPack().Take(out string prefix))
            {
                return;
            }

            await prefixMiddleware.GetDefaultTrigger()
                .ChangeForGuildAsync(
                    e.GetService<DbContext>(),
                    e.GetService<ICacheClient>(),
                    e.GetGuild().Id,
                    prefix);

            var locale = e.GetLocale();

            await new EmbedBuilder()
                .SetTitle(
                    locale.GetString("miki_module_general_prefix_success_header"))
                .SetDescription(
                    locale.GetString("miki_module_general_prefix_success_message", 
                    prefix))
                .AddField("Warning", "This command has been replaced with `>prefix set`.")
                .ToEmbed()
                .QueueToChannelAsync(e.GetChannel());
        }

		[Command("syncavatar")]
		public async Task SyncAvatarAsync(IContext e)
		{
            var context = e.GetService<MikiDbContext>();
            var cache = e.GetService<IExtendedCacheClient>();
            await Utils.SyncAvatarAsync(e.GetAuthor(), cache, context);

			await e.SuccessEmbed(
				e.GetLocale().GetString("setting_avatar_updated")	
			).QueueToChannelAsync(e.GetChannel());
		}

		[Command("listlocale")]
		public async Task ListLocaleAsync(IContext e)
		{
            var locale = e.GetService<LocalizationPipelineStage>();

            await new EmbedBuilder()
			{
				Title = e.GetLocale().GetString("locales_available"),
				Description = ("`" + string.Join("`, `", locale.LocaleNames.Keys) + "`")
			}.AddField(
				"Your language not here?",
				e.GetLocale().GetString("locales_contribute",
					$"[{e.GetLocale().GetString("locales_translations")}](https://poeditor.com/join/project/FIv7NBIReD)"
				)
			).ToEmbed().QueueToChannelAsync(e.GetChannel() as IDiscordTextChannel);
		}
	}
}
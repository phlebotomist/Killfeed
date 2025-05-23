using System;
using System.Linq;
using System.Text;
using VampireCommandFramework;

namespace PvPDetails;

public class Commands
{
	[Command("leaderboard", shortHand: "kf top")]
	public void TopCommand(ChatCommandContext ctx)
	{
		// TODO: Enhance with cache and such
		int num = 10;
		int offset = 5;
		var topKillers = DataStore.PlayerDatas.Values.OrderByDescending(k => k.Kills).Take(num).ToArray();

		offset = offset > topKillers.Length ? topKillers.Length : offset;
		num = num > topKillers.Length ? topKillers.Length : num;

		var sb = new StringBuilder();
		var sb2 = new StringBuilder();

		sb2.AppendLine("");
		sb.AppendLine($"{Markup.Prefix} <size=18><u>Leaderboard (K/D/A)</u></size>");

		const int COL_WIDTH = 5;
		const string COLOR = Markup.SecondaryColor;

		static string GetLine(DataStore.PlayerStatistics s)
		{
			string kStr = s.Kills.ToString();
			string dStr = s.Deaths.ToString();
			string aStr = s.Assists.ToString();
			int dSpacing = COL_WIDTH - dStr.Length;
			int kSpacing = COL_WIDTH - kStr.Length;
			int aSpacing = COL_WIDTH - aStr.Length;

			return
				$"<color={COLOR}><b>{Helpers.PadR(kStr, kSpacing)}</b></color>/" +
				$"<color={COLOR}><b>{Helpers.Pad(dStr, dSpacing)}</b></color>/" +
				$"<color={COLOR}><b>{Helpers.PadL(aStr, aSpacing)}</b></color>\t" +
				$"{Markup.Highlight(s.LastName)}";
		}


		for (var i = 0; i < offset; i++)
		{
			var k = topKillers[i];
			sb.AppendLine($"{i + 1}. {GetLine(k)}");
		}

		for (var i = offset; i < num; i++)
		{
			var k = topKillers[i];
			sb2.AppendLine($"{i + 1}. {GetLine(k)}");
		}

		ctx.Reply(sb.ToString());
		ctx.Reply(sb2.ToString());
	}

	[Command("reloadHook", shortHand: "rl", description: "Shows Killfeed info")]
	public async void ReloadDiscordHook(ChatCommandContext ctx)
	{
		bool issetup = await DiscordWebhook.LoadHook();
		ctx.Reply(issetup ? "Discord Webhook loaded!" : "Something went wrong loading the Discord Webhook. Check the logs or reset the file.");
	}

	[Command("testhook", shortHand: "h", description: "Shows Killfeed info")]
	public void TestDiscordHook(ChatCommandContext ctx)
	{
		_ = DiscordWebhook.SendDiscordMessageAsync("test hook");
	}

	[Command("change combat breakdown detail level", shortHand: "zz", description: "Shows Killfeed info")]
	public void ChangeCombatBreakdownDetailLevel(ChatCommandContext ctx, string v)
	{
		try
		{
			Settings.CombatBreakdownDetail = int.Parse(v);
			ctx.Reply($"combat breakdown detail level has been set to: {v}");
		}
		catch (Exception)
		{
			ctx.Reply($"failed to parse value: {v}, using keeping current value at: {Settings.CombatBreakdownDetail}");
			ctx.Reply($"Make sure to use a number between 0 and 3.");
		}
	}

	[Command("killfeed", shortHand: "kf", description: "Shows Killfeed info")]
	public void KillfeedCommand(ChatCommandContext ctx)
	{

		var steamId = ctx.User.PlatformId;

		// append current rank based on kills
		if (!DataStore.PlayerDatas.TryGetValue(steamId, out _))
		{
			throw ctx.Error($"You have no stats yet!");
		}

		var (stats, rank) = DataStore.PlayerDatas.Values.OrderByDescending(k => k.Kills)
																.Select((stats, rank) => (stats, rank))
																.First(u => u.stats.SteamId == ctx.User.PlatformId);

		var sb = new StringBuilder();
		sb.AppendLine($"{Markup.Prefix} <size=21><u>Killfeed Stats for {Markup.Highlight(stats.LastName)}</u>");

		var rankStr = $"{Markup.Highlight($"{(rank + 1)}")} / {Markup.Secondary(DataStore.PlayerDatas.Count)}";
		sb.AppendLine($"Rank: {rankStr}</size>");

		sb.AppendLine($"Kills: {Markup.Highlight(stats.Kills)}");
		sb.AppendLine($"Deaths: {Markup.Highlight(stats.Deaths)}");
		sb.AppendLine($"Assists: {Markup.Highlight(stats.Assists)}");
		sb.AppendLine($"Current Streak: {Markup.Highlight(stats.CurrentStreak)}");
		sb.AppendLine($"Highest Streak: {Markup.Highlight(stats.HighestStreak)}");


		ctx.Reply(sb.ToString());
	}
}

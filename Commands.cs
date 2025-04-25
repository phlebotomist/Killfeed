using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cpp2IL.Core.Extensions;
using ProjectM;
using ProjectM.Network;
using VampireCommandFramework;

namespace Killfeed;

public class Commands
{
	public static string Pad(string s, int n)
	{
		return new string('*', n) + s + new string('*', n);
	}

	public static string PadR(string s, int n)
	{
		return s + new string('*', n);
	}
	public static string PadL(string s, int n)
	{
		return new string('*', n) + s;
	}

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
		sb.AppendLine($"{Markup.Prefix} <size=18><u>Top Kills (K/D/A)</u></size>");

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
				$"<color={COLOR}><b>{PadR(kStr, kSpacing)}</b></color>/" +
				$"<color={COLOR}><b>{Pad(dStr, dSpacing)}</b></color>/" +
				$"<color={COLOR}><b>{PadL(aStr, aSpacing)}</b></color>\t" +
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

		//TODO this prints 2 system messages, we should prob just print some header difference to show the difference between the two messages and keep it in one message 
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

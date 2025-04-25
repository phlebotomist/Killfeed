using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Backtrace.Unity.Common;
using Bloodstone.API;
using Il2CppSystem.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;
using Random = System.Random;
using Microsoft.VisualBasic;
using Unity.Entities.UniversalDelegates;

namespace Killfeed;
public class DataStore
{
	public record struct PlayerStatistics(ulong SteamId, string LastName, int Kills, int Deaths, int CurrentStreak,
		int HighestStreak, string LastClanName, int CurrentLevel, int MaxLevel, int Assists)
	{
		private static string SafeCSVName(string s) => s.Replace(",", "");

		public string ToCsv() => $"{SteamId},{SafeCSVName(LastName)},{Kills},{Deaths},{CurrentStreak},{HighestStreak},{SafeCSVName(LastClanName)},{CurrentLevel},{MaxLevel},{Assists}";

		public static PlayerStatistics Parse(string csv)
		{
			var split = csv.Split(',');
			return new PlayerStatistics()
			{
				SteamId = ulong.Parse(split[0]),
				LastName = split[1],
				Kills = int.Parse(split[2]),
				Deaths = int.Parse(split[3]),
				CurrentStreak = int.Parse(split[4]),
				HighestStreak = int.Parse(split[5]),
				LastClanName = split.Length > 6 ? split[6] : "",
				CurrentLevel = split.Length > 7 ? int.Parse(split[7]) : -1,
				MaxLevel = split.Length > 8 ? int.Parse(split[8]) : -1,
				Assists = split.Length > 9 ? int.Parse(split[9]) : 0
			};
		}

		public string FormattedName
		{
			get
			{
				var name = Markup.Highlight(LastName);
				return Settings.IncludeLevel ? $"{name} ({Markup.Secondary(CurrentLevel)})" : $"{name}";
			}
		}
	}

	public record struct EventData(ulong VictimId, ulong KillerId, float3 Location, long Timestamp, int VictimLevel, int KillerLevel, ulong[] AssistIds)
	{
		public string ToCsv()
		{
			string line = $"{VictimId},{KillerId},{Location.x},{Location.y},{Location.z},{Timestamp},{VictimLevel},{KillerLevel}";
			if (AssistIds.Length > 0)
				line += "," + string.Join(";", AssistIds);
			return line;
		}

		public static EventData Parse(string line)
		{
			string[] lineSplit = line.Split(',');
			string[] assistsSplit = lineSplit.Length > 8 ? lineSplit[8].Split(";") : [];
			ulong[] assistIds = [];
			if (assistsSplit.Length > 0)
			{
				for (int i = 0; i < assistsSplit.Length; i++)
				{
					if (ulong.TryParse(assistsSplit[i], out ulong assistId))
					{
						assistIds[i] = assistId;
					}
				}
			}

			return new EventData()
			{
				VictimId = ulong.Parse(lineSplit[0]),
				KillerId = ulong.Parse(lineSplit[1]),
				Location = new float3(float.Parse(lineSplit[2]), float.Parse(lineSplit[3]), float.Parse(lineSplit[4])),
				Timestamp = long.Parse(lineSplit[5]),
				VictimLevel = lineSplit.Length > 6 ? int.Parse(lineSplit[6]) : 0,
				KillerLevel = lineSplit.Length > 7 ? int.Parse(lineSplit[7]) : 0,
				AssistIds = assistIds
			};
		}
	}

	public static List<EventData> Events = new();
	public static Dictionary<ulong, PlayerStatistics> PlayerDatas = new();

	private static readonly Random _rand = new();
	private static EventData GenerateTestEvent() => new()
	{
		VictimId = (ulong)_rand.NextInt64(),
		KillerId = (ulong)_rand.NextInt64(),
		Location = new float3((float)_rand.NextDouble(), (float)_rand.NextDouble(), (float)_rand.NextDouble()),
		Timestamp = DateTime.UtcNow.AddMinutes(_rand.Next(-10000, 10000)).Ticks
	};

	public static void GenerateNTestData(int count)
	{
		for (var i = 0; i < count; i++)
		{
			Events.Add(GenerateTestEvent());
		}
	}

	private const string EVENTS_FILE_NAME = "events.v1.csv";
	private const string EVENTS_FILE_PATH = $"BepInEx/config/Killfeed/{EVENTS_FILE_NAME}";

	private const string STATS_FILE_NAME = "stats.v1.csv";
	private const string STATS_FILE_PATH = $"BepInEx/config/Killfeed/{STATS_FILE_NAME}";

	public static void WriteToDisk()
	{
		var dir = Path.GetDirectoryName(EVENTS_FILE_PATH);
		if (!Directory.Exists(dir))
		{
			Directory.CreateDirectory(dir);
		}

		// TODO: Ideally this appends events and is smarter
		using StreamWriter eventsFile = new StreamWriter(EVENTS_FILE_PATH, append: false);
		foreach (var eventData in Events)
		{
			eventsFile.WriteLine(eventData.ToCsv());
		}

		using StreamWriter statsFile = new StreamWriter(STATS_FILE_PATH, append: false);
		foreach (var playerData in PlayerDatas.Values)
		{
			statsFile.WriteLine(playerData.ToCsv());
		}
	}

	public static void LoadFromDisk()
	{
		// can't think of how they would not be newed but let's be sure we don't duplicate data
		Events.Clear();
		PlayerDatas.Clear();

		// let's assume maybe it can be empty or not exist and we don't care
		LoadEventData();
		LoadPlayerData();
	}

	private static void LoadEventData()
	{
		if (!File.Exists(EVENTS_FILE_PATH))
		{
			return;
		}
		using StreamReader eventsFile = new StreamReader(EVENTS_FILE_PATH);
		while (!eventsFile.EndOfStream)
		{
			var line = eventsFile.ReadLine();
			if (string.IsNullOrWhiteSpace(line)) continue;
			try
			{
				Events.Add(EventData.Parse(line));
			}
			catch (Exception)
			{
				Plugin.Logger.LogError($"Failed to parse event line: \"{line}\"");
			}
		}
	}

	private static void LoadPlayerData()
	{
		if (!File.Exists(STATS_FILE_PATH)) return;
		using StreamReader statsFile = new StreamReader(STATS_FILE_PATH);
		while (!statsFile.EndOfStream)
		{
			var line = statsFile.ReadLine();
			if (string.IsNullOrWhiteSpace(line)) continue;
			try
			{
				var playerData = PlayerStatistics.Parse(line);
				if (PlayerDatas.TryGetValue(playerData.SteamId, out PlayerStatistics data))
				{
					Plugin.Logger.LogWarning($"Duplicate player data found, overwriting {data} with {playerData}");
				}
				PlayerDatas[playerData.SteamId] = playerData;
			}
			catch (Exception)
			{
				Plugin.Logger.LogError($"Failed to parse player line: \"{line}\"");
			}
		}
	}

	public static void RegisterKillEvent(PlayerCharacter victim, PlayerCharacter killer, float3 location, int victimLevel, int killerLevel, ulong[] assistIds)
	{
		var victimUser = victim.UserEntity.Read<User>();
		var killerUser = killer.UserEntity.Read<User>();

		// Plugin.Logger.LogWarning($"{victimLevel} {killerLevel}");

		var newEvent = new EventData(victimUser.PlatformId, killerUser.PlatformId, location, DateTime.UtcNow.Ticks, victimLevel, killerLevel, assistIds);

		Events.Add(newEvent);


		PlayerStatistics UpsertName(ulong steamId, string name, string clanName, int level)
		{
			if (PlayerDatas.TryGetValue(steamId, out var player))
			{
				player.LastName = name;
				player.LastClanName = clanName;
				player.CurrentLevel = level;
				player.MaxLevel = Math.Max(player.MaxLevel, level);
				PlayerDatas[steamId] = player;
			}
			else
			{
				PlayerDatas[steamId] = new PlayerStatistics() { LastName = name, SteamId = steamId, LastClanName = clanName, CurrentLevel = level, MaxLevel = level };
			}

			return PlayerDatas[steamId];
		}

		var victimData = UpsertName(victimUser.PlatformId, victimUser.CharacterName.ToString(), victim.SmartClanName.ToString(), victimLevel);
		var killerData = UpsertName(killerUser.PlatformId, killerUser.CharacterName.ToString(), killer.SmartClanName.ToString(), killerLevel);

		RecordKill(killerUser.PlatformId);
		var lostStreak = RecordDeath(victimUser.PlatformId);

		// Record each Assists:
		foreach (var platformId in assistIds)
		{
			if (platformId == killerUser.PlatformId) continue; // skip the killer
			if (platformId == victimUser.PlatformId) continue; // skip the victim
			RecordAssist(platformId);
		}

		AnnounceKill(victimData, killerData, lostStreak);

		// TODO: Very bad, but going to save to disk each kill for nice hiccup of lag
		// while this is naieve and whole file, in append or WAL this might be better
		WriteToDisk();
	}
	private static string GetFormatedAssistString(Dictionary<string, int> assisters)
	{
		var msg = "";
		foreach (var assist in assisters)
		{
			msg += $"{Markup.Highlight(assist.Key)} ({Markup.Secondary(assist.Value)})";
			if (assisters.Last().Key != assist.Key)
			{
				msg += ", ";
			}
		}
		return msg;
	}
	public static void HandleUnitKillSteal(string victimName, int victimLvl, Dictionary<ulong, int> assisters)
	{
		// TODO: need to reimplement this and decide default behavior, should it give assist or kill and if it give kill does it give to most damage or last hit?

		// var victimName = victim.Name.ToString();
		// var assisters = PlayerHitStore.GetRecentAttackersHighestLevel(victimUser.PlatformId);
		// if (assisters.Count == 0)
		// {
		// Plugin.Logger.LogInfo($"{victim.Name} was killed by a unit, no other vampires involved");
		// return;
		// }

		// TODO: get highest level in fight if the setting is enabled
		// int victimLvl = victimEntity.Has<Equipment>(out var victimGS) ? (int)Math.Round(victimGS.GetFullLevel()) : -1;

		// Plugin.Logger.LogInfo($"{victimName} was killed by a unit. while fighting: {string.Join(", ", assisters)}");


		// var deathmsg = $"{Markup.Highlight(victimName)} ({Markup.Secondary(victimLvl)}) was killed by a unit while fighting: {GetFormatedAssistString(assisters)}";
		// ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, deathmsg);

	}
	private static void AnnounceKill(PlayerStatistics victimUser, PlayerStatistics killerUser, int lostStreakAmount)
	{
		if (!Settings.AnnounceKills) return;

		var victimName = victimUser.FormattedName;
		var killerName = killerUser.FormattedName;

		var message = lostStreakAmount > Settings.AnnounceKillstreakLostMinimum
			? $"{killerName} ended {victimName}'s {Markup.Secondary(lostStreakAmount)} kill streak!"
			: $"{killerName} killed {victimName}!";

		var killStreakMsg = killerUser.CurrentStreak switch
		{
			5 => $"<size=18>{killerName} is on a killing spree!",
			10 => $"<size=19>{killerName} is on a rampage!",
			15 => $"<size=20>{killerName} is dominating!",
			20 => $"<size=21>{killerName} is unstoppable!",
			25 => $"<size=22>{killerName} is godlike!",
			30 => $"<size=24>{killerName} is WICKED SICK!",
			_ => null
		};

		Dictionary<ulong, (string, int)> helpersDict = PlayerHitStore.GetRecentAttackersWithLvl(victimUser.SteamId);

		// Filter out the killer and format each entry to include the level.
		var filteredHelpers = helpersDict
			.Where(x => x.Key != killerUser.SteamId)
			.ToDictionary(x => x.Value.Item1, x => x.Value.Item2);


		var assistsString = GetFormatedAssistString(filteredHelpers);
		if (filteredHelpers.Count > 0)
		{
			message += $" with help from: {assistsString}";
		}

		var fullKillMessage = Markup.Prefix + message;
		ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, fullKillMessage);

		switch (Settings.CombatBreakdownDetail)
		{
			case 1:
				DiscordWebhook.SendSimpleKillReportAsync(killerUser, victimUser, filteredHelpers.Keys.ToArray());
				break;
			case 2:
				DiscordWebhook.SendFightSummaryAsync(victimUser, killerUser);
				break;
			case 3:
				DiscordWebhook.SendDetailedBreakdownAsync(victimUser, killerUser, filteredHelpers.Keys.ToArray());
				break;
			default:
				break;
		}

		if (!string.IsNullOrEmpty(killStreakMsg) && Settings.AnnounceKillstreak)
		{
			var fullKillSteakMsg = Markup.Prefix + killStreakMsg;
			ServerChatUtils.SendSystemMessageToAllClients(VWorld.Server.EntityManager, fullKillSteakMsg);
			if (Settings.UseDiscordWebhook)
			{
				DiscordWebhook.SendKillStreakOnDiscord(killerUser);
			}
		}
	}

	private static void RecordAssist(ulong platformId)
	{
		if (PlayerDatas.TryGetValue(platformId, out var player))
		{
			player.Assists++;
			PlayerDatas[platformId] = player;
		}
		else
		{
			PlayerDatas[platformId] = new PlayerStatistics() { Assists = 1, SteamId = platformId };
		}
	}

	private static int RecordDeath(ulong platformId)
	{
		var lostStreak = 0;
		if (PlayerDatas.TryGetValue(platformId, out var player))
		{
			player.Deaths++;

			lostStreak = player.CurrentStreak;
			player.CurrentStreak = 0;

			PlayerDatas[platformId] = player;
		}
		else
		{
			PlayerDatas[platformId] = new PlayerStatistics() { Deaths = 1, SteamId = platformId };
		}

		return lostStreak;
	}

	private static void RecordKill(ulong steamId)
	{
		if (PlayerDatas.TryGetValue(steamId, out var player))
		{
			player.Kills++;
			player.CurrentStreak++;
			player.HighestStreak = math.max(player.HighestStreak, player.CurrentStreak);
			PlayerDatas[steamId] = player;
		}
		else
		{
			PlayerDatas[steamId] = new PlayerStatistics() { Kills = 1, CurrentStreak = 1, HighestStreak = 1, SteamId = steamId };
		}
	}
}

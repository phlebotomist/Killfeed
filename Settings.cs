using System;
using BepInEx.Configuration;

namespace Killfeed;

internal class Settings
{
	internal static bool AnnounceKills { get; private set; }
	internal static int AnnounceKillstreakLostMinimum { get; private set; }
	internal static bool AnnounceKillstreak { get; private set; }
	internal static bool AnounceUnitKillSteals { get; private set; }

	internal static bool IncludeLevel { get; private set; }
	// internal static bool UseMaxLevel { get; private set; } depricated

	// internal static bool UseMaxPerFightLevel { get; private set; } this will be added back in the future maybe but I don't see any reason to not have it be true

	internal static bool UseDiscordWebhook { get; private set; } = false;

	internal static int CombatBreakdownDetail { get; set; } = 2;
	internal static void Initialize(ConfigFile config)
	{
		AnnounceKills = config.Bind("General", "AnnounceKills", true, "Announce kills in chat").Value;
		AnnounceKillstreakLostMinimum = config.Bind("General", "AnnounceKillstreakLostMinimum", 3, "Minimum killstreak count that must be lost to be announced.").Value;
		AnnounceKillstreak = config.Bind("General", "AnnounceKillstreak", true, "Announce killstreaks in chat").Value;
		AnounceUnitKillSteals = config.Bind("General", "AnounceUnitKillSteals", true, "Announce that a player died to a unit while fighting a vampire").Value;

		IncludeLevel = config.Bind("General", "IncludeLevel", true, "Include player gear levels in announcements.").Value;
		// UseMaxLevel = config.Bind("General", "UseMaxLevel", false, "Use max gear level instead of current gear level.").Value;
		// UseMaxPerFightLevel = config.Bind("General", "UseMaxPerFightLevel", true, "Announce the highest gear level that was used in the fight.").Value;
		UseDiscordWebhook = config.Bind("General", "UseDiscordWebhook", true, "Announce kills and damage breakdowns in discord (requires setup with hook.txt)").Value;
		CombatBreakdownDetail = config.Bind("General", "CombatBreakdownDetail", 2, "The level of detail you want to show in the combat report sent to discord.").Value;
	}
}

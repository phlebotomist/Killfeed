using BepInEx.Configuration;

namespace Killfeed;

internal class Settings
{
	internal static bool AnnounceKills { get; private set; }
	internal static int AnnounceKillstreakLostMinimum { get; private set; }
	internal static bool AnnounceKillstreak { get; private set; }
	internal static bool AnounceUnitKillSteals { get; private set; }

	internal static bool IncludeLevel { get; private set; }
	internal static bool UseMaxLevel { get; private set; }

	internal static bool UseMaxPerFightLevel { get; private set; }
	internal static void Initialize(ConfigFile config)
	{
		AnnounceKills = config.Bind("General", "AnnounceKills", true, "Announce kills in chat").Value;
		AnnounceKillstreakLostMinimum = config.Bind("General", "AnnounceKillstreakLostMinimum", 3, "Minimum killstreak count that must be lost to be announced.").Value;
		AnnounceKillstreak = config.Bind("General", "AnnounceKillstreak", true, "Announce killstreaks in chat").Value;
		AnounceUnitKillSteals = config.Bind("General", "AnounceUnitKillSteals", true, "Announce that a player died to a unit while fighting a vampire").Value;

		IncludeLevel = config.Bind("General", "IncludeLevel", true, "Include player gear levels in announcements.").Value;
		UseMaxLevel = config.Bind("General", "UseMaxLevel", false, "Use max gear level instead of current gear level.").Value;
		UseMaxPerFightLevel = config.Bind("General", "UseMaxPerFightLevel", true, "announce the highest gear level that was used in the fight.").Value;

	}
}

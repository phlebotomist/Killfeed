using System;
using System.Collections.Generic;
using System.Linq;
using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace PvPDetails;

[HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
public static class VampireDownedHook
{
	public static void Prefix(VampireDownedServerEventSystem __instance)
	{
		var downedEvents = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp);
		foreach (var entity in downedEvents)
		{
			ProcessVampireDowned(entity);
			CleanupVampireHits(entity);
		}
	}

	private static void CleanupVampireHits(Entity entity)
	{
		if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, VWorld.Server.EntityManager, out var victimEntity))
		{
			Plugin.Logger.LogMessage("Couldn't get victim entity");
			return;
		}
		var victimPlayer = victimEntity.Read<PlayerCharacter>();
		var victimSteamId = victimPlayer.UserEntity.Read<User>().PlatformId;
		PlayerHitStore.ResetPlayerHitInteractions(victimSteamId);
	}
	private static void ProcessVampireDowned(Entity entity)
	{

		if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, VWorld.Server.EntityManager, out var victimEntity))
		{
			Plugin.Logger.LogMessage("Couldn't get victim entity");
			return;
		}

		var downBuff = entity.Read<VampireDownedBuff>();


		if (!VampireDownedServerEventSystem.TryFindRootOwner(downBuff.Source, 1, VWorld.Server.EntityManager, out var killerEntity))
		{
			Plugin.Logger.LogMessage("Couldn't get victim entity");
			return;
		}

		PlayerCharacter victim = victimEntity.Read<PlayerCharacter>();
		User victimUser = victim.UserEntity.Read<User>();

		if (!killerEntity.Has<UnitLevel>() && !killerEntity.Has<PlayerCharacter>())
		{
			Plugin.Logger.LogWarning($"Killer could not be identified for {victim.Name}, if you know how to reproduce this please contact Morphine on discord or report on github");
			return;
		}

		if (killerEntity.Has<UnitLevel>())
		{
			if (Settings.AnounceUnitKillSteals)
			{
				// DataStore.HandleUnitKillSteal(victimEntity);
				return;
			}
			return;
		}

		PlayerCharacter killer = killerEntity.Read<PlayerCharacter>();
		User killerUser = killer.UserEntity.Read<User>();

		if (killer.UserEntity == victim.UserEntity)
		{
			Plugin.Logger.LogInfo($"{victim.Name} killed themselves. [Not currently tracked]");
			return;
		}

		var location = victimEntity.Read<LocalToWorld>();

		int victimCurrentLevel = victimEntity.Has<Equipment>(out var victimEquipment) ? (int)Math.Round(victimEquipment.GetFullLevel()) : -1;
		int killerCurrentLevel = killerEntity.Has<Equipment>(out var killerEquipment) ? (int)Math.Round(killerEquipment.GetFullLevel()) : -1;

		Dictionary<ulong, (string, int)> attackers = PlayerHitStore.GetRecentAttackersWithLvl(victimUser.PlatformId);
		ulong[] assistIds = attackers.Keys.ToArray();
		if (attackers.TryGetValue(killerUser.PlatformId, out (string, int) name_lvl))
		{
			killerCurrentLevel = name_lvl.Item2;
		}

		int peakLvlInFight = PlayerHitStore.GetHighestLvlUsedOnKiller(victimUser.PlatformId, killerUser.PlatformId);
		victimCurrentLevel = Math.Max(peakLvlInFight, victimCurrentLevel);

		DataStore.RegisterKillEvent(victim, killer, location.Position, victimCurrentLevel, killerCurrentLevel, assistIds);
	}
}

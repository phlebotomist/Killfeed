using System;
using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Killfeed;

[HarmonyPatch(typeof(VampireDownedServerEventSystem), nameof(VampireDownedServerEventSystem.OnUpdate))]
public static class VampireDownedHook
{
	public static void Prefix(VampireDownedServerEventSystem __instance)
	{
		var downedEvents = __instance.__query_1174204813_0.ToEntityArray(Allocator.Temp);
		foreach (var entity in downedEvents)
		{
			ProcessVampireDowned(entity);
			CleanupVampireHits(entity); // clean up hit data so if they are revived assists don't bleed over into next fight
		}
	}

	private static void CleanupVampireHits(Entity entity)
	{
		if (!VampireDownedServerEventSystem.TryFindRootOwner(entity, 1, VWorld.Server.EntityManager, out var victimEntity))
		{
			Plugin.Logger.LogMessage("Couldn't get victim entity");
			return;
		}

		PlayerHitStore.ResetPlayerHitInteractions(victimEntity.Read<PlayerCharacter>().Name.ToString());
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

		var victim = victimEntity.Read<PlayerCharacter>();

		Plugin.Logger.LogMessage($"{victim.Name} is victim");
		var unitKiller = killerEntity.Has<UnitLevel>();

		if (unitKiller)
		{
			if (!Settings.AnounceUnitKillSteals)
			{
				Plugin.Logger.LogInfo($"{victim.Name} was killed by a unit but announcement turned off");
				return;
			}

			var victimName = victim.Name.ToString();
			var assisters = PlayerHitStore.GetRecentAttackersHighestLevel(victimName);
			if (assisters.Count == 0)
			{
				Plugin.Logger.LogInfo($"{victim.Name} was killed by a unit, no other vampires involved");
				return;
			}

			// TODO: get highest level in fight if the setting is enabled
			int victimLvl = victimEntity.Has<Equipment>(out var victimGS) ? (int)Math.Round(victimGS.GetFullLevel()) : -1;

			DataStore.HandleUnitKillSteal(victimName, victimLvl, assisters);
			return;
		}

		var playerKiller = killerEntity.Has<PlayerCharacter>();

		if (!playerKiller)
		{
			Plugin.Logger.LogWarning($"Killer could not be identified for {victim.Name}, if you know how to reproduce this please contact deca on discord or report on github");
			return;
		}

		var killer = killerEntity.Read<PlayerCharacter>();

		if (killer.UserEntity == victim.UserEntity)
		{
			Plugin.Logger.LogInfo($"{victim.Name} killed themselves. [Not currently tracked]");
			return;
		}

		var location = victimEntity.Read<LocalToWorld>();

		int victimCurrentLevel = victimEntity.Has<Equipment>(out var victimEquipment) ? (int)Math.Round(victimEquipment.GetFullLevel()) : -1;
		int killerCurrentLevel = killerEntity.Has<Equipment>(out var killerEquipment) ? (int)Math.Round(killerEquipment.GetFullLevel()) : -1;


		if (Settings.UseMaxPerFightLevel)
		{
			var attackers = PlayerHitStore.GetRecentAttackersHighestLevel(victim.Name.ToString());
			// find the killer in the attackers list and set the lvl to that level
			if (attackers.TryGetValue(killer.Name.ToString(), out var maxLevel))
			{
				killerCurrentLevel = maxLevel;
			}
			else
			{
				killerCurrentLevel = -100;
			}
		}


		DataStore.RegisterKillEvent(victim, killer, location.Position, victimCurrentLevel, killerCurrentLevel);
	}
}

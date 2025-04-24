
using System;
using System.Data;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;

namespace Killfeed;

[HarmonyPatch(typeof(StatChangeSystem), "ApplyHealthChangeToEntity")]
public static class TrackVampireAttacksPatch
{
    public static void p(EntityManager em, string msg)
    {
        ServerChatUtils.SendSystemMessageToAllClients(em, msg);
    }

    private static int GetAbilityGUIDHash(EntityManager em, Entity dmgSource)
    {
        // direct PrefabGUID on the source entity
        if (dmgSource != Entity.Null && em.HasComponent<PrefabGUID>(dmgSource))
        {
            return em.GetComponentData<PrefabGUID>(dmgSource).GuidHash;
        }
        return -1;
    }
    public static bool Prefix(StatChangeSystem __instance, ref StatChangeEvent statChange, EntityCommandBuffer commandBuffer)
    {
        // p(__instance.EntityManager, $"[1]:");
        // Ensure player->player damage
        if (statChange.Reason != StatChangeReason.DealDamageSystem_0 && statChange.Reason != StatChangeReason.TakeDamageInSunSystem_0)
            return true;

        // p(__instance.EntityManager, $"[2]:");

        // Ensure damage, not healing
        if (statChange.Change > 0)
            return true;

        // p(__instance.EntityManager, $"[3]:");
        EntityManager em = __instance.EntityManager;

        if (!em.HasComponent<EntityOwner>(statChange.Source))
            return true;


        if (!em.HasComponent<PlayerCharacter>(statChange.Entity))
            return true;

        Entity defenderEntity = statChange.Entity;
        PlayerCharacter defenderCharacter = em.GetComponentData<PlayerCharacter>(statChange.Entity);
        Entity attackerEntity = em.GetComponentData<EntityOwner>(statChange.Source).Owner;

        // p(entityManager, $"[4]:");

        if (!em.Exists(attackerEntity) || !em.HasComponent<PlayerCharacter>(attackerEntity))
            return true;

        // p(entityManager, $"[5]:");

        PlayerCharacter attackerCharacter = em.GetComponentData<PlayerCharacter>(attackerEntity);


        // p(em, $"damage done: {damageAmount}, reason: {statChange.Reason}");

        ulong attackerPlatformId = attackerCharacter.UserEntity.Read<User>().PlatformId;
        ulong victimPlatformId = defenderCharacter.UserEntity.Read<User>().PlatformId;
        string attackerName = attackerCharacter.Name.ToString();
        string victimName = defenderCharacter.Name.ToString();
        int attackerLvl = attackerEntity.Has<Equipment>(out var attackerGS) ? (int)Math.Round(attackerGS.GetFullLevel()) : -1;
        int defenderLvl = defenderEntity.Has<Equipment>(out var victimGS) ? (int)Math.Round(victimGS.GetFullLevel()) : -1;

        int sourceDmgGuidHash = GetAbilityGUIDHash(em, statChange.Source);
        int damageAmount = (int)Math.Abs(Math.Round(statChange.Change));
        p(em, $"damage done: {damageAmount}, reason: {statChange.Reason}");


        PlayerHitStore.AddHit(attackerPlatformId, attackerName, attackerLvl, victimPlatformId, victimName, defenderLvl, sourceDmgGuidHash, damageAmount);
        return true;
    }
}
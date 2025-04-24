// using System;
// using Bloodstone.API;
// using HarmonyLib;
// using ProjectM;
// using ProjectM.Gameplay.Systems;
// using ProjectM.Network; // Contains EntityOwner, AbilityOwner, etc.
// using Stunlock.Core;
// using Unity.Collections;
// using Unity.Entities;

// namespace Killfeed;

// [HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.DealDamage))]
// public static class DealDamageHook
// {
//     // Simple helper to print messages.
//     public static void p(EntityManager em, string msg)
//     {
//         ServerChatUtils.SendSystemMessageToAllClients(em, msg);
//     }

//     private static int GetAbilityGUIDHash(EntityManager em, Entity dmgSource)
//     {
//         // direct PrefabGUID on the source entity
//         if (dmgSource != Entity.Null && em.HasComponent<PrefabGUID>(dmgSource))
//         {
//             return em.GetComponentData<PrefabGUID>(dmgSource).GuidHash;
//         }
//         return -1;
//     }
//     private static PlayerCharacter? GetSpellsPlayerCharacter(EntityManager em, DealDamageEvent damageEvt)
//     {
//         var spellSource = damageEvt.SpellSource;
//         if (spellSource == Entity.Null)
//             return null;

//         if (!em.HasComponent<EntityOwner>(spellSource))
//             return null;

//         var entOwner = em.GetComponentData<EntityOwner>(spellSource);
//         Entity ownerEntity = entOwner; // Implicit conversion to get the owner Entity.

//         if (ownerEntity == Entity.Null || !em.HasComponent<PlayerCharacter>(ownerEntity))
//             return null;

//         var ownerPlayer = em.GetComponentData<PlayerCharacter>(ownerEntity);
//         return ownerPlayer;
//     }


//     public static void Prefix(DealDamageSystem __instance)
//     {
//         var em = VWorld.Server.EntityManager;
//         var entities = __instance._Query.ToEntityArray(Allocator.Temp);

//         foreach (var entity in entities)
//         {
//             if (!em.HasComponent<DealDamageEvent>(entity))
//                 continue;

//             var damageEvent = em.GetComponentData<DealDamageEvent>(entity);
//             var sourcePlayer = GetSpellsPlayerCharacter(em, damageEvent);

//             if (sourcePlayer == null)
//                 continue;

//             var victimEntity = damageEvent.Target;

//             if (victimEntity == Entity.Null || !em.HasComponent<PlayerCharacter>(victimEntity))
//                 continue;

//             var victimPlayer = em.GetComponentData<PlayerCharacter>(victimEntity);

//             var attackerPlatformId = sourcePlayer.Value.UserEntity.Read<User>().PlatformId;
//             var victimPlatformId = victimPlayer.UserEntity.Read<User>().PlatformId;

//             var attackerName = sourcePlayer.Value.Name.ToString();
//             var victimName = victimPlayer.Name.ToString();

//             // get gear score for players
//             int victimLvl = victimEntity.Has<Equipment>(out var victimGS) ? (int)Math.Round(victimGS.GetFullLevel()) : -1;
//             var attackerOwnerEnt = em.GetComponentData<EntityOwner>(damageEvent.SpellSource);
//             Entity attackerEnt = attackerOwnerEnt;
//             int attackerLvl = attackerEnt.Has<Equipment>(out var attackerGS) ? (int)Math.Round(attackerGS.GetFullLevel()) : -1;


//             // get the spell or weapon GUID hash:
//             int sourceDmgGuidHash = GetAbilityGUIDHash(em, damageEvent.SpellSource);
//             p(em, $"dealdamage guid:{sourceDmgGuidHash}");
//             // PlayerHitStore.AddHit(attackerPlatformId, attackerName, attackerLvl, victimPlatformId, victimName, victimLvl, sourceDmgGuidHash);
//         }

//         entities.Dispose();
//     }
// }

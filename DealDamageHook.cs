using System;
using Bloodstone.API;
using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network; // Contains EntityOwner, AbilityOwner, etc.
using Unity.Collections;
using Unity.Entities;

namespace Killfeed;

[HarmonyPatch(typeof(DealDamageSystem), nameof(DealDamageSystem.DealDamage))]
public static class DealDamageHook
{
    // Simple helper to print messages.
    public static void p(EntityManager em, string msg)
    {
        ServerChatUtils.SendSystemMessageToAllClients(em, msg);
    }
    private static PlayerCharacter? GetSpellsPlayerCharacter(EntityManager em, DealDamageEvent damageEvt)
    {
        var spellSource = damageEvt.SpellSource;
        if (spellSource == Entity.Null)
        {
            // p(em, "SpellSource is null.");
            return null;
        }

        if (!em.HasComponent<EntityOwner>(spellSource))
        {
            // p(em, "SpellSource does not have an EntityOwner component.");
            return null;
        }

        var entOwner = em.GetComponentData<EntityOwner>(spellSource);
        // Implicit conversion to get the owner Entity.
        Entity ownerEntity = entOwner;
        if (ownerEntity == Entity.Null)
        {
            // p(em, "EntityOwner.Owner is Entity.Null.");
            return null;
        }

        if (!em.HasComponent<PlayerCharacter>(ownerEntity))
        {
            // p(em, "Owner entity from EntityOwner does not have a PlayerCharacter component.");
            return null;
        }

        var ownerPlayer = em.GetComponentData<PlayerCharacter>(ownerEntity);
        // p(em, $"SpellSource Owner (from EntityOwner): {ownerPlayer.Name.ToString()}");
        return ownerPlayer;
    }


    public static void Prefix(DealDamageSystem __instance)
    {
        var i = 0;
        var em = VWorld.Server.EntityManager;
        var entities = __instance._Query.ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            if (!em.HasComponent<DealDamageEvent>(entity))
                continue;

            var damageEvent = em.GetComponentData<DealDamageEvent>(entity);
            var rawDamage = damageEvent.RawDamage;
            var rawDamagePercent = damageEvent.RawDamagePercent;
            var resourceModifier = damageEvent.ResourceModifier;
            // p(em, $"DamageEvent: {rawDamage}");
            // p(em, $"RawDamage: {rawDamagePercent}");
            // p(em, $"RawDamagePercent: {resourceModifier}");

            var matMods = damageEvent.MaterialModifiers;
            // p(em, $"PlayerVampire: {matMods.PlayerVampire}");
            // p(em, $"damageEvent Main Type: {damageEvent.MainType}");


            var sourcePlayer = GetSpellsPlayerCharacter(em, damageEvent);
            if (sourcePlayer == null)
            {
                // p(em, "SpellSource is null or does not have a PlayerCharacter component.");
                continue;
            }

            var victimEntity = damageEvent.Target;
            if (victimEntity == Entity.Null || !em.HasComponent<PlayerCharacter>(victimEntity))
            {
                // p(em, "Victim is null or does not have a PlayerCharacter component.");
                continue;
            }

            //CHECKS PASSED:
            // p(em, $"[i]: {i} ");

            var victimPlayer = em.GetComponentData<PlayerCharacter>(victimEntity);
            var attackerName = sourcePlayer.Value.Name.ToString();
            var victimName = victimPlayer.Name.ToString();
            var ue = victimPlayer.UserEntity;
            // p(em, $"ue: {ue}");

            // p(em, $"attacking player clan name : {sourcePlayer.Value.SmartClanName}");

            // get gear score for players
            int victimLvl = victimEntity.Has<Equipment>(out var victimGS) ? (int)Math.Round(victimGS.GetFullLevel()) : -1;
            var attackerOwnerEnt = em.GetComponentData<EntityOwner>(damageEvent.SpellSource);
            Entity attackerEnt = attackerOwnerEnt;
            int attackerLvl = attackerEnt.Has<Equipment>(out var attackerGS) ? (int)Math.Round(attackerGS.GetFullLevel()) : -1;
            // p(em, $"attackerLvl: {attackerLvl}");
            // p(em, $"victimLvl: {victimLvl}");


            PlayerHitStore.AddHit(attackerName, attackerLvl, victimName, victimLvl);

            i++;
        }

        entities.Dispose();
    }
}

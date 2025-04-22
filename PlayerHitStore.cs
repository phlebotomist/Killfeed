using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Killfeed;

public struct HitInteraction
{
    public ulong AttackerSteamId;
    public ulong VictimSteamId;
    public string AttackerName;
    public int AttackerLevel;
    public string VictimName;
    public int VictimLevel;
    public long Timestamp;
    public int DmgSourceGUID;
}

public class PlayerHitData
{
    public List<HitInteraction> Attacks { get; } = new List<HitInteraction>();
    public List<HitInteraction> Defenses { get; } = new List<HitInteraction>();
}

// Static, in-memory store for all player hit interactions.
public static class PlayerHitStore
{
    private const double PVP_WINDOW = 30.0; // seconds

    private static readonly Dictionary<ulong, PlayerHitData> interactionsByPlayer =
        new Dictionary<ulong, PlayerHitData>();

    public static IReadOnlyDictionary<ulong, PlayerHitData> InteractionsByPlayer => interactionsByPlayer;

    public static void AddHit(
        ulong attackerSteamId,
        string attackerName,
        int attackerLevel,
        ulong victimSteamId,
        string victimName,
        int victimLevel,
        int dmgSourceGUID)
    {
        var hit = new HitInteraction
        {
            AttackerSteamId = attackerSteamId,
            AttackerName = attackerName,
            AttackerLevel = attackerLevel,
            VictimSteamId = victimSteamId,
            VictimName = victimName,
            VictimLevel = victimLevel,
            Timestamp = Stopwatch.GetTimestamp(),
            DmgSourceGUID = dmgSourceGUID
        };

        AddAttack(attackerSteamId, hit);
        AddDefense(victimSteamId, hit);

        // cleanup if too many defenses
        if (interactionsByPlayer.TryGetValue(victimSteamId, out var victimHitData)
            && victimHitData.Defenses.Count >= 500)
        {
            CleanupOldHitInteractionsByPlayer(victimSteamId);
        }
    }

    private static void AddAttack(ulong playerSteamId, HitInteraction hit)
    {
        if (!interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
        {
            hitData = new PlayerHitData();
            interactionsByPlayer[playerSteamId] = hitData;
        }
        hitData.Attacks.Add(hit);
    }

    private static void AddDefense(ulong playerSteamId, HitInteraction hit)
    {
        if (!interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
        {
            hitData = new PlayerHitData();
            interactionsByPlayer[playerSteamId] = hitData;
        }
        hitData.Defenses.Add(hit);
    }

    public static IReadOnlyList<HitInteraction> GetAttacks(ulong playerSteamId)
    {
        if (interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
            return hitData.Attacks;
        return [];
    }

    public static IReadOnlyList<HitInteraction> GetDefenses(ulong playerSteamId)
    {
        if (interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
            return hitData.Defenses;
        return [];
    }

    //returns a map of attacker names to their highest level in the last pvpWindowSeconds seconds 
    public static Dictionary<string, int> GetRecentAttackersHighestLevel(
        ulong playerSteamId,
        double pvpWindowSeconds = PVP_WINDOW)
    {
        var result = new Dictionary<string, int>();
        if (!interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
            return result;

        long currentTicks = Stopwatch.GetTimestamp();
        long windowTicks = (long)(pvpWindowSeconds * Stopwatch.Frequency);

        foreach (var hit in hitData.Defenses)
        {
            if (currentTicks - hit.Timestamp <= windowTicks)
            {
                if (result.TryGetValue(hit.AttackerName, out int existingLevel))
                {
                    if (hit.AttackerLevel > existingLevel)
                        result[hit.AttackerName] = hit.AttackerLevel;
                }
                else
                {
                    result[hit.AttackerName] = hit.AttackerLevel;
                }
            }
        }
        return result;
    }
    /// <summary>
    /// Returns all hit interactions (both attacks and defenses) for the given player
    /// that occurred within the 'pvpWindowSeconds' seconds,
    /// sorted by timestamp (oldest first).
    /// </summary>
    public static IReadOnlyList<HitInteraction> GetRecentInteractions(
        ulong playerSteamId,
        double pvpWindowSeconds = PVP_WINDOW)
    {
        if (!interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
            return [];

        long nowTicks = Stopwatch.GetTimestamp();
        long windowTicks = (long)(pvpWindowSeconds * Stopwatch.Frequency);
        long earliest = nowTicks - windowTicks;

        return hitData.Attacks
            .Concat(hitData.Defenses)
            .Where(hit => hit.Timestamp >= earliest)
            .OrderBy(hit => hit.Timestamp)
            .ToList();
    }

    public static void CleanupOldHitInteractionsByPlayer(
        ulong playerSteamId,
        double pvpWindowSeconds = PVP_WINDOW)
    {
        if (!interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
            return;

        Plugin.Logger.LogMessage($"CLEANING up hit interactions for SteamID: {playerSteamId}");

        long currentTicks = Stopwatch.GetTimestamp();
        long windowTicks = (long)(pvpWindowSeconds * Stopwatch.Frequency);

        int before = hitData.Defenses.Count;
        hitData.Defenses.RemoveAll(hit => (currentTicks - hit.Timestamp) > windowTicks);
        int after = hitData.Defenses.Count;
        Plugin.Logger.LogMessage($"CLEANED up {before - after} old hit interactions for SteamID: {playerSteamId}");
    }

    public static void ResetPlayerHitInteractions(ulong playerSteamId)
    {
        if (interactionsByPlayer.TryGetValue(playerSteamId, out var hitData))
        {
            hitData.Attacks.Clear();
            hitData.Defenses.Clear();
        }
    }

    public static void Clear() => interactionsByPlayer.Clear();
}

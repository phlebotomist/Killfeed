using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Killfeed;

public struct HitInteraction
{
    public string AttackerName;
    public int AttackerLevel;
    public string VictimName;
    public int VictimLevel;
    public long Timestamp;
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
    // Dictionary keyed by player name (case-insensitive) holding each player's hit data.
    private static readonly Dictionary<string, PlayerHitData> interactionsByPlayer =
        new Dictionary<string, PlayerHitData>(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, PlayerHitData> InteractionsByPlayer => interactionsByPlayer;

    public static void AddHit(string attackerName, int attackerLevel, string victimName, int victimLevel)
    {
        var hit = new HitInteraction
        {
            AttackerName = attackerName,
            AttackerLevel = attackerLevel,
            VictimName = victimName,
            VictimLevel = victimLevel,
            Timestamp = Stopwatch.GetTimestamp()
        };

        // Currently no use for attack list so we don't add to it.
        // AddAttack(attackerName, hit);

        AddDefense(victimName, hit);

        // it might be over kill to check this but in the case someone is really good and never dies and fights a lot we should have way to clean up their data every once and a while.
        // Server restart should do this as well but better to be safe than sorry.
        if (InteractionsByPlayer.TryGetValue(victimName, out var victimHitData) && victimHitData.Defenses.Count >= 500)
        {
            CleanupOldHitInteractionsByPlayer(victimName);
        }
    }

    private static void AddAttack(string playerName, HitInteraction hit)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        if (!interactionsByPlayer.TryGetValue(playerName, out var hitData))
        {
            hitData = new PlayerHitData();
            interactionsByPlayer[playerName] = hitData;
        }
        hitData.Attacks.Add(hit);
    }

    private static void AddDefense(string playerName, HitInteraction hit)
    {
        if (string.IsNullOrWhiteSpace(playerName))
            return;

        if (!interactionsByPlayer.TryGetValue(playerName, out var hitData))
        {
            hitData = new PlayerHitData();
            interactionsByPlayer[playerName] = hitData;
        }
        hitData.Defenses.Add(hit);
    }

    // Retrieves the list of attacks (hits initiated by the player).
    public static IReadOnlyList<HitInteraction> GetAttacks(string playerName)
    {
        if (interactionsByPlayer.TryGetValue(playerName, out var hitData))
            return hitData.Attacks;
        return new List<HitInteraction>();
    }

    // Retrieves the list of defenses (hits received by the player).
    public static IReadOnlyList<HitInteraction> GetDefenses(string playerName)
    {
        if (interactionsByPlayer.TryGetValue(playerName, out var hitData))
            return hitData.Defenses;
        return new List<HitInteraction>();
    }

    /// <summary>
    /// Returns a dictionary mapping attacker names to their highest level recorded among defense hits
    /// for the given player within the specified pvp time window (in seconds).
    /// </summary>
    public static Dictionary<string, int> GetRecentAttackersHighestLevel(string playerName, double pvpWindowSeconds = PVP_WINDOW)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!interactionsByPlayer.TryGetValue(playerName, out var hitData))
            return result;

        // Get current tick count and calculate the pvp window in ticks.
        long currentTicks = Stopwatch.GetTimestamp();
        long windowTicks = (long)(pvpWindowSeconds * Stopwatch.Frequency);

        foreach (var hit in hitData.Defenses)
        {
            if (currentTicks - hit.Timestamp <= windowTicks)
            {
                // If the attacker already exists in the result, update if this hit's level is higher. TODO: this needs to check settings to make sure we want maxlvlperfight
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


    public static void CleanupOldHitInteractionsByPlayer(string playerName, double pvpWindowSeconds = PVP_WINDOW)
    {

        if (!interactionsByPlayer.TryGetValue(playerName, out var hitData))
            return;

        Plugin.Logger.LogMessage($"CLEANING up hit interactions for player: {playerName}");

        long currentTicks = Stopwatch.GetTimestamp();
        long windowTicks = (long)(pvpWindowSeconds * Stopwatch.Frequency);

        // Remove any hit interactions from the Desfenses list that are outside the current PvP window.
        var sizeBefore = hitData.Defenses.Count;
        hitData.Defenses.RemoveAll(hit => (currentTicks - hit.Timestamp) > windowTicks);
        var sizeAfter = hitData.Defenses.Count;
        Plugin.Logger.LogMessage($"CLEANED up [{sizeAfter - sizeBefore}] old hit interactions for player: {playerName}");
    }
    public static void ResetPlayerHitInteractions(string playerName)
    {
        if (interactionsByPlayer.TryGetValue(playerName, out var hitData))
        {
            Plugin.Logger.LogMessage($"RESETTING {playerName}'s hit interactions.");
            hitData.Attacks.Clear();
            hitData.Defenses.Clear();
        }
        else
        {
            Plugin.Logger.LogMessage($"No hit interactions found for {playerName} to reset.");
        }
    }
    public static void Clear() => interactionsByPlayer.Clear();
}

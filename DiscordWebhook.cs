using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static PvPDetails.DataStore;

namespace PvPDetails;

public static class DiscordWebhook
{
    private const string HOOK_FILE_NAME = "hook.txt";
    private const string DISCORD_HOOK_FILE_PATH = $"BepInEx/config/{HOOK_FILE_NAME}";
    private static string _webhookUrl;

    private static readonly HttpClient _http = new();

    public static async Task<bool> LoadHook()
    {
        if (!File.Exists(DISCORD_HOOK_FILE_PATH))
        {
            Plugin.Logger.LogWarning($"Discord webhook file not found at '{DISCORD_HOOK_FILE_PATH}'. Webhook disabled.");
            return false;
        }

        var url = File.ReadAllText(DISCORD_HOOK_FILE_PATH).Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            Plugin.Logger.LogWarning($"Discord webhook URL in '{DISCORD_HOOK_FILE_PATH}' is empty. Webhook disabled.");
            return false;
        }

        bool ok;
        try
        {
            ok = await PingAsync(url);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"Exception while pinging Discord webhook: {ex.Message}. Webhook disabled.");
            _webhookUrl = null;
            return false;
        }

        if (!ok)
        {
            Plugin.Logger.LogWarning($"Discord webhook URL in '{DISCORD_HOOK_FILE_PATH}' is invalid. Webhook disabled.");
            _webhookUrl = null;
            return false;
        }
        else
        {
            _webhookUrl = url;
            return true;
        }
    }

    // <summary>
    // returns true if the hook is enabled in settings and valid.
    // </summary>
    public static bool HookEnabled()
    {
        return Settings.UseDiscordWebhook && !string.IsNullOrWhiteSpace(_webhookUrl);
    }
    public static void SendKillStreakOnDiscord(PlayerStatistics killerUser)
    {
        if (!HookEnabled())
            return;
        // TODO: this gets lumped into the kill message because the time is so close the name trick for discord doesn't work. Because of this we need to addd a specific name change 
        _ = SendDiscordMessageAsync("testest:" + killerUser.LastName);
    }


    private static string GetKillString(PlayerStatistics killer, PlayerStatistics victim)
    {
        return $"🗡️ **{killer.LastName}** ({killer.CurrentLevel}) killed **{victim.LastName}** ({victim.CurrentLevel}) ☠️";
    }

    /// <summary>
    /// Builds a simple Markdown kill report and sends it.
    /// </summary>
    public static void SendSimpleKillReportAsync(PlayerStatistics killer, PlayerStatistics victim, string[] assisters)
    {
        if (!HookEnabled())
            return;

        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine(GetKillString(killer, victim));
        if (assisters != null && assisters.Length > 0)
            sb.AppendLine($"**Assisters:** {string.Join(", ", assisters)}");

        _ = SendDiscordMessageAsync(sb.ToString());
    }


    /// <summary>
    /// sends hit by hit breakdown to the webhook.
    /// </summary>
    public static void SendDetailedBreakdownAsync(
        PlayerStatistics victim,
        PlayerStatistics killer,
        string[] assisters,
        double pvpWindowSeconds = 30.0)
    {
        if (!HookEnabled())
            return;

        var headerSb = new StringBuilder();
        headerSb.Append(GetKillString(killer, victim));
        if (assisters != null && assisters.Length > 0)
        {
            headerSb.Append($" (assisted by {string.Join(", ", assisters)})");
        }

        // Fetch recent hits
        var hits = PlayerHitStore.GetRecentInteractions(victim.SteamId, pvpWindowSeconds);

        // Build breakdown lines
        var sb = new StringBuilder();
        sb.AppendLine(headerSb.ToString());
        sb.AppendLine(); // blank line
        sb.AppendLine($"📊 __hit by hit breakdown breakdown for__ **{victim.LastName}** (last {pvpWindowSeconds:F0}s)");

        foreach (var hit in hits)
        {
            var ability = HitNameResolver.Resolve(hit.DmgSourceGUID);
            sb.AppendLine(
                $"• **{hit.AttackerName}** ({hit.AttackerLevel}) " +
                $"hit **{hit.VictimName}** ({hit.VictimLevel}) " +
                $"with **{ability}**" +
                $" for **{hit.DmgAmount}** damage"
            );
        }

        _ = SendDiscordMessageAsync(sb.ToString());
    }

    public static void SendFightSummaryAsync(
    PlayerStatistics victim,
    PlayerStatistics killer,
    double pvpWindowSeconds = 30.0)
    {
        if (!HookEnabled())
            return;

        var hits = PlayerHitStore.GetRecentInteractions(victim.SteamId, pvpWindowSeconds);
        var incoming = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var outgoing = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var hit in hits)
        {
            var ability = HitNameResolver.Resolve(hit.DmgSourceGUID);

            //TODO: We prob need to deal with sun and silve here as well
            if (string.Equals(hit.VictimName, victim.LastName, StringComparison.OrdinalIgnoreCase))
            {
                incoming[ability] = incoming.TryGetValue(ability, out var v) ? v + hit.DmgAmount : hit.DmgAmount;
            }
            else if (string.Equals(hit.AttackerName, victim.LastName, StringComparison.OrdinalIgnoreCase))
            {
                outgoing[ability] = outgoing.TryGetValue(ability, out var v) ? v + hit.DmgAmount : hit.DmgAmount;
            }
        }

        var sb = new StringBuilder();

        sb.AppendLine(GetKillString(killer, victim));
        sb.AppendLine();
        if (incoming.Count > 0)
        {
            sb.AppendLine($"📊 __Incoming damage for__ **{victim.LastName}** (last {pvpWindowSeconds:F0}s)");
            foreach (var kvp in incoming.OrderByDescending(k => k.Value))
                sb.AppendLine($"• {kvp.Key}: **{kvp.Value:F0}**");
        }

        if (outgoing.Count > 0)
        {
            sb.AppendLine(); // blank line between the two sections
            sb.AppendLine($"📊 __Outgoing damage for__ **{victim.LastName}** (last {pvpWindowSeconds:F0}s)");
            foreach (var kvp in outgoing.OrderByDescending(k => k.Value))
                sb.AppendLine($"• {kvp.Key}: **{kvp.Value:F0}**");
        }

        _ = SendDiscordMessageAsync(sb.ToString());
    }

    public static async Task SendDiscordMessageAsync(string msg)
    {
        if (!HookEnabled())
            return;

        var now = DateTime.UtcNow.ToString("yyyy-MM-dd_HH:mm:ss");
        var payload = JsonSerializer.Serialize(new { username = $"💀 Kill Reporter 💀 [{now}]", content = msg });

        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        try
        {
            var response = await _http.PostAsync(_webhookUrl, content);
            if (!response.IsSuccessStatusCode)
                Plugin.Logger.LogWarning($"Discord returned {response.StatusCode} when sending message.");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Failed to send Discord message: {ex.Message}");
        }
    }

    private static async Task<bool> PingAsync(string test_url)
    {
        if (string.IsNullOrWhiteSpace(test_url))
        {
            Plugin.Logger.LogWarning("Webhook URL not loaded. Call Load() first.");
            return false;
        }

        try
        {
            // GET the webhook metadata (no message is sent)
            using var resp = await _http.GetAsync(test_url);
            if (resp.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                Plugin.Logger.LogWarning($"Ping failed: Discord returned {resp.StatusCode}.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"[Error] Exception pinging webhook: {ex.Message}");
            return false;
        }
    }
}

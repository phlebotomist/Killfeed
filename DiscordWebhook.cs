using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Killfeed;

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

    /// <summary>
    /// Builds a simple Markdown kill report and sends it.
    /// </summary>
    public static Task SendSimpleKillReportAsync(string killer, string victim, string[] assisters)
    {
        var sb = new StringBuilder();
        sb.AppendLine("**💀 V Rising Kill Report**");
        sb.AppendLine();
        sb.AppendLine($"Killer: 🗡️ {killer}");
        sb.AppendLine($"Victim: ☠️ {victim}");
        if (assisters != null && assisters.Length > 0)
            sb.AppendLine($"**Assisters:** {string.Join(", ", assisters)}");

        return SendDiscordMessageAsync(sb.ToString());
    }

    public static Task SendDetailedBreakdownAsync(
        ulong victimSteamId,
        string victimName,
        string killerName,
        string[] assisters,
        double pvpWindowSeconds = 30.0)
    {
        // Header line: who killed and who assisted
        var headerSb = new StringBuilder();
        headerSb.Append($"**⚔️ {killerName} killed {victimName}**");
        if (assisters != null && assisters.Length > 0)
        {
            headerSb.Append($" (assisted by {string.Join(", ", assisters)})");
        }
        headerSb.AppendLine();

        // Fetch recent hits
        var hits = PlayerHitStore.GetRecentInteractions(victimSteamId, pvpWindowSeconds);
        if (hits.Count == 0)
        {
            headerSb.AppendLine($"No interactions to show for **{victimName}** in the last {pvpWindowSeconds:F0}s.");
            return SendDiscordMessageAsync(headerSb.ToString());
        }

        // Build breakdown lines
        var sb = new StringBuilder();
        sb.AppendLine(headerSb.ToString());
        sb.AppendLine(); // blank line

        foreach (var hit in hits)
        {
            var ability = HitNameResolver.Resolve(hit.DmgSourceGUID);
            sb.AppendLine(
                $"{hit.AttackerName} ({hit.AttackerLevel}) " +
                $"hit **{hit.VictimName}** ({hit.VictimLevel}) " +
                $"with **{ability}**" +
                $" for **{hit.DmgAmount}** damage"
            );
        }

        return SendDiscordMessageAsync(sb.ToString());
    }


    public static async Task SendDiscordMessageAsync(string msg)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            Plugin.Logger.LogWarning("Webhook URL not loaded. Call Load() before sending.");
            return;
        }

        var payload = JsonSerializer.Serialize(new { content = msg });
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

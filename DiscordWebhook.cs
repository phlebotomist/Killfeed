using System;
using System.IO;
using System.Net.Http;
using System.Text;
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

    public static async Task SendDiscordMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            Plugin.Logger.LogWarning("[Warning] Webhook URL not loaded. Call Load() before sending.");
            return;
        }

        var payload = "{\"content\":\"testMsg\"}";
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        try
        {
            var response = await _http.PostAsync(_webhookUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                Plugin.Logger.LogWarning($"Discord returned {response.StatusCode} when sending test message.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"[Error] Failed to send test message: {ex.Message}");
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

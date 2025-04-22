using System.Collections.Generic;

namespace Killfeed
{
    /// <summary>
    /// Resolves prefab GUIDs to human-readable ability/weapon names.
    /// </summary>
    public static class HitNameResolver
    {
        private static readonly Dictionary<int, string> _guidToName = new()
        {
            { 1998252380, "Shadowbolt"},
            {706730253, "Frenzy"},
            {-1733898626, "Axe melee 1"},
            {-1192587580, "Axe melee 2"},
            {-1064937884, "Axe melee 3"},
            {-1274932233, "ExplosiveShot"},
            {728144325 ,"ExplosiveShot explosion"}
        };

        /// <summary>
        /// Returns the name for the given prefab GUID, or "<GUID>" if not found.
        /// </summary>
        public static string Resolve(int prefabGuid)
        {
            return _guidToName.TryGetValue(prefabGuid, out var name)
                ? name
                : $"<{prefabGuid}>";
        }
    }
}

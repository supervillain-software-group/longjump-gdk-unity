using System;
using System.Linq;
using System.Threading.Tasks;
using LongJump.Api;
using LongJump.Api.Model;
using Newtonsoft.Json.Linq;

namespace LongJump
{
    /// <summary>
    /// The player's saves on LongJump, by slot name. Save with the generated <c>CloudSavesApi</c>; each
    /// save to a slot adds a new one, and loading a slot returns its newest.
    /// </summary>
    /// <example>
    /// <code>
    /// await LongJump.Session.CallAsync(c =&gt; new CloudSavesApi(c).CreateCloudSaveAsync(
    ///     LongJump.Setup.GameEnvironmentId, new NewCloudSave("slot1", saveData)));
    /// var save = await LongJump.CloudSaves.LoadAsync&lt;SaveData&gt;("slot1");
    /// if (save != null) Apply(save);
    /// </code>
    /// </example>
    public static class CloudSaves
    {
        /// <summary>The newest save in a slot as <typeparamref name="T"/>, or default when the slot is empty.</summary>
        /// <param name="slot">The exact slot name; "slot1" doesn't match "slot10".</param>
        public static async Task<T> LoadAsync<T>(string slot)
        {
            // The server ignores a blank name filter and lists every save, so a blank slot would look like an empty one.
            if (string.IsNullOrWhiteSpace(slot)) throw new ArgumentException("Required.", nameof(slot));
            var newest = await FindNewestAsync(slot);
            if (newest == null) return default;

            var save = await Session.CallAsync(configuration => new CloudSavesApi(configuration).GetCloudSaveAsync(newest.Id));
            return save.Data == null ? default : JToken.FromObject(save.Data).ToObject<T>();
        }

        // The server matches names by substring, newest first, ten to a page, so look for the exact name
        // page by page.
        static async Task<CloudSaveSummary> FindNewestAsync(string slot)
        {
            for (var page = 1; ; page++)
            {
                var current = page;
                var result = await Session.CallAsync(configuration =>
                    new CloudSavesApi(configuration).ListCloudSavesAsync(Setup.GameEnvironmentId, current, slot));
                var match = result.Data?.FirstOrDefault(save => save.Name == slot);
                if (match != null) return match;
                if (result.Data == null || result.Data.Count == 0 || result.Meta == null || page >= result.Meta.LastPage) return null;
            }
        }
    }
}

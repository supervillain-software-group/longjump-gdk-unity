using System;
using Newtonsoft.Json.Linq;

namespace LongJump
{
    /// <summary>A player event another player (or this one) broadcast, received in real time.</summary>
    public sealed class GameEvent
    {
        /// <summary>The event name, such as "item_get".</summary>
        public string Name { get; }

        /// <summary>The event data as the sender broadcast it. Empty when there was none. <c>Data.ToObject&lt;T&gt;()</c> reads it as one of your own types.</summary>
        public JObject Data { get; }

        /// <summary>Who broadcast it. Null if the message didn't say.</summary>
        public PlayerInfo Player { get; }

        /// <summary>The whole message as received, for games that parse it into their own types.</summary>
        public string Json { get; }

        /// <summary>Broadcast by the signed-in player: the server sends players their own events too.</summary>
        public bool IsMine => Player?.Id != null && Session.Player != null &&
            string.Equals(Player.Id, Session.Player.Id.ToString(), StringComparison.OrdinalIgnoreCase);

        internal GameEvent(string name, JObject data, PlayerInfo player, string json)
        {
            Json = json;
            Name = name;
            Data = data ?? new JObject();
            Player = player;
        }
    }

    /// <summary>A player as real-time messages describe them.</summary>
    public sealed class PlayerInfo
    {
        public string Id { get; }
        public string SteamId { get; }
        public string DisplayName { get; }
        public string AvatarUrl { get; }

        internal PlayerInfo(string id, string steamId, string displayName, string avatarUrl)
        {
            Id = id;
            SteamId = steamId;
            DisplayName = displayName;
            AvatarUrl = avatarUrl;
        }

        internal static PlayerInfo From(JToken token) =>
            token is JObject player
                ? new PlayerInfo((string)player["id"], (string)player["steamId"], (string)player["displayName"], (string)player["avatarUrl"])
                : null;
    }
}

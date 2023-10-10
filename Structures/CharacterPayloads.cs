using GW2IO.Maps.Converters;
using Gw2Sharp.Models;
using Microsoft.Xna.Framework;
using System.Text.Json.Serialization;

namespace GW2IO.Maps.Structures
{
    internal class CharacterStateUpdate
    {
        public string Type { get { return "UpdateCharacterState"; } }
        public string CharacterName { get; set; }
        public int ContinentId { get; set; }
        public int MapId { get; set; }

        public uint ShardId { get; set; }
        public string ServerConnectionInfo { get; set; }
        public int BuildId { get; set; }

        public bool IsCommander { get; set; }
        public MountType Mount { get; set; }
        public ProfessionType Profession { get; set; }
        public int Specialisation { get; set; }
    }

    internal class CharacterLocationUpdate
    {
        public string Type { get { return "UpsertCharacterMovement"; } }
        public string CharacterName { get; set; }
        public int ContinentId { get; set; }
        public int MapId { get; set; }
        [JsonConverter(typeof(Vector2))]
        public Vector2 MapPosition { get; set; }
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 CharacterForward { get; set; }
    }

    internal class CharacterDeleteUpdate
    {
        public string Type { get { return "DeleteCharacterData"; } }
        public string CharacterName { get; set; }
    }

    internal class CharacterKeepAlive
    {
        public string Type { get { return "UpdateCharacterKeepAlive"; } }
    }
}

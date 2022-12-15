namespace ChuckDeviceController.JobControllers.Models
{
    using System.Collections.Concurrent;

    using POGOProtos.Rpc;

    using ChuckDeviceController.Collections.Queues;
    using ChuckDeviceController.Geometry.Models.Contracts;

    internal class PlayerLevelingData
    {
        public ConcurrentDictionary<string, PokemonFortProto> UnspunPokestops { get; } = new();

        //public object UnspunPokestopsLock { get; } = new();

        public ConcurrentDictionary<string, ICoordinate> LastPokestopsSpun { get; } = new();

        public ulong LastSeen { get; set; }

        public ulong XP { get; set; }

        public ushort Level { get; set; }

        public PokemonPriorityQueue<(ulong, ulong)> XpPerTime { get; } = new(); // timestamp, xp

        public ICoordinate? LastLocation { get; set; }
    }
}
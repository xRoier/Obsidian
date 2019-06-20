﻿using Obsidian.Entities;
using Obsidian.PlayerData;
using System.Threading.Tasks;

namespace Obsidian.Net.Packets
{
    public class JoinGame : Packet
    {

        public int EntityId { get; private set; }

        public Gamemode GameMode { get; private set; } = Gamemode.Survival;

        public Dimension Dimension { get; private set; } = Dimension.Overworld; 

        public Difficulty Difficulty { get; private set; } = Difficulty.Peaceful; 

        public byte MaxPlayers { get; private set; } = 0; // Gets ignored by client

        public string LevelType { get; private set; } = "default";

        public bool ReducedDebugInfo { get; private set; } = false;

        public JoinGame(byte[] data) : base(0x25, data) { }

        public JoinGame(int entityid, Gamemode gamemode, Dimension dimension, Difficulty difficulty, string leveltype, bool debugging) : base(0x25, new byte[0])
        {
            this.EntityId = entityid;
            this.GameMode = gamemode;
            this.Dimension = dimension;
            this.Difficulty = difficulty;
            this.LevelType = leveltype;
            this.ReducedDebugInfo = !debugging;
        }

        public override async Task PopulateAsync()
        {
            using (var stream = new MinecraftStream(this.PacketData))
            {
                this.EntityId = await stream.ReadVarIntAsync();
                this.GameMode = (Gamemode)await stream.ReadUnsignedByteAsync();
                this.Dimension = (Dimension)await stream.ReadIntAsync();
                this.Difficulty = (Difficulty)await stream.ReadUnsignedByteAsync();
                this.MaxPlayers = await stream.ReadUnsignedByteAsync();
                this.LevelType = await stream.ReadStringAsync();
                this.ReducedDebugInfo = await stream.ReadBooleanAsync();
            }
        }

        public override async Task<byte[]> ToArrayAsync()
        {
            using(var stream = new MinecraftStream())
            {
                await stream.WriteIntAsync(this.EntityId);
                await stream.WriteUnsignedByteAsync((byte)this.GameMode);
                await stream.WriteIntAsync((int)this.Dimension);
                await stream.WriteUnsignedByteAsync((byte)this.Difficulty);
                await stream.WriteUnsignedByteAsync(this.MaxPlayers);
                await stream.WriteStringAsync(this.LevelType);
                await stream.WriteBooleanAsync(this.ReducedDebugInfo);
                return stream.ToArray();
            }
        }
    }
}
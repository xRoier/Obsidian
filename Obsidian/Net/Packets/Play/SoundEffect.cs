﻿using System;
using System.Threading.Tasks;
using Obsidian.Sounds;
using Obsidian.Util.DataTypes;

namespace Obsidian.Net.Packets.Play
{
    public class SoundEffect : Packet
    {
        public SoundEffect(int soundId, Position location, SoundCategory category = SoundCategory.Master, float pitch = 1.0f, float volume = 1f) : base(0x4D, Array.Empty<byte>())
        {
            this.SoundId = soundId;
            this.Location = location;
            this.Category = category;
            this.Pitch = pitch;
            this.Volume = volume;
        }

        public SoundCategory Category { get; set; }
        public float Pitch { get; set; }
        public Position Location { get; set; }
        public int SoundId { get; set; }
        public float Volume { get; set; }

        protected override Task PopulateAsync(MinecraftStream stream) => throw new NotImplementedException();

        protected override async Task ComposeAsync(MinecraftStream stream)
        {
            await stream.WriteVarIntAsync(this.SoundId);
            await stream.WriteVarIntAsync((int)this.Category);
            await stream.WriteIntAsync((int)(this.Location.X / 32.0D));
            await stream.WriteIntAsync((int)(this.Location.Y / 32.0D));
            await stream.WriteIntAsync((int)(this.Location.Z / 32.0D));
            await stream.WriteFloatAsync(this.Volume);
            await stream.WriteFloatAsync(this.Pitch);
        }
    }
}
﻿using Obsidian.API;
using Obsidian.Net;
using System;
using System.Threading.Tasks;

namespace Obsidian.Entities
{
    public class FallingBlock : Entity
    {
        public VectorF SpawnPosition { get; private set; }

        public Material BlockMaterial { get; set; }

        private int AliveTime { get; set; }

        private VectorF DeltaPosition { get; set; }

        private readonly VectorF gravity = new VectorF(0F, -0.02F, 0F);
        
        private readonly float windResFactor = 0.98F;

        public FallingBlock() : base()
        {
            SpawnPosition = Position;
            LastPosition = Position;
            AliveTime = 0;
            DeltaPosition = new VectorF(0F, 0F, 0F);
        }

        public async override Task TickAsync()
        {
            AliveTime++;
            LastPosition = Position;
            if (!this.NoGravity)
                DeltaPosition += gravity; // y - 0.04
            DeltaPosition *= windResFactor; // * 0.98
            Position += DeltaPosition;

            // Check below to see if we're about to hit a solid block.
            var upcomingBlockPos = new Vector(
                (int)Math.Floor(Position.X),
                (int)Math.Floor(Position.Y - 1),
                (int)Math.Floor(Position.Z));

            bool convertToBlock = !server.World.GetBlock(upcomingBlockPos)?.IsAir ?? false;
            if (convertToBlock) { await ConvertToBlock(upcomingBlockPos + (0, 1, 0)); }
        }

        private async Task ConvertToBlock(Vector loc)
        {
            var block = new Block(BlockMaterial);
            server.World.SetBlock(loc, block);
            //
            foreach (var p in server.PlayersInRange(loc))
            {
                await server.BroadcastBlockPlacementToPlayerAsync(p, block, loc);
            }

            await server.World.DestroyEntityAsync(this);
        }
    }
}

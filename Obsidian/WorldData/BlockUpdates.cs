﻿using Obsidian.API;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Obsidian.WorldData
{
    internal static class BlockUpdates
    {
        internal static async Task<bool> HandleFallingBlock(BlockUpdate blockUpdate)
        {
            if (blockUpdate.block is null) { return false; }
            var world = blockUpdate.world;
            var location = blockUpdate.position;
            var material = blockUpdate.block.Value.Material;
            if (world.GetBlock(location + Vector.Down) is Block below && (Block.Replaceable.Contains(below.Material) || below.IsFluid))
            {
                world.SetBlock(location, Block.Air);
                world.SpawnFallingBlock(location, material);
                return true;
            }
            return false;
        }

        internal static async Task<bool> HandleLiquidPhysics(BlockUpdate blockUpdate)
        {
            if (blockUpdate.block is null) { return false; }
            var block = blockUpdate.block.Value;
            var world = blockUpdate.world;
            var location = blockUpdate.position;
            int state = block.State;
            Vector belowPos = location + Vector.Down;
            
            // Handle the initial search for closet path downwards.
            // Just going to do a crappy pathfind for now. We can do
            // proper pathfinding some other time.
            if (state == 0)
            {
                var validPaths = new List<Vector>();
                var paths = new List<Vector>()
                {
                    { location + Vector.Forwards },
                    { location + Vector.Backwards },
                    { location + Vector.Left },
                    { location + Vector.Right }
                };

                foreach (var pathLoc in paths)
                {
                    if (world.GetBlock(pathLoc) is Block pathSide && (Block.Replaceable.Contains(pathSide.Material) || pathSide.IsFluid))
                    {
                        var pathBelow = world.GetBlock(pathLoc + Vector.Down);
                        if (pathBelow is Block pBelow && (Block.Replaceable.Contains(pBelow.Material) || pBelow.IsFluid))
                        {
                            validPaths.Add(pathLoc);
                        }
                    }
                }

                // if all directions are valid, or none are, use normal liquid spread physics instead
                if (validPaths.Count != 4 && validPaths.Count != 0)
                {
                    var path = validPaths[0];
                    var newBlock = new Block(block.BaseId, state + 1);
                    world.SetBlock(path, newBlock);
                    var neighborUpdate = new BlockUpdate(world, path, newBlock);
                    world.ScheduleBlockUpdate(neighborUpdate);
                    return false;
                }
            }

            if (state >= 8) // Falling water
            {
                // If above me is no longer water, than I should disappear too
                if (world.GetBlock(location + Vector.Up) is Block up && !up.IsFluid)
                {
                    world.SetBlock(location, Block.Air);
                    world.ScheduleBlockUpdate(new BlockUpdate(world, belowPos));
                    return false;
                }

                // Keep falling
                if (world.GetBlock(belowPos) is Block below && Block.Replaceable.Contains(below.Material))
                {
                    var newBlock = new Block(block.BaseId, state);
                    world.SetBlock(belowPos, newBlock);
                    world.ScheduleBlockUpdate(new BlockUpdate(world, belowPos, newBlock));
                    return false;
                }
                else
                {
                    // Falling water has hit something solid. Change state to spread.
                    state = 1;
                    world.SetBlockUntracked(location, new Block(block.BaseId, state));
                }
            }

            if (state < 8)
            {
                var horizontalNeighbors = new Dictionary<Vector, Block?>()
                {
                    { location + Vector.Forwards, world.GetBlock(location + Vector.Forwards) },
                    { location + Vector.Backwards, world.GetBlock(location + Vector.Backwards) },
                    { location + Vector.Left, world.GetBlock(location + Vector.Left) },
                    { location + Vector.Right, world.GetBlock(location + Vector.Right) }
                };

                // Check infinite source blocks
                if (state == 1 && block.Material == Material.Water)
                {
                    // if 2 neighbors are source blocks (state = 0), then become source block
                    int sourceNeighborCount = 0;
                    foreach (var (loc, neighborBlock) in horizontalNeighbors)
                    {
                        if (neighborBlock is Block neighbor && neighbor.Material == block.Material && neighbor.State == 0)
                        {
                            sourceNeighborCount++;
                        }
                    }
                    if (sourceNeighborCount > 1)
                    {
                        var newBlock = new Block(Material.Water);
                        world.SetBlock(location, newBlock);
                        return true;
                    }
                }

                if (state > 0)
                {
                    // On some side of the block, there should be another water block with a lower state.
                    int lowestState = state;
                    foreach (var (loc, neighborBlock) in horizontalNeighbors)
                    {
                        if (neighborBlock.Value.Material == block.Material)
                            lowestState = Math.Min(lowestState, neighborBlock.Value.State);
                    }

                    // If not, turn to air and update neighbors.
                    if (lowestState >= state && world.GetBlock(location + Vector.Up).Value.Material != block.Material)
                    {
                        world.SetBlock(location, Block.Air);
                        return true;
                    }
                }

                if (world.GetBlock(belowPos) is Block below)
                {
                    if (below.Material == block.Material) { return false; }
                    if (Block.Replaceable.Contains(below.Material))
                    {
                        var newBlock = new Block(block.BaseId, state + 8);
                        world.SetBlock(belowPos, newBlock);
                        var neighborUpdate = new BlockUpdate(world, belowPos, newBlock);
                        world.ScheduleBlockUpdate(neighborUpdate);
                        return false;
                    }
                }

                // the lowest level of water can only go down, so bail now.
                if (state == 7) { return false; }

                foreach (var (loc, neighborBlock) in horizontalNeighbors)
                {
                    if (neighborBlock is null) { continue; }
                    var neighbor = neighborBlock.Value;
                    if (Block.Replaceable.Contains(neighbor.Material) || (neighbor.Material == block.Material && neighbor.State > state + 1))
                    {
                        var newBlock = new Block(block.BaseId, state + 1);
                        world.SetBlock(loc, newBlock);
                        var neighborUpdate = new BlockUpdate(world, loc, newBlock);
                        world.ScheduleBlockUpdate(neighborUpdate);
                    }
                }
            }

            return false;
        }
    }
}

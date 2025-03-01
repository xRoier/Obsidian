﻿using Obsidian.API;
using Obsidian.ChunkData;
using Obsidian.Entities;
using Obsidian.Nbt;
using Obsidian.Utilities;
using Obsidian.Utilities.Collection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Obsidian.WorldData
{
    public class Region
    {
        public const int cubicRegionSizeShift = 5;
        public const int cubicRegionSize = 1 << cubicRegionSizeShift;

        public int X { get; }
        public int Z { get; }

        public bool IsDirty { get; private set; } = true;

        public string RegionFolder { get; }

        public ConcurrentDictionary<int, Entity> Entities { get; } = new();

        public int LoadedChunkCount => loadedChunks.Count;

        private DenseCollection<Chunk> loadedChunks { get; } = new(cubicRegionSize, cubicRegionSize);
        
        private readonly RegionFile regionFile;

        private readonly ConcurrentDictionary<Vector, BlockUpdate> blockUpdates = new();

        internal Region(int x, int z, string worldRegionsPath)
        {
            X = x;
            Z = z;
            RegionFolder = Path.Join(worldRegionsPath, "regions");
            Directory.CreateDirectory(RegionFolder);
            var filePath = Path.Join(RegionFolder, $"{X}.{Z}.mca");
            regionFile = new RegionFile(filePath, cubicRegionSize);

        }

        internal void AddBlockUpdate(BlockUpdate bu)
        {
            if (!blockUpdates.TryAdd(bu.position, bu))
            {
                blockUpdates[bu.position] = bu;
            }
        }

        internal async Task InitAsync()
        {
            await regionFile.InitializeAsync();
        }

        internal async Task FlushAsync()
        {
            foreach(Chunk c in loadedChunks) { SerializeChunk(c); }
            await regionFile.FlushToDiskAsync();
        }

        internal Chunk GetChunk((int X, int Z) relativePos) =>  GetChunk(relativePos.X, relativePos.Z);

        internal Chunk GetChunk(int relativeX, int relativeZ) => GetChunk(new Vector(relativeX, 0, relativeZ));

        internal Chunk GetChunk(Vector relativePosition)
        {
            var chunk = loadedChunks[relativePosition.X, relativePosition.Z];
            if (chunk is null)
            {
                chunk = GetChunkFromFile(relativePosition); // Still might be null but that's okay.
                loadedChunks[relativePosition.X, relativePosition.Z] = chunk;
            }
            return chunk;
        }

        private Chunk GetChunkFromFile(Vector relativePosition)
        {
            var compressedBytes = regionFile.GetChunkCompressedBytes(relativePosition);
            if (compressedBytes is null) { return null; }
            using Stream strm = new MemoryStream(compressedBytes);
            NbtReader reader = new(strm, NbtCompression.GZip);
            NbtCompound chunkNbt = reader.ReadNextTag() as NbtCompound;
            return GetChunkFromNbt(chunkNbt);
        }

        internal IEnumerable<Chunk> GeneratedChunks()
        {
            foreach(var c in loadedChunks)
            {
                if (c is not null && c.isGenerated)
                {
                    yield return c;
                }
            }
        }

        internal void SetChunk(Chunk chunk)
        {
            if (chunk is null) { return; } // I dunno... maybe we'll need to null out a chunk someday?
            var (x, z) = (NumericsHelper.Modulo(chunk.X, cubicRegionSize), NumericsHelper.Modulo(chunk.Z, cubicRegionSize));
            loadedChunks[x, z] = chunk;
        }

        internal void SerializeChunk(Chunk chunk)
        {
            var relativePosition = new Vector(NumericsHelper.Modulo(chunk.X, cubicRegionSize), 0, NumericsHelper.Modulo(chunk.Z, cubicRegionSize));
            NbtCompound chunkNbt = GetNbtFromChunk(chunk);

            using MemoryStream strm = new();
            using NbtWriter writer = new(strm, NbtCompression.GZip);

            writer.WriteTag(chunkNbt);
            writer.WriteInt("DataVersion", 2724);// Hardcoded version try to get data version through minecraft data and use data correctly

            writer.TryFinish();
            regionFile.SetChunkCompressedBytes(relativePosition, strm.ToArray());
        }

        internal async Task BeginTickAsync(CancellationToken cts)
        {
            var timer = new BalancingTimer(50, cts);
            while (await timer.WaitForNextTickAsync())
            {
                await Task.WhenAll(Entities.Select(entityEntry => entityEntry.Value.TickAsync()));

                List<BlockUpdate> neighborUpdates = new();
                List<BlockUpdate> delayed = new();

                foreach (var pos in blockUpdates.Keys)
                {
                    blockUpdates.Remove(pos, out var bu);
                    if (bu.delayCounter > 0)
                    {
                        bu.delayCounter--;
                        delayed.Add(bu);
                    }
                    else
                    {
                        bool updateNeighbor = await bu.world.HandleBlockUpdate(bu);
                        if (updateNeighbor) { neighborUpdates.Add(bu); }
                    }
                }
                delayed.ForEach(i => AddBlockUpdate(i));
                neighborUpdates.ForEach(u => u.world.BlockUpdateNeighbors(u));
            }
        }

        #region NBT Ops
        public static Chunk GetChunkFromNbt(NbtCompound chunkCompound)
        {
            int x = chunkCompound.GetInt("xPos");
            int z = chunkCompound.GetInt("zPos");

            var chunk = new Chunk(x, z)
            {
                isGenerated = true
            };

            foreach (var child in chunkCompound["Sections"] as NbtList)
            {
                var sectionCompound = child as NbtCompound;
                var secY = (int)sectionCompound.GetByte("Y");
                var states = sectionCompound["BlockStates"] as NbtArray<long>;//TODO
                var palettes = sectionCompound["Palette"] as NbtList;

                chunk.Sections[secY].BlockStorage.Storage = states.GetArray();

                var chunkSecPalette = (LinearBlockStatePalette)chunk.Sections[secY].Palette;
                foreach (NbtCompound palette in palettes)
                {

                    var block = new Block(palette.GetInt("Id"));
                    chunkSecPalette.GetIdFromState(block);
                }
            }

            chunk.BiomeContainer.Biomes = (chunkCompound["Biomes"] as NbtArray<int>).GetArray().ToList();

            foreach (var (name, heightmap) in chunkCompound["Heightmaps"] as NbtCompound)
            {
                var heightmapType = (HeightmapType)Enum.Parse(typeof(HeightmapType), name.Replace("_", ""), true);
                chunk.Heightmaps[heightmapType].data.Storage = ((NbtArray<long>)heightmap).GetArray();
            }

            return chunk;
        }

        public static NbtCompound GetNbtFromChunk(Chunk chunk)
        {
            var sectionsCompound = new NbtList(NbtTagType.Compound, "Sections");
            foreach (var section in chunk.Sections)
            {
                if (section.YBase is null) { throw new InvalidOperationException("Section Ybase should not be null"); }//THIS should never happen

                var palette = new NbtList(NbtTagType.Compound, "Palette");

                if (section.Palette is LinearBlockStatePalette linear)
                {
                    foreach (var stateId in linear.BlockStateArray)
                    {
                        if (stateId == 0)
                            continue;

                        var block = new Block(stateId);

                        palette.Add(new NbtCompound()//TODO redstone etc... has a lit metadata added when creating the palette
                            {
                                new NbtTag<string>("Name", block.UnlocalizedName),
                                new NbtTag<int>("Id", block.StateId)
                            });
                    }
                }

                var sec = new NbtCompound()
                    {
                        new NbtTag<byte>("Y", (byte)section.YBase),
                        palette,
                        new NbtArray<long>("BlockStates", section.BlockStorage.Storage)
                    };
                sectionsCompound.Add(sec);
            }

            var chunkCompound = new NbtCompound($"Level")
                {
                    new NbtTag<int>("xPos", chunk.X),
                    new NbtTag<int>("zPos", chunk.Z),
                    new NbtArray<int>("Biomes", chunk.BiomeContainer.Biomes),
                    new NbtCompound("Heightmaps")
                    {
                        new NbtArray<long>("MOTION_BLOCKING", chunk.Heightmaps[HeightmapType.MotionBlocking].data.Storage),
                        new NbtArray<long>("OCEAN_FLOOR", chunk.Heightmaps[HeightmapType.OceanFloor].data.Storage),
                        new NbtArray<long>("WORLD_SURFACE", chunk.Heightmaps[HeightmapType.WorldSurface].data.Storage),
                    },
                    sectionsCompound
                };
            return chunkCompound;
        }
        #endregion NBT Ops
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Text;
using OpenTK.Mathematics;
using Engine.Core;

namespace Engine
{
    public class World
    {
        public Dictionary<Vector2, Chunk> Chunks = new Dictionary<Vector2, Chunk>();
        public FastNoiseLite Noise { get; private set; }
        public FastNoiseLite OreNoise { get; private set; }
        public FastNoiseLite TreeNoise { get; private set; }
        public FastNoiseLite BiomeNoise { get; private set; }
        public List<Chunk> ActiveChunks { get; set; }
        public List<Chunk> PreviouslyActiveChunks { get; set; }
        public int Seed = 100000;
        public bool Initialized { get; set; } = false;
        public bool WindowInitialized { get; set; } = false;
        public System.Numerics.Vector3 AmbientColor = new System.Numerics.Vector3(1,1,1);
        public float AmbientStrength = 0.2f;
        public float AmbientIntensity = 1.0f;


        public World()
        {
            ActiveChunks = new List<Chunk>();
            PreviouslyActiveChunks = new List<Chunk>();
            this.InitNoise();
            GenerateChunks(true);
        }

        public void GenerateChunks(bool firstGen = false)
        {
            if (!firstGen)
            {
                this.InitNoise();
                this.Chunks = new Dictionary<Vector2, Chunk>();
            }
            
            for (int x = -Globals.RENDER_DISTANCE; x < Globals.RENDER_DISTANCE; x++)
            {
                for (int z = -Globals.RENDER_DISTANCE; z < Globals.RENDER_DISTANCE; z++)
                {
                    Vector2 chunkCoord = new Vector2(x, z);
                    Chunk chunk = new Chunk(chunkCoord, this);
                    this.Chunks.Add(chunkCoord, chunk);
                    chunk.IsActive = true;
                    this.ActiveChunks.Add(chunk);
                }
            }

            this.Initialized = true;
        }

        public VoxelType GetVoxel(Vector3 pos)
        {
            // check if voxel is in world
            if (!VoxelInWorld(pos)) return VoxelType.Air;
            var chunk = GetChunk(pos);
            // check if chunk exists
            if (chunk == null || chunk.Blocks == null) return VoxelType.Air;
            // calc voxel pos
            int x = (int)Math.Floor(pos.X) % Globals.CHUNK_WIDTH;
            int y = (int)Math.Floor(pos.Y);
            int z = (int)Math.Floor(pos.Z) % Globals.CHUNK_WIDTH;
            // make sure they are in a valid range
            if (x < 0) x = Globals.CHUNK_WIDTH - Math.Abs(x);
            if (z < 0) z = Globals.CHUNK_WIDTH - Math.Abs(z);
            return chunk.Blocks[x,y,z];
        }

        public VoxelType GetVoxel(Vector3I pos) => GetVoxel(pos.ToOTKVector3());

        public Chunk GetChunk(Vector3 pos)
        {
            int x = (int)Math.Floor(pos.X / Globals.CHUNK_WIDTH);
            int y = (int)Math.Floor(pos.Y);
            int z = (int)Math.Floor(pos.Z / Globals.CHUNK_WIDTH);

            if (x < Globals.MIN_RANGE || z < Globals.MIN_RANGE || x >= Globals.MAX_RANGE || z >= Globals.MAX_RANGE) return null;

            if (this.Chunks.TryGetValue(new Vector2(x, z), out Chunk c)) return c;
            else return null;
        }

        public Chunk GetChunk(Vector3I pos) => GetChunk(pos.ToOTKVector3());

        public bool VoxelInWorld(Vector3 pos)
        {
            return pos.X > Globals.MIN_RANGE && pos.X < Globals.MAX_RANGE * Globals.CHUNK_WIDTH && pos.Y >= 0 && pos.Y <= 127 && pos.Z > Globals.MIN_RANGE && pos.Z < Globals.MAX_RANGE * Globals.CHUNK_WIDTH;
        }

        public bool VoxelInWorld(Vector3I pos) => VoxelInWorld(pos.ToOTKVector3());

        /// <summary>
        /// Init Noise Generators
        /// </summary>
        public void InitNoise()
        {
            Noise = new FastNoiseLite(this.Seed);
            Noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            Noise.SetFrequency(0.0050f);
            BiomeNoise = new FastNoiseLite(this.Seed + 1000);
            BiomeNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            BiomeNoise.SetFrequency(0.0015f);
            OreNoise = new FastNoiseLite(this.Seed + 500);
            OreNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
            OreNoise.SetFrequency(0.5f);
            TreeNoise = new FastNoiseLite(this.Seed - 500);
            TreeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            TreeNoise.SetFrequency(0.5f);
        }

        public void Update(Player player, int frame)
        {
            if (!this.Initialized) return;

            UpdateChunks(player);

            // Build and update meshes sequentially in the main thread
            lock (this.Chunks)
            {
                foreach (Chunk c in this.Chunks.Values)
                {
                    if (!c.initialized)
                    {
                        Task.Run(() =>
                        {
                            c.GenerateData();
                        });
                    }

                    if (c.initialized && (!c.meshGenerated || c.needsUpdate))
                    {
                        Task.Run(() =>
                        {
                            c.PropagateLight();
                        });

                        c.BuildMeshDataAsync().Wait();
                        c.UpdateMeshData();
                    }
                }
            }
        }

        private void UpdateChunks(Player player)
        {
            // Get the current chunk of the player
            Chunk currentChunk = GetChunk(player.Transform.Position);

            // If the player has moved to a new chunk
            if (currentChunk != player.LastChunk && currentChunk != null)
            {
                // Clear the active chunks list
                ActiveChunks.Clear();

                // Get the position of the current chunk
                int chunkX = (int)Math.Floor(currentChunk.Position.X);
                int chunkZ = (int)Math.Floor(currentChunk.Position.Y);

                // Loop through neighboring chunks within render distance
                for (int x = chunkX - Globals.RENDER_DISTANCE; x <= chunkX + Globals.RENDER_DISTANCE; x++)
                {
                    for (int z = chunkZ - Globals.RENDER_DISTANCE; z <= chunkZ + Globals.RENDER_DISTANCE; z++)
                    {
                        // Ensure the chunk position is within valid range
                        if (x >= Globals.MIN_RANGE && z >= Globals.MIN_RANGE && x < Globals.MAX_RANGE && z < Globals.MAX_RANGE)
                        {
                            Vector2 chunkPos = new Vector2(x, z);

                            // Check if the chunk already exists in the world
                            if (!Chunks.TryGetValue(chunkPos, out Chunk chunk))
                            {
                                // If the chunk doesn't exist, create a new one and add it to the world
                                this.Chunks.Add(chunkPos, new Chunk(chunkPos, this));
                                this.Chunks[chunkPos].IsActive = true;
                                this.ActiveChunks.Add(this.Chunks[chunkPos]);
                            }
                            else
                            {
                                // Mark the chunk as active and add it to the list of active chunks
                                chunk.IsActive = true;
                                ActiveChunks.Add(chunk);
                            }
                        }
                    }
                }

                // Deactivate previously active chunks not in the new active list
                foreach (Chunk chunk in PreviouslyActiveChunks)
                {
                    if (!ActiveChunks.Contains(chunk))
                    {
                        chunk.IsActive = false;
                    }
                }

                // Update the previously active chunks list
                PreviouslyActiveChunks = new List<Chunk>(ActiveChunks);
            }

            // Update the player's last chunk reference
            player.LastChunk = currentChunk;
        }
    }
}

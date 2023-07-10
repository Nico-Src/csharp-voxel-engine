using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Engine
{
    public class World
    {
        public List<Vector2> SavedChunks = new List<Vector2>();
        public Dictionary<Vector2, Chunk> Chunks = new Dictionary<Vector2, Chunk>();
        public FastNoiseLite Noise { get; private set; }
        public FastNoiseLite OreNoise { get; private set; }
        public FastNoiseLite TreeNoise { get; private set; }
        public List<Chunk> ActiveChunks { get; set; }
        public List<Chunk> PreviouslyActiveChunks { get; set; }
        public LineRenderer LineRenderer { get; set; }
        public int Seed = 100000;
        public float LightLevel = 1.0f;
        public bool Initialized { get; set; } = false;

        public World()
        {
            this.SavedChunks = new List<Vector2>();
            this.LoadSavedChunks();
            ActiveChunks = new List<Chunk>();
            this.InitNoise();
            LineRenderer = new LineRenderer();
            GenerateChunks(true);
        }

        public void LoadSavedChunks()
        {
            if (Directory.Exists("Chunks"))
            {
                var files = Directory.GetFiles("Chunks");
                foreach(var file in files)
                {
                    var coords = file.Replace(@"Chunks\", String.Empty).Split('.')[0].Split('_');
                    Vector2 chunkCoord = new Vector2(int.Parse(coords[0]), int.Parse(coords[1]));
                    this.SavedChunks.Add(chunkCoord);
                }
            }
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
                    if (!this.SavedChunks.Contains(chunkCoord))
                    {
                        Chunk chunk = new Chunk(chunkCoord, this);
                        this.Chunks.Add(chunkCoord, chunk);
                        chunk.IsActive = true;
                        this.ActiveChunks.Add(chunk);
                    }
                    else this.LoadChunkFromFile(chunkCoord);
                    AddBorderLines(chunkCoord);
                }
            }

            this.Initialized = true;
        }

        public void LoadChunkFromFile(Vector2 chunkCoord)
        {
            Task.Run(() =>
            {
                VoxelType[,,] blocks = new VoxelType[Globals.CHUNK_WIDTH, Globals.CHUNK_HEIGHT, Globals.CHUNK_WIDTH];
                using (StreamReader reader = new StreamReader($"Chunks\\{chunkCoord.X}_{chunkCoord.Y}.sav"))
                {
                    for (int y = 0; y < Globals.CHUNK_HEIGHT; y++)
                    {
                        for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                        {
                            var line = reader.ReadLine();
                            var lineVoxels = line.Split(';');
                            for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
                            {
                                blocks[x, y, z] = (VoxelType)Enum.Parse(typeof(VoxelType), lineVoxels[x]);
                            }
                        }
                        reader.ReadLine();
                    }
                }

                lock (this.Chunks)
                {
                    Chunk chunk = new Chunk(chunkCoord, this, blocks);
                    if(!this.Chunks.TryAdd(chunkCoord, chunk))
                    {
                        var oldChunk = this.Chunks[chunkCoord];
                        oldChunk.Dispose();
                        this.Chunks[chunkCoord] = chunk;
                    }
                    chunk.IsActive = true;
                    this.ActiveChunks.Add(chunk);
                }
            });
        }

        public VoxelType GetVoxel(Vector3 pos)
        {
            if (!VoxelInWorld(pos)) return VoxelType.Air;
            var chunk = GetChunk(pos);
            if (chunk == null || chunk.Blocks == null) return VoxelType.Air;
            int x = (int)Math.Floor(pos.X) % Globals.CHUNK_WIDTH;
            int y = (int)Math.Floor(pos.Y);
            int z = (int)Math.Floor(pos.Z) % Globals.CHUNK_WIDTH;
            if (x < 0) x = Globals.CHUNK_WIDTH - Math.Abs(x);
            if (z < 0) z = Globals.CHUNK_WIDTH - Math.Abs(z);
            return chunk.Blocks[x,y,z];
        }

        public Chunk GetChunk(Vector3 pos)
        {
            int x = (int)Math.Floor(pos.X / Globals.CHUNK_WIDTH);
            int y = (int)Math.Floor(pos.Y);
            int z = (int)Math.Floor(pos.Z / Globals.CHUNK_WIDTH);

            if (x < Globals.MIN_RANGE || z < Globals.MIN_RANGE || x >= Globals.MAX_RANGE || z >= Globals.MAX_RANGE || y < 0 || y > 128) return null;

            if (this.Chunks.TryGetValue(new Vector2(x, z), out Chunk c))
            {
                return c;
            } else
            {
                return null;
            }
        }

        public bool VoxelInWorld(Vector3 pos)
        {
            return pos.X > Globals.MIN_RANGE && pos.X < Globals.MAX_RANGE * Globals.CHUNK_WIDTH && pos.Y >= 0 && pos.Y <= 127 && pos.Z > Globals.MIN_RANGE && pos.Z < Globals.MAX_RANGE * Globals.CHUNK_WIDTH;
        }

        /// <summary>
        /// Checks for chunks that need to be initialized or updated
        /// </summary>
        public void CheckForUpdates()
        {
            lock (this.Chunks)
            {
                // iterate over all chunks
                foreach (Chunk chunk in Chunks.Values)
                {
                    // initialize chunks that havent been initialized yet
                    if (!chunk.initialized)
                    {
                        chunk.GenerateData();
                    }
                    else if (chunk.needsUpdate && !chunk.dataReady)
                    {
                        chunk.BuildMeshData();
                    }
                    // update mesh data for chunks that are tagged to be updated
                    else if (chunk.needsUpdate && chunk.initialized && chunk.dataReady)
                    {
                        chunk.UpdateMeshData();
                    }
                }
            }
        }

        public void AddBorderLines(Vector2 pos)
        {
            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH, 0, pos.Y * Globals.CHUNK_WIDTH));
            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH, 164, pos.Y * Globals.CHUNK_WIDTH));

            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH + Globals.CHUNK_WIDTH, 0, pos.Y * Globals.CHUNK_WIDTH));
            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH + Globals.CHUNK_WIDTH, 164, pos.Y * Globals.CHUNK_WIDTH));

            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH, 164, pos.Y * Globals.CHUNK_WIDTH));
            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH + 16, 164, pos.Y * Globals.CHUNK_WIDTH));

            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH, 164, pos.Y * Globals.CHUNK_WIDTH));
            this.LineRenderer.AddLine(new Vector3(pos.X * Globals.CHUNK_WIDTH, 164, pos.Y * Globals.CHUNK_WIDTH + Globals.CHUNK_WIDTH));

            this.LineRenderer.UpdateLines();
        }

        public void RenderBorder(Matrix4 model, Matrix4 view, Matrix4 projection)
        {
            this.LineRenderer.Render(model, view, projection);
        }

        /// <summary>
        /// Init Noise Generators
        /// </summary>
        public void InitNoise()
        {
            Noise = new FastNoiseLite(this.Seed);
            Noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            Noise.SetFrequency(0.0050f);
            OreNoise = new FastNoiseLite(this.Seed + 500);
            OreNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2S);
            OreNoise.SetFrequency(0.5f);
            TreeNoise = new FastNoiseLite(this.Seed - 500);
            TreeNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            TreeNoise.SetFrequency(0.5f);
        }

        public void Update(Player player)
        {
            if (!this.Initialized) return;
            
            lock (this.Chunks)
            {
                this.PreviouslyActiveChunks = new List<Chunk>();
                foreach (Chunk chunk in this.ActiveChunks)
                {
                    this.PreviouslyActiveChunks.Add(chunk);
                }
            }

            player.CurrentChunk = this.GetChunk(player.Position);
            if (player.CurrentChunk != player.LastChunk && player.CurrentChunk != null)
            {
                this.ActiveChunks = new List<Chunk>();
                int chunkX = (int)Math.Floor(player.CurrentChunk.Position.X);
                int chunkZ = (int)Math.Floor(player.CurrentChunk.Position.Y);
                for (int x = chunkX - Globals.RENDER_DISTANCE; x <= chunkX + Globals.RENDER_DISTANCE; x++)
                {
                    for (int z = chunkZ - Globals.RENDER_DISTANCE; z <= chunkZ + Globals.RENDER_DISTANCE; z++)
                    {
                        if (x > Globals.MIN_RANGE && z > Globals.MIN_RANGE && x < Globals.MAX_RANGE && z < Globals.MAX_RANGE)
                        {
                            var chunkPos = new Vector2(x, z);
                            if (!this.Chunks.ContainsKey(chunkPos))
                            {
                                if (!this.SavedChunks.Contains(chunkPos))
                                {
                                    this.Chunks.Add(chunkPos, new Chunk(chunkPos, this));
                                    this.AddBorderLines(chunkPos);
                                    this.Chunks[chunkPos].IsActive = true;
                                    this.ActiveChunks.Add(this.Chunks[chunkPos]);
                                }
                                else
                                {
                                    this.LoadChunkFromFile(chunkPos);
                                }
                            }
                            else
                            {
                                this.ActiveChunks.Add(this.Chunks[chunkPos]);
                                this.Chunks[chunkPos].IsActive = true;
                            }
                        }
                    }
                }

                if (this.PreviouslyActiveChunks != this.ActiveChunks && this.PreviouslyActiveChunks != null)
                {
                    foreach (Chunk chunk in this.PreviouslyActiveChunks)
                    {
                        if (!this.ActiveChunks.Contains(chunk))
                        {
                            chunk.Dispose();
                            lock (this.Chunks)
                            {
                                this.Chunks.Remove(chunk.Position);
                            }
                        }
                    }
                }
            }
        }
    }
}

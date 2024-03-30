using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Compute.Native;
using System.Diagnostics;

namespace Engine
{
    public class Chunk
    {
        public VoxelType[,,] Blocks = new VoxelType[Globals.CHUNK_WIDTH, Globals.CHUNK_HEIGHT, Globals.CHUNK_WIDTH];
        public byte[,] HeightMap = new byte[Globals.CHUNK_WIDTH, Globals.CHUNK_WIDTH];
        public byte[,,] LightMap = new byte[Globals.CHUNK_WIDTH, Globals.CHUNK_HEIGHT, Globals.CHUNK_WIDTH];
        public Vector2 Position { get; set; }
        public List<float> Data { get; set; }

        private World world;

        private int vbo = -1;
        private int vao = -1;
        public bool initialized = false;
        public bool meshGenerated = false;
        public bool needsUpdate = true;
        public bool IsActive { get; set; } = true;
        public int BlockCount { 
            get
            {
                return Blocks.Length;
            } 
        }
        public float timer = 0f;

        public Chunk(Vector2 pos, World world)
        {
            this.Position = pos;
            this.Data = new List<float>();
            this.world = world;

            this.GenerateData();
        }

        public Chunk(Vector2 pos, World world, VoxelType[,,] blocks)
        {
            this.Position = pos;
            this.Data = new List<float>();
            this.world = world;
            this.initialized = true;
            this.Blocks = blocks;
        }

        public void GenerateData()
        {
            if(this.initialized) return;

            for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                {
                    var noise = Math.Abs(world.Noise.GetNoise(x + (this.Position.X * Globals.CHUNK_WIDTH) + 500f, z + (this.Position.Y * Globals.CHUNK_WIDTH) + 500f));
                    if (noise > 1) noise = .95f;
                    var height = (int)Math.Max(10, noise * Globals.CHUNK_HEIGHT);
                    this.HeightMap[x, z] = (byte)height;
                    if (height >= Globals.CHUNK_HEIGHT) height = Globals.CHUNK_HEIGHT - 1;

                    var biomeNoise = Math.Abs(world.BiomeNoise.GetNoise(x + this.Position.X * Globals.CHUNK_WIDTH, z + this.Position.Y * Globals.CHUNK_WIDTH));
                    var variant = Random.Shared.Next(0, 3);
                    for (int y = 0; y < height; y++)
                    {
                        if (y >= height - 1)
                        {
                            if (biomeNoise > 0.5)
                            {
                                AddVoxel((VoxelType)Enum.Parse(typeof(VoxelType), $"Grass_{variant+1}"), new Vector3(x, y, z));
                            }
                            else AddVoxel(VoxelType.Iron_Ore, new Vector3(x, y, z));
                        }
                        else if (y > height - 5) AddVoxel((VoxelType)Enum.Parse(typeof(VoxelType), $"Dirt_{variant + 1}"), new Vector3(x, y, z));
                        else
                        {
                            var prob = Math.Abs(world.OreNoise.GetNoise(x + (this.Position.X * Globals.CHUNK_WIDTH) + 1500f, y + 1500f, z + (this.Position.Y * Globals.CHUNK_WIDTH) + 1500f));
                            if (prob > 0.85) AddVoxel(VoxelType.Iron_Ore, new Vector3(x, y, z));
                            else AddVoxel((VoxelType)Enum.Parse(typeof(VoxelType), $"Stone_{variant + 1}"), new Vector3(x, y, z));
                        }

                        // TODO: Biomes and Structures

                        var grasProb = Math.Abs(world.TreeNoise.GetNoise(x + (this.Position.X * Globals.CHUNK_WIDTH) + 1500f, y + 1500f, z + (this.Position.Y * Globals.CHUNK_WIDTH) + 1500f));
                        if(grasProb > 0.75)
                        {
                            var rndHeight = Random.Shared.Next(3, 7);
                            for(int h = 1; h <= rndHeight; h++)
                            {
                                AddVoxel((VoxelType)Enum.Parse(typeof(VoxelType), $"Grass_{variant + 1}"), new Vector3(x, y + h, z));
                            }

                            AddVoxel(rndHeight > 4 ? VoxelType.Red : VoxelType.Blue, new Vector3(x, y + rndHeight + 1, z));
                        }
                    }
                }
            }

            // Initialize light map based on heightmap
            for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                {
                    int height = HeightMap[x, z]; // Get height from heightmap

                    for (int y = 0; y < Globals.CHUNK_HEIGHT; y++)
                    {
                        if (y >= height) // Above or at the heightmap level
                        {
                            this.LightMap[x, y, z] = 15; // Air block, full light intensity
                        }
                        else // Below the heightmap level
                        {
                            this.LightMap[x, y, z] = 0; // Solid block, no light
                        }
                    }
                }
            }

            this.PropagateLight();

            this.initialized = true;
        }

        public void PropagateLight()
        {
            for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
            {
                for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                {
                    for (int y = Globals.CHUNK_HEIGHT - 2; y >= 0; y--)
                    {
                        byte currentLightLevel = this.LightMap[x, y + 1, z]; // Cache the current light level
                        if (this.Blocks[x, y, z] == VoxelType.Air) continue;

                        if (this.Blocks[x, y, z] != VoxelType.Light)
                        {
                            if (currentLightLevel > 0) currentLightLevel--;
                            this.LightMap[x, y, z] = currentLightLevel;
                        }
                        else // No need to process further downward if it's already a light-emitting block
                        {
                            break;
                        }
                    }
                }
            }

            /* for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < Globals.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                    {
                        if (this.Blocks[x,y,z] == VoxelType.Light)
                        {
                            this.LightMap[x, y, z] = 15;
                            // PropagateLightToNeighbors(x, y, z, this.LightMap[x, y, z]);
                        }
                    }
                }
            } */
        }

        // Define the offsets for neighboring blocks
        int[,] neighborOffsets = new int[,] {
             { 1, 0, 0 }, { -1, 0, 0 }, // Right, Left
             { 0, 1, 0 }, { 0, -1, 0 }, // Top, Bottom
             { 0, 0, 1 }, { 0, 0, -1 }  // Front, Back
        };

        private void PropagateLightToNeighbors(int x, int y, int z, byte currentLight)
        {
            // Precompute chunk coordinates
            int chunkX = (int)Math.Floor(Position.X * Globals.CHUNK_WIDTH);
            int chunkZ = (int)Math.Floor(Position.Y * Globals.CHUNK_WIDTH);

            // Iterate over neighboring offsets
            for (int i = 0; i < neighborOffsets.GetLength(0); i++)
            {
                int nx = x + neighborOffsets[i, 0];
                int ny = y + neighborOffsets[i, 1];
                int nz = z + neighborOffsets[i, 2];

                // Check if the neighbor block is within bounds of the current chunk
                if (nx >= 0 && nx < Globals.CHUNK_WIDTH &&
                    ny >= 0 && ny < Globals.CHUNK_HEIGHT &&
                    nz >= 0 && nz < Globals.CHUNK_WIDTH)
                {
                    byte neighborLight = LightMap[nx, ny, nz];
                    byte newLight = (byte)(currentLight - 1);

                    if (currentLight > neighborLight + 1)
                    {
                        LightMap[nx, ny, nz] = newLight;
                        PropagateLightToNeighbors(nx, ny, nz, newLight);
                    }
                }
                else
                {
                    int localX = x + neighborOffsets[i, 0];
                    int localY = y + neighborOffsets[i, 1];
                    int localZ = z + neighborOffsets[i, 2];
                    int neighborChunkX = chunkX + localX;
                    int neighborChunkZ = chunkZ + localZ;

                    if (localX < 0 || localX >= Globals.CHUNK_WIDTH ||
                        localZ < 0 || localZ >= Globals.CHUNK_WIDTH ||
                        neighborChunkX < Globals.MIN_RANGE * Globals.CHUNK_WIDTH ||
                        neighborChunkZ < Globals.MIN_RANGE * Globals.CHUNK_WIDTH ||
                        neighborChunkX >= Globals.MAX_RANGE * Globals.CHUNK_WIDTH ||
                        neighborChunkZ >= Globals.MAX_RANGE * Globals.CHUNK_WIDTH)
                    {
                        continue;
                    }

                    Chunk neighborChunk = world.GetChunk(new Vector3(neighborChunkX, y, neighborChunkZ));

                    if (neighborChunk != null)
                    {
                        byte newLight = (byte)(currentLight - 1);
                        neighborChunk.LightMap[localX, y, localZ] = newLight;
                        neighborChunk.PropagateLightToNeighbors(localX, y, localZ, newLight);
                    }
                }
            }
        }

        /// <summary>
        /// Build Mesh of Chunk
        /// </summary>
        /*public void BuildMeshData()
        {
            if (this.Blocks == null) return;
            // Pre-allocate Data list with estimated size
            this.Data = new List<float>(Globals.CHUNK_WIDTH * Globals.CHUNK_HEIGHT * Globals.CHUNK_WIDTH * 6 * 6 * 6); // Max expected vertices per chunk

            for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < Globals.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                    {
                        if (this.Blocks[x, y, z] != VoxelType.Air) AddVoxelData(this.Blocks[x, y, z], new Vector3(x, y, z));
                    }
                }
            }

            this.meshGenerated = true;
        }*/

        public async Task BuildMeshDataAsync()
        {
            if (this.Blocks == null) return;
            this.Data = new List<float>();

            // Create a lock object for synchronization
            object lockObj = new object();

            // Divide the work into smaller sections or tasks
            List<Task> tasks = new List<Task>();
            int sectionWidth = Globals.CHUNK_WIDTH / Environment.ProcessorCount; // Divide the chunk width by the number of processor cores

            for (int x = 0; x < Globals.CHUNK_WIDTH; x += sectionWidth)
            {
                int startX = x;
                int endX = Math.Min(startX + sectionWidth, Globals.CHUNK_WIDTH);

                tasks.Add(BuildMeshSectionAsync(startX, endX, lockObj));
            }

            // Wait for all tasks to complete
            await Task.WhenAll(tasks);

            this.meshGenerated = true;
        }

        private async Task BuildMeshSectionAsync(int startX, int endX, object lockObj)
        {
            await Task.Run(() =>
            {
                for (int x = startX; x < endX; x++)
                {
                    for (int y = 0; y < Globals.CHUNK_HEIGHT; y++)
                    {
                        for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                        {
                            if (this.Blocks[x, y, z] != VoxelType.Air)
                            {
                                AddVoxelData(this.Blocks[x, y, z], new Vector3(x, y, z), LightMap[x, y, z], lockObj);
                            }
                        }
                    }
                }
            });
        }

        public void UpdateMeshData()
        {
            Stopwatch sw = Stopwatch.StartNew();
            sw.Start();
            // create vbo
            if (this.vbo == -1) this.vbo = GL.GenBuffer();
            // bind type of buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.vbo);
            // copy data into buffer
            GL.BufferData(BufferTarget.ArrayBuffer, this.Data.Count * sizeof(float), this.Data.ToArray(), BufferUsageHint.StaticDraw);

            if (this.vao == -1) this.vao = GL.GenVertexArray();

            // bind Vertex Array Object
            GL.BindVertexArray(this.vao);
            // copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, this.Data.Count * sizeof(float), this.Data.ToArray(), BufferUsageHint.StaticDraw);
            // set vertex attribute pointers
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 8 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 8 * sizeof(float), 6 * sizeof(float));

            GL.EnableVertexAttribArray(3);
            GL.VertexAttribPointer(3, 1, VertexAttribPointerType.Float, false, 8 * sizeof(float), 7 * sizeof(float));

            this.needsUpdate = false;
        }

        /// <summary>
        /// Marks Chunk to be updated
        /// </summary>
        public void MarkForUpdate()
        {
            this.needsUpdate = true;
        }

        public void Render(Shader shader, Matrix4 model, Matrix4 view, Matrix4 projection)
        {
            if (!this.IsActive || !this.initialized) return;
            shader.Use();

            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);

            // Set uniform for light data
            shader.SetVector3("ambientColor", new Vector3(world.AmbientColor.X, world.AmbientColor.Y, world.AmbientColor.Z));
            shader.SetFloat("ambientStrength", world.AmbientStrength);
            shader.SetFloat("ambientIntensity", world.AmbientIntensity);

            GL.BindVertexArray(this.vao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, this.Data.Count);
        }

        /// <summary>
        /// Add Voxel at the given position
        /// </summary>
        /// <param name="type"> type of the voxel (Air, Dirt, etc...) </param>
        /// <param name="pos"> position the voxel should be placed at </param>
        public void AddVoxel(VoxelType type, Vector3 pos)
        {
            if (this.Blocks == null) return;
            // check if voxel is out of bounds
            if(pos.X < 0 || pos.X >= Globals.CHUNK_WIDTH || pos.Y < 0 || pos.Y >= Globals.CHUNK_HEIGHT || pos.Z < 0 || pos.Z >= Globals.CHUNK_WIDTH) return;
            
            int x = (int)Math.Floor(pos.X);
            int y = (int)Math.Floor(pos.Y);
            int z = (int)Math.Floor(pos.Z);

            this.Blocks[x,y,z] = type;
        }
        
        /// <summary>
        /// Add voxel vertices, uvs and other values (ao) at the given position
        /// </summary>
        /// <param name="type"> type of the block </param>
        /// <param name="pos"> position where the position </param>
        public void AddVoxelData(VoxelType type, Vector3 pos, byte lightLevel, object lockObj)
        {
            lock (lockObj) // Acquire lock to synchronize access to the shared Data list
            {
                var globalPos = pos + new Vector3(this.Position.X * Globals.CHUNK_WIDTH, 0, this.Position.Y * Globals.CHUNK_WIDTH);

                for (int i = 0; i < 6; i++)
                {
                    // check if face is visible (if not skip voxel)
                    if (!FaceVisible(globalPos, i))
                    {
                        continue;
                    }

                    for (int j = 0; j < 6; j++)
                    {
                        Vector3 vertex = globalPos + Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, j]];

                        // Calculate ambient occlusion factor for the vertex
                        float aoFactor = CalculateAmbientOcclusionFactor(vertex);

                        // Add vertex and ambient occlusion factor to data
                        AddVertex(vertex, type, aoFactor, lightLevel);
                    }
                }
            }
        }

        private float CalculateAmbientOcclusionFactor(Vector3 vertex)
        {
            // Define offsets for sampling neighboring voxels
            Vector3[] offsets = new Vector3[]
            {
                new Vector3(1, 0, 0), new Vector3(-1, 0, 0),
                new Vector3(0, 1, 0), new Vector3(0, -1, 0),
                new Vector3(0, 0, 1), new Vector3(0, 0, -1)
            };

            int occupiedCount = 0;

            // Iterate over neighboring positions
            foreach (var offset in offsets)
            {
                Vector3 neighborPos = vertex + offset;

                if(world.GetVoxel(neighborPos) != VoxelType.Air) occupiedCount++;
            }

            // Normalize the occupied count to obtain an ambient occlusion factor in the range [0, 1]
            float aoFactor = (float)occupiedCount / offsets.Length;

            // Adjust the factor to make it lighter
            float lightFactor = 0.5f; // Adjust this value as needed
            aoFactor *= lightFactor;

            return aoFactor;
        }

        public void AddVertex(Vector3 vertex, VoxelType type, float aoFactor, byte lightLevel)
        {
            if (this.Data == null) return;

            this.Data.Add(vertex.X);
            this.Data.Add(vertex.Y);
            this.Data.Add(vertex.Z);

            var voxelColor = Globals.VOXEL_TYPES[type].Color;

            this.Data.Add(voxelColor.X);
            this.Data.Add(voxelColor.Y);
            this.Data.Add(voxelColor.Z);

            this.Data.Add(aoFactor);
            this.Data.Add(lightLevel / 15.0f);
        }

        /// <summary>
        /// check if voxel face should be rendered (visible or not)
        /// </summary>
        /// <param name="pos"> voxel position </param>
        /// <param name="faceIndex"> index of the face </param>
        /// <returns></returns>
        private bool FaceVisible(Vector3 pos, int faceIndex)
        {
            switch (faceIndex)
            {
                case 0: return world.GetVoxel(new Vector3(pos.X, pos.Y, pos.Z - 1)) == VoxelType.Air; // Back Face
                case 1: return world.GetVoxel(new Vector3(pos.X, pos.Y, pos.Z + 1)) == VoxelType.Air; // Front Face
                case 2: return world.GetVoxel(new Vector3(pos.X, pos.Y + 1, pos.Z)) == VoxelType.Air; // Top Face
                case 3: return world.GetVoxel(new Vector3(pos.X, pos.Y - 1, pos.Z)) == VoxelType.Air; // Bottom Face
                case 4: return world.GetVoxel(new Vector3(pos.X - 1, pos.Y, pos.Z)) == VoxelType.Air; // Left Face
                case 5: return world.GetVoxel(new Vector3(pos.X + 1, pos.Y, pos.Z)) == VoxelType.Air; // Right Face
            }
            return true;
        }

        public void Dispose()
        {
            this.Blocks = null;
            this.Data = null;
            this.HeightMap = null;
        }

        public override string ToString()
        {
            return $"{this.Position.X}x{this.Position.Y}";
        }
    }
}

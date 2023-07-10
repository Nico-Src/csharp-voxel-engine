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

namespace Engine
{
    public class Chunk
    {
        public VoxelType[,,] Blocks = new VoxelType[Globals.CHUNK_WIDTH, Globals.CHUNK_HEIGHT, Globals.CHUNK_WIDTH];
        public byte[,] HeightMap = new byte[Globals.CHUNK_WIDTH, Globals.CHUNK_WIDTH];
        public Vector2 Position { get; set; }
        public List<float> Data { get; set; }

        private World world;

        private int vbo = -1;
        private int vao = -1;
        public bool initialized = false;
        public bool needsUpdate = true;
        public bool dataReady = false;
        public bool generating = false;
        public bool building = false;
        public bool IsActive { get; set; } = false;
        private object _lock_obj = new object();
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
            if (generating) return;
            this.generating = true;
            Task.Run(() =>
            {
                for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
                {
                    for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                    {
                        var noise = Math.Abs(world.Noise.GetNoise(x + (this.Position.X * Globals.CHUNK_WIDTH) + 500f, z + (this.Position.Y * Globals.CHUNK_WIDTH) + 500f));
                        if (noise > 1) noise = .95f;
                        var height = (int)Math.Max(10, noise * Globals.CHUNK_HEIGHT);
                        this.HeightMap[x, z] = (byte)height;
                        if (height >= Globals.CHUNK_HEIGHT) height = Globals.CHUNK_HEIGHT - 1;
                        for (int y = 0; y < height; y++)
                        {
                            if (y >= height - 1) AddVoxel(VoxelType.Grass, new Vector3(x, y, z));
                            else if (y > height - 5) AddVoxel(VoxelType.Dirt, new Vector3(x, y, z));
                            else
                            {
                                var prob = Math.Abs(world.OreNoise.GetNoise(x + (this.Position.X * Globals.CHUNK_WIDTH) + 1500f, y + 1500f, z + (this.Position.Y * Globals.CHUNK_WIDTH) + 1500f));
                                if (prob > 0.85) AddVoxel(VoxelType.Iron_Ore, new Vector3(x, y, z));
                                else AddVoxel(VoxelType.Stone, new Vector3(x, y, z));
                            }

                            if(y == height - 1)
                            {
                                var val = Math.Abs(world.TreeNoise.GetNoise(x, z));
                                if (val > 0.80)
                                {
                                    AddVoxel(VoxelType.Wood, new Vector3(x, y + 1, z));
                                    AddVoxel(VoxelType.Wood, new Vector3(x, y + 2, z));
                                    AddVoxel(VoxelType.Wood, new Vector3(x, y + 3, z));
                                    AddVoxel(VoxelType.Wood, new Vector3(x, y + 4, z));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 5, z));
                                    
                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 5, z));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 5, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 5, z));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 5, z - 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 5, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 5, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 5, z - 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 5, z - 1));

                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 6, z));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 6, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 6, z));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 6, z - 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 6, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 6, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 6, z - 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 6, z - 1));

                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 7, z));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 7, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 7, z));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 7, z - 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 7, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x - 1, y + 7, z + 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 7, z - 1));
                                    AddVoxel(VoxelType.Leaves, new Vector3(x + 1, y + 7, z - 1));

                                    AddVoxel(VoxelType.Leaves, new Vector3(x, y + 8, z));
                                }
                            }
                        }
                    }

                    this.initialized = true;
                };
            });
            this.generating = false;
        }

        /// <summary>
        /// Build Mesh of Chunk
        /// </summary>
        public void BuildMeshData()
        {
            // if chunks doesnt need to be updated is already building return
            if (!needsUpdate || building) return;
            
            this.building = true;
            Task.Run(() =>
            {
                this.Data = new List<float>(); // clear previous Data

                lock (this._lock_obj)
                {
                    // render transparent voxels after all others
                    List<Vector3> transparentVoxels = new List<Vector3>();
                    
                    for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
                    {
                        for (int y = 0; y < Globals.CHUNK_HEIGHT; y++)
                        {
                            for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                            {
                                if (this.Blocks == null) return;

                                if (this.Blocks[x, y, z] != VoxelType.Air && this.Blocks[x, y, z] != VoxelType.Leaves) AddVoxelData(this.Blocks[x, y, z], new Vector3(x, y, z));
                                else if (this.Blocks[x, y, z] == VoxelType.Leaves) transparentVoxels.Add(new Vector3(x, y, z));
                            }
                        }
                    };

                    foreach(var v in transparentVoxels)
                    {
                        AddVoxelData(this.Blocks[(int)v.X, (int)v.Y, (int)v.Z], v);
                    }
                }

                this.dataReady = true;
            });
            this.building = false;
        }

        /// <summary>
        /// Marks Chunk to be updated
        /// </summary>
        public void MarkForUpdate()
        {
            this.dataReady = false;
            this.needsUpdate = true;
        }

        public void Render(Shader shader, Matrix4 model, Matrix4 view, Matrix4 projection, float lightLevel)
        {
            if (!this.IsActive || !this.initialized) return;
            shader.Use();

            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);
            shader.SetFloat("globalLightLevel", lightLevel);
            shader.SetFloat("timer", this.timer);
            shader.SetFloat("time", this.timer);

            GL.BindVertexArray(this.vao);
            lock (this._lock_obj)
            {
                try
                {
                    GL.DrawArrays(PrimitiveType.Triangles, 0, this.Data.Count);
                } catch(Exception ex)
                {

                }
            }
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
        public void AddVoxelData(VoxelType type, Vector3 pos)
        {
            var globalPos = pos + new Vector3(this.Position.X * Globals.CHUNK_WIDTH, 0, this.Position.Y * Globals.CHUNK_WIDTH);
            var transparent = Globals.VOXEL_TYPES[type].Transparent;

            for (int i = 0; i < 6; i++)
            {
                // check if face is visible (if not skip voxel)
                if (!FaceVisible(globalPos, i) && !transparent)
                {
                    continue;
                }

                lock (this._lock_obj)
                {
                    var light = GetLightLevel(globalPos);
                    AddVertex(globalPos + Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 0]], 0, i, type, pos, light);
                    AddVertex(globalPos + Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 1]], 1, i, type, pos, light);
                    AddVertex(globalPos + Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 2]], 2, i, type, pos, light);
                    AddVertex(globalPos + Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 3]], 3, i, type, pos, light);
                    AddVertex(globalPos + Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 4]], 4, i, type, pos, light);
                    AddVertex(globalPos + Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 5]], 5, i, type, pos, light);
                }
            }
        }

        public void AddVertex(Vector3 vertex, int vertexIndex, int faceIndex, VoxelType type, Vector3 voxelPos, float light)
        {
            // add vertex
            this.Data.Add(vertex.X);
            this.Data.Add(vertex.Y);
            this.Data.Add(vertex.Z);

            // get texture id for the given block at the given face index
            int texID = Globals.BLOCK_TEXTURES[type][faceIndex];
            // calculate uv coordinates based on the texture id
            float y = texID / Globals.ATLAS_SIZE_IN_VOXELS;
            float x = texID - (y * Globals.ATLAS_SIZE_IN_VOXELS);

            y *= Globals.NORMALIZED_ATLAS_SIZE;
            x *= Globals.NORMALIZED_ATLAS_SIZE;

            y = 1f - y - Globals.NORMALIZED_ATLAS_SIZE;

            // add uv belonging to the given vertex
            this.AddUV(new Vector2(x + (Globals.CUBE_UVS[vertexIndex].X * Globals.NORMALIZED_ATLAS_SIZE), y + (Globals.CUBE_UVS[vertexIndex].Y * Globals.NORMALIZED_ATLAS_SIZE)));
            this.AddLight(light);
        }

        /// <summary>
        /// Add uvs to mesh data
        /// </summary>
        /// <param name="uv"></param>
        public void AddUV(Vector2 uv)
        {
            this.Data.Add(uv.X);
            this.Data.Add(uv.Y);
        }

        public void AddLight(float lightLevel)
        {
            this.Data.Add(lightLevel);
        }

        /// <summary>
        /// Returns the light level for the given voxel
        /// </summary>
        /// <param name="pos"> Voxel position to get the lighting level for </param>
        /// <returns></returns>
        public float GetLightLevel(Vector3 pos)
        {
            float l = 1.0f;
            return l;
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
                case 0: return Globals.VOXEL_TYPES[world.GetVoxel(new Vector3(pos.X, pos.Y, pos.Z - 1))].Transparent; // Back Face
                case 1: return Globals.VOXEL_TYPES[world.GetVoxel(new Vector3(pos.X, pos.Y, pos.Z + 1))].Transparent; // Front Face
                case 2: return Globals.VOXEL_TYPES[world.GetVoxel(new Vector3(pos.X, pos.Y + 1, pos.Z))].Transparent; // Top Face
                case 3: return Globals.VOXEL_TYPES[world.GetVoxel(new Vector3(pos.X, pos.Y - 1, pos.Z))].Transparent; // Bottom Face
                case 4: return Globals.VOXEL_TYPES[world.GetVoxel(new Vector3(pos.X - 1, pos.Y, pos.Z))].Transparent; // Left Face
                case 5: return Globals.VOXEL_TYPES[world.GetVoxel(new Vector3(pos.X + 1, pos.Y, pos.Z))].Transparent; // Right Face
            }
            return true;
        }

        public void UpdateMeshData()
        {
            // save to file
            this.SaveToFile();
            // create vbo
            if(this.vbo == -1) this.vbo = GL.GenBuffer();
            // bind type of buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.vbo);
            // copy data into buffer
            GL.BufferData(BufferTarget.ArrayBuffer, this.Data.Count * sizeof(float), this.Data.ToArray(), BufferUsageHint.StaticDraw);

            if(this.vao == -1) this.vao = GL.GenVertexArray();

            // bind Vertex Array Object
            GL.BindVertexArray(this.vao);
            // copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, this.Data.Count * sizeof(float), this.Data.ToArray(), BufferUsageHint.StaticDraw);
            // set vertex attribute pointers
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));

            GL.EnableVertexAttribArray(2);
            GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, 6 * sizeof(float), 5 * sizeof(float));

            // reset bools
            this.needsUpdate = false;
            this.dataReady = false;
        }

        public void SaveToFile()
        {
            if (!Directory.Exists("Chunks")) Directory.CreateDirectory("Chunks");
            string file = $"Chunks/{this.Position.X}_{this.Position.Y}.sav";
            StringBuilder builder = new StringBuilder();

            for(int y = 0; y < Globals.CHUNK_HEIGHT; y++)
            {
                for (int z = 0; z < Globals.CHUNK_WIDTH; z++)
                {
                    for (int x = 0; x < Globals.CHUNK_WIDTH; x++)
                    {
                        if (x == 0) builder.Append((int)this.Blocks[x, y, z]);
                        else builder.Append($";{(int)this.Blocks[x, y, z]}");
                    }
                    builder.AppendLine();
                }
                builder.AppendLine();
            };

            lock (this._lock_obj)
            {
                File.WriteAllText(file, builder.ToString());
                this.world.SavedChunks.Add(this.Position);
            }
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

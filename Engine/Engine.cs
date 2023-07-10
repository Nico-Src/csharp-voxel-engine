using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;
using OpenTK.Mathematics;

namespace Engine
{
    public class Engine : GameWindow
    {
        public int Framerate { get; set; }
        public double Frametime { get; set; }
        public bool showConsole = false;
        public string consoleText = "";

        int frameCounter = 0;
        int frameUpdateRate = 10;
        double time = 0;
        bool showControls = false;
        public bool showStats = false;
        bool showChunkBorder = false;

        System.Numerics.Vector3 clearColor = new System.Numerics.Vector3(0.2f,0.2f,0.2f);

        Shader shader;
        GUI GUI;
        Texture textureAtlas;
        Player player;
        
        public World world;
        public HighlightBlock HighlightBlock;

        public Engine(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title }) {

        }

        /// <summary>
        /// Load handler for the engine
        /// </summary>
        protected override void OnLoad()
        {
            base.OnLoad();
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);
            

            // init gui
            this.GUI = new GUI(this.Size.X, this.Size.Y);
            this.HighlightBlock = new HighlightBlock();
            this.world = new World();
            this.player = new Player(this);

            CursorState = CursorState.Grabbed;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // load voxel shader
            shader = new Shader("../../../../shader.vert", "../../../../shader.frag");
            shader.Use();
            shader.SetInt("atlas", 0);

            // load textures
            this.textureAtlas = new Texture("../../../../atlas.png");
            this.textureAtlas.Use(TextureUnit.Texture0);

            GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1.0f);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            // Get keyboard input state
            KeyboardState input = KeyboardState;
            MouseState mouse = MouseState;

            if (input.IsKeyDown(Keys.Escape)) Close(); // Close window on escape

            // update framerate and frametime only at the given rate
            if (this.frameCounter % this.frameUpdateRate == 0)
            {
                // calc framerate and frametime
                this.Framerate = (int)Math.Floor((decimal)(1.0f / ImGui.GetIO().DeltaTime));
                this.Frametime = Math.Round(e.Time * 1000, 3);
            }

            this.player.ResetRay();
            this.world.Update(this.player);

            world.CheckForUpdates();

            this.player.Raycast(mouse, this.player.Camera.Front);

            if (this.showConsole)
            {
                
                if (input.IsKeyPressed(Keys.Enter))
                {
                    if (this.consoleText == "/fly") this.player.Fly = !this.player.Fly;
                    this.showConsole = false;
                    this.consoleText = "";
                }
                return;
            }
            
            // change cursorstate on f1
            if (input.IsKeyPressed(Keys.F1))
            {
                if (CursorState == CursorState.Grabbed)
                {
                    CursorState = CursorState.Normal;
                    // reset firstmove to true to dont jump to a new mouse position
                    this.player.firstMove = true;
                }
                else CursorState = CursorState.Grabbed;
            }

            if (input.IsKeyPressed(Keys.F11))
            {
                if(WindowState != WindowState.Fullscreen) WindowState = WindowState.Fullscreen;
                else WindowState = WindowState.Normal;
            }

            if (input.IsKeyPressed(Keys.T)) this.showConsole = true;
            if (input.IsKeyPressed(Keys.F3)) this.showStats = !this.showStats;
            if (input.IsKeyPressed(Keys.F6)) this.showChunkBorder = !this.showChunkBorder;
            if (input.IsKeyPressed(Keys.F7)) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            if (input.IsKeyPressed(Keys.F8)) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            if (input.IsKeyPressed(Keys.F9)) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            if (input.IsKeyPressed(Keys.C)) this.showControls = !this.showControls; // controls toggle

            this.player.HandleInput(input, mouse, e);

            this.player.LastChunk = this.player.CurrentChunk;
        }

        /// <summary>
        /// Frame render handler
        /// </summary>
        /// <param name="e"></param>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            frameCounter++;
            time += e.Time;

            this.GUI.Update(this, (float)e.Time);

            GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1.0f);
            // clean screen
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var model = Matrix4.Identity;
            var view = this.player.Camera.GetViewMatrix();
            var projection = this.player.Camera.GetProjectionMatrix();

            this.textureAtlas.Use(TextureUnit.Texture0);

            int totalChunks = 0;
            int renderedChunks = 0;

            // chunk mid coords
            var chunkMidX = Globals.CHUNK_WIDTH / 2;
            var chunkMidY = Globals.CHUNK_HEIGHT / 2;

            lock (this.world.Chunks)
            {
                totalChunks = world.Chunks.Count;
                foreach (var chunk in world.Chunks.Values)
                {
                    // check if chunk is in view frustum
                    if (this.player.Camera.Frustum.VolumeVsFrustum(chunk.Position.X * Globals.CHUNK_WIDTH + chunkMidX, chunkMidY, chunk.Position.Y * Globals.CHUNK_WIDTH + chunkMidX, Globals.CHUNK_WIDTH, Globals.CHUNK_HEIGHT, Globals.CHUNK_WIDTH))
                    {
                        chunk.Render(shader, model, view, projection, this.world.LightLevel);
                        chunk.timer += (float)e.Time;
                        renderedChunks++;
                    }
                }
            }

            if (this.showChunkBorder) world.RenderBorder(model, view, projection);

            this.HighlightBlock.Render(model, view, projection, new Vector3(HighlightBlock.Position.X, HighlightBlock.Position.Y, HighlightBlock.Position.Z));

            if (this.showStats)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(350, 250));
                ImGui.SetNextWindowBgAlpha(0.2f);
                ImGui.Begin("", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration);
                ImGui.Text($"FPS: {this.Framerate}");
                ImGui.Text($"Frametime: {this.Frametime}ms");
                ImGui.Text($"Chunks: {totalChunks} (Rendered: {renderedChunks})");
                if (this.player.CurrentChunk != null) ImGui.Text($"Chunk: {this.player.CurrentChunk.Position.X}, {this.player.CurrentChunk.Position.Y}");
                ImGui.Text($"Looking at: {this.HighlightBlock.Position.X}, {this.HighlightBlock.Position.Y}, {this.HighlightBlock.Position.Z}");
                ImGui.DragFloat("Speed", ref this.player.Camera.Speed);
                ImGui.DragInt("Break-Radius", ref this.player.BreakRadius, 1, 0, 50);
                ImGui.DragFloat("Light Level: ", ref this.world.LightLevel, 0.1f, 1f, 16f);
                ImGui.End();
            }

            ImGui.SetNextWindowPos(new System.Numerics.Vector2((this.Size.X / 2f)-250f, this.Size.Y - 65), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(480, 65));
            ImGui.SetNextWindowBgAlpha(0.6f);
            ImGui.Begin("Hotbar", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration);
            ImGui.Columns(8, "items", true);
            foreach(var voxel in Globals.BLOCK_TEXTURES.Keys)
            {
                if (voxel == VoxelType.Air || voxel == VoxelType.None) continue;
                var type = voxel;
                // get texture id for the given block at the given face index
                int texID = Globals.BLOCK_TEXTURES[type][0];
                // calculate uv coordinates based on the texture id
                float y = texID / Globals.ATLAS_SIZE_IN_VOXELS;
                float x = texID - (y * Globals.ATLAS_SIZE_IN_VOXELS);

                y *= Globals.NORMALIZED_ATLAS_SIZE;
                x *= Globals.NORMALIZED_ATLAS_SIZE;

                y = 1f - y - Globals.NORMALIZED_ATLAS_SIZE;

                var uv1 = new System.Numerics.Vector2(x, y + Globals.NORMALIZED_ATLAS_SIZE);
                var uv2 = uv1 + new System.Numerics.Vector2(Globals.NORMALIZED_ATLAS_SIZE, -Globals.NORMALIZED_ATLAS_SIZE);
                // add uv belonging to the given vertex
                ImGui.Image((IntPtr)this.textureAtlas.Handle, new System.Numerics.Vector2(50, 50), uv1, uv2, this.player.HotbarVoxel == voxel ? new System.Numerics.Vector4(1,1,1,1) : new System.Numerics.Vector4(.5f,.5f,.5f, 1));
                ImGui.NextColumn();
            }
            ImGui.End();

            if (this.showConsole)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, this.Size.Y - 40), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(350, 40));
                ImGui.SetNextWindowBgAlpha(0.5f);
                ImGui.Begin("Console", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration);
                ImGui.InputText("", ref consoleText, 250);
                ImGui.End();
            }

            this.GUI.Render();

            GUI.CheckGLError("End of frame");

            SwapBuffers();
        }

        /// <summary>
        /// Resize handler
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
             
            // resize viewport
            GL.Viewport(0, 0, Size.X, Size.Y);
            this.GUI.WindowResized(Size.X, Size.Y);
            this.player.Camera.AspectRatio = Size.X / (float)Size.Y;
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            shader.Dispose();
            this.GUI.Dispose();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            this.GUI.PressChar((char)e.Unicode);
            if(this.showConsole) this.consoleText += (char)e.Unicode;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if(e.Key == Keys.Backspace && this.consoleText.Length > 0 && this.showConsole) this.consoleText = this.consoleText.Remove(this.consoleText.Length - 1, 1);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if(e.OffsetY < 0)
            {
                var currentVoxel = (int)this.player.HotbarVoxel;
                currentVoxel++;
                currentVoxel = Math.Max(1, currentVoxel % (Globals.VOXEL_TYPES.Count));
                this.player.HotbarVoxel = (VoxelType)currentVoxel;
            } else if(e.OffsetY > 0)
            {
                var currentVoxel = (int)this.player.HotbarVoxel;
                currentVoxel--;
                if (currentVoxel == 0) currentVoxel = Globals.VOXEL_TYPES.Count - 1;
                this.player.HotbarVoxel = (VoxelType)currentVoxel;
            }

            this.GUI.MouseScroll(e.Offset);
        }
    }
}

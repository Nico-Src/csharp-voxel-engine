using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

using ImGuiNET;

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

        System.Numerics.Vector3 clearColor = new System.Numerics.Vector3(0.2f,0.2f,0.2f);

        Shader shader;
        GUI GUI;
        Player player;
        
        public World world;
        public HighlightBlock HighlightBlock;
        public Sphere Preview;

        public Engine(int width, int height, string title) : base(GameWindowSettings.Default, new NativeWindowSettings() { Size = (width, height), Title = title }) {

        }

        /// <summary>
        /// Load handler for the engine
        /// </summary>
        protected override void OnLoad()
        {
            base.OnLoad();
            Title += ": OpenGL Version: " + GL.GetString(StringName.Version);

            CursorState = CursorState.Grabbed;
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // load voxel shader
            shader = new Shader("../../../../shader.vert", "../../../../shader.frag");
            shader.Use();

            GL.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1.0f);

            // init gui
            this.GUI = new GUI(this.Size.X, this.Size.Y);
            this.HighlightBlock = new HighlightBlock();
            this.Preview = new Sphere(Vector3.Zero, 5f, Vector3.One);
            this.world = new World();
            this.player = new Player(this);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            this.world.WindowInitialized = true;

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
            this.world.Update(this.player, this.frameCounter);
            // raycast 
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

            if (input.IsKeyPressed(Keys.F11))
            {
                if(WindowState != WindowState.Fullscreen) WindowState = WindowState.Fullscreen;
                else WindowState = WindowState.Normal;
            }

            if (input.IsKeyPressed(Keys.T)) this.showConsole = true;
            if (input.IsKeyPressed(Keys.F3)) this.showStats = !this.showStats;
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

            int renderedChunks = 0;

            // chunk mid coords
            var chunkMidX = Globals.CHUNK_WIDTH / 2;
            var chunkMidY = Globals.CHUNK_HEIGHT / 2;

            lock (this.world.Chunks)
            {
                foreach (var chunk in world.Chunks.Values)
                {
                    // check if chunk is in view frustum
                    if (this.player.Camera.Frustum.VolumeVsFrustum(chunk.Position.X * Globals.CHUNK_WIDTH + chunkMidX, chunkMidY, chunk.Position.Y * Globals.CHUNK_WIDTH + chunkMidX, Globals.CHUNK_WIDTH, Globals.CHUNK_HEIGHT, Globals.CHUNK_WIDTH))
                    {
                        chunk.Render(shader, model, view, projection);
                        renderedChunks++;
                    }
                }
            }

            // render block highlight
            this.HighlightBlock.Render(view, projection);
            this.Preview.Render(view, projection);

            GUI.ApplyTheme();
            if (this.showStats)
            {
                ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, 0), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new System.Numerics.Vector2(350, 250));
                ImGui.SetNextWindowBgAlpha(0.2f);
                ImGui.Begin("", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDecoration);
                ImGui.Text($"FPS: {this.Framerate}");
                ImGui.Text($"Frametime: {this.Frametime}ms");
                ImGui.Text($"Chunks: {world.Chunks.Count} (Rendered: {renderedChunks})");
                if (this.player.CurrentChunk != null) ImGui.Text($"Chunk: {this.player.CurrentChunk.Position.X}, {this.player.CurrentChunk.Position.Y}");
                ImGui.Text($"Looking at: {this.HighlightBlock.Position.X}, {this.HighlightBlock.Position.Y}, {this.HighlightBlock.Position.Z}");
                ImGui.DragFloat("Speed", ref this.player.Camera.Speed);
                ImGui.ColorEdit3("Ambient Color", ref this.world.AmbientColor);
                ImGui.DragFloat("Ambient Strength", ref this.world.AmbientStrength, 0.01f, 0.01f, 1.0f);
                ImGui.DragFloat("Ambient Intensity", ref this.world.AmbientIntensity, 0.01f, -1.0f, 1.0f);
                if(ImGui.DragInt("Break Radius", ref this.player.BreakRadius))
                {
                    this.Preview.Resize(this.player.BreakRadius);
                }
                ImGui.End();
            }

            ImGui.End();

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

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            this.GUI.MouseScroll(e.Offset);
        }
    }
}

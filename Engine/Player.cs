using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Engine
{
    public class Player
    {
        public Camera Camera { get; set; }
        public float Height = 1.8f;
        public float Width = .8f;
        public Vector3 Position { get; set; }
        public bool Fly { get; set; }
        public VoxelType HotbarVoxel { get; set; } = VoxelType.Dirt;
        public float Velocity { get; set; }
        public float MaxVelocity { get; set; }
        public Chunk CurrentChunk { get; set; }
        public Chunk LastChunk { get; set; }

        public bool firstMove = true;
        public int BreakRadius = 0;

        private float distance = 0;
        private float maxDistance = 5;
        private float step = 0.25f;
        private Vector2 lastPos;
        private Engine engine;
        private const float BobbingSpeed = 1.5f;       // Speed of the head bobbing motion
        private const float BobbingAmount = 0.1f;    // Amount of head bobbing displacement

        private float headBobOffset;                  // Current head bob offset
        private float bobbingTimer;                   // Timer for head bobbing motion


        public Player(Engine engine)
        {
            Velocity = 0;
            MaxVelocity = 4;
            this.engine = engine;

            this.InitCamera();
        }

        public void InitCamera()
        {
            var noise = Math.Abs(this.engine.world.Noise.GetNoise(500f, 500f));
            if (noise > 1) noise = .95f;
            var height = Math.Max(10, noise * Globals.CHUNK_HEIGHT);
            // init camera
            this.Camera = new Camera(new Vector3(0, 0, 0), this.engine.Size.X / (float)this.engine.Size.Y);
            this.Position = new Vector3(0, height + this.Height + 1, 0);
            this.Camera.Position = this.Position + new Vector3(0, this.Height, 0);
        }

        public void HandleInput(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            bool grounded = this.engine.world.GetVoxel(this.Position) != VoxelType.Air;
            // bool forward = this.engine.world.GetVoxel(new Vector3(xPos, yPos, zPos + this.Width), true) == BlockType.Air && this.engine.world.GetVoxel(new Vector3(xPos, yPos + .5f, zPos + this.Width), true) == BlockType.Air;
            // bool backward = this.engine.world.GetVoxel(new Vector3(xPos, yPos, zPos - this.Width), true) == BlockType.Air && this.engine.world.GetVoxel(new Vector3(xPos, yPos + .5f, zPos - this.Width), true) == BlockType.Air;
            // bool left = this.engine.world.GetVoxel(new Vector3(xPos - this.Width, yPos, zPos), true) == BlockType.Air && this.engine.world.GetVoxel(new Vector3(xPos - this.Width, yPos + .5f, zPos), true) == BlockType.Air;
            // bool right = this.engine.world.GetVoxel(new Vector3(xPos + this.Width, yPos, zPos), true) == BlockType.Air && this.engine.world.GetVoxel(new Vector3(xPos + this.Width, yPos + .5f, zPos), true) == BlockType.Air;

            // multiply speed when shift is pressed
            float camSpeed = this.Camera.Speed * (input.IsKeyDown(Keys.LeftShift) ? 1.5f : 1f);
            Vector2 movVec = new Vector2(input.IsKeyDown(Keys.A) ? -1 : input.IsKeyDown(Keys.D) ? 1 : 0, input.IsKeyDown(Keys.W) ? 1 : input.IsKeyDown(Keys.S) ? -1 : 0);
            movVec.NormalizeFast();

            // calc velocity for movement vector
            if(this.Camera.Pitch <= -45) this.Position += new Vector3(this.Camera.Up.X * movVec.Y, 0, this.Camera.Up.Z * movVec.Y) * camSpeed * (float)e.Time;
            else if(this.Camera.Pitch >= 45) this.Position -= new Vector3(this.Camera.Up.X * movVec.Y, 0, this.Camera.Up.Z * movVec.Y) * camSpeed * (float)e.Time;
            else this.Position += new Vector3(this.Camera.Front.X * movVec.Y, 0, this.Camera.Front.Z * movVec.Y) * camSpeed * (float)e.Time;
            this.Position += new Vector3(this.Camera.Right.X * movVec.X, 0, this.Camera.Right.Z * movVec.X) * camSpeed * (float)e.Time;

            // jump / fly and descend controls
            if ((input.IsKeyDown(Keys.Space) && grounded) || (input.IsKeyDown(Keys.Space) && this.Fly)) this.Velocity = -1.75f;
            if ((input.IsKeyDown(Keys.LeftControl) && this.Fly)) this.Velocity = 1.75f;

            // if none of the fly controls is pressed, set velocity to 0
            if (!input.IsKeyDown(Keys.Space) && !input.IsKeyDown(Keys.LeftControl) && this.Fly) this.Velocity = 0;

            // decrease velocity from jump to fall again
            if (Velocity < MaxVelocity && !this.Fly)
            {
                Velocity += 0.01f;
            }

            // reset velocity once the player is grounded
            if ((grounded && this.Velocity > 0)) Velocity = 0;

            this.Position -= new Vector3(0, this.Velocity, 0) * this.Camera.Speed * (float)e.Time; // Up
            this.Camera.Position = this.Position + new Vector3(0, this.Height, 0);

            // Head bobbing only when walking
            if (!this.Fly)
            {
                // Calculate head bobbing motion
                float bobbingSpeed = camSpeed * BobbingSpeed;
                if (Math.Abs(movVec.X) < float.Epsilon && Math.Abs(movVec.Y) < float.Epsilon)
                {
                    bobbingTimer = 0f;  // Reset timer when no movement
                }
                else
                {
                    bobbingTimer += bobbingSpeed * (float)e.Time;
                }

                // Apply head bobbing offset to the camera's vertical position
                float bobbingOffset = (float)Math.Sin(bobbingTimer) * BobbingAmount;
                this.Camera.Position += new Vector3(0, bobbingOffset, 0);
            }

            if (this.engine.CursorState != CursorState.Grabbed) return;
            
            if (this.firstMove) // This bool variable is initially set to true.
            {
                this.lastPos = new Vector2(mouse.X, mouse.Y);
                this.firstMove = false;
                this.ResetRay();
            }
            else
            {
                // Calculate the offset of the mouse position
                var deltaX = mouse.X - this.lastPos.X;
                var deltaY = mouse.Y - this.lastPos.Y;
                this.lastPos = new Vector2(mouse.X, mouse.Y);

                // Apply the camera pitch and yaw (we clamp the pitch in the camera class)
                this.Camera.Yaw += deltaX * this.Camera.Sensitivity;
                this.Camera.Pitch -= deltaY * this.Camera.Sensitivity; // Reversed since y-coordinates range from bottom to top

                this.ResetRay();
            }
        }

        public void ResetRay()
        {
            this.distance = 0;
        }

        public void Raycast(MouseState mouse, Vector3 dir)
        {
            bool rayhit = false;
            var camPos = new Vector3(this.Camera.Position.X, this.Camera.Position.Y, this.Camera.Position.Z);
            Vector3 prevPos = camPos + (dir * step);
            // shoot ray and check if there is a voxel
            while (distance < maxDistance)
            {
                distance += step;

                Vector3 point = camPos + (dir * distance);

                int x = (int)Math.Floor(point.X);
                int y = (int)Math.Floor(point.Y);
                int z = (int)Math.Floor(point.Z);
                Vector3 voxelPos = new Vector3(x, y, z);

                var voxel = this.engine.world.GetVoxel(voxelPos);
                if (voxel != VoxelType.Air)
                {
                    rayhit = true;
                    this.engine.HighlightBlock.Position = new System.Numerics.Vector3(x, y, z);
                    this.engine.HighlightBlock.IsActive = true;
                    break;
                }
                prevPos = point;
            }

            if (!rayhit)
            {
                this.engine.HighlightBlock.IsActive = false;
            }

            // remove block on left click (if there is a block in front of the camera)
            if (mouse.IsButtonPressed(MouseButton.Left) && rayhit)
            {
                int x = (int)Math.Floor((this.engine.HighlightBlock.Position.X));
                int y = (int)Math.Floor(this.engine.HighlightBlock.Position.Y);
                int z = (int)Math.Floor((this.engine.HighlightBlock.Position.Z));

                if (BreakRadius == 0) this.ModifyBlock(x, y, z, VoxelType.Air);
                else
                {
                    for (int x2 = x - this.BreakRadius; x2 < x + this.BreakRadius; x2++)
                    {
                        for (int z2 = z - this.BreakRadius; z2 < z + this.BreakRadius; z2++)
                        {
                            for (int y2 = y - this.BreakRadius; y2 < y + this.BreakRadius; y2++)
                            {
                                this.ModifyBlock(x2, y2, z2, VoxelType.Air);
                            }
                        }
                    }
                }
            }

            if(mouse.IsButtonPressed(MouseButton.Right) && rayhit)
            {
                // use prevPos to place block before the one your looking at
                int x = (int)Math.Floor((prevPos.X));
                int y = (int)Math.Floor(prevPos.Y);
                int z = (int)Math.Floor((prevPos.Z));
                this.ModifyBlock(x, y, z, this.HotbarVoxel);
            }
        }

        public void ModifyBlock(int x, int y, int z, VoxelType type)
        {
            if (this.engine.showStats) return;
            
            var chunk = this.engine.world.GetChunk(new Vector3(x, y, z));
            if (chunk != null)
            {
                int voxelX = x % Globals.CHUNK_WIDTH;
                int voxelZ = z % Globals.CHUNK_WIDTH;
                if (voxelX < 0) voxelX = Globals.CHUNK_WIDTH - Math.Abs(voxelX);
                if (voxelZ < 0) voxelZ = Globals.CHUNK_WIDTH - Math.Abs(voxelZ);
                chunk.Blocks[voxelX, y, voxelZ] = type;
                chunk.MarkForUpdate();

                // check for neighbour chunks
                if (voxelZ == 0 && chunk.Position.Y > int.MinValue) // left neighbour
                {
                    var leftChunk = this.engine.world.GetChunk(new Vector3(x, y, z - 1));
                    if (leftChunk != null) leftChunk.MarkForUpdate();
                }

                if (voxelZ == 15 && chunk.Position.Y < int.MaxValue) // right neighbour
                {
                    var rightChunk = this.engine.world.GetChunk(new Vector3(x, y, z + 1));
                    if (rightChunk != null) rightChunk.MarkForUpdate();
                }

                if (voxelX == 0 && chunk.Position.X > int.MinValue) // in front
                {
                    var frontChunk = this.engine.world.GetChunk(new Vector3(x - 1, y, z));
                    if (frontChunk != null) frontChunk.MarkForUpdate();
                }

                if (voxelX == 15 && chunk.Position.X < int.MaxValue) // in back
                {
                    var backChunk = this.engine.world.GetChunk(new Vector3(x + 1, y, z));
                    if (backChunk != null) backChunk.MarkForUpdate();
                }

                distance = 0;
            }
        }
    }
}

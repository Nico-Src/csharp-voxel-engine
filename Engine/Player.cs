using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Engine.Core;

namespace Engine
{
    public class Player
    {
        /// <summary>
        /// The camera that is used to render the players view
        /// </summary>
        public Camera Camera { get; set; }

        /// <summary>
        /// Size of the Player (width, height, "depth")
        /// </summary>
        public Size Size { get; set; } = new Size(0.8f, 10.8f);

        /// <summary>
        /// Transform of the Player (Position, Rotation, Scale)
        /// </summary>
        public Transform Transform { get; set; }

        /// <summary>
        /// Temporary flying bool (TODO: Replace with Gamemode)
        /// </summary>
        public bool Fly { get; set; } = true;

        /// <summary>
        /// The Current Chunk the Player is in
        /// </summary>
        public Chunk CurrentChunk { get; set; }

        /// <summary>
        /// The Chunk that the Player was in the last frame (used for keeping track of which chunks are not in the players range anymore
        /// </summary>
        public Chunk LastChunk { get; set; }

        /// <summary>
        /// The radius of blocks getting destroyed
        /// </summary>
        public int BreakRadius = 5;

        /// <summary>
        /// The Players max distance he can reach
        /// </summary>
        public float PlayerReach { get; set; }

        /// <summary>
        /// Boolean that tells if the current mouse move is the first one (to initialize some variables)
        /// </summary>
        private bool firstMove = true;

        /// <summary>
        /// Current distance of the raycast
        /// </summary>
        private float distance = 0;

        /// <summary>
        /// Last moiuse position
        /// </summary>
        private Vector2 lastPos;

        /// <summary>
        /// Engine reference
        /// </summary>
        private Engine engine;

        public Player(Engine engine)
        {
            this.Transform = new Transform();
            
            this.PlayerReach = 500;
            
            this.engine = engine;
            this.Init();
        }

        /// <summary>
        /// Initialize Camera and Transforms
        /// </summary>
        public void Init()
        {
            // find height at spawn position
            var noise = Math.Abs(this.engine.world.Noise.GetNoise(500f, 500f));
            if (noise > 1) noise = .95f;
            var height = Math.Max(10, noise * Globals.CHUNK_HEIGHT);
            
            // init camera
            this.Camera = new Camera(new Vector3(0, 0, 0), this.engine.Size.X / (float)this.engine.Size.Y);
            // set position
            this.Transform.Position = new Vector3(0, height + this.Size.Height + 1, 0);
            // set camera parent to player
            this.Camera.Transform.Parent = this.Transform;
            // set camera position to player height
            this.Camera.Transform.Position = new Vector3(0, this.Size.Height, 0);
        }

        /// <summary>
        /// Handle Input
        /// </summary>
        /// <param name="input"> keyboard state (keys, ...) </param>
        /// <param name="mouse"> mouse state (buttons, ...) </param>
        /// <param name="e"> frame details (time, ...) </param>
        public void HandleInput(KeyboardState input, MouseState mouse, FrameEventArgs e)
        {
            // check if player is grounded (if voxel is beneath the player)
            bool grounded = this.engine.world.GetVoxel(this.Transform.Position) != VoxelType.Air;

            // multiply speed when shift is pressed
            float camSpeed = this.Camera.Speed * (input.IsKeyDown(Keys.LeftShift) ? 1.5f : 1f);

            // build movement vector based on which keys are pressed
            Vector2 movVec = new Vector2(input.IsKeyDown(Keys.A) ? -1 : input.IsKeyDown(Keys.D) ? 1 : 0, input.IsKeyDown(Keys.W) ? 1 : input.IsKeyDown(Keys.S) ? -1 : 0);
            // normalize movement vector to prevent faster diagonal movement
            movVec.NormalizeFast();

            // calculate velocity for movement vector (based on pitch different camera vectors must be used)
            if(this.Camera.Pitch <= -45) this.Transform.Position += new Vector3(this.Camera.Up.X * movVec.Y, 0, this.Camera.Up.Z * movVec.Y) * camSpeed * (float)e.Time;
            else if(this.Camera.Pitch >= 45) this.Transform.Position -= new Vector3(this.Camera.Up.X * movVec.Y, 0, this.Camera.Up.Z * movVec.Y) * camSpeed * (float)e.Time;
            else this.Transform.Position += new Vector3(this.Camera.Front.X * movVec.Y, 0, this.Camera.Front.Z * movVec.Y) * camSpeed * (float)e.Time;
            
            this.Transform.Position += new Vector3(this.Camera.Right.X * movVec.X, 0, this.Camera.Right.Z * movVec.X) * camSpeed * (float)e.Time;

            var yVel = 0f;
            // jump / fly and descend controls
            if ((input.IsKeyDown(Keys.Space) && grounded) || (input.IsKeyDown(Keys.Space) && this.Fly)) yVel = -1.75f;
            if ((input.IsKeyDown(Keys.LeftControl) && this.Fly)) yVel = 1.75f;

            this.Transform.Position -= new Vector3(0, yVel, 0) * this.Camera.Speed * (float)e.Time; // Update Position

            // change cursorstate on f1
            if (input.IsKeyPressed(Keys.F1))
            {
                if (this.engine.CursorState == CursorState.Grabbed)
                {
                    this.engine.CursorState = CursorState.Normal;
                    // reset firstmove to true to dont jump to a new mouse position
                    this.firstMove = true;
                }
                else this.engine.CursorState = CursorState.Grabbed;
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

        /// <summary>
        /// Reset ray distance
        /// </summary>
        public void ResetRay()
        {
            this.distance = 0;
        }

        /// <summary>
        /// Raycast in front of the camera to check if there is a voxel
        /// </summary>
        /// <param name="mouse"></param>
        /// <param name="dir"></param>
        public void Raycast(MouseState mouse, Vector3 dir)
        {
            bool rayhit = false;
            var camPos = this.Camera.Transform.Position;
            Vector3 prevPos = camPos + (dir * Globals.RAY_STEP);
            // create ray at camera position with the cameras direction
            Ray ray = new Ray(camPos, dir);
            // shoot ray and check if there is a voxel
            while (distance < this.PlayerReach)
            {
                distance += Globals.RAY_STEP;
                Vector3 point = ray.GetPoint(distance);
                Vector3I voxelPos = new Vector3I(point);
                VoxelType voxel = this.engine.world.GetVoxel(voxelPos);
                if (voxel != VoxelType.Air)
                {
                    // position and show highlightblock
                    this.engine.HighlightBlock.Position = voxelPos.ToOTKVector3();
                    this.engine.Preview.Position = voxelPos.ToOTKVector3() + new Vector3(0.5f, 0.5f, 0.5f);
                    this.engine.HighlightBlock.IsActive = true;
                    this.engine.Preview.IsActive = true;
                    rayhit = true;
                    break;
                }
                prevPos = point;
            }

            // hide highlight block if there is no voxel
            if (!rayhit)
            {
                this.engine.HighlightBlock.IsActive = false;
                this.engine.Preview.IsActive = false;
            }

            // remove block on left click (if there is a block in front of the camera)
            if (mouse.IsButtonPressed(MouseButton.Left) && rayhit)
            {
                // Define the radius of the sphere (e.g., 3 units)
                float sphereRadius = this.BreakRadius;

                // Get the center position (e.g., camera position)
                Vector3 centerPosition = this.engine.HighlightBlock.Position;

                // Iterate through all positions within the specified radius around the center position
                for (int x = (int)Math.Floor(centerPosition.X - sphereRadius); x <= (int)Math.Ceiling(centerPosition.X + sphereRadius); x++)
                {
                    for (int y = (int)Math.Floor(centerPosition.Y - sphereRadius); y <= (int)Math.Ceiling(centerPosition.Y + sphereRadius); y++)
                    {
                        for (int z = (int)Math.Floor(centerPosition.Z - sphereRadius); z <= (int)Math.Ceiling(centerPosition.Z + sphereRadius); z++)
                        {
                            // Check if the current position is within the sphere
                            if (Vector3.Distance(new Vector3(x, y, z), centerPosition) <= sphereRadius)
                            {
                                // Remove the block at the current position
                                this.ModifyBlock(x, y, z, VoxelType.Air);
                            }
                        }
                    }
                }
            }

            // place block on right click
            if(mouse.IsButtonPressed(MouseButton.Right) && rayhit)
            {
                // use prevPos to place block before the one your looking at
                Vector3I v = new Vector3I(prevPos);
                this.ModifyBlock(v.X, v.Y, v.Z, VoxelType.Light);
            }
        }

        /// <summary>
        /// Change voxel type at the given position to the given type
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="type"></param>
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

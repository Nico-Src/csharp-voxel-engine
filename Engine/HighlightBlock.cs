using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class HighlightBlock
    {
        public List<float> Data { get; set; }
        public bool IsActive { get; set; } = true;
        public System.Numerics.Vector3 Position { get; set; }
        public float Scale { get; set; } = 1.01f;
        private Shader Shader;
        private Texture Texture;
        private int VAO;
        private int VBO;
        public HighlightBlock()
        {
            this.Shader = new Shader("../../../../highlight_shader.vert", "../../../../highlight_shader.frag");
            this.Texture = new Texture("../../../../highlight.png");
            this.Data = new List<float>();
            this.InitData();
        }

        private void InitData()
        {
            for (int i = 0; i < 6; i++)
            {
                AddVertex(Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 0]], 0);
                AddVertex(Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 1]], 1);
                AddVertex(Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 2]], 2);
                AddVertex(Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 3]], 3);
                AddVertex(Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 4]], 4);
                AddVertex(Globals.CUBE_VERTICES[Globals.CUBE_TRIANGLES[i, 5]], 5);
            }

            this.InitBuffers();
        }

        private void InitBuffers()
        {
            // create vbo
            this.VBO = GL.GenBuffer();
            this.VAO = GL.GenVertexArray();

            // bind type of buffer
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VBO);
            // copy data into buffer
            GL.BufferData(BufferTarget.ArrayBuffer, this.Data.Count * sizeof(float), this.Data.ToArray(), BufferUsageHint.StaticDraw);

            // bind Vertex Array Object
            GL.BindVertexArray(this.VAO);
            // copy our vertices array in a buffer for OpenGL to use
            GL.BindBuffer(BufferTarget.ArrayBuffer, this.VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, this.Data.Count * sizeof(float), this.Data.ToArray(), BufferUsageHint.StaticDraw);
            // set vertex attribute pointers
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        }

        private void AddVertex(Vector3 vertex, int vertexIndex)
        {
            // add vertex
            this.Data.Add(vertex.X * this.Scale);
            this.Data.Add(vertex.Y * this.Scale);
            this.Data.Add(vertex.Z * this.Scale);

            this.AddUV(Globals.CUBE_UVS[vertexIndex]);
        }

        private void AddUV(Vector2 uv)
        {
            this.Data.Add(uv.X);
            this.Data.Add(uv.Y);
        }

        public void Render(Matrix4 model, Matrix4 view, Matrix4 projection, OpenTK.Mathematics.Vector3 pos)
        {
            if (!this.IsActive) return;
            GL.Clear(ClearBufferMask.DepthBufferBit);
            this.Texture.Use();
            this.Shader.Use();

            this.Shader.SetMatrix4("model", model);
            this.Shader.SetMatrix4("view", view);
            this.Shader.SetMatrix4("projection", projection);
            this.Shader.SetVector3("position", pos);

            GL.BindVertexArray(this.VAO);
            GL.DrawArrays(PrimitiveType.Triangles, 0, this.Data.Count);
        }
    }
}

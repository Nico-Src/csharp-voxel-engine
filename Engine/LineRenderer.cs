using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine
{
    public class LineRenderer
    {
        private int lineVAO;
        private int lineVBO;
        public float Opacity = 0.3f;
        Shader Shader { get; set; }
        List<float> Vertices { get; set; }

        public LineRenderer()
        {
            this.Shader = new Shader("../../../../line_shader.vert", "../../../../line_shader.frag");
            this.Vertices = new List<float>();
            InitBuffers();
        }

        private void InitBuffers()
        {
            lineVAO = GL.GenVertexArray();
            lineVBO = GL.GenBuffer();
        }

        public void UpdateLines()
        {
            // bind the Vertex Array Object first, then bind and set vertex buffer(s), and then configure vertex attributes(s).
            GL.BindVertexArray(lineVAO);
            GL.BindBuffer(BufferTarget.ArrayBuffer, lineVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, this.Vertices.Count * sizeof(float), this.Vertices.ToArray(), BufferUsageHint.StaticDraw);

            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(lineVAO);
        }

        public void Render(Matrix4 model, Matrix4 view, Matrix4 projection)
        {
            this.Shader.Use();
            this.Shader.SetMatrix4("model", model);
            this.Shader.SetMatrix4("view", view);
            this.Shader.SetMatrix4("projection", projection);
            this.Shader.SetFloat("opacity", Opacity);

            GL.BindVertexArray(lineVAO);
            GL.DrawArrays(PrimitiveType.Lines, 0, this.Vertices.Count);
        }

        public void AddLine(Vector3 pos)
        {
            this.Vertices.Add(pos.X);
            this.Vertices.Add(pos.Y);
            this.Vertices.Add(pos.Z);
        }
    }
}

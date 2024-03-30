using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace Engine
{
    public class Sphere
    {
        private int vao;
        private int vbo;
        private int vertexCount;

        public Vector3 Position { get; set; }
        public float Radius { get; set; }
        public Vector3 Color { get; set; }
        public bool IsActive { get; set; } = true;
        private Shader shader;

        public Sphere(Vector3 position, float radius, Vector3 color)
        {
            shader = new Shader("../../../../sphere.vert", "../../../../sphere.frag");
            Position = position;
            Radius = radius;
            Color = color;
            Initialize();
        }

        private void Initialize()
        {
            float[] vertices = GenerateVertices();
            vertexCount = vertices.Length / 3;

            // Generate vertex array and vertex buffer objects
            vao = GL.GenVertexArray();
            vbo = GL.GenBuffer();

            // Bind the vertex array object and vertex buffer object
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

            // Store the vertex data in the vertex buffer object
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

            // Set vertex attribute pointers
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Unbind the vertex array object
            GL.BindVertexArray(0);
        }

        private float[] GenerateVertices()
        {
            const int stacks = 20;
            const int slices = 20;

            float[] vertices = new float[(stacks + 1) * (slices + 1) * 3];

            int index = 0;
            for (int i = 0; i <= stacks; ++i)
            {
                double phi = Math.PI / 2 - i * Math.PI / stacks;
                for (int j = 0; j <= slices; ++j)
                {
                    double theta = j * 2 * Math.PI / slices;
                    float x = (float)(Radius * Math.Cos(phi) * Math.Cos(theta));
                    float y = (float)(Radius * Math.Sin(phi));
                    float z = (float)(Radius * Math.Cos(phi) * Math.Sin(theta));
                    vertices[index++] = x;
                    vertices[index++] = y;
                    vertices[index++] = z;
                }
            }

            return vertices;
        }

        public void Render(Matrix4 view, Matrix4 projection)
        {
            if (!IsActive) return;

            // Render the sphere first
            RenderSphere(view, projection);

            // Render the outline
            RenderOutline(view, projection);
        }

        private void RenderSphere(Matrix4 view, Matrix4 projection)
        {
            shader.Use();

            Matrix4 model = Matrix4.CreateTranslation(Position);
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);
            shader.SetVector3("color", new Vector3(Color.X, Color.Y, Color.Z));

            GL.BindVertexArray(vao);
            GL.DrawArrays(PrimitiveType.TriangleFan, 0, vertexCount);
            GL.BindVertexArray(0);
        }

        private void RenderOutline(Matrix4 view, Matrix4 projection)
        {
            // Increase the radius for the outline
            float outlineRadius = Radius * 1.1f;

            // Render the outline
            shader.Use();

            Matrix4 model = Matrix4.CreateTranslation(Position);
            shader.SetMatrix4("model", model);
            shader.SetMatrix4("view", view);
            shader.SetMatrix4("projection", projection);
            shader.SetVector3("color", new Vector3(1.0f, 1.0f, 1.0f)); // Black color for the outline

            GL.BindVertexArray(vao);
            GL.PointSize(2.0f); // Set the point size for drawing lines
            GL.DrawArrays(PrimitiveType.LineLoop, 0, vertexCount); // Draw the outline as lines
            GL.BindVertexArray(0);
        }

        public void Resize(float newRadius)
        {
            Radius = newRadius;
            Initialize(); // Re-initialize the sphere with the new radius
        }

        public void Dispose()
        {
            GL.DeleteBuffer(vbo);
            GL.DeleteVertexArray(vao);
        }
    }
}

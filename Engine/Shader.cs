using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

namespace Engine
{
    public class Shader
    {
        /// <summary>
        /// Location of the Shader
        /// </summary>
        public int Handle;

        private bool disposedValue = false;

        public Shader(string vertPath, string fragPath)
        {
            int VertexShader, FragmentShader;

            // Read shader source
            string VertexShaderSource = File.ReadAllText(vertPath);
            string FragmentShaderSource = File.ReadAllText(fragPath);

            // generate shaders
            VertexShader = GL.CreateShader(ShaderType.VertexShader);
            FragmentShader = GL.CreateShader(ShaderType.FragmentShader);

            // assign source
            GL.ShaderSource(VertexShader, VertexShaderSource);
            GL.ShaderSource(FragmentShader, FragmentShaderSource);

            // compile shaders
            GL.CompileShader(VertexShader);

            // check if shader compiled successfully
            GL.GetShader(VertexShader, ShaderParameter.CompileStatus, out int success);
            if(success == 0)
            {
                string info = GL.GetShaderInfoLog(VertexShader);
                Console.WriteLine(info);
            }

            GL.CompileShader(FragmentShader);

            // check if shader compiled successfully
            GL.GetShader(FragmentShader, ShaderParameter.CompileStatus, out success);
            if(success == 0)
            {
                string info = GL.GetShaderInfoLog(FragmentShader);
                Console.WriteLine(info);
            }

            // create program
            Handle = GL.CreateProgram();

            // attach shaders
            GL.AttachShader(Handle, VertexShader);
            GL.AttachShader(Handle, FragmentShader);

            // link program
            GL.LinkProgram(Handle);

            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out success);
            if(success == 0)
            {
                string info = GL.GetProgramInfoLog(Handle);
                Console.WriteLine(info);
            }

            // now that the shaders are linked they aren't needed anymore
            GL.DetachShader(Handle, VertexShader);
            GL.DetachShader(Handle, FragmentShader);
            GL.DeleteShader(VertexShader);
            GL.DeleteShader(FragmentShader);
        }

        /// <summary>
        /// Use Shader Program
        /// </summary>
        public void Use()
        {
            GL.UseProgram(Handle);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                GL.DeleteProgram(Handle);

                disposedValue = true;
            }
        }

        ~Shader()
        {
            GL.DeleteProgram(Handle);
        }

        /// <summary>
        /// Dispose Shader
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(Handle, attribName);
        }

        public void SetInt(string name, int value)
        {
            GL.UseProgram(Handle);
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform1(location, value);
        }

        public void SetMatrix4(string name, Matrix4 value)
        {
            GL.UseProgram(Handle);
            int location = GL.GetUniformLocation(Handle, name);
            GL.UniformMatrix4(location, true, ref value);
        }

        public void SetVector3(string name, Vector3 value)
        {
            GL.UseProgram(Handle);
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform3(location, value);
        }

        public void SetFloat(string name, float value)
        {
            GL.UseProgram(Handle);
            int location = GL.GetUniformLocation(Handle, name);
            GL.Uniform1(location, value);
        }
    }
}

using System;

namespace Engine
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // start engine
            using (Engine engine = new Engine(1920, 1080, "Engine"))
            {
                engine.Run();
                engine.WindowState = OpenTK.Windowing.Common.WindowState.Fullscreen;
            }
        }
    }
}
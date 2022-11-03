using System;
using System.Diagnostics;
using System.Security.Policy;
using SFML.Window;
using SFML.Graphics;
using SFML.System;
using OpenCL;
using System.Threading.Tasks;
using Cloo;
using Cloo.Bindings;
using System.IO;
using System.Linq;
using System.Threading;

namespace OPENCL_n_body
{
    class Program
    {

        const int WINDOW_WIDTH = 500;
        const int WINDOW_HEIGHT = 500;

        private static RenderWindow window;
        private static byte[] windowBuffer;

        static void Main()
        {
            Console.WriteLine("start");

            Environment env = new Environment(20000);
            //env.Environment3();


            window = new RenderWindow(new VideoMode(WINDOW_WIDTH, WINDOW_HEIGHT), "N-Body simulation", Styles.Default);
            window.Closed += new EventHandler(OnClose);

            windowBuffer = new byte[WINDOW_WIDTH * WINDOW_HEIGHT * 4];

            Texture windowTexture = new Texture(WINDOW_WIDTH, WINDOW_HEIGHT);
            windowTexture.Update(windowBuffer);
            Sprite windowSprite = new Sprite(windowTexture);
            
            long[] avg_time = new long[100];

            try
            {
                if (AcceleratorDevice.HasGPU)
                {
                    GPU.Init(env);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Init");

            while (window.IsOpen)
            {   
                window.DispatchEvents();

                Stopwatch sw1 = new Stopwatch(), sw2 = new Stopwatch();
                sw1.Start();
                sw2.Start();

                //env.Attract4();
                GPU.Run(env);
                //GPU.RunCPUasGPU(env);
                env.Move();

                sw1.Stop();
                long calctime = sw1.ElapsedMilliseconds;
                sw1.Restart();

                window.Clear();
                windowBuffer = new byte[WINDOW_WIDTH * WINDOW_HEIGHT * 4];
                DrawEnvironment(env);
                windowTexture.Update(windowBuffer);
                window.Draw(windowSprite);

                CircleShape circ = new CircleShape(2);
                circ.Position = new Vector2f((float)(env.particles[0].x * WINDOW_WIDTH), (float)(env.particles[0].y * WINDOW_HEIGHT));
                circ.FillColor = new Color(0xff, 0x00, 0x00);
                window.Draw(circ);

                window.Display();

                sw1.Stop();
                sw2.Stop();

                Array.Copy(avg_time, 1, avg_time, 0, avg_time.Length - 1);
                avg_time[^1] = sw2.ElapsedMilliseconds;
                /*
                Console.Write($"calc: {calctime}\tgra: {sw1.ElapsedMilliseconds}\t" +
                $"oa: {sw2.ElapsedMilliseconds}\t" +
                $"avg: {Math.Round((double)avg_time.Sum() / (double)avg_time.Length)}\t" +
                $"fps: {Math.Round(1.0 / ((double)sw2.ElapsedMilliseconds / 1000.0), 2)}\n"
                );
                */
                //Thread.Sleep(1);
            }
        }

        static void DrawEnvironment(Environment env)
        {
            DrawParticles(env);
        }

        static void DrawParticles(Environment env)
        {
            //foreach (Particle particle in env.particles)
            Parallel.ForEach(env.particles, particle =>
            {
                if (particle.x < 0 || particle.x >= 1.0 || particle.y < 0 || particle.y >= 1.0)
                    return;

                int x = (int)(particle.x * WINDOW_WIDTH);
                int y = (int)(particle.y * WINDOW_HEIGHT);

                int index = (y * WINDOW_WIDTH + x) * 4;

                windowBuffer[index] = 255;
                windowBuffer[index + 1] = 255;
                windowBuffer[index + 2] = 255;
                windowBuffer[index + 3] = 255;
            });

            CircleShape circ = new CircleShape(2);
            circ.Position = new Vector2f((float)(env.particles[0].x * WINDOW_WIDTH), (float)(env.particles[0].y * WINDOW_HEIGHT));
            circ.FillColor = new Color(0xff, 0x00, 0x00);
            window.Draw(circ);
        }

        static void OnClose(object sender, EventArgs e)
        {
            // Close the window when OnClose event is received
            RenderWindow window = (RenderWindow)sender;
            window.Close();
        }

    }
}

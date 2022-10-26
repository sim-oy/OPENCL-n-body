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

namespace OPENCL_n_body
{
    class Program
    {

        const int WINDOW_WIDTH = 500;
        const int WINDOW_HEIGHT = 500;

        private static RenderWindow window;
        private static byte[] windowBuffer;

        private static ComputeProgram program;

        static void Main()
        {
            Console.WriteLine("start");

            Environment env = new Environment(10);


            window = new RenderWindow(new VideoMode(WINDOW_WIDTH, WINDOW_HEIGHT), "N-Body simulation", Styles.Default);
            window.Closed += new EventHandler(OnClose);

            windowBuffer = new byte[WINDOW_WIDTH * WINDOW_HEIGHT * 4];

            Texture windowTexture = new Texture(WINDOW_WIDTH, WINDOW_HEIGHT);
            windowTexture.Update(windowBuffer);
            Sprite windowSprite = new Sprite(windowTexture);


            try
            {
                if (AcceleratorDevice.HasGPU)
                {
                    RunGPU(env);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("All Done");

            while (window.IsOpen)
            {
                window.DispatchEvents();

                Stopwatch sw1 = new Stopwatch(), sw2 = new Stopwatch();
                sw1.Start();
                sw2.Start();

                env.Attract();
                env.Move();

                sw1.Stop();
                Console.Write($"calc: {sw1.ElapsedMilliseconds}\t");
                sw1.Restart();

                window.Clear();
                windowBuffer = new byte[WINDOW_WIDTH * WINDOW_HEIGHT * 4];
                DrawEnvironment(env);
                windowTexture.Update(windowBuffer);
                window.Draw(windowSprite);
                window.Display();
                //Thread.Sleep(3000);

                sw1.Stop();
                Console.Write($"gra: {sw1.ElapsedMilliseconds}\t");
                sw2.Stop();
                Console.Write($"oa: {sw2.ElapsedMilliseconds}\n");
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
                /*CircleShape circ = new CircleShape(2);
                circ.Position = new Vector2f((float)(particle.x * WINDOW_WIDTH), (float)(particle.y * WINDOW_HEIGHT));
                circ.FillColor = new Color(0xff, 0xff, 0xff);
                window.Draw(circ);*/
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
        }

        static void OnClose(object sender, EventArgs e)
        {
            // Close the window when OnClose event is received
            RenderWindow window = (RenderWindow)sender;
            window.Close();
        }


        static void RunGPU(ComputeContext context, Environment env)
        {
            /*
            double[] particlesd = new double[env.particles.Length * 5];
            int i = 0;
            foreach (Particle particle in env.particles)
            {
                particlesd[i + 0] = particle.x;
                particlesd[i + 1] = particle.y;
                particlesd[i + 2] = particle.vx;
                particlesd[i + 3] = particle.vy;
                particlesd[i + 4] = particle.mass;

                i += 5;
            }*/
            double[] particlesd = { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0 };

            Console.WriteLine("\nStarted run on GPU");

            try
            {
                program = new ComputeProgram(context, env.GPUattract);
                ComputeProgramBuildNotifier notify = null;
                program.Build(null, null, notify, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            program.Dispose();

            OpenCL cl = new OpenCL()
            {
                Accelerator = AcceleratorDevice.GPU
            };
            OpenCL.OpenCL.SetKernel(env.GPUattract, cl);

            cl.Invoke("Attract", 0, env.particles.Length, particlesd);

            Console.WriteLine(particlesd);

            

        }

        private void notify(CLProgramHandle programHandle, IntPtr userDataPtr)
        {
            Console.WriteLine("Program build notification.");
            byte[] bytes = program.Binaries[0];
            Console.WriteLine("Beginning of program binary (compiled for the 1st selected device):");
            Console.WriteLine(BitConverter.ToString(bytes, 0, 24) + "...");
        }

    }
}

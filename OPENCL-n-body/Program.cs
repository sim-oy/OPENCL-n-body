using System;
using System.Diagnostics;
using System.Security.Policy;
using SFML.Window;
using SFML.Graphics;
using SFML.System;
using OpenCL;
using System.Threading.Tasks;

namespace OPENCL_n_body
{
    class Program
    {

        static string IsPrime
        {
            get
            {
                return @"
                kernel void GetIfPrime(global int* message) 
                {
                    int index = get_global_id(0);
                    printf(""%s"", message);
                    int upperl=(int)sqrt((float)message[index]);
                    for(int i=2;i<=upperl;i++)
                    {
                        if(message[index]%i==0)
                        {
                            //printf("" %d / %d\n"",index,i );
                            message[index]=0;
                            return;
                        }
                    }
                    for(int i=0;i<1000000;i++)
                    {
                        if(i==999999)
                        {
                            printf('%s', i);
                        }
                    }
                    //printf("" % d"",index);
                }";
            }
        }

        const int WINDOW_WIDTH = 500;
        const int WINDOW_HEIGHT = 500;

        private static RenderWindow window;
        private static byte[] windowBuffer;

        static void Main()
        {
            Console.WriteLine("start");

            Environment env = new Environment(20000);


            window = new RenderWindow(new VideoMode(WINDOW_WIDTH, WINDOW_HEIGHT), "N-Body simulation", Styles.Default);
            window.Closed += new EventHandler(OnClose);

            windowBuffer = new byte[WINDOW_WIDTH * WINDOW_HEIGHT * 4];

            Texture windowTexture = new Texture(WINDOW_WIDTH, WINDOW_HEIGHT);
            windowTexture.Update(windowBuffer);
            Sprite windowSprite = new Sprite(windowTexture);

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



            int[] ArrayB = new int[5];

            try
            {
                if (AcceleratorDevice.HasGPU)
                {
                    RunGPU(ArrayB);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("All Done");
            Console.ReadKey();
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


        static void RunGPU(int[] WorkSet)
        {
            Console.WriteLine("\nRun on GPU");

            EasyCL cl = new EasyCL()
            {
                Accelerator = AcceleratorDevice.GPU
            };
            cl.LoadKernel(IsPrime);
            //cl.Invoke("GetIfPrime", 0, 1, WorkSet);    //OpenCL uses a Cache. Real speed after that
            Stopwatch time = Stopwatch.StartNew();

            cl.Invoke("GetIfPrime", 0, WorkSet.Length, WorkSet);

            time.Stop();
            double performance = WorkSet.Length / (1000000.0 * time.Elapsed.TotalSeconds);
            Console.WriteLine("\t" + performance.ToString("0.00") + " MegaPrimes/Sec");
        }

    }
}

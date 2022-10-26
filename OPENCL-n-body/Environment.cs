using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENCL_n_body
{
    class Environment
    {
        const double G = 0.00000001;

        public Particle[] particles;

        public Environment(int particleAmount)
        {
            particles = new Particle[particleAmount];

            Random rng = new Random();

            for (int i = 0; i < particleAmount; i++)
            {
                particles[i] = new Particle(rng.NextDouble(), rng.NextDouble(), rng.NextDouble());
            }

            //particles[particleAmount] = new Particle(0.5, 0.5, 500);
        }

        public void Move()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Move();
            }
        }

        public string GPUattract
        {
            get
            {
                return @"
                kernel double Attract(global double* message)
                {
                    int index = get_global_id(0);
                    //printf(""%lf\n"", (double)message[index]);
                    if ((double)message[index] == (double)10)
                    {
                        printf(""%d\n"", index);
                        return 6.4;
                    }
                }";
            }
        }

        public void Attract()
        {
            Parallel.For(0, particles.Length, i =>
            {
                double sumX = 0, sumY = 0;
                for (int j = 0; j < particles.Length; j++)
                {
                    if (i == j)
                        continue;

                    double distanceX = particles[j].x - particles[i].x;
                    double distanceY = particles[j].y - particles[i].y;
                    double dist = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

                    double b = G * particles[j].mass / (dist + 0.00001);

                    sumX += distanceX * b;
                    sumY += distanceY * b;
                }

                particles[i].vx += sumX;
                particles[i].vy += sumY;
            });
        }


        public void Attract2()
        {
            Parallel.For(0, particles.Length, i =>
            {
                double sumXi = 0, sumYi = 0;
                for (int j = i + 1; j < particles.Length; j++)
                {
                    //Console.WriteLine($"{i}\t{j}");
                    double distanceX = particles[j].x - particles[i].x;
                    double distanceY = particles[j].y - particles[i].y;
                    double dist = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

                    double b = G / (dist + 0.00001);

                    double Ai = particles[j].mass * b;
                    double Aj = particles[i].mass * b;

                    sumXi += distanceX * Ai;
                    sumYi += distanceY * Ai;

                    //sumXi += distanceX * b;
                    //sumYi += distanceY * b;

                    particles[j].vx += -distanceX * Aj;
                    particles[j].vy += -distanceY * Aj;
                }

                particles[i].vx += sumXi;
                particles[i].vy += sumYi;
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENCL_n_body
{
    class Environment
    {
        public const double G = 0.0000000001;

        public Particle[] particles;

        public Environment(int particleAmount)
        {
            particles = new Particle[particleAmount];

            Random rng = new Random(0);

            for (int i = 0; i < particleAmount; i++)
            {
                particles[i] = new Particle(rng.NextDouble() * 0.2 + 0.4, rng.NextDouble() * 0.2 + 0.4, 0, 0, rng.NextDouble());
            }

            //particles[particleAmount] = new Particle(0.5, 0.5, 0, 0, 500);
        }

        public void Environment2()
        {
            particles = new Particle[2];

            Random rng = new Random();

            particles[0] = new Particle(0.5, 0.5, 0, 0, 5000);

            //particles[1] = new Particle(0.5, 0.45, 0.0133, -0.001, 0.0001);//1.5
            particles[1] = new Particle(0.5, 0.45, 0.01335 * 3, -0.00001, 0.0001);
        }

        public void Environment3()
        {
            particles = new Particle[2];

            Random rng = new Random();

            particles[0] = new Particle(0.5, 0.25, 0.00007, 0, 1.0);

            //particles[1] = new Particle(0.5, 0.45, 0.0133, -0.001, 0.0001);//1.5
            particles[1] = new Particle(0.5, 0.75, -0.00007, 0, 1.0);
        }

        public void Environment4()
        {
            particles = new Particle[2];

            Random rng = new Random();

            particles[0] = new Particle(0.5, 0.25, 0, 0, 1.0);

            //particles[1] = new Particle(0.5, 0.45, 0.0133, -0.001, 0.0001);//1.5
            particles[1] = new Particle(0.5, 0.75, 0, 0, 1.0);
        }

        public void Move()
        {
            double systemMomentum = 0;
            for (int i = 0; i < particles.Length; i++)
            {
                //Console.WriteLine($"{i}: {particles[i].vx}, {particles[i].vx}");

                particles[i].Move();
                systemMomentum += particles[i].vx + particles[i].vy;
            }
            //particles[particles.Length - 1].x = 0.5;
            //particles[particles.Length - 1].y = 0.5;
            //Console.WriteLine(systemMomentum);
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
                    double x2_y2 = distanceX * distanceX + distanceY * distanceY;// 3/2root(x)
                    double dist = Math.Sqrt(x2_y2 * x2_y2 * x2_y2);

                    double b = G * particles[j].mass / (dist + 0.000001);

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
                    double x2_y2 = distanceX * distanceX + distanceY * distanceY;
                    double dist = Math.Sqrt(x2_y2 * x2_y2 * x2_y2);

                    double b = G / (dist + 0.000001);

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

        public void Attract3()
        {
            //Parallel.For(0, particles.Length, i =>
            for (int i = 0; i < particles.Length; i++)
            {
                double sumX = 0, sumY = 0;
                for (int j = 0; j < particles.Length; j++)
                {
                    if (i == j)
                        continue;

                    double distanceX = particles[j].x - particles[i].x;
                    double distanceY = particles[j].y - particles[i].y;

                    double x2_y2 = distanceX * distanceX + distanceY * distanceY;

                    double dist = Math.Sqrt(x2_y2);

                    double sx = distanceX / dist;
                    double sy = distanceY / dist;

                    //double dist = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

                    //double b = G * ((particles[j].mass * particles[i].mass) / (dist * dist));
                    double f = G * ((particles[j].mass * particles[i].mass) / x2_y2);

                    sumX += f * sx;
                    sumY += f * sy;
                }

                particles[i].vx += sumX / particles[i].mass;
                particles[i].vy += sumY / particles[i].mass;
            }//);
        }

        public void CPURun(float[] input_X, int size_X, float G1)
        {
            //for (int i = 0; i < size_X; i++)
            Parallel.For(0, size_X, i =>
            {
                for (int j = 0; j < size_X; j++)
                {
                    if (i == j)
                        continue;

                    float distanceX = input_X[j * 5] - input_X[i * 5];
                    float distanceY = input_X[j * 5 + 1] - input_X[i * 5 + 1];
                    float x2_y2 = distanceX * distanceX + distanceY * distanceY;

                    float dist = (float)Math.Sqrt(x2_y2 * x2_y2);
                    //float dist = x2_y2 * x2_y2;
                    //if (i == 0 && j == 1)
                    //    Console.WriteLine($"{x2_y2:0.0000000000000000000000000000}");

                    float b = G1 * input_X[j * 5 + 4] / (dist + 0.000001f);

                    input_X[i * 5 + 2] += distanceX * b;
                    input_X[i * 5 + 3] += distanceY * b;
                }
            });

            //Move
            for (int i = 0; i < size_X; i++)
            {
                float vx = input_X[i * 5 + 2];
                float vy = input_X[i * 5 + 3];

                input_X[i * 5] += vx;
                input_X[i * 5 + 1] += vy;
            }
        }
    }
}

/*
#include <stdio.h>
#include <math.h>

void Attract(float* input_X, int size_X, float* output_Z)
{
    for (int i = 0; i < size_X; i++)
    {
        float G = 0.00000001;
        float xi = input_X[i * 5];
        float yi = input_X[i * 5 + 1];

        float sumX = 0, sumY = 0;
        for (int j = 0; j < size_X; j++)
        {
            if (i == j)
            {
                continue;
            }
            
            float distanceX = input_X[j * 5] - xi;
            float distanceY = input_X[j * 5 + 1] - yi;
            
            float dist = sqrt(distanceX * distanceX + distanceY * distanceY);
            
            float b = G * input_X[j * 5 + 4] / (dist + 0.00001);
            //printf("%0.20f\t%0.20f\t%0.20f\n", G, input_X[j * 5 + 4], (dist + 0.00001));
            sumX += distanceX * b;
            sumY += distanceY * b;
            
            
        }

        output_Z[i * 2] += sumX;
        output_Z[i * 2 + 1] += sumY;
        
        //printf("%lf\n", output_Z[i * 2 + 1]);
    }
    printf("end\n");
}

int main()
{
    printf("Hello World\n");

    int size_X = 3;
    float input[3 * 5] = { 0.4, 0.2, 0, 0, -0.48, 0.4, -0.2, 0, 0, 0.48, -0.4, 0.2, 0, 0, 0.48 };
    float output[3 * 2];

    Attract(input, size_X, output);
    for (int i = 0; i < size_X * 2; i++)
    {
        printf("%0.20f\n", output[i]);
    }

    return 0;
}
*/

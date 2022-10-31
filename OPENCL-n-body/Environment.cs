using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPENCL_n_body
{
    class Environment
    {
        public const double G = 0.00000001;

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
                kernel void Attract(global float * input_X, int size_X, float G, global float * output_Z)
                {
    
                    int i = get_global_id(0);

                    //printf(""o: %0.20f\t%0.20f\n"", input_X[i * 2], input_X[i * 2 + 1]);
                    
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
                        //printf(""%0.20f\t%0.20f\t%0.20f\n"", G, input_X[j * 5 + 4], (dist + 0.00001));
                        sumX += distanceX * b;
                        sumY += distanceY * b;
            
            
                    }

                    output_Z[i * 2] += input_X[i * 5 + 2] + sumX;
                    output_Z[i * 2 + 1] += input_X[i * 5 + 3] + sumY;

                    //printf(""%lf\n"", (double)input_X[index]);
                    //printf(""%d\n"", i);
                    //printf(""%d\n"", size_X);
                    /*
                    if ((float)i == (float)(size_X - 1))
                    {
                        #define fmt ""%s\n""
                        printf(""%d\n"", i);
                        printf(""G: %0.10f\n"", G);
                        //output_Z[i] = 6.4;
                        for (int i = 0; i < size_X * 2; i++)
                        {
                            printf(""%0.38f\n"", output_Z[i]);
                        }
                    }*/
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
                    //double dist = distanceX * distanceX + distanceY * distanceY;

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

using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Compute.OpenCL;

namespace OPENCL_n_body
{
    class GPU
    {

        private static CLContext context;
        private static CLProgram program;
        private static CLKernel kernel;
        private static CLCommandQueue queue;

        private static float[] input_X;
        private static float[] output_Z;

        private static CLBuffer a;
        private static CLBuffer z;

        private const int blockRoundUpSize = 128;


        public static void Init(Environment env)
        {
            //Console.WriteLine("\nStarted run on GPU");
            
            int size_X = env.particles.Length;
            
            string sourceName = @"./Kernel.cl";
            string clProgramSource = File.ReadAllText(sourceName);

            CLPlatform[] platforms = { };
            CL.GetPlatformIds(out platforms);
            CLPlatform platform = platforms[0];

            //CLContextPropertyList properties = new ComputeContextPropertyList(platform);
            CLDevice[] devices;
            CL.GetDeviceIds(platform, DeviceType.Gpu, out devices);

            CLResultCode resultCode;
            context = CL.CreateContext(IntPtr.Zero, devices, IntPtr.Zero, IntPtr.Zero, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create context failed");

            CLDevice computer = devices[0];

            program = CL.CreateProgramWithSource(context, clProgramSource, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create program failed");

            try
            {
                resultCode = CL.BuildProgram(program, (uint)devices.Length,devices, "", IntPtr.Zero, IntPtr.Zero);
                if (resultCode != CLResultCode.Success) Console.WriteLine("Build failed", resultCode);
            }
            catch
            {
                string buildLog = "you failed big time. OOF.";//program.GetBuildLog(computer);
                Console.WriteLine($"Build log:\n{buildLog}");
            }

            kernel = CL.CreateKernel(program, "Attract", out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create kernel failed");

            queue = CL.CreateCommandQueueWithProperties(context, computer, IntPtr.Zero, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create command queue failed");


            input_X = new float[env.particles.Length * 5];
            output_Z = new float[env.particles.Length * 2];

            a = CL.CreateBuffer(context, MemoryFlags.ReadOnly | MemoryFlags.CopyHostPtr, input_X, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create buffer {a} failed");
            z = CL.CreateBuffer(context, MemoryFlags.WriteOnly | MemoryFlags.CopyHostPtr, output_Z, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create buffer {z} failed");

            resultCode = CL.SetKernelArg(kernel, 0, a);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Set kernel arg {a} failed");
            resultCode = CL.SetKernelArg(kernel, 1, size_X);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Set kernel arg {size_X} failed");
            resultCode = CL.SetKernelArg(kernel, 2, (float)Environment.G);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Set kernel arg {G} failed");
            resultCode = CL.SetKernelArg(kernel, 3, z);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Set kernel arg {z} failed");

            //Console.WriteLine("Stopped run on GPU\n");
        }

        public static void Run(Environment env)
        {
            input_X = new float[env.particles.Length * 5];
            int i = 0;
            foreach (Particle particle in env.particles)
            {
                input_X[i + 0] = (float)particle.x;
                input_X[i + 1] = (float)particle.y;
                input_X[i + 2] = (float)particle.vx;
                input_X[i + 3] = (float)particle.vy;
                input_X[i + 4] = (float)particle.mass;

                i += 5;
            }

            output_Z = new float[env.particles.Length * 2];

            CLResultCode resultCode;
            CLEvent evnt;// = CL.CreateUserEvent(context, out resultCode);
            //if (resultCode != CLResultCode.Success) Console.WriteLine("Create event failed");

            resultCode = CL.EnqueueWriteBuffer(queue, a, false, UIntPtr.Zero, input_X, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque write buffer {a} failed");
            resultCode = CL.EnqueueWriteBuffer(queue, z, false, UIntPtr.Zero, output_Z, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque write buffer {z} failed");
            

            //Stopwatch sw1 = new Stopwatch();
            //sw1.Start();
            
            
            Func<int, int> roundup = x => x % blockRoundUpSize == 0 ? x : (x - x % blockRoundUpSize) + blockRoundUpSize;
            /*resultCode = CL.EnqueueNDRangeKernel(queue, kernel, 2, new UIntPtr[] { UIntPtr.Zero, UIntPtr.Zero }, new UIntPtr[] { (UIntPtr)roundup(env.particles.Length),
                                    (UIntPtr)roundup(env.particles.Length) }, null, 0, null, out evnt);*/
            resultCode = CL.EnqueueNDRangeKernel(queue, kernel, 2, new UIntPtr[] { UIntPtr.Zero, UIntPtr.Zero }, new UIntPtr[] { (UIntPtr)roundup(env.particles.Length),
                                    (UIntPtr)roundup(env.particles.Length) }, new UIntPtr[] { (UIntPtr)128, (UIntPtr)8 }, 0, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque NDRangeKernel failed");
            

            resultCode = CL.EnqueueReadBuffer(queue, z, false, UIntPtr.Zero, output_Z, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque read buffer {z} failed");
            
            //sw1.Stop();
            //Console.Write($"GPUcalc: {sw1.ElapsedMilliseconds}\n");

            resultCode = CL.Finish(queue);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Finish failed");

            int j = 0;
            foreach (Particle particle in env.particles)
            {
                particle.vx += (double)output_Z[j];
                particle.vy += (double)output_Z[j + 1];
                j += 2;
            }

        }



        public static void RunCPUasGPU(Environment env)
        {
            input_X = new float[env.particles.Length * 5];
            int i = 0;
            foreach (Particle particle in env.particles)
            {
                input_X[i + 0] = (float)particle.x;
                input_X[i + 1] = (float)particle.y;
                input_X[i + 2] = (float)particle.vx;
                input_X[i + 3] = (float)particle.vy;
                input_X[i + 4] = (float)particle.mass;

                i += 5;
            }

            output_Z = new float[env.particles.Length * 2];

            //Stopwatch sw1 = new Stopwatch();
            //sw1.Start();

            env.CPUAttract(input_X, env.particles.Length, (float)Environment.G, output_Z);

            //sw1.Stop();
            //Console.Write($"GPUcalc: {sw1.ElapsedMilliseconds}\n");

            int j = 0;
            foreach (Particle particle in env.particles)
            {
                particle.vx += (double)output_Z[j];
                particle.vy += (double)output_Z[j + 1];
                j += 2;
            }

        }
    }
}

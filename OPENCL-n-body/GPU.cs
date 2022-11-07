using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using OpenTK.Compute.OpenCL;

namespace OPENCL_n_body
{
    class GPU
    {

        private static CLContext context;
        private static CLProgram program;
        private static CLKernel kernel1;
        private static CLKernel kernel2;
        private static CLCommandQueue queue;
        private static CLResultCode resultCode;
        private static CLEvent evnt;

        private static float[] input_X;

        private static CLBuffer a;

        private const int blockRoundUpSize = 32;


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

            context = CL.CreateContext(IntPtr.Zero, devices, IntPtr.Zero, IntPtr.Zero, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create context failed");

            CLDevice computer = devices[0];

            byte[] sizes = new byte[3*8];
            CL.GetDeviceInfo(computer, DeviceInfo.MaximumWorkItemSizes, out sizes);
            Console.WriteLine($"{BitConverter.ToUInt64(sizes, 0)} {BitConverter.ToUInt64(sizes, 8)}, {BitConverter.ToUInt64(sizes, 16)}");

            program = CL.CreateProgramWithSource(context, clProgramSource, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create program failed");

            resultCode = CL.BuildProgram(program, (uint)devices.Length,devices, "", IntPtr.Zero, IntPtr.Zero);
            if (resultCode != CLResultCode.Success) 
            {
                Console.WriteLine("Build failed1", resultCode);
                byte[] result;
                resultCode = CL.GetProgramBuildInfo(program, computer, ProgramBuildInfo.Log, out result);
                if (resultCode != CLResultCode.Success) Console.WriteLine("Build log failed", resultCode);
                Console.WriteLine(System.Text.Encoding.Default.GetString(result));
            }

            kernel1 = CL.CreateKernel(program, "Attract", out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create kernel1 failed");

            kernel2 = CL.CreateKernel(program, "Move", out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create kernel2 failed");


            CL.GetKernelWorkGroupInfo(kernel1, computer, KernelWorkGroupInfo.WorkGroupSize, out sizes);
            Console.WriteLine($"k1 {BitConverter.ToUInt64(sizes, 0)}");
            CL.GetKernelWorkGroupInfo(kernel1, computer, KernelWorkGroupInfo.PreferredWorkGroupSizeMultiple, out sizes);
            Console.WriteLine($"k1 {BitConverter.ToUInt64(sizes, 0)}");

            CL.GetKernelWorkGroupInfo(kernel2, computer, KernelWorkGroupInfo.WorkGroupSize, out sizes);
            Console.WriteLine($"k2 {BitConverter.ToUInt64(sizes, 0)}");
            CL.GetKernelWorkGroupInfo(kernel2, computer, KernelWorkGroupInfo.PreferredWorkGroupSizeMultiple, out sizes);
            Console.WriteLine($"k2 {BitConverter.ToUInt64(sizes, 0)}");

            queue = CL.CreateCommandQueueWithProperties(context, computer, IntPtr.Zero, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create command queue failed");


            input_X = new float[env.particles.Length * 5];

            a = CL.CreateBuffer(context, MemoryFlags.ReadWrite | MemoryFlags.CopyHostPtr, input_X, out resultCode);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Create buffer {a} failed");

            resultCode = CL.SetKernelArg(kernel1, 0, a);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Set kernel arg {a} failed");
            resultCode = CL.SetKernelArg(kernel1, 1, (float)Environment.G);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Set kernel arg {G} failed");
            
            resultCode = CL.SetKernelArg(kernel2, 0, a);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Set kernel arg {a} failed");
            

            // = CL.CreateUserEvent(context, out resultCode);
            //if (resultCode != CLResultCode.Success) Console.WriteLine("Create event failed");

            
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
            /*
            resultCode = CL.EnqueueWriteBuffer(queue, a, false, UIntPtr.Zero, input_X, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque write buffer {a} failed");
            */
            //Stopwatch sw1 = new Stopwatch(); sw1.Start();

            Func<int, int> roundup = x => x % blockRoundUpSize == 0 ? x : (x - x % blockRoundUpSize) + blockRoundUpSize;

            resultCode = CL.EnqueueNDRangeKernel(queue, kernel1, 2, new UIntPtr[] { UIntPtr.Zero, UIntPtr.Zero }, new UIntPtr[] { (UIntPtr)roundup(env.particles.Length),
                                    (UIntPtr)roundup(env.particles.Length), (UIntPtr)1 }, new UIntPtr[] { (UIntPtr)32, (UIntPtr)8, (UIntPtr)1 }, 0, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque NDRangeKernel1 failed");

            resultCode = CL.EnqueueNDRangeKernel(queue, kernel2, 2, new UIntPtr[] { UIntPtr.Zero, UIntPtr.Zero }, new UIntPtr[] { (UIntPtr)roundup(env.particles.Length),
                                    (UIntPtr)1, (UIntPtr)1 }, null, 0, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque NDRangeKernel2 failed");

            resultCode = CL.EnqueueReadBuffer(queue, a, false, UIntPtr.Zero, input_X, null, out evnt);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Enque read buffer {z} failed");
            
            //sw1.Stop(); Console.Write($"GPUcalc: {sw1.ElapsedMilliseconds}\n");
            CL.buffer
            resultCode = CL.Finish(queue);
            if (resultCode != CLResultCode.Success) Console.WriteLine("Finish failed");

            int j = 0;
            foreach (Particle particle in env.particles)
            {
                particle.x = (double)input_X[j];
                particle.y = (double)input_X[j + 1];
                particle.vx = (double)input_X[j + 2];
                particle.vy = (double)input_X[j + 3];

                j += 5;
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

            //Stopwatch sw1 = new Stopwatch();
            //sw1.Start();
            env.CPURun(input_X, env.particles.Length, (float)Environment.G);
            //sw1.Stop();
            //Console.Write($"GPUcalc: {sw1.ElapsedMilliseconds}\n");

            int j = 0;
            foreach (Particle particle in env.particles)
            {
                particle.x = (double)input_X[j];
                particle.y = (double)input_X[j + 1];
                particle.vx = (double)input_X[j + 2];
                particle.vy = (double)input_X[j + 3];
                j += 5;
            }

        }
    }
}

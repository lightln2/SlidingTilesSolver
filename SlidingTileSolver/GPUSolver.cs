using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using System;
using System.Collections.Generic;
using System.Diagnostics;


public unsafe class GpuSolver
{
    const int OFFSET_ZERO = 15;
    public const int GPUSIZE = 48_000_000;


    public struct PuzzleParams
    {
        public int Width, Height, Size;
        public int WidthIsEven;

        public PuzzleParams(int width, int height)
        {
            Width = width;
            Height = height;
            Size = Width * Height;
            WidthIsEven = (Width % 2 == 0) ? 1 : 0;
        }
    }

    public static PuzzleParams pparams;

    private static Context context;
    private static Accelerator accelerator;
    private static Action<Index1D, PuzzleParams, ArrayView<long>> life_kernel_up;
    private static Action<Index1D, PuzzleParams, ArrayView<long>> life_kernel_dn;
    private static Action<Index1D, PuzzleParams, ArrayView<long>, int, int> life_kernel_multimove;
    private static MemoryBuffer1D<long, Stride1D.Dense> dev;

    public static long ProcessedValues = 0;
    public static TimeSpan GpuExecTime = TimeSpan.Zero;

    static GpuSolver()
    {
        CompileKernel();
        AppDomain.CurrentDomain.ProcessExit += Destructor;
    }

    static void Destructor(object sender, EventArgs e)
    {
        Dispose();
    }

    public static void Initialize(int width, int height)
    {
        pparams = new PuzzleParams(width, height);
    }

    static void LifeKernelUp(
        Index1D index,
        PuzzleParams p,
        ArrayView<long> input)
    {
        input[index] = VerticalMoveUp(ref p, input[index]);
    }

    static void LifeKernelDn(
        Index1D index,
        PuzzleParams p,
        ArrayView<long> input)
    {
        input[index] = VerticalMoveDown(ref p, input[index]);
    }

    static void LifeKernelMultimove(
        Index1D index,
        PuzzleParams p,
        ArrayView<long> input,
        int count,
        int row)
    {
        byte[] arr1 = new byte[16];
        byte[] arr2 = new byte[16];
        byte[] arr3 = new byte[16];

        long value = input[index];
        FromIndex(ref p, value, arr1);
        Unpack(ref p, arr1);
        Copy(arr1, arr2);

        Index1D pos = index;

        for (int i = row + 1; i < p.Height; i++)
        {
            if (!CanRotateDn(ref p, arr1))
            {
                input[pos] = -1;
            }
            else
            {
                RotateDn(ref p, arr1);
                Copy(arr1, arr3);
                Pack(ref p, arr3);
                input[pos] = GetIndex(ref p, arr3);
            }
            pos += count;
        }

        for (int i = 0; i < row; i++)
        {
            if (!CanRotateUp(ref p, arr2))
            {
                input[pos] = -1;
            }
            else
            {
                RotateUp(ref p, arr2);
                Copy(arr2, arr3);
                Pack(ref p, arr3);
                input[pos] = GetIndex(ref p, arr3);
            }
            pos += count;
        }

    }

    public static void CompileKernel()
    {
        context = Context.Create(builder =>
        {
            builder.Optimize(OptimizationLevel.O2).Cuda();
        });

        foreach (var d in context.Devices)
        {
            Console.WriteLine(d.ToString());
        }

        accelerator = context.CreateCudaAccelerator(0);
        life_kernel_up = accelerator.LoadAutoGroupedStreamKernel<
            Index1D,
            PuzzleParams,
            ArrayView<long>>(LifeKernelUp);
        life_kernel_dn = accelerator.LoadAutoGroupedStreamKernel<
            Index1D,
            PuzzleParams,
            ArrayView<long>>(LifeKernelDn);
        life_kernel_multimove = accelerator.LoadAutoGroupedStreamKernel<
            Index1D,
            PuzzleParams,
            ArrayView<long>,
            int,
            int>(LifeKernelMultimove);

        dev = accelerator.Allocate1D<long>(GPUSIZE);
    }

    public static void Dispose()
    {
        dev.Dispose();
        accelerator.Dispose();
        context.Dispose();
    }

    public static void PrintStats()
    {
        Console.WriteLine($"GPUSolver: Items={ProcessedValues:N0}, time={GpuExecTime}");
    }

    private static void Copy(byte[] src, byte[] dst)
    {
        for (int i = 0; i < 16; i++) dst[i] = src[i];
    }

    private static void FromIndex(ref PuzzleParams p, long index, byte[] arr)
    {
        long newIndex = index / 16;
        arr[OFFSET_ZERO] = (byte)(index - newIndex * 16);
        index = newIndex;

        int div = p.Size;
        for (int i = 0; i < p.Size - 3; i++)
        {
            div--;
            newIndex = index / div;
            arr[i] = (byte)(index - newIndex * div);
            index = newIndex;
        }
        arr[p.Size - 3] = 0;
        arr[p.Size - 2] = 0;
    }

    private static long GetIndex(ref PuzzleParams p, byte[] arr)
    {
        byte zeroPos = arr[OFFSET_ZERO];
        long index = 0;
        int div = 1;
        for (int i = p.Size - 3; i >= 0; i--)
        {
            div++;
            index = index * div + arr[i];
        }
        return index * 16 + zeroPos;
    }

    private static void Unpack(ref PuzzleParams p, byte[] arr)
    {
        bool invEven = true;

        for (int i = p.Size - 2; i >= 0; i--)
        {
            for (int j = i + 1; j < p.Size - 1; j++)
            {
                if (arr[j] >= arr[i]) arr[j]++;
                else invEven = !invEven;
            }
        }

        // restore by inversion count
        byte zeroPos = arr[OFFSET_ZERO];
        bool rowEven = ((zeroPos / p.Width) & 1) == 0;
        bool swapLast = (p.WidthIsEven == 1 && invEven == rowEven) || (!(p.WidthIsEven == 1) && invEven);
        if (swapLast)
        {
            byte tmp = arr[p.Size - 2];
            arr[p.Size - 2] = arr[p.Size - 3];
            arr[p.Size - 3] = tmp;
        }
    }


    private static void Pack(ref PuzzleParams p, byte[] arr)
    {
        for (int i = 0; i < p.Size - 4; i++)
        {
            for (int j = i + 1; j < p.Size - 3; j++)
            {
                if (arr[j] >= arr[i]) arr[j]--;
            }
        }
        arr[p.Size - 2] = 0;
        arr[p.Size - 3] = 0;
    }

    private static bool CanRotateUp(ref PuzzleParams p, byte[] arr)
    {
        int zeroPos = arr[OFFSET_ZERO];
        return zeroPos >= p.Width;
    }

    private static bool CanRotateDn(ref PuzzleParams p, byte[] arr)
    {
        int zeroPos = arr[OFFSET_ZERO];
        return zeroPos < p.Size - p.Width;
    }

    private static void RotateUp(ref PuzzleParams p, byte[] arr)
    {
        int zeroPos = arr[OFFSET_ZERO];
        byte cur = arr[zeroPos - p.Width];
        for (int i = zeroPos - p.Width; i < zeroPos - 1; i++) arr[i] = arr[i + 1];
        arr[zeroPos - 1] = cur;
        arr[OFFSET_ZERO] -= (byte)p.Width;
    }

    private static void RotateDn(ref PuzzleParams p, byte[] arr)
    {
        int zeroPos = arr[OFFSET_ZERO];
        byte cur = arr[zeroPos + p.Width - 1];
        for (int i = zeroPos + p.Width - 1; i > zeroPos; i--) arr[i] = arr[i - 1];
        arr[zeroPos] = cur;
        arr[OFFSET_ZERO] += (byte)p.Width;
    }

    public static long VerticalMoveUp(ref PuzzleParams p, long index)
    {
        byte[] arr = new byte[16];
        FromIndex(ref p, index, arr);
        Unpack(ref p, arr);
        if (!CanRotateUp(ref p, arr)) return -1;
        RotateUp(ref p, arr);
        Pack(ref p, arr);
        return GetIndex(ref p, arr);
    }


    public static long VerticalMoveDown(ref PuzzleParams p, long index)
    {
        byte[] arr = new byte[16];
        FromIndex(ref p, index, arr);
        Unpack(ref p, arr);
        if (!CanRotateDn(ref p, arr)) return -1;
        RotateDn(ref p, arr);
        Pack(ref p, arr);
        return GetIndex(ref p, arr);
    }

    public static void CalcGPU_Up(int count, long[] indexes)
    {
        fixed (long* ptr = indexes)
        {
            CalcGPU_Up(count, ptr);
        }
    }

    public static void CalcGPU_Up(int count, long* indexes)
    {
        if (count == 0) return;
        lock(context)
        {
            ProcessedValues += count;
            var sw = Stopwatch.StartNew();
            dev.AsContiguous().CopyFromCPU(ref indexes[0], count);
            life_kernel_up(count, pparams, dev.View);
            accelerator.Synchronize();
            dev.AsContiguous().CopyToCPU(ref indexes[0], count);
            GpuExecTime += sw.Elapsed;
        }
    }

    public static void CalcGPU_Down(int count, long[] indexes)
    {
        fixed (long* ptr = indexes)
        {
            CalcGPU_Down(count, ptr);
        }
    }

    public static void CalcGPU_Down(int count, long* indexes)
    {
        if (count == 0) return;
        lock (context)
        {
            ProcessedValues += count;
            var sw = Stopwatch.StartNew();
            dev.AsContiguous().CopyFromCPU(ref indexes[0], count);
            life_kernel_dn(count, pparams, dev.View);
            accelerator.Synchronize();
            dev.AsContiguous().CopyToCPU(ref indexes[0], count);
            GpuExecTime += sw.Elapsed;
        }
    }

    public static void CalcGPU_MultislideDown(int count, long* indexes, int times, Action onStep)
    {
        if (count == 0) return;
        lock (context)
        {
            var sw = Stopwatch.StartNew();
            dev.AsContiguous().CopyFromCPU(ref indexes[0], count);
            for (int i = 0; i < times; i++)
            {
                ProcessedValues += count;
                life_kernel_dn(count, pparams, dev.View);
                accelerator.Synchronize();
                dev.AsContiguous().CopyToCPU(ref indexes[0], count);
                onStep();
            }
            GpuExecTime += sw.Elapsed;
        }
    }

    public static void CalcGPU_MultislideUp(int count, long* indexes, int times, Action onStep)
    {
        if (count == 0) return;
        if (times == 0) return;
        lock (context)
        {
            var sw = Stopwatch.StartNew();
            dev.AsContiguous().CopyFromCPU(ref indexes[0], count);
            for (int i = 0; i < times; i++)
            {
                ProcessedValues += count;
                life_kernel_up(count, pparams, dev.View);
                accelerator.Synchronize();
                dev.AsContiguous().CopyToCPU(ref indexes[0], count);
                onStep();
            }
            GpuExecTime += sw.Elapsed;
        }
    }

    public static void CalcGPU_Multimove(int count, long* indexes, int row)
    {
        if (count == 0) return;
        lock (context)
        {
            var sw = Stopwatch.StartNew();
            dev.AsContiguous().CopyFromCPU(ref indexes[0], count);
            life_kernel_multimove(count, pparams, dev.View, count, row);
            accelerator.Synchronize();
            dev.AsContiguous().CopyToCPU(ref indexes[0], count * (pparams.Height - 1));
            GpuExecTime += sw.Elapsed;
        }
    }

}
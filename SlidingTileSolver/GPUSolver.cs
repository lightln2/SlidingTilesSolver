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
    private static MemoryBuffer1D<long, Stride1D.Dense> dev;

    public static int ProcessedValues = 0;
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

        // restore by inversion count (only if rotating up/down would affect it)
        byte zeroPos = arr[OFFSET_ZERO];
        bool rowEven = ((zeroPos / p.Width) & 1) == 0;
        if (zeroPos >= p.Size - 2 || (zeroPos >= p.Size - p.Width - 2 && zeroPos < p.Size - p.Width))
        {
            bool swapLast = (p.WidthIsEven == 1 && invEven == rowEven) || (!(p.WidthIsEven == 1) && invEven);
            if (swapLast)
            {
                byte tmp = arr[p.Size - 2];
                arr[p.Size - 2] = arr[p.Size - 3];
                arr[p.Size - 3] = tmp;
            }
        }
    }

    private static void Pack(ref PuzzleParams p, byte[] arr)
    {
        byte[] dst = new byte[16];
        Copy(arr, dst);
        for (int i = 0; i < p.Size - 4; i++)
        {
            for (int j = i + 1; j < p.Size - 3; j++)
            {
                if (arr[j] > arr[i]) dst[j]--;
            }
        }
        // will be restored by inversions count
        dst[p.Size - 2] = 0;
        dst[p.Size - 3] = 0;

        Copy(dst, arr);
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

    public static void CalcGPU_Multislide(int count, long[] indexes, int row, int height)
    {
        fixed (long* ptr = indexes)
        {
            CalcGPU_Multislide(count, ptr, row, height);
        }
    }

    public static void CalcGPU_Multislide(int count, long* indexes, int row, int height)
    {
        if (count == 0) return;
        if (height == 2)
        {
            if (row == 0)
                CalcGPU_Down(count, indexes);
            else
                CalcGPU_Up(count, indexes);
        }
        else if (height == 3)
        {
            if (row == 0)
            {

            }
            throw new Exception($"height={height} is not supported");
        }
        else throw new Exception($"height={height} is not supported");
    }

}
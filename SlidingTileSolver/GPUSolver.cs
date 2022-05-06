using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using System;
using System.Collections.Generic;
using System.Diagnostics;


public unsafe class GpuSolver
{
    const int OFFSET_ZERO = 15;
    public const int GPUSIZE = 64_000_000;


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
    private static Action<Index1D, PuzzleParams, ArrayView<long>, ArrayView<long>> life_kernel;
    private static MemoryBuffer1D<long, Stride1D.Dense> dev_in;
    private static MemoryBuffer1D<long, Stride1D.Dense> dev_out;

    public static int ProcessedValues = 0;
    public static TimeSpan GpuExec = TimeSpan.Zero;

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

    static void LifeKernel(
        Index1D index,
        PuzzleParams p,
        ArrayView<long> input, ArrayView<long> output)
    {
        long upIndex, dnIndex;
        VerticalMoves(ref p, input[index], out upIndex, out dnIndex);
        output[index * 2] = upIndex;
        output[index * 2 + 1] = dnIndex;
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
        life_kernel = accelerator.LoadAutoGroupedStreamKernel<
            Index1D,
            PuzzleParams,
            ArrayView<long>, ArrayView<long>>(LifeKernel);

        dev_in = accelerator.Allocate1D<long>(GPUSIZE);
        dev_out = accelerator.Allocate1D<long>(GPUSIZE * 2);

    }

    public static void Dispose()
    {
        dev_in.Dispose();
        dev_out.Dispose();
        accelerator.Dispose();
        context.Dispose();
        Console.WriteLine($"Gpu cycles: {ProcessedValues}, total time: {GpuExec}");
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

    public static void VerticalMoves(ref PuzzleParams p, long index, out long upIndex, out long dnIndex)
    {
        byte[] arr = new byte[16];
        byte[] arr2 = new byte[16];
        FromIndex(ref p, index, arr);
        Unpack(ref p, arr);

        if (CanRotateUp(ref p, arr))
        {
            Copy(arr, arr2);
            RotateUp(ref p, arr2);
            Pack(ref p, arr2);
            upIndex = GetIndex(ref p, arr2);
        }
        else
        {
            upIndex = -1;
        }

        if (CanRotateDn(ref p, arr))
        {
            Copy(arr, arr2);
            RotateDn(ref p, arr2);
            Pack(ref p, arr2);
            dnIndex = GetIndex(ref p, arr2);
        }
        else
        {
            dnIndex = -1;
        }
    }

    public static void CalcGPU(int count, long[] inIndexes, long[] outIndexes)
    {
        ProcessedValues++;
        var sw = Stopwatch.StartNew();
        dev_in.AsContiguous().CopyFromCPU(ref inIndexes[0], count);
        life_kernel(count, pparams, dev_in.View, dev_out.View);
        accelerator.Synchronize();
        dev_out.AsContiguous().CopyToCPU(ref outIndexes[0], count * 2);
        GpuExec += sw.Elapsed;
        return;
    }

}
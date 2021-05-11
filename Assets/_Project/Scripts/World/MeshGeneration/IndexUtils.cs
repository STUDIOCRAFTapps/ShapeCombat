using Unity.Mathematics;
using System.Runtime.CompilerServices;

public static class IndexUtilities {
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int XyzToIndex (int3 xyz, int width, int height) {
        return XyzToIndex(xyz.x, xyz.y, xyz.z, width, height);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int XyzToIndex (int x, int y, int z, int width, int height) {
        return z * width * height + y * width + x;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 IndexToXyz (int index, int width, int height) {
        int3 position = new int3(
            index % width,
            index / width % height,
            index / (width * height));
        return position;
    }
}
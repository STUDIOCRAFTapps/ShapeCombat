using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine.Rendering;

[StructLayout(LayoutKind.Sequential)]
public struct MeshingVertexData {
    public float3 position;
    public float3 normal;
    public float4 uvs;
    
    public MeshingVertexData (float3 position, float3 normal, float4 uvs) {
        this.position = position;
        this.normal = normal;
        this.uvs = uvs;
    }
    
    public static readonly VertexAttributeDescriptor[] VertexBufferMemoryLayout = {
        new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
        new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4)
    };
}
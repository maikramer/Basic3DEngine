using System.Numerics;
using Veldrid;

namespace Basic3DEngine.Entities;

/// <summary>
/// Vértice com posição, normal e cor (para iluminação)
/// </summary>
public struct VertexPositionNormalColor
{
    public Vector3 Position;
    public Vector3 Normal;
    public RgbaFloat Color;

    public VertexPositionNormalColor(Vector3 position, Vector3 normal, RgbaFloat color)
    {
        Position = position;
        Normal = normal;
        Color = color;
    }

    public static VertexLayoutDescription GetVertexLayoutDescription()
    {
        return new VertexLayoutDescription(
            new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
            new VertexElementDescription("Color", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float4)
        );
    }
} 
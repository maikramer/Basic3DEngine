using System.IO;
using System.Numerics;
using Veldrid;
using Basic3DEngine.Entities;
using Basic3DEngine.Services;

namespace Basic3DEngine.Entities.Primitives
{
    public class Icosphere : Geometry
    {

        private struct UniformBufferObject
        {
            public Matrix4x4 Projection;
            public Matrix4x4 View;
            public Matrix4x4 World;
            public Vector4 Color;
        }

        public RgbaFloat Color { get; set; }

        public Icosphere(GraphicsDevice graphicsDevice, ResourceFactory factory, CommandList commandList, Vector3 position, RgbaFloat color, int subdivisions = 2, OutputDescription? hdrOutputDescription = null)
            : base(graphicsDevice, factory, commandList, position)
        {
            Position = position;
            Color = color;

            // Gerar geometria da icosfera
            var (vertices, indices) = GenerateIcosphere(subdivisions);

            // Criar vertex buffer
            _vertexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)(vertices.Length * 32), // 3 floats para posição + 3 para normal + 2 para UV = 8 floats = 32 bytes
                BufferUsage.VertexBuffer));
            graphicsDevice.UpdateBuffer(_vertexBuffer, 0, vertices);

            // Criar index buffer
            _indexBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)(indices.Length * sizeof(uint)),
                BufferUsage.IndexBuffer));
            graphicsDevice.UpdateBuffer(_indexBuffer, 0, indices);

            // Criar uniform buffer
            _uniformBuffer = factory.CreateBuffer(new BufferDescription(
                (uint)System.Runtime.InteropServices.Marshal.SizeOf<UniformBufferObject>(),
                BufferUsage.UniformBuffer));

            // Criar shader e pipeline
            CreateShaderAndPipeline(factory, hdrOutputDescription);

            // Criar resource set após criar o pipeline
        }

        private void CreateShaderAndPipeline(ResourceFactory factory, OutputDescription? hdrOutputDescription = null)
        {
            try
            {
                // Carregar shaders de arquivos externos
                var shadersBasePath = ShaderLoader.GetShadersBasePath();
                var icosphereShaderPath = Path.Combine(shadersBasePath, "Icosphere");
                
                var vertexShader = ShaderLoader.LoadShader(factory, 
                    Path.Combine(icosphereShaderPath, "icosphere.vert"), 
                    ShaderStages.Vertex);
                    
                var fragmentShader = ShaderLoader.LoadShader(factory, 
                    Path.Combine(icosphereShaderPath, "icosphere.frag"), 
                    ShaderStages.Fragment);

                // Criar layout de recursos
                var resourceLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionViewWorldColor", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

                // Criar vertex layout
                var vertexLayout = new VertexLayoutDescription(
                    new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                    new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

                // Criar pipeline
                _pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                    BlendStateDescription.SingleOverrideBlend,
                    DepthStencilStateDescription.DepthOnlyLessEqual,
                    RasterizerStateDescription.Default,
                    PrimitiveTopology.TriangleList,
                    new ShaderSetDescription(
                        new[] { vertexLayout },
                        new[] { vertexShader, fragmentShader }),
                    new[] { resourceLayout },
                    hdrOutputDescription ?? _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription));

                // Criar resource set
                _resourceSet = factory.CreateResourceSet(new ResourceSetDescription(
                    resourceLayout,
                    _uniformBuffer));
                
                // Limpar shaders temporários
                vertexShader.Dispose();
                fragmentShader.Dispose();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao criar shaders e pipeline da icosphere: {ex.Message}", ex);
            }
        }

        private (float[], uint[]) GenerateIcosphere(int subdivisions)
        {
            // Criar icosaedro inicial
            var vertices = new List<Vector3>();
            var indices = new List<uint>();

            // Golden ratio
            float t = (1.0f + (float)Math.Sqrt(5.0f)) / 2.0f;

            // Vértices do icosaedro
            vertices.AddRange(new[]
            {
                new Vector3(-1, t, 0).Normalized(),
                new Vector3(1, t, 0).Normalized(),
                new Vector3(-1, -t, 0).Normalized(),
                new Vector3(1, -t, 0).Normalized(),
                new Vector3(0, -1, t).Normalized(),
                new Vector3(0, 1, t).Normalized(),
                new Vector3(0, -1, -t).Normalized(),
                new Vector3(0, 1, -t).Normalized(),
                new Vector3(t, 0, -1).Normalized(),
                new Vector3(t, 0, 1).Normalized(),
                new Vector3(-t, 0, -1).Normalized(),
                new Vector3(-t, 0, 1).Normalized()
            });

            // Faces do icosaedro
            indices.AddRange(new uint[]
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            });

            // Subdividir
            for (int i = 0; i < subdivisions; i++)
            {
                var newIndices = new List<uint>();
                var edgeCache = new Dictionary<(uint, uint), uint>();

                for (int j = 0; j < indices.Count; j += 3)
                {
                    uint a = indices[j];
                    uint b = indices[j + 1];
                    uint c = indices[j + 2];

                    uint ab = GetMiddlePoint(a, b, vertices, edgeCache);
                    uint bc = GetMiddlePoint(b, c, vertices, edgeCache);
                    uint ca = GetMiddlePoint(c, a, vertices, edgeCache);

                    newIndices.AddRange(new uint[] { a, ab, ca });
                    newIndices.AddRange(new uint[] { b, bc, ab });
                    newIndices.AddRange(new uint[] { c, ca, bc });
                    newIndices.AddRange(new uint[] { ab, bc, ca });
                }

                indices = newIndices;
            }

            // Converter para array de floats com normais e coordenadas UV
            var vertexData = new List<float>();
            foreach (var vertex in vertices)
            {
                // Posição
                vertexData.Add(vertex.X);
                vertexData.Add(vertex.Y);
                vertexData.Add(vertex.Z);

                // Normal (para esfera, a normal é a própria posição normalizada)
                vertexData.Add(vertex.X);
                vertexData.Add(vertex.Y);
                vertexData.Add(vertex.Z);

                // Coordenadas UV esféricas
                float u = 0.5f + (float)(Math.Atan2(vertex.Z, vertex.X) / (2 * Math.PI));
                float v = 0.5f - (float)(Math.Asin(vertex.Y) / Math.PI);
                vertexData.Add(u);
                vertexData.Add(v);
            }

            return (vertexData.ToArray(), indices.ToArray());
        }

        private uint GetMiddlePoint(uint p1, uint p2, List<Vector3> vertices, Dictionary<(uint, uint), uint> cache)
        {
            uint smallerIndex = Math.Min(p1, p2);
            uint greaterIndex = Math.Max(p1, p2);
            var key = (smallerIndex, greaterIndex);

            if (cache.ContainsKey(key))
                return cache[key];

            Vector3 point1 = vertices[(int)p1];
            Vector3 point2 = vertices[(int)p2];
            Vector3 middle = ((point1 + point2) / 2.0f).Normalized();

            vertices.Add(middle);
            uint index = (uint)(vertices.Count - 1);
            cache[key] = index;

            return index;
        }
        
        
        public override void Update(float deltaTime)
        {
            // Atualiza a matriz de transformação corretamente
            var rotationMatrix = Matrix4x4.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z);
            var scaleMatrix = Matrix4x4.CreateScale(Scale);
            var translationMatrix = Matrix4x4.CreateTranslation(Position);
            _worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;
        }

        public override void Render(CommandList commandList, Matrix4x4 viewMatrix, Matrix4x4 projectionMatrix)
        {
            if (_vertexBuffer == null || _indexBuffer == null || _pipeline == null || 
                _resourceSet == null || _uniformBuffer == null)
            {
                return;
            }

            // Criar estrutura para as matrizes
            var ubo = new UniformBufferObject
            {
                Projection = projectionMatrix,
                View = viewMatrix,
                World = _worldMatrix,
                Color = new Vector4(Color.R, Color.G, Color.B, Color.A)
            };

            // Atualiza o buffer de uniformes com as matrizes
            _graphicsDevice.UpdateBuffer(_uniformBuffer, 0, ubo);

            // Define o pipeline
            commandList.SetPipeline(_pipeline);

            // Define os buffers de vértices e índices
            commandList.SetVertexBuffer(0, _vertexBuffer);
            commandList.SetIndexBuffer(_indexBuffer, IndexFormat.UInt32);

            // Define o conjunto de recursos
            commandList.SetGraphicsResourceSet(0, _resourceSet);

            // Desenha a icosfera
            commandList.DrawIndexed((uint)(_indexBuffer.SizeInBytes / sizeof(uint)), 1, 0, 0, 0);
        }

        public override void Dispose()
        {
            _vertexBuffer?.Dispose();
            _indexBuffer?.Dispose();
            _uniformBuffer?.Dispose();
            _resourceSet?.Dispose();
            _pipeline?.Dispose();
        }
    }

    public static class Vector3Extensions
    {
        public static Vector3 Normalized(this Vector3 vector)
        {
            return Vector3.Normalize(vector);
        }
    }
}

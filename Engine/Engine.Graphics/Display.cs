using System;
using System.Runtime.InteropServices;
#if Direct3D11
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
#else
using Silk.NET.OpenGLES;
#endif

namespace Engine.Graphics
{
    public static class Display
    {
        private static RenderTarget2D m_renderTarget;

        private static RasterizerState m_rasterizerState = RasterizerState.CullCounterClockwise;

        private static DepthStencilState m_depthStencilState = DepthStencilState.Default;

        private static BlendState m_blendState = BlendState.Opaque;

        private static bool m_useReducedZRange = false;

        public static Point2 BackbufferSize
        {
            get;
            private set;
        }

        public static Viewport Viewport
        {
            get;
            set;
        }

        public static Rectangle ScissorRectangle
        {
            get;
            set;
        }

        public static RasterizerState RasterizerState
        {
            get
            {
                return m_rasterizerState;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                m_rasterizerState = value;
                value.IsLocked = true;
            }
        }

        public static DepthStencilState DepthStencilState
        {
            get
            {
                return m_depthStencilState;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                m_depthStencilState = value;
                value.IsLocked = true;
            }
        }

        public static BlendState BlendState
        {
            get
            {
                return m_blendState;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                m_blendState = value;
                value.IsLocked = true;
            }
        }

        public static RenderTarget2D RenderTarget
        {
            get
            {
                return m_renderTarget;
            }
            set
            {
                m_renderTarget = value;
                if (value != null)
                {
                    Viewport = new Viewport(0, 0, value.Width, value.Height);
                    ScissorRectangle = new Rectangle(0, 0, value.Width, value.Height);
                }
                else
                {
                    Viewport = new Viewport(0, 0, BackbufferSize.X, BackbufferSize.Y);
                    ScissorRectangle = new Rectangle(0, 0, BackbufferSize.X, BackbufferSize.Y);
                }
            }
        }
        
                        public static bool UseReducedZRange
        {
            get
            {
                return m_useReducedZRange;
            }
            set
            {
                if (value != m_useReducedZRange)
                {
                    m_useReducedZRange = value;
                    foreach (GraphicsResource resource in GraphicsResource.m_resources)
                    {
                        (resource as Shader)?.CompileShaders();
                    }
                }
            }
        }

        public static string DeviceDescription { get; set; }

        public static event Action DeviceLost;

        public static event Action DeviceReset;

        public static void DrawUser<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount) where T : struct
        {
            VerifyParametersDrawUser(primitiveType, shader, vertexDeclaration, vertexData, startVertex, verticesCount);
#if Direct3D11
            int num = DXWrapper.AppendUserVertices<T>(vertexData, vertexDeclaration.VertexStride, startVertex, verticesCount);
            DXWrapper.ApplyViewportScissor(Viewport, ScissorRectangle);
            DXWrapper.ApplyRasterizerState(RasterizerState);
            DXWrapper.ApplyDepthStencilState(DepthStencilState);
            DXWrapper.ApplyBlendState(BlendState);
            DXWrapper.ApplyShaderAndRenderTarget(RenderTarget, shader, vertexDeclaration);
            DXWrapper.ApplyVertexBuffer(DXWrapper.UserVertexBuffer, vertexDeclaration.VertexStride, num);
            DXWrapper.ApplyIndexBuffer(null, Format.R32_UInt, 0);
            DXWrapper.ApplyPrimitiveType(primitiveType);
            DXWrapper.Context.Draw(verticesCount, 0);
#else
            var gCHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            try
            {
                GLWrapper.ApplyRenderTarget(RenderTarget);
                GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
                GLWrapper.ApplyShaderAndBuffers(shader, vertexDeclaration, gCHandle.AddrOfPinnedObject() + (startVertex * vertexDeclaration.VertexStride), 0, null);
                GLWrapper.ApplyRasterizerState(RasterizerState);
                GLWrapper.ApplyDepthStencilState(DepthStencilState);
                GLWrapper.ApplyBlendState(BlendState);
                GLWrapper.GL.DrawArrays(GLWrapper.TranslatePrimitiveType(primitiveType), startVertex, (uint)verticesCount);
            }
            finally
            {
                gCHandle.Free();
            }
#endif
        }

        public static unsafe void DrawUserIndexed<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount, int[] indexData, int startIndex, int indicesCount) where T : struct
        {
            VerifyParametersDrawUserIndexed(primitiveType, shader, vertexDeclaration, vertexData, startVertex, verticesCount, indexData, startIndex, indicesCount);
#if Direct3D11
            int num = DXWrapper.AppendUserVertices(vertexData, vertexDeclaration.VertexStride, startVertex, verticesCount);
            int num2 = DXWrapper.AppendUserIndices(indexData, 4, startIndex, indicesCount);
            DXWrapper.ApplyViewportScissor(Viewport, ScissorRectangle);
            DXWrapper.ApplyRasterizerState(RasterizerState);
            DXWrapper.ApplyDepthStencilState(DepthStencilState);
            DXWrapper.ApplyBlendState(BlendState);
            DXWrapper.ApplyShaderAndRenderTarget(RenderTarget, shader, vertexDeclaration);
            DXWrapper.ApplyVertexBuffer(DXWrapper.UserVertexBuffer, vertexDeclaration.VertexStride, num);
            DXWrapper.ApplyIndexBuffer(DXWrapper.UserIndexBuffer, Format.R32_UInt, num2);
            DXWrapper.ApplyPrimitiveType(primitiveType);
            DXWrapper.Context.DrawIndexed(indicesCount, 0, 0);
#else
            var gCHandle = GCHandle.Alloc(vertexData, GCHandleType.Pinned);
            var gCHandle2 = GCHandle.Alloc(indexData, GCHandleType.Pinned);
            try
            {
                GLWrapper.ApplyRenderTarget(RenderTarget);
                GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
                GLWrapper.ApplyShaderAndBuffers(shader, vertexDeclaration, gCHandle.AddrOfPinnedObject(), 0, 0);
                GLWrapper.ApplyRasterizerState(RasterizerState);
                GLWrapper.ApplyDepthStencilState(DepthStencilState);
                GLWrapper.ApplyBlendState(BlendState);
                GLWrapper.GL.DrawElements(GLWrapper.TranslatePrimitiveType(primitiveType), (uint)indicesCount, DrawElementsType.UnsignedInt, (gCHandle2.AddrOfPinnedObject() + (4 * startIndex)).ToPointer());
            }
            finally
            {
                gCHandle.Free();
                gCHandle2.Free();
            }
#endif
        }

        public static void Draw(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, int startVertex, int verticesCount)
        {
            VerifyParametersDraw(primitiveType, shader, vertexBuffer, startVertex, verticesCount);
#if Direct3D11
            DXWrapper.ApplyViewportScissor(Viewport, ScissorRectangle);
            DXWrapper.ApplyRasterizerState(RasterizerState);
            DXWrapper.ApplyDepthStencilState(DepthStencilState);
            DXWrapper.ApplyBlendState(BlendState);
            DXWrapper.ApplyShaderAndRenderTarget(RenderTarget, shader, vertexBuffer.VertexDeclaration);
            DXWrapper.ApplyVertexBuffer(vertexBuffer.m_buffer, vertexBuffer.VertexDeclaration.VertexStride, 0);
            DXWrapper.ApplyIndexBuffer(null, Format.R32_UInt, 0);
            DXWrapper.ApplyPrimitiveType(primitiveType);
            DXWrapper.Context.Draw(verticesCount, startVertex);
#else
            GLWrapper.ApplyRenderTarget(RenderTarget);
            GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
            GLWrapper.ApplyShaderAndBuffers(shader, vertexBuffer.VertexDeclaration, IntPtr.Zero, vertexBuffer.m_buffer, null);
            GLWrapper.ApplyRasterizerState(RasterizerState);
            GLWrapper.ApplyDepthStencilState(DepthStencilState);
            GLWrapper.ApplyBlendState(BlendState);
            GLWrapper.GL.DrawArrays(GLWrapper.TranslatePrimitiveType(primitiveType), startVertex, (uint)verticesCount);
#endif
        }

        public static unsafe void DrawIndexed(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, IndexBuffer indexBuffer, int startIndex, int indicesCount)
        {
            VerifyParametersDrawIndexed(primitiveType, shader, vertexBuffer, indexBuffer, startIndex, indicesCount);
#if Direct3D11
            DXWrapper.ApplyViewportScissor(Viewport, ScissorRectangle);
            DXWrapper.ApplyRasterizerState(RasterizerState);
            DXWrapper.ApplyDepthStencilState(DepthStencilState);
            DXWrapper.ApplyBlendState(BlendState);
            DXWrapper.ApplyShaderAndRenderTarget(RenderTarget, shader, vertexBuffer.VertexDeclaration);
            DXWrapper.ApplyVertexBuffer(vertexBuffer.m_buffer, vertexBuffer.VertexDeclaration.VertexStride, 0);
            DXWrapper.ApplyIndexBuffer(indexBuffer.m_buffer, DXWrapper.TranslateIndexFormat(indexBuffer.IndexFormat), 0);
            DXWrapper.ApplyPrimitiveType(primitiveType);
            DXWrapper.Context.DrawIndexed(indicesCount, startIndex, 0);
#else
            GLWrapper.ApplyRenderTarget(RenderTarget);
            GLWrapper.ApplyViewportScissor(Viewport, ScissorRectangle, RasterizerState.ScissorTestEnable);
            GLWrapper.ApplyShaderAndBuffers(shader, vertexBuffer.VertexDeclaration, IntPtr.Zero, vertexBuffer.m_buffer, indexBuffer.m_buffer);
            GLWrapper.ApplyRasterizerState(RasterizerState);
            GLWrapper.ApplyDepthStencilState(DepthStencilState);
            GLWrapper.ApplyBlendState(BlendState);
            GLWrapper.GL.DrawElements(GLWrapper.TranslatePrimitiveType(primitiveType), (uint)indicesCount, GLWrapper.TranslateIndexFormat(indexBuffer.IndexFormat), new IntPtr(startIndex * indexBuffer.IndexFormat.GetSize()).ToPointer());
#endif
        }

        public static void Clear(Vector4? color, float? depth = null, int? stencil = null)
        {
#if Direct3D11
            if (color != null)
            {
                if (RenderTarget != null && RenderTarget.m_colorTextureView != null)
                {
                    DXWrapper.Context.ClearRenderTargetView(RenderTarget.m_colorTextureView, new Color4(color.Value.X, color.Value.Y, color.Value.Z, color.Value.W));
                }
                else if (DXWrapper.ColorBufferView != null)
                {
                    DXWrapper.Context.ClearRenderTargetView(DXWrapper.ColorBufferView, new Color4(color.Value.X, color.Value.Y, color.Value.Z, color.Value.W));
                }
            }
            if (depth != null || stencil != null)
            {
                float num = 0f;
                byte b = 0;
                DepthStencilClearFlags depthStencilClearFlags = (DepthStencilClearFlags)0;
                if (depth != null)
                {
                    depthStencilClearFlags |= DepthStencilClearFlags.Depth;
                    num = depth.Value;
                }
                if (stencil != null)
                {
                    depthStencilClearFlags |= DepthStencilClearFlags.Stencil;
                    b = (byte)stencil.Value;
                }
                if (RenderTarget != null && RenderTarget.m_depthTextureView != null)
                {
                    DXWrapper.Context.ClearDepthStencilView(RenderTarget.m_depthTextureView, depthStencilClearFlags, num, b);
                    return;
                }
                if (DXWrapper.DepthBufferView != null)
                {
                    DXWrapper.Context.ClearDepthStencilView(DXWrapper.DepthBufferView, depthStencilClearFlags, num, b);
                }
            }
#else
            GLWrapper.Clear(RenderTarget, color, depth, stencil);
#endif
        }

        public static void ResetGLStateCache()
        {
#if !Direct3D11
            GLWrapper.InitializeCache();
#endif
        }

        public static void Initialize()
        {
#if Direct3D11
            DXWrapper.CreateDevice();
#else
            GLWrapper.Initialize();
            GLWrapper.InitializeCache();
#endif
            Resize();
        }

        public static void Dispose()
        {
        }

        public static void BeforeFrame()
        {
        }

        public static void AfterFrame()
        {
        }

        public static void Resize()
        {
            BackbufferSize = new Point2(Window.Size.X, Window.Size.Y);
            Viewport = new Viewport(0, 0, Window.Size.X, Window.Size.Y);
            ScissorRectangle = new Rectangle(0, 0, Window.Size.X, Window.Size.Y);
#if Direct3D11
            DXWrapper.ResizeSwapChainIfNeeded();
#endif
        }

        public static long GetGpuMemoryUsage()
        {
            long num = 8 * BackbufferSize.X * BackbufferSize.Y;
            foreach (GraphicsResource resource in GraphicsResource.m_resources)
            {
                num += resource.GetGpuMemoryUsage();
            }
            return num;
        }

        public static void Clear(Color? color, float? depth = null, int? stencil = null)
        {
            Clear(color.HasValue ? new Vector4?(new Vector4(color.Value)) : null, depth, stencil);
        }

        public static void VerifyParametersDrawUser<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount) where T : struct
        {
            int num = Utilities.SizeOf<T>();
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexDeclaration);
            ArgumentNullException.ThrowIfNull(vertexData);
            if (vertexDeclaration.VertexStride / num * num != vertexDeclaration.VertexStride)
            {
                throw new InvalidOperationException($"Vertex is not an integer multiple of array element, vertex stride is {vertexDeclaration.VertexStride}, array element is {num}.");
            }
            if (startVertex < 0 || verticesCount < 0 || startVertex + verticesCount > vertexData.Length)
            {
                throw new ArgumentException("Vertices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
        }

        public static void VerifyParametersDrawUserIndexed<T>(PrimitiveType primitiveType, Shader shader, VertexDeclaration vertexDeclaration, T[] vertexData, int startVertex, int verticesCount, int[] indexData, int startIndex, int indicesCount) where T : struct
        {
            int num = Utilities.SizeOf<T>();
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexDeclaration);
            ArgumentNullException.ThrowIfNull(vertexData);
            ArgumentNullException.ThrowIfNull(indexData);
            if (vertexDeclaration.VertexStride / num * num != vertexDeclaration.VertexStride)
            {
                throw new InvalidOperationException($"Vertex is not an integer multiple of array element, vertex stride is {vertexDeclaration.VertexStride}, array element is {num}.");
            }
            if (startVertex < 0 || verticesCount < 0 || startVertex + verticesCount > vertexData.Length)
            {
                throw new ArgumentException("Vertices range is out of bounds.");
            }
            if (startIndex < 0 || indicesCount < 0 || startIndex + indicesCount > indexData.Length)
            {
                throw new ArgumentException("Indices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
        }

        public static void VerifyParametersDraw(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, int startVertex, int verticesCount)
        {
            vertexBuffer.VerifyNotDisposed();
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexBuffer);
            if (startVertex < 0 || verticesCount < 0 || startVertex + verticesCount > vertexBuffer.VerticesCount)
            {
                throw new ArgumentException("Vertices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
        }

        public static void VerifyParametersDrawIndexed(PrimitiveType primitiveType, Shader shader, VertexBuffer vertexBuffer, IndexBuffer indexBuffer, int startIndex, int indicesCount)
        {
            ArgumentNullException.ThrowIfNull(shader);
            ArgumentNullException.ThrowIfNull(vertexBuffer);
            ArgumentNullException.ThrowIfNull(indexBuffer);
            if (startIndex < 0 || indicesCount < 0 || startIndex + indicesCount > indexBuffer.IndicesCount)
            {
                throw new ArgumentException("Indices range is out of bounds.");
            }
            shader.VerifyNotDisposed();
            vertexBuffer.VerifyNotDisposed();
            indexBuffer.VerifyNotDisposed();
        }

        public static void HandleDeviceLost()
        {
            foreach (GraphicsResource resource in GraphicsResource.m_resources)
            {
                resource.HandleDeviceLost();
            }
            DeviceLost?.Invoke();
        }

        public static void HandleDeviceReset()
        {
            foreach (GraphicsResource resource in GraphicsResource.m_resources)
            {
                resource.HandleDeviceReset();
            }
            DeviceReset?.Invoke();
        }
    }
}

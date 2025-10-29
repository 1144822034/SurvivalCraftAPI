using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Device2 = SharpDX.Direct3D11.Device2;
using Device3 = SharpDX.DXGI.Device3;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using ResultCode = SharpDX.DXGI.ResultCode;

namespace Engine.Graphics {
    public static class DXWrapper {
        public static Device2 Device;
        public static DeviceContext2 Context;
        public static SwapChain2 SwapChain;
        public static RenderTargetView ColorBufferView;
        public static DepthStencilView DepthBufferView;
        public static FeatureLevel FeatureLevel;
        public static Dictionary<RasterizerState, SharpDX.Direct3D11.RasterizerState> m_dxRasterizerStates = new();
        public static Dictionary<DepthStencilState, SharpDX.Direct3D11.DepthStencilState> m_dxDepthStencilStates = new();
        public static Dictionary<BlendState, SharpDX.Direct3D11.BlendState> m_dxBlendStates = new();
        public static Dictionary<SamplerState, SharpDX.Direct3D11.SamplerState> m_dxSamplerStates = new();
        public static Viewport? m_viewport;
        public static Rectangle? m_scissorRectangle;
        public static IntPtr[] m_dataPointers;
        public static RenderTargetView m_renderTargetView;
        public static DepthStencilView m_depthStencilView;
        public static VertexShader m_vertexShader;
        public static PixelShader m_pixelShader;
        public static InputLayout m_inputLayout;
        public static PrimitiveTopology m_primitiveTopology;
        public static SharpDX.Direct3D11.Buffer m_vertexBuffer;
        public static int m_vertexStride;
        public static int m_vertexOffset;
        public static SharpDX.Direct3D11.Buffer m_indexBuffer;
        public static int m_indexOffset;
        public static RasterizerState m_rasterizerState;
        public static DepthStencilState m_depthStencilState;
        public static BlendState m_blendState;
        public static SamplerState[] m_vsSamplerStates = new SamplerState[16];
        public static SamplerState[] m_psSamplerStates = new SamplerState[16];
        public static SharpDX.Direct3D11.Buffer UserVertexBuffer;
        public static SharpDX.Direct3D11.Buffer UserIndexBuffer;
        public static int m_userVertexBufferOffset;
        public static int m_userIndexBufferOffset;
        public static int m_userVertexBufferSize;
        public static int m_userIndexBufferSize;
        public static int REQ_TEXTURE2D_U_OR_V_DIMENSION;

        public static FeatureLevel[] m_featureLevels = [
            FeatureLevel.Level_11_1,
            FeatureLevel.Level_11_0,
            FeatureLevel.Level_10_1,
            FeatureLevel.Level_10_0,
            FeatureLevel.Level_9_3,
            FeatureLevel.Level_9_2,
            FeatureLevel.Level_9_1
        ];

        public static void CreateDevice() {
            if (Window.m_hwnd == IntPtr.Zero) {
                throw new InvalidOperationException("Failed to get window handle");
            }
#if DEBUG
            DeviceCreationFlags deviceCreationFlags = DeviceCreationFlags.Debug;
#else
            DeviceCreationFlags deviceCreationFlags = DeviceCreationFlags.None;
#endif
            using (Device device = new(
                    DriverType.Hardware,
                    deviceCreationFlags,
                    FeatureLevel.Level_11_0,
                    FeatureLevel.Level_10_1,
                    FeatureLevel.Level_10_0,
                    FeatureLevel.Level_9_3,
                    FeatureLevel.Level_9_2,
                    FeatureLevel.Level_9_1
                )) {
                Device = device.QueryInterface<Device2>();
            }
            Context = Device.ImmediateContext2;
            FeatureLevel = Device.FeatureLevel;
            SwapChainDescription1 swapChainDescription = new() {
                AlphaMode = AlphaMode.Ignore,
                BufferCount = 2,
                Format = Format.R8G8B8A8_UNorm,
                Width = Window.Size.X,
                Height = Window.Size.Y,
                SampleDescription = new SampleDescription(1, 0),
                Scaling = Scaling.Stretch,
                Stereo = false,
                SwapEffect = SwapEffect.FlipSequential,
                Usage = Usage.RenderTargetOutput
            };
            using (Device3 device2 = Device.QueryInterface<Device3>()) {
                using (Factory3 parent = device2.Adapter.GetParent<Factory3>()) {
                    using (SwapChain1 swapChain = new(parent, Device, Window.m_hwnd, ref swapChainDescription)) {
                        SwapChain = swapChain.QueryInterface<SwapChain2>();
                    }
                }
                device2.MaximumFrameLatency = 1;
            }
            /*using (ISwapChainPanelNative swapChainPanelNative = ComObject.As<ISwapChainPanelNative>(Window.m_swapChainPanel))
            {
                swapChainPanelNative.SwapChain = SwapChain;
            }*/
            CreateBufferViews();
            switch (FeatureLevel) {
                case FeatureLevel.Level_11_1:
                case FeatureLevel.Level_11_0: REQ_TEXTURE2D_U_OR_V_DIMENSION = 16384; break;
                case FeatureLevel.Level_10_1:
                case FeatureLevel.Level_10_0: REQ_TEXTURE2D_U_OR_V_DIMENSION = 8192; break;
                case FeatureLevel.Level_9_3: REQ_TEXTURE2D_U_OR_V_DIMENSION = 4096; break;
                case FeatureLevel.Level_9_2:
                case FeatureLevel.Level_9_1: REQ_TEXTURE2D_U_OR_V_DIMENSION = 2048; break;
                default: REQ_TEXTURE2D_U_OR_V_DIMENSION = -1; break;
            }
            Display.DeviceDescription =
                $"DX11 Metro, FeatureLevel={FeatureLevel}, Debug={Context.Device.CreationFlags.HasFlag(DeviceCreationFlags.Debug)}, ReqTexture2DUOrVDimension={(REQ_TEXTURE2D_U_OR_V_DIMENSION > 0 ? REQ_TEXTURE2D_U_OR_V_DIMENSION : "unknown")}";
            Log.Information("Initialized display device: " + Display.DeviceDescription);
        }

        public static void Trim() {
            using (Device3 device = Device.QueryInterface<Device3>()) {
                device.Trim();
            }
        }

        public static void DisposeDevice() {
            DeviceDebug deviceDebug = null;
            if ((Device.CreationFlags & DeviceCreationFlags.Debug) != DeviceCreationFlags.None) {
                deviceDebug = new DeviceDebug(Device);
            }
            DisposeBufferViews();
            foreach (SharpDX.Direct3D11.RasterizerState rasterizerState in m_dxRasterizerStates.Values) {
                rasterizerState.Dispose();
            }
            m_dxRasterizerStates.Clear();
            foreach (SharpDX.Direct3D11.DepthStencilState depthStencilState in m_dxDepthStencilStates.Values) {
                depthStencilState.Dispose();
            }
            m_dxDepthStencilStates.Clear();
            foreach (SharpDX.Direct3D11.BlendState blendState in m_dxBlendStates.Values) {
                blendState.Dispose();
            }
            m_dxBlendStates.Clear();
            foreach (SharpDX.Direct3D11.SamplerState samplerState in m_dxSamplerStates.Values) {
                samplerState.Dispose();
            }
            m_dxSamplerStates.Clear();
            Utilities.Dispose(ref UserVertexBuffer);
            Utilities.Dispose(ref UserIndexBuffer);
            Utilities.Dispose(ref Context);
            Utilities.Dispose(ref SwapChain);
            Utilities.Dispose(ref Device);
            m_viewport = null;
            m_scissorRectangle = null;
            m_renderTargetView = null;
            m_depthStencilView = null;
            m_vertexShader = null;
            m_pixelShader = null;
            m_inputLayout = null;
            m_primitiveTopology = PrimitiveTopology.Undefined;
            m_vertexBuffer = null;
            m_vertexStride = 0;
            m_vertexOffset = 0;
            m_indexBuffer = null;
            m_indexOffset = 0;
            m_rasterizerState = null;
            m_depthStencilState = null;
            m_blendState = null;
            Array.Clear(m_vsSamplerStates, 0, m_vsSamplerStates.Length);
            Array.Clear(m_psSamplerStates, 0, m_psSamplerStates.Length);
            if (deviceDebug != null) {
                deviceDebug.ReportLiveDeviceObjects(ReportingLevel.Detail);
                deviceDebug.Dispose();
            }
        }

        public static bool ResizeSwapChainIfNeeded() {
            /*Matrix3x2 matrix3x = default(Matrix3x2);
            matrix3x.M11 = 1f / Window.m_swapChainPanel.CompositionScaleX;
            matrix3x.M22 = 1f / Window.m_swapChainPanel.CompositionScaleY;
            SwapChain.MatrixTransform = matrix3x;*/
            if (Window.Size.X != SwapChain.Description.ModeDescription.Width
                || Window.Size.Y != SwapChain.Description.ModeDescription.Height) {
                DisposeBufferViews();
                SwapChain.ResizeBuffers(2, Window.Size.X, Window.Size.Y, Format.R8G8B8A8_UNorm, SwapChainFlags.None);
                CreateBufferViews();
                return true;
            }
            return false;
        }

        public static void Present(int presentationInterval) {
            try {
                SwapChain.Present(presentationInterval, PresentFlags.None);
                if (ColorBufferView != null) {
                    Context.DiscardView(ColorBufferView);
                }
                if (DepthBufferView != null) {
                    Context.DiscardView(DepthBufferView);
                }
            }
            catch (SharpDXException ex) {
                if (ex.HResult == ResultCode.DeviceRemoved.Code
                    || ex.HResult == ResultCode.DeviceReset.Code) {
                    HandleDeviceLost();
                }
                else {
                    Log.Error("SwapChain.Present failed. Reason: {0}", ex.Message);
                }
            }
            catch (Exception ex2) {
                Log.Error("SwapChain.Present failed. Reason: {0}", ex2.Message);
            }
            m_renderTargetView = null;
            m_depthStencilView = null;
        }

        public static int AppendUserVertices<T>(T[] vertices, int vertexStride, int startVertex, int verticesCount) where T : struct {
            int num = vertexStride * startVertex;
            int num2 = vertexStride * verticesCount;
            if (UserVertexBuffer == null
                || num2 > m_userVertexBufferSize) {
                Utilities.Dispose(ref UserVertexBuffer);
                m_userVertexBufferOffset = 0;
                m_userVertexBufferSize = num2;
                UserVertexBuffer = new SharpDX.Direct3D11.Buffer(
                    Device,
                    new BufferDescription {
                        BindFlags = BindFlags.VertexBuffer,
                        Usage = ResourceUsage.Dynamic,
                        CpuAccessFlags = CpuAccessFlags.Write,
                        SizeInBytes = m_userVertexBufferSize
                    }
                );
            }
            if (m_userVertexBufferOffset + num2 <= m_userVertexBufferSize) {
                DataBox dataBox = Context.MapSubresource(UserVertexBuffer, 0, MapMode.WriteNoOverwrite, MapFlags.None);
                GCHandle gchandle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
                CopyMemory(gchandle.AddrOfPinnedObject() + num, dataBox.DataPointer + m_userVertexBufferOffset, num2);
                gchandle.Free();
                Context.UnmapSubresource(UserVertexBuffer, 0);
                int userVertexBufferOffset = m_userVertexBufferOffset;
                m_userVertexBufferOffset += num2;
                return userVertexBufferOffset;
            }
            DataBox dataBox2 = Context.MapSubresource(UserVertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            GCHandle gchandle2 = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            CopyMemory(gchandle2.AddrOfPinnedObject() + num, dataBox2.DataPointer, num2);
            gchandle2.Free();
            Context.UnmapSubresource(UserVertexBuffer, 0);
            m_userVertexBufferOffset = num2;
            return 0;
        }

        public static int AppendUserIndices(int[] indices, int indexStride, int startIndex, int indicesCount) {
            int num = indexStride * startIndex;
            int num2 = indexStride * indicesCount;
            if (UserIndexBuffer == null
                || num2 > m_userIndexBufferSize) {
                Utilities.Dispose(ref UserIndexBuffer);
                m_userIndexBufferOffset = 0;
                m_userIndexBufferSize = num2;
                UserIndexBuffer = new SharpDX.Direct3D11.Buffer(
                    Device,
                    new BufferDescription {
                        BindFlags = BindFlags.IndexBuffer,
                        Usage = ResourceUsage.Dynamic,
                        CpuAccessFlags = CpuAccessFlags.Write,
                        SizeInBytes = m_userIndexBufferSize
                    }
                );
            }
            if (m_userIndexBufferOffset + num2 <= m_userIndexBufferSize) {
                DataBox dataBox = Context.MapSubresource(UserIndexBuffer, 0, MapMode.WriteNoOverwrite, MapFlags.None);
                GCHandle gchandle = GCHandle.Alloc(indices, GCHandleType.Pinned);
                CopyMemory(gchandle.AddrOfPinnedObject() + num, dataBox.DataPointer + m_userIndexBufferOffset, num2);
                gchandle.Free();
                Context.UnmapSubresource(UserIndexBuffer, 0);
                int userIndexBufferOffset = m_userIndexBufferOffset;
                m_userIndexBufferOffset += num2;
                return userIndexBufferOffset;
            }
            DataBox dataBox2 = Context.MapSubresource(UserIndexBuffer, 0, MapMode.WriteDiscard, MapFlags.None);
            GCHandle gchandle2 = GCHandle.Alloc(indices, GCHandleType.Pinned);
            CopyMemory(gchandle2.AddrOfPinnedObject() + num, dataBox2.DataPointer, num2);
            gchandle2.Free();
            Context.UnmapSubresource(UserIndexBuffer, 0);
            m_userIndexBufferOffset = num2;
            return 0;
        }

        public static void ApplyViewportScissor(Viewport viewport, Rectangle scissorRectangle) {
            if (m_viewport == null
                || viewport != m_viewport.Value) {
                Context.Rasterizer.SetViewport(viewport.X, viewport.Y, viewport.Width, viewport.Height, viewport.MinDepth, viewport.MaxDepth);
                m_viewport = viewport;
            }
            if (m_scissorRectangle == null
                || scissorRectangle != m_scissorRectangle.Value) {
                Context.Rasterizer.SetScissorRectangle(
                    scissorRectangle.Left,
                    scissorRectangle.Top,
                    scissorRectangle.Left + scissorRectangle.Width,
                    scissorRectangle.Top + scissorRectangle.Height
                );
                m_scissorRectangle = scissorRectangle;
            }
        }

        public static void ApplyRasterizerState(RasterizerState rasterizerState) {
            if (rasterizerState != m_rasterizerState) {
                if (!m_dxRasterizerStates.TryGetValue(rasterizerState, out SharpDX.Direct3D11.RasterizerState rasterizerState2)) {
                    RasterizerStateDescription rasterizerStateDescription = RasterizerStateDescription.Default();
                    rasterizerStateDescription.FillMode = FillMode.Solid;
                    switch (rasterizerState.CullMode) {
                        case CullMode.None: rasterizerStateDescription.CullMode = SharpDX.Direct3D11.CullMode.None; break;
                        case CullMode.CullClockwise:
                            rasterizerStateDescription.CullMode = SharpDX.Direct3D11.CullMode.Back;
                            rasterizerStateDescription.IsFrontCounterClockwise = true;
                            break;
                        case CullMode.CullCounterClockwise:
                            rasterizerStateDescription.CullMode = SharpDX.Direct3D11.CullMode.Back;
                            rasterizerStateDescription.IsFrontCounterClockwise = false;
                            break;
                        default: throw new InvalidOperationException("Unsupported cull mode.");
                    }
                    rasterizerStateDescription.DepthBias = (int)rasterizerState.DepthBias;
                    rasterizerStateDescription.DepthBiasClamp = 0f;
                    rasterizerStateDescription.SlopeScaledDepthBias = rasterizerState.SlopeScaleDepthBias;
                    rasterizerStateDescription.IsDepthClipEnabled = true;
                    rasterizerStateDescription.IsScissorEnabled = rasterizerState.ScissorTestEnable;
                    rasterizerStateDescription.IsMultisampleEnabled = false;
                    rasterizerStateDescription.IsAntialiasedLineEnabled = false;
                    rasterizerState2 = new SharpDX.Direct3D11.RasterizerState(Device, rasterizerStateDescription);
                    m_dxRasterizerStates.Add(rasterizerState, rasterizerState2);
                }
                Context.Rasterizer.State = rasterizerState2;
                m_rasterizerState = rasterizerState;
            }
        }

        public static void ApplyDepthStencilState(DepthStencilState depthStencilState) {
            if (depthStencilState != m_depthStencilState) {
                if (!m_dxDepthStencilStates.TryGetValue(depthStencilState, out SharpDX.Direct3D11.DepthStencilState depthStencilState2)) {
                    DepthStencilStateDescription depthStencilStateDescription = DepthStencilStateDescription.Default();
                    depthStencilStateDescription.IsStencilEnabled = false;
                    depthStencilStateDescription.IsDepthEnabled = depthStencilState.DepthBufferTestEnable || depthStencilState.DepthBufferWriteEnable;
                    depthStencilStateDescription.DepthWriteMask = depthStencilState.DepthBufferWriteEnable ? DepthWriteMask.All : DepthWriteMask.Zero;
                    depthStencilStateDescription.DepthComparison = depthStencilState.DepthBufferTestEnable
                        ? TranslateCompareFunction(depthStencilState.DepthBufferFunction)
                        : Comparison.Always;
                    depthStencilState2 = new SharpDX.Direct3D11.DepthStencilState(Device, depthStencilStateDescription);
                    m_dxDepthStencilStates.Add(depthStencilState, depthStencilState2);
                }
                Context.OutputMerger.SetDepthStencilState(depthStencilState2);
                m_depthStencilState = depthStencilState;
            }
        }

        public static void ApplyBlendState(BlendState blendState) {
            if (blendState != m_blendState) {
                if (!m_dxBlendStates.TryGetValue(blendState, out SharpDX.Direct3D11.BlendState blendState2)) {
                    BlendStateDescription blendStateDescription = BlendStateDescription.Default();
                    blendStateDescription.RenderTarget[0].RenderTargetWriteMask = ColorWriteMaskFlags.All;
                    if (blendState.ColorBlendFunction == BlendFunction.Add
                        && blendState.ColorSourceBlend == Blend.One
                        && blendState.ColorDestinationBlend == Blend.Zero
                        && blendState.AlphaBlendFunction == BlendFunction.Add
                        && blendState.AlphaSourceBlend == Blend.One
                        && blendState.AlphaDestinationBlend == Blend.Zero) {
                        blendStateDescription.RenderTarget[0].IsBlendEnabled = false;
                    }
                    else {
                        blendStateDescription.RenderTarget[0].IsBlendEnabled = true;
                        blendStateDescription.RenderTarget[0].BlendOperation = TranslateBlendFunction(blendState.ColorBlendFunction);
                        blendStateDescription.RenderTarget[0].AlphaBlendOperation = TranslateBlendFunction(blendState.AlphaBlendFunction);
                        blendStateDescription.RenderTarget[0].SourceBlend = TranslateBlend(blendState.ColorSourceBlend);
                        blendStateDescription.RenderTarget[0].SourceAlphaBlend = TranslateBlend(blendState.AlphaSourceBlend);
                        blendStateDescription.RenderTarget[0].DestinationBlend = TranslateBlend(blendState.ColorDestinationBlend);
                        blendStateDescription.RenderTarget[0].DestinationAlphaBlend = TranslateBlend(blendState.AlphaDestinationBlend);
                    }
                    blendState2 = new SharpDX.Direct3D11.BlendState(Device, blendStateDescription);
                    m_dxBlendStates.Add(blendState, blendState2);
                }
                Color4 color = new(blendState.BlendFactor.X, blendState.BlendFactor.Y, blendState.BlendFactor.Z, blendState.BlendFactor.W);
                Context.OutputMerger.SetBlendState(blendState2, color);
                m_blendState = blendState;
            }
        }

        public static void ApplyVsSamplerState(int slot, SamplerState samplerState) {
            if (m_vsSamplerStates[slot] != samplerState) {
                Context.VertexShader.SetSampler(slot, GetDxSamplerState(samplerState));
                m_vsSamplerStates[slot] = samplerState;
            }
        }

        public static void ApplyPsSamplerState(int slot, SamplerState samplerState) {
            if (m_psSamplerStates[slot] != samplerState) {
                Context.PixelShader.SetSampler(slot, GetDxSamplerState(samplerState));
                m_psSamplerStates[slot] = samplerState;
            }
        }

        public static void ApplyShaderAndRenderTarget(RenderTarget2D renderTarget, Shader shader, VertexDeclaration vertexDeclaration) {
            shader.PrepareForDrawing();
            if (renderTarget != null) {
                if (renderTarget.m_colorTextureView != m_renderTargetView
                    || renderTarget.m_depthTextureView != m_depthStencilView) {
                    Context.OutputMerger.SetRenderTargets(renderTarget.m_depthTextureView, renderTarget.m_colorTextureView);
                    m_renderTargetView = renderTarget.m_colorTextureView;
                    m_depthStencilView = renderTarget.m_depthTextureView;
                }
            }
            else if (ColorBufferView != m_renderTargetView
                || DepthBufferView != m_depthStencilView) {
                Context.OutputMerger.SetRenderTargets(DepthBufferView, ColorBufferView);
                m_renderTargetView = ColorBufferView;
                m_depthStencilView = DepthBufferView;
            }
            InputLayout inputLayout;
            if (vertexDeclaration == shader.m_lastVertexDeclaration) {
                inputLayout = shader.m_lastInputLayout;
            }
            else {
                if (!shader.m_inputLayouts.TryGetValue(vertexDeclaration, out inputLayout)) {
                    InputElement[] array = new InputElement[vertexDeclaration.m_elements.Length];
                    for (int i = 0; i < array.Length; i++) {
                        VertexElement vertexElement = vertexDeclaration.m_elements[i];
                        array[i] = new InputElement {
                            SemanticName = vertexElement.SemanticName,
                            AlignedByteOffset = vertexElement.Offset,
                            Format = TranslateVertexElementFormat(vertexElement.Format),
                            Classification = InputClassification.PerVertexData,
                            Slot = 0,
                            SemanticIndex = vertexElement.SemanticIndex
                        };
                    }
                    inputLayout = new InputLayout(Device, shader.m_vertexShaderBytecode, array);
                    shader.m_inputLayouts.Add(vertexDeclaration, inputLayout);
                }
                shader.m_lastVertexDeclaration = vertexDeclaration;
                shader.m_lastInputLayout = inputLayout;
            }
            if (inputLayout != m_inputLayout) {
                Context.InputAssembler.InputLayout = inputLayout;
                m_inputLayout = inputLayout;
            }
            if (m_dataPointers == null
                || m_dataPointers.Length < shader.m_allConstantBuffers.Length) {
                m_dataPointers = new IntPtr[shader.m_allConstantBuffers.Length];
            }
            for (int j = 0; j < shader.m_parameters.Length; j++) {
                ShaderParameter shaderParameter = shader.m_parameters[j];
                if (shaderParameter.Type == ShaderParameterType.Texture2D) {
                    if (shaderParameter.IsChanged
                        || shader.m_pixelShader != m_pixelShader
                        || shader.m_vertexShader != m_vertexShader) {
                        Texture2D texture2D = (Texture2D)shaderParameter.Resource;
                        if (shaderParameter.VsResourceBindingSlot != -1) {
                            Context.VertexShader.SetShaderResource(
                                shaderParameter.VsResourceBindingSlot,
                                texture2D != null ? texture2D.m_textureView : null
                            );
                        }
                        if (shaderParameter.PsResourceBindingSlot != -1) {
                            Context.PixelShader.SetShaderResource(
                                shaderParameter.PsResourceBindingSlot,
                                texture2D != null ? texture2D.m_textureView : null
                            );
                        }
                        shaderParameter.IsChanged = false;
                    }
                }
                else if (shaderParameter.Type == ShaderParameterType.Sampler2D) {
                    if (shaderParameter.IsChanged
                        || shader.m_pixelShader != m_pixelShader
                        || shader.m_vertexShader != m_vertexShader) {
                        SamplerState samplerState = (SamplerState)shaderParameter.Resource;
                        if (samplerState != null) {
                            if (shaderParameter.VsResourceBindingSlot != -1) {
                                ApplyVsSamplerState(shaderParameter.VsResourceBindingSlot, samplerState);
                            }
                            if (shaderParameter.PsResourceBindingSlot != -1) {
                                ApplyPsSamplerState(shaderParameter.PsResourceBindingSlot, samplerState);
                            }
                        }
                        shaderParameter.IsChanged = false;
                    }
                }
                else if (shaderParameter.IsChanged) {
                    if (shaderParameter.VsBufferIndex != -1
                        && m_dataPointers[shaderParameter.VsBufferIndex] == IntPtr.Zero) {
                        SharpDX.Direct3D11.Buffer buffer = shader.m_allConstantBuffers[shaderParameter.VsBufferIndex];
                        m_dataPointers[shaderParameter.VsBufferIndex] = Context.MapSubresource(buffer, 0, MapMode.WriteDiscard, MapFlags.None)
                            .DataPointer;
                    }
                    if (shaderParameter.PsBufferIndex != -1
                        && m_dataPointers[shaderParameter.PsBufferIndex] == IntPtr.Zero) {
                        SharpDX.Direct3D11.Buffer buffer2 = shader.m_allConstantBuffers[shaderParameter.PsBufferIndex];
                        m_dataPointers[shaderParameter.PsBufferIndex] = Context.MapSubresource(buffer2, 0, MapMode.WriteDiscard, MapFlags.None)
                            .DataPointer;
                    }
                    shaderParameter.IsChanged = false;
                }
            }
            for (int k = 0; k < shader.m_allConstantBuffers.Length; k++) {
                if (m_dataPointers[k] != IntPtr.Zero) {
                    CopyMemory(shader.m_allConstantBuffersCpu[k], m_dataPointers[k], shader.m_allConstantBuffersSizes[k]);
                    Context.UnmapSubresource(shader.m_allConstantBuffers[k], 0);
                    m_dataPointers[k] = IntPtr.Zero;
                }
            }
            if (shader.m_vertexShader != m_vertexShader) {
                Context.VertexShader.Set(shader.m_vertexShader);
                m_vertexShader = shader.m_vertexShader;
                if (shader.m_vertexShaderConstantBuffers.Length != 0) {
                    Context.VertexShader.SetConstantBuffers(0, shader.m_vertexShaderConstantBuffers);
                }
            }
            if (shader.m_pixelShader != m_pixelShader) {
                Context.PixelShader.Set(shader.m_pixelShader);
                m_pixelShader = shader.m_pixelShader;
                if (shader.m_pixelShaderConstantBuffers.Length != 0) {
                    Context.PixelShader.SetConstantBuffers(0, shader.m_pixelShaderConstantBuffers);
                }
            }
        }

        public static void ApplyVertexBuffer(SharpDX.Direct3D11.Buffer vertexBuffer, int vertexStride, int vertexOffset) {
            if (vertexBuffer != m_vertexBuffer
                || vertexStride != m_vertexStride
                || vertexOffset != m_vertexOffset) {
                Context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, vertexStride, vertexOffset));
                m_vertexBuffer = vertexBuffer;
                m_vertexStride = vertexStride;
                m_vertexOffset = vertexOffset;
            }
        }

        public static void ApplyIndexBuffer(SharpDX.Direct3D11.Buffer indexBuffer, Format format, int indexOffset) {
            if (indexBuffer != m_indexBuffer
                || indexOffset != m_indexOffset) {
                Context.InputAssembler.SetIndexBuffer(indexBuffer, format, indexOffset);
                m_indexBuffer = indexBuffer;
                m_indexOffset = indexOffset;
            }
        }

        public static void ApplyPrimitiveType(PrimitiveType primitiveType) {
            PrimitiveTopology primitiveTopology = TranslatePrimitiveType(primitiveType);
            if (primitiveTopology != m_primitiveTopology) {
                Context.InputAssembler.PrimitiveTopology = primitiveTopology;
                m_primitiveTopology = primitiveTopology;
            }
        }

        public static SharpDX.Direct3D11.SamplerState GetDxSamplerState(SamplerState samplerState) {
            if (!m_dxSamplerStates.TryGetValue(samplerState, out SharpDX.Direct3D11.SamplerState samplerState2)) {
                SamplerStateDescription samplerStateDescription = SamplerStateDescription.Default();
                samplerStateDescription.AddressU = TranslateTextureAddressMode(samplerState.AddressModeU);
                samplerStateDescription.AddressV = TranslateTextureAddressMode(samplerState.AddressModeV);
                samplerStateDescription.AddressW = SharpDX.Direct3D11.TextureAddressMode.Wrap;
                samplerStateDescription.MaximumAnisotropy = samplerState.MaxAnisotropy;
                if (FeatureLevel >= FeatureLevel.Level_10_0) {
                    samplerStateDescription.MinimumLod = samplerState.MinLod;
                    samplerStateDescription.MaximumLod = samplerState.MaxLod;
                }
                samplerStateDescription.MipLodBias = samplerState.MipLodBias;
                samplerStateDescription.ComparisonFunction = Comparison.Always;
                samplerStateDescription.BorderColor = new Color4(0f, 0f, 0f, 1f);
                samplerStateDescription.Filter = TranslateTextureFilterMode(samplerState.FilterMode);
                samplerState2 = new SharpDX.Direct3D11.SamplerState(Device, samplerStateDescription);
                m_dxSamplerStates.Add(samplerState, samplerState2);
            }
            return samplerState2;
        }

        public static PrimitiveTopology TranslatePrimitiveType(PrimitiveType primitiveType) {
            switch (primitiveType) {
                case PrimitiveType.LineList: return PrimitiveTopology.LineList;
                case PrimitiveType.LineStrip: return PrimitiveTopology.LineStrip;
                case PrimitiveType.TriangleList: return PrimitiveTopology.TriangleList;
                case PrimitiveType.TriangleStrip: return PrimitiveTopology.TriangleStrip;
                default: throw new InvalidOperationException("Unsupported primitive type.");
            }
        }

        public static ShaderParameterType TranslateShaderTypeDescription(ShaderTypeDescription description) {
            return description.Type switch {
                ShaderVariableType.Float when description.Class == ShaderVariableClass.Scalar => ShaderParameterType.Float,
                ShaderVariableType.Float when description is { Class: ShaderVariableClass.Vector, ColumnCount: 2 } => ShaderParameterType.Vector2,
                ShaderVariableType.Float when description is { Class: ShaderVariableClass.Vector, ColumnCount: 3 } => ShaderParameterType.Vector3,
                ShaderVariableType.Float when description is { Class: ShaderVariableClass.Vector, ColumnCount: 4 } => ShaderParameterType.Vector4,
                ShaderVariableType.Float when description is { Class: ShaderVariableClass.MatrixColumns, RowCount: 4, ColumnCount: 4 } =>
                    ShaderParameterType.Matrix,
                _ => throw new InvalidOperationException(
                    string.Format("Variable \"{0}\" uses unsupported shader variable type.", new object[] { description.Name })
                )
            };
        }

        public static ShaderParameterType TranslateInputBindingDescription(InputBindingDescription description) {
            if (description is { Type: ShaderInputType.Texture, Dimension: ShaderResourceViewDimension.Texture2D, BindCount: 1 }) {
                return ShaderParameterType.Texture2D;
            }
            if (description is { Type: ShaderInputType.Sampler, BindCount: 1 }) {
                return ShaderParameterType.Sampler2D;
            }
            throw new InvalidOperationException(
                string.Format("Shader resource \"{0}\" uses unsupported shader resource type.", new object[] { description.Name })
            );
        }

        public static Format TranslateVertexElementFormat(VertexElementFormat vertexElementFormat) {
            switch (vertexElementFormat) {
                case VertexElementFormat.Single: return Format.R32_Float;
                case VertexElementFormat.Vector2: return Format.R32G32_Float;
                case VertexElementFormat.Vector3: return Format.R32G32B32_Float;
                case VertexElementFormat.Vector4: return Format.R32G32B32A32_Float;
                case VertexElementFormat.Byte4: return Format.R8G8B8A8_UInt;
                case VertexElementFormat.NormalizedByte4: return Format.R8G8B8A8_UNorm;
                case VertexElementFormat.Short2: return Format.R16G16_SInt;
                case VertexElementFormat.NormalizedShort2: return Format.R16G16_SNorm;
                case VertexElementFormat.Short4: return Format.R16G16B16A16_SInt;
                case VertexElementFormat.NormalizedShort4: return Format.R16G16B16A16_SNorm;
                default: throw new InvalidOperationException("Unsupported VertexElementFormat.");
            }
        }

        public static Format TranslateIndexFormat(IndexFormat indexFormat) {
            if (indexFormat == IndexFormat.SixteenBits) {
                return Format.R16_UInt;
            }
            if (indexFormat != IndexFormat.ThirtyTwoBits) {
                throw new InvalidOperationException("Unsupported IndexFormat.");
            }
            return Format.R32_UInt;
        }

        public static Format TranslateColorFormat(ColorFormat colorFormat) {
            switch (colorFormat) {
                case ColorFormat.Rgba8888: return Format.R8G8B8A8_UNorm;
                case ColorFormat.Rgba5551: return Format.B5G5R5A1_UNorm;
                case ColorFormat.Rgb565: return Format.B5G6R5_UNorm;
                case ColorFormat.R8: return Format.R8_UNorm;
                case ColorFormat.R32f: return Format.R32_Float;
                case ColorFormat.RG32f: return Format.R32G32_Float;
                case ColorFormat.RGBA32f: return Format.R32G32B32A32_Float;
                default: throw new InvalidOperationException("Unsupported ColorFormat.");
            }
        }

        public static Format TranslateDepthFormat(DepthFormat depthFormat) {
            if (depthFormat == DepthFormat.Depth16) {
                return Format.D16_UNorm;
            }
            if (depthFormat != DepthFormat.Depth24Stencil8) {
                throw new InvalidOperationException("Unsupported DepthFormat.");
            }
            return Format.D24_UNorm_S8_UInt;
        }

        public static BlendOperation TranslateBlendFunction(BlendFunction blendFunction) {
            switch (blendFunction) {
                case BlendFunction.Add: return BlendOperation.Add;
                case BlendFunction.Subtract: return BlendOperation.Subtract;
                case BlendFunction.ReverseSubtract: return BlendOperation.ReverseSubtract;
                default: throw new InvalidOperationException("Unsupported BlendFunction.");
            }
        }

        public static BlendOption TranslateBlend(Blend blend) {
            switch (blend) {
                case Blend.Zero: return BlendOption.Zero;
                case Blend.One: return BlendOption.One;
                case Blend.SourceColor: return BlendOption.SourceColor;
                case Blend.InverseSourceColor: return BlendOption.InverseSourceColor;
                case Blend.DestinationColor: return BlendOption.DestinationColor;
                case Blend.InverseDestinationColor: return BlendOption.InverseDestinationColor;
                case Blend.SourceAlpha: return BlendOption.SourceAlpha;
                case Blend.InverseSourceAlpha: return BlendOption.InverseSourceAlpha;
                case Blend.DestinationAlpha: return BlendOption.DestinationAlpha;
                case Blend.InverseDestinationAlpha: return BlendOption.InverseDestinationAlpha;
                case Blend.BlendFactor: return BlendOption.BlendFactor;
                case Blend.InverseBlendFactor: return BlendOption.InverseBlendFactor;
                case Blend.SourceAlphaSaturation: return BlendOption.SourceAlphaSaturate;
                default: throw new InvalidOperationException("Unsupported Blend.");
            }
        }

        public static SharpDX.Direct3D11.TextureAddressMode TranslateTextureAddressMode(TextureAddressMode textureAddressMode) {
            switch (textureAddressMode) {
                case TextureAddressMode.Clamp: return SharpDX.Direct3D11.TextureAddressMode.Clamp;
                case TextureAddressMode.Wrap: return SharpDX.Direct3D11.TextureAddressMode.Wrap;
                case TextureAddressMode.MirrorWrap: return SharpDX.Direct3D11.TextureAddressMode.Mirror;
                default: throw new InvalidOperationException("Unsupported TextureAddressMode.");
            }
        }

        public static Filter TranslateTextureFilterMode(TextureFilterMode textureFilterMode) {
            switch (textureFilterMode) {
                case TextureFilterMode.Point: return Filter.MinMagMipPoint;
                case TextureFilterMode.Linear: return Filter.MinMagMipLinear;
                case TextureFilterMode.Anisotropic: return Filter.Anisotropic;
                case TextureFilterMode.PointMipLinear: return Filter.MinMagPointMipLinear;
                case TextureFilterMode.LinearMipPoint: return Filter.MinMagLinearMipPoint;
                case TextureFilterMode.MinPointMagLinearMipPoint: return Filter.MinPointMagLinearMipPoint;
                case TextureFilterMode.MinPointMagLinearMipLinear: return Filter.MinPointMagMipLinear;
                case TextureFilterMode.MinLinearMagPointMipPoint: return Filter.MinLinearMagMipPoint;
                case TextureFilterMode.MinLinearMagPointMipLinear: return Filter.MinLinearMagPointMipLinear;
                default: throw new InvalidOperationException("Unsupported TextureFilterMode.");
            }
        }

        public static Comparison TranslateCompareFunction(CompareFunction compareFunction) {
            switch (compareFunction) {
                case CompareFunction.Always: return Comparison.Always;
                case CompareFunction.Never: return Comparison.Never;
                case CompareFunction.Less: return Comparison.Less;
                case CompareFunction.LessEqual: return Comparison.LessEqual;
                case CompareFunction.Equal: return Comparison.Equal;
                case CompareFunction.GreaterEqual: return Comparison.GreaterEqual;
                case CompareFunction.Greater: return Comparison.Greater;
                case CompareFunction.NotEqual: return Comparison.NotEqual;
                default: throw new InvalidOperationException("Unsupported CompareFunction.");
            }
        }

        public static void CreateBufferViews() {
            Point2 point;
            using (SharpDX.Direct3D11.Texture2D backBuffer = SwapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0)) {
                ColorBufferView = new RenderTargetView(Device, backBuffer);
                point = new Point2(backBuffer.Description.Width, backBuffer.Description.Height);
            }
            Texture2DDescription texture2DDescription = new() {
                ArraySize = 1,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = TranslateDepthFormat(DepthFormat.Depth24Stencil8),
                MipLevels = 1,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                Width = point.X,
                Height = point.Y
            };
            using (SharpDX.Direct3D11.Texture2D texture2D = new(Device, texture2DDescription)) {
                DepthBufferView = new DepthStencilView(Device, texture2D);
            }
        }

        public static void DisposeBufferViews() {
            Utilities.Dispose(ref ColorBufferView);
            Utilities.Dispose(ref DepthBufferView);
        }

        public static unsafe void CopyMemory(IntPtr source, IntPtr destination, int count) {
            int* ptr = (int*)source.ToPointer();
            int* ptr2 = (int*)((byte*)source.ToPointer() + (IntPtr)(count / 4) * 4);
            int* ptr3 = (int*)destination.ToPointer();
            while (ptr < ptr2) {
                *ptr3 = *ptr;
                ptr++;
                ptr3++;
            }
            byte* ptr4 = (byte*)ptr;
            byte* ptr5 = (byte*)source.ToPointer() + count;
            byte* ptr6 = (byte*)ptr3;
            while (ptr4 < ptr5) {
                *ptr6 = *ptr4;
                ptr4++;
                ptr6++;
            }
        }

        public static void HandleDeviceLost() {
            try {
                Log.Information("Device lost");
                Display.HandleDeviceLost();
                DisposeDevice();
                GC.Collect();
                CreateDevice();
                Display.Resize();
                Display.HandleDeviceReset();
                Log.Information("Device reset");
            }
            catch (Exception ex) {
                Log.Error("Failed to recreate graphics device. Reason: {0}", ex.Message);
            }
        }
    }
}
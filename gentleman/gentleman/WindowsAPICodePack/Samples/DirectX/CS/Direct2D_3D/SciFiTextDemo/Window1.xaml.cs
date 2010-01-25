// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

using Microsoft.WindowsAPICodePack.DirectX;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;

using D3D10 = Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace SciFiTextDemo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1
    {
        object syncObject = new object();

        string text =
            "Episode CCCXLVII:\nA Misguided Hope\n\n"
            + "Not so long ago, in a cubicle not so far away...\n\n"
            + "It is days before milestone lockdown. A small group of rebel developers toil through the weekend, relentlessly fixing bugs in defiance of familial obligations. Aside from pride in their work, their only reward will be takeout food and cinema gift certificates.\n\n"
            + "Powered by coffee and soda, our hyper-caffeinated heroine stares at her screen with glazed-over eyes. She repeatedly slaps her face in a feeble attempt to stay awake. Lapsing into micro-naps, she reluctantly takes a break from debugging to replenish her caffeine levels.\n\n"
            + "On her way to the kitchen she spots a fallen comrade, passed out on his keyboard and snoring loudly. After downing two coffees, she fills a pitcher with ice water and...";

        // The factories
        D2DFactory d2DFactory;
        DWriteFactory dWriteFactory;

        bool pause;
        int lastSavedDelta;

        //Device-Dependent Resources
        D3DDevice1 device;
        SwapChain swapChain;
        RasterizerState rasterizerState;
        Texture2D depthStencil;
        DepthStencilView depthStencilView;
        RenderTargetView renderTargetView;
        Texture2D offscreenTexture;
        Effect shader;
        D3DBuffer vertexBuffer;
        InputLayout vertexLayout;
        D3DBuffer facesIndexBuffer;
        ShaderResourceView textureResourceView;

        RenderTarget renderTarget;
        LinearGradientBrush textBrush;

        BitmapRenderTarget opacityRenderTarget;
        bool isOpacityRTPopulated;

        EffectTechnique technique;
        EffectMatrixVariable worldMatrixVariable;
        EffectMatrixVariable viewMatrixVariable;
        EffectMatrixVariable projectionMarixVariable;
        EffectShaderResourceVariable diffuseVariable;

        // Device-Independent Resources
        TextFormat textFormat;

        Matrix4x4F worldMatrix;
        Matrix4x4F viewMatrix;
        Matrix4x4F projectionMatrix;

        ColorRgba backColor = new ColorRgba(Colors.Black);

        float currentTimeVariation;
        int startTime = Environment.TickCount;

        InputElementDescription[] inputLayoutDescriptions =
        {
                new InputElementDescription
                    {
                    SemanticName = "POSITION",
                    SemanticIndex = 0,
                    Format = Format.R32G32B32_FLOAT,
                    InputSlot = 0,
                    AlignedByteOffset = 0,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                },
                new InputElementDescription
                    {
                    SemanticName = "TEXCOORD",
                    SemanticIndex = 0,
                    Format = Format.R32G32_FLOAT,
                    InputSlot = 0,
                    AlignedByteOffset = 12,
                    InputSlotClass = InputClassification.PerVertexData,
                    InstanceDataStepRate = 0
                }
        };

        VertexData VertexArray = new VertexData();

        public Window1()
        {
            InitializeComponent();
            textBox.Text = text;
            host.Loaded += new RoutedEventHandler(host_Loaded);
            host.SizeChanged += new SizeChangedEventHandler(host_SizeChanged);
        }

        void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lock (syncObject)
            {
                if (device == null)
                    return;
                uint nWidth = (uint)host.ActualWidth;
                uint nHeight = (uint)host.ActualHeight;

                device.OM.SetRenderTargets(new RenderTargetView[] { null }, null);
                //need to remove the reference to the swapchain's backbuffer to enable ResizeBuffers() call
                renderTargetView.Dispose();
                depthStencilView.Dispose();
                depthStencil.Dispose();

                device.RS.SetViewports(null);

                SwapChainDescription sd = swapChain.Description;
                //Change the swap chain's back buffer size, format, and number of buffers
                swapChain.ResizeBuffers(
                    sd.BufferCount,
                    nWidth,
                    nHeight,
                    sd.BufferDescription.Format,
                    sd.Flags);

                using (Texture2D pBuffer = swapChain.GetBuffer<Texture2D>(0))
                {
                    renderTargetView = device.CreateRenderTargetView(pBuffer);
                }

                InitializeDepthStencil(nWidth, nHeight);

                // bind the views to the device
                device.OM.SetRenderTargets(new[] { renderTargetView }, depthStencilView);

                SetViewport(nWidth, nHeight);

                // update the aspect ratio
                projectionMatrix = Camera.MatrixPerspectiveFovLH(
                    (float)Math.PI * 0.1f, // fovy
                    nWidth / (float)nHeight, // aspect
                    0.1f, // zn
                    100.0f // zf
                    );
                projectionMarixVariable.Matrix = projectionMatrix;
            }
        }

        void host_Loaded(object sender, RoutedEventArgs e)
        {
            CreateDeviceIndependentResources();
            startTime = Environment.TickCount;
            host.Render = RenderScene;
        }

        static Effect LoadResourceShader(D3DDevice device, string resourceName)
        {
            using (Stream stream = Application.ResourceAssembly.GetManifestResourceStream(resourceName))
            {
                return device.CreateEffectFromCompiledBinary(stream);
            }
        }

        void CreateDeviceIndependentResources()
        {
            // Create a Direct2D factory.
            d2DFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);

            // Create a DirectWrite factory.
            dWriteFactory = DWriteFactory.CreateFactory();

            // Create a DirectWrite text format object.
            textFormat = dWriteFactory.CreateTextFormat("Calibri", 50, DWrite.FontWeight.Bold, DWrite.FontStyle.Normal, DWrite.FontStretch.Normal);

            // Center the text both horizontally and vertically.
            textFormat.TextAlignment = DWrite.TextAlignment.Leading;
            textFormat.ParagraphAlignment = ParagraphAlignment.Near;
        }

        void CreateDeviceResources()
        {
            uint width = (uint) host.ActualWidth;
            uint height = (uint) host.ActualHeight;

            // If we don't have a device, need to create one now and all
            // accompanying D3D resources.
            CreateDevice();

            DXGIFactory dxgiFactory = DXGIFactory.CreateFactory();

            SwapChainDescription swapDesc = new SwapChainDescription();
            swapDesc.BufferDescription.Width = width;
            swapDesc.BufferDescription.Height = height;
            swapDesc.BufferDescription.Format = Format.R8G8B8A8_UNORM;
            swapDesc.BufferDescription.RefreshRate.Numerator = 60;
            swapDesc.BufferDescription.RefreshRate.Denominator = 1;
            swapDesc.SampleDescription.Count = 1;
            swapDesc.SampleDescription.Quality = 0;
            swapDesc.BufferUsage = UsageOption.RenderTargetOutput;
            swapDesc.BufferCount = 1;
            swapDesc.OutputWindowHandle = host.Handle;
            swapDesc.Windowed = true;

            swapChain = dxgiFactory.CreateSwapChain(
                device, swapDesc);

            // Create rasterizer state object
            RasterizerDescription rsDesc = new RasterizerDescription();
            rsDesc.AntialiasedLineEnable = false;
            rsDesc.CullMode = CullMode.None;
            rsDesc.DepthBias = 0;
            rsDesc.DepthBiasClamp = 0;
            rsDesc.DepthClipEnable = true;
            rsDesc.FillMode = D3D10.FillMode.Solid;
            rsDesc.FrontCounterClockwise = false; // Must be FALSE for 10on9
            rsDesc.MultisampleEnable = false;
            rsDesc.ScissorEnable = false;
            rsDesc.SlopeScaledDepthBias = 0;

            rasterizerState = device.CreateRasterizerState(
                rsDesc);

            device.RS.SetState(
                rasterizerState
                );

            // If we don't have a D2D render target, need to create all of the resources
            // required to render to one here.
            // Ensure that nobody is holding onto one of the old resources
            device.OM.SetRenderTargets(new RenderTargetView[] {null});

            InitializeDepthStencil(width, height);

            // Create views on the RT buffers and set them on the device
            RenderTargetViewDescription renderDesc = new RenderTargetViewDescription();
            renderDesc.Format = Format.R8G8B8A8_UNORM;
            renderDesc.ViewDimension = RenderTargetViewDimension.Texture2D;
            renderDesc.Texture2D.MipSlice = 0;

            using (D3DResource spBackBufferResource = swapChain.GetBuffer<D3DResource>(0))
            {
                renderTargetView = device.CreateRenderTargetView(
                    spBackBufferResource,
                    renderDesc);
            }

            device.OM.SetRenderTargets(new RenderTargetView[] {renderTargetView}, depthStencilView);

            SetViewport(width, height);


            // Create a D2D render target which can draw into the surface in the swap chain
            RenderTargetProperties props =
                new RenderTargetProperties(
                    RenderTargetType.Default, new PixelFormat(Format.Unknown, AlphaMode.Premultiplied),
                    96, 96, RenderTargetUsage.None, FeatureLevel.Default);

            // Allocate a offscreen D3D surface for D2D to render our 2D content into
            Texture2DDescription tex2DDescription;
            tex2DDescription.ArraySize = 1;
            tex2DDescription.BindFlags = BindFlag.RenderTarget | BindFlag.ShaderResource;
            tex2DDescription.CpuAccessFlags = CpuAccessFlag.Unspecified;
            tex2DDescription.Format = Format.R8G8B8A8_UNORM;
            tex2DDescription.Height = 4096;
            tex2DDescription.Width = 512;
            tex2DDescription.MipLevels = 1;
            tex2DDescription.MiscFlags = 0;
            tex2DDescription.SampleDescription.Count = 1;
            tex2DDescription.SampleDescription.Quality = 0;
            tex2DDescription.Usage = Usage.Default;

            offscreenTexture = device.CreateTexture2D(tex2DDescription);

            using (Surface dxgiSurface = offscreenTexture.GetDXGISurface())
            {
                // Create a D2D render target which can draw into our offscreen D3D surface
                renderTarget = d2DFactory.CreateDxgiSurfaceRenderTarget(
                    dxgiSurface,
                    props);
            }

            PixelFormat alphaOnlyFormat = new PixelFormat(Format.A8_UNORM, AlphaMode.Premultiplied);

            opacityRenderTarget = renderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None,
                                                                            alphaOnlyFormat);

            // Load pixel shader
            // Open precompiled vertex shader
            // This file was compiled using DirectX's SDK Shader compilation tool: 
            // fxc.exe /T fx_4_0 /Fo SciFiText.fxo SciFiText.fx
            shader = LoadResourceShader(device, "SciFiTextDemo.SciFiText.fxo");

            // Obtain the technique
            technique = shader.GetTechniqueByName("Render");

            // Obtain the variables
            worldMatrixVariable = shader.GetVariableByName("World").AsMatrix();
            viewMatrixVariable = shader.GetVariableByName("View").AsMatrix();
            projectionMarixVariable = shader.GetVariableByName("Projection").AsMatrix();
            diffuseVariable = shader.GetVariableByName("txDiffuse").AsShaderResource();

            // Create the input layout
            PassDescription passDesc = new PassDescription();
            passDesc = technique.GetPassByIndex(0).Description;

            vertexLayout = device.CreateInputLayout(
                inputLayoutDescriptions,
                passDesc.InputAssemblerInputSignature,
                passDesc.InputAssemblerInputSignatureSize
                );

            // Set the input layout
            device.IA.SetInputLayout(
                vertexLayout
                );

            IntPtr verticesDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(VertexArray.VerticesInstance));
            Marshal.StructureToPtr(VertexArray.VerticesInstance, verticesDataPtr, true);

            BufferDescription bd = new BufferDescription();
            bd.Usage = Usage.Default;
            bd.ByteWidth = (uint) Marshal.SizeOf(VertexArray.VerticesInstance);
            bd.BindFlags = BindFlag.VertexBuffer;
            bd.CpuAccessFlags = CpuAccessFlag.Unspecified;
            bd.MiscFlags = ResourceMiscFlag.Undefined;

            SubresourceData InitData = new SubresourceData()
                                           {
                                               SysMem = verticesDataPtr
                                           };


            vertexBuffer = device.CreateBuffer(
                bd,
                InitData
                );

            Marshal.FreeHGlobal(verticesDataPtr);

            // Set vertex buffer
            uint stride = (uint) Marshal.SizeOf(typeof (SimpleVertex));
            uint offset = 0;

            device.IA.SetVertexBuffers(
                0,
                new D3DBuffer[] {vertexBuffer},
                new uint[] {stride},
                new uint[] {offset}
                );

            IntPtr indicesDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(VertexArray.IndicesInstance));
            Marshal.StructureToPtr(VertexArray.IndicesInstance, indicesDataPtr, true);

            bd.Usage = Usage.Default;
            bd.ByteWidth = (uint) Marshal.SizeOf(VertexArray.IndicesInstance);
            bd.BindFlags = BindFlag.IndexBuffer;
            bd.CpuAccessFlags = 0;
            bd.MiscFlags = 0;

            InitData.SysMem = indicesDataPtr;

            facesIndexBuffer = device.CreateBuffer(
                bd,
                InitData
                );

            Marshal.FreeHGlobal(indicesDataPtr);

            // Set primitive topology
            device.IA.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

            // Convert the D2D texture into a Shader Resource View
            textureResourceView = device.CreateShaderResourceView(
                offscreenTexture);

            // Initialize the world matrices
            worldMatrix = Matrix4x4F.Identity;

            // Initialize the view matrix
            Vector3F Eye = new Vector3F(0.0f, 0.0f, 13.0f);
            Vector3F At = new Vector3F(0.0f, -3.5f, 45.0f);
            Vector3F Up = new Vector3F(0.0f, 1.0f, 0.0f);

            viewMatrix = Camera.MatrixLookAtLH(Eye, At, Up);

            // Initialize the projection matrix
            projectionMatrix = Camera.MatrixPerspectiveFovLH(
                (float) Math.PI*0.1f,
                width/(float) height,
                0.1f,
                100.0f);

            // Update Variables that never change
            viewMatrixVariable.Matrix = viewMatrix;

            projectionMarixVariable.Matrix = projectionMatrix;

            GradientStop[] gradientStops =
                {
                    new GradientStop(0.0f, new ColorF(Colors.Yellow)),
                    new GradientStop(1.0f, new ColorF(Colors.Black))
                };

            GradientStopCollection spGradientStopCollection = renderTarget.CreateGradientStopCollection(
                gradientStops,
                Gamma.Gamma_22,
                ExtendMode.Clamp);

            // Create a linear gradient brush for text
            textBrush = renderTarget.CreateLinearGradientBrush(
                new LinearGradientBrushProperties(new Point2F(0, 0), new Point2F(0, -2048)),
                spGradientStopCollection
                );
        }

        private void CreateDevice()
        {
            try
            {
                // Create device
                device = D3DDevice1.CreateDevice1(
                    null,
                    DriverType.Hardware,
                    null,
                    CreateDeviceFlag.SupportBGRA,
                    FeatureLevel.FeatureLevel_10_0);
            }
            catch (Exception)
            {
                // if we can't create a hardware device,
                // try the warp one
            }
            if (device == null)
            {
                device = D3DDevice1.CreateDevice1(
                    null,
                    DriverType.Software,
                    "d3d10warp.dll",
                    CreateDeviceFlag.SupportBGRA,
                    FeatureLevel.FeatureLevel_10_0);
            }
        }

        void RenderScene()
        {
            lock (syncObject)
            {
                if (device == null)
                    CreateDeviceResources();

                if (!pause)
                {
                    if (lastSavedDelta != 0)
                    {
                        startTime = Environment.TickCount - lastSavedDelta;
                        lastSavedDelta = 0;
                    }
                    currentTimeVariation = (Environment.TickCount - startTime)/6000.0f;
                    worldMatrix = MatrixMath.MatrixTranslate(0, 0, currentTimeVariation);
                    textBrush.Transform = Matrix3x2F.Translation(0, (4096f/16f)*currentTimeVariation);
                }

                device.ClearDepthStencilView(
                    depthStencilView,
                    ClearFlag.Depth,
                    1,
                    0
                    );

                // Clear the back buffer
                device.ClearRenderTargetView(renderTargetView, backColor);

                diffuseVariable.SetResource(null);

                technique.GetPassByIndex(0).Apply();

                // Draw the D2D content into our D3D surface
                RenderD2DContentIntoSurface();

                diffuseVariable.SetResource(
                    textureResourceView
                    );

                // Update variables
                worldMatrixVariable.Matrix = worldMatrix;

                // Set index buffer
                device.IA.SetIndexBuffer(
                    facesIndexBuffer,
                    Format.R16_UINT,
                    0
                    );

                // Draw the scene
                technique.GetPassByIndex(0).Apply();

                device.DrawIndexed((uint) Marshal.SizeOf(VertexArray.VerticesInstance), 0, 0);

                swapChain.Present(0, PresentFlag.Default);
            }
        }

        void RenderD2DContentIntoSurface()
        {
            SizeF rtSize = renderTarget.Size;

            renderTarget.BeginDraw();

            if (!isOpacityRTPopulated)
            {
                opacityRenderTarget.BeginDraw();

                opacityRenderTarget.Transform = Matrix3x2F.Identity;

                opacityRenderTarget.Clear(new ColorF(Colors.Black, 0));

                opacityRenderTarget.DrawText(
                    text,
                    textFormat,
                    new RectF(
                        0,
                        0,
                        rtSize.Width,
                        rtSize.Height
                        ),
                    textBrush
                    );

                opacityRenderTarget.EndDraw();

                isOpacityRTPopulated = true;
            }

            renderTarget.Clear(new ColorF(Colors.Black));

            renderTarget.AntialiasMode = AntialiasMode.Aliased;

            D2DBitmap spBitmap = opacityRenderTarget.GetBitmap();

            renderTarget.FillOpacityMask(
                spBitmap,
                textBrush,
                OpacityMaskContent.TextNatural,
                new RectF(0, 0, rtSize.Width, rtSize.Height),
                new RectF(0, 0, rtSize.Width, rtSize.Height)
                );

            renderTarget.EndDraw();
        }

        #region InitializeDepthStencil()
        private void InitializeDepthStencil(uint nWidth, uint nHeight)
        {
            // Create depth stencil texture
            Texture2DDescription descDepth = new Texture2DDescription()
                                                 {
                                                     Width = nWidth,
                                                     Height = nHeight,
                                                     MipLevels = 1,
                                                     ArraySize = 1,
                                                     Format = Format.D16_UNORM,
                                                     SampleDescription = new SampleDescription()
                                                                             {
                                                                                 Count = 1,
                                                                                 Quality = 0
                                                                             },
                                                     BindFlags = BindFlag.DepthStencil,
                                                 };
            depthStencil = device.CreateTexture2D(descDepth);

            // Create the depth stencil view
            DepthStencilViewDescription depthStencilViewDesc = new DepthStencilViewDescription()
                                                                   {
                                                                       Format = descDepth.Format,
                                                                       ViewDimension =
                                                                           DepthStencilViewDimension.
                                                                           Texture2D
                                                                   };
            depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc);
        }
        #endregion

        #region SetViewport()
        private void SetViewport(uint nWidth, uint nHeight)
        {
            Viewport viewport = new Viewport
            {
                Width = nWidth,
                Height = nHeight,
                TopLeftX = 0,
                TopLeftY = 0,
                MinDepth = 0,
                MaxDepth = 1
            };
            device.RS.SetViewports(new[] { viewport });
        }
        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            pause = !pause;

            if (pause)
            {
                lastSavedDelta = Environment.TickCount - startTime;
                actionText.Text = "Resume Text";
            }
            else
                actionText.Text = "Pause Text";
        }

        private void textBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            text = textBox.Text;
            isOpacityRTPopulated = false;
        }
    }
}

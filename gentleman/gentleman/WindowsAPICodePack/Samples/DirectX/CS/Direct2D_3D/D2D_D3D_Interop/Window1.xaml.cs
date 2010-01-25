// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.DirectWrite;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;
using Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent;
using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;

namespace Microsoft.WindowsAPICodePack.DirectX.Samples
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1
    {
        #region Fields
        object syncObject = new object();
        const string HelloWorldText = "Hello, World!";
        D2DFactory d2DFactory;
        ImagingFactory imagingFactory;
        DWriteFactory dWriteFactory;

        float currentTicks;
        readonly int startTime = Environment.TickCount;
        int lastTicks;
        float fps;

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
        Surface textureSurface;

        RenderTarget backBufferRenderTarget;
        SolidColorBrush backBufferTextBrush;
        LinearGradientBrush backBufferGradientBrush;
        BitmapBrush gridPatternBitmapBrush;

        RenderTarget textureRenderTarget;
        LinearGradientBrush linearGradientBrush;
        SolidColorBrush blackBrush;
        D2DBitmap d2dBitmap;

        EffectTechnique technique;
        EffectMatrixVariable worldVariable;
        EffectMatrixVariable viewVariable;
        EffectMatrixVariable projectionVariable;
        EffectShaderResourceVariable diffuseVariable;

        // Device-Independent Resources
        TextFormat textFormat;
        TextFormat textFormatFps;
        PathGeometry pathGeometry;

        Matrix4x4F worldMatrix;
        Matrix4x4F viewMatrix;
        Matrix4x4F projectionMatrix;

        #region Read-only initialization values
        readonly VertexData vertexArray = new VertexData();

        readonly InputElementDescription[] inputLayouts =
        {
            new InputElementDescription { SemanticName = "POSITION", SemanticIndex = 0, Format = Format.R32G32B32_FLOAT, InputSlot = 0, AlignedByteOffset = 0, InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0 },
            new InputElementDescription {  SemanticName = "TEXCOORD", SemanticIndex = 0, Format = Format.R32G32_FLOAT, InputSlot =0, AlignedByteOffset = 12, InputSlotClass = InputClassification.PerVertexData, InstanceDataStepRate = 0}
        };

        readonly RenderTargetProperties renderTargetProperties =
            new RenderTargetProperties(
                  RenderTargetType.Default,
                new PixelFormat(Format.Unknown, AlphaMode.Premultiplied),
                96,
                96,
                RenderTargetUsage.None,
                FeatureLevel.Default
                );

        readonly GradientStop[] stopsBackground =
            {
                new GradientStop ( 0.0f, new ColorF(Colors.Blue)),
                new GradientStop ( 1.0f, new ColorF(Colors.Black))
            };

        readonly GradientStop[] stopsGeometry =
                {
                    new GradientStop ( 0.0f, new ColorF( Colors.LightBlue)),
                    new GradientStop (  1.0f, new ColorF( Colors.Blue)),
                };
        #endregion
        #endregion

        public Window1()
        {
            InitializeComponent();
        }

        void host_Loaded(object sender, RoutedEventArgs e)
        {
            CreateDeviceIndependentResources();
            host.SizeChanged += host_SizeChanged;
            host.Render = RenderScene;
        }

        #region RenderScene()
        void RenderScene()
        {
            lock (syncObject)
            {
                //initialize D3D device and D2D render targets the first time we get here
                if (device == null)
                    CreateDeviceResources();

                //tick count is used to control animation and calculate FPS
                int currentTime = Environment.TickCount;
                currentTicks = currentTime - startTime;

                float a = (currentTicks * 360.0f) * ((float)Math.PI / 180.0f) * 0.0001f;
                worldMatrix = MatrixMath.MatrixRotationY(a);

                // Swap chain will tell us how big the back buffer is
                SwapChainDescription swapDesc = swapChain.Description;
                uint nWidth = swapDesc.BufferDescription.Width;
                uint nHeight = swapDesc.BufferDescription.Height;

                device.ClearDepthStencilView(
                    depthStencilView, ClearFlag.Depth,
                    1, 0
                    );

                // Draw a gradient background before we draw the cube
                if (backBufferRenderTarget != null)
                {
                    backBufferRenderTarget.BeginDraw();

                    backBufferGradientBrush.Transform =
                        Matrix3x2F.Scale(
                            backBufferRenderTarget.Size,
                            new Point2F(0.0f, 0.0f));

                    RectF rect = new RectF(
                        0.0f, 0.0f,
                        nWidth,
                        nHeight);

                    backBufferRenderTarget.FillRectangle(rect, backBufferGradientBrush);
                    backBufferRenderTarget.EndDraw();
                }

                diffuseVariable.SetResource(null);
                technique.GetPassByIndex(0).Apply();

                // Draw the D2D content into a D3D surface.
                RenderD2DContentIntoTexture();

                // Pass the updated texture to the pixel shader
                diffuseVariable.SetResource(textureResourceView);

                // Update variables that change once per frame.
                worldVariable.Matrix = worldMatrix;

                // Set the index buffer.
                device.IA.SetIndexBuffer(
                    facesIndexBuffer,
                    Format.R16_UINT,
                    0
                    );

                // Render the scene
                technique.GetPassByIndex(0).Apply();

                device.DrawIndexed(
                    vertexArray.s_FacesIndexArray.Length,
                    0,
                    0
                    );
                // Update fps

                currentTime = Environment.TickCount; // Get the ticks again
                currentTicks = currentTime - startTime;
                if ((currentTime - lastTicks) > 250)
                {
                    fps = (swapChain.LastPresentCount) / (currentTicks / 1000f);
                    lastTicks = currentTime;
                }

                backBufferRenderTarget.BeginDraw();

                // Draw fps
                backBufferRenderTarget.DrawText(
                    String.Format("Average FPS: {0:F1}", fps),
                    textFormatFps,
                    new RectF(
                        10f,
                        nHeight - 32f,
                        nWidth,
                        nHeight
                        ),
                    backBufferTextBrush
                    );

                backBufferRenderTarget.EndDraw();

                swapChain.Present(0, PresentFlag.Default);
            }
        }
        #endregion

        #region RenderD2DContentIntoTexture()
        void RenderD2DContentIntoTexture()
        {
            SizeF rtSize = textureRenderTarget.Size;

            textureRenderTarget.BeginDraw();

            textureRenderTarget.Transform = Matrix3x2F.Identity;
            textureRenderTarget.Clear(new ColorF(Colors.White));

            textureRenderTarget.FillRectangle(
                new RectF(0.0f, 0.0f, rtSize.Width, rtSize.Height),
                gridPatternBitmapBrush);

            SizeF size = d2dBitmap.Size;

            textureRenderTarget.DrawBitmap(
                d2dBitmap, 1.0f, BitmapInterpolationMode.Linear,
                new RectF(
                0.0f,
                0.0f,
                size.Width,
                size.Height)
                );

            // Draw the bitmap at the bottom corner of the window
            textureRenderTarget.DrawBitmap(
                d2dBitmap, 1.0f, BitmapInterpolationMode.Linear,
                new RectF(
                rtSize.Width - size.Width,
                rtSize.Height - size.Height,
                rtSize.Width,
                rtSize.Height));

            // Set the world transform to rotatate the drawing around the center of the render target
            // and write "Hello World"
            float angle = 0.1f * Environment.TickCount;
            textureRenderTarget.Transform
                = Matrix3x2F.Rotation(
                    angle,
                    new Point2F(
                        rtSize.Width / 2,
                        rtSize.Height / 2
                        ));

            textureRenderTarget.DrawText(
                HelloWorldText,
                textFormat,
                new RectF(
                    0,
                    0,
                    rtSize.Width,
                    rtSize.Height
                    ),
                blackBrush
                );

            // Reset back to the identity transform
            textureRenderTarget.Transform
                = Matrix3x2F.Translation(
                    0,
                    rtSize.Height - 200
                    );

            textureRenderTarget.FillGeometry(
                pathGeometry,
                linearGradientBrush);

            textureRenderTarget.Transform =
                Matrix3x2F.Translation(
                    rtSize.Width - 200,
                    0
                    );

            textureRenderTarget.FillGeometry(
                pathGeometry,
                linearGradientBrush
                );

            textureRenderTarget.EndDraw();
        }
        #endregion

        #region host_SizeChanged()
        void host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lock (syncObject)
            {
                if (device != null)
                {
                    uint nWidth = (uint)host.ActualWidth;
                    uint nHeight = (uint)host.ActualHeight;

                    backBufferRenderTarget.Dispose();
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

                    CreateBackBufferD2DRenderTarget();

                    // update the aspect ratio
                    projectionMatrix = Camera.MatrixPerspectiveFovLH(
                        (float)Math.PI * 0.24f, // fovy
                        nWidth / (float)nHeight, // aspect
                        0.1f, // zn
                        100.0f // zf
                        );
                    projectionVariable.Matrix = projectionMatrix;

                }
            }
        }
        #endregion

        #region CreateDeviceResources()
        void CreateDeviceResources()
        {
            uint nWidth = (uint)host.ActualWidth;
            uint nHeight = (uint)host.ActualHeight;

            // Create D3D device and swap chain
            SwapChainDescription swapDesc = new SwapChainDescription();
            swapDesc.BufferDescription.Width = nWidth;
            swapDesc.BufferDescription.Height = nHeight;
            swapDesc.BufferDescription.Format = Format.R8G8B8A8_UNORM;
            swapDesc.BufferDescription.RefreshRate.Numerator = 60;
            swapDesc.BufferDescription.RefreshRate.Denominator = 1;
            swapDesc.SampleDescription.Count = 1;
            swapDesc.SampleDescription.Quality = 0;
            swapDesc.BufferUsage = UsageOption.RenderTargetOutput;
            swapDesc.BufferCount = 1;
            swapDesc.OutputWindowHandle = host.Handle;
            swapDesc.Windowed = true;
            device = D3DDevice1.CreateDeviceAndSwapChain1(
                null,
                DriverType.Hardware,
                null,
                CreateDeviceFlag.SupportBGRA,
                FeatureLevel.FeatureLevel_10_0,
                swapDesc,
                out swapChain
                );

            using (Texture2D pBuffer = swapChain.GetBuffer<Texture2D>(0))
            {
                renderTargetView = device.CreateRenderTargetView(pBuffer);
            }

            MakeBothSidesRendered();
            InitializeDepthStencil(nWidth, nHeight);

            device.OM.SetRenderTargets(new[] { renderTargetView }, depthStencilView);

            // Set a new viewport based on the new dimensions
            SetViewport(nWidth, nHeight);

            // Load pixel shader
            shader = LoadResourceShader(device, "Microsoft.WindowsAPICodePack.DirectX.Samples.dxgisample.fxo");

            // Obtain the technique
            technique = shader.GetTechniqueByName("Render");

            // Create the input layout
            InitializeGeometryBuffers();

            // Obtain the variables
            Initialize3DTransformations(nWidth, nHeight);

            // Allocate a offscreen D3D surface for D2D to render our 2D content into
            InitializeTextureRenderTarget();

            // Create a D2D render target which can draw into the surface in the swap chain
            CreateD2DRenderTargets();
        }
        #endregion

        #region Initialize3DTransformations()
        private void Initialize3DTransformations(uint nWidth, uint nHeight)
        {
            worldVariable = shader.GetVariableByName("World").AsMatrix();
            viewVariable = shader.GetVariableByName("View").AsMatrix();
            projectionVariable = shader.GetVariableByName("Projection").AsMatrix();
            diffuseVariable = shader.GetVariableByName("txDiffuse").AsShaderResource();

            worldMatrix = Matrix4x4F.Identity;

            // Initialize the view matrix.
            Vector3F eye = new Vector3F(0.0f, 2.0f, -6.0f);
            Vector3F at = new Vector3F(0.0f, 0.0f, 0.0f);
            Vector3F up = new Vector3F(0.0f, 1.0f, 0.0f);
            viewMatrix = Camera.MatrixLookAtLH(eye, at, up);
            viewVariable.Matrix = viewMatrix;

            // Initialize the projection matrix
            projectionMatrix = Camera.MatrixPerspectiveFovLH(
                (float)Math.PI * 0.24f, // fovy
                nWidth / (float)nHeight, // aspect
                0.1f, // zn
                100.0f // zf
                );
            projectionVariable.Matrix = projectionMatrix;
        }
        #endregion

        #region InitializeTextureRenderTarget()
        private void InitializeTextureRenderTarget()
        {
            Texture2DDescription offscreenTextureDesc = new Texture2DDescription
                                                            {
                                                                ArraySize = 1,
                                                                BindFlags = BindFlag.RenderTarget | BindFlag.ShaderResource,
                                                                CpuAccessFlags = 0,
                                                                Format = Format.R8G8B8A8_UNORM,
                                                                Height = 512,
                                                                Width = 512,
                                                                MipLevels = 1,
                                                                MiscFlags = 0,
                                                                SampleDescription = new SampleDescription
                                                                                        {
                                                                                            Count = 1,
                                                                                            Quality = 0
                                                                                        },
                                                                Usage = Usage.Default,
                                                            };
            offscreenTexture = device.CreateTexture2D(offscreenTextureDesc);
            // Convert the Direct2D texture into a Shader Resource View
            textureResourceView = device.CreateShaderResourceView(offscreenTexture);
            textureSurface = offscreenTexture.GetDXGISurface();
        }
        #endregion

        #region InitializeGeometryBuffers()
        private void InitializeGeometryBuffers()
        {
            PassDescription PassDesc = technique.GetPassByIndex(0).Description;

            vertexLayout = device.CreateInputLayout(
                inputLayouts,
                PassDesc.InputAssemblerInputSignature,
                PassDesc.InputAssemblerInputSignatureSize
                );

            // Set the input layout
            device.IA.SetInputLayout(vertexLayout);


            BufferDescription bd = new BufferDescription();
            bd.Usage = Usage.Default;
            bd.ByteWidth = (uint)Marshal.SizeOf(vertexArray.s_VertexArray);
            bd.BindFlags = BindFlag.VertexBuffer;
            bd.CpuAccessFlags = 0;
            bd.MiscFlags = 0;

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(vertexArray.s_VertexArray));
            Marshal.StructureToPtr(vertexArray.s_VertexArray, ptr, true);
            SubresourceData initData = new SubresourceData { SysMem = ptr };
            vertexBuffer = device.CreateBuffer(bd, initData);
            Marshal.FreeHGlobal(ptr);

            // Set vertex buffer
            uint stride = (uint)Marshal.SizeOf(typeof(SimpleVertex));
            uint offset = 0;

            device.IA.SetVertexBuffers(
                0, // StartSlot
                new[] { vertexBuffer },
                new[] { stride },
                new[] { offset });

            bd.Usage = Usage.Default;
            bd.ByteWidth = (uint)Marshal.SizeOf(vertexArray.s_FacesIndexArray);
            bd.BindFlags = BindFlag.IndexBuffer;
            bd.CpuAccessFlags = 0;
            bd.MiscFlags = 0;

            ptr = Marshal.AllocHGlobal(Marshal.SizeOf(vertexArray.s_FacesIndexArray));
            Marshal.StructureToPtr(vertexArray.s_FacesIndexArray, ptr, true);

            initData.SysMem = ptr;
            facesIndexBuffer = device.CreateBuffer(bd, initData);

            // Set primitive topology
            device.IA.SetPrimitiveTopology(
                PrimitiveTopology.TriangleList
                );
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

        #region MakeBothSidesRendered()
        private void MakeBothSidesRendered()
        {
            RasterizerDescription rsDesc = new RasterizerDescription();
            rsDesc.AntialiasedLineEnable = false;
            rsDesc.CullMode = CullMode.None;
            rsDesc.DepthBias = 0;
            rsDesc.DepthBiasClamp = 0;
            rsDesc.DepthClipEnable = true;
            rsDesc.FillMode = Microsoft.WindowsAPICodePack.DirectX.Direct3D10.FillMode.Solid;
            rsDesc.FrontCounterClockwise = false; // Must be FALSE for 10on9
            rsDesc.MultisampleEnable = false;
            rsDesc.ScissorEnable = false;
            rsDesc.SlopeScaledDepthBias = 0;
            rasterizerState = device.CreateRasterizerState(rsDesc);

            device.RS.SetState(rasterizerState);
        }
        #endregion

        #region LoadResourceShader()
        static Effect LoadResourceShader(D3DDevice device, string resourceName)
        {
            using (Stream stream = Application.ResourceAssembly.GetManifestResourceStream(resourceName))
            {
                return device.CreateEffectFromCompiledBinary(stream);
            }
        }
        #endregion

        #region CreateD2DRenderTargets()
        private void CreateD2DRenderTargets()
        {
            // Create a D2D render target which can draw into our offscreen D3D surface
            textureRenderTarget = d2DFactory.CreateDxgiSurfaceRenderTarget(
                textureSurface,
                renderTargetProperties
                );

            // Create a linear gradient brush for the 2D geometry
            GradientStopCollection gradientStops = textureRenderTarget.CreateGradientStopCollection(stopsGeometry, Gamma.Gamma_22, ExtendMode.Mirror);
            linearGradientBrush = textureRenderTarget.CreateLinearGradientBrush(
                new LinearGradientBrushProperties(
                    new Point2F(100, 0),
                    new Point2F(100, 200)),
                gradientStops
                );

            // create a black brush
            blackBrush = textureRenderTarget.CreateSolidColorBrush(new ColorF(Colors.Black));

            using (Stream stream = Application.ResourceAssembly.GetManifestResourceStream("Microsoft.WindowsAPICodePack.DirectX.Samples.tulip.jpg"))
            {
                d2dBitmap = BitmapUtilities.LoadBitmapFromStream(
                    textureRenderTarget,
                    imagingFactory,
                    stream);
            }

            gridPatternBitmapBrush = CreateGridPatternBrush(textureRenderTarget);
            gridPatternBitmapBrush.Opacity = 0.5f;

            CreateBackBufferD2DRenderTarget();
        }
        #endregion

        #region CreateBackBufferD2DRenderTarget()
        private void CreateBackBufferD2DRenderTarget()
        {
            // Get a surface in the swap chain
            using (Surface backBufferSurface = swapChain.GetBuffer<Surface>(0))
            {
                backBufferRenderTarget = d2DFactory.CreateDxgiSurfaceRenderTarget(
                    backBufferSurface,
                    renderTargetProperties
                    );

                GradientStopCollection stops = backBufferRenderTarget.CreateGradientStopCollection(stopsBackground,
                                                                                                   Gamma.Gamma_22,
                                                                                                   ExtendMode.Mirror);
                backBufferGradientBrush = backBufferRenderTarget.CreateLinearGradientBrush(
                    new LinearGradientBrushProperties(
                        new Point2F(0.0f, 0.0f),
                        new Point2F(0.0f, 1.0f)),
                    stops);

                // Create a red brush for text drawn into the back buffer
                backBufferTextBrush = backBufferRenderTarget.CreateSolidColorBrush(new ColorF(Colors.WhiteSmoke));
            }
        }
        #endregion

        #region CreateDeviceIndependentResources()
        void CreateDeviceIndependentResources()
        {
            string msc_fontName = "Verdana";
            float msc_fontSize = 50;

            string fps_fontName = "Courier New";
            float fps_fontSize = 12;

            GeometrySink spSink;

            // Create D2D factory
            d2DFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded);

            // Create WIC factory
            imagingFactory = new ImagingFactory();

            // Create DWrite factory
            dWriteFactory = DWriteFactory.CreateFactory();

            // Create DWrite text format object
            textFormat = dWriteFactory.CreateTextFormat(
                msc_fontName,
                msc_fontSize);

            textFormat.TextAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.TextAlignment.Center;
            textFormat.ParagraphAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.ParagraphAlignment.Center;


            // Create DWrite text format object
            textFormatFps = dWriteFactory.CreateTextFormat(
                fps_fontName,
                fps_fontSize);

            textFormatFps.TextAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.TextAlignment.Leading;
            textFormatFps.ParagraphAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.ParagraphAlignment.Near;

            // Create the path geometry.
            pathGeometry = d2DFactory.CreatePathGeometry();

            // Write to the path geometry using the geometry sink. We are going to create an
            // hour glass.
            spSink = pathGeometry.Open();

            spSink.SetFillMode(Microsoft.WindowsAPICodePack.DirectX.Direct2D1.FillMode.Alternate);

            spSink.BeginFigure(
                new Point2F(0, 0),
                FigureBegin.Filled
                );

            spSink.AddLine(new Point2F(200, 0));

            spSink.AddBezier(
                new BezierSegment(
                new Point2F(150, 50),
                new Point2F(150, 150),
                new Point2F(200, 200)
                ));

            spSink.AddLine(
                new Point2F(0,
                200)
                );

            spSink.AddBezier(
                new BezierSegment(
                new Point2F(50, 150),
                new Point2F(50, 50),
                new Point2F(0, 0)
                ));

            spSink.EndFigure(
                FigureEnd.Closed
                );

            spSink.Close(
                );
        }
        #endregion

        #region CreateGridPatternBrush()
        BitmapBrush CreateGridPatternBrush(RenderTarget pRenderTarget)
        {
            // Create a compatible render target.
            BitmapRenderTarget spCompatibleRenderTarget =
                pRenderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None, (new SizeF(10.0f, 10.0f)));

            // Draw a pattern.
            SolidColorBrush spGridBrush =
                spCompatibleRenderTarget.CreateSolidColorBrush(
                    new ColorF(0.93f, 0.94f, 0.96f, 1.0f));

            spCompatibleRenderTarget.BeginDraw();

            spCompatibleRenderTarget.FillRectangle(new RectF(0.0f, 0.0f, 10.0f, 1.0f), spGridBrush);
            spCompatibleRenderTarget.FillRectangle(new RectF(0.0f, 0.1f, 1.0f, 10.0f), spGridBrush);
            spCompatibleRenderTarget.EndDraw();

            // Retrieve the bitmap from the render target.
            D2DBitmap spGridBitmap = spCompatibleRenderTarget.GetBitmap();

            // Choose the tiling mode for the bitmap brush.
            BitmapBrushProperties brushProperties =
                new BitmapBrushProperties(ExtendMode.Wrap, ExtendMode.Wrap, BitmapInterpolationMode.Linear);

            // Create the bitmap brush.
            return textureRenderTarget.CreateBitmapBrush(spGridBitmap, brushProperties);
        }
        #endregion
    }
}

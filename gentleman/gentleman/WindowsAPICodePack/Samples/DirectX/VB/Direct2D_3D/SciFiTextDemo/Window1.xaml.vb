' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows

Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DirectWrite
Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports Microsoft.WindowsAPICodePack.DirectX.DXGI


Namespace SciFiTextDemo
	''' <summary>
	''' Interaction logic for Window1.xaml
	''' </summary>
	Partial Public Class Window1
		Private syncObject As New Object()

		Private text As String = "Episode CCCXLVII:" & Constants.vbLf & "A Misguided Hope" & Constants.vbLf + Constants.vbLf & "Not so long ago, in a cubicle not so far away..." & Constants.vbLf + Constants.vbLf & "It is days before milestone lockdown. A small group of rebel developers toil through the weekend, relentlessly fixing bugs in defiance of familial obligations. Aside from pride in their work, their only reward will be takeout food and cinema gift certificates." & Constants.vbLf + Constants.vbLf & "Powered by coffee and soda, our hyper-caffeinated heroine stares at her screen with glazed-over eyes. She repeatedly slaps her face in a feeble attempt to stay awake. Lapsing into micro-naps, she reluctantly takes a break from debugging to replenish her caffeine levels." & Constants.vbLf + Constants.vbLf & "On her way to the kitchen she spots a fallen comrade, passed out on his keyboard and snoring loudly. After downing two coffees, she fills a pitcher with ice water and..."

		' The factories
		Private d2DFactory As D2DFactory
		Private dWriteFactory As DWriteFactory

		Private pause As Boolean
		Private lastSavedDelta As Integer

		'Device-Dependent Resources
		Private device As D3DDevice1
		Private swapChain As SwapChain
		Private rasterizerState As RasterizerState
		Private depthStencil As Texture2D
		Private depthStencilView As DepthStencilView
		Private renderTargetView As RenderTargetView
		Private offscreenTexture As Texture2D
		Private shader As Effect
		Private vertexBuffer As D3DBuffer
		Private vertexLayout As InputLayout
		Private facesIndexBuffer As D3DBuffer
		Private textureResourceView As ShaderResourceView

		Private renderTarget As RenderTarget
		Private textBrush As LinearGradientBrush

		Private opacityRenderTarget As BitmapRenderTarget
		Private isOpacityRTPopulated As Boolean

		Private technique As EffectTechnique
		Private worldMatrixVariable As EffectMatrixVariable
		Private viewMatrixVariable As EffectMatrixVariable
		Private projectionMarixVariable As EffectMatrixVariable
		Private diffuseVariable As EffectShaderResourceVariable

		' Device-Independent Resources
		Private textFormat As TextFormat

		Private worldMatrix As Matrix4x4F
		Private viewMatrix As Matrix4x4F
		Private projectionMatrix As Matrix4x4F

		Private backColor As New ColorRgba(Colors.Black)

		Private currentTimeVariation As Single
		Private startTime As Integer = Environment.TickCount

		Private inputLayoutDescriptions() As InputElementDescription = { New InputElementDescription With {.SemanticName = "POSITION", .SemanticIndex = 0, .Format = Format.R32G32B32_FLOAT, .InputSlot = 0, .AlignedByteOffset = 0, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}, New InputElementDescription With {.SemanticName = "TEXCOORD", .SemanticIndex = 0, .Format = Format.R32G32_FLOAT, .InputSlot = 0, .AlignedByteOffset = 12, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0} }

		Private VertexArray As New VertexData()

		Public Sub New()
			InitializeComponent()
			textBox.Text = text
			AddHandler host.Loaded, AddressOf host_Loaded
			AddHandler host.SizeChanged, AddressOf host_SizeChanged
		End Sub

		Private Sub host_SizeChanged(ByVal sender As Object, ByVal e As SizeChangedEventArgs)
			SyncLock syncObject
				If device Is Nothing Then
					Return
				End If
				Dim nWidth As UInteger = CUInt(host.ActualWidth)
				Dim nHeight As UInteger = CUInt(host.ActualHeight)

				device.OM.SetRenderTargets(New RenderTargetView() { Nothing }, Nothing)
				'need to remove the reference to the swapchain's backbuffer to enable ResizeBuffers() call
				renderTargetView.Dispose()
				depthStencilView.Dispose()
				depthStencil.Dispose()

				device.RS.SetViewports(Nothing)

				Dim sd As SwapChainDescription = swapChain.Description
				'Change the swap chain's back buffer size, format, and number of buffers
				swapChain.ResizeBuffers(sd.BufferCount, nWidth, nHeight, sd.BufferDescription.Format, sd.Flags)

				Using pBuffer As Texture2D = swapChain.GetBuffer(Of Texture2D)(0)
					renderTargetView = device.CreateRenderTargetView(pBuffer)
				End Using

				InitializeDepthStencil(nWidth, nHeight)

				' bind the views to the device
				device.OM.SetRenderTargets(New RenderTargetView() { renderTargetView }, depthStencilView)

				SetViewport(nWidth, nHeight)

				' update the aspect ratio
				projectionMatrix = Camera.MatrixPerspectiveFovLH(CSng(Math.PI) * 0.1f, nWidth / CSng(nHeight), 0.1f, 100.0f)
                projectionMarixVariable.Matrix = projectionMatrix
			End SyncLock
		End Sub

		Private Sub host_Loaded(ByVal sender As Object, ByVal e As RoutedEventArgs)
			CreateDeviceIndependentResources()
			startTime = Environment.TickCount
			host.Render = AddressOf RenderScene
		End Sub

		Private Shared Function LoadResourceShader(ByVal device As D3DDevice, ByVal resourceName As String) As Effect
			Using stream As Stream = Application.ResourceAssembly.GetManifestResourceStream(resourceName)
				Return device.CreateEffectFromCompiledBinary(stream)
			End Using
		End Function

		Private Sub CreateDeviceIndependentResources()
			' Create a Direct2D factory.
			d2DFactory = D2DFactory.CreateFactory(D2DFactoryType.SingleThreaded)

			' Create a DirectWrite factory.
			dWriteFactory = DWriteFactory.CreateFactory()

			' Create a DirectWrite text format object.
            textFormat = dWriteFactory.CreateTextFormat("Calibri", 50, Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontWeight.Bold, Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontStyle.Normal, Microsoft.WindowsAPICodePack.DirectX.DirectWrite.FontStretch.Normal)

			' Center the text both horizontally and vertically.
            textFormat.TextAlignment = Microsoft.WindowsAPICodePack.DirectX.DirectWrite.TextAlignment.Leading
			textFormat.ParagraphAlignment = ParagraphAlignment.Near
		End Sub

		Private Sub CreateDeviceResources()
			Dim width As UInteger = CUInt(host.ActualWidth)
			Dim height As UInteger = CUInt(host.ActualHeight)

			' If we don't have a device, need to create one now and all
			' accompanying D3D resources.
			CreateDevice()

			Dim dxgiFactory As DXGIFactory = DXGIFactory.CreateFactory()

			Dim swapDesc As New SwapChainDescription()
			swapDesc.BufferDescription.Width = width
			swapDesc.BufferDescription.Height = height
			swapDesc.BufferDescription.Format = Format.R8G8B8A8_UNORM
			swapDesc.BufferDescription.RefreshRate.Numerator = 60
			swapDesc.BufferDescription.RefreshRate.Denominator = 1
			swapDesc.SampleDescription.Count = 1
			swapDesc.SampleDescription.Quality = 0
			swapDesc.BufferUsage = UsageOption.RenderTargetOutput
			swapDesc.BufferCount = 1
			swapDesc.OutputWindowHandle = host.Handle
			swapDesc.Windowed = True

			swapChain = dxgiFactory.CreateSwapChain(device, swapDesc)

			' Create rasterizer state object
			Dim rsDesc As New RasterizerDescription()
			rsDesc.AntialiasedLineEnable = False
			rsDesc.CullMode = CullMode.None
			rsDesc.DepthBias = 0
			rsDesc.DepthBiasClamp = 0
			rsDesc.DepthClipEnable = True
            rsDesc.FillMode = Microsoft.WindowsAPICodePack.DirectX.Direct3D10.FillMode.Solid
			rsDesc.FrontCounterClockwise = False ' Must be FALSE for 10on9
			rsDesc.MultisampleEnable = False
			rsDesc.ScissorEnable = False
			rsDesc.SlopeScaledDepthBias = 0

			rasterizerState = device.CreateRasterizerState(rsDesc)

			device.RS.SetState(rasterizerState)

			' If we don't have a D2D render target, need to create all of the resources
			' required to render to one here.
			' Ensure that nobody is holding onto one of the old resources
			device.OM.SetRenderTargets(New RenderTargetView() {Nothing})

			InitializeDepthStencil(width, height)

			' Create views on the RT buffers and set them on the device
			Dim renderDesc As New RenderTargetViewDescription()
			renderDesc.Format = Format.R8G8B8A8_UNORM
			renderDesc.ViewDimension = RenderTargetViewDimension.Texture2D
			renderDesc.Texture2D.MipSlice = 0

			Using spBackBufferResource As D3DResource = swapChain.GetBuffer(Of D3DResource)(0)
				renderTargetView = device.CreateRenderTargetView(spBackBufferResource, renderDesc)
			End Using

			device.OM.SetRenderTargets(New RenderTargetView() {renderTargetView}, depthStencilView)

			SetViewport(width, height)


			' Create a D2D render target which can draw into the surface in the swap chain
			Dim props As New RenderTargetProperties(RenderTargetType.Default, New PixelFormat(Format.Unknown, AlphaMode.Premultiplied), 96, 96, RenderTargetUsage.None, FeatureLevel.Default)

			' Allocate a offscreen D3D surface for D2D to render our 2D content into
			Dim tex2DDescription As Texture2DDescription
			tex2DDescription.ArraySize = 1
			tex2DDescription.BindFlags = BindFlag.RenderTarget Or BindFlag.ShaderResource
			tex2DDescription.CpuAccessFlags = CpuAccessFlag.Unspecified
			tex2DDescription.Format = Format.R8G8B8A8_UNORM
			tex2DDescription.Height = 4096
			tex2DDescription.Width = 512
			tex2DDescription.MipLevels = 1
			tex2DDescription.MiscFlags = 0
			tex2DDescription.SampleDescription.Count = 1
			tex2DDescription.SampleDescription.Quality = 0
			tex2DDescription.Usage = Usage.Default

			offscreenTexture = device.CreateTexture2D(tex2DDescription)

			Using dxgiSurface As Surface = offscreenTexture.GetDXGISurface()
				' Create a D2D render target which can draw into our offscreen D3D surface
				renderTarget = d2DFactory.CreateDxgiSurfaceRenderTarget(dxgiSurface, props)
			End Using

			Dim alphaOnlyFormat As New PixelFormat(Format.A8_UNORM, AlphaMode.Premultiplied)

			opacityRenderTarget = renderTarget.CreateCompatibleRenderTarget(CompatibleRenderTargetOptions.None, alphaOnlyFormat)

			' Load pixel shader
			' Open precompiled vertex shader
			' This file was compiled using DirectX's SDK Shader compilation tool: 
			' fxc.exe /T fx_4_0 /Fo SciFiText.fxo SciFiText.fx
            shader = LoadResourceShader(device, "SciFiText.fxo")

			' Obtain the technique
			technique = shader.GetTechniqueByName("Render")

			' Obtain the variables
			worldMatrixVariable = shader.GetVariableByName("World").AsMatrix()
			viewMatrixVariable = shader.GetVariableByName("View").AsMatrix()
			projectionMarixVariable = shader.GetVariableByName("Projection").AsMatrix()
			diffuseVariable = shader.GetVariableByName("txDiffuse").AsShaderResource()

			' Create the input layout
			Dim passDesc As New PassDescription()
			passDesc = technique.GetPassByIndex(0).Description

			vertexLayout = device.CreateInputLayout(inputLayoutDescriptions, passDesc.InputAssemblerInputSignature, passDesc.InputAssemblerInputSignatureSize)

			' Set the input layout
			device.IA.SetInputLayout(vertexLayout)

			Dim verticesDataPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(VertexArray.VerticesInstance))
			Marshal.StructureToPtr(VertexArray.VerticesInstance, verticesDataPtr, True)

			Dim bd As New BufferDescription()
			bd.Usage = Usage.Default
			bd.ByteWidth = CUInt(Marshal.SizeOf(VertexArray.VerticesInstance))
			bd.BindFlags = BindFlag.VertexBuffer
			bd.CpuAccessFlags = CpuAccessFlag.Unspecified
			bd.MiscFlags = ResourceMiscFlag.Undefined

			Dim InitData As New SubresourceData() With {.SysMem = verticesDataPtr}


			vertexBuffer = device.CreateBuffer(bd, InitData)

			Marshal.FreeHGlobal(verticesDataPtr)

			' Set vertex buffer
			Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(SimpleVertex)))
			Dim offset As UInteger = 0

			device.IA.SetVertexBuffers(0, New D3DBuffer() {vertexBuffer}, New UInteger() {stride}, New UInteger() {offset})

			Dim indicesDataPtr As IntPtr = Marshal.AllocHGlobal(Marshal.SizeOf(VertexArray.IndicesInstance))
			Marshal.StructureToPtr(VertexArray.IndicesInstance, indicesDataPtr, True)

			bd.Usage = Usage.Default
			bd.ByteWidth = CUInt(Marshal.SizeOf(VertexArray.IndicesInstance))
			bd.BindFlags = BindFlag.IndexBuffer
			bd.CpuAccessFlags = 0
			bd.MiscFlags = 0

			InitData.SysMem = indicesDataPtr

			facesIndexBuffer = device.CreateBuffer(bd, InitData)

			Marshal.FreeHGlobal(indicesDataPtr)

			' Set primitive topology
			device.IA.SetPrimitiveTopology(PrimitiveTopology.TriangleList)

			' Convert the D2D texture into a Shader Resource View
			textureResourceView = device.CreateShaderResourceView(offscreenTexture)

			' Initialize the world matrices
            worldMatrix = Matrix4x4F.Identity

			' Initialize the view matrix
			Dim Eye As New Vector3F(0.0f, 0.0f, 13.0f)
			Dim At As New Vector3F(0.0f, -3.5f, 45.0f)
			Dim Up As New Vector3F(0.0f, 1.0f, 0.0f)

			viewMatrix = Camera.MatrixLookAtLH(Eye, At, Up)

			' Initialize the projection matrix
			projectionMatrix = Camera.MatrixPerspectiveFovLH(CSng(Math.PI)*0.1f, width/CSng(height), 0.1f, 100.0f)

			' Update Variables that never change
            viewMatrixVariable.Matrix = viewMatrix

            projectionMarixVariable.Matrix = projectionMatrix

			Dim gradientStops() As GradientStop = { New GradientStop(0.0f, New ColorF(Colors.Yellow)), New GradientStop(1.0f, New ColorF(Colors.Black)) }

			Dim spGradientStopCollection As GradientStopCollection = renderTarget.CreateGradientStopCollection(gradientStops, Gamma.Gamma_22, ExtendMode.Clamp)

			' Create a linear gradient brush for text
			textBrush = renderTarget.CreateLinearGradientBrush(New LinearGradientBrushProperties(New Point2F(0, 0), New Point2F(0, -2048)), spGradientStopCollection)
		End Sub

		Private Sub CreateDevice()
			Try
				' Create device
				device = D3DDevice1.CreateDevice1(Nothing, DriverType.Hardware, Nothing, CreateDeviceFlag.SupportBGRA, FeatureLevel.FeatureLevel_10_0)
			Catch e1 As Exception
				' if we can't create a hardware device,
				' try the warp one
			End Try
			If device Is Nothing Then
				device = D3DDevice1.CreateDevice1(Nothing, DriverType.Software, "d3d10warp.dll", CreateDeviceFlag.SupportBGRA, FeatureLevel.FeatureLevel_10_0)
			End If
		End Sub

		Private Sub RenderScene()
			SyncLock syncObject
				If device Is Nothing Then
					CreateDeviceResources()
				End If

				If Not pause Then
					If lastSavedDelta <> 0 Then
						startTime = Environment.TickCount - lastSavedDelta
						lastSavedDelta = 0
					End If
					currentTimeVariation = (Environment.TickCount - startTime)/6000.0f
					worldMatrix = MatrixMath.MatrixTranslate(0, 0, currentTimeVariation)
					textBrush.Transform = Matrix3x2F.Translation(0, (4096f/16f)*currentTimeVariation)
				End If

				device.ClearDepthStencilView(depthStencilView, ClearFlag.Depth, 1, 0)

				' Clear the back buffer
				device.ClearRenderTargetView(renderTargetView, backColor)

				diffuseVariable.SetResource(Nothing)

				technique.GetPassByIndex(0).Apply()

				' Draw the D2D content into our D3D surface
				RenderD2DContentIntoSurface()

				diffuseVariable.SetResource(textureResourceView)

				' Update variables
                worldMatrixVariable.Matrix = worldMatrix

				' Set index buffer
				device.IA.SetIndexBuffer(facesIndexBuffer, Format.R16_UINT, 0)

				' Draw the scene
				technique.GetPassByIndex(0).Apply()

				device.DrawIndexed(CUInt(Marshal.SizeOf(VertexArray.VerticesInstance)), 0, 0)

				swapChain.Present(0, PresentFlag.Default)
			End SyncLock
		End Sub

		Private Sub RenderD2DContentIntoSurface()
			Dim rtSize As SizeF = renderTarget.Size

			renderTarget.BeginDraw()

			If Not isOpacityRTPopulated Then
				opacityRenderTarget.BeginDraw()

				opacityRenderTarget.Transform = Matrix3x2F.Identity

				opacityRenderTarget.Clear(New ColorF(Colors.Black, 0))

				opacityRenderTarget.DrawText(text, textFormat, New RectF(0, 0, rtSize.Width, rtSize.Height), textBrush)

				opacityRenderTarget.EndDraw()

				isOpacityRTPopulated = True
			End If

			renderTarget.Clear(New ColorF(Colors.Black))

			renderTarget.AntialiasMode = AntialiasMode.Aliased

			Dim spBitmap As D2DBitmap = opacityRenderTarget.GetBitmap()

			renderTarget.FillOpacityMask(spBitmap, textBrush, OpacityMaskContent.TextNatural, New RectF(0, 0, rtSize.Width, rtSize.Height), New RectF(0, 0, rtSize.Width, rtSize.Height))

			renderTarget.EndDraw()
		End Sub

		#Region "InitializeDepthStencil()"
		Private Sub InitializeDepthStencil(ByVal nWidth As UInteger, ByVal nHeight As UInteger)
			' Create depth stencil texture
			Dim descDepth As New Texture2DDescription() With {.Width = nWidth, .Height = nHeight, .MipLevels = 1, .ArraySize = 1, .Format = Format.D16_UNORM, .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, .BindFlags = BindFlag.DepthStencil}
			depthStencil = device.CreateTexture2D(descDepth)

			' Create the depth stencil view
			Dim depthStencilViewDesc As New DepthStencilViewDescription() With {.Format = descDepth.Format, .ViewDimension = DepthStencilViewDimension. Texture2D}
			depthStencilView = device.CreateDepthStencilView(depthStencil, depthStencilViewDesc)
		End Sub
		#End Region

		#Region "SetViewport()"
		Private Sub SetViewport(ByVal nWidth As UInteger, ByVal nHeight As UInteger)
			Dim viewport As Viewport = New Viewport With {.Width = nWidth, .Height = nHeight, .TopLeftX = 0, .TopLeftY = 0, .MinDepth = 0, .MaxDepth = 1}
			device.RS.SetViewports(New Viewport() { viewport })
		End Sub
		#End Region

		Private Sub Button_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
			pause = Not pause

			If pause Then
				lastSavedDelta = Environment.TickCount - startTime
				actionText.Text = "Resume Text"
			Else
				actionText.Text = "Pause Text"
			End If
		End Sub

		Private Sub textBox_TextChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.TextChangedEventArgs)
			text = textBox.Text
			isOpacityRTPopulated = False
		End Sub
	End Class
End Namespace

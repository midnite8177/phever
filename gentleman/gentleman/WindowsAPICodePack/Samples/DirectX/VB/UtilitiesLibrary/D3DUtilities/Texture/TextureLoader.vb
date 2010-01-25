' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.IO
Imports System.Runtime.InteropServices


Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DXGI
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	Public Class TextureLoader
		''' <summary>
		''' Creates a ShaderResourceView from a bitmap in a Stream. 
		''' </summary>
		''' <param name="device">The Direct3D device that will own the ShaderResourceView</param>
		''' <param name="stream">Any Windows Imaging Component decodable image</param>
		''' <returns></returns>
		Public Shared Function LoadTexture(ByVal device As D3DDevice, ByVal stream As Stream) As ShaderResourceView
			Dim factory As New ImagingFactory()

			Dim bitmapDecoder As BitmapDecoder = factory.CreateDecoderFromStream(stream, DecodeMetadataCacheOptions.OnDemand)

			If bitmapDecoder.FrameCount = 0 Then
				Throw New ArgumentException("Image file successfully loaded, but it has no image frames.")
			End If

			Dim bitmapFrameDecode As BitmapFrameDecode = bitmapDecoder.GetFrame(0)
			Dim bitmapSource As BitmapSource = bitmapFrameDecode.ToBitmapSource()

			' create texture description
			Dim textureDescription As New Texture2DDescription() With {.Width = bitmapSource.Size.Width, .Height = bitmapSource.Size.Height, .MipLevels = 1, .ArraySize = 1, .Format = Format.R8G8B8A8_UNORM, .SampleDescription = New SampleDescription() With {.Count = 1, .Quality = 0}, .Usage = Usage.Dynamic, .BindFlags = BindFlag.ShaderResource, .CpuAccessFlags = CpuAccessFlag.Write, .MiscFlags = 0}

			' create texture
			Dim texture As Texture2D = device.CreateTexture2D(textureDescription)

			' Create a format converter
			Dim converter As WICFormatConverter = factory.CreateFormatConverter()
			converter.Initialize(bitmapSource, PixelFormats.Pf32bppRGBA, BitmapDitherType.None, BitmapPaletteType.Custom)

			' get bitmap data
			Dim buffer() As Byte = converter.CopyPixels()

			' Copy bitmap data to texture
			Dim texmap As MappedTexture2D = texture.Map(0, Map.WriteDiscard, MapFlag.Unspecified)
			Marshal.Copy(buffer, 0, texmap.Data, buffer.Length)
			texture.Unmap(0)

			' create shader resource view description
			Dim srvDescription As New ShaderResourceViewDescription() With {.Format = textureDescription.Format, .ViewDimension = ShaderResourceViewDimension.Texture2D, .Texture2D = New Texture2DShaderResourceView() With {.MipLevels = textureDescription.MipLevels, .MostDetailedMip = 0}}

			' create shader resource view from texture
			Return device.CreateShaderResourceView(texture, srvDescription)
		End Function
	End Class
End Namespace

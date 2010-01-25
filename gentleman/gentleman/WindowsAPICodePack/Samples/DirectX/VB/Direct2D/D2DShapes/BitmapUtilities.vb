' Copyright (c) Microsoft Corporation.  All rights reserved.

Imports Microsoft.VisualBasic
Imports Microsoft.WindowsAPICodePack.DirectX.WindowsImagingComponent
Imports Microsoft.WindowsAPICodePack.DirectX.Direct2D1
Imports System.IO

Namespace D2DShapes
	Friend Class BitmapUtilities
		Friend Shared Function LoadBitmapFromFile(ByVal renderTarget As RenderTarget, ByVal wicFactory As ImagingFactory, ByVal fileName As String) As D2DBitmap
			Dim decoder As BitmapDecoder = wicFactory.CreateDecoderFromFilename(fileName, DesiredAccess.Read, DecodeMetadataCacheOptions.OnLoad)
			Dim ret As D2DBitmap = CreateBitmapFromDecoder(renderTarget, wicFactory, decoder)
			decoder.Dispose()
			Return ret
		End Function

		Friend Shared Function LoadBitmapFromStream(ByVal renderTarget As RenderTarget, ByVal wicFactory As ImagingFactory, ByVal ioStream As Stream) As D2DBitmap
			Dim decoder As BitmapDecoder = wicFactory.CreateDecoderFromStream(ioStream, DecodeMetadataCacheOptions.OnLoad)
			Dim ret As D2DBitmap = CreateBitmapFromDecoder(renderTarget, wicFactory, decoder)
			decoder.Dispose()
			Return ret
		End Function

		Private Shared Function CreateBitmapFromDecoder(ByVal renderTarget As RenderTarget, ByVal wicFactory As ImagingFactory, ByVal decoder As BitmapDecoder) As D2DBitmap
			' Create the initial frame.
			Dim source As BitmapFrameDecode = decoder.GetFrame(0)
			' Convert the image format to 32bppPBGRA -- which Direct2D expects.
			Dim converter As WICFormatConverter = wicFactory.CreateFormatConverter()
			converter.Initialize(source.ToBitmapSource(), PixelFormats.Pf32bppPBGRA, BitmapDitherType.None, BitmapPaletteType.MedianCut)

			' Create a Direct2D bitmap from the WIC bitmap.
			Dim ret As D2DBitmap = renderTarget.CreateBitmapFromWicBitmap(converter.ToBitmapSource())

			converter.Dispose()
			source.Dispose()

			Return ret
		End Function
	End Class
End Namespace

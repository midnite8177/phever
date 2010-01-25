' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DXGI
Imports System.Windows.Media.Media3D
Imports System.Runtime.InteropServices

Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	''' <summary>
	''' 
	''' </summary>
	Public Class XMesh
		Implements IDisposable
		#Region "public methods"
		''' <summary>
		''' Renders the mesh with the specified transformation
		''' </summary>
		''' <param name="modelTransform"></param>
		Public Sub Render(ByVal modelTransform As Matrix4x4F)
			' setup rasterization
			Dim rDescription As New RasterizerDescription() With {.FillMode = If(wireFrame, FillMode.Wireframe, FillMode.Solid), .CullMode = CullMode.Back, .FrontCounterClockwise = False, .DepthBias = 0, .DepthBiasClamp = 0, .SlopeScaledDepthBias = 0, .DepthClipEnable = True, .ScissorEnable = False, .MultisampleEnable = True, .AntialiasedLineEnable = True}
			Dim rState As RasterizerState = Me.manager.device.CreateRasterizerState(rDescription)

			Me.manager.device.RS.SetState(rState)

            Me.manager.brightnessVariable.FloatValue = Me.lightIntensity_Renamed

			' start rendering
			For Each part As Part In scene.parts
				Dim technique As EffectTechnique = Nothing
				If part.material Is Nothing Then
					technique = Me.manager.techniqueRenderVertexColor
				Else
					If part.material.textureResource IsNot Nothing Then
						technique = Me.manager.techniqueRenderTexture
						Me.manager.diffuseVariable.SetResource(part.material.textureResource)
					Else
						technique = Me.manager.techniqueRenderMaterialColor
                        Me.manager.materialColorVariable.FloatVector = part.material.materialColor
					End If
				End If

				' set part transform
				Dim partGroup As New Transform3DGroup()
				partGroup.Children.Add(New MatrixTransform3D(PartAnimation(part.name)))
'                    new MatrixTransform3D( (Matrix3D)part.partTransform ) );
				partGroup.Children.Add(New MatrixTransform3D(part.partTransform.ToMatrix3D()))
				partGroup.Children.Add(New MatrixTransform3D(scene.sceneTransform.ToMatrix3D()))
				partGroup.Children.Add(New MatrixTransform3D(modelTransform.ToMatrix3D()))

                Me.manager.worldVariable.Matrix = partGroup.Value.ToMatrix4x4F()

				If part.vertexBuffer IsNot Nothing Then
					'  set up vertex buffer and index buffer
					Dim stride As UInteger = CUInt(Marshal.SizeOf(GetType(XMeshVertex)))
					Dim offset As UInteger = 0
					Me.manager.device.IA.SetVertexBuffers(0, New D3DBuffer() { part.vertexBuffer }, New UInteger() { stride }, New UInteger() { offset })

					' Set primitive topology
					Me.manager.device.IA.SetPrimitiveTopology(PrimitiveTopology.TriangleList)

					Dim techDesc As TechniqueDescription = technique.Description
                    For p As UInteger = 0 To techDesc.Passes - 1UI
                        technique.GetPassByIndex(p).Apply()
                        Dim passDescription As PassDescription = technique.GetPassByIndex(p).Description

                        ' set vertex layout
                        Me.manager.device.IA.SetInputLayout(Me.manager.device.CreateInputLayout(part.dataDescription, passDescription.InputAssemblerInputSignature, passDescription.InputAssemblerInputSignatureSize))

                        ' draw part
                        Me.manager.device.Draw(CUInt(part.vertexCount), 0)
                    Next p
				End If
			Next part
		End Sub
		#End Region

		#Region "public properties"
		''' <summary>
		''' Displays the unshaded wireframe if true
		''' </summary>
		Public Property ShowWireFrame() As Boolean
			Get
				Return wireFrame
			End Get
			Set(ByVal value As Boolean)
				wireFrame = value
			End Set
		End Property
		Private wireFrame As Boolean = False

		''' <summary>
		''' Sets the intensity of the light used in rendering.
		''' </summary>
		Public Property LightIntensity() As Single
			Get
				Return lightIntensity_Renamed
			End Get
			Set(ByVal value As Single)
				lightIntensity_Renamed = value
			End Set
		End Property
        Private lightIntensity_Renamed As Single = 1.0F
		#End Region

		#Region "virtual methods"
		Protected Overridable Function PartAnimation(ByVal partName As String) As Matrix3D
			Return Matrix3D.Identity
		End Function
		#End Region

		#Region "implementation"
		Friend Sub New()
		End Sub

		Friend Sub Load(ByVal path As String, ByVal manager As XMeshManager)
			Me.manager = manager
			Dim loader As New XMeshTextLoader(Me.manager.device)
			scene = loader.XMeshFromFile(path)
		End Sub

		''' <summary>
		''' The scene as loaded by XMeshTextLoader
		''' </summary>
		Friend scene As Scene

		''' <summary>
		''' The object that manages the XMeshes
		''' </summary>
		Friend manager As XMeshManager

		#End Region

		#Region "IDisposable Members"

		''' <summary>
		''' Releases resources no longer needed.
		''' </summary>
		Public Sub Dispose() Implements IDisposable.Dispose
			If scene.parts IsNot Nothing Then
				For Each part As Part In scene.parts
					If part.vertexBuffer IsNot Nothing Then
						part.vertexBuffer.Dispose()
					End If
					If (part.material IsNot Nothing) AndAlso (part.material.textureResource IsNot Nothing) Then
						part.material.textureResource.Dispose()
					End If
				Next part
				scene.parts.Clear()
				scene.parts = Nothing
			End If
		End Sub

		#End Region
	End Class
End Namespace

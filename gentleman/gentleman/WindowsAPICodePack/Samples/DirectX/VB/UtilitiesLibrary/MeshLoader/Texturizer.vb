' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Runtime.InteropServices
Imports System.IO

Imports Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
Imports Microsoft.WindowsAPICodePack.DirectX
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DXGI
Imports System.Windows.Media.Media3D


Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	''' <summary>
	''' A Mesh that allow for changing textures within the scene
	''' </summary>
	Public Class Texturizer
		Inherits XMesh
		''' <summary>
		''' If trus shows one one texture at a time
		''' </summary>
		Public Property ShowOneTexture() As Boolean
			Get
				Return showOneTexture_Renamed
			End Get
			Set(ByVal value As Boolean)
				showOneTexture_Renamed = value
			End Set
		End Property
        Private showOneTexture_Renamed As Boolean = True

		''' <summary>
		''' This method sets which part to texture during rendering.
		''' </summary>
		''' <param name="partName"></param>
		Public Sub PartToTexture(ByVal partName As String)
			partEmphasis = partName
		End Sub
		Private partEmphasis As String

		''' <summary>
		''' Clears the alternate texture list (restoring the model's textures)
		''' </summary>
		Public Sub RevertTextures()
			alternateTextures.Clear()
		End Sub

		''' <summary>
		''' Gets a list of the names of the parts in the mesh
		''' </summary>
		''' <returns></returns>
		Public Function GetParts() As List(Of String)
			Dim partNames As New List(Of String)()
			For Each part As Part In scene.parts
				partNames.Add(part.name)
			Next part
			Return partNames
		End Function


		''' <summary>
		''' Caretes an alternate texture for a part
		''' </summary>
		''' <param name="partName">The name of the part to create the texture for.</param>
		''' <param name="imagePath">The path to the image to be use for the texture.</param>
		Public Sub SwapTexture(ByVal partName As String, ByVal imagePath As String)
			If partName IsNot Nothing Then
				If File.Exists(imagePath) Then
					Dim stream As FileStream = File.OpenRead(imagePath)

					Try
						Dim srv As ShaderResourceView = TextureLoader.LoadTexture(Me.manager.device, stream)
						If srv IsNot Nothing Then
							alternateTextures(partName) = srv
						End If
					Catch e1 As COMException
						System.Windows.MessageBox.Show("Not a valid image.")
					End Try

				Else
					alternateTextures(partName) = Nothing
				End If
			End If
		End Sub
		Private alternateTextures As New Dictionary(Of String, ShaderResourceView)()

		''' <summary>
		''' Renders the mesh with the specified transformation. This render overrides the base class rendering
		''' to provide part-by-part texturing support.
		''' </summary>
		''' <param name="modelTransform"></param>
		Public Overloads Sub Render(ByVal modelTransform As Matrix3D)
			' setup rasterization
			Dim rDescription As New RasterizerDescription() With {.FillMode = If(ShowWireFrame, FillMode.Wireframe, FillMode.Solid), .CullMode = CullMode.Back, .FrontCounterClockwise = False, .DepthBias = 0, .DepthBiasClamp = 0, .SlopeScaledDepthBias = 0, .DepthClipEnable = True, .ScissorEnable = False, .MultisampleEnable = True, .AntialiasedLineEnable = True}
			Dim rState As RasterizerState = Me.manager.device.CreateRasterizerState(rDescription)

			Me.manager.device.RS.SetState(rState)

            Me.manager.brightnessVariable.FloatValue = Me.LightIntensity

			' start rendering
			For Each part As Part In scene.parts
				If showOneTexture_Renamed AndAlso (part.name <> partEmphasis) Then
					rDescription.FillMode = FillMode.Wireframe
					Dim state As RasterizerState = Me.manager.device.CreateRasterizerState(rDescription)
					Me.manager.device.RS.SetState(state)
				Else
					rDescription.FillMode = FillMode.Solid
					Dim state As RasterizerState = Me.manager.device.CreateRasterizerState(rDescription)
					Me.manager.device.RS.SetState(state)
				End If

				Dim technique As EffectTechnique = Nothing
				If part.material Is Nothing Then
					technique = Me.manager.techniqueRenderVertexColor
				Else
					If part.material.textureResource IsNot Nothing Then
						technique = Me.manager.techniqueRenderTexture

						Dim texture As ShaderResourceView = part.material.textureResource
						If alternateTextures.ContainsKey(part.name) Then
							texture = alternateTextures(part.name)
						End If

						Me.manager.diffuseVariable.SetResource(texture)
					Else
						technique = Me.manager.techniqueRenderMaterialColor
                        Me.manager.materialColorVariable.FloatVector = part.material.materialColor
					End If
				End If

				' set part transform
				Dim partGroup As New Transform3DGroup()
				partGroup.Children.Add(New MatrixTransform3D(PartAnimation(part.name)))
				partGroup.Children.Add(New MatrixTransform3D(part.partTransform.ToMatrix3D()))
				partGroup.Children.Add(New MatrixTransform3D(scene.sceneTransform.ToMatrix3D()))
				partGroup.Children.Add(New MatrixTransform3D(modelTransform))

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

	End Class
End Namespace

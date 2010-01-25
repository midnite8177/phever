' Copyright (c) Microsoft Corporation.  All rights reserved.


Imports Microsoft.VisualBasic
Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports System.Runtime.InteropServices
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3D10
Imports Microsoft.WindowsAPICodePack.DirectX.DXGI
Imports System.Diagnostics
Imports Microsoft.WindowsAPICodePack.DirectX.Direct3DX10


Namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
	''' <summary>
	''' The format of each XMesh vertex
	''' </summary>
	<StructLayout(LayoutKind.Sequential)> _
	Public Structure XMeshVertex
		''' <summary>
		''' The vertex location
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Vertex As Vector4F

		''' <summary>
		''' The vertex normal
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Normal As Vector4F

		''' <summary>
		''' The vertex color
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Color As Vector4F

		''' <summary>
		''' The texture coordinates (U,V)
		''' </summary>
		<MarshalAs(UnmanagedType.Struct)> _
		Public Texture As Vector2F
	End Structure

	''' <summary>
	''' A part is a piece of a scene
	''' </summary>
	Friend Structure Part
		''' <summary>
		''' The name of the part
		''' </summary>
		Public name As String

		''' <summary>
		''' A description of the part data format
		''' </summary>
		Public dataDescription() As InputElementDescription

		''' <summary>
		''' The vertex buffer for the part
		''' </summary>
		Public vertexBuffer As D3DBuffer

		''' <summary>
		''' The number of verticies in the vertex buffer
		''' </summary>
		Public vertexCount As Integer

		''' <summary>
		''' The part texture/material
		''' </summary>
		Public material As Material

		''' <summary>
		''' The transformation to be applied to this part relative to the scene
		''' </summary>
		Public partTransform As Matrix4x4F
	End Structure

	''' <summary>
	''' A scene is the collection of parts that make up a .X file
	''' </summary>
	Friend Structure Scene
		''' <summary>
		''' The parts that make up the scene
		''' </summary>
		Public parts As List(Of Part)

		''' <summary>
		''' The transformation that is to be applied to the scene relative to the view
		''' </summary>
		Public sceneTransform As Matrix4x4F
	End Structure

	Friend Class Material
		''' <summary>
		''' The difuse color of the material
		''' </summary>
		Public materialColor As Vector4F

		''' <summary>
		''' The exponent of the specular color
		''' </summary>
		Public specularPower As Single

		''' <summary>
		''' The specualr color
		''' </summary>
		Public specularColor As Vector3F

		''' <summary>
		''' The emissive color
		''' </summary>
		Public emissiveColor As Vector3F

		''' <summary>
		''' The part texture
		''' </summary>
		Public textureResource As ShaderResourceView
	End Class


	''' <summary>
	''' Specifies how a particular mesh should be shaded
	''' </summary>
	Friend Structure MaterialSpecification
		''' <summary>
		''' The difuse color of the material
		''' </summary>
		Public materialColor As Vector4F

		''' <summary>
		''' The exponent of the specular color
		''' </summary>
		Public specularPower As Single

		''' <summary>
		''' The specualr color
		''' </summary>
		Public specularColor As Vector3F

		''' <summary>
		''' The emissive color
		''' </summary>
		Public emissiveColor As Vector3F

		''' <summary>
		''' The name of the texture file
		''' </summary>
		Public textureFileName As String
	End Structure

	''' <summary>
	''' Loads a text formated .X file
	''' </summary>
	Friend Class XMeshTextLoader
		Private device As D3DDevice
		Private meshDirectory As String = ""

		''' <summary>
		''' Constructor that associates a device with the resulting mesh
		''' </summary>
		''' <param name="device"></param>
		Public Sub New(ByVal device As D3DDevice)
			Me.device = device
		End Sub

		''' <summary>
		''' Loads the mesh from the file
		''' </summary>
		''' <param name="path"></param>
		''' <returns></returns>
		Public Function XMeshFromFile(ByVal path As String) As Scene
			Dim meshPath As String = Nothing

			Dim xFile As StreamReader
			If File.Exists(path) Then
				meshPath = path
			Else
				Dim sdkMediaPath As String = GetDXSDKMediaPath() & path
				If File.Exists(sdkMediaPath) Then
					meshPath = sdkMediaPath
				End If
			End If

			If meshPath Is Nothing Then
				Throw New System.IO.FileNotFoundException("Could not find mesh file.")
			Else
                meshDirectory = System.IO.Path.GetDirectoryName(meshPath)
			End If

			xFile = File.OpenText(meshPath)

			ValidateHeader(xFile)

			Dim data As String = xFile.ReadToEnd()
			Return ExtractScene(data)
		End Function

		''' <summary>
		''' Returns the path to the DX SDK dir
		''' </summary>
		''' <returns></returns>
		Private Function GetDXSDKMediaPath() As String
			Return Environment.GetEnvironmentVariable("DXSDK_DIR")
		End Function

		''' <summary>
		''' Validates the header of the .X file. Enforces the text-only requirement of this code.
		''' </summary>
		''' <param name="xFile"></param>
		Private Sub ValidateHeader(ByVal xFile As StreamReader)
			Dim fileHeader As String = xFile.ReadLine()
			Dim headerParse As New Regex("xof (\d\d)(\d\d)(\w\w\w[\w\s])(\d\d\d\d)")
			Dim m As Match = headerParse.Match(fileHeader)

			If m.Success = False Then
				Throw New System.IO.InvalidDataException("Invalid .X file.")
			End If

			If m.Groups.Count <> 5 Then
				Throw New System.IO.InvalidDataException("Invalid .X file.")
			End If

			If m.Groups(1).ToString() <> "03" Then ' version 3.x supported
				Throw New System.IO.InvalidDataException("Unknown .X file version.")
			End If

			If m.Groups(3).ToString() <> "txt " Then
				Throw New System.IO.InvalidDataException("Only text .X files are supported.")
			End If
		End Sub

		''' <summary>
		''' Parses the root scene of the .X file 
		''' </summary>
		''' <param name="data"></param>
		Private Function ExtractScene(ByVal data As String) As Scene
			' .X files may have frames with sub meshes, or may have a single mesh
			Dim frameSceneRootTag As New Regex("^Frame[\s]?([\w_]+)?[\s]*{", RegexOptions.Multiline)
			Dim scene As New Scene()
			Dim root As Match = frameSceneRootTag.Match(data)
			If root.Success Then
				Dim frameConent As String = GetCurlyBraceContent(data, root.Index + root.Length - 1)
				scene.sceneTransform = ExtractFrameTransformation(frameConent)
				scene.parts = ExtractParts(frameConent)
			Else
				Dim firstMesh As New Regex("^Mesh[\s]?([\w\d_]+)?[\s]*{", RegexOptions.Multiline)
				Dim mesh As Match = firstMesh.Match(data)
				If Not mesh.Success Then
					Throw New System.IO.InvalidDataException("Problem parsing file")
				End If

				scene.parts = New List(Of Part)()
				scene.parts.Add(BuildPart(data.Substring(mesh.Index), ""))
                scene.sceneTransform = Matrix4x4F.Identity
			End If

			Return scene
		End Function

		''' <summary>
		''' Searches through a string to find the matching closing breace and return the content 
		''' between the braces
		''' </summary>
		''' <param name="sourceData">the data to extract curly brace content from</param>
		''' <param name="startIndex">the index of the starting brace in the sourceData string</param>
		''' <returns>the content enclosed by a curly brace pair</returns>
		Private Function GetCurlyBraceContent(ByVal sourceData As String, ByVal braceStartIndex As Integer) As String
			If sourceData.Chars(braceStartIndex) <> "{"c Then
				Throw New ArgumentException("braceStartIndex must point to a '{' in sourceData")
			End If

			Dim braceLevel As Integer = 1
			Dim braceEndIndex As Integer = braceStartIndex + 1
			Do While braceEndIndex < sourceData.Length
				If sourceData.Chars(braceEndIndex) = "{"c Then
					braceLevel += 1
				ElseIf sourceData.Chars(braceEndIndex) = "}"c Then
					braceLevel -= 1
				End If

				If braceLevel = 0 Then
					Return sourceData.Substring(braceStartIndex + 1, braceEndIndex - braceStartIndex - 2)
				End If
				braceEndIndex += 1
			Loop

			Throw New ArgumentException("matching brace not found")
		End Function

		''' <summary>
		''' Extracts the transformation associated with the current frame
		''' </summary>
		''' <param name="dataFile"></param>
		''' <param name="dataOffset"></param>
		''' <returns></returns>
		Private Function ExtractFrameTransformation(ByVal frameContent As String) As Matrix4x4F
			Dim frameTransformationMatrixTag As New Regex("FrameTransformMatrix {")
			Dim frameTransformTag As Match = frameTransformationMatrixTag.Match(frameContent)
			If Not frameTransformTag.Success Then
                Return Matrix4x4F.Identity
			End If

			Dim rawMatrixData As String = GetCurlyBraceContent(frameContent, frameTransformTag.Index + frameTransformTag.Length - 1)

			Dim matrixData As New Regex("([-\d\.,\s]+);;")
			Dim data As Match = matrixData.Match(rawMatrixData)
			If Not data.Success Then
				Throw New System.IO.InvalidDataException("Error parsing frame transformation.")
			End If

			Dim values() As String = data.Groups(1).ToString().Split(New Char() { ","c })
			If values.Length <> 16 Then
				Throw New System.IO.InvalidDataException("Error parsing frame transformation.")
			End If
			Dim fvalues(15) As Single
			For n As Integer = 0 To 15
                fvalues(n) = Single.Parse(values(n), CultureInfo.InvariantCulture)
			Next n

			Return New Matrix4x4F(fvalues)
		End Function

		''' <summary>
		''' Extracts the list of parts from the scene
		''' </summary>
		''' <param name="frameData"></param>
		''' <returns></returns>
		Private Function ExtractParts(ByVal frameData As String) As List(Of Part)
			Dim parts As New List(Of Part)()

			Dim frameMatch As New Regex("Frame([\s][\w]+[\s\r\n]+)?{")
			Dim frames As MatchCollection = frameMatch.Matches(frameData)
			If frames.Count > 0 Then
				For Each frame As Match In frames
					Dim subFrameData As String = GetCurlyBraceContent(frameData, frame.Index + frame.Length - 1)
					Dim partName As String = frame.Groups(1).ToString()
					partName = partName.TrimEnd(New Char() { " "c })
					partName = partName.TrimStart(New Char() { " "c })
					Dim part As Part = BuildPart(subFrameData, partName)
					parts.Add(part)
				Next frame
			Else
				Dim part As Part = BuildPart(frameData, "")
				parts.Add(part)
			End If

			Return parts
		End Function

		''' <summary>
		''' Extracts the vertex, normal, and texture data for a part
		''' </summary>
		''' <param name="partData"></param>
		''' <param name="partName"></param>
		''' <returns></returns>
		Private Function BuildPart(ByVal partData As String, ByVal partName As String) As Part
			Dim part As New Part()

			Dim description() As InputElementDescription = { New InputElementDescription() With {.SemanticName = "POSITION", .SemanticIndex = 0, .Format = Format.R32G32B32A32_FLOAT, .InputSlot = 0, .AlignedByteOffset = 0, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}, New InputElementDescription() With {.SemanticName = "NORMAL", .SemanticIndex = 0, .Format = Format.R32G32B32A32_FLOAT, .InputSlot = 0, .AlignedByteOffset = 16, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}, New InputElementDescription() With {.SemanticName = "COLOR", .SemanticIndex = 0, .Format = Format.R32G32B32A32_FLOAT, .InputSlot = 0, .AlignedByteOffset = 32, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0}, New InputElementDescription() With {.SemanticName = "TEXCOORD", .SemanticIndex = 0, .Format = Format.R32G32_FLOAT, .InputSlot = 0, .AlignedByteOffset = 48, .InputSlotClass = InputClassification.PerVertexData, .InstanceDataStepRate = 0} }
			part.dataDescription = description
			part.name = partName

			part.partTransform = ExtractFrameTransformation(partData)

			' extract mesh (vertex, index, and colors)
			Dim meshContents As String = GetTagContent(New Regex("Mesh[\s]?([\w\d_]+)?[\s]+{"), partData)
			If meshContents.Length > 0 Then
				LoadMesh(part, meshContents)
			End If

			Return part
		End Function

		''' <summary>
		''' Extracts the data part from a construct that looks like:
		'''   tag { data }
		''' </summary>
		''' <param name="searchTag">A regex that specifies the search pattern. Needs to end in '{'</param>
		''' <param name="data">the string to search through for data enclosed within braces.</param>
		''' <returns></returns>
		Private Function GetTagContent(ByVal searchTag As Regex, ByVal data As String) As String
			If searchTag.ToString().EndsWith("{") = False Then
				Throw New ArgumentException("Search tag must end with '{'")
			End If

			Dim match As Match = searchTag.Match(data)
			If Not match.Success Then
				Return ""
			Else
				Return GetCurlyBraceContent(data, match.Index + match.Length - 1)
			End If
		End Function

		Private findArrayCount As New Regex("([\d]+);")
		Private findVector4F As New Regex("([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);")
		Private findVector3F As New Regex("([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);")
		Private findVector2F As New Regex("([-\d]+\.[\d]+);([-\d]+\.[\d]+);")
		Private findScalarF As New Regex("([-\d]+\.[\d]+);")


		''' <summary>
		''' Loads the first material for a mesh
		''' </summary>
		''' <param name="meshMAterialData"></param>
		''' <returns></returns>
		Private Function LoadMeshMaterialList(ByVal meshMaterialListData As String) As List(Of MaterialSpecification)
			Dim findMaterial As New Regex("Material[\s]+{")
			Dim materials As MatchCollection = findMaterial.Matches(meshMaterialListData)
			If materials.Count = 0 Then
				Return Nothing
			End If

			Dim materialList As New List(Of MaterialSpecification)()
			For Each material As Match In materials
				Dim materialContent As String = GetCurlyBraceContent(meshMaterialListData, material.Index + material.Length - 1)
				materialList.Add(LoadMeshMaterial(materialContent))
			Next material

			Return materialList
		End Function

		''' <summary>
		''' Loads a MeshMaterial subresource
		''' </summary>
		''' <param name="materialData"></param>
		''' <returns></returns>
		Private Function LoadMeshMaterial(ByVal materialData As String) As MaterialSpecification
			Dim m As New MaterialSpecification()
			Dim dataOffset As Integer = 0
			Dim color As Match = findVector4F.Match(materialData, dataOffset)
			If Not color.Success Then
				Throw New System.IO.InvalidDataException("problem reading material color")
			End If
            m.materialColor.x = Single.Parse(color.Groups(1).ToString(), CultureInfo.InvariantCulture)
            m.materialColor.y = Single.Parse(color.Groups(2).ToString(), CultureInfo.InvariantCulture)
            m.materialColor.z = Single.Parse(color.Groups(3).ToString(), CultureInfo.InvariantCulture)
            m.materialColor.w = Single.Parse(color.Groups(4).ToString(), CultureInfo.InvariantCulture)
			dataOffset = color.Index + color.Length

			Dim power As Match = findScalarF.Match(materialData, dataOffset)
			If Not power.Success Then
				Throw New System.IO.InvalidDataException("problem reading material specular color exponent")
			End If
            m.specularPower = Single.Parse(power.Groups(1).ToString(), CultureInfo.InvariantCulture)
			dataOffset = power.Index + power.Length

			Dim specular As Match = findVector3F.Match(materialData, dataOffset)
			If Not specular.Success Then
				Throw New System.IO.InvalidDataException("problem reading material specular color")
			End If
            m.specularColor.x = Single.Parse(specular.Groups(1).ToString(), CultureInfo.InvariantCulture)
            m.specularColor.y = Single.Parse(specular.Groups(2).ToString(), CultureInfo.InvariantCulture)
            m.specularColor.z = Single.Parse(specular.Groups(3).ToString(), CultureInfo.InvariantCulture)
            dataOffset = specular.Index + specular.Length

            Dim emissive As Match = findVector3F.Match(materialData, dataOffset)
            If Not emissive.Success Then
                Throw New System.IO.InvalidDataException("problem reading material emissive color")
            End If
            m.emissiveColor.x = Single.Parse(emissive.Groups(1).ToString(), CultureInfo.InvariantCulture)
            m.emissiveColor.y = Single.Parse(emissive.Groups(2).ToString(), CultureInfo.InvariantCulture)
            m.emissiveColor.z = Single.Parse(emissive.Groups(3).ToString(), CultureInfo.InvariantCulture)
            dataOffset = emissive.Index + emissive.Length

            Dim findTextureFile As New Regex("TextureFilename[\s]+{")
            Dim textureFile As Match = findTextureFile.Match(materialData, dataOffset)
            If textureFile.Success Then
                Dim materialFilenameContent As String = GetCurlyBraceContent(materialData, textureFile.Index + textureFile.Length - 1)
                Dim findFilename As New Regex("[\s]+""([\\\w\.]+)"";")
                Dim filename As Match = findFilename.Match(materialFilenameContent)
                If Not filename.Success Then
                    Throw New System.IO.InvalidDataException("problem reading texture filename")
                End If
                m.textureFileName = filename.Groups(1).ToString()
            End If

            Return m
        End Function

        Friend Class IndexedMeshNormals
            Public normalVectors As List(Of Vector4F)
            Public normalIndexMap As List(Of Int32)
        End Class

        ''' <summary>
        ''' Loads the indexed normal vectors for a mesh
        ''' </summary>
        ''' <param name="meshNormalData"></param>
        ''' <returns></returns>
        Private Function LoadMeshNormals(ByVal meshNormalData As String) As IndexedMeshNormals
            Dim indexedMeshNormals As New IndexedMeshNormals()

            Dim normalCount As Match = findArrayCount.Match(meshNormalData)
            If Not normalCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading mesh normals count")
            End If

            indexedMeshNormals.normalVectors = New List(Of Vector4F)()
            Dim normals As Integer = Integer.Parse(normalCount.Groups(1).Value, CultureInfo.InvariantCulture)
            Dim dataOffset As Integer = normalCount.Index + normalCount.Length
            For normalIndex As Integer = 0 To normals - 1
                Dim normal As Match = findVector3F.Match(meshNormalData, dataOffset)
                If Not normal.Success Then
                    Throw New System.IO.InvalidDataException("problem reading mesh normal vector")
                Else
                    dataOffset = normal.Index + normal.Length
                End If

                indexedMeshNormals.normalVectors.Add(New Vector4F(Single.Parse(normal.Groups(1).Value, CultureInfo.InvariantCulture), Single.Parse(normal.Groups(2).Value, CultureInfo.InvariantCulture), Single.Parse(normal.Groups(3).Value, CultureInfo.InvariantCulture), 1.0F))
            Next normalIndex

            Dim faceNormalCount As Match = findArrayCount.Match(meshNormalData, dataOffset)
            If Not faceNormalCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading mesh normals count")
            End If

            indexedMeshNormals.normalIndexMap = New List(Of Int32)()
            Dim faceCount As Integer = Integer.Parse(faceNormalCount.Groups(1).Value, CultureInfo.InvariantCulture)
            dataOffset = faceNormalCount.Index + faceNormalCount.Length
            For faceNormalIndex As Integer = 0 To faceCount - 1
                Dim normalFace As Match = findVertexIndex.Match(meshNormalData, dataOffset)
                If Not normalFace.Success Then
                    Throw New System.IO.InvalidDataException("problem reading mesh normal face")
                Else
                    dataOffset = normalFace.Index + normalFace.Length
                End If

                Dim vertexIndexes() As String = normalFace.Groups(2).Value.Split(New Char() {","c})

                For n As Integer = 0 To vertexIndexes.Length - 3
                    indexedMeshNormals.normalIndexMap.Add(Integer.Parse(vertexIndexes(0), CultureInfo.InvariantCulture))
                    indexedMeshNormals.normalIndexMap.Add(Integer.Parse(vertexIndexes(1 + n), CultureInfo.InvariantCulture))
                    indexedMeshNormals.normalIndexMap.Add(Integer.Parse(vertexIndexes(2 + n), CultureInfo.InvariantCulture))
                Next n
            Next faceNormalIndex

            Return indexedMeshNormals
        End Function

        ''' <summary>
        ''' Loads the per vertex color for a mesh
        ''' </summary>
        ''' <param name="vertexColorData"></param>
        ''' <returns></returns>
        Private Function LoadMeshColors(ByVal vertexColorData As String) As Dictionary(Of Integer, Vector4F)
            Dim findVertexColor As New Regex("([\d]+); ([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);;")

            Dim vertexCount As Match = findArrayCount.Match(vertexColorData)
            If Not vertexCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading vertex colors count")
            End If

            Dim colorDictionary As New Dictionary(Of Integer, Vector4F)()
            Dim verticies As Integer = Integer.Parse(vertexCount.Groups(1).Value, CultureInfo.InvariantCulture)
            Dim dataOffset As Integer = vertexCount.Index + vertexCount.Length
            For vertexIndex As Integer = 0 To verticies - 1
                Dim vertexColor As Match = findVertexColor.Match(vertexColorData, dataOffset)
                If Not vertexColor.Success Then
                    Throw New System.IO.InvalidDataException("problem reading vertex colors")
                Else
                    dataOffset = vertexColor.Index + vertexColor.Length
                End If

                colorDictionary(Integer.Parse(vertexColor.Groups(1).Value, CultureInfo.InvariantCulture)) = New Vector4F(Single.Parse(vertexColor.Groups(2).Value, CultureInfo.InvariantCulture), Single.Parse(vertexColor.Groups(3).Value, CultureInfo.InvariantCulture), Single.Parse(vertexColor.Groups(4).Value, CultureInfo.InvariantCulture), Single.Parse(vertexColor.Groups(5).Value, CultureInfo.InvariantCulture))
            Next vertexIndex

            Return colorDictionary
        End Function

        ''' <summary>
        ''' Loads the texture coordinates(U,V) for a mesh
        ''' </summary>
        ''' <param name="textureCoordinateData"></param>
        ''' <returns></returns>
        Private Function LoadMeshTextureCoordinates(ByVal textureCoordinateData As String) As List(Of Vector2F)
            Dim coordinateCount As Match = findArrayCount.Match(textureCoordinateData)
            If Not coordinateCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading mesh texture coordinates count")
            End If

            Dim textureCoordinates As New List(Of Vector2F)()
            Dim coordinates As Integer = Integer.Parse(coordinateCount.Groups(1).Value, CultureInfo.InvariantCulture)
            Dim dataOffset As Integer = coordinateCount.Index + coordinateCount.Length
            For coordinateIndex As Integer = 0 To coordinates - 1
                Dim coordinate As Match = findVector2F.Match(textureCoordinateData, dataOffset)
                If Not coordinate.Success Then
                    Throw New System.IO.InvalidDataException("problem reading texture coordinate count")
                Else
                    dataOffset = coordinate.Index + coordinate.Length
                End If

                textureCoordinates.Add(New Vector2F(Single.Parse(coordinate.Groups(1).Value, CultureInfo.InvariantCulture), Single.Parse(coordinate.Groups(2).Value, CultureInfo.InvariantCulture)))
            Next coordinateIndex

            Return textureCoordinates
        End Function

        Private findVertexIndex As New Regex("([\d]+);[\s]*([\d,]+)?;")

        ''' <summary>
        ''' Loads a mesh and creates the vertex/index buffers for the part
        ''' </summary>
        ''' <param name="part"></param>
        ''' <param name="meshData"></param>
        Private Sub LoadMesh(ByRef part As Part, ByVal meshData As String)

            ' load vertex data
            Dim dataOffset As Integer = 0
            Dim vertexCount As Match = findArrayCount.Match(meshData)
            If Not vertexCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading vertex count")
            End If

            Dim vertexList As New List(Of Vector4F)()
            Dim verticies As Integer = Integer.Parse(vertexCount.Groups(1).Value, CultureInfo.InvariantCulture)
            dataOffset = vertexCount.Index + vertexCount.Length
            For vertexIndex As Integer = 0 To verticies - 1
                Dim vertex As Match = findVector3F.Match(meshData, dataOffset)
                If Not vertex.Success Then
                    Throw New System.IO.InvalidDataException("problem reading vertex")
                Else
                    dataOffset = vertex.Index + vertex.Length
                End If

                vertexList.Add(New Vector4F(Single.Parse(vertex.Groups(1).Value, CultureInfo.InvariantCulture), Single.Parse(vertex.Groups(2).Value, CultureInfo.InvariantCulture), Single.Parse(vertex.Groups(3).Value, CultureInfo.InvariantCulture), 1.0F))
            Next vertexIndex

            ' load triangle index data
            Dim triangleIndexCount As Match = findArrayCount.Match(meshData, dataOffset)
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length
            If Not triangleIndexCount.Success Then
                Throw New System.IO.InvalidDataException("problem reading index count")
            End If

            Dim triangleIndiciesList As New List(Of Int32)()
            Dim triangleIndexListCount As Integer = Integer.Parse(triangleIndexCount.Groups(1).Value, CultureInfo.InvariantCulture)
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length
            For triangleIndicyIndex As Integer = 0 To triangleIndexListCount - 1
                Dim indexEntry As Match = findVertexIndex.Match(meshData, dataOffset)
                If Not indexEntry.Success Then
                    Throw New System.IO.InvalidDataException("problem reading vertex index entry")
                Else
                    dataOffset = indexEntry.Index + indexEntry.Length
                End If

                Dim indexEntryCount As Integer = Integer.Parse(indexEntry.Groups(1).Value, CultureInfo.InvariantCulture)
                Dim vertexIndexes() As String = indexEntry.Groups(2).Value.Split(New Char() {","c})
                If indexEntryCount <> vertexIndexes.Length Then
                    Throw New System.IO.InvalidDataException("vertex index count does not equal count of indicies found")
                End If

                For entryIndex As Integer = 0 To indexEntryCount - 3
                    triangleIndiciesList.Add(Integer.Parse(vertexIndexes(0), CultureInfo.InvariantCulture))
                    triangleIndiciesList.Add(Integer.Parse(vertexIndexes(1 + entryIndex).ToString(), CultureInfo.InvariantCulture))
                    triangleIndiciesList.Add(Integer.Parse(vertexIndexes(2 + entryIndex).ToString(), CultureInfo.InvariantCulture))
                Next entryIndex
            Next triangleIndicyIndex

            ' load mesh colors
            Dim vertexColorData As String = GetTagContent(New Regex("MeshVertexColors[\s]+{"), meshData)
            Dim colorDictionary As Dictionary(Of Integer, Vector4F) = Nothing
            If vertexColorData <> "" Then
                colorDictionary = LoadMeshColors(vertexColorData)
            End If

            ' load mesh normals
            Dim meshNormalData As String = GetTagContent(New Regex("MeshNormals[\s]+{"), meshData)
            Dim meshNormals As IndexedMeshNormals = Nothing
            If meshNormalData <> "" Then
                meshNormals = LoadMeshNormals(meshNormalData)
            End If

            ' load mesh texture coordinates
            Dim meshTextureCoordsData As String = GetTagContent(New Regex("MeshTextureCoords[\s]+{"), meshData)
            Dim meshTextureCoords As List(Of Vector2F) = Nothing
            If meshTextureCoordsData <> "" Then
                meshTextureCoords = LoadMeshTextureCoordinates(meshTextureCoordsData)
            End If

            ' load mesh material
            Dim meshMaterialsData As String = GetTagContent(New Regex("MeshMaterialList[\s]+{"), meshData)
            Dim meshMaterials As List(Of MaterialSpecification) = Nothing
            If meshMaterialsData <> "" Then
                meshMaterials = LoadMeshMaterialList(meshMaterialsData)
            End If

            ' copy vertex data to HGLOBAL
            Dim byteLength As Integer = Marshal.SizeOf(GetType(XMeshVertex)) * triangleIndiciesList.Count
            Dim nativeVertex As IntPtr = Marshal.AllocHGlobal(byteLength)
            Dim byteBuffer(byteLength - 1) As Byte
            Dim varray(triangleIndiciesList.Count - 1) As XMeshVertex
            For n As Integer = 0 To triangleIndiciesList.Count - 1
                Dim vertex As New XMeshVertex() With {.Vertex = vertexList(triangleIndiciesList(n)), .Normal = If((meshNormals Is Nothing), New Vector4F(0, 0, 0, 1.0F), meshNormals.normalVectors(meshNormals.normalIndexMap(n))), .Color = (If((colorDictionary Is Nothing), New Vector4F(0, 0, 0, 0), colorDictionary(triangleIndiciesList(n)))), .Texture = (If((meshTextureCoords Is Nothing), New Vector2F(0, 0), meshTextureCoords(triangleIndiciesList(n))))}
                Dim vertexData() As Byte = RawSerialize(vertex)
                Buffer.BlockCopy(vertexData, 0, byteBuffer, vertexData.Length * n, vertexData.Length)
            Next n
            Marshal.Copy(byteBuffer, 0, nativeVertex, byteLength)

            ' build vertex buffer
            Dim bdv As New BufferDescription() With {.Usage = Usage.Default, .ByteWidth = CUInt(Marshal.SizeOf(GetType(XMeshVertex)) * triangleIndiciesList.Count), .BindFlags = BindFlag.VertexBuffer, .CpuAccessFlags = 0, .MiscFlags = 0}
            Dim vertexInit As New SubresourceData() With {.SysMem = nativeVertex}

            part.vertexBuffer = device.CreateBuffer(bdv, vertexInit)
            Debug.Assert(part.vertexBuffer IsNot Nothing)

            part.vertexCount = triangleIndiciesList.Count

            If meshMaterials IsNot Nothing Then
                ' only a single material is currently supported
                Dim m As MaterialSpecification = meshMaterials(0)

                part.material = New Material() With {.emissiveColor = m.emissiveColor, .specularColor = m.specularColor, .materialColor = m.materialColor, .specularPower = m.specularPower}

                Dim texturePath As String = ""
                If File.Exists(m.textureFileName) Then
                    texturePath = m.textureFileName
                End If
                If File.Exists(meshDirectory & "\" & m.textureFileName) Then
                    texturePath = meshDirectory & "\" & m.textureFileName
                End If
                If File.Exists(meshDirectory & "\..\" & m.textureFileName) Then
                    texturePath = meshDirectory & "\..\" & m.textureFileName
                End If

                If texturePath.Length = 0 Then
                    part.material.textureResource = Nothing
                Else
                    part.material.textureResource = D3D10XHelpers.CreateShaderResourceViewFromFile(device, texturePath)
                End If
            End If

            Marshal.FreeHGlobal(nativeVertex)
        End Sub

		''' <summary>
		''' Copys an arbitrary strcuture into a byte array
		''' </summary>
		''' <param name="anything"></param>
		''' <returns></returns>
		Public Function RawSerialize(ByVal anything As Object) As Byte()
			Dim rawsize As Integer = Marshal.SizeOf(anything)
			Dim buffer As IntPtr = Marshal.AllocHGlobal(rawsize)
			Marshal.StructureToPtr(anything, buffer, False)
			Dim rawdatas(rawsize - 1) As Byte
			Marshal.Copy(buffer, rawdatas, 0, rawsize)
			Marshal.FreeHGlobal(buffer)
			Return rawdatas
		End Function
	End Class
End Namespace

// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.Direct3DX10;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;


namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    /// <summary>
    /// The format of each XMesh vertex
    /// </summary>
    [StructLayout( LayoutKind.Sequential )]
    public struct XMeshVertex
    {
        /// <summary>
        /// The vertex location
        /// </summary>
        [MarshalAs( UnmanagedType.Struct )]
        public Vector4F Vertex;

        /// <summary>
        /// The vertex normal
        /// </summary>
        [MarshalAs( UnmanagedType.Struct )]
        public Vector4F Normal;

        /// <summary>
        /// The vertex color
        /// </summary>
        [MarshalAs( UnmanagedType.Struct )]
        public Vector4F Color;

        /// <summary>
        /// The texture coordinates (U,V)
        /// </summary>
        [MarshalAs( UnmanagedType.Struct )]
        public Vector2F Texture;
    };
    
    /// <summary>
    /// A part is a piece of a scene
    /// </summary>
    internal struct Part
    {
        /// <summary>
        /// The name of the part
        /// </summary>
        public string name;

        /// <summary>
        /// A description of the part data format
        /// </summary>
        public InputElementDescription[ ] dataDescription;

        /// <summary>
        /// The vertex buffer for the part
        /// </summary>
        public D3DBuffer vertexBuffer;

        /// <summary>
        /// The number of verticies in the vertex buffer
        /// </summary>
        public int vertexCount;
        
        /// <summary>
        /// The part texture/material
        /// </summary>
        public Material material;

        /// <summary>
        /// The transformation to be applied to this part relative to the scene
        /// </summary>
        public Matrix4x4F partTransform;
    }

    /// <summary>
    /// A scene is the collection of parts that make up a .X file
    /// </summary>
    internal struct Scene
    {
        /// <summary>
        /// The parts that make up the scene
        /// </summary>
        public List<Part> parts;

        /// <summary>
        /// The transformation that is to be applied to the scene relative to the view
        /// </summary>
        public Matrix4x4F sceneTransform;
    }

    internal class Material
    {
        /// <summary>
        /// The difuse color of the material
        /// </summary>
        public Vector4F materialColor;

        /// <summary>
        /// The exponent of the specular color
        /// </summary>
        public float specularPower;

        /// <summary>
        /// The specualr color
        /// </summary>
        public Vector3F specularColor;

        /// <summary>
        /// The emissive color
        /// </summary>
        public Vector3F emissiveColor;

        /// <summary>
        /// The part texture
        /// </summary>
        public ShaderResourceView textureResource;
    }


    /// <summary>
    /// Specifies how a particular mesh should be shaded
    /// </summary>
    internal struct MaterialSpecification
    {
        /// <summary>
        /// The difuse color of the material
        /// </summary>
        public Vector4F materialColor;

        /// <summary>
        /// The exponent of the specular color
        /// </summary>
        public float specularPower;

        /// <summary>
        /// The specualr color
        /// </summary>
        public Vector3F specularColor;

        /// <summary>
        /// The emissive color
        /// </summary>
        public Vector3F emissiveColor;

        /// <summary>
        /// The name of the texture file
        /// </summary>
        public string textureFileName;
    }

    /// <summary>
    /// Loads a text formated .X file
    /// </summary>
    internal class XMeshTextLoader
    {
        static InputElementDescription[] description = new InputElementDescription[]
        {
            new InputElementDescription()
            {
                SemanticName = "POSITION",
                SemanticIndex = 0,
                Format = Format.R32G32B32A32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = 0,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            },
            new InputElementDescription()
            {
                SemanticName = "NORMAL",
                SemanticIndex = 0,
                Format = Format.R32G32B32A32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = 16,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            },
            new InputElementDescription()
            {
                SemanticName = "COLOR",
                SemanticIndex = 0,
                Format = Format.R32G32B32A32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = 32,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0
            },
            new InputElementDescription()
            {
                SemanticName = "TEXCOORD",
                SemanticIndex = 0,
                Format = Format.R32G32_FLOAT,
                InputSlot = 0,
                AlignedByteOffset = 48,
                InputSlotClass = InputClassification.PerVertexData,
                InstanceDataStepRate = 0,
            }

        };
        private D3DDevice device;
        private string meshDirectory = "";
        
        /// <summary>
        /// Constructor that associates a device with the resulting mesh
        /// </summary>
        /// <param name="device"></param>
        public XMeshTextLoader( D3DDevice device )
        {
            this.device = device;
        }

        /// <summary>
        /// Loads the mesh from the file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Scene XMeshFromFile( string path )
        {
            string meshPath = null;

            if( File.Exists( path ) )
            {
                meshPath = path;
            }
            else
            {
                string sdkMediaPath = GetDXSDKMediaPath( ) + path;
                if (File.Exists(sdkMediaPath))
                    meshPath = sdkMediaPath;
            }

            if( meshPath == null )
                throw new System.IO.FileNotFoundException( "Could not find mesh file." );
            else
                meshDirectory = Path.GetDirectoryName( meshPath );

            string data = null;
            using (StreamReader xFile = File.OpenText(meshPath))
            {
                string header = xFile.ReadLine();
                ValidateHeader(header);
                data = xFile.ReadToEnd();
            }

            return ExtractScene( data );
        }

        /// <summary>
        /// Returns the path to the DX SDK dir
        /// </summary>
        /// <returns></returns>
        private string GetDXSDKMediaPath( )
        {
            return Environment.GetEnvironmentVariable( "DXSDK_DIR" );
        }

        /// <summary>
        /// Validates the header of the .X file. Enforces the text-only requirement of this code.
        /// </summary>
        /// <param name="xFile"></param>
        private void ValidateHeader( string fileHeader)
        {
            Regex headerParse = new Regex( @"xof (\d\d)(\d\d)(\w\w\w[\w\s])(\d\d\d\d)" );
            Match m = headerParse.Match( fileHeader );

            if( m.Success == false )
                throw new System.IO.InvalidDataException( "Invalid .X file." );

            if( m.Groups.Count != 5 )
                throw new System.IO.InvalidDataException( "Invalid .X file." );

            if( m.Groups[ 1 ].ToString( ) != "03" )                     // version 3.x supported
                throw new System.IO.InvalidDataException( "Unknown .X file version." );

            if( m.Groups[ 3 ].ToString( ) != "txt " )
                throw new System.IO.InvalidDataException( "Only text .X files are supported." );
        }

        /// <summary>
        /// Parses the root scene of the .X file 
        /// </summary>
        /// <param name="data"></param>
        private Scene ExtractScene( string data )
        {

            // .X files may have frames with sub meshes, or may have a single mesh
            Regex frameSceneRootTag = new Regex( @"^Frame[\s]?([\w_]+)?[\s]*{", RegexOptions.Multiline );
            Scene scene = new Scene( );
            Match root = frameSceneRootTag.Match( data );
            if( root.Success )
            {
                string frameConent = GetCurlyBraceContent( data, root.Index + root.Length - 1 );
                scene.sceneTransform = ExtractFrameTransformation( frameConent );
                scene.parts = ExtractParts( frameConent );
            }
            else
            {
                Regex firstMesh = new Regex( @"^Mesh[\s]?([\w\d_]+)?[\s]*{", RegexOptions.Multiline );
                Match mesh = firstMesh.Match( data );
                if( !mesh.Success )
                    throw new System.IO.InvalidDataException( "Problem parsing file" );

                scene.parts = new List<Part>( );
                scene.parts.Add( BuildPart( data.Substring( mesh.Index ), "" ) );
                scene.sceneTransform = Matrix4x4F.Identity;
            }
            return scene;
        }

        /// <summary>
        /// Searches through a string to find the matching closing breace and return the content 
        /// between the braces
        /// </summary>
        /// <param name="sourceData">the data to extract curly brace content from</param>
        /// <param name="startIndex">the index of the starting brace in the sourceData string</param>
        /// <returns>the content enclosed by a curly brace pair</returns>
        private string GetCurlyBraceContent( string sourceData, int braceStartIndex )
        {
            if( sourceData[ braceStartIndex ] != '{' )
                throw new ArgumentException( "braceStartIndex must point to a '{' in sourceData" );

            int braceLevel = 1;
            int braceEndIndex = braceStartIndex + 1;
            for( ; braceEndIndex < sourceData.Length; braceEndIndex++ )
            {
                if( sourceData[ braceEndIndex ] == '{' )
                    braceLevel++;
                else if( sourceData[ braceEndIndex ] == '}' )
                    braceLevel--;

                if( braceLevel == 0 )
                {
                    return sourceData.Substring( braceStartIndex + 1, braceEndIndex - braceStartIndex - 2 );
                }
            }

            throw new ArgumentException( "matching brace not found" );
        }

        /// <summary>
        /// Extracts the transformation associated with the current frame
        /// </summary>
        /// <param name="dataFile"></param>
        /// <param name="dataOffset"></param>
        /// <returns></returns>
        private Matrix4x4F ExtractFrameTransformation( string frameContent )
        {
            Regex frameTransformationMatrixTag = new Regex( "FrameTransformMatrix {" );
            Match frameTransformTag = frameTransformationMatrixTag.Match( frameContent );
            if( !frameTransformTag.Success )
                return Matrix4x4F.Identity;

            string rawMatrixData = GetCurlyBraceContent( frameContent, frameTransformTag.Index + frameTransformTag.Length - 1 );

            Regex matrixData = new Regex( @"([-\d\.,\s]+);;" );
            Match data = matrixData.Match( rawMatrixData );
            if( !data.Success )
                throw new System.IO.InvalidDataException( "Error parsing frame transformation." );

            string[ ] values = data.Groups[ 1 ].ToString( ).Split( new char[ ] { ',' } );
            if( values.Length != 16 )
                throw new System.IO.InvalidDataException( "Error parsing frame transformation." );
            float[ ] fvalues = new float[ 16 ];
            for( int n = 0; n < 16; n++ )
            {
                fvalues[n] = float.Parse(values[n], CultureInfo.InvariantCulture);
            }

            return new Matrix4x4F( fvalues );
        }

        /// <summary>
        /// Extracts the list of parts from the scene
        /// </summary>
        /// <param name="frameData"></param>
        /// <returns></returns>
        private List<Part> ExtractParts( string frameData )
        {
            List<Part> parts = new List<Part>();

            Regex frameMatch = new Regex( @"Frame([\s][\w]+[\s\r\n]+)?{" );
            MatchCollection frames = frameMatch.Matches( frameData );
            if( frames.Count > 0 )
            {
                foreach (Match frame in frames)
                {
                    string subFrameData = GetCurlyBraceContent( frameData, frame.Index + frame.Length - 1 );
                    string partName = frame.Groups[ 1 ].ToString( );
                    partName = partName.Trim(new char[] { ' ' });
                    Part part = BuildPart(subFrameData, partName);
                    parts.Add( part );
                }
            }
            else
            {
                Part part = BuildPart( frameData, "" );
                parts.Add( part );
            }

            return parts;
        }

        /// <summary>
        /// Extracts the vertex, normal, and texture data for a part
        /// </summary>
        /// <param name="partData"></param>
        /// <param name="partName"></param>
        /// <returns></returns>
        private Part BuildPart( string partData, string partName )
        {
            Part part = new Part( );

            part.dataDescription = description;
            part.name = partName;

            part.partTransform = ExtractFrameTransformation( partData );

            // extract mesh (vertex, index, and colors)
            string meshContents = GetTagContent( new Regex( @"Mesh[\s]?([\w\d_]+)?[\s]+{" ), partData );
            if( meshContents.Length > 0 )
                LoadMesh( ref part, meshContents );

            return part;
        }

        /// <summary>
        /// Extracts the data part from a construct that looks like:
        ///   tag { data }
        /// </summary>
        /// <param name="searchTag">A regex that specifies the search pattern. Needs to end in '{'</param>
        /// <param name="data">the string to search through for data enclosed within braces.</param>
        /// <returns></returns>
        private string GetTagContent( Regex searchTag, string data )
        {
            if( searchTag.ToString( ).EndsWith( "{" ) == false )
                throw new ArgumentException( "Search tag must end with '{'" );

            Match match = searchTag.Match( data );
            if( !match.Success )
                return "";
            else
                return GetCurlyBraceContent( data, match.Index + match.Length - 1 );
        }

        Regex findArrayCount = new Regex( @"([\d]+);" );
        Regex findVector4F = new Regex( @"([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);" );
        Regex findVector3F = new Regex( @"([-\d]+\.[\d]+);([-\d]+\.[\d]+);([-\d]+\.[\d]+);" );
        Regex findVector2F = new Regex( @"([-\d]+\.[\d]+);([-\d]+\.[\d]+);" );
        Regex findScalarF = new Regex( @"([-\d]+\.[\d]+);" );


        /// <summary>
        /// Loads the first material for a mesh
        /// </summary>
        /// <param name="meshMAterialData"></param>
        /// <returns></returns>
        List<MaterialSpecification> LoadMeshMaterialList( string meshMaterialListData )
        {
            Regex findMaterial = new Regex( @"Material[\s]+{" );
            MatchCollection materials = findMaterial.Matches( meshMaterialListData );
            if( materials.Count == 0 )
                return null;

            List<MaterialSpecification> materialList = new List<MaterialSpecification>( );
            foreach( Match material in materials )
            {
                string materialContent = GetCurlyBraceContent( meshMaterialListData, material.Index + material.Length - 1 );
                materialList.Add( LoadMeshMaterial( materialContent ) );
            }

            return materialList;
        }

        /// <summary>
        /// Loads a MeshMaterial subresource
        /// </summary>
        /// <param name="materialData"></param>
        /// <returns></returns>
        MaterialSpecification LoadMeshMaterial( string materialData )
        {
            MaterialSpecification m = new MaterialSpecification( );
            int dataOffset = 0;
            Match color = findVector4F.Match( materialData, dataOffset );
            if( !color.Success )
                throw new System.IO.InvalidDataException( "problem reading material color" );
            m.materialColor.X = float.Parse(color.Groups[1].ToString(), CultureInfo.InvariantCulture);
            m.materialColor.Y = float.Parse(color.Groups[2].ToString(), CultureInfo.InvariantCulture);
            m.materialColor.Z = float.Parse(color.Groups[3].ToString(), CultureInfo.InvariantCulture);
            m.materialColor.W = float.Parse(color.Groups[4].ToString(), CultureInfo.InvariantCulture);
            dataOffset = color.Index + color.Length;

            Match power = findScalarF.Match( materialData, dataOffset );
            if( !power.Success )
                throw new System.IO.InvalidDataException( "problem reading material specular color exponent" );
            m.specularPower = float.Parse(power.Groups[1].ToString(), CultureInfo.InvariantCulture);
            dataOffset = power.Index + power.Length;

            Match specular = findVector3F.Match( materialData, dataOffset );
            if( !specular.Success )
                throw new System.IO.InvalidDataException( "problem reading material specular color" );
            m.specularColor.X = float.Parse(specular.Groups[1].ToString(), CultureInfo.InvariantCulture);
            m.specularColor.Y = float.Parse(specular.Groups[2].ToString(), CultureInfo.InvariantCulture);
            m.specularColor.Z = float.Parse(specular.Groups[3].ToString(), CultureInfo.InvariantCulture);
            dataOffset = specular.Index + specular.Length;

            Match emissive = findVector3F.Match( materialData, dataOffset );
            if( !emissive.Success )
                throw new System.IO.InvalidDataException( "problem reading material emissive color" );
            m.emissiveColor.X = float.Parse(emissive.Groups[1].ToString(), CultureInfo.InvariantCulture);
            m.emissiveColor.Y = float.Parse(emissive.Groups[2].ToString(), CultureInfo.InvariantCulture);
            m.emissiveColor.Z = float.Parse(emissive.Groups[3].ToString(), CultureInfo.InvariantCulture);
            dataOffset = emissive.Index + emissive.Length;

            Regex findTextureFile = new Regex( @"TextureFilename[\s]+{" ); 
            Match textureFile = findTextureFile.Match( materialData, dataOffset );
            if( textureFile.Success )
            {
                string materialFilenameContent = GetCurlyBraceContent( materialData, textureFile.Index + textureFile.Length - 1 );
                Regex findFilename = new Regex( @"[\s]+""([\\\w\.]+)"";" );
                Match filename = findFilename.Match( materialFilenameContent );
                if( !filename.Success )
                    throw new System.IO.InvalidDataException( "problem reading texture filename" );
                m.textureFileName = filename.Groups[ 1 ].ToString( );
            }

            return m;
        }

        internal class IndexedMeshNormals
        {
            public List<Vector4F> normalVectors;
            public List<Int32> normalIndexMap;
        }

        /// <summary>
        /// Loads the indexed normal vectors for a mesh
        /// </summary>
        /// <param name="meshNormalData"></param>
        /// <returns></returns>
        IndexedMeshNormals LoadMeshNormals( string meshNormalData )
        {
            IndexedMeshNormals indexedMeshNormals = new IndexedMeshNormals( );

            Match normalCount = findArrayCount.Match( meshNormalData );
            if( !normalCount.Success )
                throw new System.IO.InvalidDataException( "problem reading mesh normals count" );

            indexedMeshNormals.normalVectors = new List<Vector4F>( );
            int normals = int.Parse(normalCount.Groups[1].Value, CultureInfo.InvariantCulture);
            int dataOffset = normalCount.Index + normalCount.Length;
            for( int normalIndex = 0; normalIndex < normals; normalIndex++ )
            {
                Match normal = findVector3F.Match( meshNormalData, dataOffset );
                if( !normal.Success )
                    throw new System.IO.InvalidDataException( "problem reading mesh normal vector" );
                else
                    dataOffset = normal.Index + normal.Length;

                indexedMeshNormals.normalVectors.Add( 
                    new Vector4F(
                        float.Parse(normal.Groups[1].Value, CultureInfo.InvariantCulture),
                        float.Parse(normal.Groups[2].Value, CultureInfo.InvariantCulture),
                        float.Parse(normal.Groups[3].Value, CultureInfo.InvariantCulture),
                        1.0f) );
            }

            Match faceNormalCount = findArrayCount.Match( meshNormalData, dataOffset );
            if( !faceNormalCount.Success )
                throw new System.IO.InvalidDataException( "problem reading mesh normals count" );
            
            indexedMeshNormals.normalIndexMap = new List<Int32>();
            int faceCount = int.Parse(faceNormalCount.Groups[1].Value, CultureInfo.InvariantCulture);
            dataOffset = faceNormalCount.Index + faceNormalCount.Length;
            for( int faceNormalIndex = 0; faceNormalIndex < faceCount; faceNormalIndex++ )
            {
                Match normalFace = findVertexIndex.Match( meshNormalData, dataOffset );
                if( !normalFace.Success )
                    throw new System.IO.InvalidDataException( "problem reading mesh normal face" );
                else
                    dataOffset = normalFace.Index + normalFace.Length;

                string[ ] vertexIndexes = normalFace.Groups[ 2 ].Value.Split( new char[ ] { ',' } );

                for( int n = 0; n <= vertexIndexes.Length - 3; n ++ )
                {
                    indexedMeshNormals.normalIndexMap.Add(int.Parse(vertexIndexes[0], CultureInfo.InvariantCulture));
                    indexedMeshNormals.normalIndexMap.Add(int.Parse(vertexIndexes[1 + n], CultureInfo.InvariantCulture));
                    indexedMeshNormals.normalIndexMap.Add(int.Parse(vertexIndexes[2 + n], CultureInfo.InvariantCulture));
                }
            }

            return indexedMeshNormals;
        }

        /// <summary>
        /// Loads the per vertex color for a mesh
        /// </summary>
        /// <param name="vertexColorData"></param>
        /// <returns></returns>
        Dictionary<int, Vector4F> LoadMeshColors( string vertexColorData )
        {
            Regex findVertexColor = new Regex( @"([\d]+); ([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);([\d]+\.[\d]+);;" );

            Match vertexCount = findArrayCount.Match( vertexColorData );
            if( !vertexCount.Success )
                throw new System.IO.InvalidDataException( "problem reading vertex colors count" );

            Dictionary<int, Vector4F> colorDictionary = new Dictionary<int,Vector4F>( );
            int verticies = int.Parse(vertexCount.Groups[1].Value, CultureInfo.InvariantCulture);
            int dataOffset = vertexCount.Index + vertexCount.Length;
            for( int vertexIndex = 0; vertexIndex < verticies; vertexIndex++ )
            {
                Match vertexColor = findVertexColor.Match( vertexColorData, dataOffset );
                if( !vertexColor.Success )
                    throw new System.IO.InvalidDataException( "problem reading vertex colors" );
                else
                    dataOffset = vertexColor.Index + vertexColor.Length;

                colorDictionary[int.Parse(vertexColor.Groups[1].Value, CultureInfo.InvariantCulture)] =
                    new Vector4F(
                        float.Parse(vertexColor.Groups[2].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertexColor.Groups[3].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertexColor.Groups[4].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertexColor.Groups[5].Value, CultureInfo.InvariantCulture));
            }

            return colorDictionary;
        }

        /// <summary>
        /// Loads the texture coordinates(U,V) for a mesh
        /// </summary>
        /// <param name="textureCoordinateData"></param>
        /// <returns></returns>
        List<Vector2F> LoadMeshTextureCoordinates( string textureCoordinateData )
        {
            Match coordinateCount = findArrayCount.Match( textureCoordinateData );
            if( !coordinateCount.Success )
                throw new System.IO.InvalidDataException( "problem reading mesh texture coordinates count" );

            List<Vector2F> textureCoordinates = new List<Vector2F>( );
            int coordinates = int.Parse(coordinateCount.Groups[1].Value, CultureInfo.InvariantCulture);
            int dataOffset = coordinateCount.Index + coordinateCount.Length;
            for( int coordinateIndex = 0; coordinateIndex < coordinates; coordinateIndex++ )
            {
                Match coordinate = findVector2F.Match( textureCoordinateData, dataOffset );
                if( !coordinate.Success )
                    throw new System.IO.InvalidDataException( "problem reading texture coordinate count" );
                else
                    dataOffset = coordinate.Index + coordinate.Length;

                textureCoordinates.Add(
                    new Vector2F(
                        float.Parse(coordinate.Groups[1].Value, CultureInfo.InvariantCulture),
                        float.Parse(coordinate.Groups[2].Value, CultureInfo.InvariantCulture)));
            }

            return textureCoordinates;
        }

        Regex findVertexIndex = new Regex( @"([\d]+);[\s]*([\d,]+)?;" );

        /// <summary>
        /// Loads a mesh and creates the vertex/index buffers for the part
        /// </summary>
        /// <param name="part"></param>
        /// <param name="meshData"></param>
        void LoadMesh( ref Part part, string meshData )
        {
            // load vertex data
            int dataOffset = 0;
            Match vertexCount = findArrayCount.Match( meshData );
            if( !vertexCount.Success )
                throw new System.IO.InvalidDataException( "problem reading vertex count" );

            List<Vector4F> vertexList = new List<Vector4F>();
            int verticies = int.Parse(vertexCount.Groups[1].Value, CultureInfo.InvariantCulture);
            dataOffset = vertexCount.Index + vertexCount.Length;
            for( int vertexIndex = 0; vertexIndex < verticies; vertexIndex++ )
            {
                Match vertex = findVector3F.Match( meshData, dataOffset );
                if( !vertex.Success )
                    throw new System.IO.InvalidDataException( "problem reading vertex" );
                else
                    dataOffset = vertex.Index + vertex.Length;

                vertexList.Add(
                    new Vector4F(
                        float.Parse(vertex.Groups[1].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertex.Groups[2].Value, CultureInfo.InvariantCulture),
                        float.Parse(vertex.Groups[3].Value, CultureInfo.InvariantCulture),
                        1.0f) );
            }

            // load triangle index data
            Match triangleIndexCount = findArrayCount.Match( meshData, dataOffset );
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length;
            if( !triangleIndexCount.Success )
                throw new System.IO.InvalidDataException( "problem reading index count" );

            List<Int32> triangleIndiciesList = new List<Int32>( );
            int triangleIndexListCount = int.Parse(triangleIndexCount.Groups[1].Value, CultureInfo.InvariantCulture);
            dataOffset = triangleIndexCount.Index + triangleIndexCount.Length;
            for( int triangleIndicyIndex = 0; triangleIndicyIndex < triangleIndexListCount; triangleIndicyIndex++ )
            {
                Match indexEntry = findVertexIndex.Match( meshData, dataOffset );
                if( !indexEntry.Success )
                    throw new System.IO.InvalidDataException( "problem reading vertex index entry" );
                else
                    dataOffset = indexEntry.Index + indexEntry.Length;

                int indexEntryCount = int.Parse(indexEntry.Groups[1].Value, CultureInfo.InvariantCulture);
                string[ ] vertexIndexes = indexEntry.Groups[ 2 ].Value.Split( new char[ ] { ',' } );
                if( indexEntryCount != vertexIndexes.Length )
                    throw new System.IO.InvalidDataException( "vertex index count does not equal count of indicies found" );

                for( int entryIndex = 0; entryIndex <= indexEntryCount - 3; entryIndex++ )
                {
                    triangleIndiciesList.Add(int.Parse(vertexIndexes[0], CultureInfo.InvariantCulture));
                    triangleIndiciesList.Add(int.Parse(vertexIndexes[1 + entryIndex].ToString(), CultureInfo.InvariantCulture));
                    triangleIndiciesList.Add(int.Parse(vertexIndexes[2 + entryIndex].ToString(), CultureInfo.InvariantCulture));
                }
            }

            // load mesh colors
            string vertexColorData = GetTagContent( new Regex( @"MeshVertexColors[\s]+{" ), meshData );
            Dictionary<int,Vector4F> colorDictionary = null;
            if( vertexColorData != "" )
                colorDictionary = LoadMeshColors( vertexColorData );

            // load mesh normals
            string meshNormalData = GetTagContent( new Regex( @"MeshNormals[\s]+{" ), meshData );
            IndexedMeshNormals meshNormals = null;
            if( meshNormalData != "" )
            {
                meshNormals = LoadMeshNormals( meshNormalData );
            }

            // load mesh texture coordinates
            string meshTextureCoordsData = GetTagContent( new Regex( @"MeshTextureCoords[\s]+{" ), meshData );
            List<Vector2F> meshTextureCoords = null;
            if( meshTextureCoordsData != "" )
            {
                meshTextureCoords = LoadMeshTextureCoordinates( meshTextureCoordsData );
            }

            // load mesh material
            string meshMaterialsData = GetTagContent( new Regex( @"MeshMaterialList[\s]+{" ), meshData );
            List<MaterialSpecification> meshMaterials = null;
            if( meshMaterialsData != "" )
            {
                meshMaterials = LoadMeshMaterialList( meshMaterialsData );
            }
            
            // copy vertex data to HGLOBAL
            int byteLength = Marshal.SizeOf( typeof( XMeshVertex ) ) * triangleIndiciesList.Count;
            IntPtr nativeVertex = Marshal.AllocHGlobal( byteLength );
            byte[ ] byteBuffer = new byte[ byteLength ];
            XMeshVertex[ ] varray = new XMeshVertex[ triangleIndiciesList.Count ];
            for( int n = 0; n < triangleIndiciesList.Count; n++ )
            {
                XMeshVertex vertex = new XMeshVertex( )
                {
                    Vertex = vertexList[ triangleIndiciesList[ n ] ],
                    Normal = (meshNormals == null) ? new Vector4F( 0, 0, 0, 1.0f ) : meshNormals.normalVectors[ meshNormals.normalIndexMap[ n ] ],
                    Color = ((colorDictionary == null) ? new Vector4F( 0, 0, 0, 0 ) : colorDictionary[ triangleIndiciesList[ n ] ]),
                    Texture = ((meshTextureCoords == null) ? new Vector2F( 0, 0 ) : meshTextureCoords[ triangleIndiciesList[ n ] ])
                };
                byte[ ] vertexData = RawSerialize( vertex );
                Buffer.BlockCopy( vertexData, 0, byteBuffer, vertexData.Length * n, vertexData.Length );
            }
            Marshal.Copy( byteBuffer, 0, nativeVertex, byteLength );

            // build vertex buffer
            BufferDescription bdv = new BufferDescription( )
            {
                Usage = Usage.Default,
                ByteWidth = (uint)(Marshal.SizeOf( typeof( XMeshVertex ) ) * triangleIndiciesList.Count),
                BindFlags = BindFlag.VertexBuffer,
                CpuAccessFlags = 0,
                MiscFlags = 0
            };
            SubresourceData vertexInit = new SubresourceData( )
            {
                SysMem = nativeVertex
            };

            part.vertexBuffer = device.CreateBuffer( bdv, vertexInit );
            Debug.Assert( part.vertexBuffer != null );


            part.vertexCount = triangleIndiciesList.Count;

            if( meshMaterials != null )
            {
                // only a single material is currently supported
                MaterialSpecification m = meshMaterials[ 0 ];

                part.material = new Material()
                {
                    emissiveColor = m.emissiveColor,
                    specularColor = m.specularColor,
                    materialColor = m.materialColor,
                    specularPower = m.specularPower
                };
                
                string texturePath = "";
                if( File.Exists( m.textureFileName ) )
                    texturePath = m.textureFileName;
                if( File.Exists( meshDirectory + "\\" + m.textureFileName ) )
                    texturePath = meshDirectory + "\\" + m.textureFileName;
                if( File.Exists( meshDirectory + "\\..\\" + m.textureFileName ) )
                    texturePath = meshDirectory + "\\..\\" + m.textureFileName;

                if( texturePath.Length == 0 )
                {
                    part.material.textureResource = null;
                }
                else
                {
                    part.material.textureResource =
                        D3D10XHelpers.CreateShaderResourceViewFromFile(
                            device,
                            texturePath );
                }
            }
            Marshal.FreeHGlobal( nativeVertex );
        }

        /// <summary>
        /// Copys an arbitrary strcuture into a byte array
        /// </summary>
        /// <param name="anything"></param>
        /// <returns></returns>
        public byte[ ] RawSerialize( object anything )
        {
            int rawsize = Marshal.SizeOf( anything );
            IntPtr buffer = Marshal.AllocHGlobal( rawsize );
            Marshal.StructureToPtr( anything, buffer, false );
            byte[ ] rawdatas = new byte[ rawsize ];
            Marshal.Copy( buffer, rawdatas, 0, rawsize );
            Marshal.FreeHGlobal( buffer );
            return rawdatas;
        } 
    }
}

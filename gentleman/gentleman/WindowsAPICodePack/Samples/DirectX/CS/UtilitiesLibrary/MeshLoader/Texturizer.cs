// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities;
using Microsoft.WindowsAPICodePack.DirectX;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;
using System.Windows.Media.Media3D;


namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    /// <summary>
    /// A Mesh that allow for changing textures within the scene
    /// </summary>
    public class Texturizer : XMesh
    {
        /// <summary>
        /// If trus shows one one texture at a time
        /// </summary>
        public bool ShowOneTexture
        {
            get
            {
                return showOneTexture;
            }
            set
            {
                showOneTexture = value;
            }
        }
        bool showOneTexture = true;

        /// <summary>
        /// This method sets which part to texture during rendering.
        /// </summary>
        /// <param name="partName"></param>
        public void PartToTexture( string partName )
        {
            partEmphasis = partName;
        }
        private string partEmphasis;
        
        /// <summary>
        /// Clears the alternate texture list (restoring the model's textures)
        /// </summary>
        public void RevertTextures()
        {
            alternateTextures.Clear();
        }

        /// <summary>
        /// Gets a list of the names of the parts in the mesh
        /// </summary>
        /// <returns></returns>
        public List<string> GetParts()
        {
            List<string> partNames = new List<string>( );
            foreach( Part part in scene.parts )
                partNames.Add( part.name );
            return partNames;
        }

        
        /// <summary>
        /// Caretes an alternate texture for a part
        /// </summary>
        /// <param name="partName">The name of the part to create the texture for.</param>
        /// <param name="imagePath">The path to the image to be use for the texture.</param>
        public void SwapTexture(string partName, string imagePath)
        {
            if (partName != null)
            {
                if (File.Exists(imagePath))
                {
                    FileStream stream = File.OpenRead(imagePath);

                    try
                    {
                        ShaderResourceView srv = TextureLoader.LoadTexture( this.manager.device, stream );
                        if( srv != null )
                            alternateTextures[ partName ] = srv;
                    }
                    catch( COMException )
                    {
                        System.Windows.MessageBox.Show( "Not a valid image." );
                    }

                }
                else
                {
                    alternateTextures[partName] = null;
                }
            }
        }
        Dictionary<string, ShaderResourceView> alternateTextures = new Dictionary<string, ShaderResourceView>( );

        /// <summary>
        /// Renders the mesh with the specified transformation. This render overrides the base class rendering
        /// to provide part-by-part texturing support.
        /// </summary>
        /// <param name="modelTransform"></param>
        public void Render(Matrix3D modelTransform)
        {
            // setup rasterization
            RasterizerDescription rDescription = new RasterizerDescription()
            {
                FillMode = ShowWireFrame ? FillMode.Wireframe : FillMode.Solid,
                CullMode = CullMode.Back,
                FrontCounterClockwise = false,
                DepthBias = 0,
                DepthBiasClamp = 0,
                SlopeScaledDepthBias = 0,
                DepthClipEnable = true,
                ScissorEnable = false,
                MultisampleEnable = true,
                AntialiasedLineEnable = true,
            };
            RasterizerState rState = this.manager.device.CreateRasterizerState(rDescription);

            this.manager.device.RS.SetState(rState);

            this.manager.brightnessVariable.FloatValue = this.LightIntensity;

            // start rendering
            foreach (Part part in scene.parts)
            {
                if(showOneTexture && (part.name != partEmphasis))
                {
                    rDescription.FillMode = FillMode.Wireframe;
                    RasterizerState state = this.manager.device.CreateRasterizerState(rDescription);
                    this.manager.device.RS.SetState(state);
                }
                else
                {
                    rDescription.FillMode = FillMode.Solid;
                    RasterizerState state = this.manager.device.CreateRasterizerState(rDescription);
                    this.manager.device.RS.SetState(state);
                }

                EffectTechnique technique = null;
                if (part.material == null)
                {
                    technique = this.manager.techniqueRenderVertexColor;
                }
                else
                {
                    if (part.material.textureResource != null)
                    {
                        technique = this.manager.techniqueRenderTexture;

                        ShaderResourceView texture = part.material.textureResource;
                        if (alternateTextures.ContainsKey(part.name))
                            texture = alternateTextures[part.name];

                        this.manager.diffuseVariable.SetResource(texture);
                    }
                    else
                    {
                        technique = this.manager.techniqueRenderMaterialColor;
                        this.manager.materialColorVariable.FloatVector = part.material.materialColor;
                    }
                }

                // set part transform
                Transform3DGroup partGroup = new Transform3DGroup();
                partGroup.Children.Add(
                    new MatrixTransform3D(PartAnimation(part.name)));
                partGroup.Children.Add(
                    new MatrixTransform3D( part.partTransform.ToMatrix3D() ));
                partGroup.Children.Add(
                    new MatrixTransform3D( scene.sceneTransform.ToMatrix3D() ) );
                partGroup.Children.Add(
                    new MatrixTransform3D(modelTransform));

                this.manager.worldVariable.Matrix =  partGroup.Value.ToMatrix4x4F();

                if (part.vertexBuffer != null)
                {
                    //  set up vertex buffer and index buffer
                    uint stride = (uint)Marshal.SizeOf(typeof(XMeshVertex));
                    uint offset = 0;
                    this.manager.device.IA.SetVertexBuffers(0, new D3DBuffer[] { part.vertexBuffer }, new uint[] { stride }, new uint[] { offset });

                    // Set primitive topology
                    this.manager.device.IA.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

                    TechniqueDescription techDesc = technique.Description;
                    for (uint p = 0; p < techDesc.Passes; ++p)
                    {
                        technique.GetPassByIndex(p).Apply();
                        PassDescription passDescription = technique.GetPassByIndex(p).Description;

                        // set vertex layout
                        this.manager.device.IA.SetInputLayout(
                            this.manager.device.CreateInputLayout(
                                part.dataDescription,
                                passDescription.InputAssemblerInputSignature,
                                passDescription.InputAssemblerInputSignatureSize));

                        // draw part
                        this.manager.device.Draw((uint)part.vertexCount, 0);
                    }
                }
            }
        }

    }
}

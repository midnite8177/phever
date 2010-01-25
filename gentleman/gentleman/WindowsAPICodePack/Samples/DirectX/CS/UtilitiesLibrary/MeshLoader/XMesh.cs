// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.WindowsAPICodePack.DirectX;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D;
using Microsoft.WindowsAPICodePack.DirectX.Direct3D10;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;

namespace Microsoft.WindowsAPICodePack.DirectX.DirectXUtilities
{
    /// <summary>
    /// 
    /// </summary>
    public class XMesh : IDisposable
    {
        RasterizerDescription rDescription = new RasterizerDescription()
        {
            FillMode = FillMode.Solid,
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

        #region public methods
        /// <summary>
        /// Renders the mesh with the specified transformation
        /// </summary>
        /// <param name="modelTransform"></param>
        public void Render( Matrix4x4F modelTransform )
        {
            rDescription.FillMode = wireFrame ? FillMode.Wireframe : FillMode.Solid;
            // setup rasterization
            using (RasterizerState rState = this.manager.device.CreateRasterizerState(rDescription))
            {
                this.manager.device.RS.SetState(rState);
                this.manager.brightnessVariable.FloatValue = this.lightIntensity;

                // start rendering
                foreach (Part part in scene.parts)
                {
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
                            this.manager.diffuseVariable.SetResource(part.material.textureResource);
                        }
                        else
                        {
                            technique = this.manager.techniqueRenderMaterialColor;
                            this.manager.materialColorVariable.FloatVector = part.material.materialColor;
                        }
                    }

                    // set part transform
                    Transform3DGroup partGroup = new Transform3DGroup();
                    partGroup.Children.Add(new MatrixTransform3D(PartAnimation(part.name)));
                    partGroup.Children.Add(new MatrixTransform3D(part.partTransform.ToMatrix3D()));
                    partGroup.Children.Add(new MatrixTransform3D(scene.sceneTransform.ToMatrix3D()));
                    partGroup.Children.Add(new MatrixTransform3D(modelTransform.ToMatrix3D()));
                    this.manager.worldVariable.Matrix = partGroup.Value.ToMatrix4x4F();

                    if (part.vertexBuffer != null)
                    {
                        //set up vertex buffer and index buffer
                        uint stride = (uint)Marshal.SizeOf(typeof(XMeshVertex));
                        uint offset = 0;
                        this.manager.device.IA.SetVertexBuffers(0, new D3DBuffer[] { part.vertexBuffer }, new uint[] { stride }, new uint[] { offset });

                        //Set primitive topology
                        this.manager.device.IA.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

                        TechniqueDescription techDesc = technique.Description;
                        for (uint p = 0; p < techDesc.Passes; ++p)
                        {
                            technique.GetPassByIndex(p).Apply();
                            PassDescription passDescription = technique.GetPassByIndex(p).Description;

                            using (InputLayout inputLayout = this.manager.device.CreateInputLayout(
                                    part.dataDescription,
                                    passDescription.InputAssemblerInputSignature,
                                    passDescription.InputAssemblerInputSignatureSize))
                            {
                                // set vertex layout
                                this.manager.device.IA.SetInputLayout(inputLayout);

                                // draw part
                                this.manager.device.Draw((uint)part.vertexCount, 0);
                                this.manager.device.IA.SetInputLayout(null);
                            }
                        }
                    }
                }
                this.manager.device.RS.SetState(null);
            }
        }
        #endregion

        #region public properties
        /// <summary>
        /// Displays the unshaded wireframe if true
        /// </summary>
        public bool ShowWireFrame
        {
            get { return wireFrame; }
            set { wireFrame = value; }
        }
        private bool wireFrame = false;

        /// <summary>
        /// Sets the intensity of the light used in rendering.
        /// </summary>
        public float LightIntensity
        {
            get { return lightIntensity; }
            set { lightIntensity = value; }
        }
        private float lightIntensity = 1.0f;
        #endregion

        #region virtual methods
        protected virtual Matrix3D PartAnimation( string partName )
        {
            return Matrix3D.Identity;
        }
        #endregion

        #region implementation
        internal XMesh()
        {
        }
        
        internal void Load( string path, XMeshManager manager )
        {
            this.manager = manager;
            XMeshTextLoader loader = new XMeshTextLoader( this.manager.device );
            scene = loader.XMeshFromFile( path );
        }

        /// <summary>
        /// The scene as loaded by XMeshTextLoader
        /// </summary>
        internal Scene scene;

        /// <summary>
        /// The object that manages the XMeshes
        /// </summary>
        internal XMeshManager manager;

        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed;
        /// <summary>
        /// Releases resources no longer needed.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed && scene.parts != null)
            {
                disposed = true;
                for (int i = 0; i < scene.parts.Count; i++)
                {
                    Part part = scene.parts[i];
                    if(part.vertexBuffer != null)
                    {
                        part.vertexBuffer.Dispose();
                        part.vertexBuffer = null;
                    }
                    if((part.material != null) && (part.material.textureResource != null))
                    {
                        part.material.textureResource.Dispose();
                        part.material.textureResource = null;
                    }
                }
                scene.parts.Clear();
                scene.parts = null;
            }
        }
        #endregion
    }
}

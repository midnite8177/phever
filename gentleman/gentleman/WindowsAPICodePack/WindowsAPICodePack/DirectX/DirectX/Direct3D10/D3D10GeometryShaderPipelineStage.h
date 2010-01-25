//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D10PipelineStage.h"

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D10 {

/// <summary>
/// Geometry Shader pipeline stage. 
/// </summary>
public ref class GeometryShaderPipelineStage : PipelineStage
{
public:
    /// <summary>
    /// Get the constant buffers used by the geometry shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSGetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin retrieving constant buffers from (ranges from 0 to D3D10_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="numBuffers">Number of buffers to retrieve (ranges from 0 to D3D10_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - startSlot).</param>
    /// <returns>A collection of constant buffer objects (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> to be returned by the method.</returns>
    ReadOnlyCollection<D3DBuffer^>^ GetConstantBuffers(UInt32 startSlot, UInt32 numBuffers);

    /// <summary>
    /// Get an array of sampler state objects from the geometry shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSGetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into a zero-based array to begin getting samplers from (ranges from 0 to D3D10_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="numSamplers">Number of samplers to get from a device context. Each pipeline stage has a total of 16 sampler slots available (ranges from 0 to D3D10_COMMONSHADER_SAMPLER_SLOT_COUNT - startSlot).</param>
    /// <returns>A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>.</returns>
    ReadOnlyCollection<SamplerState^>^ GetSamplers(UInt32 startSlot, UInt32 numSamplers);

    /// <summary>
    /// Get the geometry shader currently set on the device.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSGetShader)</para>
    /// </summary>
    /// <returns>A geometry shader (see <see cref="GeometryShader"/>)<seealso cref="GeometryShader"/> to be returned by the method.</returns>
    GeometryShader^ GetShader();

    /// <summary>
    /// Get the geometry shader resources.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSGetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin getting shader resources from (ranges from 0 to D3D10_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="numViews">The number of resources to request from the device. Up to a maximum of 128 slots are available for shader resources (ranges from 0 to D3D10_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - startSlot).</param>
    /// <returns>Collection of shader resource view objects to be returned by the device.</returns>
    ReadOnlyCollection<ShaderResourceView^>^ GetShaderResources(UInt32 startSlot, UInt32 numViews);

    /// <summary>
    /// Set the constant buffers used by the geometry shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSSetConstantBuffers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting constant buffers to (ranges from 0 to D3D10_COMMONSHADER_CONSTANT_BUFFER_API_SLOT_COUNT - 1).</param>
    /// <param name="constantBuffers">Collection of constant buffers (see <see cref="D3DBuffer"/>)<seealso cref="D3DBuffer"/> being given to the device.</param>
    void SetConstantBuffers(UInt32 startSlot, IEnumerable<D3DBuffer^>^ constantBuffers);

    /// <summary>
    /// Set an array of sampler states to the geometry shader pipeline stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSSetSamplers)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting samplers to (ranges from 0 to D3D10_COMMONSHADER_SAMPLER_SLOT_COUNT - 1).</param>
    /// <param name="samplers">A collection of sampler-state objects (see <see cref="SamplerState"/>)<seealso cref="SamplerState"/>. See Remarks.</param>
    void SetSamplers(UInt32 startSlot, IEnumerable<SamplerState^>^ samplers);

    /// <summary>
    /// Set a geometry shader to the device.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSSetShader)</para>
    /// </summary>
    /// <param name="geometryShader">A geometry shader (see <see cref="GeometryShader"/>)<seealso cref="GeometryShader"/>. Passing in NULL disables the shader for this pipeline stage.</param>
    void SetShader(GeometryShader^ geometryShader);

    /// <summary>
    /// Bind an array of shader resources to the geometry shader stage.
    /// <para>(Also see DirectX SDK: ID3D10Device::GSSetShaderResources)</para>
    /// </summary>
    /// <param name="startSlot">Index into the device's zero-based array to begin setting shader resources to (ranges from 0 to D3D10_COMMONSHADER_INPUT_RESOURCE_SLOT_COUNT - 1).</param>
    /// <param name="shaderResourceViews">Collection of shader resource view objects to set to the device.</param>
    void SetShaderResources(UInt32 startSlot, IEnumerable<ShaderResourceView^>^ shaderResourceViews);
internal:
    GeometryShaderPipelineStage() {}
internal:
    GeometryShaderPipelineStage(D3DDevice^ parent) : PipelineStage(parent)
    {
    }
};
} } } }

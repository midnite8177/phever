//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11PipelineStage.h"

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

/// <summary>
/// Rasterizer pipeline stage. 
/// </summary>
public ref class RasterizerPipelineStage : PipelineStage
{
public:

    /// <summary>
    /// Get the collection of scissor rectangles bound to the rasterizer stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::RSGetScissorRects)</para>
    /// </summary>
    /// <returns>A collection of scissor rectangles (see <see cref="D3dRect"/>)<seealso cref="D3dRect"/>.
    /// If maximumNumberOfRects is greater than the number of scissor rects currently bound, 
    /// then the collection will be trimmed to contain only the currently bound. 
    /// </returns>
    ReadOnlyCollection<D3dRect>^ GetScissorRects(UInt32 maxNumberOfRects);

    /// <summary>
    /// Get the rasterizer state from the rasterizer stage of the pipeline.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::RSGetState)</para>
    /// </summary>
    /// <returns>A rasterizer-state interface (see <see cref="RasterizerState"/>)<seealso cref="RasterizerState"/> to fill with information from the device.</returns>
    RasterizerState^ GetState();

    /// <summary>
    /// Get the array of viewports bound to the rasterizer stage
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::RSGetViewports)</para>
    /// </summary>
    /// <param name="maxNumberOfViewports">The input specifies the maximum number of viewports (ranges from 0 to D3D11_VIEWPORT_AND_SCISSORRECT_OBJECT_COUNT_PER_PIPELINE) to retrieve, the output contains the actual number of viewports returned.</param>
    /// <returns>A collection of Viewport structures that are bound to the device. 
    /// If the number of viewports (in maxNumberOfViewports) is greater than the actual number of viewports currently bound, 
    /// then the collection will be trimmed to contain only the currently bound. 
    /// See the structure page for details about how the viewport size is dependent on the device feature level 
    /// which has changed between Direct3D 11 and Direct3D 10.</returns>
    ReadOnlyCollection<Viewport>^ GetViewports(UInt32 maxNumberOfViewports);

    /// <summary>
    /// Bind an array of scissor rectangles to the rasterizer stage.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::RSSetScissorRects)</para>
    /// </summary>
    /// <param name="rects">A collection of scissor rectangles (see <see cref="D3dRect"/>)<seealso cref="D3dRect"/>.</param>
    void SetScissorRects(IEnumerable<D3dRect>^ rects);

    /// <summary>
    /// Set the rasterizer state for the rasterizer stage of the pipeline.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::RSSetState)</para>
    /// </summary>
    /// <param name="rasterizerState">A rasterizer-state interface (see <see cref="RasterizerState"/>)<seealso cref="RasterizerState"/> to bind to the pipeline.</param>
    void SetState(RasterizerState^ rasterizerState);

    /// <summary>
    /// Bind an array of viewports to the rasterizer stage of the pipeline.
    /// <para>(Also see DirectX SDK: ID3D11DeviceContext::RSSetViewports)</para>
    /// </summary>
    /// <param name="viewports">A collection of Viewport structures to bind to the device. 
    /// See the SDK for details about how the viewport size is dependent on the device feature 
    /// level which has changed between Direct3D 11 and Direct3D 10.</param>
    void SetViewports(IEnumerable<Viewport>^ viewports);

protected:
    RasterizerPipelineStage() {}
internal:
    RasterizerPipelineStage(DeviceContext^ parent) : PipelineStage(parent)
    {
    }
};
} } } }

// Copyright (c) Microsoft Corporation.  All rights reserved.

#include "stdafx.h"
#include "D3D10EffectPass.h"

#include "D3D10EffectVariable.h"
using namespace Microsoft::WindowsAPICodePack::DirectX::Direct3D10;
using namespace msclr::interop;

void EffectPass::Apply()
{
    CommonUtils::VerifyResult(GetInterface<ID3D10EffectPass>()->Apply(0));
}

StateBlockMask^ EffectPass::ComputeStateBlockMask()
{
    StateBlockMask^ blockMask = gcnew StateBlockMask();

    CommonUtils::VerifyResult(GetInterface<ID3D10EffectPass>()->ComputeStateBlockMask(blockMask->nativeObject.Get()));

    return blockMask;
}

EffectVariable^ EffectPass::GetAnnotationByIndex(UInt32 index)
{
    ID3D10EffectVariable* returnValue = GetInterface<ID3D10EffectPass>()->GetAnnotationByIndex(static_cast<UINT>(index));
    return gcnew EffectVariable(returnValue);
}

EffectVariable^ EffectPass::GetAnnotationByName(String^ name)
{
    IntPtr ptr = Marshal::StringToHGlobalAnsi(name);

    try
    {
        ID3D10EffectVariable * returnValue = GetInterface<ID3D10EffectPass>()->GetAnnotationByName(static_cast<char*>(ptr.ToPointer()));

        return returnValue ? gcnew EffectVariable(returnValue, false) : nullptr;
    }
    finally
    {
        Marshal::FreeHGlobal(ptr);
    }
}

PassDescription EffectPass::Description::get()
{
    D3D10_PASS_DESC desc = {0};

    CommonUtils::VerifyResult(GetInterface<ID3D10EffectPass>()->GetDesc(&desc));
    
    return PassDescription(desc);
}

PassShaderDescription EffectPass::GeometryShaderDescription::get()
{
    D3D10_PASS_SHADER_DESC desc = {0};

    CommonUtils::VerifyResult(GetInterface<ID3D10EffectPass>()->GetGeometryShaderDesc(&desc));

    return PassShaderDescription(desc);
}

PassShaderDescription EffectPass::PixelShaderDescription::get()
{
    D3D10_PASS_SHADER_DESC desc = {0};

    CommonUtils::VerifyResult(GetInterface<ID3D10EffectPass>()->GetPixelShaderDesc(&desc));

    return PassShaderDescription(desc);
}

PassShaderDescription EffectPass::VertexShaderDescription::get()
{
    D3D10_PASS_SHADER_DESC desc = {0};

    CommonUtils::VerifyResult(GetInterface<ID3D10EffectPass>()->GetVertexShaderDesc(&desc));

    return PassShaderDescription(desc);
}

Boolean EffectPass::IsValid::get()
{
    return GetInterface<ID3D10EffectPass>()->IsValid() != 0;
}


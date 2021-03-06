//Copyright (c) Microsoft Corporation.  All rights reserved.

#pragma once
#include "D3D11DeviceChild.h"

namespace  Microsoft { namespace WindowsAPICodePack { namespace DirectX { namespace Direct3D11 {

using namespace System;

    /// <summary>
    /// A domain-shader class manages an executable program (a domain shader) that controls the domain-shader stage.
    /// <para>(Also see DirectX SDK: ID3D11DomainShader)</para>
    /// </summary>
    public ref class DomainShader :
        public Microsoft::WindowsAPICodePack::DirectX::Direct3D11::DeviceChild
    {
    public: 

    internal:
        DomainShader()
        {
        }
    internal:

        DomainShader(ID3D11DomainShader* pNativeID3D11DomainShader)
        {
            Attach(pNativeID3D11DomainShader);
        }


    };
} } } }

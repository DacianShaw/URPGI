# **Dynamic GI in unity URP**



## Introduction

This repository is a project that implements dynamic global illumination in Unity URP. It uses PRT to achieve dynamic global illumination for diffuse lighting and stochastic SSR(Screen space Relection)to achieve global illumination for specular lighting.

## General guidance 

### Files related to PRT

1. The definition of the light probe can be found in the "Assets/Scripts/Probes"
2. "Assets/Scripts/IrradianceVolume.cs" and "Assets/Scripts/IrradianceVolumeData.cs" is used to generate a group of light probes and the corresponding data structures for storing SH coefficients.
3. "Assets/Scripts/PRTRelight" and "Assets/Scripts/PRTBlend" are two render features used to insert Relighting pass and BlendPass to unity URP.
4. "Assets/Shaders/SampleSurfel.compute" and "Assets/Shaders/surfelRelightCS.compute" are used to generate surfels and compute the SH coefficients using Monte-Carlo integration.
5. "Assets/Shaders/SH.hlsl" is to define some helper functions to help relighting.
6. "Assets/Shaders/Blend.shader" is the shader to blend the direct light and global illumilation.

### Files related to Stochastic SSR(need to be further implemented):
1."Assets/Scripts/DepthNormalFeature.cs",""Assets/Scripts/SSR.cs".
2."Assets/Shaders/HizGenerator.Shader",”Assets/Shaders/SSReflection.shader".





#define FIXED_SCALE 100000.0

int Float2Int(float x)
{
    return int(x * FIXED_SCALE);
}
float Int2Float(int x)
{
    return float(x) / FIXED_SCALE;
}

float3 TrilinearInterpolation(in float3 value[8], float3 diff)
{
    //offset definition
    //    int3(0, 0, 0), int3(0, 0, 1), int3(0, 1, 0), int3(0, 1, 1), 
     //   int3(1, 0, 0), int3(1, 0, 1), int3(1, 1, 0), int3(1, 1, 1), 
    float3 v_04 = lerp(value[0], value[4], diff.x);  
    float3 v_26 = lerp(value[2], value[6], diff.x); 
    float3 v_15 = lerp(value[1], value[5], diff.x);   
    float3 v_37= lerp(value[3], value[7], diff.x);    
    float3 v_0426 = lerp(v_04, v_26, diff.y);
    float3 v_1537 = lerp(v_15, v_37, diff.y);
    float3 phi = lerp(v_0426, v_1537, diff.z); 
    return phi;
}


int3 computeProbeIndex(float3 worldPos, float4 _probeSize, float _probeGridSize, float4 _probeCorner)
{
    float3 probeIndexFloat = floor((worldPos.xyz - _probeCorner.xyz) / _probeGridSize);
    int3 probeIndex = int3(probeIndexFloat.x, probeIndexFloat.y, probeIndexFloat.z);
    return probeIndex;
}

int Index3D21D(int3 probeIndex3, float4 _probeSize)
{
    int probeIndex = probeIndex3.x * _probeSize.y * _probeSize.z + probeIndex3.y * _probeSize.z + probeIndex3.z;
    return probeIndex;
}

bool checkIndexOveride(int3 probeIndex3, float4 _probeSize)
{
    bool isInsideVoxelX = 0 <= probeIndex3.x && probeIndex3.x < _probeSize.x;
    bool isInsideVoxelY = 0 <= probeIndex3.y && probeIndex3.y < _probeSize.y;
    bool isInsideVoxelZ = 0 <= probeIndex3.z && probeIndex3.z < _probeSize.z;
    bool isInsideVoxel = isInsideVoxelX && isInsideVoxelY && isInsideVoxelZ;
    return isInsideVoxel;
}

float3 computeProbePosition(int3 probeIndex3, float _probeGridSize, float4 _probeCorner)
{
    float3 res = float3(probeIndex3.x, probeIndex3.y, probeIndex3.z) * _probeGridSize + _probeCorner.xyz;
    return res;
}
float SH(in int l, in int m, in float3 s) 
{ 
    #define k01 0.2820947918   
    #define k02 0.4886025119   
    #define k03 1.0925484306    
    #define k04 0.3153915652    
    #define k05 0.5462742153   

    float x = s.x;
    float y = s.z;
    float z = s.y;
	
    //zero order
    if( l==0 )          return  k01;
    // first order
	if( l==1 && m==-1 ) return  k02*y;
    if( l==1 && m== 0 ) return  k02*z;
    if( l==1 && m== 1 ) return  k02*x;
    //second oerder
	if( l==2 && m==-2 ) return  k03*x*y;
    if( l==2 && m==-1 ) return  k03*y*z;
    if( l==2 && m== 0 ) return  k04*(2.0*z*z-x*x-y*y);
    if( l==2 && m== 1 ) return  k03*x*z;
    if( l==2 && m== 2 ) return  k05*(x*x-y*y);

	return 0.0;
}
 
float3 ComputeSurfelIrradiance(in float3 c[9], in float3 dir)
{
    #define A0 3.1415
    #define A1 2.0943
    #define A2 0.7853

    float3 irradiance = float3(0, 0, 0);
    irradiance += SH(0,  0, dir) * c[0] * A0;
    irradiance += SH(1, -1, dir) * c[1] * A1;
    irradiance += SH(1,  0, dir) * c[2] * A1;
    irradiance += SH(1,  1, dir) * c[3] * A1;
    irradiance += SH(2, -2, dir) * c[4] * A2;
    irradiance += SH(2, -1, dir) * c[5] * A2;
    irradiance += SH(2,  0, dir) * c[6] * A2;
    irradiance += SH(2,  1, dir) * c[7] * A2;
    irradiance += SH(2,  2, dir) * c[8] * A2;
    irradiance = max(float3(0, 0, 0), irradiance);

    return irradiance;
}


void getSH(inout float3 c[9], in StructuredBuffer<int> _probeSH, int probeIndex)
{
    const int SHByteSize = 3 * 9;
    int offset = probeIndex * SHByteSize;   
    for(int i=0; i<9; i++)
    {
        c[i].x = Int2Float(_probeSH[offset + i*3+0]);
        c[i].y = Int2Float(_probeSH[offset + i*3+1]);
        c[i].z = Int2Float(_probeSH[offset + i*3+2]);
    }
}


float3 Relight(
    in float4 worldPos, 
    in float3 albedo, 
    in float3 normal,
    in StructuredBuffer<int> _probeSH,
    in float _probeGridSize,
    in float4 _probeCorner,
    in float4 _probeSize
    )
{
    // probe grid index for current fragment
    int3 probeIndex3 =computeProbeIndex(worldPos, _probeSize, _probeGridSize, _probeCorner);
    int3 offset[8] = {
        int3(0, 0, 0), int3(0, 0, 1), int3(0, 1, 0), int3(0, 1, 1), 
        int3(1, 0, 0), int3(1, 0, 1), int3(1, 1, 0), int3(1, 1, 1), 
    };

    float3 c[9];
    float3 Lo[8] = { float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0), };
    float3 BRDF = albedo / PI;
    float weight = 0.0005;

    // TrilinearInterpolation
    for(int i=0; i<8; i++)
    {
        int3 idx3 = probeIndex3 + offset[i];
        bool no_overide =checkIndexOveride(idx3, _probeSize);
        if(!no_overide) 
        {
            //is overide the Index, set 0
            Lo[i] = float3(0, 0, 0);
            continue;
        }

        // normal weight blend
        float3 probePos =computeProbePosition(idx3, _probeGridSize, _probeCorner);
        float3 dir = normalize(probePos - worldPos.xyz);
        float normalWeight = saturate(dot(dir, normal));
        weight += normalWeight;

        // get SH from compute buffer
        int probeIndex = Index3D21D(idx3, _probeSize);
        getSH(c, _probeSH, probeIndex);
        Lo[i] =  normalWeight* BRDF *  ComputeSurfelIrradiance(c, normal)  ;      
    }

    // trilinear interpolation
    float3 min = computeProbePosition(probeIndex3, _probeGridSize, _probeCorner);
    float3 diff = (worldPos - min) / _probeGridSize;
    float3 color = TrilinearInterpolation(Lo, diff) / weight;
    
    return color;
}


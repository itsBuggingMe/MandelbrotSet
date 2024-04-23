﻿#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#else
	#define VS_SHADERMODEL vs_5_0
	#define PS_SHADERMODEL ps_5_0
#endif

int worldPositionXa;
int worldPositionXb;
int worldPositionYa;
int worldPositionYb;
int worldPositionZa;
int worldPositionZb;

int colorMapExp;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : SV_Position;
	float4 Color : COLOR0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

    output.Position = input.Position;
	output.Color = input.Color;
	
	return output;
}

float4 forceColor(float norm)
{
    norm = 1 - norm;
    norm = pow(norm, colorMapExp);
    
    float rgb = norm * 1024;
    float r = saturate(1 - abs(rgb - 512) * 0.00390625);
    float g = saturate(1 - abs(rgb - 256) * 0.00390625);
    float b = saturate(1 - abs(rgb - 768) * 0.00390625);
    
    return float4(r, g, b, 1);
}



float4 mandelbrotcalc(double2 coord)
{
    double x = 0;
    double xSq = 0;
    double y = 0;
    double ySq = 0;
    
    int iter = 0;
    
    const int maxIter = 128;
    const float maxIterInv = 1.0 / maxIter;

    while (xSq + ySq < 4.0 && iter < maxIter)
    {
        double xTemp = xSq - ySq + coord.x;
        y = 2 * x * y + coord.y;
        x = xTemp;
        xSq = x * x;
        ySq = y * y;
        iter++;
    }

    float norm = iter * maxIterInv;
    if (norm >= 1.0)
    {
        return float4(0, 0, 0, 1);
    }
    return forceColor(norm);
}


float4 MainPS(VertexShaderOutput input) : COLOR
{//fuck AA
    double3 position = double3(
        asdouble(asuint(worldPositionXa), asuint(worldPositionXb)),
        asdouble(asuint(worldPositionYa), asuint(worldPositionYb)),
        asdouble(asuint(worldPositionZa), asuint(worldPositionZb))
    );
    
    double2 tmp = input.Position.xy + position.xy;
    return mandelbrotcalc(tmp * position.z);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
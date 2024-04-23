#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float3 worldPosition;
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
    
    int rgb = (1 - norm) * 1024;
    float rgbNorm = rgb % 256 / 255.0;
    
    //rgb in range [0, 1024)
    
    if (rgb < 256)
    { //^G
        return float4(1, rgbNorm, 0, 1);
    }
    if (rgb < 512)
    { //↓R
        return float4(1 - rgbNorm, 1, 0, 1);
    }
    if (rgb < 768)
    { //^B
        return float4(0, 1, rgbNorm, 1);
    }
    return float4(rgbNorm, 1, 1, 1);
}


float4 mandelbrotcalc(float2 coord)
{
	/* Source: https://en.wikipedia.org/wiki/Mandelbrot_set
	x0 := scaled x coordinate of pixel (scaled to lie in the Mandelbrot X scale (-2.00, 0.47))
    y0 := scaled y coordinate of pixel (scaled to lie in the Mandelbrot Y scale (-1.12, 1.12))
    x := 0.0
    y := 0.0
    iteration := 0
    max_iteration := 1000
    while (x^2 + y^2 ≤ 2^2 AND iteration < max_iteration) do
        xtemp := x^2 - y^2 + x0
        y := 2*x*y + y0
        x := xtemp
        iteration := iteration + 1
	*/
    
    float x = 0;
    float y = 0;
    int iter = 0;
	

    while (dot(x, y) <= 4 && iter < 256)
    {
        float xTemp = x * x - y * y + coord.x;
        y = 2 * x * y + coord.y;
        x = xTemp;
        iter++;
    }
    
    if (iter == 255)
    {
        return float4(0,0,0,1);
    }
    return forceColor(iter / 256.0);
}

float4 MainPS(VertexShaderOutput input) : COLOR
{//fuck AA
    float2 tmp = input.Position.xy + worldPosition.xy;
    return mandelbrotcalc(tmp * worldPosition.z) * 0.5f;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};
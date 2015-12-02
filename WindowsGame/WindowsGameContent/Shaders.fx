cbuffer Matricies
{
	float4x4 xWorld;
	float4x4 xView;
	float4x4 xProjection;
};

float4 Ambient;
float4 xDc;
float3 xDd;
float4 xSc;
float shine;

float3 xCamPos;
float4 xDepthMono;
float3 xDepthRange;

Texture2D colorMap;
SamplerState SampleType;

struct VertexShaderInput
{
    float4 Position	: POSITION0;
	float3 Normal	: NORMAL;
	float2 TexPos	: TEXCOORD0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderOutput
{
    float4 Position		: POSITION0;
	float2 TexPos		: TEXCOORD0;
    float4 WorldNormal	: TEXCOORD1;
    float4 WorldPosition : TEXCOORD2;
};

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.WorldPosition = mul(input.Position, xWorld);
    float4 ViewPosition = mul(output.WorldPosition, xView);
    output.Position = mul(ViewPosition, xProjection);

	output.WorldNormal = normalize(mul(input.Normal, xWorld));
	output.TexPos = input.TexPos;
    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float3 toLight = normalize(mul(-xDd, xWorld));
	float3 Reflect = reflect(xDd, input.WorldNormal);
	float3 toViewer = normalize(xCamPos - input.WorldPosition.xyz);
	float SpecI = pow(saturate(dot(Reflect, toViewer)), shine);
	float DI = saturate(dot(-xDd, input.WorldNormal));
	float4 textureColor = colorMap.Sample(SampleType, input.TexPos);

    return (Ambient + xDc * DI) * textureColor + SpecI;
}

technique Light
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

struct VertexShaderDepthIn
{
    float4 Position	: POSITION0;

    // TODO: add input channels such as texture
    // coordinates and vertex colors here.
};

struct VertexShaderDepthOut
{
    float4 Position		: POSITION0;
    float4 worldPosition : TEXCOORD0;
};

VertexShaderDepthOut VertexShaderDepth(VertexShaderDepthIn input)
{
    VertexShaderDepthOut output;

    output.worldPosition = mul(input.Position, xWorld);
    float4 ViewPosition = mul(output.worldPosition, xView);
    output.Position = mul(ViewPosition, xProjection);
    
	//output.Distance = distance(WorldPosition.xyz, xCamPos);
	//output.Distance = output.Position.z;

    return output;
}

float4 PixelShaderDepthMono(VertexShaderDepthOut input) : COLOR0
{
	float Distance = distance(input.worldPosition.xyz, xCamPos);
    if (Distance > xDepthRange.y)
	{
		return float4( 0, 0, 0, 1 );
	}
	if (Distance < xDepthRange.x)
	{
		return float4( 1, 1, 1, 1 );
	}
	float z = 1 - (Distance - xDepthRange.x) / xDepthRange.z;
	return float4(xDepthMono.xyz * z, 1);
}

technique DepthMono
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderDepth();
        PixelShader = compile ps_2_0 PixelShaderDepthMono();
    }
}

float4 PixelShaderDepthRGB(VertexShaderDepthOut input) : COLOR0
{
	float Distance = distance(input.worldPosition.xyz, xCamPos);
    if (Distance > xDepthRange.y)
	{
		return float4( 0, 0, 0, 1 );
	}
	if (Distance < xDepthRange.x)
	{
		return float4( 1, 1, 1, 1 );
	}
	float z = 1 - (Distance - xDepthRange.x) / xDepthRange.z, r = 0, b = 0, g = 0;
	float q = z * 7;
	//	0 - 1		1. - 2		2. - 3		3. - 4		4. - 5		5. - 6		6. - 7
	//	(z,0,z)	->	(z',0,1) ->	(0,z,1)	->	(0,1,z') ->	(z,1,0)	->	(1,z',0) ->	(1,z,z)
	if(q > 3)
	{
		if(q <= 4)
		{
			g = 1;
			b = 1 - (q - 3);
		}
		else
		{
			if(q > 5)
			{
				if(q <= 6)
				{
					r = 1;
					g = 1 - (q - 5);
				}
				else
				{
					r = 1;
					g = (q - 6);
					b = (q - 6);
				}
			}
			else
			{
				r = (q - 4);
				g = 1;
			}
		}
	}
	else
	{
		if(q > 1)
		{
			if(q <= 2)
			{
				r = 1 - (q - 1);
				b = 1;
			}
			else
			{
				g = (q - 2);
				b = 1;
			}
		}
		else
		{
			r = q;
			b = q;
		}
	}
	return float4( r, g, b, 1);
}

technique DepthRGB
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderDepth();
        PixelShader = compile ps_2_0 PixelShaderDepthRGB();
    }
}
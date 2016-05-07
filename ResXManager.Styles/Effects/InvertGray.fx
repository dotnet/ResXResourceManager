//--------------------------------------------------------------------------------------
// Sampler Inputs
//--------------------------------------------------------------------------------------

sampler2D implicitInputSampler : register(s0);

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 main(float2 uv : TEXCOORD) : COLOR
{
	float4 color = tex2D(implicitInputSampler, uv);
	
	if ((color.r == color.g) && (color.r == color.b))
	{
		float4 inverted_color = 1 - color;
		inverted_color.a = color.a;
		inverted_color.rgb *= inverted_color.a;
		
		return inverted_color;
	}
	return color;
}

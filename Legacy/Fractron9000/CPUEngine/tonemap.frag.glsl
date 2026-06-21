const float Tone_C1 = 0.5;
const float AlphaLimit = 1.0 / 256.0;

uniform sampler2D accumSampler;
uniform float scaleConstant;
uniform float brightness;
uniform float invGamma;
uniform float vibrancy;
uniform float upScaleFactor;
uniform float subStepX;
uniform float subStepY;

//log base 10
float log_b10(float x){
  return log(x) / 2.30258509;
}

vec4 tonemap(vec4 rawPix)
{
	float z, ka, lg10, gammaFactor;
	vec4 logPix;
	vec4 result;

	rawPix *= upScaleFactor;
  if(rawPix.a <= 0.5) //bail if alpha is too small to avoid dividing by zero
    return vec4(0.0, 0.0, 0.0, 0.0);
  
  logPix.a = Tone_C1 * brightness * log_b10(1.0 + rawPix.a*scaleConstant);
  ka = logPix.a / rawPix.a;
  
	logPix.r = ka * rawPix.r;
	logPix.g = ka * rawPix.g;
	logPix.b = ka * rawPix.b;
	z = pow(logPix.a, invGamma);
  gammaFactor = z / logPix.a;
  
	result.r = clamp(mix(pow(logPix.r,invGamma), gammaFactor*logPix.r, vibrancy), 0.0, 1.0);
	result.g = clamp(mix(pow(logPix.g,invGamma), gammaFactor*logPix.g, vibrancy), 0.0, 1.0);
	result.b = clamp(mix(pow(logPix.b,invGamma), gammaFactor*logPix.b, vibrancy), 0.0, 1.0);
	result.a = clamp(z, 0.0, 1.0);
  
	return result;
}

void main()
{
  vec2 tc = gl_TexCoord[0].xy;
  vec4 rawPix;
  vec4 tPix;
  vec4 accum = vec4(0.0,0.0,0.0,0.0);
  
	rawPix = texture2D(accumSampler, vec2(tc.x - subStepX, tc.y - subStepY));
  tPix = tonemap(rawPix);
  accum.r += tPix.a * tPix.r;
  accum.g += tPix.a * tPix.g;
  accum.b += tPix.a * tPix.b;
  accum.a += tPix.a;
  
  rawPix = texture2D(accumSampler, vec2(tc.x + subStepX, tc.y - subStepY));
  tPix = tonemap(rawPix);
  accum.r += tPix.a * tPix.r;
  accum.g += tPix.a * tPix.g;
  accum.b += tPix.a * tPix.b;
  accum.a += tPix.a;
  
  rawPix = texture2D(accumSampler, vec2(tc.x - subStepX, tc.y + subStepY));
  tPix = tonemap(rawPix);
  accum.r += tPix.a * tPix.r;
  accum.g += tPix.a * tPix.g;
  accum.b += tPix.a * tPix.b;
  accum.a += tPix.a;
  
  rawPix = texture2D(accumSampler, vec2(tc.x + subStepX, tc.y + subStepY));
  tPix = tonemap(rawPix);
  accum.r += tPix.a * tPix.r;
  accum.g += tPix.a * tPix.g;
  accum.b += tPix.a * tPix.b;
  accum.a += tPix.a;
 
  if(accum.a < AlphaLimit)
  {
    gl_FragColor = vec4(0.0,0.0,0.0,0.0);
  }
  else
  {
    accum.r /= accum.a;
    accum.g /= accum.a;
    accum.b /= accum.a;
    accum.a *= 0.25;
    gl_FragColor = accum;
  }
}


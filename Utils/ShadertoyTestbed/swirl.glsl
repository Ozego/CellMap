float cubicPulse( float c, float w, float x )
{
    x = abs(x - c);
    if( x>w ) return 0.0;
    x /= w;
    return 1.0 - x*x*(3.0-2.0*x);
}
float quadImpulse( float k, float x )
{
    return 2.0*sqrt(k)*x/(1.0+k*x*x);
}
float polyImpulse( float k, float n, float x )
{
    return (n/(n-1.0))*pow((n-1.0)*k,1.0/n) * x/(1.0+k*pow(x,n));
}
float checker(float size, vec2 uv)
{
    uv *= size;
    uv = floor(uv);
    float v = mod(uv.x+uv.y,2.);
    return v;
}
void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = ( fragCoord - .5 * iResolution.xy ) / iResolution.y;
    uv *=8.;
    vec2 U = vec2(length(uv), atan(uv.x,uv.y));
    float t = U.x*16. + U.y;
    vec2 d1 = vec2(sin(t),cos(t))*polyImpulse(64.,2.,U.x)*.5;
    vec2 disp = vec2(.5);
    fragColor = vec4( disp+d1, .5, 1. );
}

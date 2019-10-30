#define PI 3.141592
#define THETA 2.399963229728653 //THETA is the golden angle in radians: 2 * PI * ( 1 - 1 / PHI )
vec2 spiralPosition(float t)
{
    float angle = t * THETA - iTime * .001; 
    float radius = log( ( t + .5 ) * .5 );
    return vec2( radius * cos( angle ) , radius * sin( angle ) );
}
float impulse( float x )
{
    return 1./(x*x+1.);
}
vec2 siralDistortion(vec2 P, float t, float l, float s)
{
    vec2 U = P;
    P = vec2(length(P),atan(P.x,P.y));
    P.y += (1.-P.x)*t+iTime*4.;
    P = impulse(P.x*l)*(vec2(sin(P.y),cos(P.y))*(P.x)-U);
    return P*s;
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
    vec2 mouse = iMouse.xy/iResolution.xy-.5;
    vec2 uv = ( fragCoord - .5 * iResolution.xy ) / iResolution.y;
    uv *=6.;
    vec2 D = vec2(0.);
    float a = 0.;
    float xTime = iTime;
    for(float i = 0.; i<32.; i++)
    {

        D += siralDistortion( uv - spiralPosition(i) ,64.,16.,1.);
        xTime += PI;
    }
    float c = checker
    (
        8., 
        uv
        +vec2(iTime/2.,-iTime)*.1
        + D
    );
    fragColor = vec4( D+.5,c, 1. );
}

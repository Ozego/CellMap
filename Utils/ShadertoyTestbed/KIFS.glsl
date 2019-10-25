#define PI 3.14159265359
void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = ( fragCoord - .5 * iResolution.xy ) / iResolution.y;
    vec2 mouse = iMouse.xy/iResolution.xy;
    uv *= 3.;
    vec3 col = vec3(0.);

    
    uv.x = abs( uv.x );
    uv.x -= .5;

    float a = 2. / 3. * PI;
    vec2  n = vec2(sin(a),cos(a));
    float d = dot(uv, n);

    col.b += smoothstep(4./iResolution.y,.0,abs(d));

    uv   -= n * min( 0., d ) * 2.;
    uv.x -=.5;

    
    // uv   *= 3.;
    // uv.x -= 1.5;
    
    
    d = length( uv - vec2(clamp(uv.x,-1.,1.),0.));
    col += smoothstep(4./iResolution.y,.0,d);
    col.rg += .5*smoothstep(4./iResolution.y,.0,cos(uv*PI*8.)+1.);
    
    fragColor = vec4( col, 1. );
}

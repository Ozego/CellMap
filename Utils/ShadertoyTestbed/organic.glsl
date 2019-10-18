vec2 fermatSpiralPosition(float t)
{
    float theta = t * 2.399963229728653 - iTime*.001;
    float radius = (t+.5)*.5;
    // float radius = sqrt(t+.5)/.45;
    return vec2(radius*cos(theta)+.5,radius*sin(theta)+.5);
}

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    int v = int( fragCoord.x ) & int( fragCoord.y );
    vec2 uv = ( fragCoord - .5 * iResolution.xy ) / iResolution.y * 1024.;
    vec3 col = vec3( 0. );
    float a = 0.;
    float d = 1.;
    for(int i = 0; i < 200; i++)
    {
        vec2 point = fermatSpiralPosition(float(i))*6.66;
        a += atan(uv.x-point.x,uv.y-point.y);
        d = min(length(uv-point)*.02,d);
        if(floor(point)!=floor(uv)) continue;
        col = vec3(0.,1.,0.);
    }
    float r1 = 1.;
    float r2 = 1.;
    d = 1.-pow(1.-d,32.);
    // a += sin(length(uv)*.01+iTime*.5)*2.75;
    // a += length(uv)*pow(sin(iTime*.1),3.)*.5;
    col   = smoothstep(.75,1.0,vec3(.5+.5*sin(a+iTime * -1. )));
    col.r = (.5+.5*sin(a+iTime + 2.9 ));
    col.g = (.5+.5*sin(a+iTime + 1.7 ));
    col.b = (.5+.5*sin(a+iTime + 0.0 ));
    // if(floor(uv)==vec2(0.))col=vec3(1.,0.,0.);
    col   *= d;
    fragColor = vec4( col, 1. );
}
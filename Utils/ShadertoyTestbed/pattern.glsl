float sdSphere( vec3 point, float radius )
{
  return length(point)-radius;
}
void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    int v = int( fragCoord.x ) & int( fragCoord.y );
    vec2 uv = ( fragCoord - .5 * iResolution.xy ) / iResolution.y;

    vec3    cameraOrigin    = vec3(0.,1.+sin(2.*iTime)*.125,-.75);
    vec3    lookAt          = vec3(0.);
    float   zoom            = .25;

    vec3    forward         = normalize( lookAt - cameraOrigin ),
            right           = normalize( cross( vec3(0.,1.,0.), forward ) ),
            up              = cross( forward, right ),
            center          = cameraOrigin + forward * zoom,
            intersection    = center + uv.x * right + uv.y * up,
            rayDirection    = normalize( intersection - cameraOrigin ) ;

    float distanceToSurface, distanceToOrigin; vec3 point;
    for(int i=0; i<32; i++)
    {
        point = cameraOrigin + rayDirection * distanceToOrigin;
        distanceToSurface = sdSphere(point,1.);
        if(distanceToSurface<.00050) break;
        distanceToOrigin += distanceToSurface;
    }

    vec3 col = vec3( 0. );
    col = rayDirection;

    if(distanceToSurface<.01)
    {
        float x = atan(point.x, point.z);
        float y = atan(length(point.xz), point.y);
        col = vec3(int(sin(13.*y+iTime)*.5+1.) ^ int(sin(8.*x+iTime*4.)*.5+1.));
        //col.r = distanceToSurface*100.;
    }
    fragColor = vec4( col, 1. );
}
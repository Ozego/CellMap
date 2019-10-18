float sdSphere( vec3 point, float radius )
{
  return length(point)-radius;
}
float sdPlane( vec3 point, vec3 normal )
{
  // n must be normalized
  return dot(point,normal.xyz);
}
void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    int v = int( fragCoord.x ) & int( fragCoord.y );
    vec2 uv = ( fragCoord - .5 * iResolution.xy ) / iResolution.y;

    vec3    cameraOrigin    = vec3(.75,.75,-.75+sin(2.*iTime)*.25);
    vec3    lookAt          = vec3(0.);
    float   zoom            = .25;

    vec3    forward         = normalize( lookAt - cameraOrigin ),
            right           = normalize( cross( vec3(0.,1.,0.), forward ) ),
            up              = cross( forward, right ),
            center          = cameraOrigin + forward * zoom,
            intersection    = center + uv.x * right + uv.y * up,
            rayDirection    = normalize( intersection - cameraOrigin ) ;

    float distanceToSurface, distanceToOrigin, stepTest; vec3 point;
    for(int i=0; i<32; i++)
    {
        stepTest = 1. - stepTest;
        point = cameraOrigin + rayDirection * distanceToOrigin;
        distanceToSurface = sdSphere(point,1.);
        if(distanceToSurface<.0001) break;
        distanceToOrigin += distanceToSurface;
    }

    vec3 col = vec3( 0. );
    col = vec3(log(1.-rayDirection.y));

    if(distanceToSurface<.005)
    {
        vec3 normal = normalize(point);
        float lumination = dot(normal,normalize(vec3(1.,3.,0.)));
        float x = atan(point.x, point.z);
        float y = atan(length(point.xz), point.y);
        col = vec3(int(sin(13.*y+iTime)*.5+1.) ^ int(sin(8.*x+iTime*4.)*.5+1.));
        col.r = stepTest==0.?.9:.8;
        col *= lumination;
        //col.r = distanceToSurface*100.;
    }
    fragColor = vec4( col, 1. );
}
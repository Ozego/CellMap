fixed3 TrigGrad             (fixed t, fixed3 a, fixed3 b, fixed3 c, fixed3 d)
{
    fixed3 x = c * t + d;
    return a + b * cos( TAU * x );                                              //7 steps
}
fixed3 AsymetricPolyGrad    (fixed t, fixed3 a, fixed3 b, fixed3 c, fixed3 d)
{
    fixed3 x = c * t + d;
    return a + b * x * x * ( 3. - 2. * x );                                     //9 steps
}
fixed3 SymetricPolyGrad     (fixed t, fixed3 a, fixed3 b, fixed3 c, fixed3 d)
{
    fixed3 x = c * t + d;
    return a + b * ( 1. - x * x * ( 3. - 2. * abs( x )));                       //11 steps
}
fixed3 ExponentialGrad      (fixed t, fixed3 a, fixed3 b, fixed3 c, fixed3 d)
{
    fixed3 x = c * t + d;
    return a + b * x * exp( 1. - x );                                           //8 steps
}
fixed Mod (fixed i, fixed m)
{
    return (i%m+m)%m;
}
int Mod (int i, int m)
{
    return (i%m+m)%m;
}
fixed2 PolarCoordinates(fixed2 uv)
{
    return 
        fixed2(
            atan2(uv.y-.5,uv.x-.5)/TAU+.5,
            1. - distance(uv,(.5,.5))*2.
        );
}  
fixed2 PolarLogCoordinates(fixed2 uv)
{
    return 
        fixed2(
            atan2(uv.y-.5,uv.x-.5)/TAU+.5,
            - log(distance(uv,(.5,.5))*2.)*.1
        );
}  
fixed2x2 ShearMatrix(fixed2 shear)
{
    return
        fixed2x2(
                1.0 , -shear.y ,
            shear.x ,      1.0
        );
}
fixed SDFCircle(fixed2 uv, fixed radius)
{
    return length(uv) - radius; 
}
fixed SDFLine(fixed2 uv, fixed2 a, fixed2 b)
{
    fixed2 uva = uv-a, ba = b-a;
    fixed h = saturate( dot(uva,ba) / dot(ba,ba));
    return length(uva-ba*h);
}
fixed SDFectangle(fixed2 uv, fixed2 rectangle)
{
    fixed2 d = abs(uv)-rectangle;
    return length
    ( 
          max( d, (0.).xx ) ) 
        + min( max( d.x, d.y ), 
        0.0 
    );
}
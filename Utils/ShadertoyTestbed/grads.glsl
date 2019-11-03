#define PI      3.141592653589793
#define TAU     6.283185307179586

vec3 TrigGrad (float t, vec3 a, vec3 b, vec3 c, vec3 d)
{
    vec3 x = c * t + d;
    return a + b * cos( TAU * x );                                              //7 steps
}

void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
    vec3 A1 = vec3( 0.2694418 , 0.1439928, 0.1541992 );
    vec3 B1 = vec3(-0.06860124,-0.1302561, 0.03530375);
    vec3 C1 = vec3( 0.7455726 , 0.5831975, 0.9784149 );
    vec3 D1 = vec3( 0.2532835 , 1.163837 , 0.3923408 );

    vec3 A2 = vec3( 0.490207 , 0.4397284, 0.2817695);
    vec3 B2 = vec3( 0.4378781, 0.3620131, 0.3923452);
    vec3 C2 = vec3( 0.8398046, 0.6591553, 0.6446771);
    vec3 D2 = vec3( 0.876716 , 0.9750792, 0.9924104); 

    vec3 A3 = vec3( 0.6408299, 0.6948664, 0.4449073);
    vec3 B3 = vec3( 0.3206481, 0.2465469, 0.4434777);
    vec3 C3 = vec3( 0.7130515, 0.8601837, 0.729125 );
    vec3 D3 = vec3( 0.8716165, 0.8527499, 0.8729508); 

    vec3 A4 = vec3( 0.5125155, 0.3941055, 0.1912305);
    vec3 B4 = vec3( 0.5177792, 0.1622187, 0.3018994);
    vec3 C4 = vec3( 0.6838295, 0.8619183, 0.7930082);
    vec3 D4 = vec3( 0.7797305, 0.4526185, 0.3980739);

    vec3 A5 = vec3( 0.9390419, 0.3795465,-0.3360146);
    vec3 B5 = vec3( 0.7839309, 0.4056006, 1.065312 );
    vec3 C5 = vec3( 0.9896352, 0.7986437, 0.6249498);
    vec3 D5 = vec3( 0.6151313, 0.4002389, 0.5049848); 

    vec2 uv = fragCoord/iResolution.xy;


    vec3 grad1 = TrigGrad(uv.y,A1,B1,C1,D1);
    vec3 grad2 = TrigGrad(uv.y,A2,B2,C2,D2);
    vec3 grad3 = TrigGrad(uv.y,A3,B3,C3,D3);
    vec3 grad4 = TrigGrad(uv.y,A4,B4,C4,D4);
    vec3 grad5 = TrigGrad(uv.y,A5,B5,C5,D5);
    vec3 col = smoothstep(grad4,grad5,uv.x);

    fragColor = vec4(col,1.0);
}


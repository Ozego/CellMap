using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveFitter : MonoBehaviour
{
    [SerializeField] private bool       restart;
    [SerializeField] private Texture2D  gradients;
    [SerializeField] private float      gradientPosition;
    [SerializeField] private bool       useRandomPosition;
    [SerializeField] private int        sampleCount;
    [SerializeField] private int        variationCount;
    [SerializeField] private gFuncs     gradientFunction;
    private Material material;
    private Vector4 oldDiff;
    private Vector4 A;
    private Vector4 B;
    private Vector4 C;
    private Vector4 D;

    private class gradValues 
    {
        public float a;
        public float b;
        public float c;
        public float d;
        public float diff;
    }
    private enum gFuncs
    {
        trigonometric,
        asymetricPolynomial,
        symetricPolynomial,
        exponential
    }

    private gFuncs oldFunction;
    void OnValidate()
    {
        if(restart)
        {
            restart = false;
            initiate();
        }
        if(gradientFunction!=oldFunction)
        {
            bool oldUseRandomPosition = useRandomPosition;
            useRandomPosition = false;
            initiate();
            useRandomPosition = oldUseRandomPosition;
        }
        oldFunction = gradientFunction;
    }
    void Awake()
    {
        initiate();
    }
    void Update()
    {
        step();
    }

    private void initiate()
    {
        if (useRandomPosition) gradientPosition = Random.value;
        switch (gradientFunction)
        {
            case gFuncs.trigonometric:          setTrigValues(); break;
            case gFuncs.asymetricPolynomial:    setAsymetricPolyValues(); break;
            case gFuncs.symetricPolynomial:     setSymetricPolyValues();  break;
            case gFuncs.exponential:            setExponentialValues();  break;
        }
        
        GetComponent<MeshRenderer>().material = material;
        material.SetTexture("_MainTex", gradients);
        material.SetFloat("_p", gradientPosition);
        setVectors();
        oldDiff = new Vector4(1f, 1f, 1f, .0f);
    }

    private void setTrigValues()
    {
        A = new Vector4(.5f,.5f,.5f,.0f);
        B = new Vector4(.5f,.5f,.5f, 0f);
        C = new Vector4( 1f, 1f, 1f,.0f);
        D = new Vector4(.5f,.5f,.5f,.0f);
        material = new Material(Shader.Find("Ozeg/Unlit/SplitGradientTrigometric"));
    }
    private void setAsymetricPolyValues()
    {
        A = new Vector4(.5f,.5f,.5f,.0f);
        B = new Vector4( 0f, 0f, 0f, 0f);
        C = new Vector4( 0f, 0f, 0f, 0f);
        D = new Vector4(-1f,-1f,-1f, 0f);
        material = new Material(Shader.Find("Ozeg/Unlit/SplitGradientAsymetricPoly"));
    }
    private void setSymetricPolyValues()
    {
        A = new Vector4(.5f,.5f,.5f,.0f);
        B = new Vector4( 0f, 0f, 0f, 0f);
        C = new Vector4( 2f, 2f, 2f, 2f);
        D = new Vector4(-1f,-1f,-1f, 0f);
        material = new Material(Shader.Find("Ozeg/Unlit/SplitGradientSymetricPoly"));
    }
    private void setExponentialValues()
    {
        A = new Vector4(.5f,.5f,.5f,.0f);
        B = new Vector4( 0f, 0f, 0f, 0f);
        C = new Vector4( 0f, 0f, 0f, 0f);
        D = new Vector4( 0f, 0f, 0f, 0f);
        material = new Material(Shader.Find("Ozeg/Unlit/SplitGradientExponential"));
    }

    private void step()
    {
        for (int c = 0; c < 3; c++)
        {
            if(oldDiff[c]>.015f) stepChannel(c);
        }
        setVectors();
    }

    private void recalcOldDiff(int c)
    {
        gradValues grad = new gradValues();
        grad.a = A[c];
        grad.b = B[c];
        grad.c = C[c];
        grad.d = D[c];
        setGradDiff(c, grad);
        oldDiff[c] = grad.diff;
    }

    private void stepChannel(int c)
    {
        List<gradValues> grads = new List<gradValues>();
        for (int v = 0; v < variationCount; v++)
        {
            gradValues grad = new gradValues();
            grad.a = A[c] + Random.Range( -oldDiff[c]/2f, oldDiff[c]/2f );
            grad.b = B[c] + Random.Range( -oldDiff[c]/2f, oldDiff[c]/2f );
            grad.c = C[c] + Random.Range( -oldDiff[c]/4f, oldDiff[c]/4f );
            grad.d = D[c] + Random.Range( -oldDiff[c]/2f, oldDiff[c]/2f );
            setGradDiff(c, grad); 
            grads.Add(grad);
        }
        grads.Sort((a,b)=>a.diff.CompareTo(b.diff));
        if (grads[0].diff < oldDiff[c])
        {
            oldDiff[c] = grads[0].diff;
            A[c] = grads[0].a;
            B[c] = grads[0].b;
            C[c] = grads[0].c;
            D[c] = grads[0].d;
        }
    }


    private void setGradDiff(int c, gradValues grad)
    {
        grad.diff = 0f;
        for (int s = 0; s < sampleCount; s++)
        {
            float point = ((float)s + .25f) / (float)sampleCount;
            float sVal = gradients.GetPixelBilinear(point, gradientPosition)[c];
            float gVal = 0f;
            switch (gradientFunction)
            {
                case gFuncs.trigonometric:          gVal = getTrigGradValue(          point, grad.a, grad.b, grad.c, grad.d ); break;
                case gFuncs.asymetricPolynomial:    gVal = getAsymetricPolyGradValue( point, grad.a, grad.b, grad.c, grad.d ); break;
                case gFuncs.symetricPolynomial:     gVal = getSymetricPolyGradValue(  point, grad.a, grad.b, grad.c, grad.d ); break;
                case gFuncs.exponential:            gVal = getExponentialGradValue(   point, grad.a, grad.b, grad.c, grad.d ); break;
            }
            gVal = Mathf.Clamp01(gVal);
            grad.diff += Mathf.Abs(sVal - gVal);
        }
        grad.diff /= (float)sampleCount;
    }

    private void setVectors()
    {
        material.SetVector("_A", A);
        material.SetVector("_B", B);
        material.SetVector("_C", C);
        material.SetVector("_D", D);
    }
    private float getTrigGradValue (float t, float a, float b, float c, float d)
    {
        return Mathf.Clamp01(a + b * Mathf.Cos( Mathf.PI * 2f * ( c * t + d ) ));
    }
    private float getAsymetricPolyGradValue (float t, float a, float b, float c, float d)
    {
        float x = c * t + d;
        return a + b *  x * x * ( 3f - 2f * x );
    }
    private float getSymetricPolyGradValue (float t, float a, float b, float c, float d)
    {
        float x = c * t + d;
        return a + b * ( 1f - x * x * ( 3f - 2f * Mathf.Abs( x )));
    }
    private float getExponentialGradValue (float t, float a, float b, float c, float d)
    {
        float x = c * t + d;
        return a + b * x * Mathf.Exp( 1f - x );
    }
}

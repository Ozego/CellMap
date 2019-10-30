using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class WaveFitter : MonoBehaviour
{
    [SerializeField] private Texture2D  gradients;
    [SerializeField] private float      gradientPosition;
    [SerializeField] private bool       useRandomPosition;
    [SerializeField] private int        sampleCount;
    [SerializeField] private int        variationCount;

    [SerializeField] private Vector4 A = new Vector4(.5f, .5f, .5f, .0f);
    [SerializeField] private Vector4 B = new Vector4(.5f, .5f, .5f, .0f);
    [SerializeField] private Vector4 C = new Vector4( 1f,  1f,  1f, .0f);
    [SerializeField] private Vector4 D = new Vector4(.5f, .5f, .5f, .0f);
    private Material material;
    [SerializeField] private Vector4 oldDiff;


    void Awake()
    {
        initiate();
    }
    void Update()
    {
        if(Time.frameCount%640==0&useRandomPosition) gradientPosition = Random.value;
        material.SetFloat("_p", gradientPosition);
        step();
    }

    void initiate()
    {
        if (useRandomPosition) gradientPosition = Random.value;
        material = new Material(Shader.Find("Ozeg/Unlit/TrigometricSplitGradient"));
        GetComponent<MeshRenderer>().material = material;
        material.SetTexture("_MainTex", gradients);
        material.SetFloat("_p", gradientPosition);
        setVectors();
        oldDiff = new Vector4(1f, 1f, 1f, .0f);
    }

    void step()
    {
        for (int c = 0; c < 3; c++)
        {
            recalcOldDiff(c);
            if(oldDiff[c]>.015f) stepChannel(c);
        }
        setVectors();
    }

    private void recalcOldDiff(int c)
    {
        trigGrad grad = new trigGrad();
        grad.a = A[c];
        grad.b = B[c];
        grad.c = C[c];
        grad.d = D[c];
        setGradDiff(c, grad);
        oldDiff[c] = grad.diff;
    }

    void stepChannel(int c)
    {
        List<trigGrad> grads = new List<trigGrad>();
        for (int v = 0; v < variationCount; v++)
        {
            trigGrad grad = new trigGrad();
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


    private void setGradDiff(int c, trigGrad grad)
    {
        grad.diff = 0f;
        for (int s = 0; s < sampleCount; s++)
        {
            float point = ((float)s + .25f/* + .5f*Random.value*/) / (float)sampleCount;
            float sVal = gradients.GetPixelBilinear(point, gradientPosition)[c];
            float gVal = getTrigGradValue(point, grad.a, grad.b, grad.c, grad.d);
            grad.diff += Mathf.Abs(sVal - gVal);
        }
        grad.diff /= (float)sampleCount;
    }

    class trigGrad 
    {
        public float a;
        public float b;
        public float c;
        public float d;
        public float diff;
    }
    private void setVectors()
    {
        material.SetVector("_A", A);
        material.SetVector("_B", B);
        material.SetVector("_C", C);
        material.SetVector("_D", D);
    }

    float getTrigGradValue (float t, float a, float b, float c, float d)
    {
        return Mathf.Clamp01(a + b * Mathf.Cos( Mathf.PI * 2f * ( c * t + d ) ));
    }
}

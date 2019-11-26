using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFluidMap : MonoBehaviour
{
    [SerializeField] private Texture2D initMap;
    [SerializeField] private Texture2D mask;
    [SerializeField] private Texture2D display;
    [SerializeField] [Range( -1f, 20f )] private float flowAdvection         = 1.0f;
    [SerializeField] [Range(  0f, 1f  )] private float flowDiffusion         = 0.1f;
    [SerializeField] [Range( -1f, 2f  )] private float pressureDissipation   = 1.0f;
    [SerializeField] [Range( -1f, 3f  )] private float pressureVelocity      = 1.0f;

    [SerializeField] private Boolean useScreenSize;
    [SerializeField] private Vector2Int size;
    [SerializeField] private Material rMaterial;
    private static int          rFrameCount     = 2;
    private int                 rFrame          = 0;
    private RenderTexture[]     rTextures       = new RenderTexture[rFrameCount];
    private static int          dFrameCount     = 2;
    private int                 dFrame          = 0;
    private RenderTexture[]     dTextures       = new RenderTexture[dFrameCount];
    private MeshRenderer        rRenderer;
    private bool materialToggle = false;
    private bool uvToggle       = false;

    void OnValidate()
    { 
        SetMaterialProperties(); 
        if(uvToggle&materialToggle) rRenderer.material.SetTexture("_dTex", display);
    }
    void OnGizmos(){}
    void Awake()
    {
        if (useScreenSize)
        {
            size.x = (Screen.width  + 7) & 0xFFF8;
            size.y = (Screen.height + 7) & 0xFFF8;
        }
        Vector4 sizeVector = new Vector4(1f / size.x, 1f / size.y, size.x, size.y);
        rMaterial.SetVector(  "_Size",                  sizeVector          );
        SetMaterialProperties();
        GenerateDisplay(); 
        GenerateMaps();
    }

    private void SetMaterialProperties()
    {
        rMaterial.SetTexture( "_Mask",                  mask                );
        rMaterial.SetFloat(   "_FlowAdvection",         flowAdvection       );
        rMaterial.SetFloat(   "_FlowDiffusion",         flowDiffusion       );
        rMaterial.SetFloat(   "_PressureDissipation",   pressureDissipation );
        rMaterial.SetFloat(   "_PressureVelocity",      pressureVelocity    );
    }   

    void Update()
    {
        rMaterial.SetFloat("_Frame",(float)Time.frameCount);
        int pFrame;
        for (int i = 1; i <= 3; i++)
        {
            pFrame = rFrame; rFrame++; rFrame %= rFrameCount;
            Graphics.Blit(rTextures[pFrame],rTextures[rFrame],rMaterial,i);
        }
        rMaterial.SetTexture("_VecMap", rTextures[rFrame]);
        pFrame = dFrame; dFrame++; dFrame %= dFrameCount;
        Graphics.Blit(dTextures[pFrame],dTextures[rFrame],rMaterial,5);
        if(Input.GetKeyDown("s"))
        {
            RenderTexture saveTex = new RenderTexture(dTextures[rFrame]);
            Graphics.Blit(dTextures[rFrame], saveTex, rMaterial, 6);
            Texture2D t2d = new Texture2D(saveTex.width,saveTex.height,TextureFormat.RGBA32,false);
            RenderTexture.active = saveTex;
            t2d.ReadPixels(new Rect(0,0,saveTex.width,saveTex.height),0,0);
            RenderTexture.active = null;
            byte[]  b = t2d.EncodeToPNG();
            string  p = String.Format( "{0}/{1}{2}", Application.persistentDataPath, this.name, DateTime.Now.GetHashCode().ToString("X4") );
            Debug.Log( p );
            System.IO.File.WriteAllBytes(p+".png",b);
            saveTex.Release();
        }
        if(Input.GetKeyDown("u")) uvToggle = !uvToggle; 
        if(Input.GetKeyDown("d"))
        {
            materialToggle = !materialToggle;
            if(materialToggle)
            {
                rRenderer.material = new Material( Shader.Find( "Unlit/Texture" ) );
            }
            else
            {
                if(!uvToggle)
                rRenderer.material = new Material( Shader.Find( "Ozeg/Unlit/VectorFieldDisplay" ) );
                else
                {
                    Vector4 sizeVector = new Vector4(1f / size.x, 1f / size.y, size.x, size.y);
                    rRenderer.material = new Material( Shader.Find( "Ozeg/Unlit/VectorFieldDisplayTex" ) );
                    rRenderer.material.SetTexture("_dTex", display);
                    rRenderer.material.SetVector( "_Size", sizeVector);
                }
            }
        } 
        if(Input.GetKeyDown("r"))
        {
            for(int f = 0; f < rFrameCount; f++)
            {
                Graphics.Blit(initMap,rTextures[f],rMaterial,0);
            }
            for(int f = 0; f < dFrameCount; f++)
            {
                Graphics.Blit(initMap,dTextures[f],rMaterial,4);
            }
        }

        if(uvToggle)
        {
            if(!materialToggle) rRenderer.material.SetTexture( "_VecMap", rTextures[rFrame] );
            rRenderer.material.SetTexture( "_MainTex", dTextures[dFrame] );
        }
        else
        rRenderer.material.SetTexture( "_MainTex", rTextures[rFrame] );
        
    }
    void GenerateDisplay()
    {
        Camera.main.orthographicSize = size.y / 2;
        var display = new GameObject( "display" );
        display.transform.parent = transform;
        var meshFilter = display.AddComponent<MeshFilter>();
        meshFilter.mesh = MeshGenerator.GetQuad( size.x, size.y );
        rRenderer = display.AddComponent<MeshRenderer>();
        rRenderer.material = new Material( Shader.Find( "Ozeg/Unlit/VectorFieldDisplay" ) );
    }
    void GenerateMaps()
    {
        initMap.wrapMode = TextureWrapMode.Repeat;
        for(int f = 0; f < rFrameCount; f++)
        {
            rTextures[f] = new RenderTexture( size.x, size.y, 24 );
            rTextures[f].enableRandomWrite = true;
            rTextures[f].filterMode = FilterMode.Bilinear;
            rTextures[f].wrapMode = TextureWrapMode.Repeat;
            rTextures[f].Create();
            Graphics.Blit(initMap,rTextures[f],rMaterial,0);
        }
        for(int f = 0; f < rFrameCount; f++)
        {
            dTextures[f] = new RenderTexture( size.x, size.y, 24 );
            dTextures[f].enableRandomWrite = true;
            dTextures[f].filterMode = FilterMode.Bilinear;
            dTextures[f].wrapMode = TextureWrapMode.Repeat;
            dTextures[f].Create();
            Graphics.Blit(initMap,dTextures[f],rMaterial,4);
        }
    }
}

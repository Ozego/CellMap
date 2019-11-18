using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleFluidMap : MonoBehaviour
{
    [SerializeField] private Texture2D initMap;
    [SerializeField] private Texture2D mask;
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
    private MeshRenderer        rRenderer;

    void OnValidate()
    { 
        SetMaterialProperties(); 
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

    bool materialToggle = false;
    void Update()
    {
        rMaterial.SetFloat("_Frame",(float)Time.frameCount);
        int pFrame;
        for (int i = 1; i <= 3; i++)
        {
            pFrame = rFrame; rFrame++; rFrame %= rFrameCount;
            Graphics.Blit(rTextures[pFrame],rTextures[rFrame],rMaterial,i);
        }
        rRenderer.material.SetTexture( "_MainTex", rTextures[rFrame] );
        if(Input.GetKeyDown("d"))
        {
            materialToggle = !materialToggle;
            if(materialToggle)
            {
                rRenderer.material = new Material( Shader.Find( "Unlit/Texture" ) );
            }
            else
            {
                rRenderer.material = new Material( Shader.Find( "Ozeg/Unlit/VectorFieldDisplay" ) );
            }
        } 
        if(Input.GetKeyDown("r"))
        {
            for(int f = 0; f < rFrameCount; f++)
            {
                Graphics.Blit(initMap,rTextures[f],rMaterial,0);
            }
        }
        
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
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidMap : MonoBehaviour
{
    [SerializeField] private Boolean useScreenSize;
    [SerializeField] private Vector2Int size;
    [SerializeField] private Texture2D InitialMap;
    [SerializeField] private Texture2D ForceMap;

    [SerializeField] private Material Advection;
    [SerializeField] private Material Diffusion;
    [SerializeField] private Material Forces;
    [SerializeField] private Material Pressure;
    [SerializeField] private Material PressureSubstraction;

    private static int          rFrameCount     = 2;
    private int                 rFrame          = 0;
    private RenderTexture[]     vectorMap       = new RenderTexture[rFrameCount];
    private RenderTexture       advectionMap;
    // private RenderTexture       pressureMap;
    private MeshRenderer        rRenderer;

    void OnValidate(){}
    void OnGizmos(){}
    void Awake()
    {
        if(useScreenSize)
        {
            size.x = (Screen.width  + 7) & 0xFFF8;
            size.y = (Screen.height + 7) & 0xFFF8;
        }
        GenerateDisplay(); GenerateMaps();

        Vector4 sizeVector = new Vector4( 1f/size.x, 1f/size.y, size.x, size.y );
        Advection.SetVector("_Size", sizeVector );
        Diffusion.SetVector("_Size", sizeVector );
        Forces.SetVector(   "_Size", sizeVector );
        Pressure.SetVector( "_Size", sizeVector );
        
        Graphics.Blit(InitialMap,vectorMap[rFrame]);
    }
    void Update()
    {
        int pFrame = rFrame; rFrame++; rFrame %= rFrameCount;
        Graphics.Blit(vectorMap[pFrame],advectionMap,Advection,0);
        Diffusion.SetTexture("_Advection", advectionMap);
        for (int i = 0; i < 16; i++)
        {
            Graphics.Blit(vectorMap[pFrame],vectorMap[rFrame],Diffusion,0);
            pFrame = rFrame; rFrame++; rFrame %= rFrameCount;
        }
        Forces.SetTexture("_ForceMap", ForceMap);
        Graphics.Blit(vectorMap[pFrame],vectorMap[rFrame],Forces,0);
        pFrame = rFrame; rFrame++; rFrame %= rFrameCount;
        Graphics.Blit(vectorMap[pFrame],vectorMap[rFrame],Pressure,0);
        pFrame = rFrame; rFrame++; rFrame %= rFrameCount;
        Graphics.Blit(vectorMap[pFrame],vectorMap[rFrame],PressureSubstraction,0);
        rRenderer.material.SetTexture( "_MainTex", vectorMap[rFrame] );
    }
    void GenerateDisplay()
    {
        Camera.main.orthographicSize = size.y / 2;
        var display = new GameObject( "display" );
        display.transform.parent = transform;
        var meshFilter = display.AddComponent<MeshFilter>();
        meshFilter.mesh = MeshGenerator.GetQuad( size.x, size.y );
        rRenderer = display.AddComponent<MeshRenderer>();
        rRenderer.material = new Material( Shader.Find( "Unlit/Texture" ) );
    }
    void GenerateMaps()
    {
        for(int f = 0; f < rFrameCount; f++)
        {
            vectorMap[f] = GenerateMap();
        }
        advectionMap = GenerateMap();
    }
    RenderTexture GenerateMap()
    {
        RenderTexture rt = new RenderTexture( size.x, size.y, 24 );
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();
        return rt;
    }
}

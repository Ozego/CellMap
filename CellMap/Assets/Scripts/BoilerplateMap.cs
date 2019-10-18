using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoilerplateMap : MonoBehaviour
{
    [SerializeField] private Boolean useScreenSize;
    [SerializeField] private Vector2Int size;
    [SerializeField] private Material rMaterial;
    private static int          rFrameCount     = 2;
    private int                 rFrame          = 0;
    private RenderTexture[]     rTextures       = new RenderTexture[rFrameCount];
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
    }
    void Update()
    {
        int pFrame = rFrame; rFrame++; rFrame %= rFrameCount;
        Graphics.Blit(rTextures[pFrame],rTextures[rFrame],rMaterial,0);
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
        rRenderer.material = new Material( Shader.Find( "Unlit/Texture" ) );
    }
    void GenerateMaps()
    {
        for(int f = 0; f < rFrameCount; f++)
        {
            rTextures[f] = new RenderTexture( size.x, size.y, 24 );
            rTextures[f].enableRandomWrite = true;
            rTextures[f].filterMode = FilterMode.Point;
            rTextures[f].Create();
        }
    }
}

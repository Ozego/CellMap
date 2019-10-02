using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HeightMap : MonoBehaviour
{
//  Tag                 Access      Type            Name                        Set
    [HideInInspector]   public      RenderTexture   HeightMapTexture;

    [SerializeField]    private     Vector2Int      size;
    [SerializeField]    private     ComputeShader   csErrosion;
    [SerializeField]    private     Texture2D       input;
    [SerializeField]    private     Texture2D       blueNoise;
    [SerializeField]    private     Material        initializeHeightmap;
    [SerializeField]    private     Material        flowErosion;

                        private     RenderTexture   filterMapTexture;
                        private     MeshRenderer    displayRenderer;
                        private     ComputeBuffer[] headBuffers                 = new ComputeBuffer[2];
                        private     List<Head>      heads                       = new List<Head>();
                        private     int             maxHeads;
                        private     int ConsumeBufferID = 0, 
                                        AppendBufferID  = 1;
    //Structs
    public struct Head
    {
        public Vector2 position;
        public Vector2 direction;
        public float   speed;
        public uint    state;
        
        static public int ByteSize{get{return sizeof(float)*5+sizeof(int);}}
    }
    //Editor
    void OnGizmos()
    {
    }
    void OnValidate(){}
    //Game
    void Awake()
    {
        if( size.x % 8 != 0 ) size.x = Mathf.CeilToInt( (float)size.x / 8f ) * 8;
        if( size.y % 8 != 0 ) size.y = Mathf.CeilToInt( (float)size.y / 8f ) * 8;
        GenerateDisplay();
        GenerateTexutres();
        GenerateBuffers();
        GenerateMaps();
        displayRenderer.material.SetTexture("_MainTex", filterMapTexture);
    }

    void OnDestroy() // 
    {
        HeightMapTexture.Release();
        foreach (var buffer in headBuffers) buffer.Release();
    }
    void Update()
    {
        if (Time.frameCount % 128 == 0) ResetMaps();
        // for (int i = 0; i < 63; i++)
        DrawStep();
        if(Input.GetMouseButton(0)) displayRenderer.material.SetTexture("_MainTex", filterMapTexture);
        else displayRenderer.material.SetTexture("_MainTex", HeightMapTexture);
    }

    void DrawStep()
    {
        int DrawHeadsKID = csErrosion.FindKernel("DrawHeads");
        csErrosion.SetInt( "Frame", Time.frameCount );

        ConsumeBufferID = AppendBufferID; AppendBufferID = (AppendBufferID + 1) % 2;
        
        headBuffers[ AppendBufferID ].Release();
        headBuffers[ AppendBufferID ] = new ComputeBuffer(maxHeads, Head.ByteSize, ComputeBufferType.Append);
        headBuffers[ AppendBufferID ].SetCounterValue(0);

        csErrosion.SetBuffer( DrawHeadsKID, "HeadAppendBuffer", headBuffers[AppendBufferID]);
        csErrosion.SetBuffer( DrawHeadsKID, "HeadConsumeBuffer", headBuffers[ConsumeBufferID]);
        csErrosion.Dispatch( DrawHeadsKID, size.x / 32, size.y / 32, 1);
    }
    void ResetMaps()
    {
        int ClearID = csErrosion.FindKernel( "Clear" );
        csErrosion.SetTexture( ClearID, "FilterMap", filterMapTexture );
        csErrosion.Dispatch( ClearID, size.x / 8, size.y / 8, 1 );
        for (int i = 0; i < headBuffers.Length; i++)
        {
            headBuffers[i].Release();
            headBuffers[i] = new ComputeBuffer(maxHeads, Head.ByteSize, ComputeBufferType.Append);
            headBuffers[i].SetCounterValue(0);
        }
        csErrosion.SetInt(      "lDensity",     Random.Range( 50,    200 ) );
        csErrosion.SetFloat(    "clInertia",    Random.Range( 0.75f, 1.0f ) );
        csErrosion.SetFloat(    "slInertia",    Random.Range( 0.9f,  1.1f ) );
        csErrosion.SetFloat(    "clChaos",      Random.Range( 0.1f,  1.0f ) );
        csErrosion.SetFloat(    "slChaos",      Random.Range( 0.1f,  1.0f ) );
        csErrosion.SetFloat(    "cInertia",     Random.Range( 0.8f,  1.2f ) );
        csErrosion.SetFloat(    "sInertia",     Random.Range( 0.8f,  1.2f ) );
        csErrosion.SetFloat(    "cChaos",       Random.Range( 0.1f,  1.0f ) );
        csErrosion.SetFloat(    "sChaos",       Random.Range( 0.1f,  1.0f ) );
        int GetHeadsKID = csErrosion.FindKernel("GetHeads");
        csErrosion.Dispatch(GetHeadsKID, size.x / 8, size.y / 8, 1);
    }
    void GenerateDisplay()
    {
        var display     = new GameObject( "display" );
        var meshFilter  = display.AddComponent<MeshFilter>();
        displayRenderer = display.AddComponent<MeshRenderer>();
        meshFilter.mesh = MeshGenerator.GetQuad( size.x, size.y );

        display.transform.parent = transform;
        Camera.main.orthographicSize = size.y / 2;
        displayRenderer.material = new Material( Shader.Find( "Unlit/Texture" ) );
    }
    void GenerateTexutres()
    {
        filterMapTexture = new RenderTexture( size.x, size.y, 24 );
        filterMapTexture.enableRandomWrite = true;
        filterMapTexture.Create();
        filterMapTexture.filterMode = FilterMode.Point;

        HeightMapTexture = new RenderTexture( size.x, size.y, 24 );
        HeightMapTexture.enableRandomWrite = true;


        RenderTexture tRT = new RenderTexture(HeightMapTexture);
        Graphics.Blit( input, HeightMapTexture, initializeHeightmap);
    }
    void GenerateBuffers()
    {
        maxHeads = size.x/4*size.y/4;
        for (int i = 0; i < headBuffers.Length; i++)
        {
            headBuffers[i] = new ComputeBuffer( maxHeads, Head.ByteSize, ComputeBufferType.Append );
            headBuffers[i].SetCounterValue( 0 );
        }
    }
    void GenerateMaps()
    {
        csErrosion.SetInt(   "lDensity",    10 );
        csErrosion.SetFloat( "clInertia",   1.0f );
        csErrosion.SetFloat( "slInertia",   0.5f );
        csErrosion.SetFloat( "clChaos",     0.5f );
        csErrosion.SetFloat( "slChaos",     0.5f );
        csErrosion.SetFloat( "cInertia",    1.0f );
        csErrosion.SetFloat( "sInertia",    1.0f );
        csErrosion.SetFloat( "cChaos",      0.5f );
        csErrosion.SetFloat( "sChaos",      0.5f );

        int ClearID = csErrosion.FindKernel( "Clear" );
        csErrosion.SetTexture( ClearID, "FilterMap", filterMapTexture );
        csErrosion.Dispatch( ClearID, size.x / 8, size.y / 8, 1 );

        int GetHeadsKID = csErrosion.FindKernel("GetHeads");
        csErrosion.SetInts("Size", new int[] { size.x, size.y });
        csErrosion.SetInt("Frame", Time.frameCount);
        csErrosion.SetTexture(GetHeadsKID, "BlueNoise", blueNoise);
        csErrosion.SetBuffer(GetHeadsKID, "HeadAppendBuffer", headBuffers[AppendBufferID]);
        csErrosion.SetTexture(GetHeadsKID, "FilterMap", filterMapTexture);
        csErrosion.Dispatch(GetHeadsKID, size.x / 8, size.y / 8, 1);

        ConsumeBufferID = AppendBufferID; AppendBufferID = (AppendBufferID + 1) % 2;
        int DrawHeadsKID = csErrosion.FindKernel("DrawHeads");
        csErrosion.SetTexture(DrawHeadsKID, "FilterMap", filterMapTexture);
        csErrosion.SetBuffer(DrawHeadsKID, "HeadAppendBuffer", headBuffers[AppendBufferID]);
        csErrosion.SetBuffer(DrawHeadsKID, "HeadConsumeBuffer", headBuffers[ConsumeBufferID]);
        csErrosion.Dispatch(DrawHeadsKID, size.x / 32, size.y / 32, 1);
    }
}

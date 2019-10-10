using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LinePlotMap : MonoBehaviour
{
//  Tag                 Access      Type            Name                        Set
    [SerializeField]    private     Vector2Int      size;
    [SerializeField]    private     ComputeShader   csErrosion;
    [SerializeField]    private     int             frameCount = 320;

    [SerializeField]    private     int             lDensity                    = 10 ;
    [SerializeField]    private     float           clInertia                   = 1.0f ;
    [SerializeField]    private     float           slInertia                   = 0.5f ;
    [SerializeField]    private     float           clChaos                     = 0.5f ;
    [SerializeField]    private     float           slChaos                     = 0.5f ;
    [SerializeField]    private     float           cInertia                    = 1.0f ;
    [SerializeField]    private     float           sInertia                    = 1.0f ;
    [SerializeField]    private     float           cChaos                      = 0.5f ;
    [SerializeField]    private     float           sChaos                      = 0.5f ;

                        private     RenderTexture   lineMapTexture;
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
        displayRenderer.material.SetTexture("_MainTex", lineMapTexture);
    }

    void OnDestroy() 
    {
        lineMapTexture.Release();
        foreach (var buffer in headBuffers) buffer.Release();
    }
    private int frameOffset = 0;
    void Update()
    {
        frameCount = frameCount&0xFFFE;
        if (Input.GetKeyDown("n")) frameOffset = (Time.frameCount+1)&0xFFFE; //For some reason the frame can only be reset on even frame counts
        if ((Time.frameCount - frameOffset) % frameCount == 0) ResetMaps();
        DrawStep();
        displayRenderer.material.SetTexture("_MainTex", lineMapTexture);
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
        csErrosion.SetTexture( ClearID, "LineMap", lineMapTexture );
        csErrosion.Dispatch( ClearID, size.x / 8, size.y / 8, 1 );
        for (int i = 0; i < headBuffers.Length; i++)
        {
            headBuffers[i].Release();
            headBuffers[i] = new ComputeBuffer(maxHeads, Head.ByteSize, ComputeBufferType.Append);
            headBuffers[i].SetCounterValue(0);
        }
        csErrosion.SetInt(      "lDensity",     lDensity );
        csErrosion.SetFloat(    "clInertia",    clInertia );
        csErrosion.SetFloat(    "slInertia",    slInertia );
        csErrosion.SetFloat(    "clChaos",      clChaos );
        csErrosion.SetFloat(    "slChaos",      slChaos );
        csErrosion.SetFloat(    "cInertia",     cInertia );
        csErrosion.SetFloat(    "sInertia",     sInertia );
        csErrosion.SetFloat(    "cChaos",       cChaos );
        csErrosion.SetFloat(    "sChaos",       sChaos );
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
        lineMapTexture = new RenderTexture( size.x, size.y, 24 );
        lineMapTexture.enableRandomWrite = true;
        lineMapTexture.Create();
        lineMapTexture.filterMode = FilterMode.Point;
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
        csErrosion.SetInt(   "lDensity",    lDensity );
        csErrosion.SetFloat( "clInertia",   clInertia );
        csErrosion.SetFloat( "slInertia",   slInertia );
        csErrosion.SetFloat( "clChaos",     clChaos );
        csErrosion.SetFloat( "slChaos",     slChaos );
        csErrosion.SetFloat( "cInertia",    cInertia );
        csErrosion.SetFloat( "sInertia",    sInertia );
        csErrosion.SetFloat( "cChaos",      cChaos );
        csErrosion.SetFloat( "sChaos",      sChaos );

        int ClearID = csErrosion.FindKernel( "Clear" );
        csErrosion.SetTexture( ClearID, "LineMap", lineMapTexture );
        csErrosion.Dispatch( ClearID, size.x / 8, size.y / 8, 1 );

        int GetHeadsKID = csErrosion.FindKernel("GetHeads");
        csErrosion.SetInts("Size", new int[] { size.x, size.y });
        csErrosion.SetInt("Frame", Time.frameCount);
        csErrosion.SetBuffer(GetHeadsKID, "HeadAppendBuffer", headBuffers[AppendBufferID]);
        csErrosion.SetTexture(GetHeadsKID, "LineMap", lineMapTexture);
        csErrosion.Dispatch(GetHeadsKID, size.x / 8, size.y / 8, 1);

        ConsumeBufferID = AppendBufferID; AppendBufferID = (AppendBufferID + 1) % 2;
        int DrawHeadsKID = csErrosion.FindKernel("DrawHeads");
        csErrosion.SetTexture(DrawHeadsKID, "LineMap", lineMapTexture);
        csErrosion.SetBuffer(DrawHeadsKID, "HeadAppendBuffer", headBuffers[AppendBufferID]);
        csErrosion.SetBuffer(DrawHeadsKID, "HeadConsumeBuffer", headBuffers[ConsumeBufferID]);
        csErrosion.Dispatch(DrawHeadsKID, size.x / 32, size.y / 32, 1);
    }
}

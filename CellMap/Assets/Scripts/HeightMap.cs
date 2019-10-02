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
        // public float   speed;
        public uint    state;
        
        static public int ByteSize{get{return sizeof(float)*4+sizeof(int);}}
    }
    //Editor
    void OnGizmos()
    {
    }
    void OnValidate(){}
    //Game
    void Awake()
    {
        size.x = Mathf.CeilToInt((float)size.x/8f)*8;
        size.y = Mathf.CeilToInt((float)size.y/8f)*8;
        GenerateDisplay();
        GenerateTexutres();
        GenerateBuffers();

        csErrosion.SetInt(   "lDensity",    10   );
        csErrosion.SetFloat( "cInertia",    0.9f );
        csErrosion.SetFloat( "sInertia",    0.9f );
        csErrosion.SetFloat( "cChaos",      5.0f );
        csErrosion.SetFloat( "sChaos",      5.0f );

        int GetHeadsKID = csErrosion.FindKernel( "GetHeads" );
        csErrosion.SetInts(                     "Size",                 new int[] { size.x, size.y } );
        csErrosion.SetInt(                      "Frame",                Time.frameCount);
        csErrosion.SetTexture(  GetHeadsKID,    "BlueNoise",            blueNoise );
        csErrosion.SetBuffer(   GetHeadsKID,    "HeadAppendBuffer",     headBuffers[AppendBufferID] );
        csErrosion.SetTexture(  GetHeadsKID,    "FilterMap",            filterMapTexture );
        csErrosion.Dispatch(    GetHeadsKID, size.x / 8, size.y / 8, 1 );

        ConsumeBufferID = AppendBufferID; AppendBufferID = ( AppendBufferID + 1 ) % 2;
        int DrawHeadsKID = csErrosion.FindKernel( "DrawHeads" );
        csErrosion.SetTexture(  DrawHeadsKID,   "FilterMap",            filterMapTexture );
        csErrosion.SetBuffer(   DrawHeadsKID,   "HeadAppendBuffer",     headBuffers[AppendBufferID] );
        csErrosion.SetBuffer(   DrawHeadsKID,   "HeadConsumeBuffer",    headBuffers[ConsumeBufferID] );
        csErrosion.Dispatch(    DrawHeadsKID, size.x / 32, size.y / 32, 1 );

        displayRenderer.material.SetTexture( "_MainTex", filterMapTexture );

    }
    void OnDestroy() // 
    {
        HeightMapTexture.Release();
        foreach (var buffer in headBuffers) buffer.Release();
    }
    void Update()
    {
            if(Time.frameCount%64==0)
            {
                int ClearID = csErrosion.FindKernel( "Clear" );
                csErrosion.SetTexture(  ClearID, "FilterMap", filterMapTexture );
                csErrosion.Dispatch(    ClearID, size.x / 8, size.y / 8, 1 );
                for (int i = 0; i < headBuffers.Length; i++)
                {
                    headBuffers[i].Release();
                    headBuffers[i] = new ComputeBuffer( maxHeads, Head.ByteSize, ComputeBufferType.Append );
                    headBuffers[i].SetCounterValue( 0 );
                }
                csErrosion.SetInt(   "lDensity", Random.Range(01,255) );
                csErrosion.SetFloat( "cInertia", Random.Range(.5f,1.5f) );
                csErrosion.SetFloat( "sInertia", Random.Range(.5f,1.5f) );  
                csErrosion.SetFloat( "cChaos", Random.Range(.1f,15f) );
                csErrosion.SetFloat( "sChaos", Random.Range(.1f,15f) );
                int GetHeadsKID = csErrosion.FindKernel( "GetHeads" );
                csErrosion.Dispatch( GetHeadsKID, size.x / 8, size.y / 8, 1 );
            }
            int DrawHeadsKID = csErrosion.FindKernel( "DrawHeads" );
            csErrosion.SetInt( "Frame", Time.frameCount);
            
            ConsumeBufferID = AppendBufferID; AppendBufferID = ( AppendBufferID + 1 ) % 2;
            headBuffers[AppendBufferID].Release();
            headBuffers[AppendBufferID] = new ComputeBuffer( maxHeads, Head.ByteSize, ComputeBufferType.Append );
            headBuffers[AppendBufferID].SetCounterValue( 0 );

            csErrosion.SetBuffer(   DrawHeadsKID,   "HeadAppendBuffer",     headBuffers[AppendBufferID] );
            csErrosion.SetBuffer(   DrawHeadsKID,   "HeadConsumeBuffer",    headBuffers[ConsumeBufferID] );
            csErrosion.Dispatch(    DrawHeadsKID, size.x / 32, size.y / 32, 1 );
    }
    //Classes
    //Functions
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
        HeightMapTexture = new RenderTexture( size.x, size.y, 24 );
        filterMapTexture = new RenderTexture( size.x, size.y, 24 );
        HeightMapTexture.enableRandomWrite = true;
        filterMapTexture.enableRandomWrite = true;
        HeightMapTexture.Create();
        filterMapTexture.Create();
        filterMapTexture.filterMode = FilterMode.Point;
        Graphics.Blit( input, HeightMapTexture );
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
}

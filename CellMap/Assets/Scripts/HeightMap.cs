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
                        private     ComputeBuffer   headBuffer;
                        private     int             maxHeads;
                        private     List<Head>      heads                       = new List<Head>();
    //Structs
    public struct Head
    {
        public Vector2 position;
        public Vector2 direction;
        static public int ByteSize{get{return sizeof(float)*4;}}
    }
    //Editor
    void OnGizmos()
    {
    }
    void OnValidate(){}
    //Game
    void Awake()
    {
        GenerateDisplay();
        GenerateTexutres();
        GenerateBuffers();

        headBuffer.SetCounterValue( 0 );
        int GetHeadsKID = csErrosion.FindKernel( "GetHeads" );
        csErrosion.SetTexture(  GetHeadsKID , "BlueNoise", blueNoise );
        csErrosion.SetBuffer(   GetHeadsKID , "HeadAppendBuffer", headBuffer );
        csErrosion.SetTexture(  GetHeadsKID , "FilterMap" , filterMapTexture );
        csErrosion.Dispatch(    GetHeadsKID , size.x/8 , size.y/8 , 1 );

        headBuffer.SetCounterValue( (uint)maxHeads );
        int DrawHeadsKID = csErrosion.FindKernel( "DrawHeads" );
        csErrosion.SetTexture(  DrawHeadsKID , "FilterMap" , filterMapTexture );
        csErrosion.SetBuffer(   DrawHeadsKID , "HeadConsumeBuffer", headBuffer );
        csErrosion.Dispatch(    DrawHeadsKID , size.x/8, size.y/8, 1);
        displayRenderer.material.SetTexture( "_MainTex" , filterMapTexture );
        headBuffer.Release();
    }
    void OnDestroy()
    {
        HeightMapTexture.Release();
        headBuffer.Release();
    }
    void Update(){}
    //Classes
    //Functions
    void GenerateDisplay()
    {
        var display     = new GameObject( "display" );
        var meshFilter  = display.AddComponent<MeshFilter>();
        displayRenderer = display.AddComponent<MeshRenderer>();
        meshFilter.mesh = MeshGenerator.GetQuad( size.x , size.y );

        display.transform.parent = transform;
        Camera.main.orthographicSize = size.y / 2;
        displayRenderer.material = new Material( Shader.Find( "Unlit/Texture" ) );
    }
    void GenerateTexutres()
    {
        HeightMapTexture = new RenderTexture( size.x , size.y , 24 );
        filterMapTexture = new RenderTexture( size.x , size.y , 24 );
        HeightMapTexture.enableRandomWrite = true;
        filterMapTexture.enableRandomWrite = true;
        HeightMapTexture.Create();
        filterMapTexture.Create();
        Graphics.Blit(input,HeightMapTexture);
    }
    void GenerateBuffers()
    {
        maxHeads = size.x/8*size.y/8;
        Debug.Log("Head count: "+maxHeads);
        headBuffer = new ComputeBuffer( maxHeads , Head.ByteSize , ComputeBufferType.Append );
    }
}

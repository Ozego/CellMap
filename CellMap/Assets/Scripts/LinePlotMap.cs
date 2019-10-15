using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LinePlotMap : MonoBehaviour
{
//  Tag                 Access      Type            Name                        Set
    
    [Header("Line Map Attributes")]
    [SerializeField]    private     Boolean     UseScreenSize                   = false;
    [SerializeField]    private     Boolean     RandomizeAttributes             = false;
    [Tooltip("Size of the bitmap the lines are plotted on. \nMap size is floored to a multiple of 8")]
    [SerializeField]    private     Vector2Int  size;
    [Tooltip("Size of the hidden jittered grid the initiating lines are plotted on.")]
    [SerializeField]    private     int         gridSize                        = 256 ;
    [SerializeField]    private     float       initialSpeed                    = 2f;
    [SerializeField]    private     float       initialDirection                = 0f;
    [SerializeField]    private     float       initialDirectionVariation       = 1f;
    [Tooltip("Frame count before new simulation or next step of multi parameter simulation. \nFrame count is floored to a multiple of 2")]
    [SerializeField]    private     int         frameCount                      = 320;
    
    [Header("Line Drawing Attributes")]
    [SerializeField]    private     float       lineAcceleration                = 1.0f ;
    [SerializeField]    private     float       lineVariation                   = 0.5f ;
    [SerializeField]    private     float       lineAngularAcceleration         = 1.0f ;
    [SerializeField]    private     float       lineAngularVariation            = 0.5f ;

    [Header("Line Spawning Attributes")]
    [SerializeField]    private     float       lineSpawnAcceleration           = 0.5f ;
    [SerializeField]    private     float       lineSpawnVariation              = 0.5f ;
    [SerializeField]    private     float       lineSpawnAngularAcceleration    = 1.0f ;
    [SerializeField]    private     float       lineSpawnAngularVariation       = 0.5f ;
    [Space] [Range(0f,1f)]
    [SerializeField]    private     float       lineSpawnChance                 = 0.5f ;
    [SerializeField]    private     float       spawnAngle                      = 0.5f ;
    [SerializeField]    private     float       spawnAngleVariation             = 0.0f ;
    [SerializeField]    private     bool        spawnSymmetrically              = false ;
    [Header("Required Components")]
    [SerializeField]    private     ComputeShader   csErrosion;

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
        public float   angularMomentum;
        public uint    state;
        
        static public int ByteSize{get{return sizeof(float)*6+sizeof(int);}}
    }
    //Editor
    void OnGizmos()
    {
    }
    void OnValidate()
    {
        size.x     = size.x     & 0xFFF8; //if( size.x % 8 != 0 ) size.x = Mathf.CeilToInt( (float)size.x / 8f ) * 8;
        size.y     = size.y     & 0xFFF8; //if( size.y % 8 != 0 ) size.y = Mathf.CeilToInt( (float)size.y / 8f ) * 8;
        frameCount = frameCount & 0xFFFE; 
    }
    //Game
    void Awake()
    {
        if(UseScreenSize)
        {
            size.x = (Screen.width  + 7) & 0xFFF8;
            size.y = (Screen.height + 7) & 0xFFF8;
        }
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
        if (Input.GetKeyDown("s")) SaveTexture(lineMapTexture);
        if (Input.GetKeyDown("c")) ClearMap();
        if (Input.GetKeyDown("n")) frameOffset = (Time.frameCount+1) & 0xFFFE;    //For some reason the frame can only be reset on even frame counts
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
        ClearMap();
        SetShaderAttributes();
        int GetHeadsKID = csErrosion.FindKernel("GetHeads");
        csErrosion.Dispatch(GetHeadsKID, size.x / 8, size.y / 8, 1);
    }

    private void ClearMap()
    {
        int ClearID = csErrosion.FindKernel("Clear");
        csErrosion.SetTexture(ClearID, "LineMap", lineMapTexture);
        csErrosion.Dispatch(ClearID, size.x / 8, size.y / 8, 1);
        for (int i = 0; i < headBuffers.Length; i++)
        {
            headBuffers[i].Release();
            headBuffers[i] = new ComputeBuffer(maxHeads, Head.ByteSize, ComputeBufferType.Append);
            headBuffers[i].SetCounterValue(0);
        }
    }

    private void SetShaderAttributes()
    {
        if(RandomizeAttributes)
        {
            SetRandomShaderAttributes();
            return;
        }
        csErrosion.SetInt(   "gridSize",                        gridSize);
        csErrosion.SetFloat( "initialSpeed",                    initialSpeed);
        csErrosion.SetFloat( "initialDirection",                initialDirection);
        csErrosion.SetFloat( "initialDirectionVariation",       initialDirectionVariation);
        csErrosion.SetFloat( "lineSpawnChance",                 1f - lineSpawnChance);
        csErrosion.SetFloat( "lineAcceleration",                lineAcceleration);
        csErrosion.SetFloat( "lineSpawnAcceleration",           lineSpawnAcceleration);
        csErrosion.SetFloat( "lineVariation",                   lineVariation);
        csErrosion.SetFloat( "lineSpawnVariation",              lineSpawnVariation);
        csErrosion.SetFloat( "lineAngularAcceleration",         lineAngularAcceleration);
        csErrosion.SetFloat( "lineSpawnAngularAcceleration",    lineSpawnAngularAcceleration);
        csErrosion.SetFloat( "lineAngularVariation",            lineAngularVariation);
        csErrosion.SetFloat( "lineSpawnAngularVariation",       lineSpawnAngularVariation);
        csErrosion.SetFloat( "spawnAngle",                      spawnAngle);
        csErrosion.SetFloat( "spawnAngleVariation",             spawnAngleVariation);
        int boolArray                        = 0b0000_0000_0000_0000;
        if(spawnSymmetrically) boolArray    |= 0b0000_0000_0000_0001;
        csErrosion.SetInt(   "boolArray",                       boolArray);
    }
    
    private void SetRandomShaderAttributes()
    {
        csErrosion.SetInt(   "gridSize",                        Random.Range(0, gridSize*2));
        csErrosion.SetFloat( "initialSpeed",                    Random.Range(0, initialSpeed*2f));
        csErrosion.SetFloat( "initialDirection",                Random.Range(0, initialDirection*2f));
        csErrosion.SetFloat( "initialDirectionVariation",       Random.Range(0, initialDirectionVariation*2f));
        csErrosion.SetFloat( "lineSpawnChance",                 1f - Random.value * lineSpawnChance);
        csErrosion.SetFloat( "lineAcceleration",                Random.Range(-1f, 1f)*Mathf.Abs(1f-lineAcceleration)+1f);
        csErrosion.SetFloat( "lineSpawnAcceleration",           Random.Range(-1f, 1f)*Mathf.Abs(1f-lineSpawnAcceleration)+1f);
        csErrosion.SetFloat( "lineVariation",                   Random.Range(0, lineVariation*2f));
        csErrosion.SetFloat( "lineSpawnVariation",              Random.Range(0, lineSpawnVariation*2f));
        csErrosion.SetFloat( "lineAngularAcceleration",         Random.Range(-1f, 1f)*Mathf.Abs(1f-lineAngularAcceleration)+1f);
        csErrosion.SetFloat( "lineSpawnAngularAcceleration",    Random.Range(-1f, 1f)*Mathf.Abs(1f-lineSpawnAngularAcceleration)+1f);
        csErrosion.SetFloat( "lineAngularVariation",            Random.Range(0, lineAngularVariation*2f));
        csErrosion.SetFloat( "lineSpawnAngularVariation",       Random.Range(0, lineSpawnAngularVariation*2f));
        csErrosion.SetFloat( "spawnAngle",                      Random.Range(0, spawnAngle*2f));
        csErrosion.SetFloat( "spawnAngleVariation",             Random.Range(0, spawnAngleVariation*2f));
        int boolArray                                           =  0b0000_0000_0000_0000;
        if(Random.value > 0.5f & spawnSymmetrically) boolArray  |= 0b0000_0000_0000_0001;
        csErrosion.SetInt(   "boolArray",                       boolArray);
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
        SetShaderAttributes();
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
    void SaveTexture(RenderTexture rt)
    {
        Texture2D t2d = new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false);
        RenderTexture.active = rt;
        t2d.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);
        RenderTexture.active = null;
        byte[] b = t2d.EncodeToPNG();
        string p = String.Format( "{0}/{1}{2}.png", Application.persistentDataPath, this.name, DateTime.Now.GetHashCode() );
        System.IO.File.WriteAllBytes(p,b);
        Debug.Log("Saved file: " + p);
    }
}

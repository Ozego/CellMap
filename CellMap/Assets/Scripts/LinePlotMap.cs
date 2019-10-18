using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LinePlotMap : MonoBehaviour
{
//  Tag                 Access      Type            Name                        Set
    
    [Header("Line Map Attributes")]
    [SerializeField]    private     Boolean     useScreenSize                   = false;
    [SerializeField]    private     Boolean     randomizeAttributes             = false;
    [Tooltip("Size of the bitmap the lines are plotted on. \nMap size is floored to a multiple of 8")]
    [SerializeField]    private     Vector2Int  size;
    [SerializeField]    private     plotMethods plotMethod;
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
    [Header("Droplets")]
    [SerializeField]    private     TextAsset       loadFile;


                        private     RenderTexture   lineMapTexture;
                        private     MeshRenderer    displayRenderer;
                        private     ComputeBuffer[] headBuffers                 = new ComputeBuffer[2];
                        private     List<Head>      heads                       = new List<Head>();
                        private     int             maxHeads;
                        private     int ConsumeBufferID = 0, 
                                        AppendBufferID  = 1;
                        private     SaveState       presentSaveState            = new SaveState();
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
    private enum plotMethods
    {
        GetHead,
        GetHeadsOnLine,
        GetHeadsOnGrid
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
        if(loadFile!=null)
        {
            LoadAttributes();
            loadFile = null;
        }
    }

    private void LoadAttributes()
    {
        var loadState = JsonUtility.FromJson<SaveState>(loadFile.text);
        gridSize                                        = loadState.gridSize;
        initialSpeed                                    = loadState.initialSpeed;
        initialDirection                                = loadState.initialDirection;
        initialDirectionVariation                       = loadState.initialDirectionVariation;
        lineSpawnChance                                 = 1f - loadState.lineSpawnChance;
        lineAcceleration                                = loadState.lineAcceleration;
        lineSpawnAcceleration                           = loadState.lineSpawnAcceleration;
        lineVariation                                   = loadState.lineVariation;
        lineSpawnVariation                              = loadState.lineSpawnVariation;
        lineAngularAcceleration                         = loadState.lineAngularAcceleration;
        lineSpawnAngularAcceleration                    = loadState.lineSpawnAngularAcceleration;
        lineAngularVariation                            = loadState.lineAngularVariation;
        lineSpawnAngularVariation                       = loadState.lineSpawnAngularVariation;
        spawnAngle                                      = loadState.spawnAngle;
        spawnAngleVariation                             = loadState.spawnAngleVariation;
        int b1                                          = 0b0000_0000_0000_0001; 
        spawnSymmetrically                              = ( loadState.boolArray & b1 ) == b1;
    }

    //Game
    void Awake()
    {
        if(useScreenSize)
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
        int GetHeadsKID = csErrosion.FindKernel(plotMethod.ToString());
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
        SetSaveStateAttributes();
        csErrosion.SetInt(   "gridSize",                        presentSaveState.gridSize );
        csErrosion.SetFloat( "initialSpeed",                    presentSaveState.initialSpeed );
        csErrosion.SetFloat( "initialDirection",                presentSaveState.initialDirection );
        csErrosion.SetFloat( "initialDirectionVariation",       presentSaveState.initialDirectionVariation );
        csErrosion.SetFloat( "lineSpawnChance",                 presentSaveState.lineSpawnChance );
        csErrosion.SetFloat( "lineAcceleration",                presentSaveState.lineAcceleration );
        csErrosion.SetFloat( "lineSpawnAcceleration",           presentSaveState.lineSpawnAcceleration );
        csErrosion.SetFloat( "lineVariation",                   presentSaveState.lineVariation );
        csErrosion.SetFloat( "lineSpawnVariation",              presentSaveState.lineSpawnVariation );
        csErrosion.SetFloat( "lineAngularAcceleration",         presentSaveState.lineAngularAcceleration );
        csErrosion.SetFloat( "lineSpawnAngularAcceleration",    presentSaveState.lineSpawnAngularAcceleration );
        csErrosion.SetFloat( "lineAngularVariation",            presentSaveState.lineAngularVariation );
        csErrosion.SetFloat( "lineSpawnAngularVariation",       presentSaveState.lineSpawnAngularVariation );
        csErrosion.SetFloat( "spawnAngle",                      presentSaveState.spawnAngle );
        csErrosion.SetFloat( "spawnAngleVariation",             presentSaveState.spawnAngleVariation );
        csErrosion.SetInt(   "boolArray",                       presentSaveState.boolArray );
    }
    
    private void SetSaveStateAttributes()
    {
        if(randomizeAttributes)
        {
            SetRandomSaveStateAttributes();
            return;
        }
        presentSaveState.gridSize                       = gridSize;
        presentSaveState.initialSpeed                   = initialSpeed;
        presentSaveState.initialDirection               = initialDirection;
        presentSaveState.initialDirectionVariation      = initialDirectionVariation;
        presentSaveState.lineSpawnChance                = 1f - lineSpawnChance;
        presentSaveState.lineAcceleration               = lineAcceleration;
        presentSaveState.lineSpawnAcceleration          = lineSpawnAcceleration;
        presentSaveState.lineVariation                  = lineVariation;
        presentSaveState.lineSpawnVariation             = lineSpawnVariation;
        presentSaveState.lineAngularAcceleration        = lineAngularAcceleration;
        presentSaveState.lineSpawnAngularAcceleration   = lineSpawnAngularAcceleration;
        presentSaveState.lineAngularVariation           = lineAngularVariation;
        presentSaveState.lineSpawnAngularVariation      = lineSpawnAngularVariation;
        presentSaveState.spawnAngle                     = spawnAngle;
        presentSaveState.spawnAngleVariation            = spawnAngleVariation;
        int boolArray                                   = 0b0000_0000_0000_0000;
        if(spawnSymmetrically) boolArray               |= 0b0000_0000_0000_0001;
        presentSaveState.boolArray                      = boolArray;
    }
    
    private void SetRandomSaveStateAttributes()
    {
        presentSaveState.gridSize                       = Random.Range(0, gridSize*2);
        presentSaveState.initialSpeed                   = Random.Range(0, initialSpeed*2f);
        presentSaveState.initialDirection               = Random.Range(0, initialDirection*2f);
        presentSaveState.initialDirectionVariation      = Random.Range(0, initialDirectionVariation*2f);
        presentSaveState.lineSpawnChance                = 1f - Random.value * lineSpawnChance;
        presentSaveState.lineAcceleration               = Random.Range(-1f, 1f)*Mathf.Abs(1f-lineAcceleration)+1f;
        presentSaveState.lineSpawnAcceleration          = Random.Range(-1f, 1f)*Mathf.Abs(1f-lineSpawnAcceleration)+1f;
        presentSaveState.lineVariation                  = Random.Range(0, lineVariation*2f);
        presentSaveState.lineSpawnVariation             = Random.Range(0, lineSpawnVariation*2f);
        presentSaveState.lineAngularAcceleration        = Random.Range(-1f, 1f)*Mathf.Abs(1f-lineAngularAcceleration)+1f;
        presentSaveState.lineSpawnAngularAcceleration   = Random.Range(-1f, 1f)*Mathf.Abs(1f-lineSpawnAngularAcceleration)+1f;
        presentSaveState.lineAngularVariation           = Random.Range(0, lineAngularVariation*2f);
        presentSaveState.lineSpawnAngularVariation      = Random.Range(0, lineSpawnAngularVariation*2f);
        presentSaveState.spawnAngle                     = Random.Range(0, spawnAngle*2f);
        presentSaveState.spawnAngleVariation            = Random.Range(0, spawnAngleVariation*2f);
        int boolArray                                   = 0b0000_0000_0000_0000;
        if(Random.value > 0.5f & spawnSymmetrically) 
            boolArray                                  |= 0b0000_0000_0000_0001;
        presentSaveState.boolArray                      = boolArray;
    }
    private class SaveState
    {
        public int     gridSize;
        public float   initialSpeed;
        public float   initialDirection;
        public float   initialDirectionVariation;
        public float   lineSpawnChance;
        public float   lineAcceleration;
        public float   lineSpawnAcceleration;
        public float   lineVariation;
        public float   lineSpawnVariation;
        public float   lineAngularAcceleration;
        public float   lineSpawnAngularAcceleration;
        public float   lineAngularVariation;
        public float   lineSpawnAngularVariation;
        public float   spawnAngle;
        public float   spawnAngleVariation;
        public int     boolArray;
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

        int GetHeadsKID = csErrosion.FindKernel(plotMethod.ToString());
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
        byte[]  b = t2d.EncodeToPNG();
        string  p = String.Format( "{0}/{1}{2}", Application.persistentDataPath, this.name, DateTime.Now.GetHashCode().ToString("X4") );
        string ss = JsonUtility.ToJson(presentSaveState, true);
        System.IO.File.WriteAllBytes(p+".png",b);
        System.IO.File.WriteAllText(p+".json",ss);
        Debug.Log("Saved file: " + p);
    }
}

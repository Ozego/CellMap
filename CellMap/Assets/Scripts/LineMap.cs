using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
public class LineMap : MonoBehaviour
{
    // itterative line renderer 
    // -------------------------------------------------------------------------
    // -------------------------------------------------------------------------
    // fish scale pattern 
    // CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
    // CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC
    //dragon scale pattern 
    // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
    // <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
    // waves of joy division formula 
    // =========================================================================
    // =========================================================================
    // lovecrafian eyes 
    // <o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><o><
    // negative crosshatch ex:
    // - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
    //  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 
    // Start is called before the first frame update

    //think sewing machine
    // straightlines with rounded edges

    //render data

    [SerializeField] Vector2Int mSize; 
    [SerializeField] private int frameCount;
    // [SerializeField] private Shader sLine;
    [SerializeField] private ComputeShader csNoise;
    [SerializeField] private ComputeShader csLine;
    [SerializeField] private string seed;

    private MeshRenderer mRenderer;
    private GameObject qDisplay;
    private List<RenderTexture> rTextures = new List<RenderTexture>();
    private int rFrame = 0; 

    private List<Line> lines = new List<Line>();
    private int lineCount;
    private ComputeBuffer lineBuffer;
    private int threadX = 8;

    public struct Line 
    {
        public Vector2 _start;
        public Vector2 _end;
        public Color _color;

        public Vector2 localVector{ get{ return _end - _start; } }
        public static int byteSize{ get{ return sizeof(float)*8; } }
    }
    void Start()
    {
        if(seed==""|seed==null)
        {
            seed = System.DateTime.Now.ToString();
        }
        Random.InitState(seed.GetHashCode());
        GenerateDisplay();
        GenerateLines();
        lineBuffer = new ComputeBuffer(lineCount, Line.byteSize, ComputeBufferType.Default);
        ComputeStepFrame();
    }
    void OnDestroy()
    {
        lineBuffer.Release();
    }
    private void Update()
    {
        updateRFrame();
        // int noiseKernelID = csNoise.FindKernel("DrawNoise");
        // csNoise.SetInt("Frame", Time.frameCount);
        // csNoise.SetTexture(noiseKernelID, "OutTexture", rTextures[rFrame]);
        // csNoise.Dispatch(noiseKernelID, mSize.x/8, mSize.y/8, 1);
        int rasterizeKernelID = csLine.FindKernel("DrawLine");
        int moveKernelID = csLine.FindKernel("MoveLines");
        csLine.SetBuffer(rasterizeKernelID, "LineBuffer", lineBuffer);
        csLine.Dispatch(moveKernelID, lineCount / threadX, 1, 1);
        // csLine.SetBuffer(rasterizeKernelID, "LineBuffer", lineBuffer);
        csLine.SetTexture(rasterizeKernelID, "OutTexture", rTextures[rFrame]);
        csLine.Dispatch(rasterizeKernelID, lineCount / threadX, 1, 1);

        mRenderer.material.SetTexture("_MainTex", rTextures[rFrame]); 
    }

    private void ComputeStepFrame()
    {
        // int noiseKernelID = csNoise.FindKernel("DrawNoise");
        // csNoise.SetTexture(noiseKernelID, "OutTexture", rTextures[rFrame]);
        // csNoise.Dispatch(noiseKernelID, mSize.x/8, mSize.y/8, 1);
        //recompile
        
        lineBuffer.SetData(lines);
        int rasterizeKernelID = csLine.FindKernel("DrawLine");

        csLine.SetInt("width", mSize.x);
        csLine.SetInt("height", mSize.y);
        csLine.SetBuffer(rasterizeKernelID, "LineBuffer", lineBuffer);

        csLine.SetTexture(rasterizeKernelID, "OutTexture", rTextures[rFrame]);
        csLine.Dispatch(rasterizeKernelID, lineCount / threadX, 1, 1);

        mRenderer.material.SetTexture("_MainTex", rTextures[rFrame]); 
    }


    private void GenerateLines()
    {
        Vector2 pos = new Vector2(mSize.x/2f,mSize.y/2f);
        for (int i = 0; i < 500; i++) //500000 limit
        {
            var mLine = new Line();
            mLine._start = pos;
            pos += new Vector2(Random.value*50f-25f,Random.value*50f-25f);
            mLine._end = pos;
            mLine._color = Random.ColorHSV(0f,1f,.5f,1f,1f,1f);
            lines.Add(mLine);
        }
        cielLineCount();
    }

    private void cielLineCount()
    {
        lineCount = lines.Count;
        if((lineCount % threadX) > 0)
        {
            lineCount += threadX - (lineCount % threadX);
        }
        // Debug.Log("Actual Line count: " + lines.Count);
        // Debug.Log("Line count for gpu: " + lineCount);
    }
    private void updateRFrame()
    {
        rFrame++;
        rFrame = rFrame%rTextures.Count;
    }


    void OnDrawGizmos()
    {
            // Gizmos.color = Color.red;
            // foreach (var line in lines)
            // {
            //     Gizmos.DrawLine
            //     (
            //         new Vector3(line._start.x-mSize.x/2f,0f,line._start.y-mSize.y/2f),
            //         new Vector3(line._end.x-mSize.x/2f,0f,line._end.y-mSize.y/2f)
            //     );
            // }
    }

    private void GenerateDisplay()
    {
        //Boilerplate display 1px Rendertexture per 1 unit 
        Camera.main.orthographicSize = mSize.y / 2;
        qDisplay = new GameObject("Display");
        qDisplay.transform.parent = transform;
        var mFilter = qDisplay.AddComponent<MeshFilter>();
        mRenderer = qDisplay.AddComponent<MeshRenderer>();
        var mGen = new MeshGenerator();
        mFilter.mesh = mGen.GetQuad(mSize.x, mSize.y);
        mRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        
        for (int i = 0; i < frameCount; i++)
        {
            var rT = new RenderTexture(mSize.x, mSize.y, 8);
            rT.enableRandomWrite = true;
            rT.filterMode = FilterMode.Point;
            rT.Create();
            rTextures.Add( rT );
        }
        mRenderer.material.SetTexture("_MainTex", rTextures[0]);
    }


}

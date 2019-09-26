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
    //think sewing machine
    // straightlines with rounded edges


    [SerializeField] private string seed;
    [SerializeField] private ComputeShader csNoise;
    [SerializeField] private ComputeShader csLine;


    [SerializeField] Vector2Int mSize; 
    private MeshRenderer mRenderer;
    private GameObject qDisplay;

    private int cFrameCount = 2;
    private int cFramePointer = 0; 
    private int threadSize = 8;

    private List<RenderTexture> rTextures = new List<RenderTexture>();
    private List<ComputeBuffer> lineBuffers = new List<ComputeBuffer>();

    private List<Line> lines = new List<Line>();
    private int lineCount;

    public struct Line 
    {
        public Vector2 _start;
        public Vector2 _end;
        public Color   _color;

        public Vector2 localVector{ get{ return _end - _start; } }
        public static int byteSize{ get{ return sizeof(float)*8; } }
    }
    // void OnDrawGizmos()
    // {
    // }
    void Start()
    {
        if (seed == "" | seed == null)
        {
            seed = System.DateTime.Now.ToString();
        }
        Random.InitState(seed.GetHashCode());
        GenerateDisplay();
        GenerateRenderTextures();
        GenerateLines();
        GenerateBuffers();
        ComputeStepFrame();
    }


    void OnDestroy()
    {
        for (int i = 0; i < cFrameCount; i++)
        {
            lineBuffers[i].Release();
        }
    }
    private void Update()
    {
        
        // // int noiseKernelID = csNoise.FindKernel("DrawNoise");
        // // csNoise.SetInt("Frame", Time.cFrameCount);
        // // csNoise.SetTexture(noiseKernelID, "OutTexture", rTextures[cFramePointer]);
        // // csNoise.Dispatch(noiseKernelID, mSize.x/8, mSize.y/8, 1);
        csLine.SetInt("frame", Time.frameCount);
    
        int clearKernelID = csLine.FindKernel("Clear");
        csLine.SetTexture(clearKernelID, "OutTexture", rTextures[0]);
        // csLine.Dispatch(clearKernelID, mSize.x/threadSize, mSize.y/threadSize, 1);

        // updatecFramePointer();

        int rasterizeKernelID = csLine.FindKernel("DrawLine");
        csLine.SetBuffer(rasterizeKernelID, "LineBuffer", lineBuffers[0]);
        csLine.SetTexture(rasterizeKernelID, "OutTexture", rTextures[0]);
        csLine.Dispatch(rasterizeKernelID, lineCount / threadSize, 1, 1);

        mRenderer.material.SetTexture("_MainTex", rTextures[0]); 

        int moveKernelID = csLine.FindKernel("MoveLines");
        csLine.SetBuffer(moveKernelID, "LineBuffer", lineBuffers[0]);
        csLine.Dispatch(moveKernelID, lineCount / threadSize, 1, 1);
    }

    private void ComputeStepFrame()
    {
        lineBuffers[cFramePointer].SetData(lines);
        int rasterizeKernelID = csLine.FindKernel("DrawLine");

        csLine.SetInt("width", mSize.x);
        csLine.SetInt("height", mSize.y);
        csLine.SetBuffer(rasterizeKernelID, "LineBuffer", lineBuffers[0]);

        csLine.SetTexture(rasterizeKernelID, "OutTexture", rTextures[0]);
        csLine.Dispatch(rasterizeKernelID, lineCount / threadSize, 1, 1);

        mRenderer.material.SetTexture("_MainTex", rTextures[0]); 
    }


    private void updatecFramePointer()
    {
        cFramePointer++;
        cFramePointer = cFramePointer%rTextures.Count;
    }


    //
    private void GenerateDisplay()
    {
        //Boilerplate display 1px Rendertexture per 1 unit 
        Camera.main.orthographicSize = mSize.y / 2;
        //Gameobject
        qDisplay = new GameObject("Display");
        qDisplay.transform.parent = transform;
        //Mesh Renderer
        var mFilter = qDisplay.AddComponent<MeshFilter>();
        mRenderer = qDisplay.AddComponent<MeshRenderer>();
        var mGen = new MeshGenerator();
        mFilter.mesh = mGen.GetQuad(mSize.x, mSize.y);
        mRenderer.material = new Material(Shader.Find("Unlit/Texture"));
    }
    private void GenerateRenderTextures()
    {
        //RendecFrames
        for (int i = 0; i < cFrameCount; i++)
        {
            var rT = new RenderTexture(mSize.x, mSize.y, 8);
            rT.enableRandomWrite = true;
            rT.filterMode = FilterMode.Trilinear;
            rT.Create();
            rTextures.Add(rT);
        }
        mRenderer.material.SetTexture("_MainTex", rTextures[0]);
    }
    private void GenerateLines()
    {
        Vector2 pos = new Vector2( mSize.x/2f, mSize.y/2f );
        for (int i = 0; i < 4096; i++) //500000 limit
        {
            var mLine = new Line();
            mLine._start = pos;
            pos += new Vector2( Random.value*4f-2f, Random.value*4f-2f );
            mLine._end = pos;
            mLine._color = Random.ColorHSV(0f,1f,.5f,1f,1f,1f);
            lines.Add(mLine);
        }
        cielLineCount();
    }
    private void cielLineCount()
    {
        lineCount = lines.Count;
        if((lineCount % threadSize) > 0)
        {
            lineCount += threadSize - (lineCount % threadSize);
        }
    }
    private void GenerateBuffers()
    {
        for (int i = 0; i < cFrameCount; i++)
        {
            var lBuffer = new ComputeBuffer
            (
                lineCount, 
                Line.byteSize, 
                ComputeBufferType.Default
            );
            lineBuffers.Add(lBuffer);
        }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
public class LineParticleMap : MonoBehaviour
{
//  Tag                 Access      Type                Name                    Set

    [SerializeField]    private     string              seed;
    [SerializeField]    private     ComputeShader       csNoise;
    [SerializeField]    private     ComputeShader       csLine;
    [SerializeField]    private     Vector2Int          mSize;
    
                        private     MeshRenderer        mRenderer;
                        private     GameObject          qDisplay;
                        private     List<RenderTexture> rTextures               = new List<RenderTexture>();

                        private     List<ComputeBuffer> lineBuffers             = new List<ComputeBuffer>();
                        private     List<Line>          lines                   = new List<Line>();
                        private     int                 lineCount;

                        private     int                 cFrameCount             = 2;
                        private     int                 cFramePointer           = 0; 
                        private     int                 threadSize              = 8;

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

        csLine.SetInt("frame", Time.frameCount);

        int fadeKernelID = csLine.FindKernel("Fade");
        csLine.SetTexture(fadeKernelID, "OutTexture", rTextures[0]);
        csLine.Dispatch(fadeKernelID, mSize.x/threadSize, mSize.y/threadSize, 1);

        if(Input.GetMouseButtonDown(0))
        {
            csLine.SetFloat("Chaos",Random.value*Random.value*20f);
            int clearKernelID = csLine.FindKernel("Clear");
            csLine.SetTexture(clearKernelID, "OutTexture", rTextures[0]);
            csLine.Dispatch(clearKernelID, mSize.x/threadSize, mSize.y/threadSize, 1);
            int alignKernelID = csLine.FindKernel("AlignLines");
            csLine.SetBuffer(alignKernelID, "LineBuffer", lineBuffers[0]);
            csLine.Dispatch(alignKernelID, lineCount / threadSize, 1, 1);
        }   
        
        int rasterizeKernelID = csLine.FindKernel("DrawLine");
        int moveKernelID = csLine.FindKernel("MoveLines");
        // updatecFramePointer();
        for (int i = 0; i < 1; i++)
        {
            csLine.SetBuffer(rasterizeKernelID, "LineBuffer", lineBuffers[0]);
            csLine.SetTexture(rasterizeKernelID, "OutTexture", rTextures[0]);
            csLine.Dispatch(rasterizeKernelID, lineCount / threadSize, 1, 1);

            mRenderer.material.SetTexture("_MainTex", rTextures[0]); 

            csLine.SetBuffer(moveKernelID, "LineBuffer", lineBuffers[0]);
            csLine.Dispatch(moveKernelID, lineCount / threadSize, 1, 1);
        }
    }

    private void ComputeStepFrame()
    {
        lineBuffers[cFramePointer].SetData(lines);
        int rasterizeKernelID = csLine.FindKernel("DrawLine");
        csLine.SetInt("width", mSize.x);
        csLine.SetInt("height", mSize.y);
        csLine.SetFloat("chaos",1f);
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
        mFilter.mesh = MeshGenerator.GetQuad(mSize.x, mSize.y);
        mRenderer.material = new Material(Shader.Find("Unlit/Texture"));
    }
    private void GenerateRenderTextures()
    {
        //RendecFrames
        for (int i = 0; i < cFrameCount; i++)
        {
            var rT = new RenderTexture(mSize.x, mSize.y, 8);
            rT.enableRandomWrite = true;
            rT.filterMode = FilterMode.Point;
            rT.Create();
            rTextures.Add(rT);
        }
        mRenderer.material.SetTexture("_MainTex", rTextures[0]);
    }
    private void GenerateLines()
    {
        Vector2 pos = new Vector2( mSize.x/2f, mSize.y/2f );
        for (int i = 0; i < 1000; i++) //500000 limit
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

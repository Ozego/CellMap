using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
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
    private MeshRenderer mRenderer;
    private GameObject qDisplay;
    private List<RenderTexture> rTextures = new List<RenderTexture>();
    [SerializeField]private int frameCount;
    private int rFrame; 

    [SerializeField] private Shader sLine;
    [SerializeField] private ComputeShader csLine;
    private List<Line> lines = new List<Line>();
    private ComputeBuffer lineBuffer;

    public class Line
    {
        public Vector2 _start;
        public Vector2 _end;
        public Vector4 _color;
        Line(Vector2 start, Vector2 end, Color color)
        {
            _start = start;
            _end = end;
            _color = color;
        }
        public Vector2 localVector
        {
            get 
            {
                return _end - _start;
            }
        }
    }
    private void updateRFrame()
    {
        rFrame++;
        rFrame = rFrame%rTextures.Count;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void GenerateDisplay()
    {
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
            var rT = new RenderTexture(mSize.x, mSize.y, 24);
            rT.enableRandomWrite = true;
            rT.filterMode = FilterMode.Point;
            rT.Create();
            rTextures.Add( rT );
        
        }
        mRenderer.material.SetTexture("_MainTex", rTextures[0]);
    }
}

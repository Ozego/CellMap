using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeMap : MonoBehaviour
{
    [SerializeField] private int width = 256, height = 256;
    [SerializeField] private ComputeShader shader;
    [SerializeField] private ComputeShader gameOfLifeShader;
    [SerializeField] private Texture2D initialTexture;

    private int windowRange = 8;
    private int[,] map; 
    private MeshRenderer mRenderer;
    private RenderTexture[] rTextures;
    private GameObject DisplayQuad;
    private uint frameCount = 2;
    private uint frame = 0;

    void Start()
    {
        GenerateDisplay();
        int kernelID = shader.FindKernel("CSMain");
        Graphics.Blit(initialTexture,rTextures[0]);
        Graphics.Blit(initialTexture,rTextures[1]);
        // shader.SetTexture(kernelID, "Result", rTextures[0]);
        // shader.Dispatch(kernelID, width/windowRange, height/windowRange,1);
        // mRenderer.material.SetTexture("_MainTex", rTexture);
    }
    void Update()
    {
        uint pastFrame = frame; 
        frame++; // Advance frame
        frame = frame%frameCount; // Loop
        int kernelID = gameOfLifeShader.FindKernel("GameOfLife");
        gameOfLifeShader.SetInt("width", width);
        gameOfLifeShader.SetInt("height", height);
        gameOfLifeShader.SetTexture(kernelID, "Prev", rTextures[pastFrame]);
        gameOfLifeShader.SetTexture(kernelID, "Result", rTextures[frame]);
        gameOfLifeShader.Dispatch(kernelID, width/windowRange, height/windowRange,1);
        mRenderer.material.SetTexture("_MainTex", rTextures[frame]);
        if(Time.frameCount%1024==1 | Input.GetMouseButtonDown(0))
        {
            var tTexture = new Texture2D(width,height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tTexture.SetPixel(x,y,Color.red*Random.Range(0f,.53f));
                }
            }
            tTexture.Apply();
            Graphics.Blit(tTexture,rTextures[0]);
            Graphics.Blit(tTexture,rTextures[1]);
        }
    }

    private void GenerateDisplay()
    {
        Camera.main.orthographicSize = height / 2;
        DisplayQuad = new GameObject("Display");
        DisplayQuad.transform.parent = transform;
        var mFilter = DisplayQuad.AddComponent<MeshFilter>();
        mRenderer = DisplayQuad.AddComponent<MeshRenderer>();
        var mGen = new MeshGenerator();
        mFilter.mesh = mGen.GetQuad(width, height);
        mRenderer.material = new Material(Shader.Find("Unlit/Texture"));

        rTextures = new RenderTexture[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            rTextures[i] = new RenderTexture(width, height, 24);
            rTextures[i].enableRandomWrite = true;
            rTextures[i].filterMode = FilterMode.Point;
            rTextures[i].Create();
        }
        mRenderer.material.SetTexture("_MainTex", rTextures[0]);
    }

}

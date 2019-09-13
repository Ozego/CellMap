using UnityEngine;

public class CellGrid : MonoBehaviour
{
    [SerializeField] private int width = 256, height = 256;
    [SerializeField][Range(0,100)] private int fillProbability = 50;
    [SerializeField] private string seed;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private bool displayGizmos = false;

    private int[,] map;

    void Start()
    {
        GenerateMap();
        // Display("Display");
    }
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) Smooth();
        // for (int x = 0; x < width; x++)
        // {
        //     for (int y = 0; y < height; y++)
        //     {
        //         map[x,y] = map[GetXTiled(x+1),GetYTiled(y+1)];
        //     }
        // }
    }
    
    void GenerateMap()
    {
        map = new int[width, height];
        // RandomFill();
        RectFill(64,8);
        var buffer = (int[,])map.Clone();;
        RectFill(8,64);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x,y] = Mathf.Max(map[x,y],buffer[x,y]);
            }
        }
        buffer = (int[,])map.Clone();;
        RandomFill();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x,y] = Mathf.Max(map[x,y],buffer[x,y]);
            }
        }
        // for (int i = 0; i < 32; i++)
        // {
        //     Smooth();
        // }
    }

    void RectFill(int fillWidth, int fillHeight, int xPos, int yPos)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[GetXTiled(x+xPos), GetYTiled(y+yPos)] = 
                      x > width /2 - fillWidth 
                    & x < width /2 + fillWidth 
                    & y > height/2 - fillHeight 
                    & y < height/2 + fillHeight 
                    ? 1 : 0;
            }
        }
    }
    void RectFill(int fillWidth, int fillHeight)
    {
        RectFill(fillWidth,fillHeight,0,0);
    }

    void RandomFill()
    {
        if (useRandomSeed) seed = System.DateTime.Now.GetHashCode().ToString();
        System.Random psuedoRNG = new System.Random(seed.GetHashCode());
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x,y] = psuedoRNG.Next(100) < fillProbability ? 1 : 0;
            }
        }
    }
    void Smooth()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x,y] = GetMooreCount(x,y,2) > 12 ? 1 : 0;
                // map[x,y] = GetMooreCount(x,y,1) > 4 ? 1 : 0;
                // map[x,y] = GetMooreCount(x,y,2) > 12 & GetMooreCount(x,y,1) > 4 ? 1 : 0;
                // map[x,y] = GetVonNeumannCount(x,y,2) > 6 ? 1 : 0;
                // map[x,y] = GetVonNeumannCount(x,y,3) > 12 ? 1 : 0;
                // map[x,y] = GetVonNeumannCount(x,y,3) > 12 | GetMooreCount(x,y,1) > 5 ? 1 : 0;
            }
        }
    }

    int GetMooreCount(int x, int y, int range) //Moore Neighborhood
    {
        int count = 0;
        for (int neighborX = x-range; neighborX <= x+range; neighborX++)
        {
            for (int neighborY = y-range; neighborY <= y+range; neighborY++)
            {
                count += map[GetXTiled(neighborX), GetYTiled(neighborY)] > 0 ? 1 : 0;
            }
        }
        return count;
    }
    int GetVonNeumannCount(int x, int y, int range) //Von Neumann Neighborhood
    {
        int count = 0;
        for (int neighborX = x-range; neighborX <= x+range; neighborX++)
        {
            int rangeY = range - System.Math.Abs(x-neighborX);
            for (int neighborY = y-rangeY; neighborY <= y+rangeY; neighborY++)
            {
                count += map[GetXTiled(neighborX), GetYTiled(neighborY)] > 0 ? 1 : 0;
            }
        }
        return count;
    }

    private int GetXTiled(int x)
    {
        return (x % width + width) % width;
    }
    private int GetYTiled(int y)
    {
        return (y % height + height) % height;
    }

    void OnDrawGizmos()
    {
        if(map!=null&displayGizmos)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color = map[x,y] == 1 ? Color.black : Color.red;
                    Vector3 position = new Vector3
                    ( 
                        -width / 2 + x + .5f, 
                        .5f, 
                        -height / 2 + y + .5f
                    );
                    Gizmos.DrawCube(position,Vector3.one*1f);
                }
            }
        }
    }

    private GameObject DisplayQuad;
    void Display(string name)
    {
        DisplayQuad = new GameObject(name);
        DisplayQuad.transform.parent = transform;
        var mFilter = DisplayQuad.AddComponent<MeshFilter>();
        var mRenderer = DisplayQuad.AddComponent<MeshRenderer>();
        var mGen = new MeshGenerator();
        mFilter.mesh = mGen.GetQuad(width, height);
        mRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        var displayTexture = new Texture2D(width,height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                displayTexture.SetPixel(x,y,map[x,y] == 1 ? Color.black : Color.red);
            }
        }
        displayTexture.Apply();
        mRenderer.material.SetTexture("_MainTex", displayTexture);
    }
    
}

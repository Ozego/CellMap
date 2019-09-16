using UnityEngine;

public class CellMap : MonoBehaviour
{
    [SerializeField] private int width = 256, height = 256;
    [SerializeField] private int windowRange = 8;
    [SerializeField][Range(0,100)] private int fillProbability = 50;
    [SerializeField] private string seed;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private bool displayGizmos = false;
    [SerializeField] private ComputeShader smoothMapShader;
    [SerializeField] private ComputeBuffer cBuffer;

    private int[,] map; 
    private Texture2D dTexture;
    private RenderTexture rTexture;


    void Start()
    {
        Camera.main.orthographicSize = height/2;
        GenerateMap();
        Display();
    }
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) 
        {
            GenerateMap(); 
            UpdateDisplayTexture();
        };
        for (int i = 0; i < 4; i++)
        {
            Smooth();
        }
        if(Time.frameCount%512==0) fillProbability = (int)Random.Range(0,4);
        if(Time.frameCount%128==0) GenerateMap();
        if(Time.frameCount%64 ==0) SmoothDarken();
        if(Time.frameCount%12 ==0) for (int i = 0; i < 4; i++) SmoothLighten();
        UpdateDisplayTexture();
    }
    
    void GenerateMap()
    {
        map = new int[width, height];
        RectFill(32,16);
        RandomFill();
        RectFill( width*(int)Random.Range(8,24)/64,width*4/64 ,0,(int)Random.Range(-8,8) );
        var buffer = (int[,])map.Clone();;
        RectFill( height*4/64, height*(int)Random.Range(8,24)/64 );
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x,y] = Mathf.Max(map[x,y],buffer[x,y]);
            }
        }
        buffer = (int[,])map.Clone();;
        int pillarCount = Random.Range(0,5);
        for (int i = -pillarCount; i <= pillarCount; i++)
        {
            RectFill( height*4/64, height*4/64, i*16,height/2 );
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    map[x,y] = Mathf.Max(map[x,y],buffer[x,y]);
                }
            }
            buffer = (int[,])map.Clone();;
        }
        RandomFill();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x,y] = Mathf.Max(map[x,y],buffer[x,y]);
            }
        }
        // for (int i = 0; i < 0; i++)
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
    void SmoothLighten()
    {
        var bufferMap = (int[,])map.Clone();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bufferMap[x,y] = GetVonNeumannCount(x,y,3) > 16 | GetVonNeumannCount(x,y,5) < 16 ? 0 : 1;
            }
        }
        map = (int[,])bufferMap.Clone();
    }
    void SmoothDarken()
    {
        var bufferMap = (int[,])map.Clone();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bufferMap[x,y] = ( GetMooreCount(x,y,2) > 12 & GetMooreCount(x,y,1) > 3 ) || GetMooreCount(x,y,4) < 1 ? 1 : 0;
            }
        }
        map = (int[,])bufferMap.Clone();
    }
    void Smooth()
    {
        var bufferMap = (int[,])map.Clone();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // bufferMap[x,y] = GetOneCount(x,y) > 4  ? 1 : 0;
                // bufferMap[x,y] = GetMooreCount(x,y,1) > 4 ? 1 : 0;
                // bufferMap[x,y] = GetMooreCount(x,y,2) > 12 ? 1 : 0;
                // bufferMap[x,y] = GetMooreCount(x,y,2) > 12 & GetMooreCount(x,y,1) > 3 ? 1 : 0;
                // bufferMap[x,y] = ( GetMooreCount(x,y,2) > 12 & GetMooreCount(x,y,1) > 3 ) || GetMooreCount(x,y,4) < 1 ? 1 : 0;
                bufferMap[x,y] = GetVonNeumannCount(x,y,2) > 6 ? 1 : 0;
                // bufferMap[x,y] = GetVonNeumannCount(x,y,3) > 12 ? 1 : 0;
                // bufferMap[x,y] = GetVonNeumannCount(x,y,3) > 12 | GetMooreCount(x,y,1) > 5 ? 1 : 0;
                // bufferMap[x,y] = GetVonNeumannCount(x,y,4) > 32 | GetMooreCount(x,y,2) > 12 ? 1 : 0;
            }
        }
        map = (int[,])bufferMap.Clone();
    }
    int GetOneCount(int x, int y)
    {
        int count = 0;
        for (int nX = -1; nX <= 1; nX++) 
        {
            for (int nY = -1; nY <= 1; nY++) 
            {
                count += map[GetXTiled(x+nX),GetYTiled(y+nY)] > 0 ? 1 : 0;
            }
        }
        return count;
    }
    int GetMooreCount(int x, int y, int range) //Moore Neighborhood
    {
        int count = 0;
        for (int nX = -range; nX <= range; nX++)
        {
            for (int nY = -range; nY <= range; nY++)
            {
                count += map[GetXTiled(x+nX), GetYTiled(y+nY)] > 0 ? 1 : 0;
            }
        }
        return count;
    }
    int GetVonNeumannCount(int x, int y, int range) //Von Neumann Neighborhood
    {
        int count = 0;
        for (int nX = x-range; nX <= x+range; nX++)
        {
            int rangeY = range - System.Math.Abs(x-nX);
            for (int nY = y-rangeY; nY <= y+rangeY; nY++)
            {
                count += map[GetXTiled(nX), GetYTiled(nY)] > 0 ? 1 : 0;
            }
        }
        return count;
    }

    private int GetXTiled(int x)
    {
        return GetPositiveModulo(x,width);
    }
    private int GetYTiled(int y)
    {
        return GetPositiveModulo(y,height);
    }
    private int GetPositiveModulo(int c, int m) //c = congruent, m = modulus
    {
        return (c%m+m)%m;
    }
    void OnDrawGizmos()
    {
        if(map!=null&displayGizmos)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color = map[x,y] == 1 ? Color.blue : Color.red;
                    Vector3 position = new Vector3
                    ( 
                        -width / 2 + x + .5f, 
                        .5f, 
                        -height / 2 + y + .5f
                    );
                    Gizmos.DrawCube(position,Vector3.one*.1f);
                }
            }

            for(int x = 0; x <= width; x+=windowRange)
            {
                Gizmos.color = new Color((float)x/(float)height, 0f, 0f,1f);
                Gizmos.DrawLine
                (
                    new Vector3
                    ( 
                        -width / 2, 
                        .5f, 
                        -height / 2 + x
                    ),
                    new Vector3
                    ( 
                        width / 2, 
                        .5f, 
                        -height / 2 + x
                    )
                );
            }
            for(int y = 0; y <= height; y+=windowRange)
            {
                Gizmos.color = new Color(0f, (float)y/(float)height,0f,1f);
                Gizmos.DrawLine
                (
                    new Vector3
                    ( 
                        -width / 2 + y, 
                        .5f, 
                        -height / 2
                    ),
                    new Vector3
                    ( 
                        -width / 2 + y,  
                        .5f, 
                        height / 2
                    )
                );
            }
        }
    }


    private GameObject DisplayQuad;
    void Display()
    {
        DisplayQuad = new GameObject("Display");
        DisplayQuad.transform.parent = transform;
        var mFilter = DisplayQuad.AddComponent<MeshFilter>();
        var mRenderer = DisplayQuad.AddComponent<MeshRenderer>();
        var mGen = new MeshGenerator();
        mFilter.mesh = mGen.GetQuad(width, height);
        mRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        dTexture = new Texture2D(width,height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                dTexture.SetPixel(x,y,map[x,y] == 1 ? Color.black : Color.white);
            }
        }
        dTexture.Apply();
        dTexture.filterMode = FilterMode.Point;
        mRenderer.material.SetTexture("_MainTex", dTexture);
    }
    void UpdateDisplayTexture()
    {
        var mRenderer = GetComponentInChildren<MeshRenderer>();
        dTexture = new Texture2D(width,height);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                dTexture.SetPixel(x,y,map[x,y] == 1 ? Color.black : Color.white);
            }
        }
        dTexture.Apply();
        dTexture.filterMode = FilterMode.Point;
        mRenderer.material.SetTexture("_MainTex", dTexture);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Random = UnityEngine.Random;

public class NodeMap : MonoBehaviour
{
    [SerializeField] private bool generateMaps = false;
    [SerializeField] private bool infectNOW = false;
    [SerializeField] private float minDist = 1;
    [SerializeField] private Vector2 mapSize = new Vector2(16f,16f);
    [SerializeField] private int nodeCheckLimit = 30;
    [SerializeField] private Vector2 noiseST = Vector2.one;
    [SerializeField][Range( 0f, 1f )] private float infectionProbability = 0f;
    [SerializeField][Range( 1f, 32f)] private float infectionStrenght = 8f;
    [SerializeField][Range(-1f, 1f )] private float infectionSpread = 0f;
    [SerializeField] private bool drawNode = false;
    [SerializeField] private bool drawParent = false;
    [SerializeField] private bool drawDirection = false;
    [SerializeField] private bool drawInfection = false;
    
    private const float SQR2 = 1.4142135623730951f; // square root of 2
    private string hiddenSeed = "";
    private float cellSize;
    private int[,] cellMap;
    private List<Node> nodes = new List<Node>();
    private List<Node> infectiousNodes = new List<Node>();
    
    void OnValidate()
    {
        if(minDist < 0.05f) minDist = 0.05f;
        if(generateMaps)
        {
            generateMaps = false;
            hiddenSeed = System.DateTime.Now.GetHashCode().ToString()+Random.value.ToString();
            SetCellSize();
            InitializeCellMap();
            PopulateMap();
        }

    }
    void Update()
    {
        if(infectNOW)
        {
            for (int i = 0; i < 128; i++)
            {
                infectNeighbors();
            }
            if(!(infectiousNodes.Count>0))
            {
                infectNOW = false;
            }
        }
    }
    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(Vector3.zero,new Vector3(mapSize.x,0f,mapSize.y));
        if (nodes != null) {
            if(drawParent)
            {
                foreach (Node node in nodes) 
                {
                    Gizmos.color = Color.blue*.75f;
                    if(node._parent!=null)
                    {
                        Gizmos.DrawLine
                        (
                            new Vector3
                            (
                                node._position.x-mapSize.x/2,
                                0f,
                                node._position.y-mapSize.y/2
                            ),
                            new Vector3
                            (
                                node._parent._position.x-mapSize.x/2,
                                0f,
                                node._parent._position.y-mapSize.y/2
                            )
                        );
                    }
                }
            }
            foreach (Node node in nodes) {
                if(drawNode)
                {
                    Gizmos.color = node._isInfected? Color.red : Color.green;
                    Gizmos.DrawLine
                    (
                        new Vector3
                        (
                            node._position.x-mapSize.x/2 + .01f,
                            0f,
                            node._position.y-mapSize.y/2
                        ),
                        new Vector3
                        (
                            node._position.x-mapSize.x/2 - .01f,
                            0f,
                            node._position.y-mapSize.y/2
                        )
                    );
                    Gizmos.DrawLine
                    (
                        new Vector3
                        (
                            node._position.x-mapSize.x/2,
                            0f,
                            node._position.y-mapSize.y/2 + .01f
                        ),
                        new Vector3
                        (
                            node._position.x-mapSize.x/2,
                            0f,
                            node._position.y-mapSize.y/2 - .01f
                        )
                    );
                }
                if(drawDirection)
                {
                    Gizmos.color = !node._isInfected ? 
                    Color.HSVToRGB
                    (
                        Vector2.Angle
                        (
                            Vector2.up,
                            node._direction
                        )/360f,
                        1f, 
                        node._direction.sqrMagnitude+.5f
                    ) : Color.red
                    ;
                    Gizmos.DrawLine
                    (
                        new Vector3
                        (
                            node._position.x-mapSize.x/2,
                            0f,
                            node._position.y-mapSize.y/2
                        ),
                        new Vector3
                        (
                            node._position.x+node._direction.x*minDist*2f-mapSize.x/2,
                            0f,
                            node._position.y+node._direction.y*minDist*2f-mapSize.y/2
                        )
                    );
                }
                if(drawInfection)
                {
                    if(node._isInfected)
                    {
                        Gizmos.color = new Color((float)(64-node._infectionGeneration)/64f,0f,0f,1f);
                        if(node._infectedNeighbors!=null)
                        {
                            foreach (var infectedNeighborNode in node._infectedNeighbors)
                            {
                                Gizmos.DrawLine
                                (
                                    new Vector3
                                    (
                                        node._position.x-mapSize.x/2,
                                        0f,
                                        node._position.y-mapSize.y/2
                                    ),
                                    new Vector3
                                    (
                                        infectedNeighborNode._position.x-mapSize.x/2,
                                        0f,
                                        infectedNeighborNode._position.y-mapSize.y/2
                                    )
                                );
                            }
                        }
                    }
                }
            }
        }
        Handles.Label
        (
            Vector3.forward*(mapSize.y/2f+1f),
            "Node count: " + nodes.Count.ToString()
        );

    }
    public class Node
    {
        public Vector2 _position;
        public Node _parent;
        public Vector2 _direction;
        public bool _isInfected;
        public int _infectionGeneration;
        public List<Node> _infectedNeighbors = new List<Node>();

        public Node(Vector2 position, Vector2 direction, bool isInfected)
        {
            _position = position;
            _direction = direction;
            _isInfected = isInfected;
            _infectionGeneration = isInfected ? 0 : -1;
        }
        public Node(Vector2 position, Vector2 direction, bool isInfected, Node parent)
        {
            _position = position;
            _direction = direction;
            _isInfected = isInfected;
            _parent = parent;
            _infectionGeneration = 0;
            _infectionGeneration = isInfected ? parent._infectionGeneration + 1 : -1;
        }
    }

    private Vector2 getDirection(Vector2 position)
    {
        var hashInt = hiddenSeed.GetHashCode();
        var outVector = new Vector2
        (
            Mathf.PerlinNoise
            (
                noiseST.x*position.x+(hashInt&0b10001000), 
                noiseST.y*position.y+(hashInt&0b01000100)
            )*2f-1f,
            Mathf.PerlinNoise
            (
                noiseST.x*position.x+(hashInt&0b00100010), 
                noiseST.y*position.y+(hashInt&0b00010001)
            )*2f-1f
        );
        return outVector;
    }

    private void PopulateMap()
    {
        nodes.Clear();
        infectiousNodes.Clear();
        List<Node> liveNodes = new List<Node>();
        // add first node to center of map
        bool isInfected = Random.value < infectionProbability;
        nodes.Add
        (
            new Node
            (
                mapSize/2f,
                getDirection(mapSize/2f), 
                isInfected
            )
        );
        cellMap
        [
            Mathf.RoundToInt(mapSize.x/(2f*cellSize)),
            Mathf.RoundToInt(mapSize.y/(2f*cellSize))
        ] = 0;
        if (isInfected) infectiousNodes.Add(nodes[0]);
        liveNodes.Add(nodes[0]);
        while(liveNodes.Count>0)
        {
            //Select random live node
            int id = Random.Range(0,liveNodes.Count);
            Vector2 position = liveNodes[id]._position;
            bool newNodeAdded = false;
            for (int i = 0; i < nodeCheckLimit; i++)
            {
                float offsetAngle = Random.value * Mathf.PI * 2f; //angle in radians
                Vector2 offsetVector = new Vector2
                (
                    Mathf.Sin(offsetAngle), 
                    Mathf.Cos(offsetAngle)
                ) * Random.Range(minDist,2f*minDist);
                Vector2 newNodePosition = position + offsetVector;
                if(nodePositionIsValid(newNodePosition))
                {
                    isInfected = Random.value < infectionProbability;
                    var newNode = new Node
                    (
                        newNodePosition, 
                        getDirection(newNodePosition), 
                        isInfected, 
                        liveNodes[id]
                    );
                    cellMap
                    [
                        Mathf.RoundToInt(newNodePosition.x/cellSize),
                        Mathf.RoundToInt(newNodePosition.y/cellSize)
                    ] = nodes.Count;
                    nodes.Add(newNode);
                    liveNodes.Add(newNode);
                    if (isInfected) infectiousNodes.Add(newNode);
                    newNodeAdded = true;
                    break;
                }
            }
            if(!newNodeAdded)
            {
                liveNodes.RemoveAt(id);
            }
        }
    }
    private void infectNeighbors()
    {
        if(infectiousNodes==null)
        {
            return;
        }
        if (infectiousNodes.Count > 0)
        {
            int infectiousID = Random.Range(0,infectiousNodes.Count);
            var node = infectiousNodes[infectiousID];
            if(node._infectedNeighbors.Count < 3)
            {
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        Vector2Int cell = new Vector2Int
                        (
                            Mathf.RoundToInt(node._position.x/cellSize)+x,
                            Mathf.RoundToInt(node._position.y/cellSize)+y
                        );
                        if
                        (
                              cell.x >= 0
                            & cell.y >= 0
                            & cell.x <  cellMap.GetLength(0)
                            & cell.y <  cellMap.GetLength(1)
                            & cell   != Vector2Int.RoundToInt(node._position)
                        )
                        {
                            int id = cellMap[cell.x,cell.y];
                            if(id!=-1)
                            {
                                float distance = Vector2.Distance(node._position,nodes[id]._position);
                                Vector2 offset = nodes[id]._position - node._position;
                                float dotP = Vector2.Dot(node._direction.normalized,offset.normalized);
                                if
                                (
                                      dotP > infectionSpread 
                                    & distance < node._direction.sqrMagnitude * minDist * infectionStrenght * 2f 
                                    & !nodes[id]._isInfected
                                )
                                {
                                    nodes[id]._isInfected = true;
                                    nodes[id]._infectionGeneration = node._infectionGeneration + 1;
                                    node._infectedNeighbors.Add(nodes[id]);
                                    infectiousNodes.Add(nodes[id]);
                                }
                            }
                        }
                    }
                }
            }
            infectiousNodes.RemoveAt(infectiousID);
        }
    }


    private bool nodePositionIsValid(Vector2 newNodePosition)
    {
        Vector2Int cell = new Vector2Int
        (                        
            Mathf.RoundToInt(newNodePosition.x/cellSize),
            Mathf.RoundToInt(newNodePosition.y/cellSize)
        );
        if
        (
              cell.x < 0
            | cell.y < 0
            | cell.x >= cellMap.GetLength(0)
            | cell.y >= cellMap.GetLength(1)
        ) return false;
        for (int x = -2; x < 2; x++)
        {
            for (int y = -2; y < 2; y++)
            {
                Vector2Int sCell = cell + new Vector2Int(x,y);
                if
                (
                      sCell.x < 0
                    | sCell.y < 0
                    | sCell.x >= cellMap.GetLength(0)
                    | sCell.y >= cellMap.GetLength(1)
                ) return false;
                if (cellMap[sCell.x,sCell.y]!=-1)
                {
                    // Debug.Log(cellMap[sCell.x,sCell.y]);
                    float squareDistance  = 
                    (
                        newNodePosition 
                        - nodes
                        [
                            cellMap
                            [
                                sCell.x,
                                sCell.y
                            ]
                        ]._position
                    ).sqrMagnitude;
                    if(squareDistance < minDist * minDist) return false;
                }
            }
        }
        return true;
    }

    private void InitializeCellMap()
    {
        cellMap = new int
        [ 
            Mathf.CeilToInt( mapSize.x / cellSize ), 
            Mathf.CeilToInt( mapSize.y / cellSize ) 
        ];
        for (int x = 0; x < cellMap.GetLength(0); x++)
        {
            for (int y = 0; y < cellMap.GetLength(1); y++)
            {
                cellMap[x,y]=-1;
            }
        }
    }

    private void SetCellSize()
    {
        cellSize = minDist/SQR2;
    }
    Vector2 GetTiledPosition (Vector2 position)
    {
        var outVector = new Vector2
        (
            GetModulo( position.x, mapSize.x ),
            GetModulo( position.y, mapSize.y )
        );
        return outVector;
    }
    Vector2Int GetTiledPosition (Vector2Int position)
    {
        var outVector = new Vector2Int
        (
            GetModulo( position.x, cellMap.GetLength(0) ),
            GetModulo( position.y, cellMap.GetLength(1) )
        );
        return outVector;
    }
    private float GetModulo(float c, float m) //c = congruent, m = modulus
    {
        return (c%m+m)%m;
    }
    private int GetModulo(int c, int m)
    {
        return (c%m+m)%m;
    }
}

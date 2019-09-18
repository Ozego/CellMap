using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class VoronoiMap : MonoBehaviour
{
    [SerializeField] private float minDist = 1;
    [SerializeField] private Vector2 mapSize = new Vector2(16f,16f);
    [SerializeField] private int nodeCheckLimit = 30;
    private const float SQR2 = 1.4142135623730951f; // square root of 2
    private float cellSize;
    private int[,] cellMap;
    private List<Node> nodes = new List<Node>();
    
    void OnValidate()
    {
        if(minDist < 0.05f) minDist = 0.05f;
        SetCellSize();
        InitializeCellMap();
        PopulateMap();
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(Vector3.zero,new Vector3(mapSize.x,0f,mapSize.y));
        if (nodes != null) {
            foreach (Node node in nodes) {
                // Gizmos.DrawSphere
                // (
                //     new Vector3
                //     (
                //         node._position.x-mapSize.x/2,
                //         0f,
                //         node._position.y-mapSize.y/2
                //     ), 
                //     .05f
                // );
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
    }
    public class Node
    {
        public Vector2 _position;
        public Node _parent;
        public Node(Vector2 position)
        {
            this._position = position;
        }
        public Node(Vector2 position, Node parent)
        {
            this._position = position;
            this._parent = parent;
        }
    }

    private void PopulateMap()
    {
        nodes.Clear();
        List<Node> liveNodes = new List<Node>();
        // add first node to center of map
        nodes.Add(new Node(mapSize/2f)); 
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
                    var newNode = new Node(newNodePosition, liveNodes[id]);
                    cellMap
                    [
                        (int)(newNodePosition.x/cellSize),
                        (int)(newNodePosition.y/cellSize)
                    ] = nodes.Count;
                    nodes.Add(newNode);
                    liveNodes.Add(newNode);
                    newNodeAdded = true;
                    break;
                }
            }
            if(!newNodeAdded)
            {
                liveNodes.RemoveAt(id);
            }
        }
        Debug.Log(nodes.Count);
    }

    private bool nodePositionIsValid(Vector2 newNodePosition)
    {
        Vector2Int cell = new Vector2Int
        (                        
            (int)(newNodePosition.x/cellSize),
            (int)(newNodePosition.y/cellSize)
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

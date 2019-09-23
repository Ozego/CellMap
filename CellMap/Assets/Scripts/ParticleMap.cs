using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleMap : MonoBehaviour
{
    [SerializeField] private Vector2Int size = new Vector2Int(256,256);
    [SerializeField] private int cellSize = 8;
    [SerializeField] private ComputeShader pShader;
    [SerializeField] bool evaluateDebugFunction;

    List<RenderTexture> rTextures;

    void OnValidate()
    {
        if(evaluateDebugFunction)
        {
            evaluateDebugFunction = false;
            dFunc();
        }
    }

    private void dFunc()
    {
        var tParticle = new particle();
        tParticle._position._x = 5f;
        tParticle._position._y = 53f;
        Debug.Log( tParticle._position );
    }

    void OnDrawGizmos()
    {
    }
    void Start()
    {
    }
    void Update()
    {
    }


    public struct particle
    {
        public float2 _position;
        public float2 _direction;
        public float2 _size;
        public int byteSize
        { 
            get => 
                  _position.byteSize
                + _direction.byteSize
                + _size.byteSize ; 
        }
    }
    public struct float2
    {
        public float _x;
        public float _y;
        public int byteSize 
        { 
            get => sizeof(float) * 2 ; 
        }
    }
}

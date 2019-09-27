using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator
{
    public static Mesh GetQuad(float width, float height)
    {
        var mesh = new Mesh();
        var vertices = new Vector3[4]
        {
            new Vector3( -width/2f, 0f, -height/2f),
            new Vector3(  width/2f, 0f, -height/2f),
            new Vector3( -width/2f, 0f,  height/2f),
            new Vector3(  width/2f, 0f,  height/2f)
        };
        mesh.vertices = vertices;
        var triangles = new int[6]
        {
            0, 2, 1,
            2, 3, 1
        };
        mesh.triangles = triangles;
        var normals = new Vector3[4]
        {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
        };
        mesh.normals = normals;
        var uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        mesh.uv = uv;
        return mesh;
    }
    public static Mesh GetQuad(int width, int height)
    {
        return GetQuad((float)width,(float)height);
    }
}

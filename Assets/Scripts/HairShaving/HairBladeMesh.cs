using System.Collections.Generic;
using UnityEngine;

namespace GiantGrass
{
    //Hair Blade Mesh, it's better to create blades through code to avoid problems with the pivot point later
    public static class HairBladeMesh
    {
        public static Mesh Create(float width = 0.08f, int segments = 4, float curve = 0.3f)
        {
            segments = Mathf.Max(1, segments);
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();

            for (int side = 0; side < 2; side++)
            {
                int start = vertices.Count;
                float flip = side == 0 ? 1f : -1f;

                for (int s = 0; s <= segments; s++)
                {
                    float t = s / (float)segments;
                    float halfWidth = 0.5f * width * (1f - t);
                    float z = curve * t * t;
                    Vector3 n = new Vector3(0f, -2f * curve * t, 1f).normalized * flip;
                    var c = new Color(t, t, t, 1f);

                    vertices.Add(new Vector3(-halfWidth, t, z));
                    vertices.Add(new Vector3(halfWidth, t, z));
                    normals.Add(n); normals.Add(n);
                    uvs.Add(new Vector2(0f, t)); uvs.Add(new Vector2(1f, t));
                    colors.Add(c); colors.Add(c);
                }

                for (int s = 0; s < segments; s++)
                {
                    int bl = start + s * 2, br = bl + 1, tl = bl + 2, tr = bl + 3;
                    if (side == 0)
                    {
                        triangles.Add(bl); triangles.Add(br); triangles.Add(tl);
                        triangles.Add(br); triangles.Add(tr); triangles.Add(tl);
                    }
                    else
                    {
                        triangles.Add(bl); triangles.Add(tl); triangles.Add(br);
                        triangles.Add(br); triangles.Add(tl); triangles.Add(tr);
                    }
                }
            }

            var mesh = new Mesh
            {
                name = "Hair Blade",
                hideFlags = HideFlags.DontSave
            };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
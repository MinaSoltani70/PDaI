using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GiantGrass
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [DisallowMultipleComponent]
    public class HairSpawner : MonoBehaviour, IShaveable
    {
        const int MaxPerBatch = 1023;

        [Header("Material and mesh")]
        public Material grassMaterial;
        public Mesh bladeMesh;

        [Range(0.01f, 0.5f)] public float generatedWidth = 0.08f;
        [Range(1, 8)] public int generatedSegments = 4;
        [Range(0f, 1f)] public float generatedCurve = 0.3f;

        [Header("Placement")]
        [Min(0f)] public float density = 30f;
        [Min(1)] public int maxBlades = 100000;
        public int seed = 12345;
        public Texture2D densityMask;

        [Header("Blade shape")]
        [Min(0.01f)] public float minHeight = 0.5f;
        [Min(0.01f)] public float maxHeight = 1f;
        [Range(0f, 60f)] public float maxLean = 15f;
        [Range(0f, 1f)] public float upAlignment = 0f;

        [Header("Shaving")]
        [Range(0f, 0.5f)] public float minLength = 0.05f;

        [Header("Regrowth")]
        public bool canRegrow = true;
        [Min(0f)] public float regrowthDelay = 4f;
        [Min(0.01f)] public float regrowthSpeed = 0.3f;

        [Header("Trampling")]
        [Range(0f, 90f)] public float maxBendAngle = 70f;
        [Min(0f)] public float bendHoldTime = 3f;
        [Min(0.1f)] public float bendPressSpeed = 12f;
        [Min(0.01f)] public float bendRecoverSpeed = 0.5f;

        [Header("Rendering")]
        public ShadowCastingMode castShadows = ShadowCastingMode.Off;
        public bool receiveShadows = true;
        [Min(0.5f)] public float chunkSize = 4f;
        [Min(1f)] public float maxDrawDistance = 150f;
        public bool previewInEditMode = true;
        public bool hideSourceMesh;

        [Header("Debug")]
        public bool drawChunkBounds;

        public event Action<int> BladesCut;

        public int BladeCount => bladeCount;
        public int BatchCount => batches.Count;
        public int CutBladeCount { get; private set; }

        private int bladeCount;
        private Vector3[] localPos;
        private Vector3[] localNormal;
        private float[] yaw, lean, height;
        private float[] length;
        private float[] lastShaveTime;
        private Vector3[] worldPos;
        private Quaternion[] worldRot;
        private Vector3[] worldUp;
        private float[] bend;
        private float[] bendTarget;
        private float[] bendHoldUntil;
        private Vector3[] bendDirLocal;

        private class Batch
        {
            public readonly int[] blades = new int[MaxPerBatch];
            public readonly Matrix4x4[] matrices = new Matrix4x4[MaxPerBatch];
            public int bladeTotal;
            public int count;
            public Bounds bounds;
            public bool dirty = true;
        }

        private readonly List<Batch> batches = new List<Batch>();
        private readonly Dictionary<Vector3Int, List<Batch>> grid = new Dictionary<Vector3Int, List<Batch>>();
        private readonly HashSet<Batch> bendingBatches = new HashSet<Batch>();
        private readonly HashSet<Batch> regrowingBatches = new HashSet<Batch>();
        private readonly List<Batch> tmpBatches = new List<Batch>();
        private readonly Plane[] frustum = new Plane[6];

        private Mesh generatedMesh;
        private bool needsSpawn = true;

        void OnEnable()
        {
            needsSpawn = true;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            DestroyGeneratedMesh();
        }

        void OnValidate()
        {
            if (maxHeight < minHeight) maxHeight = minHeight;
            needsSpawn = true;
        }

        [ContextMenu("Respawn Grass")]
        public void Respawn() => needsSpawn = true;

        void LateUpdate()
        {
            if (!Application.isPlaying) return;
            TickRegrow();
            TickBend();
            Draw(null);
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (Application.isPlaying || !previewInEditMode) return;
            if (cam.cameraType != CameraType.SceneView && cam.cameraType != CameraType.Game) return;
            Draw(cam);
        }


public int ShaveNearPoint(Vector3 hitPoint, float radius, float cutAmount, int maxHairUnits)
{
    if (needsSpawn) Spawn();

    if (bladeCount == 0 || radius <= 0f || cutAmount <= 0f || maxHairUnits <= 0)
        return 0;

    float r2 = radius * radius;
    float now = Time.time;
    int cutNow = 0;

    Vector3Int min = Cell(hitPoint - Vector3.one * radius);
    Vector3Int max = Cell(hitPoint + Vector3.one * radius);

    for (int x = min.x; x <= max.x && cutNow < maxHairUnits; x++)
        for (int y = min.y; y <= max.y && cutNow < maxHairUnits; y++)
            for (int z = min.z; z <= max.z && cutNow < maxHairUnits; z++)
            {
                if (!grid.TryGetValue(new Vector3Int(x, y, z), out var list))
                    continue;

                foreach (var b in list)
                {
                    if (cutNow >= maxHairUnits) break;

                    bool changed = false;

                    for (int k = 0; k < b.bladeTotal && cutNow < maxHairUnits; k++)
                    {
                        int i = b.blades[k];

                        if (length[i] <= 0f) continue;
                        if ((worldPos[i] - hitPoint).sqrMagnitude > r2) continue;

                        length[i] -= cutAmount;
                        lastShaveTime[i] = now;

                        if (length[i] <= minLength)
                        {
                            length[i] = 0f;
                            cutNow++;
                        }

                        changed = true;
                    }

                    if (changed)
                    {
                        b.dirty = true;
                        if (canRegrow) regrowingBatches.Add(b);
                    }
                }
            }

    if (cutNow > 0)
    {
        CutBladeCount += cutNow;
        BladesCut?.Invoke(cutNow);
    }

    return cutNow;
}

        [ContextMenu("Regrow All")]
        public void RegrowAll()
        {
            if (length == null) return;
            for (int i = 0; i < bladeCount; i++) { length[i] = 1f; bend[i] = 0f; bendTarget[i] = 0f; }
            CutBladeCount = 0;
            bendingBatches.Clear();
            regrowingBatches.Clear();
            foreach (var b in batches) b.dirty = true;
        }

        void TickRegrow()
        {
            if (regrowingBatches.Count == 0) return;
            if (!canRegrow) { regrowingBatches.Clear(); return; }

            float now = Time.time;
            float grow = regrowthSpeed * Time.deltaTime;

            tmpBatches.Clear();
            tmpBatches.AddRange(regrowingBatches);
            foreach (var b in tmpBatches)
            {
                bool waiting = false, changed = false;
                for (int k = 0; k < b.bladeTotal; k++)
                {
                    int i = b.blades[k];
                    float len = length[i];
                    if (len <= 0f || len >= 1f) continue;

                    if (now - lastShaveTime[i] < regrowthDelay) { waiting = true; continue; }

                    len = Mathf.Min(1f, len + grow);
                    length[i] = len;
                    changed = true;
                    if (len < 1f) waiting = true;
                }

                if (changed) b.dirty = true;
                if (!waiting) regrowingBatches.Remove(b);
            }
        }

        public void Trample(Vector3 center, float radius, float strength = 1f)
        {
            if (needsSpawn) Spawn();
            if (bladeCount == 0 || radius <= 0f) return;

            float r2 = radius * radius;
            float now = Time.time;
            Vector3Int min = Cell(center - Vector3.one * radius);
            Vector3Int max = Cell(center + Vector3.one * radius);

            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                    for (int z = min.z; z <= max.z; z++)
                    {
                        if (!grid.TryGetValue(new Vector3Int(x, y, z), out var list)) continue;

                        foreach (var b in list)
                        {
                            bool touched = false;
                            for (int k = 0; k < b.bladeTotal; k++)
                            {
                                int i = b.blades[k];
                                if (length[i] <= 0f) continue;

                                Vector3 offset = worldPos[i] - center;
                                float d2 = offset.sqrMagnitude;
                                if (d2 > r2) continue;

                                float s = Mathf.Clamp01(strength * (1f - Mathf.Sqrt(d2) / radius) * 1.5f);
                                if (s <= 0f) continue;

                                bendHoldUntil[i] = now + bendHoldTime;
                                touched = true;

                                if (s <= bendTarget[i]) continue;

                                bool fresh = bend[i] < 0.05f && bendTarget[i] < 0.05f;
                                Vector3 up = worldUp[i];
                                Vector3 dir = offset - up * Vector3.Dot(offset, up);
                                if (dir.sqrMagnitude > 1e-6f)
                                {
                                    Vector3 newDir = transform.InverseTransformDirection(dir.normalized);
                                    bendDirLocal[i] = fresh ? newDir : Vector3.Slerp(bendDirLocal[i], newDir, 0.15f).normalized;
                                }
                                else if (fresh)
                                {
                                    bendDirLocal[i] = transform.InverseTransformDirection(worldRot[i] * Vector3.forward);
                                }
                                bendTarget[i] = s;
                            }
                            if (touched) bendingBatches.Add(b);
                        }
                    }
        }

        void TickBend()
        {
            if (bendingBatches.Count == 0) return;
            float now = Time.time;
            float dt = Time.deltaTime;
            float recover = bendRecoverSpeed * dt;
            float smooth = 1f - Mathf.Exp(-bendPressSpeed * dt);

            tmpBatches.Clear();
            tmpBatches.AddRange(bendingBatches);
            foreach (var b in tmpBatches)
            {
                bool any = false;
                for (int k = 0; k < b.bladeTotal; k++)
                {
                    int i = b.blades[k];
                    if (bendTarget[i] <= 0f && bend[i] <= 0f) continue;

                    if (now > bendHoldUntil[i]) bendTarget[i] = Mathf.Max(0f, bendTarget[i] - recover);

                    float v = Mathf.Lerp(bend[i], bendTarget[i], smooth);
                    if (bendTarget[i] <= 0f && v < 0.002f) v = 0f;
                    bend[i] = v;

                    if (v > 0f || bendTarget[i] > 0f) any = true;
                }
                b.dirty = true;
                if (!any) bendingBatches.Remove(b);
            }
        }

        void Spawn()
        {
            needsSpawn = false;
            bladeCount = 0;
            CutBladeCount = 0;
            batches.Clear();
            grid.Clear();
            bendingBatches.Clear();
            regrowingBatches.Clear();

            var source = GetComponent<MeshFilter>().sharedMesh;
            if (source == null) { Debug.LogWarning($"[GrassSpawner] {name}: MeshFilter has no mesh.", this); return; }
            if (!source.isReadable) { Debug.LogError($"[GrassSpawner] {name}: enable Read/Write on the model import settings.", this); return; }

            Texture2D mask = densityMask;
            if (mask != null && !mask.isReadable)
            {
                Debug.LogError($"[GrassSpawner] {name}: enable Read/Write on the density mask. Mask ignored.", this);
                mask = null;
            }

            Vector3[] verts = source.vertices;
            Vector3[] norms = source.normals;
            Vector2[] uvs = source.uv;
            int[] tris = source.triangles;
            bool hasNormals = norms != null && norms.Length == verts.Length;
            if (mask != null && (uvs == null || uvs.Length != verts.Length)) mask = null;

            Vector3 scale = transform.lossyScale;
            int triCount = tris.Length / 3;
            var cumulativeArea = new double[triCount];
            double totalArea = 0;
            for (int t = 0; t < triCount; t++)
            {
                Vector3 a = Vector3.Scale(verts[tris[t * 3]], scale);
                Vector3 b = Vector3.Scale(verts[tris[t * 3 + 1]], scale);
                Vector3 c = Vector3.Scale(verts[tris[t * 3 + 2]], scale);
                totalArea += Vector3.Cross(b - a, c - a).magnitude * 0.5;
                cumulativeArea[t] = totalArea;
            }
            if (totalArea <= 0) return;

            int target = (int)Math.Min(maxBlades, totalArea * density);
            var rng = new System.Random(seed);
            var pPos = new List<Vector3>(target);
            var pNormal = new List<Vector3>(target);
            var pYaw = new List<float>(target);
            var pLean = new List<float>(target);
            var pHeight = new List<float>(target);

            for (int i = 0; i < target; i++)
            {
                int t = Array.BinarySearch(cumulativeArea, rng.NextDouble() * totalArea);
                if (t < 0) t = ~t;
                t = Mathf.Min(t, triCount - 1);

                float r1 = (float)rng.NextDouble(), r2 = (float)rng.NextDouble();
                if (r1 + r2 > 1f) { r1 = 1f - r1; r2 = 1f - r2; }
                float r0 = 1f - r1 - r2;
                int i0 = tris[t * 3], i1 = tris[t * 3 + 1], i2 = tris[t * 3 + 2];

                float rYaw = (float)rng.NextDouble(), rLean = (float)rng.NextDouble();
                float rHeight = (float)rng.NextDouble(), rMask = (float)rng.NextDouble();

                if (mask != null)
                {
                    Vector2 uv = uvs[i0] * r0 + uvs[i1] * r1 + uvs[i2] * r2;
                    Color m = mask.GetPixelBilinear(uv.x, uv.y);
                    if (rMask > Mathf.Max(m.r, Mathf.Max(m.g, m.b))) continue;
                }

                Vector3 n = hasNormals ? norms[i0] * r0 + norms[i1] * r1 + norms[i2] * r2
                    : Vector3.Cross(verts[i1] - verts[i0], verts[i2] - verts[i0]);

                pPos.Add(verts[i0] * r0 + verts[i1] * r1 + verts[i2] * r2);
                pNormal.Add(n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up);
                pYaw.Add(rYaw * 360f);
                pLean.Add(rLean * maxLean);
                pHeight.Add(Mathf.Lerp(minHeight, maxHeight, rHeight));
            }

            bladeCount = pPos.Count;
            localPos = pPos.ToArray();
            localNormal = pNormal.ToArray();
            yaw = pYaw.ToArray();
            lean = pLean.ToArray();
            height = pHeight.ToArray();
            length = new float[bladeCount];
            for (int i = 0; i < bladeCount; i++) length[i] = 1f;
            lastShaveTime = new float[bladeCount];
            worldPos = new Vector3[bladeCount];
            worldRot = new Quaternion[bladeCount];
            worldUp = new Vector3[bladeCount];
            bend = new float[bladeCount];
            bendTarget = new float[bladeCount];
            bendHoldUntil = new float[bladeCount];
            bendDirLocal = new Vector3[bladeCount];

            if (bladeMesh == null)
            {
                DestroyGeneratedMesh();
                generatedMesh = HairBladeMesh.Create(generatedWidth, generatedSegments, generatedCurve);
            }

            if (grassMaterial != null && !grassMaterial.enableInstancing) grassMaterial.enableInstancing = true;
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.enabled = !hideSourceMesh;

            BuildBatches();

            if (Application.isPlaying) Debug.Log($"[GrassSpawner] {name}: {bladeCount:N0} blades in {batches.Count} batches " +
                          $"(surface {totalArea:F0} m²).", this);
        }

        void BuildBatches()
        {
            batches.Clear();
            grid.Clear();
            bendingBatches.Clear();
            transform.hasChanged = false;
            if (bladeCount == 0) return;

            Matrix4x4 l2w = transform.localToWorldMatrix;
            Matrix4x4 normalMatrix = l2w.inverse.transpose;

            for (int i = 0; i < bladeCount; i++)
            {
                Vector3 pos = l2w.MultiplyPoint3x4(localPos[i]);
                Vector3 n = normalMatrix.MultiplyVector(localNormal[i]).normalized;
                Vector3 up = Vector3.Slerp(n, Vector3.up, upAlignment).normalized;

                worldPos[i] = pos;
                worldUp[i] = up;
                worldRot[i] = Quaternion.FromToRotation(Vector3.up, up)
                            * Quaternion.AngleAxis(yaw[i], Vector3.up)
                            * Quaternion.AngleAxis(lean[i], Vector3.right);

                Vector3Int cell = Cell(pos);
                if (!grid.TryGetValue(cell, out var list))
                {
                    list = new List<Batch>();
                    grid.Add(cell, list);
                }

                Batch batch = list.Count > 0 ? list[list.Count - 1] : null;
                if (batch == null || batch.bladeTotal == MaxPerBatch)
                {
                    batch = new Batch();
                    list.Add(batch);
                    batches.Add(batch);
                }
                batch.blades[batch.bladeTotal++] = i;
            }

            foreach (var b in batches)
            {
                for (int k = 0; k < b.bladeTotal; k++)
                {
                    int i = b.blades[k];
                    if (bend[i] > 0f || bendTarget[i] > 0f) { bendingBatches.Add(b); break; }
                }
            }
        }

        void RebuildBatch(Batch b)
        {
            b.count = 0;
            bool hasBounds = false;
            Bounds bounds = default;

            for (int k = 0; k < b.bladeTotal; k++)
            {
                int i = b.blades[k];
                float len = length[i];
                if (len <= 0f) continue;

                Quaternion rot = worldRot[i];
                if (bend[i] > 0.001f)
                {
                    Vector3 dir = transform.TransformDirection(bendDirLocal[i]);
                    Vector3 axis = Vector3.Cross(worldUp[i], dir);
                    if (axis.sqrMagnitude > 1e-8f)
                        rot = Quaternion.AngleAxis(bend[i] * maxBendAngle, axis.normalized) * rot;
                }

                float h = height[i];
                b.matrices[b.count++] = Matrix4x4.TRS(worldPos[i], rot, new Vector3(h, h * len, h));

                if (!hasBounds) { bounds = new Bounds(worldPos[i], Vector3.zero); hasBounds = true; }
                else bounds.Encapsulate(worldPos[i]);
            }

            if (hasBounds) bounds.Expand(maxHeight * 2f);
            b.bounds = bounds;
            b.dirty = false;
        }

        void Draw(Camera cam)
        {
            if (needsSpawn) Spawn();
            if (grassMaterial == null || bladeCount == 0) return;

            if (transform.hasChanged)
            {
                BuildBatches();
                transform.hasChanged = false;
            }

            Mesh mesh = bladeMesh != null ? bladeMesh : generatedMesh;
            if (mesh == null) return;

            Camera cullCam = cam != null ? cam : Camera.main;
            bool cull = cullCam != null;
            Vector3 camPos = Vector3.zero;
            if (cull)
            {
                GeometryUtility.CalculateFrustumPlanes(cullCam, frustum);
                camPos = cullCam.transform.position;
            }
            float maxDistSq = maxDrawDistance * maxDrawDistance;

            var rp = new RenderParams(grassMaterial)
            {
                shadowCastingMode = castShadows,
                receiveShadows = receiveShadows,
                layer = gameObject.layer,
                camera = cam
            };

            foreach (var b in batches)
            {
                if (b.dirty) RebuildBatch(b);
                if (b.count == 0) continue;
                if (cull)
                {
                    if (b.bounds.SqrDistance(camPos) > maxDistSq) continue;
                    if (!GeometryUtility.TestPlanesAABB(frustum, b.bounds)) continue;
                }
                rp.worldBounds = b.bounds;
                Graphics.RenderMeshInstanced(rp, mesh, 0, b.matrices, b.count);
            }
        }

        Vector3Int Cell(Vector3 worldPosition) => new Vector3Int(
            Mathf.FloorToInt(worldPosition.x / chunkSize),
            Mathf.FloorToInt(worldPosition.y / chunkSize),
            Mathf.FloorToInt(worldPosition.z / chunkSize));

        void DestroyGeneratedMesh()
        {
            if (generatedMesh == null) return;
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
            generatedMesh = null;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawChunkBounds) return;

            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.5f);
            foreach (var b in batches)
            {
                if (b.count > 0) Gizmos.DrawWireCube(b.bounds.center, b.bounds.size);
            }
        }
    }
}
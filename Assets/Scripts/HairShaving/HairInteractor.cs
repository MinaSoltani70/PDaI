using System.Collections.Generic;
using GiantGrass;
using UnityEngine;

//Controls trampling
[ExecuteAlways]
public class HairInteractor : MonoBehaviour
{
    [Header("Trample")]
    public bool trampleGrass = true;
    [Min(0.05f)] public float trampleRadius = 0.8f;
    [Range(0f, 1f)] public float trampleStrength = 1f;
    [Min(0.01f)] public float trampleInterval = 0.05f;

    [Header("Point")]
    public Vector3 offset = Vector3.zero;

    [Header("Shader bend")]
    public bool sendToShader = false;
    public string globalProperty = "_Position";

    private static readonly Vector4 FarAway = new Vector4(0f, 100000f, 0f, 0f);
    private readonly List<HairSpawner> spawners = new List<HairSpawner>();
    private int propertyId;
    private float nextTrampleTime;
    private float nextRefreshTime;

    private void OnEnable() => propertyId = Shader.PropertyToID(globalProperty);
    private void OnValidate() => propertyId = Shader.PropertyToID(globalProperty);

    private void LateUpdate()
    {
        Vector3 point = transform.TransformPoint(offset);

        Shader.SetGlobalVector(propertyId, sendToShader ? (Vector4)point : FarAway);

        if (!Application.isPlaying || !trampleGrass) return;

        if (Time.time >= nextRefreshTime)
        {
            nextRefreshTime = Time.time + 2f;
            spawners.Clear();
            spawners.AddRange(FindObjectsByType<HairSpawner>(FindObjectsSortMode.None));
        }

        if (Time.time < nextTrampleTime) return;
        nextTrampleTime = Time.time + trampleInterval;

        foreach (var spawner in spawners)
        {
            if (spawner != null) spawner.Trample(point, trampleRadius, trampleStrength);
        }
    }

    private void OnDisable() => Shader.SetGlobalVector(propertyId, FarAway);
}
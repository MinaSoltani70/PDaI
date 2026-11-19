using System.Collections.Generic;
using UnityEngine;

//Sends data to the trampling (displacement) shader
public class HairInteraction : MonoBehaviour
{
    [SerializeField] private string interactorTag = "headTrigger";
    [SerializeField] private string positionProperty = "_Position";
    [SerializeField] private float releaseSpeed = 2f;
    [SerializeField] private float releaseHeight = 10f;

    private static readonly Vector4 FarAway = new Vector4(0f, 1000f, 0f, 0f);

    private Renderer hairRenderer;
    private Material material;
    private int positionId;
    private readonly List<Transform> entities = new List<Transform>();
    private bool releasing;
    private float releasedDistance;

    private void Awake()
    {
        hairRenderer = GetComponent<Renderer>();
        material = hairRenderer.material;
        positionId = Shader.PropertyToID(positionProperty);
        material.SetVector(positionId, FarAway);
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    private void Update()
    {
        for (int i = entities.Count - 1; i >= 0; i--)
        {
            if (entities[i] == null || !entities[i].gameObject.activeInHierarchy)
            {
                entities.RemoveAt(i);
                if (entities.Count == 0) StartRelease();
            }
        }

        if (entities.Count > 0)
        {
            Transform last = entities[entities.Count - 1];
            material.SetVector(positionId, last.position);
            return;
        }

        if (!releasing) return;

        float step = releaseSpeed * Time.deltaTime;
        Vector3 p = material.GetVector(positionId);
        p.y += step;
        material.SetVector(positionId, p);

        releasedDistance += step;
        if (releasedDistance >= releaseHeight)
        {
            material.SetVector(positionId, FarAway);
            releasing = false;
        }
    }

    private void OnTriggerEnter(Collider col)
    {
        if (!col.CompareTag(interactorTag)) return;
        if (!entities.Contains(col.transform)) entities.Add(col.transform);
        releasing = false;
    }

    private void OnTriggerExit(Collider col)
    {
        if (!col.CompareTag(interactorTag)) return;
        entities.Remove(col.transform);
        if (entities.Count == 0) StartRelease();
    }

    private void StartRelease()
    {
        releasing = true;
        releasedDistance = 0f;
    }
}
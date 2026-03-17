using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleOutline : MonoBehaviour
{
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private bool showOnStartForDebug = false;

    private readonly List<GameObject> outlineObjects = new List<GameObject>();

    private void Awake()
    {
        CreateOutlineMeshes();
        SetActive(showOnStartForDebug);
    }

    private void CreateOutlineMeshes()
    {
        if (outlineMaterial == null)
        {
            Debug.LogWarning($"{name}: в SimpleOutline не назначен outlineMaterial");
            return;
        }

        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
        Debug.Log($"{name}: найдено MeshFilter = {meshFilters.Length}");

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.sharedMesh == null)
                continue;

            MeshRenderer sourceRenderer = mf.GetComponent<MeshRenderer>();
            if (sourceRenderer == null)
                continue;

            GameObject outlineObj = new GameObject("Outline_" + mf.name);
            outlineObj.transform.SetParent(mf.transform, false);
            outlineObj.transform.localPosition = Vector3.zero;
            outlineObj.transform.localRotation = Quaternion.identity;
            outlineObj.transform.localScale = Vector3.one;

            MeshFilter outlineMF = outlineObj.AddComponent<MeshFilter>();
            outlineMF.sharedMesh = mf.sharedMesh;

            MeshRenderer outlineMR = outlineObj.AddComponent<MeshRenderer>();
            outlineMR.sharedMaterial = outlineMaterial;
            outlineMR.shadowCastingMode = ShadowCastingMode.Off;
            outlineMR.receiveShadows = false;
            outlineMR.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            outlineMR.lightProbeUsage = LightProbeUsage.Off;
            outlineMR.reflectionProbeUsage = ReflectionProbeUsage.Off;

            outlineObjects.Add(outlineObj);
        }

        Debug.Log($"{name}: создано outline-объектов = {outlineObjects.Count}");
    }

    public void SetActive(bool state)
    {
        for (int i = 0; i < outlineObjects.Count; i++)
        {
            if (outlineObjects[i] != null)
                outlineObjects[i].SetActive(state);
        }
    }
}
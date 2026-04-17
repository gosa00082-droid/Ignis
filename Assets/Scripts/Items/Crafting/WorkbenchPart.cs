using System.Collections.Generic;
using UnityEngine;

public class WorkbenchPart : MonoBehaviour
{
    [Header("Идентификация детали")]
    [SerializeField] private string partId;
    [SerializeField] private AttachmentType partType = AttachmentType.None;

    [Header("Нижние точки детали")]
    [SerializeField] private List<Transform> bottomPoints = new();

    public string PartId => partId;
    public AttachmentType PartType => partType;
    public IReadOnlyList<Transform> BottomPoints => bottomPoints;

    public AssemblyRoot GetAssemblyRoot()
    {
        return GetComponentInParent<AssemblyRoot>();
    }
}
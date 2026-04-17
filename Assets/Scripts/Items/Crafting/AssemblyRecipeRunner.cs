using System.Collections.Generic;
using UnityEngine;

public class AssemblyRecipeRunner : MonoBehaviour
{
    [SerializeField] private AssemblyRecipe recipe;

    [Header("Состояние")]
    [SerializeField] private int currentStepIndex;
    [SerializeField] private bool isCompleted;

    private readonly List<int> completedStepIndices = new();

    public bool IsCompleted => isCompleted;

    private void OnEnable()
    {
        AssemblyEvents.AttachmentCompleted += OnAttachmentCompleted;
        AssemblyEvents.AssemblyStateChanged += RebuildState;
        RebuildState();
    }

    private void OnDisable()
    {
        AssemblyEvents.AttachmentCompleted -= OnAttachmentCompleted;
        AssemblyEvents.AssemblyStateChanged -= RebuildState;
    }

    public bool CanAcceptStep(AttachmentSocket socket, AttachableObject childObject)
    {
        if (recipe == null)
            return true;

        RebuildState();

        if (isCompleted)
            return true;

        WorkbenchPart parentPart = socket.GetComponentInParent<WorkbenchPart>();
        WorkbenchPart childPart = childObject.GetComponent<WorkbenchPart>();

        if (parentPart == null || childPart == null)
            return false;

        if (recipe.assemblyMode == RecipeAssemblyMode.Unordered)
            return true;

        if (currentStepIndex < 0 || currentStepIndex >= recipe.steps.Count)
            return false;

        return StepMatches(recipe.steps[currentStepIndex], socket, parentPart, childPart);
    }

    private void OnAttachmentCompleted(AttachmentSocket socket, AttachableObject childObject)
    {
        RebuildState();
    }

    private void RebuildState()
    {
        completedStepIndices.Clear();
        currentStepIndex = 0;
        isCompleted = false;

        if (recipe == null)
            return;

        AttachmentSocket[] sockets = GetComponentsInChildren<AttachmentSocket>(true);

        if (recipe.assemblyMode == RecipeAssemblyMode.Unordered)
        {
            for (int i = 0; i < recipe.steps.Count; i++)
            {
                if (HasMatchingCompletedConnection(recipe.steps[i], sockets))
                {
                    completedStepIndices.Add(i);
                }
            }

            if (completedStepIndices.Count >= recipe.steps.Count)
            {
                isCompleted = true;
            }

            return;
        }

        // Ordered
        for (int i = 0; i < recipe.steps.Count; i++)
        {
            if (HasMatchingCompletedConnection(recipe.steps[i], sockets))
            {
                currentStepIndex++;
            }
            else
            {
                break;
            }
        }

        if (currentStepIndex >= recipe.steps.Count)
        {
            isCompleted = true;
        }
    }

    private bool HasMatchingCompletedConnection(AssemblyRecipeStep step, AttachmentSocket[] sockets)
    {
        foreach (AttachmentSocket socket in sockets)
        {
            if (socket == null)
                continue;

            if (!socket.HasAttachedObject)
                continue;

            if (socket.TargetProgress < 1f)
                continue;

            AttachableObject childObject = socket.AttachedObject;
            if (childObject == null)
                continue;

            WorkbenchPart parentPart = socket.GetComponentInParent<WorkbenchPart>();
            WorkbenchPart childPart = childObject.GetComponent<WorkbenchPart>();

            if (parentPart == null || childPart == null)
                continue;

            if (StepMatches(step, socket, parentPart, childPart))
                return true;
        }

        return false;
    }

    private bool StepMatches(
        AssemblyRecipeStep step,
        AttachmentSocket socket,
        WorkbenchPart parentPart,
        WorkbenchPart childPart)
    {
        if (!string.IsNullOrEmpty(step.socketId) && socket.SocketId != step.socketId)
            return false;

        if (step.parentType != AttachmentType.None && parentPart.PartType != step.parentType)
            return false;

        if (!string.IsNullOrEmpty(step.parentPartId) && parentPart.PartId != step.parentPartId)
            return false;

        if (step.childType != AttachmentType.None && childPart.PartType != step.childType)
            return false;

        if (!string.IsNullOrEmpty(step.childPartId) && childPart.PartId != step.childPartId)
            return false;

        return true;
    }
}
using UnityEngine;

public class AssemblyRecipeRunner : MonoBehaviour
{
    [SerializeField] private AssemblyRecipe recipe;

    [Header("Состояние")]
    [SerializeField] private int currentStepIndex;
    [SerializeField] private bool isCompleted;

    public bool IsCompleted => isCompleted;

    private void OnEnable()
    {
        AssemblyEvents.AttachmentCompleted += OnAttachmentCompleted;
    }

    private void OnDisable()
    {
        AssemblyEvents.AttachmentCompleted -= OnAttachmentCompleted;
    }

    public bool CanAcceptStep(AttachmentSocket socket, AttachableObject childObject)
    {
        if (recipe == null)
            return true;

        if (isCompleted)
            return false;

        if (currentStepIndex < 0 || currentStepIndex >= recipe.steps.Count)
            return false;

        WorkbenchPart parentPart = socket.GetComponentInParent<WorkbenchPart>();
        WorkbenchPart childPart = childObject.GetComponent<WorkbenchPart>();

        if (parentPart == null || childPart == null)
            return false;

        return StepMatches(recipe.steps[currentStepIndex], socket, parentPart, childPart);
    }

    private void OnAttachmentCompleted(AttachmentSocket socket, AttachableObject childObject)
    {
        if (recipe == null || isCompleted)
            return;

        if (!socket.transform.IsChildOf(transform))
            return;

        if (currentStepIndex < 0 || currentStepIndex >= recipe.steps.Count)
            return;

        WorkbenchPart parentPart = socket.GetComponentInParent<WorkbenchPart>();
        WorkbenchPart childPart = childObject.GetComponent<WorkbenchPart>();

        if (parentPart == null || childPart == null)
            return;

        if (!StepMatches(recipe.steps[currentStepIndex], socket, parentPart, childPart))
            return;

        currentStepIndex++;

        if (currentStepIndex >= recipe.steps.Count)
        {
            isCompleted = true;
            Debug.Log($"Рецепт [{recipe.recipeId}] завершен.");
        }
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
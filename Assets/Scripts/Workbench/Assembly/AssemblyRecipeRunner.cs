using System.Collections.Generic;
using UnityEngine;

public class AssemblyRecipeRunner : MonoBehaviour
{
    [SerializeField] private AssemblyRecipe recipe;

    [Header("���������")]
    [SerializeField] private int currentStepIndex;
    [SerializeField] private bool isCompleted;

    private readonly List<int> completedStepIndices = new();

    public bool IsCompleted => isCompleted;
    public AssemblyRecipe Recipe => recipe;

    public void RefreshState()
    {
        RebuildState();
    }

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
        {
            Debug.LogWarning($"[RecipeRunner DEBUG] recipe=null на {name}");
            return;
        }

        AttachmentSocket[] sockets = GetComponentsInChildren<AttachmentSocket>(true);
        Debug.Log($"[RecipeRunner DEBUG] RebuildState: {name}, recipe={recipe.recipeId}, steps={recipe.steps.Count}, " +
                  $"sockets={sockets?.Length}, mode={recipe.assemblyMode}");

        foreach (var s in sockets)
        {
            string childName = s.AttachedObject != null ? s.AttachedObject.name : "none";
            Debug.Log($"[RecipeRunner DEBUG]   socket={s.SocketId}, HasAttached={s.HasAttachedObject}, " +
                      $"progress={s.TargetProgress:F2}, child={childName}");
            if (s.HasAttachedObject && s.AttachedObject != null)
            {
                WorkbenchPart parent = s.GetComponentInParent<WorkbenchPart>();
                WorkbenchPart child = s.AttachedObject.GetComponent<WorkbenchPart>();
                Debug.Log($"[RecipeRunner DEBUG]     parentPartId={parent?.PartId}, childPartId={child?.PartId}, " +
                          $"parentType={parent?.PartType}, childType={child?.PartType}");
            }
        }

        if (recipe.assemblyMode == RecipeAssemblyMode.Unordered)
        {
            for (int i = 0; i < recipe.steps.Count; i++)
            {
                if (HasMatchingCompletedConnection(recipe.steps[i], sockets))
                {
                    completedStepIndices.Add(i);
                    Debug.Log($"[RecipeRunner DEBUG] Шаг {i} ({recipe.steps[i].childPartId}) ПРОЙДЕН");
                }
                else
                {
                    Debug.Log($"[RecipeRunner DEBUG] Шаг {i} ({recipe.steps[i].childPartId}) НЕ ПРОЙДЕН");
                }
            }

            if (completedStepIndices.Count >= recipe.steps.Count)
            {
                isCompleted = true;
                Debug.Log($"[RecipeRunner DEBUG] СБОРКА ЗАВЕРШЕНА!");
            }
            else
            {
                Debug.Log($"[RecipeRunner DEBUG] Завершено {completedStepIndices.Count}/{recipe.steps.Count} шагов");
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
            {
                Debug.Log($"[RecipeRunner DEBUG]   HasMatching: socket={socket.SocketId} — нет прикрепленного объекта");
                continue;
            }

            if (socket.TargetProgress < 1f)
            {
                Debug.Log($"[RecipeRunner DEBUG]   HasMatching: socket={socket.SocketId} — прогресс {socket.TargetProgress:F2} < 1");
                continue;
            }

            AttachableObject childObject = socket.AttachedObject;
            if (childObject == null)
                continue;

            WorkbenchPart parentPart = socket.GetComponentInParent<WorkbenchPart>();
            WorkbenchPart childPart = childObject.GetComponent<WorkbenchPart>();

            if (parentPart == null || childPart == null)
                continue;

            bool matches = StepMatches(step, socket, parentPart, childPart);
            Debug.Log($"[RecipeRunner DEBUG]   HasMatching: socket={socket.SocketId}, parent={parentPart.PartId}, " +
                      $"child={childPart.PartId} — {(matches ? "СОВПАДАЕТ" : "НЕ СОВПАДАЕТ")}");
            if (matches)
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
        {
            Debug.Log($"[StepMatches] socketId FAIL: step={step.socketId}, actual={socket.SocketId}");
            return false;
        }

        if (step.parentType != AttachmentType.None && parentPart.PartType != step.parentType)
        {
            Debug.Log($"[StepMatches] parentType FAIL: step={step.parentType}, actual={parentPart.PartType}");
            return false;
        }

        if (!string.IsNullOrEmpty(step.parentPartId) && parentPart.PartId != step.parentPartId)
        {
            Debug.Log($"[StepMatches] parentPartId FAIL: step={step.parentPartId}, actual={parentPart.PartId}");
            return false;
        }

        if (step.childType != AttachmentType.None && childPart.PartType != step.childType)
        {
            Debug.Log($"[StepMatches] childType FAIL: step={step.childType}, actual={childPart.PartType}");
            return false;
        }

        if (!string.IsNullOrEmpty(step.childPartId) && childPart.PartId != step.childPartId)
        {
            Debug.Log($"[StepMatches] childPartId FAIL: step={step.childPartId}, actual={childPart.PartId}");
            return false;
        }

        return true;
    }
}
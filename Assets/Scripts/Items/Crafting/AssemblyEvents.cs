using System;

public static class AssemblyEvents
{
    public static event Action<AttachmentSocket, AttachableObject> AttachmentCompleted;
    public static event Action AssemblyStateChanged;

    public static void RaiseAttachmentCompleted(AttachmentSocket socket, AttachableObject obj)
    {
        AttachmentCompleted?.Invoke(socket, obj);
        AssemblyStateChanged?.Invoke();
    }

    public static void RaiseAssemblyStateChanged()
    {
        AssemblyStateChanged?.Invoke();
    }
}
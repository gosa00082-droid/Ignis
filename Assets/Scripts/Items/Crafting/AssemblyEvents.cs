using System;

public static class AssemblyEvents
{
    public static event Action<AttachmentSocket, AttachableObject> AttachmentCompleted;

    public static void RaiseAttachmentCompleted(AttachmentSocket socket, AttachableObject obj)
    {
        AttachmentCompleted?.Invoke(socket, obj);
    }
}
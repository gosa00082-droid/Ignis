using System;
using System.Collections.Generic;

[System.Serializable]
public class AssemblySnapshot
{
    public string recipeId;
    public List<AttachedPartData> parts;
    public float completionProgress;
}
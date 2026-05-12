using System;
using System.Collections.Generic;

[System.Serializable]
public class InventoryItem
{
    public string uniqueId;
    public string baseItemId;
    public List<string> tags;
    public int stackCount;

    public bool isAssembly;
    public AssemblySnapshot snapshot;
}
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Ship Component", menuName = "Ship/Ship Component")]
[Serializable]
public class ShipComponent : ScriptableObject
{
    public string Name;
    public string Description;
    public Texture2D Icon;
    public Dimensions SlotDimension;
}
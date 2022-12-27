using UnityEngine;
[CreateAssetMenu(fileName = "Element", menuName = "ScriptableObjects/Create New Element", order = 1)]
public class Element : ScriptableObject
{
    public string className;
    public Color baseColor;
    public float density;
    public PhyState state;
    public string[] tags;
    public enum PhyState
    {
        Gas, Liquid, Powder, Solid, Immovable
    }

}

using UnityEngine;
[CreateAssetMenu(fileName = "React", menuName = "ScriptableObjects/Create New Reaction", order = 2)]
public class Reactions : ScriptableObject
{
    public string ReactantOne;
    public string ReactantTwo;
    public Element ProductOne;
    public Element ProductTwo;
    public float Chance;
}

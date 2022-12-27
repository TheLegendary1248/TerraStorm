using UnityEngine;

public class VelocityIndicator : MonoBehaviour
{
    public LineRenderer line;
    Vector2 target;
    static readonly Color normal = new Color(0.5f, 0.5f, 1f);
    public void Change(Vector2 pos) => target = pos;
    public void Update()
    {
        line.SetPosition(1, Vector2.Lerp(line.GetPosition(1), target, Time.deltaTime * 30));
        line.endColor = normal + new Color(target.x / 2f, target.y / 2f, 0f);
    }

}

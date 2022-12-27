using UnityEngine;

public class World_BorderEditor : MonoBehaviour
{
    public LineRenderer line;
    private void Awake()
    {
        UpdateBorder();
        World.mainWorldChanged += UpdateBorder;
        World.MainWorld.borderChanged += UpdateBorder;
    }
    void UpdateBorder()
    {
        line.SetPositions(
            new Vector3[] {
                new Vector2( World.MainWorld.RightBorder * 16 + 24, World.MainWorld.TopBorder * 16 + 24),
                new Vector2( World.MainWorld.RightBorder * 16 + 24, World.MainWorld.BottomBorder * 16 - 8),
                new Vector2( World.MainWorld.LeftBorder * 16 - 8, World.MainWorld.BottomBorder * 16 - 8),
                new Vector2( World.MainWorld.LeftBorder * 16 - 8, World.MainWorld.TopBorder * 16 + 24)
            });
    }
}

using System.Collections.Generic;
using UnityEngine;
using TMPro;
/// <summary>
/// Generic painting tool for the falling sand engine
/// </summary>
public class PaintCursor : MonoBehaviour
{
    // Start is called before the first frame update
    public SpriteRenderer cursorRend;
    public SpriteRenderer chunkRend;
    public TextMesh cursorText;
    public TextMesh chunkText;
    public GameObject cursor;
    public GameObject chunk;
    public GameObject velocityView;
    public GameObject velInd;
    public Vector2Int chunkPos;
    public TextMeshProUGUI tx;
    public int index = -1;
    private void Awake()
    {
        World.MainWorld.afterSimUpdate += UpdateVelocity;
        int i = 0;
        while (i < 256)
        {
            Instantiate(velInd, new Vector2(i % 16, i++ / 16) + (Vector2.one * 0.5f), Quaternion.identity, velocityView.transform).SetActive(true);
        }
    }
    // Update is called once per frame
    void Update()
    {
        chunkRend.color = cursorRend.color = new Color(1, 1, 1, (Mathf.Sin(Time.unscaledTime * 5f) + 1) / 4); //Set a flashing white color
        Vector2 v = Camera.main.ScreenToWorldPoint(Input.mousePosition); //Get pt on screen
        if (Input.GetMouseButton(0) & index != 0)
        {
            int x = Mathf.FloorToInt(v.x);
            int y = Mathf.FloorToInt(v.y);
            World.PaintSingle(new Vector2Int(x, y), (byte)index);
            /*World.PaintSingle(new Vector2Int(x + 1, y), (byte)index);
            World.PaintSingle(new Vector2Int(x, y + 1), (byte)index);
            World.PaintSingle(new Vector2Int(x + 1, y + 1), (byte)index);*/
        }
        v.x = Mathf.FloorToInt(v.x); v.y = Mathf.FloorToInt(v.y); //Integerize
        cursor.transform.position = v; //Set pos
        Vector2Int c = World.ChunkCoord(v);
        if (chunkPos != c)//If chunk has been exited, reupdate the velocity indicator to be correct
        {
            chunkPos = c;
            UpdateVelocity();
        }
        velocityView.transform.position = chunk.transform.position = (Vector2)c * 16;
        cursorText.text = $"({v.x}, {v.y})";
        chunkText.text = $"[{c.x}, {c.y}]";
        if(Input.GetMouseButtonDown(2))
        {
            index++;
            if (index >= World.elements.Count) index = 0;
            tx.text = World.elements[index].name;
        }
        
    }
    void UpdateVelocity()
    {
        WorldChunk chk;
        if (World.MainWorld.chunkList.TryGetValue(chunkPos, out chk))
        {
            velocityView.SetActive(true);
            Dictionary<Vector2Byte, TwoByteVel> list = chk.velList;
            for (Vector2Byte i = 0; i.actualValue < 254; i.actualValue++)
            {
                TwoByteVel vel;
                bool b = list.TryGetValue(i, out vel);
                //velocityView.transform.GetChild(i.actualValue).gameObject.SetActive(b);
                velocityView.transform.GetChild(i.actualValue).GetComponent<VelocityIndicator>().Change(b ? vel : Vector2.zero);

            }
        }
        else
        {
            velocityView.SetActive(false);
        }

    }

}

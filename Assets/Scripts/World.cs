using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Overall manager of the sand simulation world. All painting and updates go through here. NOTE TO ME: Make as separate as possible from Unity so it can be rebuilt with ease for other engines and languages. hint hint wink wink probably use 'partial'.
/// </summary>
public class World : MonoBehaviour
{
    ///<summary>If true, pixels at the try to cross the world border will be deleted. If not, pixels will instead treat the world border as the edge</summary>
    public bool deleteAtEdge;
    ///<summary>Automatically expands the border when any of the paint functions are used outside of it if true.</summary>
    public static bool ignoreBorderOnPaint = true;
    public static float FramesPerSecond;
    /// <summary>
    /// Used to access several world objects in case you want separate worlds/dimensions
    /// </summary>
    public static List<World> worlds = new List<World>();
    /// <summary>
    /// The main chunk list. If multiple Worlds are used, use with caution.
    /// </summary>
    public static Dictionary<Vector2Int, WorldChunk> mainChunkList;
    ///<summary>The main world object. If there are multiple Worlds, use with caution.</summary>
    static World _mainWorld = null;
    public static World MainWorld { get => _mainWorld; set { if (mainWorldChanged != null && value != _mainWorld) mainWorldChanged(); _mainWorld = value; } }
    public Dictionary<Vector2Int, WorldChunk> chunkList = new Dictionary<Vector2Int, WorldChunk>();
    public event System.Action beforeSimUpdate;
    public event System.Action stage_reactant;
    public event System.Action stage_transform;
    public event System.Action stage_random;
    public event System.Action simulationUpdate;
    public event System.Action afterSimUpdate;
    ///<summary>Fires when the world border has changed.</summary>
    public event System.Action borderChanged;
    public static event System.Action mainWorldChanged;
    /// <summary>
    /// 
    /// </summary>
    public static GameObject chunkPrefab;
    ///Indicates which sixteenth of a frame update we're on. Used for fractional velocities
    public byte Sixteenth { get; private set; }
    public int FrameSkips = 6;
    public int i = 0;
    ///<summary>The top world border in chunks. Any attempt to create a chunk outside of it will fail. If this is set lower than bottomBorder, bottomBorder will automatically be set to one lower.</summary>
    public int TopBorder { get => _top; set { if (value != _top) borderChanged(); _top = value; if (value < _bottom) _bottom = value - 1; } }
    private int _top = 5;
    ///<summary>The bottom world border in chunks. Any attempt to create a chunk outside of it will fail. If this is set higher than topBorder, topBorder will automatically be set to one higher.</summary>
    public int BottomBorder { get => _bottom; set { if (value != _bottom) borderChanged(); _bottom = value; if (value > _top) _top = value + 1; } }
    private int _bottom = -5;
    ///<summary>The right world border in chunks. Any attempt to create a chunk outside of it will fail. If this is set lower than leftBorder, leftBorder will automatically be set to one lower.</summary>
    public int RightBorder { get => _right; set { if (value != _right) borderChanged(); _right = value; if (value < _left) _left = value - 1; } }
    private int _right = 5;
    ///<summary>The left world border in chunks. Any attempt to create a chunk outside of it will fail. If this is set higher than rightBorder, rightBorder will automatically be set to one higher.</summary>
    public int LeftBorder { get => _left; set { if (value != _left) borderChanged(); _left = value; if (value > _right) _right = value + 1; } }
    private int _left = -5;
    public int movingPixels = 0;
    public int collisions = 0;
    public static List<Element> elements;

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        chunkPrefab = Resources.Load("Chunk") as GameObject;
        elements = new List<Element>(System.Array.ConvertAll(Resources.LoadAll("Elements", typeof(Element)), item => (Element)item));
        Element nul = ScriptableObject.CreateInstance("Element") as Element;
        nul.name = "Nothing";
        elements.Insert(0, nul);
    }
    private void Awake()
    {
        if (MainWorld == null) MainWorld = this;
        mainChunkList = chunkList;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    ///<summary>Plays a single frame in the world</summary>
    public void Frame()
    {
        //Debug.Log("NEW FRAME");
        Sixteenth++;
        if (Sixteenth > 15) Sixteenth = 0;
        if (beforeSimUpdate != null) beforeSimUpdate();
        if (simulationUpdate != null) simulationUpdate();
        if (afterSimUpdate != null) afterSimUpdate();
    }
    private void FixedUpdate()
    {
        if (++i > FrameSkips)
        {
            movingPixels = 0;
            collisions = 0;
            Frame();
            i = 0;
            //Debug.Log(movingPixels);
        }
    }
    /// <summary>
    /// Returns the relative chunk position
    /// </summary>
    public static Vector2Int ChunkCoord(Vector2Int v) => new Vector2Int(v.x >> 4, v.y >> 4);
    public static Vector2Int ChunkCoord(Vector2 v) => new Vector2Int(Mathf.FloorToInt(v.x) >> 4, Mathf.FloorToInt(v.y) >> 4);
    public static byte ElementCoord(Vector2Int v) => (byte)(((v.y % 16) << 4) | ((byte)(v.x << 4) >> 4));
    /// <summary>
    /// Paint a single pixel in the world
    /// </summary>
    public static void PaintSingle(Vector2Int pos, byte element)
    {
        if (mainChunkList.ContainsKey(ChunkCoord(pos))) //Chunk found
        {
            WorldChunk c = mainChunkList[ChunkCoord(pos)];
            c.PaintSingle(pos, element, true);

        }
        else //Chunk not found
        {
            WorldChunk c;
            if (MainWorld.CreateChunk(pos, out c, ignoreBorderOnPaint)) c.PaintSingle(pos, element, true);

        }
    }
    public bool CreateChunk(Vector2Int pos, out WorldChunk chunk, bool ignoreBorder)
    {
        Vector2Int chunkCoord = ChunkCoord(pos);
        bool within = chunkCoord.y >= _bottom & chunkCoord.y <= _top & chunkCoord.x >= _left & chunkCoord.x <= _right;
        if (!within & ignoreBorder)
        {
            _left = Mathf.Min(_left, chunkCoord.x);
            _bottom = Mathf.Min(_bottom, chunkCoord.y);
            _right = Mathf.Max(_right, chunkCoord.x);
            _top = Mathf.Max(_top, chunkCoord.y);
            borderChanged?.Invoke();
        }
        if (within)
        {
            GameObject c = Instantiate(chunkPrefab, (Vector2)ChunkCoord(pos) * 16, Quaternion.identity, transform);
            chunk = c.GetComponent<WorldChunk>();
            chunkList.Add(chunkCoord, chunk);
            chunk.world = this;
            return true;
        }
        chunk = null;
        return false;
    }
    ///<summary>Destroying a world will set the main world variable to be the first world in the worlds list</summary>
    private void OnDestroy()
    {
        worlds.Remove(this);
        if (MainWorld == this) if (worlds.Count > 0) MainWorld = worlds[0];
    }
}

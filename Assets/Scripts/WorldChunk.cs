using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Contains the elements within a chunk
/// </summary>

public class WorldChunk : MonoBehaviour
{
    public World world;
    Vector2Int chunkPos; //Position of the chunk relative in chunks
    SpriteRenderer sprite; //The sprite for the texture
    Texture2D tex; //And hence, texture...
    byte[] elementList = new byte[256]; //The element list
    public Dictionary<Vector2Byte, TwoByteVel> velList = new Dictionary<Vector2Byte, TwoByteVel>();//Velocity list
    public Text txt;
    static Color32[] gridtex = new Color32[256];
    ///<summary>This array contains booleans telling on which sixteenth of a frame to move for fractional velocities</summary>
    static bool[,] fractionalDifference = new bool[16, 16];
    static Vector2Byte[,][] traverseMatrices = new Vector2Byte[9, 9][];
    ///Pool of Color arrays for changing the texture
    static ArrayPool<Color32> reusedTex = ArrayPool<Color32>.Create();
    ///Pool of byte arrays for transform stage
    static ArrayPool<byte> reusedElement = ArrayPool<byte>.Create();
    

    [RuntimeInitializeOnLoadMethod]
    static void Init() //Cache code-generated data
    {

        //Generate base grid texture
        int i = -1;
        while (++i < 256) gridtex[i] = new Color32(255, 255, 255,
            (byte)(i / 16 % 8 == 7 ? 20 : i % 16 % 8 == 7 ? 20 : 5)
            );
        i = 0;
        //Generate difference array for fractional velocities
        for (; i < 16; i++)
        {
            float measure = i / 16f;
            for (int k = 0; k < 16; k++)
                fractionalDifference[i, k] = (int)(measure * k) != (int)(measure * (k + 1));
        }
        //Generate traverse matrices
        for (int x = 0; x <= 8; x++)//X increment
            for (int y = 0; y <= 8; y++)//Y Increment
                traverseMatrices[x, y] = System.Array.ConvertAll(GenerateLine(x, y), item => (Vector2Byte)item);
    }
    #region Simulation
    ///<summary>Main Simulation Loop</summary>
    void RunSim()
    {
        TransformStage();
    }
    ///<summary>Runs the alchemy part of the simulation</summary>
    void ReactantStage()
    {

    }
    enum DETECT_TYPE
    {
        NOTHING,
        HIT_MOVED,
        HIT_UNUPDATED,
        HIT_STATIC
    }
    void TransformStage() //Major space improvement possible here by caching the already made arrays. Also, can save on swapping the color arrays
    {

        //--MOVE CHUNK PIXELS--//
        byte[] newElementList = reusedElement.Rent(256);
        elementList.CopyTo(newElementList, 0);
        Color32[] oldTex = tex.GetPixels32(0); //Get the old texture in order to get the correct pixel data. 
        Color32[] newTex = reusedTex.Rent(256);
        oldTex.CopyTo(newTex, 0);
        Dictionary<Vector2Byte, TwoByteVel> newVelList = new Dictionary<Vector2Byte, TwoByteVel>();
        Vector2Byte[] keys = new Vector2Byte[velList.Count];
        velList.Keys.CopyTo(keys, 0);
        //foreach (Vector2Byte item in keys) Debug.Log($"{elementList[item.actualValue]}, {(Vector2)item}");
        //Traverse Algorithm
        for (int i = 0; i < keys.Length; i++)
        {
            ///<summary>hello</summary>
            Vector2Byte pix = keys[i],  //pix Represent's the simulated pixel
                move = pix,             //mov Represents the spot we want to check for CheckElement()
                hit;                    //hit Represents the pixel we're colliding with for Collision()
            TwoByteVel vel = velList[pix];
            (int x, int y) mov = (
                vel.xWhole + (fractionalDifference[Mathf.Abs(vel.xFrac), world.Sixteenth] ? (int)Mathf.Sign(vel.x) : 0),
                vel.yWhole + (fractionalDifference[Mathf.Abs(vel.yFrac), world.Sixteenth] ? (int)Mathf.Sign(vel.y) : 0));

            bool canMove, shouldFail = false;
            DETECT_TYPE d;
            int iter = 0;
            void CheckElement() //Checks if an element occupies the "move" space
            {
                if (newElementList[move.actualValue] == 0)//Nothing in element list
                {
                    canMove = true;
                    d = DETECT_TYPE.NOTHING;
                }
                else //Something in element list
                {
                    if (newVelList.ContainsKey(move.actualValue))//Hit a moved pixel
                    {
                        canMove = false;
                        d = DETECT_TYPE.HIT_MOVED;
                    }
                    else
                    {
                        if (velList.ContainsKey(move.actualValue))//If it hit a pixel, but that pixel is copied over and unupdated;
                        {
                            canMove = true;
                            d = DETECT_TYPE.HIT_UNUPDATED;
                        }
                        else //Hit a static pixel
                        {
                            canMove = false;
                            d = DETECT_TYPE.HIT_STATIC;
                        }
                    }
                }
            }
            void Collision() //Where two elements collide
            {
                world.collisions++;
                if (World.elements[elementList[hit.actualValue]].state == Element.PhyState.Immovable)
                { vel = new TwoByteVel(0, 0); return; }
                bool has = newVelList.ContainsKey(hit);
                if (has)
                {
                    TwoByteVel copy = newVelList[hit];
                    newVelList[hit] = vel / 2;
                    vel = copy / 2;
                }
                else
                {
                    newVelList.Add(hit, vel / 2);
                    newVelList.Remove(pix);
                }
            }
            void AddElement()
            {
                bool rep = false;
                if (rep = !newVelList.ContainsKey(pix)) newElementList[pix.actualValue] = 0;//If there's NOT an updated pixel where we're moving from, update
                try
                {
                    if(vel != new TwoByteVel(0,0))
                    newVelList.Add(move, vel - (World.elements[elementList[pix.actualValue]].state == Element.PhyState.Gas ? new TwoByteVel(0, -1): new TwoByteVel(0, 1)));
                }             //Add our just-moved pixel
                catch (System.Exception e) { throw new System.Exception(d.ToString() + ", " + iter + ", " + ((Vector2)pix).ToString() + ((Vector2)move).ToString() + ", " + e.Message); } 
                //Add our just-moved pixel

                newElementList[move.actualValue] = elementList[pix.actualValue];    //Add the pixel from the old element list
                if (rep) newTex[pix.actualValue] = GetGridCol(pix.x, pix.y);        //Replace the texture color if there isn't a pixel there        
                newTex[move.actualValue] = oldTex[pix.actualValue];                 //Add the pixel's color from the old texture
            }
            void TryJump() //Attempts to shove the pixel up if it can't occupy it's move and stay position(MAKE PIXEL MOVE SPECIFICALLY AGAINST THE PARTICLE'S UP)
            {
                for (int up = 0; up < 16; up++)//If it can NOT stay where it is
                {
                    move.actualValue += 0b00010000;
                    CheckElement();
                    //Debug.Log("Had to jump sir");
                    if (canMove) { break; }
                    if (up > 14) shouldFail = true;
                }
            }
            if(World.elements[elementList[pix.actualValue]].state == Element.PhyState.Immovable)
            {
                vel = new TwoByteVel(0, 0);
                move = pix;
                CheckElement();//If it can stay where it is
                if (!canMove)
                    TryJump();
                AddElement();
            }
            if (Mathf.Abs(mov.x) < 2 && Mathf.Abs(mov.y) < 2)//If we move no further than a single pixel
            {
                iter = -1;
                move = new Vector2Byte(pix.x + mov.x, pix.y + mov.y); //Set move to added
                CheckElement();
                if (canMove) AddElement(); //If it can move forward
                else
                {
                    hit = move;
                    move = pix;
                    CheckElement();//If it can stay where it is
                    if (!canMove)
                        TryJump();
                    else Collision();
                    AddElement();
                }
            }
            else //If we move more than one pixel
            {
                Vector2Byte[] traverse = traverseMatrices[Mathf.Abs(mov.x), Mathf.Abs(mov.y)];
                bool xFlip = System.Math.Sign(mov.x) == -1;
                bool yFlip = System.Math.Sign(mov.y) == -1;
                d = DETECT_TYPE.NOTHING;
                Vector2Byte before = pix;
                for (iter = 1; iter < traverse.Length; iter++) //Iterate through the traverse matrices
                {
                    Vector2Byte t = traverse[iter]; //Get next element in traversal
                    t.Flip(xFlip, yFlip); //Negate using flipping
                    move = pix + t; //Add negation
                    CheckElement(); //Check spot
                    if (!canMove) //If it can't move further then break and the move var to before
                    {

                        if (iter == 1) //If we got nowhere, test our spot
                        {
                            hit = move; //Switch hit to move
                            move = pix;
                            CheckElement();//If it can stay where it is
                            if (!canMove)//If it can't, try jumping
                                TryJump();
                            else { Collision(); }
                        }
                        else { hit = move; move = before; Collision(); }

                        break;
                    }
                    before = move;
                }
                if (!shouldFail) AddElement();

            }
        }
        world.movingPixels += newVelList.Count;
        velList = newVelList;
        reusedElement.Return(elementList, true);
        elementList = newElementList;
        tex.SetPixels32(newTex);
        reusedTex.Return(newTex, false);
        if (sprite.isVisible) tex.Apply(false);
    }
    void ExternalStage()//Stage where pixels that have moved outside of the chunk's boundaries are then simulated into the chunk they moved into
    {

    }
    #endregion
    public void Paint(byte[] elements)
    {

    }
    public void PaintSingle(Vector2Byte pos, byte element, bool overWrite)
    {
        if (elementList[pos.actualValue] == 0)
        {
            velList.Add(pos, new TwoByteVel((sbyte)Random.Range(-4, 5), (sbyte)Random.Range(-3, 4)));
            elementList[pos.actualValue] = element;
            /*          //Create a box collider here
            GameObject c = new GameObject("Box", typeof(BoxCollider2D)); 
            c.transform.SetParent(transform);
            c.transform.position = new Vector2(0.5f + pos.x, 0.5f + pos.y);
            */
            tex.SetPixel(pos.x, pos.y, World.elements[element].baseColor);
            tex.Apply();
        }
    }
    #region Unity Messages
    private void Awake()
    {

        World.MainWorld.simulationUpdate += RunSim; //Add to
        sprite = GetComponent<SpriteRenderer>();
        Texture2D tex = new Texture2D(16, 16);
        chunkPos.x = Mathf.FloorToInt(transform.localPosition.x) >> 4;
        chunkPos.y = Mathf.FloorToInt(transform.localPosition.y) >> 4;
        name = $"[{chunkPos.x},{chunkPos.y}] Chunk";
        tex.SetPixels32(gridtex);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        sprite.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0f), 1f);
        this.tex = tex;
    }
    private void OnDestroy()
    {
        Destroy(tex);
        World.MainWorld.simulationUpdate -= RunSim;
    }
    #endregion
    Color32 GetGridCol(int x, int y) => new Color32(255, 255, 255, (byte)(y % 8 == 7 ? 20 : x % 8 == 7 ? 20 : 5));
    ///<summary>Generates a line in pixels in a given matrix with origin 0,0. Update this algo at some point.</summary>
    public static Vector2Int[] GenerateLine(int x2, int y2)
    {
        //Gotta make this more readable for myself and future even though it was yoinked
        Vector2Int[] pts = new Vector2Int[Mathf.Abs(Mathf.Abs(x2) > Mathf.Abs(y2) ? x2 : y2) + 1];
        int w = x2;
        int h = y2;
        int x = 0; int y = 0;
        int dx1, dy1, dx2, dy2;
        dx1 = dy1 = dx2 = dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1; //this is not acceptable
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Mathf.Abs(w); //...
        int shortest = Mathf.Abs(h);
        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            pts[i] = new Vector2Int(x, y);
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
        return pts;
    }
}
public class ChunkHash<T>
{
    T[][] array = new T[16][];
}

/// <summary>
/// A representation of in-chunk coordinates (0-15) using just one byte
/// </summary>
public struct Vector2Byte
{
    //i think i might have to make it just one byte
    public byte actualValue;
    public int x => actualValue & 0b1111;
    public int y => actualValue >> 4;
    public void Flip(bool x, bool y) { if (x) actualValue ^= 0b0000_1111; if (y) actualValue ^= 0b1111_0000; }
    public Vector2Byte(int x, int y) { actualValue = (byte)(((y << 4) & 0b1111_0000) | (x & 0b1111)); }
    public Vector2Byte(int val) { actualValue = (byte)val; }
    public static implicit operator Vector2Byte(Vector2Int v) => new Vector2Byte(v.x % 16, v.y % 16);
    public static implicit operator Vector2Byte(int v) => new Vector2Byte(v);
    public static implicit operator Vector2Int(Vector2Byte v) => new Vector2Int(v.x, v.y);
    public static implicit operator Vector2(Vector2Byte v) => new Vector2(v.x, v.y);
    public static implicit operator Vector3(Vector2Byte v) => new Vector3(v.x, v.y, 0);
    public static Vector2Byte operator +(Vector2Byte a, Vector2Byte b) => new Vector2Byte(a.x + b.x, a.y + b.y);
    public static Vector2Byte operator -(Vector2Byte a, Vector2Byte b) => new Vector2Byte(a.x - b.x, a.y - b.y);
    public static bool operator ==(Vector2Byte a, Vector2Byte b) => a.actualValue == b.actualValue;
    public static bool operator !=(Vector2Byte a, Vector2Byte b) => a.actualValue != b.actualValue;
    public override int GetHashCode() => actualValue; //Implement my own hash table thingy in the future for extra speed, cuz ints are lame
}
///<summary>Struct that defines velocity for the sand simulation.</summary>
public struct TwoByteVel
{
    public sbyte x; //see if you can do some Two's complement hacky stuff here
    public sbyte y;
    ///<summary>Returns the numerator of the x fractional bits. Interpret it as n / 16</summary>
    public int xFrac => x % 16;
    ///<summary>Returns the numerator of the y fractional bits. Interpret it as n / 16</summary>
    public int yFrac => y % 16;
    public int xSign => System.Math.Sign(x);
    public int ySign => System.Math.Sign(y);
    ///<summary>Returns the whole number of the x whole bits with sign.</summary>
    public int xWhole => x / 16;
    ///<summary>Returns the whole number of the y whole bits with sign.</summary>
    public int yWhole => y / 16;
    ///<summary>The actual value represented by x.</summary>
    public float actualX => x / 16f;
    ///<summary>The actual value represented by y.</summary>
    public float actualY => y / 16f;
    #region Constructors
    ///<summary>Set struct as is.</summary>
    public TwoByteVel(sbyte x, sbyte y) { this.x = x; this.y = y; }
    ///<summary>Set struct with a predefined short. Y will use the left 8 bits and X will use the right 8 bits.</summary>
    public TwoByteVel(short val) { x = (sbyte)(val & 0b0000_0000_1111_1111); y = (sbyte)(val << 8); }
    ///<summary>Set struct with floats. The given floats will be multiplied by 16, floored then cast into signed bytes.</summary>
    public TwoByteVel(float x, float y) { this.x = (sbyte)Mathf.FloorToInt(x * 16f); this.y = (sbyte)Mathf.FloorToInt(y * 16f); }
    #endregion
    #region Operators
    public static TwoByteVel operator +(TwoByteVel a, TwoByteVel b) => new TwoByteVel((sbyte)(a.x + b.x), (sbyte)(a.y + b.y));
    public static TwoByteVel operator -(TwoByteVel a, TwoByteVel b) => new TwoByteVel((sbyte)(a.x - b.x), (sbyte)(a.y - b.y));
    public static TwoByteVel operator /(TwoByteVel a, float b) => new TwoByteVel((sbyte)(a.x / b), (sbyte)(a.y / b));
    public static TwoByteVel operator *(TwoByteVel a, float b) => new TwoByteVel((sbyte)(a.x * b), (sbyte)(a.y * b));
    public static bool operator ==(TwoByteVel a, TwoByteVel b) => a.x == b.x && a.x == b.x;
    public static bool operator !=(TwoByteVel a, TwoByteVel b) => a.x != b.x && a.x != b.x;
    #endregion
    #region Casts
    public static implicit operator Vector2(TwoByteVel v) => new Vector2(v.actualX, v.actualY);
    #endregion
}
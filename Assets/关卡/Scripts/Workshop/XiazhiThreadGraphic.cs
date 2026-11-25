using UnityEngine;
using UnityEngine.UI;

/// <summary>以整数像素矩形绘制丝线；旋转只重建这一格的 UI 网格。</summary>
public sealed class XiazhiThreadGraphic : MaskableGraphic
{
    private int mask;
    private bool connected;
    public void SetThread(int nextMask, bool isConnected)
    {
        mask = nextMask; connected = isConnected; raycastTarget = false; SetVerticesDirty();
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        Vector2 center = rect.center;
        if (mask == 0)
        {
            Quad(vh, new Rect(center.x - 4, center.y - 4, 8, 8), new Color32(149,114,70,255));
            return;
        }
        Stroke(vh, rect, 26, new Color32(133,96,62,255));
        Stroke(vh, rect, 18, connected ? new Color32(168,119,44,255) : new Color32(182,152,106,255));
        Stroke(vh, rect, 8, connected ? new Color32(255,229,156,255) : new Color32(237,216,170,255));
        if (connected) Stroke(vh, rect, 3, new Color32(255,245,195,255));
    }
    private void Stroke(VertexHelper vh, Rect rect, float width, Color32 ink)
    {
        Vector2 c = rect.center; float half = width * .5f;
        if ((mask & XiazhiPuzzle.North) != 0) Quad(vh, Rect.MinMaxRect(c.x-half,c.y,c.x+half,rect.yMax),ink);
        if ((mask & XiazhiPuzzle.East) != 0) Quad(vh, Rect.MinMaxRect(c.x,c.y-half,rect.xMax,c.y+half),ink);
        if ((mask & XiazhiPuzzle.South) != 0) Quad(vh, Rect.MinMaxRect(c.x-half,rect.yMin,c.x+half,c.y),ink);
        if ((mask & XiazhiPuzzle.West) != 0) Quad(vh, Rect.MinMaxRect(rect.xMin,c.y-half,c.x,c.y+half),ink);
        Quad(vh, new Rect(c.x-half,c.y-half,width,width),ink);
    }
    private static void Quad(VertexHelper vh, Rect r, Color32 ink)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(r.xMin,r.yMin),ink,Vector2.zero);
        vh.AddVert(new Vector3(r.xMin,r.yMax),ink,Vector2.zero);
        vh.AddVert(new Vector3(r.xMax,r.yMax),ink,Vector2.zero);
        vh.AddVert(new Vector3(r.xMax,r.yMin),ink,Vector2.zero);
        vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
    }
}

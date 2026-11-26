using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A wet cloth locally cleans one continuous heap of large eggs.</summary>
internal sealed class SpringSilkwormWashOverlay : MonoBehaviour
{
    private sealed class Egg { internal Vector2 point; internal Image image, dust; internal float dirt=1f,coverage=1f; }
    private const int Columns=6,Rows=4,AlphaSamples=16;
    private const float StrokeStep=14f,CleanRadius=112f,CleanTolerance=.12f;
    private static SpringSilkwormWashOverlay current;
    private readonly List<Egg> eggs=new List<Egg>();
    private readonly List<Sprite> heapTiles=new List<Sprite>();
    private RectTransform frame,cloth;
    private Image clothArt;
    private Text hint;
    private Button collect;
    private Action<bool> onClose;
    private float priorTimeScale,wetness;
    private bool dragging,complete;
    private Vector2 previous;
    private static readonly Rect Water=new Rect(65,665,360,205);
    private static readonly Vector2 Rest=new Vector2(1590,800);
    internal static bool IsOpen=>current!=null;
    internal float CleanFraction {get{float sum=0,area=0;foreach(var e in eggs){sum+=(1-e.dirt)*e.coverage;area+=e.coverage;}return area>0?sum/area:0;}}
    internal bool IsComplete=>complete;

    internal static bool Show(Sprite bucket,Sprite before,Sprite cleaned,Action<bool> close)
    {
        if(IsOpen)return false;
        var frame=WorkshopUi.NewCanvas("SpringSilkwormWashOverlay",23000);
        current=frame.gameObject.AddComponent<SpringSilkwormWashOverlay>();
        current.frame=frame;current.onClose=close;current.priorTimeScale=Time.timeScale;Time.timeScale=0;
        current.Build(bucket,cleaned);return true;
    }
    private void Build(Sprite bucket,Sprite cleaned)
    {
        WorkshopImmersive.Background(frame,WorkshopSeason.Spring);
        hint=WorkshopImmersive.Text(frame,"Wash Hint",new Rect(45,34,1480,62),"拿起右侧的布，蘸水后轻轻擦拭蚕种。");
        WorkshopImmersive.Button(frame,"Leave Bath",new Rect(1735,35,140,62),"返回",Close);
        WorkshopUi.Picture(frame,"Water Basin",new Rect(22,615,440,310),Resources.Load<Sprite>("WorkshopGameArt/BathTub")??bucket);
        WorkshopImmersive.Text(frame,"Water Label",new Rect(97,920,275,48),"清水",24,TextAnchor.MiddleCenter);
        var seed=Resources.Load<Sprite>("WordRevision/SpringEggHeap");
        if(seed==null)seed=cleaned;
        // Adjacent crops reconstruct one bitmap; each crop can lose its dirt locally.
        var heap=new Rect(630,370,660,440);
        var alpha=SampleHeapAlpha(seed);
        for(int row=0;row<Rows;row++)for(int col=0;col<Columns;col++)
        {
            var source=seed.rect;
            var crop=new Rect(source.x+source.width*col/Columns,source.y+source.height*(Rows-row-1)/Rows,source.width/Columns,source.height/Rows);
            var tile=Sprite.Create(seed.texture,crop,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
            heapTiles.Add(tile);
            var bounds=new Rect(heap.x+col*heap.width/Columns,heap.y+row*heap.height/Rows,heap.width/Columns,heap.height/Rows);
            var image=WorkshopUi.Picture(frame,"Heap Surface "+eggs.Count,bounds,tile,new Color(.49f,.43f,.27f),stretch:true);
            var dust=WorkshopUi.Picture(frame,"Heap Dirt "+eggs.Count,bounds,tile,new Color(.37f,.26f,.12f,.32f),stretch:true);
            // Transparent corners have no work to do; the wiping target follows the visible seeds.
            float amount=0;var center=Vector2.zero;
            for(int y=0;y<AlphaSamples;y++)for(int x=0;x<AlphaSamples;x++)
            {
                int sx=col*AlphaSamples+x,sy=(Rows-row-1)*AlphaSamples+y;
                float a=alpha[sy*Columns*AlphaSamples+sx].a/255f;
                amount+=a;
                center+=new Vector2(heap.x+(sx+.5f)*heap.width/(Columns*AlphaSamples),
                    heap.y+(1-(sy+.5f)/(Rows*AlphaSamples))*heap.height)*a;
            }
            var egg=new Egg{point=amount>0?center/amount:bounds.center,image=image,dust=dust,
                coverage=amount/(AlphaSamples*AlphaSamples),dirt=amount>0?1f:0f};
            eggs.Add(egg);PaintEgg(egg);
        }
        var dyeing=Resources.Load<WinterDyeingConfig>("WinterDyeingConfig");
        cloth=WorkshopUi.Rect(frame,"Damp Wiping Cloth",new Rect(Rest.x-110,Rest.y-90,220,180));
        clothArt=WorkshopUi.Picture(cloth,"Cloth",new Rect(0,0,220,180),dyeing!=null?dyeing.silk:Resources.Load<Sprite>("WorkshopGameArt/CuttingCloth"));
        clothArt.raycastTarget=true;clothArt.gameObject.AddComponent<SpringWashClothInput>().owner=this;
        clothArt.gameObject.AddComponent<Shadow>().effectDistance=new Vector2(5,-6);
        WorkshopImmersive.Text(frame,"Cloth Label",new Rect(1470,900,260,42),"擦拭布",24,TextAnchor.MiddleCenter);
        collect=WorkshopImmersive.Button(frame,"Collect Clean Eggs",new Rect(788,885,340,68),"收下蚕种",()=>Close(true));
        collect.gameObject.SetActive(false);
    }
    private static Color32[] SampleHeapAlpha(Sprite seed)
    {
        // Imported game sprites are not CPU-readable. A tiny temporary copy also avoids
        // counting empty pixels around the heap as invisible, compulsory cleaning targets.
        int width=Columns*AlphaSamples,height=Rows*AlphaSamples;
        var previousTarget=RenderTexture.active;
        var target=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
        Texture2D sample=null;
        try
        {
            var rect=seed.rect;var texture=seed.texture;
            Graphics.Blit(texture,target,new Vector2(rect.width/texture.width,rect.height/texture.height),
                new Vector2(rect.x/texture.width,rect.y/texture.height));
            RenderTexture.active=target;
            sample=new Texture2D(width,height,TextureFormat.RGBA32,false);
            sample.ReadPixels(new Rect(0,0,width,height),0,0);sample.Apply();
            return sample.GetPixels32();
        }
        finally
        {
            RenderTexture.active=previousTarget;
            RenderTexture.ReleaseTemporary(target);
            if(sample!=null)Destroy(sample);
        }
    }
    private Vector2 Point(PointerEventData data)
        =>Point(data.position,data.pressEventCamera);
    private Vector2 Point(Vector2 screen,Camera camera)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(frame,screen,camera,out var p);
        return new Vector2(p.x+960,540-p.y);
    }
    internal void BeginDrag(PointerEventData data)
    {
        if(complete||data.button!=PointerEventData.InputButton.Left)return;
        dragging=true;
        previous=data.pointerPressRaycast.gameObject!=null?Point(data.pressPosition,data.pressEventCamera):Point(data);
        cloth.SetAsLastSibling();
    }
    internal void Drag(PointerEventData data)
    {
        if(!dragging||complete)return;
        var p=Point(data);MoveCloth(p);
        float distance=Vector2.Distance(previous,p);
        if(distance>.01f)
        {
            // Integrate the entire stroke, rather than capping each mouse event. Fast
            // movements and sparse events now clean the same path as slow movements.
            int steps=Mathf.Max(1,Mathf.CeilToInt(distance/StrokeStep));
            float travel=distance/steps;
            for(int step=1;step<=steps;step++)
            {
                var point=Vector2.Lerp(previous,p,step/(float)steps);
                if(Water.Contains(point)) { wetness=1f;continue; }
                if(wetness<=0)continue;
                foreach(var egg in eggs)
                {
                    if(egg.dirt<=0||egg.coverage<=0)continue;
                    float reach=Vector2.Distance(egg.point,point);if(reach>CleanRadius)continue;
                    egg.dirt=Mathf.Max(0,egg.dirt-travel*.014f*(1-reach/(CleanRadius+30)));
                    if(egg.dirt<=CleanTolerance)egg.dirt=0;
                    PaintEgg(egg);
                }
                wetness=Mathf.Max(0,wetness-travel/17000f);
            }
        }
        else if(Water.Contains(p))wetness=1f;
        previous=p;
        clothArt.color=wetness>0?new Color(.72f,.88f,.95f):Color.white;
        CheckClean();
        if(!complete)hint.text=wetness>0
            ? "湿布擦过哪里，哪里就变干净。已清洁 "+Mathf.FloorToInt(CleanFraction*100)+"%"
            : "先把布蘸一点清水，再擦拭蚕种。";
    }
    private static void PaintEgg(Egg egg)
    {
        egg.image.color=Color.Lerp(Color.white,new Color(.49f,.43f,.27f),egg.dirt);
        egg.dust.color=new Color(.37f,.26f,.12f,egg.dirt*.32f);
    }
    internal void EndDrag(PointerEventData data){if(dragging&&!complete)Drag(data);dragging=false;MoveCloth(Rest);}
    private void MoveCloth(Vector2 p)=>cloth.anchoredPosition=new Vector2(p.x-110,-p.y+90);
    private void CheckClean()
    {
        if(complete)return;
        if(CleanFraction<.975f)return;
        foreach(var egg in eggs)if(egg.coverage>.08f&&egg.dirt>CleanTolerance)return;
        complete=true;dragging=false;
        foreach(var e in eggs){e.dirt=0;PaintEgg(e);}
        MoveCloth(Rest);hint.text="蚕种干净了，收好吧。";collect.gameObject.SetActive(true);WorkshopAudioFeedback.Confirm();
    }
    private void Update(){if(Input.GetKeyDown(KeyCode.Escape))Close();}
    private void Close()=>Close(false);
    private void Close(bool collected)
    {
        if(current!=this)return;current=null;Time.timeScale=priorTimeScale;
        WorkshopUi.CloseCanvas(frame);onClose?.Invoke(complete&&collected);
    }
    private void OnDestroy(){foreach(var tile in heapTiles)Destroy(tile);if(current==this){current=null;Time.timeScale=priorTimeScale;SilkwormWashingController.washingStarted=false;}}
}
internal sealed class SpringWashClothInput:MonoBehaviour,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    internal SpringSilkwormWashOverlay owner;
    public void OnInitializePotentialDrag(PointerEventData e)=>e.useDragThreshold=false;
    public void OnBeginDrag(PointerEventData e)=>owner.BeginDrag(e);
    public void OnDrag(PointerEventData e)=>owner.Drag(e);
    public void OnEndDrag(PointerEventData e)=>owner.EndDrag(e);
}

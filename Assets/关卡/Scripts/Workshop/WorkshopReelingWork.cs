using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Move the guide across the bobbin; each band retains its own wound silk.</summary>
internal sealed class WorkshopReelingWork:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    private const int Bands=12;
    private const float BandStartX = 1240f;
    private const float BandWidth = 21f;
    private const float BandSpacing = 20f;
    private const float FirstBandCenter = BandStartX + BandWidth / 2f;
    private const float GuideWidth = 42f;
    private const float GuideTravel = (Bands - 1) * BandSpacing;
    private readonly float[] wound=new float[Bands];
    private readonly RectTransform[] wraps=new RectTransform[Bands];
    private RectTransform area,guide,wheel,thread,layThread,nextBandMarker;
    private Text hint;
    private Action finished;
    private bool dragging,done;
    private float guideX=.5f,speed;
    private int fullBands;
    private int CurrentBand => Mathf.RoundToInt(guideX * (Bands - 1));
    internal float Progress {get{float total=0;foreach(float v in wound)total+=v;return total/Bands;}}
    internal bool Complete=>done;
    internal float WheelAngle=>wheel.localEulerAngles.z;
    internal void Build(RectTransform parent,Text status,Action complete)
    {
        hint=status;finished=complete;
        WorkshopUi.Picture(parent,"Reeling Machine Support",new Rect(35,210,720,720),Resources.Load<Sprite>("WordRevision/ReelingMachineSupport"));
        var rotor=WorkshopUi.Picture(parent,"Reeling Machine Wheel",new Rect(35+(85-31)*720f/128,210+(55-31)*720f/128,63*720f/128,63*720f/128),Resources.Load<Sprite>("WorkshopGameArt/ReelingWheel"));
        wheel=rotor.rectTransform;wheel.pivot=new Vector2(.5f,.5f);wheel.anchoredPosition+=new Vector2(wheel.sizeDelta.x/2,-wheel.sizeDelta.y/2);
        WorkshopUi.Picture(parent,"Silk Bobbin",new Rect(1060,370,550,480),Resources.Load<Sprite>("WorkshopGameArt/SilkBobbin"));
        // The silk wraps lie over the bobbin spindle, growing independently as the guide passes.
        for(int i=0;i<Bands;i++)
        {
            var strip=WorkshopUi.Picture(parent,"Silk Band "+i,new Rect(BandStartX+i*BandSpacing,570-i*4.5f,BandWidth,70),Resources.Load<Sprite>("WordRevision/SilkWrap"),new Color(1,1,1,0),stretch:true);
            wraps[i]=strip.rectTransform;
        }
        area=WorkshopUi.Rect(parent,"Guide Rail",new Rect(FirstBandCenter-GuideWidth/2f,805,GuideTravel+GuideWidth,78));
        var line=WorkshopUi.Fill(area,"Rail",new Rect(0,35,area.rect.width,6),new Color32(116,80,41,255));
        line.raycastTarget=false;
        var marker = WorkshopUi.Fill(area, "Next Winding Band", new Rect(0, 77, 16, 8), new Color32(255, 223, 115, 255));
        marker.raycastTarget = false;
        nextBandMarker = marker.rectTransform;
        nextBandMarker.gameObject.SetActive(false);
        guide=WorkshopUi.Rect(area,"Silk Guide",new Rect(0,0,GuideWidth,76));
        var knob=WorkshopUi.Fill(guide,"Guide Handle",new Rect(0,0,GuideWidth,76),new Color32(195,160,96,255));
        WorkshopUi.ApplyPixelShape(knob);knob.raycastTarget=true;
        knob.gameObject.AddComponent<WorkshopReelingGuide>().owner=this;
        var ring=WorkshopUi.Fill(guide,"Guide Opening",new Rect(10,16,22,24),new Color32(45,46,29,255));ring.raycastTarget=false;
        thread=WorkshopUi.Fill(parent,"Live Silk Thread",new Rect(0,0,400,3),new Color32(254,242,210,255)).rectTransform;
        thread.pivot=new Vector2(0,.5f);
        layThread=WorkshopUi.Fill(parent,"Guide To Bobbin Thread",new Rect(0,0,200,3),new Color32(254,242,210,255)).rectTransform;
        layThread.pivot=new Vector2(0,.5f);
        WorkshopImmersive.Text(parent,"Guide Hint",new Rect(1100,908,450,46),"左右拖动导丝环",26,TextAnchor.MiddleCenter);
        hint.text="拖住导丝环，在较薄的位置稍停，把丝绕满线轴。";
        SetGuide(.5f);
    }
    private void Update()
    {
        if (done) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, .1f);
        int band = CurrentBand;
        bool winding = dragging && wound[band] < 1f;
        speed = Mathf.MoveTowards(speed, winding ? 1f : 0f, dt * 3f);
        wheel.Rotate(0, 0, -speed * 160f * dt);
        if (!winding || speed <= .02f) return;

        float amount = wound[band] = Mathf.Min(1f, wound[band] + dt * .68f * speed);
        wraps[band].sizeDelta = new Vector2(BandWidth, 70f + amount * 80f);
        wraps[band].anchoredPosition = new Vector2(BandStartX + band * BandSpacing, -570f + band * 4.5f + amount * 40f);
        wraps[band].GetComponent<Image>().color = new Color(1, 1, 1, amount * .9f);
        if (amount < 1f) return;

        fullBands++;
        if (fullBands == Bands)
        {
            done = true;
            nextBandMarker.gameObject.SetActive(false);
            hint.text = "丝线绕匀了。";
            finished?.Invoke();
            return;
        }

        RefreshHint();
    }

    private void RefreshHint()
    {
        int band = CurrentBand;
        bool bandFull = wound[band] >= 1f;
        nextBandMarker.gameObject.SetActive(bandFull);
        if (!bandFull)
        {
            hint.text = $"在这里稍停，已绕满 {fullBands}/{Bands} 段。";
            return;
        }

        float guideOffset = guideX * GuideTravel;
        float nearestDistance = float.PositiveInfinity;
        float markerOffset = 0f;
        for (int i = 0; i < Bands; i++)
        {
            if (wound[i] >= 1f) continue;
            float offset = i * BandSpacing;
            float distance = Mathf.Abs(offset - guideOffset);
            if (distance >= nearestDistance) continue;
            nearestDistance = distance;
            markerOffset = offset;
        }

        nextBandMarker.anchoredPosition = new Vector2(GuideWidth / 2f + markerOffset - nextBandMarker.rect.width / 2f, -77f);
        hint.text = $"已绕满 {fullBands}/{Bands} 段，移到金色标记处补绕。";
    }
    private void SetGuide(float position)
    {
        guideX=Mathf.Clamp01(position);
        float guideOffset = guideX * GuideTravel;
        guide.anchoredPosition=new Vector2(guideOffset,0);
        var from=new Vector2(645,-520);var to=new Vector2(FirstBandCenter+guideOffset,-832);var delta=to-from;
        thread.anchoredPosition=from;thread.sizeDelta=new Vector2(delta.magnitude,3);
        thread.localEulerAngles=new Vector3(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        var bobbin=new Vector2(to.x,-605+guideX*50);var laying=bobbin-to;
        layThread.anchoredPosition=to;layThread.sizeDelta=new Vector2(laying.magnitude,3);layThread.localEulerAngles=new Vector3(0,0,Mathf.Atan2(laying.y,laying.x)*Mathf.Rad2Deg);
    }
    private void DragTo(PointerEventData e)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position, e.pressEventCamera, out var p)) return;
        int previousBand = CurrentBand;
        SetGuide((p.x - GuideWidth / 2f) / GuideTravel);
        if (CurrentBand != previousBand) RefreshHint();
    }
    public void OnBeginDrag(PointerEventData e){if(done)return;dragging=true;DragTo(e);}
    public void OnDrag(PointerEventData e){if(dragging&&!done)DragTo(e);}
    public void OnEndDrag(PointerEventData e){dragging=false;}
}
internal sealed class WorkshopReelingGuide:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    internal WorkshopReelingWork owner;
    public void OnBeginDrag(PointerEventData e)=>owner.OnBeginDrag(e);
    public void OnDrag(PointerEventData e)=>owner.OnDrag(e);
    public void OnEndDrag(PointerEventData e)=>owner.OnEndDrag(e);
}

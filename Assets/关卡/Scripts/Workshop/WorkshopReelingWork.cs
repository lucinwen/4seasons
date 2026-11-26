using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Move the guide across the bobbin; each band retains its own wound silk.</summary>
internal sealed class WorkshopReelingWork:MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    private const int Bands=12;
    private readonly float[] wound=new float[Bands];
    private readonly RectTransform[] wraps=new RectTransform[Bands];
    private RectTransform area,guide,wheel,thread,layThread;
    private Text hint;
    private Action finished;
    private bool dragging,done;
    private float guideX=.5f,speed;
    internal float Progress {get{float total=0;foreach(float v in wound)total+=Mathf.Min(1,v);return total/Bands;}}
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
            var strip=WorkshopUi.Picture(parent,"Silk Band "+i,new Rect(1240+i*20,570-i*4.5f,21,70),Resources.Load<Sprite>("WordRevision/SilkWrap"),new Color(1,1,1,0),stretch:true);
            wraps[i]=strip.rectTransform;
        }
        area=WorkshopUi.Rect(parent,"Guide Rail",new Rect(1219,805,264,78));
        var line=WorkshopUi.Fill(area,"Rail",new Rect(0,35,264,6),new Color32(116,80,41,255));
        line.raycastTarget=false;
        guide=WorkshopUi.Rect(area,"Silk Guide",new Rect(180,0,42,76));
        var knob=WorkshopUi.Fill(guide,"Guide Handle",new Rect(0,0,42,76),new Color32(195,160,96,255));
        WorkshopUi.ApplyPixelShape(knob);knob.raycastTarget=true;
        knob.gameObject.AddComponent<WorkshopReelingGuide>().owner=this;
        var ring=WorkshopUi.Fill(guide,"Guide Opening",new Rect(10,16,22,24),new Color32(45,46,29,255));ring.raycastTarget=false;
        thread=WorkshopUi.Fill(parent,"Live Silk Thread",new Rect(0,0,400,3),new Color32(254,242,210,255)).rectTransform;
        thread.pivot=new Vector2(0,.5f);
        layThread=WorkshopUi.Fill(parent,"Guide To Bobbin Thread",new Rect(0,0,200,3),new Color32(254,242,210,255)).rectTransform;
        layThread.pivot=new Vector2(0,.5f);
        WorkshopImmersive.Text(parent,"Guide Hint",new Rect(1100,908,450,46),"左右拖动导丝环",26,TextAnchor.MiddleCenter);
        hint.text="拖动导丝环，把丝均匀绕满线轴。";
        SetGuide(.5f);
    }
    private void Update()
    {
        if(done)return;
        float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
        int band=Mathf.Clamp(Mathf.FloorToInt(guideX*Bands),0,Bands-1);
        float target=dragging && wound[band]<1.02f ? 1f:0f;
        speed=Mathf.MoveTowards(speed,target,dt*3);
        wheel.Rotate(0,0,-speed*160*dt);
        if(speed>.02f && dragging)
        {
            wound[band]=Mathf.Min(1.12f,wound[band]+dt*.68f*speed);
            hint.text=wound[band]>=1 ? "这里绕满了，移向较薄的位置。":"让导丝环缓缓左右移动。";
            float amount=Mathf.Min(1,wound[band]);
            wraps[band].sizeDelta=new Vector2(21,70+amount*80);
            wraps[band].anchoredPosition=new Vector2(1240+band*20,-570+band*4.5f+amount*40);
            wraps[band].GetComponent<Image>().color=new Color(1,1,1,amount*.9f);
            if(Progress>=.995f)
            {
                done=true;hint.text="丝线绕匀了。";finished?.Invoke();
            }
        }
    }
    private void SetGuide(float position)
    {
        guideX=Mathf.Clamp01(position);guide.anchoredPosition=new Vector2(guideX*222,0);
        var from=new Vector2(645,-520);var to=new Vector2(1240+guideX*222,-832);var delta=to-from;
        thread.anchoredPosition=from;thread.sizeDelta=new Vector2(delta.magnitude,3);
        thread.localEulerAngles=new Vector3(0,0,Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
        var bobbin=new Vector2(to.x,-605+guideX*50);var laying=bobbin-to;
        layThread.anchoredPosition=to;layThread.sizeDelta=new Vector2(laying.magnitude,3);layThread.localEulerAngles=new Vector3(0,0,Mathf.Atan2(laying.y,laying.x)*Mathf.Rad2Deg);
    }
    private void DragTo(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area,e.position,e.pressEventCamera,out var p);
        SetGuide((p.x-21)/222);
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

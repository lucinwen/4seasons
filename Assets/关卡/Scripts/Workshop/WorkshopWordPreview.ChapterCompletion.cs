#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    private static IEnumerator ReviewChapterCompletionLayout()
    {
        Feedback2Save("{\"items\":4095,\"chapters\":15,\"solarTerms\":15,\"dyedColor\":\"紫色\",\"garmentId\":\"1_04\",\"steps\":[\"quest.guide.Spring\",\"quest.guide.Summer\",\"quest.guide.Autumn\",\"quest.guide.Winter\",\"spring.guyu.fed\",\"summer.xiaoman.reeled\",\"autumn.loom\",\"autumn.bailu.woven\",\"winter.dyes\",\"winter.ground\",\"autumn.hanlu.patterned\",\"winter.xiaoxue.fitted\"],\"observations\":[\"observation.spring.qingming\",\"observation.summer.mangzhong.thread\",\"observation.summer.dashu.vent\",\"observation.autumn.qiufen\",\"observation.autumn.hanlu\",\"observation.winter.lidong\",\"observation.winter.xiaoxue\"]}");
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.5f);
        Feedback2Require(!WorkshopRewardCards.IsBusy,"Chapter review legacy progress must load without reward popups.");
        var art=Resources.Load<WorkshopCraftRecordArt>("WorkshopCraftRecordArt");
        Feedback2Require(art!=null,"Chapter completion artwork configuration is missing.");
        string priorSave=PlayerPrefs.GetString("ShiXu.Workshop.v1","");

        for(int index=0;index<4;index++)
        {
            var season=(WorkshopSeason)index;
            bool storageClosed=false;int chapterClosed=0;
            WorkshopStorageView.Show(season,()=>storageClosed=true,(_,next)=>
                Feedback2Require(false,"Continuing a chapter review unexpectedly travelled to "+next));
            yield return null;
            var storage=Object.FindObjectOfType<WorkshopStorageView>();
            Feedback2Require(storage!=null&&WorkshopStorageView.IsOpen,"Chapter review needs the original storage underneath.");
            var reopen=ChapterReviewButton(storage.transform,"Open Chapter Result");
            EventSystem.current.SetSelectedGameObject(reopen.gameObject);
            WorkshopCraftRecordView.ShowChapterRecord(season,()=>chapterClosed++);
            yield return null;
            var canvas=GameObject.Find("Workshop Chapter Record");
            Feedback2Require(canvas!=null&&WorkshopCraftRecordView.IsOpen,"Chapter completion did not open for "+season);
            var frame=canvas.transform.Find("Layout") as RectTransform;
            var cover=ChapterReviewTransform(canvas.transform,"Original Chapter Completion")?.GetComponent<Image>();
            Feedback2Require(frame!=null&&cover!=null&&cover.sprite==art.ChapterCover(season),
                "Chapter must use its original completion artwork: "+season);
            int coverCount=0;
            foreach(var image in canvas.GetComponentsInChildren<Image>())
                if(image.name=="Original Chapter Completion")coverCount++;
            Feedback2Require(coverCount==1,"Chapter completion should show one original artwork, not multiple result panels.");
            var coverBounds=ChapterReviewBounds(cover.rectTransform,frame);
            Feedback2Require(Mathf.Abs(coverBounds.center.x-960)<2&&Mathf.Abs(coverBounds.x-330)<2&&
                Mathf.Abs(coverBounds.y-120)<2&&Mathf.Abs(coverBounds.width-1260)<2&&Mathf.Abs(coverBounds.height-709)<2,
                "The original chapter result should be centered and enlarged: "+season+" "+coverBounds);
            foreach(var child in canvas.GetComponentsInChildren<Transform>(true))
                Feedback2Require(child.name!="Chapter Record"&&child.name!="Reward Stored"&&
                    child.name!="Related Solar Card"&&child.name!="Updated Status Surface"&&child.name!="Heading",
                    "A removed side panel/status/solar control remains in the chapter result: "+child.name);
            foreach(var text in canvas.GetComponentsInChildren<Text>())
                Feedback2Require(!text.text.Contains("本章工序回看")&&!text.text.Contains("本章成果已经入库")&&
                    !text.text.Contains("成衣已经缝好"),"Removed result prose is still drawn over the native chapter artwork.");

            var primary=ChapterReviewButton(canvas.transform,"Continue from Result");
            var primaryBounds=ChapterReviewBounds((RectTransform)primary.transform,frame);
            Feedback2Require(coverBounds.Contains(primaryBounds.center),"The main continue action must stay on the native result artwork.");
            var process=ChapterReviewButton(canvas.transform,"Toggle Process");
            var sources=ChapterReviewButton(canvas.transform,"Regional Sources");
            var secondary=new List<Button>{process};
            foreach(var observation in WorkshopObservationView.ObservationsFor(season))
                if(WorkshopObservationView.HasRecord(observation))
                    secondary.Add(ChapterReviewButton(canvas.transform,"Observation "+observation));
            secondary.Add(sources);
            Feedback2Require(canvas.GetComponentsInChildren<Button>().Length==secondary.Count+1,
                "Chapter result has unexpected controls besides continue and the secondary row.");
            for(int i=0;i<secondary.Count;i++)
            {
                var button=secondary[i];
                var bounds=ChapterReviewBounds((RectTransform)button.transform,frame);
                Feedback2Require(bounds.yMin>=coverBounds.yMax+12&&bounds.yMin>=849&&bounds.yMax<=908&&
                    bounds.xMin>=coverBounds.xMin-2&&bounds.xMax<=coverBounds.xMax+2,
                    "Secondary actions must occupy the quiet row below the artwork: "+button.transform.parent.name+" "+bounds);
                var surface=button.transform.parent.Find("Surface")?.GetComponent<Image>();
                Feedback2Require(surface!=null&&Vector3.Distance(new Vector3(surface.color.r,surface.color.g,surface.color.b),
                    new Vector3(WorkshopUi.Paper.r,WorkshopUi.Paper.g,WorkshopUi.Paper.b))<.05f,
                    "A chapter auxiliary action is still styled as a primary action: "+button.transform.parent.name);
                if(i>0)
                {
                    var previous=ChapterReviewBounds((RectTransform)secondary[i-1].transform,frame);
                    Feedback2Require(bounds.xMin>=previous.xMax+3,"Secondary chapter actions overlap.");
                }
                ChapterReviewHit(button);
            }
            yield return Capture((820+index)+"-chapter-completion-"+season.ToString().ToLowerInvariant());

            if(index==0)
            {
                yield return ReviewChapterAuxiliaryReturns(canvas,process,sources,storage,()=>chapterClosed);
                Feedback2Require(WorkshopStorageView.IsOpen&&!storageClosed&&chapterClosed==0,
                    "Returning from a chapter child must preserve both the result and the original storage.");
            }

            ChapterReviewClick(primary);
            yield return null;
            Feedback2Require(chapterClosed==1&&!WorkshopCraftRecordView.IsOpen&&WorkshopStorageView.IsOpen&&
                !storageClosed&&Object.FindObjectOfType<WorkshopStorageView>()==storage&&
                EventSystem.current.currentSelectedGameObject==reopen.gameObject,
                "Continue must dismiss only the chapter result and restore focus to the original storage: "+season);
            ChapterReviewHit(reopen);
            ChapterReviewClick(ChapterReviewButton(storage.transform,"Storage Close"));
            yield return null;
            Feedback2Require(storageClosed&&!WorkshopStorageView.IsOpen,"Original storage did not remain operable after the chapter result.");
        }
        Feedback2Require(priorSave==PlayerPrefs.GetString("ShiXu.Workshop.v1","")&&!WorkshopRewardCards.IsBusy,
            "Read-only chapter/process/observation/source review changed the player's saved progression.");
        Debug.Log("[ChapterCompletionLayout] PASS: four native centered results; no right card, status prose or solar button; secondary row placement/style/hit targets; process, recorded observation and sources return; continue restores the same storage and focus; saved progress unchanged.");
    }

    private static IEnumerator ReviewChapterAuxiliaryReturns(GameObject chapter,Button process,Button sources,
        WorkshopStorageView storage,System.Func<int> closeCount)
    {
        ChapterReviewClick(process);yield return null;
        var processReturn=ChapterReviewFindButton("Close Chapter Process");
        Feedback2Require(processReturn!=null&&WorkshopCraftRecordView.IsOpen&&closeCount()==0&&WorkshopStorageView.IsOpen,
            "Process should open its own dismissible child above the result.");
        yield return Capture("824-chapter-process-child");
        ChapterReviewClick(processReturn);yield return null;
        Feedback2Require(ChapterReviewFindButton("Close Chapter Process")==null&&WorkshopCraftRecordView.IsOpen&&closeCount()==0,
            "Closing process should return to the native chapter result.");
        ChapterReviewHit(process);

        var observation=ChapterReviewButton(chapter.transform,"Observation QingmingMulberry");
        ChapterReviewClick(observation);yield return null;
        Feedback2Require(WorkshopObservationView.IsOpen&&WorkshopCraftRecordView.IsOpen&&Time.timeScale==0&&closeCount()==0,
            "Recorded spring observation must open as a child without closing the result.");
        yield return Capture("825-chapter-observation-child");
        ChapterReviewClick(ChapterReviewFindButton("Return From Observation"));yield return null;
        Feedback2Require(!WorkshopObservationView.IsOpen&&WorkshopCraftRecordView.IsOpen&&Time.timeScale==1f&&closeCount()==0,
            "Recorded observation return must restore the chapter result and world clock.");
        ChapterReviewHit(sources);

        ChapterReviewClick(sources);yield return null;
        Feedback2Require(WorkshopHeritageBrief.IsOpen&&WorkshopCraftRecordView.IsOpen&&closeCount()==0,
            "Regional sources must remain a child of the original chapter result.");
        yield return Capture("826-chapter-sources-child");
        ChapterReviewClick(ChapterReviewFindButton("Heritage Return"));yield return null;
        Feedback2Require(!WorkshopHeritageBrief.IsOpen&&WorkshopCraftRecordView.IsOpen&&closeCount()==0&&
            Object.FindObjectOfType<WorkshopStorageView>()==storage,"Sources return replaced or closed the chapter/storage result.");
        ChapterReviewHit(process);
    }

    private static Transform ChapterReviewTransform(Transform root,string name)
    {
        foreach(var child in root.GetComponentsInChildren<Transform>())if(child.name==name)return child;
        return null;
    }

    private static Button ChapterReviewButton(Transform root,string name)
    {
        var target=ChapterReviewTransform(root,name);
        var button=target!=null?target.GetComponent<Button>()??target.GetComponentInChildren<Button>():null;
        Feedback2Require(button!=null,"Missing chapter review control: "+name);
        return button;
    }

    private static Button ChapterReviewFindButton(string name)
    {
        foreach(var button in Object.FindObjectsOfType<Button>())
            if(button.name==name||(button.name=="Action"&&button.transform.parent.name==name))return button;
        return null;
    }

    private static Rect ChapterReviewBounds(RectTransform target,RectTransform frame)
    {
        var corners=new Vector3[4];target.GetWorldCorners(corners);
        Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        foreach(var corner in corners)
        {
            var local=frame.InverseTransformPoint(corner);
            var point=new Vector2(local.x+960,540-local.y);min=Vector2.Min(min,point);max=Vector2.Max(max,point);
        }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }

    private static PointerEventData ChapterReviewHit(Button button)
    {
        Feedback2Require(button!=null&&button.IsInteractable(),"Chapter review action is not usable.");
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
            position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Feedback2Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,
            "Chapter review action is obscured: "+button.name+" / "+button.transform.parent.name);
        return pointer;
    }

    private static void ChapterReviewClick(Button button)=>
        ExecuteEvents.Execute(button.gameObject,ChapterReviewHit(button),ExecuteEvents.pointerClickHandler);
}
#endif

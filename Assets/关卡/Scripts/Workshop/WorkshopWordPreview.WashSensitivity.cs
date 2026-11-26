#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    // Called from the isolated Feedback2 review; dispatch through the actual cloth
    // handlers, with one large mouse move per frame rather than dense solver calls.
    private static IEnumerator ReviewWashSensitivity()
    {
        Feedback2Save("{\"items\":1,\"steps\":[\"quest.guide.Spring\",\"spring.eggPaper\"]}");
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.4f);
        var sprite=UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/关卡/1-春/道具/1-蚕种清洗后.png");
        int returned=0;bool success=false;
        System.Action<bool> closed=ok=>
        {
            returned++;success=ok;
            if(ok)WorkshopProgress.Award(WorkshopItem.SilkwormEgg);
        };
        Feedback2Require(SpringSilkwormWashOverlay.Show(null,null,sprite,closed),"Sensitivity worktable did not open.");
        yield return null;
        var wash=Object.FindObjectOfType<SpringSilkwormWashOverlay>();
        var frame=(RectTransform)wash.transform;
        var input=wash.GetComponentInChildren<SpringWashClothInput>();
        var pointer=WashSensitivityPointer(input,frame);
        WashSensitivityBegin(input,frame,pointer,new Vector2(570,430));
        WashSensitivityMove(input,frame,pointer,new Vector2(1350,430));yield return null;
        WashSensitivityMove(input,frame,pointer,new Vector2(570,790));yield return null;
        ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.endDragHandler);
        Feedback2Require(wash.CleanFraction==0&&!wash.IsComplete&&!WorkshopProgress.Has(WorkshopItem.SilkwormEgg),
            "A fast dry stroke cleaned the heap or awarded eggs.");

        pointer=WashSensitivityPointer(input,frame);
        WashSensitivityBegin(input,frame,pointer,new Vector2(240,760));yield return null;
        var untouched=frame.Find("Heap Surface 17").GetComponent<Image>();
        var untouchedTint=untouched.color;
        WashSensitivityMove(input,frame,pointer,new Vector2(740,435));yield return null;
        WashSensitivityMove(input,frame,pointer,new Vector2(855,435));yield return null;
        WashSensitivityMove(input,frame,pointer,new Vector2(740,435));yield return null;
        ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.endDragHandler);
        Feedback2Require(wash.CleanFraction>.02f&&wash.CleanFraction<.7f&&!wash.IsComplete&&untouched.color==untouchedTint,
            "A local wet wipe should visibly clean only the part it touches.");
        yield return Capture("810-wash-sensitivity-local");
        InvokeNamedButton("Leave Bath");yield return null;
        Feedback2Require(returned==1&&!success&&!WorkshopProgress.Has(WorkshopItem.SilkwormEgg)&&Time.timeScale==1f,
            "Leaving partly cleaned eggs must not grant completion.");

        returned=0;
        Feedback2Require(SpringSilkwormWashOverlay.Show(null,null,sprite,closed),"Sensitivity retry did not open.");
        yield return null;
        wash=Object.FindObjectOfType<SpringSilkwormWashOverlay>();frame=(RectTransform)wash.transform;
        input=wash.GetComponentInChildren<SpringWashClothInput>();
        pointer=WashSensitivityPointer(input,frame);
        WashSensitivityBegin(input,frame,pointer,new Vector2(240,760));yield return null;
        // Sparse, fast sweeps cover the visible heap. Empty transparent corners
        // must never require the player to search for an invisible dirty patch.
        float[] heights={420,550,680,795};
        for(int pass=0;pass<2&&!wash.IsComplete;pass++)
        {
            WashSensitivityMove(input,frame,pointer,new Vector2(240,760));yield return null;
            for(int row=0;row<heights.Length&&!wash.IsComplete;row++)
            {
                WashSensitivityMove(input,frame,pointer,new Vector2(row%2==0?570:1350,heights[row]));yield return null;
                WashSensitivityMove(input,frame,pointer,new Vector2(row%2==0?1350:570,heights[row]));yield return null;
            }
        }
        var collect=frame.Find("Collect Clean Eggs").GetComponent<Button>();
        Feedback2Require(wash.IsComplete&&wash.CleanFraction>.999f&&collect.gameObject.activeInHierarchy&&collect.interactable,
            "Fast full-heap wipes did not produce a usable collect button.");
        ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.endDragHandler);
        yield return new WaitForSecondsRealtime(.3f);
        Feedback2Require(wash.IsComplete&&collect.gameObject.activeInHierarchy&&returned==0&&!WorkshopProgress.Has(WorkshopItem.SilkwormEgg),
            "Completion or the collect button disappeared after release, or eggs were awarded before collection.");
        Canvas.ForceUpdateCanvases();
        var click=TablePointer(frame,new Vector2(958,919));
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(click,hits);
        Feedback2Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==collect,
            "The completed cloth blocks the collect button.");
        Feedback2Require(collect.GetComponentInChildren<Text>().text=="收下蚕种","The collect action needs a clear final label.");
        yield return Capture("811-wash-sensitivity-collect-ready");
        ExecuteEvents.Execute(collect.gameObject,click,ExecuteEvents.pointerClickHandler);yield return null;
        Feedback2Require(returned==1&&success&&WorkshopProgress.Has(WorkshopItem.SilkwormEgg)&&!SpringSilkwormWashOverlay.IsOpen&&Time.timeScale==1f,
            "Collecting washed eggs did not award exactly once and restore the world.");
        Debug.Log("[WashSensitivity] PASS: actual cloth handlers, sparse fast drags, dry protection, local cleaning, cancellation, stable unobstructed collect control and explicit collection reward.");
    }

    private static PointerEventData WashSensitivityPointer(SpringWashClothInput input,RectTransform frame)
    {
        Canvas.ForceUpdateCanvases();
        var pointer=TablePointer(frame,new Vector2(1590,800));
        pointer.pressPosition=pointer.position;
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        foreach(var hit in hits)
            if(hit.gameObject==input.gameObject){pointer.pointerPressRaycast=hit;break;}
        Feedback2Require(pointer.pointerPressRaycast.gameObject==input.gameObject,"The wiping cloth cannot receive a real pointer press.");
        ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.initializePotentialDrag);
        Feedback2Require(!pointer.useDragThreshold,"The wiping cloth still discards the initial drag threshold.");
        return pointer;
    }

    private static void WashSensitivityBegin(SpringWashClothInput input,RectTransform frame,PointerEventData pointer,Vector2 first)
    {
        // The input module begins dragging at the first moved position. The
        // pressPosition must preserve the full first stroke back to pickup.
        pointer.position=TablePointer(frame,first).position;
        ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.dragHandler);
    }

    private static void WashSensitivityMove(SpringWashClothInput input,RectTransform frame,PointerEventData pointer,Vector2 point)
    {
        pointer.position=TablePointer(frame,point).position;
        ExecuteEvents.Execute(input.gameObject,pointer,ExecuteEvents.dragHandler);
    }
}
#endif

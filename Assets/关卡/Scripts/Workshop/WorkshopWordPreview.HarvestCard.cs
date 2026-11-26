#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Qingming.Harvest;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed partial class WorkshopWordPreview
{
    private const string HarvestMulberryKnowledge="桑叶是家蚕的主要食物，不同生长阶段的蚕宝宝对桑叶的鲜嫩程度和大小有不同需求。";

    private static IEnumerator ReviewHarvestKnowledgeCard()
    {
        Feedback2Save("{\"items\":0,\"steps\":[\"quest.guide.Spring\"]}");
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.4f);
        yield return HarvestKnowledgeRound(false);

        // A real legacy save has the leaf and its observation but no reward receipt.
        // Loading it must backfill ownership without replaying the Qingming reward.
        Feedback2Save("{\"items\":1,\"observations\":[\"spring.qingming\"],\"steps\":[\"quest.guide.Spring\",\"spring.qingming.harvested\"]}");
        yield return SceneManager.LoadSceneAsync("Level1_Spring");
        yield return new WaitForSecondsRealtime(.5f);
        Feedback2Require(WorkshopRewardCards.HasTerm(WorkshopSeason.Spring,4)&&!WorkshopRewardCards.IsBusy,
            "Legacy harvesting should own Qingming silently before a replay.");
        yield return HarvestKnowledgeRound(true);
        Debug.Log("[HarvestKnowledgeCard] PASS: public leaf-collection callbacks; no card before eight leaves; original Mulberry title/copy; dark scene; one accept action; collection exits harvesting; no duplicate craft reward or legacy Qingming reward.");
    }

    private static IEnumerator HarvestKnowledgeRound(bool legacy)
    {
        var originalPlayer=Feedback2Player();
        Feedback2Require(HarvestOverlayManager.Instance!=null,"Spring has no harvesting overlay manager.");
        HarvestOverlayManager.Instance.ShowHarvest();
        yield return null;
        var manager=MulberryHarvestGameManager.Instance;
        Feedback2Require(manager!=null&&manager.State==HarvestGameState.Playing&&manager.targetLeaves==8,
            "Harvesting must immediately start with the eight-leaf target.");
        // Exercise the same public callback as a caught suitable leaf; remove random
        // falling leaves so an unrelated physics catch cannot advance this fixture.
        manager.spawner.StopSpawning();manager.spawner.ClearAllLeaves();
        int completions=0;manager.OnHarvestCompleted+=()=>completions++;
        for(int leaf=0;leaf<7;leaf++)manager.NotifyLeafCollected();
        yield return new WaitForSecondsRealtime(.4f);
        Feedback2Require(manager.CollectedLeaves==7&&manager.State==HarvestGameState.Playing&&completions==0&&
            HarvestKnowledgeCanvas()==null&&!WorkshopRewardCardView.IsOpen&&!WorkshopKnowledgeView.IsOpen&&
            (manager.ui.completePanel==null||!manager.ui.completePanel.activeInHierarchy),
            "Seven leaves must keep the game playable without a knowledge card or old summary.");
        if(!legacy)
        {
            Feedback2Require(!WorkshopProgress.Has(WorkshopItem.MulberryLeaf),"Incomplete harvesting awarded its leaf.");
            yield return Capture("812-harvest-seven-leaves");
        }

        manager.NotifyLeafCollected();
        manager.NotifyLeafCollected(); // A completed game must ignore an extra catch.
        Feedback2Require(manager.CollectedLeaves==8&&manager.State==HarvestGameState.Completed&&completions==1,
            "The eighth leaf should complete exactly once and clamp further catches.");
        float deadline=Time.realtimeSinceStartup+20f;
        while((MiniGameCompletionAnimation.IsPlaying||HarvestKnowledgeCanvas()==null)&&Time.realtimeSinceStartup<deadline)
            yield return null;
        var card=HarvestKnowledgeCanvas();
        Feedback2Require(card!=null&&!MiniGameCompletionAnimation.IsPlaying,
            "Harvest completion did not replace the summary with the Mulberry knowledge card.");
        Feedback2Require(HarvestOverlayManager.IsHarvestActive&&manager.ui!=null&&
            (manager.ui.completePanel==null||!manager.ui.completePanel.activeInHierarchy),
            "The old harvest summary is visible or harvesting exited before the card was accepted.");
        var title=FindHarvestCardText(card,"Item Card Title");
        var body=FindHarvestCardText(card,"Item Card Description");
        Feedback2Require(title!=null&&title.text=="桑叶"&&body!=null&&
            body.text.Replace("\n","").Replace("\r","")==HarvestMulberryKnowledge,
            "Harvest must reuse the original Mulberry title and knowledge, not a craft-summary replacement.");
        bool expectedArt=false,blackVeil=false;
        foreach(var image in card.GetComponentsInChildren<Image>())
        {
            if(image.name=="Item Card Illustration"&&image.sprite==WorkshopArt.ItemCard(WorkshopItem.MulberryLeaf))expectedArt=true;
            if(image.name=="Backdrop"&&image.color.a>=.5f&&image.color.r<=.08f&&image.color.g<=.08f&&image.color.b<=.08f)
                blackVeil=true;
        }
        Feedback2Require(expectedArt&&blackVeil,"The native Mulberry artwork or dark scene backdrop is missing.");
        var controls=card.GetComponentsInChildren<Button>();
        Feedback2Require(controls.Length==1&&controls[0].interactable&&HarvestCardAcceptLabel(controls[0]),
            "Harvest knowledge must have exactly one enabled accept-card action.");
        foreach(var text in card.GetComponentsInChildren<Text>())
            Feedback2Require(!text.text.Contains("采桑完成")&&!text.text.Contains("接到嫩叶"),
                "The obsolete harvest summary still appears on the knowledge card.");
        yield return Capture(legacy?"815-harvest-legacy-knowledge-card":"813-harvest-mulberry-knowledge-card");
        ClickHarvestKnowledgeButton(controls[0]);
        yield return null;
        Feedback2Require(!HarvestOverlayManager.IsHarvestActive&&MulberryHarvestGameManager.Instance==null&&
            originalPlayer!=null&&originalPlayer.gameObject.activeInHierarchy&&WorkshopProgress.Has(WorkshopItem.MulberryLeaf),
            "Accepting the Mulberry card must close harvesting and restore the spring player with the earned leaf.");
        yield return HarvestKnowledgeAfterAccept(!legacy);
    }

    private static IEnumerator HarvestKnowledgeAfterAccept(bool allowQingming)
    {
        float deadline=Time.realtimeSinceStartup+2f;
        int terms=0;
        while(Time.realtimeSinceStartup<deadline)
        {
            Feedback2Require(HarvestKnowledgeCanvas()==null&&!WorkshopRewardCardView.IsOpen,
                "A second harvest knowledge/craft reward popped up after its card was accepted.");
            var close=GameObject.Find("Close Solar Detail")?.GetComponent<Button>();
            if(close!=null)
            {
                Feedback2Require(allowQingming&&terms==0,"A legacy replay or repeated reward showed Qingming again.");
                var image=GameObject.Find("Full Size Solar Card")?.GetComponent<Image>();
                Feedback2Require(image!=null&&image.sprite==WorkshopArt.SolarCard(WorkshopSeason.Spring,4),
                    "The only permitted follow-up is the newly earned Qingming card.");
                var solar=close.GetComponentInParent<Canvas>();
                var buttons=solar.GetComponentsInChildren<Button>();
                Feedback2Require(buttons.Length==1&&HarvestCardAcceptLabel(close),"Qingming must also have a single accept action.");
                yield return Capture("814-harvest-first-qingming-card");
                ClickHarvestKnowledgeButton(close);terms++;
                deadline=Time.realtimeSinceStartup+1f;
            }
            yield return null;
        }
        Feedback2Require(WorkshopRewardCards.HasTerm(WorkshopSeason.Spring,4)&&!WorkshopRewardCards.IsBusy&&
            !WorkshopKnowledgeView.IsOpen&&!HarvestOverlayManager.IsHarvestActive,
            "Harvest rewards did not finish cleanly, or Qingming ownership was lost.");
    }

    private static Canvas HarvestKnowledgeCanvas()
    {
        foreach(var text in Object.FindObjectsOfType<Text>())
            if(text.name=="Item Card Title"&&text.text=="桑叶")return text.GetComponentInParent<Canvas>();
        return null;
    }

    private static Text FindHarvestCardText(Canvas canvas,string name)
    {
        foreach(var text in canvas.GetComponentsInChildren<Text>())if(text.name==name)return text;
        return null;
    }

    private static bool HarvestCardAcceptLabel(Button button)
    {
        foreach(var text in button.GetComponentsInChildren<Text>())if(text.text=="收下卡片")return true;
        // Solar artwork prints its label as a sibling of the transparent button.
        foreach(var text in button.GetComponentInParent<Canvas>().GetComponentsInChildren<Text>())
            if(text.text=="收下卡片")return true;
        return false;
    }

    private static void ClickHarvestKnowledgeButton(Button button)
    {
        Canvas.ForceUpdateCanvases();
        var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
            position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Feedback2Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button,
            "The harvest/term accept action is obscured by another interface.");
        ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
}
#endif

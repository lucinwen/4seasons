using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SeasonalPostOffice : MonoBehaviour
{
    private const string SaveKey = "ShiXu.PostOffice.v1";
    private const int MessageLimit = 120;
    private const float ArtworkWidth = 846f;
    private const float ArtworkHeight = 559f;

    private static readonly string[] Seasons = { "Spring", "Summer", "Autumn", "Winter" };
    private static readonly string[] SeasonNames = { "春", "夏", "秋", "冬" };
    private static readonly Color[] SeasonColors =
    {
        new Color32(36, 105, 92, 255), new Color32(53, 105, 108, 255),
        new Color32(137, 65, 42, 255), new Color32(56, 80, 136, 255)
    };
    private static readonly Color[] WritingPaper =
    {
        new Color32(238, 248, 235, 239), new Color32(237, 249, 241, 239),
        new Color32(255, 243, 218, 239), new Color32(238, 246, 253, 239)
    };
    private static readonly Rect[] FrontSeasonLetterBounds =
    {
        new Rect(468, 33, 87, 78), new Rect(477, 34, 79, 84),
        new Rect(469, 42, 96, 79), new Rect(467, 32, 103, 94)
    };
    private static readonly Color32[] FrontSeasonPaper =
    {
        new Color32(252, 247, 221, 255), new Color32(255, 252, 236, 255),
        new Color32(251, 241, 214, 255), new Color32(255, 247, 232, 255)
    };
    private static readonly Color32[] FrontSeasonInk =
    {
        new Color32(42, 88, 53, 255), new Color32(62, 91, 59, 255),
        new Color32(126, 46, 30, 255), new Color32(47, 64, 123, 255)
    };

    [Serializable]
    private sealed class Postcard
    {
        public int season;
        public string message;
        public long sentUtcTicks;
    }

    [Serializable]
    private sealed class PostOfficeSave
    {
        public int season;
        public string draft = "";
        public List<Postcard> sent = new List<Postcard>();
    }

    private enum Page { Write, Envelope, Receipt, Sent }

    private readonly Texture2D[] fronts = new Texture2D[Seasons.Length];
    private readonly Texture2D[] backs = new Texture2D[Seasons.Length];
    private readonly RectTransform[] seasonMarks = new RectTransform[Seasons.Length];
    private readonly UnityEngine.UI.Button[] seasonButtons = new UnityEngine.UI.Button[Seasons.Length];
    private readonly UnityEngine.UI.Text[] seasonLabels = new UnityEngine.UI.Text[Seasons.Length];
    private readonly GameObject[] frontLettering = new GameObject[Seasons.Length];
    private PostOfficeSave save;
    private Action closed;
    private Page page;
    private bool showBack = true;
    private bool animating;
    private bool draftChanged;
    private int sentIndex;
    private RectTransform card;
    private RectTransform composer;
    private RectTransform envelopeOptions;
    private RectTransform receipt;
    private RectTransform sentOptions;
    private CanvasGroup controls;
    private CanvasGroup cardOpacity;
    private UnityEngine.UI.RawImage artwork;
    private UnityEngine.UI.RawImage envelope;
    private UnityEngine.UI.Image writingPaper;
    private UnityEngine.UI.InputField message;
    private UnityEngine.UI.Text messageHint;
    private UnityEngine.UI.Text count;
    private UnityEngine.UI.Text turnLabel;
    private UnityEngine.UI.Text archiveLabel;
    private UnityEngine.UI.Text sentDetails;
    private UnityEngine.UI.Text sentReadingHint;
    private UnityEngine.UI.Text sentPosition;
    private UnityEngine.UI.Text sendStatus;
    private UnityEngine.UI.Button turn;
    private UnityEngine.UI.Button seal;
    private UnityEngine.UI.Button send;
    private UnityEngine.UI.Button unseal;
    private UnityEngine.UI.Button archive;
    private UnityEngine.UI.Button close;
    private UnityEngine.UI.Button previous;
    private UnityEngine.UI.Button next;
    private UnityEngine.UI.Button newCard;
    private UnityEngine.UI.Button returnToDraft;
    private UnityEngine.UI.Selectable[] navigation;

    public static void Open(Transform parent, Action closed)
    {
        var content = WorkshopUi.Rect(parent, "Seasonal Post Office", new Rect(204, 112, 1512, 856));
        var office = content.gameObject.AddComponent<SeasonalPostOffice>();
        office.closed = closed;
        office.Build(content);
    }

    private void Build(RectTransform content)
    {
        controls = content.gameObject.AddComponent<CanvasGroup>();
        save = LoadSave();

        var panel = WorkshopUi.PixelCard(content, "Post Office Frame", new Rect(0, 0, 1512, 856),
            WorkshopUi.Paper, WorkshopUi.Bronze);
        WorkshopUi.Label(panel, "Post Office Heading", new Rect(48, 20, 870, 68), "寄明信片", 47);
        archive = WorkshopUi.Button(panel, "Open Sent Postcards", new Rect(1112, 29, 204, 52), "已寄明信片",
            ToggleArchive, WorkshopUi.Cream, size: 25);
        archiveLabel = archive.GetComponentInChildren<UnityEngine.UI.Text>();
        close = WorkshopUi.Button(panel, "Close Post Office", new Rect(1330, 29, 134, 52), "返回",
            Close, WorkshopUi.Green, size: 25);
        WorkshopUi.Fill(panel, "Heading Rule", new Rect(48, 97, 1416, 2), WorkshopUi.Line);

        var stage = WorkshopUi.PixelCard(panel, "Postcard Display", new Rect(48, 125, ArtworkWidth, ArtworkHeight),
            new Color32(231, 219, 193, 255), WorkshopUi.Line);
        card = WorkshopUi.Rect(stage, "Postcard", new Rect(0, 0, ArtworkWidth, ArtworkHeight));
        cardOpacity = card.gameObject.AddComponent<CanvasGroup>();
        artwork = Picture(card, "Postcard Artwork", new Rect(0, 0, ArtworkWidth, ArtworkHeight));
        CreateMessage(artwork.rectTransform);
        envelope = Picture(stage, "Postcard Envelope", new Rect(81, 78, 684, 402));
        turn = WorkshopUi.Button(panel, "Turn Postcard", new Rect(48, 720, 171, 59), "看正面",
            Turn, WorkshopUi.Cream, size: 25);
        turnLabel = turn.GetComponentInChildren<UnityEngine.UI.Text>();
        count = WorkshopUi.Label(panel, "Postcard Character Count", new Rect(636, 720, 258, 59), "", 25,
            WorkshopUi.Faded, TextAnchor.MiddleRight);

        var sidebar = WorkshopUi.PixelCard(panel, "Post Office Actions", new Rect(936, 125, 528, 654),
            WorkshopUi.Cream, WorkshopUi.Line);
        CreateComposer(sidebar);
        CreateEnvelopeOptions(sidebar);
        CreateSentOptions(sidebar);
        CreateReceipt(sidebar);

        envelope.texture = Resources.Load<Texture2D>("PostOffice/Envelope");
        Show(Page.Write);
    }

    private void CreateMessage(RectTransform parent)
    {
        writingPaper = WorkshopUi.Fill(parent, "Postcard Message", new Rect(446, 232, 346, 223), WritingPaper[0]);
        writingPaper.raycastTarget = true;
        var messageBounds = new Rect(12, 9, 322, 205);
        var letters = WorkshopUi.Label(writingPaper.transform, "Message Text", messageBounds, "", 25,
            WorkshopUi.Ink, TextAnchor.UpperLeft);
        letters.supportRichText = false;
        letters.lineSpacing = 1.15f;
        messageHint = WorkshopUi.Label(writingPaper.transform, "Message Hint", messageBounds,
            "在这里写下想寄出的话…", 25, WorkshopUi.Faded, TextAnchor.UpperLeft);
        message = writingPaper.gameObject.AddComponent<UnityEngine.UI.InputField>();
        message.targetGraphic = writingPaper;
        message.textComponent = letters;
        message.placeholder = messageHint;
        message.lineType = UnityEngine.UI.InputField.LineType.MultiLineNewline;
        message.characterLimit = MessageLimit;
        message.customCaretColor = true;
        message.caretColor = WorkshopUi.Ink;
        message.transition = UnityEngine.UI.Selectable.Transition.None;
        message.onValueChanged.AddListener(value =>
        {
            if (page != Page.Write || message.wasCanceled) return;
            save.draft = value;
            draftChanged = true;
            UpdateDraftControls();
        });
        message.onEndEdit.AddListener(_ =>
        {
            // The legacy field restores the old value before notifying listeners on Escape.
            if (page == Page.Write && message.wasCanceled)
                message.SetTextWithoutNotify(save.draft);
        });
    }

    private void CreateComposer(RectTransform sidebar)
    {
        composer = WorkshopUi.Rect(sidebar, "Write Postcard", new Rect(0, 0, 528, 654));
        Heading(composer, "挑一张明信片");
        for (int i = 0; i < Seasons.Length; i++)
        {
            int season = i;
            fronts[i] = Resources.Load<Texture2D>("PostOffice/" + Seasons[i] + "Front");
            if (i == (int)WorkshopSeason.Spring)
                fronts[i] = Resources.Load<Texture2D>("PostOffice/SpringFrontBranded") ?? fronts[i];
            backs[i] = Resources.Load<Texture2D>("PostOffice/" + Seasons[i] + "Back");
            float x = 30 + (i % 2) * 240;
            float y = 96 + (i / 2) * 214;
            var button = WorkshopUi.Button(composer, "Season " + Seasons[i], new Rect(x, y, 228, 198), "",
                () => ChooseSeason(season), WorkshopUi.Paper);
            seasonButtons[i] = button;
            var mark = WorkshopUi.Fill(button.transform, "Selected Season", new Rect(0, 0, 228, 198), SeasonColors[i]);
            WorkshopUi.ApplyPixelShape(mark);
            seasonMarks[i] = mark.rectTransform;
            var preview = Picture(button.transform, "Season Preview", new Rect(9, 9, 210, 138));
            var front = fronts[i];
            preview.texture = front;
            if (front != null)
            {
                CreateFrontLettering(preview.rectTransform, front, i);
                frontLettering[i] = CreateFrontLettering(artwork.rectTransform, front, i);
                frontLettering[i].SetActive(false);
            }
            seasonLabels[i] = WorkshopUi.Label(button.transform, "Season Name", new Rect(0, 151, 228, 42),
                SeasonNames[i], 30, SeasonColors[i], TextAnchor.MiddleCenter);
        }
        seal = WorkshopUi.Button(composer, "Seal Postcard", new Rect(30, 554, 468, 72), "装入信封",
            Seal, WorkshopUi.Green, size: 32);
    }

    private void CreateEnvelopeOptions(RectTransform sidebar)
    {
        envelopeOptions = WorkshopUi.Rect(sidebar, "Sealed Postcard", new Rect(0, 0, 528, 654));
        Heading(envelopeOptions, "明信片已装入信封");
        WorkshopUi.Label(envelopeOptions, "Envelope Instructions", new Rect(30, 108, 468, 70),
            "现在可以把它寄出。", 30, WorkshopUi.Faded);
        sendStatus = WorkshopUi.Label(envelopeOptions, "Send Status", new Rect(30, 344, 468, 78), "",
            24, WorkshopUi.Clay);
        unseal = WorkshopUi.Button(envelopeOptions, "Reopen Envelope", new Rect(30, 460, 468, 66), "拆开继续写",
            () => Show(Page.Write), WorkshopUi.Paper, size: 28);
        send = WorkshopUi.Button(envelopeOptions, "Send Postcard", new Rect(30, 554, 468, 72), "确认寄出",
            Send, WorkshopUi.Green, size: 32);
    }

    private void CreateReceipt(RectTransform sidebar)
    {
        receipt = WorkshopUi.Rect(sidebar, "Sent Receipt", new Rect(0, 0, 528, 654));
        WorkshopUi.Label(receipt, "Postcard Receipt", new Rect(30, 28, 468, 58), "明信片已寄出", 37);
        WorkshopUi.Label(receipt, "Receipt Instructions", new Rect(30, 108, 468, 96),
            "可以在「已寄明信片」里翻看。", 28, WorkshopUi.Faded);
        newCard = WorkshopUi.Button(receipt, "Write Another Postcard", new Rect(30, 554, 468, 72), "再写一张",
            () => Show(Page.Write), WorkshopUi.Green, size: 32);
    }

    private void CreateSentOptions(RectTransform sidebar)
    {
        sentOptions = WorkshopUi.Rect(sidebar, "Sent Postcards", new Rect(0, 0, 528, 654));
        Heading(sentOptions, "已寄明信片");
        sentDetails = WorkshopUi.Label(sentOptions, "Sent Postcard Details", new Rect(30, 108, 468, 113), "",
            30, WorkshopUi.Faded, TextAnchor.UpperLeft);
        sentReadingHint = WorkshopUi.Label(sentOptions, "Sent Reading Hint", new Rect(30, 239, 468, 84),
            "点击信文后，可用方向键翻看长信。", 23, WorkshopUi.Faded, TextAnchor.UpperLeft);
        previous = WorkshopUi.Button(sentOptions, "Newer Postcard", new Rect(30, 420, 164, 62), "上一张",
            () => BrowseSent(-1), WorkshopUi.Paper, size: 26);
        next = WorkshopUi.Button(sentOptions, "Older Postcard", new Rect(334, 420, 164, 62), "下一张",
            () => BrowseSent(1), WorkshopUi.Paper, size: 26);
        sentPosition = WorkshopUi.Label(sentOptions, "Sent Postcard Position", new Rect(194, 420, 140, 62), "",
            25, WorkshopUi.Faded, TextAnchor.MiddleCenter);
        returnToDraft = WorkshopUi.Button(sentOptions, "Return to Draft", new Rect(30, 554, 468, 72), "返回写信",
            () => Show(Page.Write), WorkshopUi.Green, size: 32);
    }

    private void Show(Page nextPage)
    {
        page = nextPage;
        bool writing = page == Page.Write;
        bool viewingSent = page == Page.Sent;
        bool hasSent = save.sent.Count != 0;
        bool hasArtwork = writing || viewingSent && hasSent;
        showBack = hasArtwork;
        composer.gameObject.SetActive(writing);
        envelopeOptions.gameObject.SetActive(page == Page.Envelope);
        receipt.gameObject.SetActive(page == Page.Receipt);
        sentOptions.gameObject.SetActive(viewingSent);
        card.gameObject.SetActive(hasArtwork || viewingSent);
        envelope.gameObject.SetActive(page == Page.Envelope || page == Page.Receipt);
        turn.gameObject.SetActive(hasArtwork);
        count.gameObject.SetActive(writing);
        card.anchoredPosition = Vector2.zero;
        card.localScale = Vector3.one;
        cardOpacity.alpha = 1f;
        envelope.rectTransform.anchoredPosition = new Vector2(81, -78);
        envelope.color = Color.white;
        sendStatus.text = "";
        archiveLabel.text = viewingSent ? "返回写信" : "已寄明信片";

        if (writing)
        {
            UpdateDraftControls();
            DisplayPostcard(save.season, save.draft, false);
            navigation = new UnityEngine.UI.Selectable[]
            {
                seasonButtons[0], seasonButtons[1], seasonButtons[2], seasonButtons[3],
                message, turn, seal, archive, close
            };
            Select(seasonButtons[save.season]);
        }
        else if (viewingSent)
        {
            RefreshSent();
            navigation = new UnityEngine.UI.Selectable[] { turn, message, previous, next, returnToDraft, archive, close };
            Select(returnToDraft);
        }
        else if (page == Page.Envelope)
        {
            navigation = new UnityEngine.UI.Selectable[] { send, unseal, archive, close };
            Select(send);
        }
        else
        {
            navigation = new UnityEngine.UI.Selectable[] { newCard, archive, close };
            Select(newCard);
        }
    }

    private void ChooseSeason(int season)
    {
        if (save.season == season) return;
        save.season = season;
        draftChanged = true;
        UpdateDraftControls();
        DisplayPostcard(season, save.draft, false);
    }

    private void UpdateDraftControls()
    {
        count.text = $"{save.draft.Length} / {MessageLimit}";
        seal.interactable = !string.IsNullOrWhiteSpace(save.draft);
        for (int i = 0; i < Seasons.Length; i++)
        {
            bool selected = i == save.season;
            seasonMarks[i].gameObject.SetActive(selected);
            seasonLabels[i].color = selected ? WorkshopUi.Cream : SeasonColors[i];
        }
    }

    private void DisplayPostcard(int season, string body, bool readOnly)
    {
        var texture = showBack ? backs[season] : fronts[season];
        artwork.texture = texture;
        artwork.rectTransform.anchoredPosition = Vector2.zero;
        artwork.rectTransform.sizeDelta = new Vector2(ArtworkWidth, ArtworkHeight);
        if (texture != null)
        {
            float height = ArtworkWidth * texture.height / texture.width;
            artwork.rectTransform.sizeDelta = new Vector2(ArtworkWidth, height);
            artwork.rectTransform.anchoredPosition = new Vector2(0, (height - ArtworkHeight) / 2);
        }
        for (int i = 0; i < frontLettering.Length; i++)
        {
            if (frontLettering[i] != null)
                frontLettering[i].SetActive(!showBack && i == season);
        }
        writingPaper.color = WritingPaper[season];
        message.textComponent.color = SeasonColors[season];
        message.readOnly = readOnly;
        message.SetTextWithoutNotify(body ?? "");
        message.gameObject.SetActive(showBack);
        turnLabel.text = showBack ? "看正面" : readOnly ? "看背面" : "写背面";
    }

    private void Turn()
    {
        showBack = !showBack;
        if (page == Page.Write) DisplayPostcard(save.season, save.draft, false);
        else RefreshSent();
    }

    private void Seal()
    {
        if (animating || page != Page.Write || string.IsNullOrWhiteSpace(save.draft)) return;
        showBack = false;
        DisplayPostcard(save.season, save.draft, false);
        envelope.gameObject.SetActive(true);
        StartCoroutine(AnimateEnvelope(false));
    }

    private void Send()
    {
        if (animating || page != Page.Envelope || string.IsNullOrWhiteSpace(save.draft)) return;
        string previousDraft = save.draft;
        save.sent.Insert(0, new Postcard
        {
            season = save.season, message = previousDraft, sentUtcTicks = DateTime.UtcNow.Ticks
        });
        save.draft = "";
        if (!Save())
        {
            save.sent.RemoveAt(0);
            save.draft = previousDraft;
            draftChanged = true;
            sendStatus.text = "暂时无法保存，请再试一次。";
            return;
        }
        StartCoroutine(AnimateEnvelope(true));
    }

    private IEnumerator AnimateEnvelope(bool sending)
    {
        animating = true;
        controls.interactable = false;
        controls.blocksRaycasts = false;
        float duration = sending ? 0.53f : 0.63f;
        Vector2 envelopeOrigin = envelope.rectTransform.anchoredPosition;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            float progress = Mathf.SmoothStep(0, 1, elapsed / duration);
            if (sending)
            {
                envelope.rectTransform.anchoredPosition = envelopeOrigin + new Vector2(275, 190) * progress;
                envelope.color = new Color(1, 1, 1, 1 - progress);
            }
            else
            {
                card.localScale = Vector3.one * Mathf.Lerp(1, 0.73f, progress);
                card.anchoredPosition = new Vector2(114, -176) * progress;
                cardOpacity.alpha = 1 - progress;
                envelope.color = new Color(1, 1, 1, progress);
            }
            yield return null;
        }
        animating = false;
        controls.interactable = true;
        controls.blocksRaycasts = true;
        Show(sending ? Page.Receipt : Page.Envelope);
    }

    private void ToggleArchive()
    {
        if (page == Page.Sent)
        {
            Show(Page.Write);
            return;
        }
        sentIndex = 0;
        Show(Page.Sent);
    }

    private void BrowseSent(int offset)
    {
        sentIndex = Mathf.Clamp(sentIndex + offset, 0, save.sent.Count - 1);
        RefreshSent();
        if (EventSystem.current != null)
        {
            var selected = EventSystem.current.currentSelectedGameObject;
            if (selected == previous.gameObject && !previous.interactable) Select(next);
            if (selected == next.gameObject && !next.interactable) Select(previous);
        }
    }

    private void RefreshSent()
    {
        bool hasSent = save.sent.Count != 0;
        previous.gameObject.SetActive(hasSent);
        next.gameObject.SetActive(hasSent);
        sentReadingHint.gameObject.SetActive(hasSent);
        sentPosition.text = hasSent ? $"{sentIndex + 1} / {save.sent.Count}" : "";
        if (!hasSent)
        {
            DisplayPostcard(save.season, "", true);
            sentDetails.text = "还没有寄出的明信片。";
            return;
        }
        Postcard item = save.sent[sentIndex];
        DisplayPostcard(item.season, item.message, true);
        var sentDate = new DateTime(item.sentUtcTicks, DateTimeKind.Utc).ToLocalTime();
        sentDetails.text = $"{SeasonNames[item.season]}季明信片\n{sentDate:yyyy年M月d日 HH:mm}";
        previous.interactable = sentIndex > 0;
        next.interactable = sentIndex < save.sent.Count - 1;
    }

    private void Update()
    {
        if (animating || EventSystem.current == null || !string.IsNullOrEmpty(Input.compositionString)) return;
        var selected = EventSystem.current.currentSelectedGameObject;
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (selected == message.gameObject) Select(turn);
            else Close();
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool reverse = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            int index = Array.FindIndex(navigation, option => option.gameObject == selected);
            if (index < 0) index = reverse ? 0 : navigation.Length - 1;
            for (int step = 1; step <= navigation.Length; step++)
            {
                int nextIndex = (index + (reverse ? -step : step) + navigation.Length) % navigation.Length;
                var option = navigation[nextIndex];
                if (!option.gameObject.activeInHierarchy || !option.IsInteractable()) continue;
                Select(option);
                break;
            }
        }
    }

    private void Close()
    {
        if (draftChanged) Save();
        closed?.Invoke();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && draftChanged) Save();
    }

    private void OnApplicationQuit()
    {
        if (draftChanged) Save();
    }

    private static PostOfficeSave LoadSave()
    {
        PostOfficeSave loaded;
        try
        {
            loaded = JsonUtility.FromJson<PostOfficeSave>(PlayerPrefs.GetString(SaveKey, ""));
        }
        catch (ArgumentException)
        {
            loaded = null;
        }

        loaded = loaded ?? new PostOfficeSave();
        loaded.season = Mathf.Clamp(loaded.season, 0, Seasons.Length - 1);
        loaded.draft = loaded.draft ?? "";
        if (loaded.sent == null) loaded.sent = new List<Postcard>();
        loaded.sent.RemoveAll(item => item == null || item.season < 0 || item.season >= Seasons.Length ||
            item.sentUtcTicks < DateTime.MinValue.Ticks || item.sentUtcTicks > DateTime.MaxValue.Ticks);
        return loaded;
    }

    private bool Save()
    {
        string original = PlayerPrefs.GetString(SaveKey, "");
        try
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(save));
            PlayerPrefs.Save();
            draftChanged = false;
            return true;
        }
        catch (PlayerPrefsException exception)
        {
            if (original.Length == 0) PlayerPrefs.DeleteKey(SaveKey);
            else PlayerPrefs.SetString(SaveKey, original);
            Debug.LogException(exception);
            return false;
        }
    }

    private static void Select(UnityEngine.UI.Selectable item)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(item.gameObject);
    }

    private static UnityEngine.UI.RawImage Picture(Transform parent, string name, Rect bounds)
    {
        var image = WorkshopUi.Rect(parent, name, bounds).gameObject.AddComponent<UnityEngine.UI.RawImage>();
        image.raycastTarget = false;
        return image;
    }

    private static GameObject CreateFrontLettering(RectTransform artwork, Texture2D source, int season)
    {
        var layer = WorkshopUi.Rect(artwork, "Front Lettering", new Rect());
        layer.anchorMin = Vector2.zero;
        layer.anchorMax = Vector2.one;
        layer.offsetMin = layer.offsetMax = Vector2.zero;
        float scale = artwork.sizeDelta.x / source.width;
        AddFrontLabel(layer, source, FrontSeasonLetterBounds[season], SeasonNames[season],
            FrontSeasonPaper[season], FrontSeasonInk[season], scale);
        if (season == (int)WorkshopSeason.Spring && source.name != "SpringFrontBranded")
        {
            AddFrontLabel(layer, source, new Rect(397, 611, 230, 36), WorkshopTitle.GameName,
                new Color32(250, 242, 210, 255), FrontSeasonInk[season], scale);
        }
        else if (season == (int)WorkshopSeason.Summer)
        {
            AddFrontLabel(layer, source, new Rect(341, 470, 16, 20), "盐",
                new Color32(241, 198, 130, 255), new Color32(125, 80, 54, 255), scale);
        }
        else if (season == (int)WorkshopSeason.Autumn)
        {
            var jarLabel = new Color32(230, 83, 57, 255);
            var jarInk = new Color32(42, 12, 9, 255);
            AddFrontLabel(layer, source, new Rect(96, 414, 22, 23), "酒", jarLabel, jarInk, scale);
            AddFrontLabel(layer, source, new Rect(820, 569, 20, 21), "酒", jarLabel, jarInk, scale);
            AddFrontLabel(layer, source, new Rect(876, 560, 19, 23), "酒", jarLabel, jarInk, scale);
        }
        return layer.gameObject;
    }

    private static void AddFrontLabel(RectTransform layer, Texture2D source, Rect bounds, string text,
        Color paper, Color ink, float scale)
    {
        var surface = WorkshopUi.Fill(layer, "Lettering Surface", new Rect(), paper);
        surface.raycastTarget = false;
        surface.rectTransform.anchorMin = new Vector2(bounds.xMin / source.width, 1f - bounds.yMax / source.height);
        surface.rectTransform.anchorMax = new Vector2(bounds.xMax / source.width, 1f - bounds.yMin / source.height);
        surface.rectTransform.offsetMin = surface.rectTransform.offsetMax = Vector2.zero;
        int size = Mathf.Max(3, Mathf.RoundToInt(Mathf.Min(bounds.width / text.Length, bounds.height) * scale * 0.94f));
        var label = WorkshopUi.Label(surface.transform, "Postcard Lettering", new Rect(), text, size,
            ink, TextAnchor.MiddleCenter);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = Mathf.Max(3, size * 2 / 3);
        label.resizeTextMaxSize = size;
    }

    private static void Heading(Transform parent, string title) =>
        WorkshopUi.Label(parent, title, new Rect(30, 28, 468, 58), title, 37);
}

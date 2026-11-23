#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class CharacterScreenSetup
{
    private const string SCENE_PATH = "Assets/01_Scenes/00_Tilte_Scene.unity";
    private const string BOTTOM_BAR_PATH = "Assets/03_Prefabs/UI/BottomBar.prefab";
    private const string ICON_PATH = "Assets/04_Arts/00_Image/UI/Icon/Icon_Sheet_2.png";
    private static readonly Color INK = new(0.20f, 0.10f, 0.05f, 1f);
    private static readonly Color PANEL = new(0.92f, 0.81f, 0.61f, 1f);
    private static readonly Color DARK = new(0.12f, 0.14f, 0.17f, 1f);

    // 초기 화면을 씬에 직렬화한다. 사용자가 편집한 화면을 다시 덮어쓰지 않는다.
    [MenuItem("Tools/First Ninja/Create Character Screen")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(SCENE_PATH);
        Transform root = Find(scene, "Character_Window");
        if (root.GetComponent<CharacterWindow>() != null)
        {
            throw new InvalidOperationException("캐릭터 화면이 이미 있습니다. 기존 계층에서 직접 수정해 주세요.");
        }

        GameObject bottomBar = AssetDatabase.LoadAssetAtPath<GameObject>(BOTTOM_BAR_PATH);
        if (bottomBar == null)
        {
            throw new InvalidOperationException($"하단바 프리팹을 찾을 수 없습니다. {BOTTOM_BAR_PATH}");
        }

        Transform lobby = Find(scene, "Lobby_Window");
        Transform popUps = Find(scene, "PopUpManager");
        Object.DestroyImmediate(root.GetComponent<Image>());
        CharacterWindow window = root.gameObject.AddComponent<CharacterWindow>();
        Image("Background", root, 0, 0, 1, 1, Color.white,
            Sprite("Assets/04_Arts/00_Image/Background/Lobby/BG_Character.png"));

        Transform top = Object.Instantiate(lobby.Find("TopBar"), root);
        top.name = "TopBar";
        Text gold = ReplaceAmount(top, "GeumjaAmount");
        Text silver = ReplaceAmount(top, "EunjaAmount");
        Button settings = FindBelow(top, "SettingsButton").GetComponent<Button>();
        settings.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(settings.onClick, window.OpenSettings);

        PrefabUtility.InstantiatePrefab(bottomBar, root);

        CharacterDetailView detail = BuildDetail(root, "EquippedCharacter", .04f, .47f, .96f, .85f);
        CharacterPresentation[] presentations = BuildPresentations();
        Transform inventory = Rect("Inventory", root, .045f, .14f, .955f, .43f);
        CharacterInventorySlot[] slots = new CharacterInventorySlot[presentations.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            float left = (i % 5) * .20f;
            float bottomY = i < 5 ? .51f : .02f;
            Image slot = Image($"CharacterSlot_{i + 1}", inventory, left + .01f, bottomY, left + .19f, bottomY + .46f, DARK);
            slot.raycastTarget = true;
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = slot;
            CharacterInventorySlot item = slot.gameObject.AddComponent<CharacterInventorySlot>();
            Image portrait = Image("Portrait", slot.transform, .06f, .25f, .94f, .95f, Color.white, presentations[i].Portrait);
            portrait.preserveAspect = true;
            Text label = Label("NameLevel", slot.transform, .02f, .01f, .98f, .28f, "", 25, Color.white);
            Text locked = Label("Locked", slot.transform, .16f, .48f, .84f, .70f, "잠금", 26, Color.white);
            Text equipped = Label("Equipped", slot.transform, .02f, .78f, .98f, .99f, "장착 중", 24, new Color(1f, .85f, .3f));
            Set(item, "_portrait", portrait, "_label", label, "_lock", locked.gameObject, "_equipped", equipped.gameObject, "_button", button);
            slots[i] = item;
        }

        Transform modal = Overlay("Character_PopUp", popUps);
        CharacterPopUp popup = modal.gameObject.AddComponent<CharacterPopUp>();
        Image info = Image("CharacterInfoPanel", modal, .06f, .12f, .94f, .88f, PANEL);
        CharacterDetailView infoDetail = BuildDetail(info.transform, "CharacterDetail", .03f, .23f, .97f, .92f);
        Button infoClose = Button("Close", info.transform, .85f, .92f, .98f, .99f, "닫기");
        UnityEventTools.AddPersistentListener(infoClose.onClick, popup.Close);
        Button equip = Button("Equip", info.transform, .32f, .12f, .68f, .21f, "장착");
        UnityEventTools.AddPersistentListener(equip.onClick, popup.EquipCharacter);
        Button[] growth = new Button[3];
        string[] growthNames = { "레벨업", "성급업", "등급업" };
        for (int i = 0; i < growth.Length; i++)
        {
            growth[i] = Button($"Growth_{i}", info.transform, .04f + i * .32f, .025f, .32f + i * .32f, .105f, growthNames[i]);
            UnityEventTools.AddIntPersistentListener(growth[i].onClick, popup.RequestGrowth, i);
        }

        Transform skillOverlay = Overlay("SkillOverlay", modal);
        Image skill = Image("SkillPanel", skillOverlay, .10f, .24f, .90f, .76f, PANEL);
        Text skillTitle = Label("Title", skill.transform, .05f, .84f, .80f, .96f, "스킬", 38, INK);
        Image skillIcon = Image("Icon", skill.transform, .37f, .61f, .63f, .83f, Color.white);
        skillIcon.preserveAspect = true;
        Text skillDescription = Label("Description", skill.transform, .07f, .11f, .93f, .58f, "", 30, INK);
        Button skillClose = Button("Close", skill.transform, .78f, .86f, .97f, .97f, "닫기");
        UnityEventTools.AddPersistentListener(skillClose.onClick, popup.CloseSkill);

        Transform confirmOverlay = Overlay("ConfirmationOverlay", modal);
        Image confirmPanel = Image("Confirmation", confirmOverlay, .10f, .28f, .90f, .72f, DARK);
        Text confirmText = Label("Message", confirmPanel.transform, .06f, .55f, .94f, .86f, "", 30, Color.white);
        Button confirm = Button("Confirm", confirmPanel.transform, .07f, .06f, .47f, .22f, "확인");
        Button cancel = Button("Cancel", confirmPanel.transform, .53f, .06f, .93f, .22f, "취소");
        Button messageConfirm = Button("MessageConfirm", confirmPanel.transform, .30f, .06f, .70f, .22f, "확인");
        foreach (Button button in new[] { confirm, cancel, messageConfirm })
        {
            button.image.color = new Color(.25f, .29f, .34f);
        }
        UnityEventTools.AddPersistentListener(confirm.onClick, popup.ConfirmGrowth);
        UnityEventTools.AddPersistentListener(cancel.onClick, popup.CancelGrowth);
        UnityEventTools.AddPersistentListener(messageConfirm.onClick, popup.ConfirmGrowth);

        string[] slotNames = { "Center", "Left", "Right" };
        float[] slotCenters = { .50f, .27f, .73f };
        Image[] materialIcons = new Image[3];
        Text[] materialAmounts = new Text[3];
        for (int i = 0; i < slotNames.Length; i++)
        {
            float x = slotCenters[i];
            materialIcons[i] = Image("MaterialIcon" + slotNames[i], confirmPanel.transform,
                x - .176f, .397f, x - .102f, .473f, Color.white);
            materialIcons[i].preserveAspect = true;
            materialAmounts[i] = Label("MaterialAmount" + slotNames[i], confirmPanel.transform,
                x - .101f, .393f, x + .188f, .477f, "", 30, Color.white);
        }

        Set(popup, "_window", window, "_infoPanel", info.gameObject, "_detail", infoDetail, "_equip", equip,
            "_skillPanel", skillOverlay.gameObject, "_skillTitle", skillTitle, "_skillDescription", skillDescription,
            "_skillIcon", skillIcon, "_confirmPanel", confirmOverlay.gameObject, "_confirmText", confirmText,
            "_confirm", confirm, "_cancel", cancel, "_messageConfirm", messageConfirm,
            "_silverIcon", FindBelow(top, "EunjaIcon").GetComponent<Image>().sprite,
            "_goldIcon", FindBelow(top, "GeumjaIcon").GetComponent<Image>().sprite);
        SetArray(popup, "_materialIcons", materialIcons);
        SetArray(popup, "_materialAmounts", materialAmounts);
        SetArray(popup, "_fragmentIcons", Enumerable.Range(34, 6).Select(Icon).ToArray());
        SetArray(popup, "_stoneIcons", Enumerable.Range(40, 6).Select(Icon).ToArray());
        SetArray(popup, "_growthButtons", growth);
        skillOverlay.gameObject.SetActive(false);
        confirmOverlay.gameObject.SetActive(false);
        modal.gameObject.SetActive(false);

        Transform settingsModal = BuildSettings(popUps);
        PopUpManager manager = popUps.GetComponent<PopUpManager>();
        SerializedObject managerData = new(manager);
        SerializedProperty popUpArray = managerData.FindProperty("_popUps");
        popUpArray.arraySize = (int)PopUpType.Max;
        popUpArray.GetArrayElementAtIndex((int)PopUpType.Settings).objectReferenceValue = settingsModal.gameObject;
        popUpArray.GetArrayElementAtIndex((int)PopUpType.Character).objectReferenceValue = modal.gameObject;
        managerData.ApplyModifiedPropertiesWithoutUndo();

        Set(window, "_detail", detail, "_characterPopUp", popup, "_silver", silver, "_gold", gold);
        SetArray(window, "_slots", slots);
        SetPresentations(window, presentations);
        LobbyWindow lobbyWindow = lobby.GetComponent<LobbyWindow>();
        Text lobbySilver = ReplaceAmount(lobby.Find("TopBar"), "EunjaAmount");
        Text lobbyGold = ReplaceAmount(lobby.Find("TopBar"), "GeumjaAmount");
        Set(lobbyWindow, "_characterWindow", window, "_silver", lobbySilver, "_gold", lobbyGold);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("CHARACTER_SCREEN_BUILD_OK");
    }

    private static CharacterPresentation[] BuildPresentations()
    {
        string[] elements = { "Non", "Fire", "Water", "Wood", "Earth", "Wind" };
        int[][] icons = { new[] { 9, 10, 11, 12 }, new[] { 13, 14, 11, 16 }, new[] { 17, 18, 11, 20 },
            new[] { 21, 22, 11, 24 }, new[] { 25, 26, 11, 28 }, new[] { 29, 30, 11, 32 } };
        string[][] names = { new[] { "검격", "무심의 수련", "무심의 경지", "진기폭발" },
            new[] { "불씨 감각", "화염 인술", "극의 : 청화", "유성낙화" },
            new[] { "잔물결", "물의 흐름", "수룡타", "창해범람" },
            new[] { "새싹의 생기", "숲의 친화", "세계수의 가호", "가시만개" },
            new[] { "강한 의지", "대지의 기운", "대지의 분노", "대지붕괴" },
            new[] { "풍압", "순풍", "풍아일체", "폭풍승천" } };
        CharacterPresentation[] result = new CharacterPresentation[elements.Length];
        for (int i = 0; i < result.Length; i++)
        {
            string name = elements[i];
            result[i] = new CharacterPresentation
            {
                CharacterId = i + 1,
                Portrait = AssetDatabase.LoadAllAssetsAtPath($"Assets/04_Arts/00_Image/Character/{name}_Ninja/{name}_Select_Idle.png").OfType<Sprite>().OrderBy(value => value.name, StringComparer.Ordinal).First(),
                Controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/07_Animations/Character/{name}_Ninja/{name}_Ninja_Lobby.controller"),
                SkillNames = names[i],
                SkillIcons = icons[i].Select(Icon).ToArray()
            };
        }
        return result;
    }

    private static CharacterDetailView BuildDetail(Transform parent, string name, float x0, float y0, float x1, float y1)
    {
        Transform root = Rect(name, parent, x0, y0, x1, y1);
        CharacterDetailView view = root.gameObject.AddComponent<CharacterDetailView>();
        Image portrait = Image("AnimatedCharacter", root, .0f, .10f, .43f, .87f, Color.white);
        portrait.preserveAspect = true;
        Animator animator = portrait.gameObject.AddComponent<Animator>();
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        Text title = Label("Name", root, .03f, .87f, .95f, 1f, "캐릭터", 38, INK);
        Text rankLevel = Label("RankLevelElement", root, .43f, .73f, .99f, .87f, "N  Lv. 1", 34, INK);
        Text stars = Label("Stars", root, .43f, .61f, .99f, .73f, "☆☆☆☆☆", 36, new Color(.95f, .65f, .08f));
        Text[] stats = new Text[3];
        for (int i = 0; i < stats.Length; i++)
        {
            float bottom = .49f - .12f * i;
            Image("StatIcon_" + i, root, .43f, bottom, .51f, bottom + .10f, Color.white, Icon(4 + i)).preserveAspect = true;
            Image row = Image("StatRow_" + i, root, .53f, bottom, .98f, bottom + .10f, DARK);
            stats[i] = Label("Value", row.transform, .03f, .02f, .97f, .98f, "", 30, Color.white);
        }

        Button[] skills = new Button[4];
        Image[] skillIcons = new Image[4];
        GameObject[] locks = new GameObject[4];
        for (int i = 0; i < skills.Length; i++)
        {
            float left = .43f + .14f * i;
            skills[i] = Button("Skill_" + i, root, left, .06f, left + .12f, .22f, "");
            skillIcons[i] = Image("Icon", skills[i].transform, .07f, .08f, .93f, .94f, Color.white);
            skillIcons[i].preserveAspect = true;
            Image locked = Image("Lock", skills[i].transform, .02f, .02f, .98f, .98f, new Color(0, 0, 0, .65f));
            Label("Label", locked.transform, .02f, .10f, .98f, .90f, "잠금", 22, Color.white);
            locks[i] = locked.gameObject;
        }
        Set(view, "_animator", animator, "_name", title, "_rankLevel", rankLevel, "_stars", stars);
        SetArray(view, "_stats", stats);
        SetArray(view, "_skills", skills);
        SetArray(view, "_skillIcons", skillIcons);
        SetArray(view, "_locks", locks);
        return view;
    }

    private static Transform BuildSettings(Transform parent)
    {
        Transform modal = Overlay("Settings_PopUp", parent);
        CharacterSettingsPopUp settings = modal.gameObject.AddComponent<CharacterSettingsPopUp>();
        Image panel = Image("SettingsPanel", modal, .09f, .25f, .91f, .75f, PANEL);
        Label("Title", panel.transform, .1f, .86f, .8f, .98f, "환경 설정", 38, INK);
        Button close = Button("Close", panel.transform, .79f, .87f, .98f, .98f, "닫기");
        UnityEventTools.AddPersistentListener(close.onClick, settings.Close);
        Text bgmValue = Label("BgmValue", panel.transform, .08f, .72f, .92f, .82f, "BGM", 32, INK);
        Text sfxValue = Label("SfxValue", panel.transform, .08f, .46f, .92f, .56f, "SFX", 32, INK);
        Slider bgm = BuildSlider(panel.transform, "Bgm", .60f, .70f);
        Slider sfx = BuildSlider(panel.transform, "Sfx", .34f, .44f);
        UnityEventTools.AddPersistentListener(bgm.onValueChanged, settings.SetVolume);
        UnityEventTools.AddPersistentListener(sfx.onValueChanged, settings.SetVolume);
        Text damage = Label("DamageValue", panel.transform, .08f, .22f, .92f, .32f, "데미지 폰트", 30, INK);
        Button off = Button("DamageOff", panel.transform, .08f, .11f, .47f, .21f, "OFF");
        Button on = Button("DamageOn", panel.transform, .53f, .11f, .92f, .21f, "ON");
        UnityEventTools.AddIntPersistentListener(off.onClick, settings.SetDamageText, 0);
        UnityEventTools.AddIntPersistentListener(on.onClick, settings.SetDamageText, 1);
        Text status = Label("Status", panel.transform, .04f, .01f, .96f, .10f, "", 25, INK);
        Set(settings, "_bgm", bgm, "_sfx", sfx, "_bgmValue", bgmValue, "_sfxValue", sfxValue, "_damageValue", damage, "_status", status);
        modal.gameObject.SetActive(false);
        return modal;
    }

    private static Slider BuildSlider(Transform parent, string name, float y0, float y1)
    {
        Image track = Image(name, parent, .08f, y0, .92f, y1, DARK);
        track.raycastTarget = true;
        Slider slider = track.gameObject.AddComponent<Slider>();
        Image fill = Image("Fill", track.transform, 0, 0, 1, 1, new Color(.4f, .7f, .3f));
        Image handle = Image("Handle", track.transform, 0, 0, 0, 1, PANEL);
        handle.raycastTarget = true;
        handle.color = Color.white;
        handle.rectTransform.sizeDelta = new Vector2(32, 0);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private static Text ReplaceAmount(Transform top, string name)
    {
        Transform original = FindBelow(top, name);
        Object.DestroyImmediate(original.GetComponent<Image>());
        Text text = original.gameObject.AddComponent<Text>();
        ConfigureText(text, 34, INK);
        RectTransform rect = (RectTransform)original;
        SetBounds(rect, .38f, .15f, .96f, .85f);
        text.text = "0";
        return text;
    }

    private static Transform Overlay(string name, Transform parent)
    {
        Image overlay = Image(name, parent, 0, 0, 1, 1, new Color(0, 0, 0, .65f));
        overlay.raycastTarget = true;
        return overlay.transform;
    }

    private static Transform Rect(string name, Transform parent, float x0, float y0, float x1, float y1)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.layer = 5;
        RectTransform rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        SetBounds(rect, x0, y0, x1, y1);
        return rect;
    }

    private static void SetBounds(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        Vector2 parentSize = ((RectTransform)rect.parent).rect.size;
        Vector2 min = new(x0, y0);
        Vector2 max = new(x1, y1);
        Vector2 center = (min + max) * 0.5f;
        Vector2 anchor = new(Mathf.Round(center.x * 2f) * 0.5f, Mathf.Round(center.y * 2f) * 0.5f);
        Vector2 span = new(x1 - x0 >= .75f ? 1f : 0f, y1 - y0 >= .75f ? 1f : 0f);
        rect.anchorMin = new Vector2(span.x > 0f ? 0f : anchor.x, span.y > 0f ? 0f : anchor.y);
        rect.anchorMax = rect.anchorMin + span;
        rect.anchoredPosition = Vector2.Scale(center - rect.anchorMin - span * 0.5f, parentSize);
        rect.sizeDelta = Vector2.Scale(max - min - span, parentSize);
    }

    private static Image Image(string name, Transform parent, float x0, float y0, float x1, float y1, Color color, Sprite sprite = null)
    {
        Image image = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Image>();
        image.color = color;
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static Text Label(string name, Transform parent, float x0, float y0, float x1, float y1, string value, int size, Color color)
    {
        Text text = Rect(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Text>();
        ConfigureText(text, size, color);
        text.text = value;
        return text;
    }

    private static void ConfigureText(Text text, int size, Color color)
    {
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private static Button Button(string name, Transform parent, float x0, float y0, float x1, float y1, string text)
    {
        Image image = Image(name, parent, x0, y0, x1, y1, DARK);
        image.raycastTarget = true;
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Label("Label", image.transform, .02f, .02f, .98f, .98f, text, 28, Color.white);
        return button;
    }

    private static Sprite Sprite(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
    }

    private static Sprite Icon(int index)
    {
        return AssetDatabase.LoadAllAssetsAtPath(ICON_PATH).OfType<Sprite>().Single(value => value.name == $"Icon_Sheet_2_{index}");
    }

    private static Transform Find(Scene scene, string name)
    {
        return scene.GetRootGameObjects().SelectMany(value => value.GetComponentsInChildren<Transform>(true)).First(value => value.name == name);
    }

    private static Transform FindBelow(Transform root, string name)
    {
        return root.GetComponentsInChildren<Transform>(true).First(value => value.name == name);
    }

    private static void Set(Object target, params object[] fields)
    {
        SerializedObject data = new(target);
        for (int i = 0; i < fields.Length; i += 2)
        {
            data.FindProperty((string)fields[i]).objectReferenceValue = (Object)fields[i + 1];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray<T>(Object target, string name, T[] values) where T : Object
    {
        SerializedObject data = new(target);
        SerializedProperty array = data.FindProperty(name);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPresentations(CharacterWindow target, CharacterPresentation[] values)
    {
        SerializedObject data = new(target);
        SerializedProperty array = data.FindProperty("_characters");
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty item = array.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("CharacterId").intValue = values[i].CharacterId;
            item.FindPropertyRelative("Portrait").objectReferenceValue = values[i].Portrait;
            item.FindPropertyRelative("Controller").objectReferenceValue = values[i].Controller;
            SerializedProperty names = item.FindPropertyRelative("SkillNames");
            names.arraySize = 4;
            for (int j = 0; j < 4; j++)
            {
                names.GetArrayElementAtIndex(j).stringValue = values[i].SkillNames[j];
            }
            SerializedProperty icons = item.FindPropertyRelative("SkillIcons");
            icons.arraySize = 4;
            for (int j = 0; j < 4; j++)
            {
                icons.GetArrayElementAtIndex(j).objectReferenceValue = values[i].SkillIcons[j];
            }
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

}
#endif

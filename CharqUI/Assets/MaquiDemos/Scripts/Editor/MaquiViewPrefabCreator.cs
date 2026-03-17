using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MaquiDemos.Gallery;
using MaquiDemos.GameUI;
using MaquiDemos.Frosted;

namespace MaquiDemos.Editor
{
    public static class MaquiViewPrefabCreator
    {
        const string ViewsPath = "Assets/Resources/Views";

        [MenuItem("Maqui/Create View Prefabs/Create All", priority = 10)]
        public static void CreateAll()
        {
            EnsureFolder(ViewsPath);
            CreateDemo1GalleryView();
            CreateDemo2HUDView();
            CreateDemo2InventoryView();
            CreateDemo2ShopView();
            CreateDemo3FrostedHUDView();
            CreateDemo3NotificationsView();
            CreateDemo3SettingsView();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Maqui] All view prefabs created.");
        }

        // ── Demo 1: Gallery ───────────────────────────────────────────────────

        [MenuItem("Maqui/Create View Prefabs/Demo1 - Gallery View", priority = 11)]
        public static void CreateDemo1GalleryView()
        {
            var root = new GameObject("GalleryView");
            var rt = root.AddComponent<RectTransform>();
            SetFullScreen(rt);
            var cg = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<GalleryView>();
            root.AddComponent<Image>().color = new Color(0.97f, 0.97f, 0.97f, 1f);

            // Header
            var header = CreatePanel(root.transform, "Header", 0f, 1f, 1f, 1f, 0f, -80f, 0f, 0f);
            header.AddComponent<Image>().color = Color.white;
            var subtitleGO = CreateTMPGO(header.transform, "Subtitle", "Buttons");
            var subtitleTMP = subtitleGO.GetComponent<TextMeshProUGUI>();
            subtitleTMP.fontSize = 13;
            subtitleTMP.color = new Color(0.5f, 0.5f, 0.5f);
            SetAnchors(subtitleGO.GetComponent<RectTransform>(), 0f, 0.6f, 0f, 0.5f, 16f, 4f, 0f, 0f);
            var themeBtn = CreateButtonGO(header.transform, "ThemeBtn", "Theme");
            SetAnchors(themeBtn.GetComponent<RectTransform>(), 1f, 1f, 0.15f, 0.85f, -110f, 0f, -8f, 0f);
            var themeBtnComp = themeBtn.GetComponent<Button>();
            var themeLabelTMP = themeBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Tab bar
            var tabBar = CreatePanel(root.transform, "TabBar", 0f, 1f, 1f, 1f, 0f, -132f, 0f, -80f);
            tabBar.AddComponent<Image>().color = Color.white;
            string[] tabNames = { "Buttons", "Cards", "Fields" };
            var tabButtons = new Button[3];
            var tabIndicators = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float xMin = i / 3f, xMax = (i + 1) / 3f;
                var cell = CreatePanel(tabBar.transform, "Tab_" + tabNames[i], xMin, xMax, 0f, 1f);
                tabButtons[i] = cell.AddComponent<Button>();
                var label = CreateTMPGO(cell.transform, "Label", tabNames[i]);
                var lTMP = label.GetComponent<TextMeshProUGUI>();
                lTMP.fontSize = 14;
                lTMP.alignment = TextAlignmentOptions.Center;
                lTMP.color = new Color(0.2f, 0.2f, 0.2f);
                SetAnchors(label.GetComponent<RectTransform>(), 0f, 1f, 0.2f, 1f);
                var ind = CreatePanel(cell.transform, "Indicator", 0.1f, 0.9f, 0f, 0f, 0f, 0f, 0f, 3f);
                tabIndicators[i] = ind.AddComponent<Image>();
                tabIndicators[i].color = i == 0
                    ? new Color(0.2f, 0.47f, 0.9f, 1f)
                    : new Color(0.2f, 0.47f, 0.9f, 0.25f);
            }

            // Content panels
            const string ComponentsPath = "Assets/UI/OneUI/Prefabs/Components";
            string[][] panelPrefabs = {
                new[] {
                    ComponentsPath + "/Buttons/BlueIconButton.prefab",
                    ComponentsPath + "/Buttons/GreenIconButton.prefab",
                    ComponentsPath + "/Buttons/PinkIconButton.prefab",
                    ComponentsPath + "/Buttons/LikeButton.prefab",
                    ComponentsPath + "/Buttons/MenuIconButton.prefab",
                    ComponentsPath + "/Elements/BasicListItem.prefab",
                    ComponentsPath + "/Elements/AnimatedProgressBlue.prefab",
                    ComponentsPath + "/Layouts/InfoBox.prefab",
                    ComponentsPath + "/Layouts/WarningBox.prefab",
                },
                new[] {
                    ComponentsPath + "/Cards/BasicCard.prefab",
                    ComponentsPath + "/Cards/NoBodyCard.prefab",
                    ComponentsPath + "/Cards/NoFooterCard.prefab",
                    ComponentsPath + "/Cards/FullCardElement.prefab",
                    ComponentsPath + "/Elements/HeroImage.prefab",
                    ComponentsPath + "/Elements/IconCounter.prefab",
                },
                new[] {
                    ComponentsPath + "/Fields/BlueSlider.prefab",
                    ComponentsPath + "/Fields/GreenSlider.prefab",
                    ComponentsPath + "/Fields/PinkSlider.prefab",
                    ComponentsPath + "/Fields/BasicToggle.prefab",
                    ComponentsPath + "/Fields/ToggleVariant.prefab",
                    ComponentsPath + "/Fields/BaseInputField.prefab",
                    ComponentsPath + "/Fields/IconInputField.prefab",
                    ComponentsPath + "/Fields/BaseDropdown.prefab",
                    ComponentsPath + "/Fields/SearchInput.prefab",
                },
            };

            var contentArea = CreatePanel(root.transform, "Content", 0f, 1f, 0f, 1f, 0f, 0f, 0f, -132f);
            var contentPanels = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                var cp = CreatePanel(contentArea.transform, "Panel_" + tabNames[i], 0f, 1f, 0f, 1f);
                cp.AddComponent<Image>().color = new Color(0.95f, 0.95f, 0.96f, 1f);
                cp.SetActive(i == 0);
                contentPanels[i] = cp;
                var sv = CreatePanel(cp.transform, "ScrollView", 0f, 1f, 0f, 1f, 8f, 8f, -8f, -8f);
                var sr = sv.AddComponent<ScrollRect>();
                sr.horizontal = false;
                var vp = CreatePanel(sv.transform, "Viewport", 0f, 1f, 0f, 1f);
                vp.AddComponent<RectMask2D>();
                var ct = CreatePanel(vp.transform, "Content", 0f, 1f, 1f, 1f);
                var ctRT = ct.GetComponent<RectTransform>();
                ctRT.pivot = new Vector2(0.5f, 1f);
                var vlg = ct.AddComponent<VerticalLayoutGroup>();
                vlg.padding = new RectOffset(12, 12, 12, 12);
                vlg.spacing = 10;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
                ct.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                sr.viewport = vp.GetComponent<RectTransform>();
                sr.content = ctRT;

                // Populate with OneUI component prefabs
                foreach (var path in panelPrefabs[i])
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, ct.transform);
                        var le = instance.GetComponent<LayoutElement>() ?? instance.AddComponent<LayoutElement>();
                        le.minHeight = 60;
                    }
                }
            }

            var so = new SerializedObject(view);
            SetObjArrayRef(so, "_tabButtons", tabButtons);
            SetObjArrayRef(so, "_tabIndicators", tabIndicators);
            SetObjArrayRef(so, "_contentPanels", contentPanels);
            so.FindProperty("_subtitleText").objectReferenceValue = subtitleTMP;
            so.FindProperty("_themeButton").objectReferenceValue = themeBtnComp;
            so.FindProperty("_themeLabel").objectReferenceValue = themeLabelTMP;
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_panelRect").objectReferenceValue = rt;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ViewsPath + "/Demo1_Gallery.prefab");
            Debug.Log("[Maqui] Demo1_Gallery.prefab created.");
        }

        // ── Demo 2: HUD ───────────────────────────────────────────────────────

        [MenuItem("Maqui/Create View Prefabs/Demo2 - HUD View", priority = 12)]
        public static void CreateDemo2HUDView()
        {
            var root = new GameObject("HUDView");
            SetFullScreen(root.AddComponent<RectTransform>());
            var view = root.AddComponent<HUDView>();

            // Gold top-left
            var topBar = CreatePanel(root.transform, "TopBar", 0f, 0.4f, 1f, 1f, 8f, -58f, -8f, 0f);
            topBar.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.08f, 0.92f);
            var goldGO = CreateTMPGO(topBar.transform, "GoldText", "1,250 g");
            var goldTMP = goldGO.GetComponent<TextMeshProUGUI>();
            goldTMP.fontSize = 20;
            goldTMP.color = new Color(1f, 0.85f, 0.15f);
            goldTMP.alignment = TextAlignmentOptions.Center;
            SetAnchors(goldGO.GetComponent<RectTransform>(), 0f, 1f, 0f, 1f, 8f, 4f, -8f, -4f);

            // HP/MP bars bottom-left
            var barsPanel = CreatePanel(root.transform, "BarsPanel", 0f, 0.45f, 0f, 0f, 8f, 8f, -8f, 130f);
            barsPanel.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.08f, 0.92f);
            var vlg = barsPanel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 10, 10);
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var hpSlider = CreateStyledSlider(barsPanel.transform, "HPBar", new Color(0.9f, 0.2f, 0.3f), 0.72f);
            var mpSlider = CreateStyledSlider(barsPanel.transform, "MPBar", new Color(0.2f, 0.4f, 0.9f), 0.55f);

            // Status text
            var sgGO = new GameObject("StatusGroup");
            sgGO.transform.SetParent(root.transform, false);
            SetAnchors(sgGO.AddComponent<RectTransform>(), 0.2f, 0.8f, 0.4f, 0.6f);
            var statusCG = sgGO.AddComponent<CanvasGroup>();
            statusCG.alpha = 0f;
            var statusGO = CreateTMPGO(sgGO.transform, "StatusText", "");
            var statusTMP = statusGO.GetComponent<TextMeshProUGUI>();
            statusTMP.fontSize = 18;
            statusTMP.fontStyle = FontStyles.Bold;
            statusTMP.color = Color.white;
            statusTMP.alignment = TextAlignmentOptions.Center;
            SetAnchors(statusGO.GetComponent<RectTransform>(), 0f, 1f, 0f, 1f);

            // Inventory button bottom-right
            var invBtn = CreateButtonGO(root.transform, "InventoryBtn", "Bag");
            SetAnchors(invBtn.GetComponent<RectTransform>(), 1f, 1f, 0f, 0f, -120f, 8f, -8f, 56f);
            invBtn.GetComponent<Image>().color = new Color(0.22f, 0.47f, 0.90f, 0.95f);
            var invBtnComp = invBtn.GetComponent<Button>();

            var so = new SerializedObject(view);
            so.FindProperty("_goldText").objectReferenceValue = goldTMP;
            so.FindProperty("_hpSlider").objectReferenceValue = hpSlider;
            so.FindProperty("_mpSlider").objectReferenceValue = mpSlider;
            so.FindProperty("_statusText").objectReferenceValue = statusTMP;
            so.FindProperty("_statusGroup").objectReferenceValue = statusCG;
            so.FindProperty("_inventoryButton").objectReferenceValue = invBtnComp;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ViewsPath + "/Demo2_HUD.prefab");
            Debug.Log("[Maqui] Demo2_HUD.prefab created.");
        }

        // ── Demo 2: Inventory ─────────────────────────────────────────────────

        [MenuItem("Maqui/Create View Prefabs/Demo2 - Inventory View", priority = 13)]
        public static void CreateDemo2InventoryView()
        {
            var root = new GameObject("InventoryView");
            var rootRT = root.AddComponent<RectTransform>();
            SetCenteredModal(rootRT, 360f, 480f);
            var cg = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<InventoryView>();
            root.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f, 1f);

            // Header
            var titleGO = CreateTMPGO(root.transform, "Title", "Inventory (0)");
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.fontSize = 20;
            titleTMP.fontStyle = FontStyles.Bold;
            titleTMP.color = Color.white;
            titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(titleGO.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 16f, -56f, -56f, 0f);

            var closeBtn = CreateButtonGO(root.transform, "CloseBtn", "X");
            SetAnchors(closeBtn.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -44f, -44f, -8f, -8f);
            var closeBtnComp = closeBtn.GetComponent<Button>();

            // ScrollView → VerticalLayoutGroup Content = _itemContainer
            var scrollGO = CreatePanel(root.transform, "ScrollView", 0f, 1f, 0f, 1f, 8f, 60f, -8f, -64f);
            var sr = scrollGO.AddComponent<ScrollRect>();
            sr.horizontal = false;
            var vp = CreatePanel(scrollGO.transform, "Viewport", 0f, 1f, 0f, 1f);
            vp.AddComponent<RectMask2D>();
            var content = CreatePanel(vp.transform, "Content", 0f, 1f, 1f, 1f);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.pivot = new Vector2(0.5f, 1f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp.GetComponent<RectTransform>();
            sr.content = contentRT;

            // Footer with Shop button
            var footer = CreatePanel(root.transform, "Footer", 0f, 1f, 0f, 0f, 0f, 0f, 0f, 56f);
            footer.AddComponent<Image>().color = new Color(0.08f, 0.06f, 0.12f);
            var shopBtn = CreateButtonGO(footer.transform, "ShopBtn", "Go to Shop");
            SetAnchors(shopBtn.GetComponent<RectTransform>(), 0.1f, 0.9f, 0.1f, 0.9f);
            shopBtn.GetComponent<Image>().color = new Color(0.2f, 0.65f, 0.35f);
            var shopBtnComp = shopBtn.GetComponent<Button>();

            // Row prefab (hidden template)
            var rowGO = new GameObject("InventoryItemRow_Prefab");
            rowGO.transform.SetParent(root.transform, false);
            rowGO.AddComponent<RectTransform>();
            rowGO.SetActive(false);
            rowGO.AddComponent<Image>().color = new Color(0.18f, 0.15f, 0.25f);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 72f;
            var rowComp = rowGO.AddComponent<InventoryItemRow>();
            var iconGO = CreatePanel(rowGO.transform, "Icon", 0f, 0f, 0f, 1f, 4f, 4f, 68f, -4f);
            iconGO.AddComponent<Image>().color = new Color(0.3f, 0.25f, 0.4f);
            var nameGO = CreateTMPGO(rowGO.transform, "ItemName", "Item Name");
            SetAnchors(nameGO.GetComponent<RectTransform>(), 0f, 1f, 0.5f, 1f, 76f, 2f, -8f, -4f);
            var qtyGO = CreateTMPGO(rowGO.transform, "Qty", "x1");
            qtyGO.GetComponent<TextMeshProUGUI>().fontSize = 12;
            qtyGO.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);
            SetAnchors(qtyGO.GetComponent<RectTransform>(), 0f, 0.5f, 0f, 0.5f, 76f, 2f, -4f, -2f);
            var valueGO = CreateTMPGO(rowGO.transform, "Value", "50 g");
            valueGO.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.85f, 0.2f);
            valueGO.GetComponent<TextMeshProUGUI>().fontSize = 12;
            SetAnchors(valueGO.GetComponent<RectTransform>(), 0.5f, 1f, 0f, 0.5f, 4f, 2f, -8f, -2f);
            var soRow = new SerializedObject(rowComp);
            soRow.FindProperty("_icon").objectReferenceValue = iconGO.GetComponent<Image>();
            soRow.FindProperty("_nameText").objectReferenceValue = nameGO.GetComponent<TextMeshProUGUI>();
            soRow.FindProperty("_qtyText").objectReferenceValue = qtyGO.GetComponent<TextMeshProUGUI>();
            soRow.FindProperty("_valueText").objectReferenceValue = valueGO.GetComponent<TextMeshProUGUI>();
            soRow.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(view);
            so.FindProperty("_titleText").objectReferenceValue = titleTMP;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtnComp;
            so.FindProperty("_itemContainer").objectReferenceValue = content.GetComponent<Transform>();
            so.FindProperty("_rowPrefab").objectReferenceValue = rowComp;
            so.FindProperty("_shopButton").objectReferenceValue = shopBtnComp;
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_panelRect").objectReferenceValue = rootRT;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ViewsPath + "/Demo2_Inventory.prefab");
            Debug.Log("[Maqui] Demo2_Inventory.prefab created.");
        }

        // ── Demo 2: Shop ──────────────────────────────────────────────────────

        [MenuItem("Maqui/Create View Prefabs/Demo2 - Shop View", priority = 14)]
        public static void CreateDemo2ShopView()
        {
            var root = new GameObject("ShopView");
            var rootRT = root.AddComponent<RectTransform>();
            SetCenteredModal(rootRT, 400f, 560f);
            var cg = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<ShopView>();
            root.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f, 1f);

            var headerGO = CreateTMPGO(root.transform, "Header", "Shop");
            var headerTMP = headerGO.GetComponent<TextMeshProUGUI>();
            headerTMP.fontSize = 20;
            headerTMP.fontStyle = FontStyles.Bold;
            headerTMP.color = new Color(1f, 0.85f, 0.2f);
            headerTMP.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(headerGO.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 16f, -56f, -56f, 0f);

            var closeBtn = CreateButtonGO(root.transform, "CloseBtn", "X");
            SetAnchors(closeBtn.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -44f, -44f, -8f, -8f);
            var closeBtnComp = closeBtn.GetComponent<Button>();

            // Left: catalogue list (60% width)
            var listArea = CreatePanel(root.transform, "CatalogueArea", 0f, 0.58f, 0f, 1f, 4f, 4f, -2f, -64f);
            var sr2 = listArea.AddComponent<ScrollRect>();
            sr2.horizontal = false;
            var vp2 = CreatePanel(listArea.transform, "Viewport", 0f, 1f, 0f, 1f);
            vp2.AddComponent<Image>().color = Color.clear;
            vp2.AddComponent<Mask>().showMaskGraphic = false;
            var catalogue = CreatePanel(vp2.transform, "Catalogue", 0f, 1f, 1f, 1f);
            var catalogueRT = catalogue.GetComponent<RectTransform>();
            catalogueRT.pivot = new Vector2(0.5f, 1f);
            var vlg2 = catalogue.AddComponent<VerticalLayoutGroup>();
            vlg2.spacing = 4;
            vlg2.padding = new RectOffset(4, 4, 4, 4);
            vlg2.childForceExpandWidth = true;
            vlg2.childForceExpandHeight = false;
            catalogue.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr2.viewport = vp2.GetComponent<RectTransform>();
            sr2.content = catalogueRT;

            // Right: detail panel (42% width)
            var detailRoot = CreatePanel(root.transform, "DetailRoot", 0.58f, 1f, 0f, 1f, 2f, 4f, -4f, -64f);
            detailRoot.AddComponent<Image>().color = new Color(0.09f, 0.07f, 0.15f);
            detailRoot.SetActive(false);

            var detailNameGO = CreateTMPGO(detailRoot.transform, "DetailName", "Item Name");
            var detailNameTMP = detailNameGO.GetComponent<TextMeshProUGUI>();
            detailNameTMP.fontSize = 15;
            detailNameTMP.fontStyle = FontStyles.Bold;
            detailNameTMP.color = Color.white;
            detailNameTMP.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(detailNameGO.GetComponent<RectTransform>(), 0f, 1f, 0.75f, 1f, 8f, 4f, -8f, -4f);

            var detailDescGO = CreateTMPGO(detailRoot.transform, "DetailDesc", "A great item.");
            var detailDescTMP = detailDescGO.GetComponent<TextMeshProUGUI>();
            detailDescTMP.fontSize = 12;
            detailDescTMP.color = new Color(0.75f, 0.75f, 0.85f);
            SetAnchors(detailDescGO.GetComponent<RectTransform>(), 0f, 1f, 0.4f, 0.75f, 8f, 2f, -8f, -2f);

            var detailPriceGO = CreateTMPGO(detailRoot.transform, "DetailPrice", "0 g");
            var detailPriceTMP = detailPriceGO.GetComponent<TextMeshProUGUI>();
            detailPriceTMP.fontSize = 14;
            detailPriceTMP.color = new Color(1f, 0.85f, 0.2f);
            detailPriceTMP.alignment = TextAlignmentOptions.MidlineRight;
            SetAnchors(detailPriceGO.GetComponent<RectTransform>(), 0f, 1f, 0.22f, 0.42f, 8f, 2f, -8f, -2f);

            var buyBtn = CreateButtonGO(detailRoot.transform, "BuyBtn", "Buy");
            SetAnchors(buyBtn.GetComponent<RectTransform>(), 0.05f, 0.95f, 0.04f, 0.2f);
            buyBtn.GetComponent<Image>().color = new Color(0.2f, 0.65f, 0.35f);
            var buyBtnComp = buyBtn.GetComponent<Button>();

            // Feedback
            var feedbackGO = CreatePanel(root.transform, "Feedback", 0f, 1f, 0f, 0f, 8f, 0f, -8f, 0f);
            var feedbackCG = feedbackGO.AddComponent<CanvasGroup>();
            feedbackCG.alpha = 0f;
            var feedbackTextGO = CreateTMPGO(feedbackGO.transform, "FeedbackText", "Purchased!");
            var feedbackTMP = feedbackTextGO.GetComponent<TextMeshProUGUI>();
            feedbackTMP.fontSize = 14;
            feedbackTMP.color = new Color(0.2f, 0.9f, 0.4f);
            feedbackTMP.alignment = TextAlignmentOptions.Center;
            SetAnchors(feedbackGO.GetComponent<RectTransform>(), 0f, 1f, 0f, 0f, 8f, 0f, -8f, 32f);

            // Row prefab
            var rowGO = new GameObject("ShopItemRow_Prefab");
            rowGO.transform.SetParent(root.transform, false);
            rowGO.AddComponent<RectTransform>();
            rowGO.SetActive(false);
            rowGO.AddComponent<Image>().color = new Color(0.18f, 0.15f, 0.25f);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 60f;
            var rowComp = rowGO.AddComponent<ShopItemRow>();
            var sIconGO = CreatePanel(rowGO.transform, "Icon", 0f, 0f, 0f, 1f, 4f, 4f, 52f, -4f);
            sIconGO.AddComponent<Image>().color = new Color(0.3f, 0.25f, 0.4f);
            var sNameGO = CreateTMPGO(rowGO.transform, "ItemName", "Item");
            sNameGO.GetComponent<TextMeshProUGUI>().fontSize = 13;
            SetAnchors(sNameGO.GetComponent<RectTransform>(), 0f, 0.7f, 0.4f, 1f, 60f, 2f, -4f, -4f);
            var sPriceGO = CreateTMPGO(rowGO.transform, "Price", "100 g");
            sPriceGO.GetComponent<TextMeshProUGUI>().color = new Color(1f, 0.85f, 0.2f);
            sPriceGO.GetComponent<TextMeshProUGUI>().fontSize = 12;
            SetAnchors(sPriceGO.GetComponent<RectTransform>(), 0f, 0.7f, 0f, 0.4f, 60f, 2f, -4f, -2f);
            var selBtn = CreateButtonGO(rowGO.transform, "SelectBtn", ">");
            SetAnchors(selBtn.GetComponent<RectTransform>(), 0.7f, 0.98f, 0.15f, 0.85f);
            selBtn.GetComponent<Image>().color = new Color(0.25f, 0.3f, 0.5f);
            var soRow = new SerializedObject(rowComp);
            soRow.FindProperty("_icon").objectReferenceValue = sIconGO.GetComponent<Image>();
            soRow.FindProperty("_nameText").objectReferenceValue = sNameGO.GetComponent<TextMeshProUGUI>();
            soRow.FindProperty("_priceText").objectReferenceValue = sPriceGO.GetComponent<TextMeshProUGUI>();
            soRow.FindProperty("_selectButton").objectReferenceValue = selBtn.GetComponent<Button>();
            soRow.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(view);
            so.FindProperty("_closeButton").objectReferenceValue = closeBtnComp;
            so.FindProperty("_catalogueContainer").objectReferenceValue = catalogue.GetComponent<Transform>();
            so.FindProperty("_rowPrefab").objectReferenceValue = rowComp;
            so.FindProperty("_detailName").objectReferenceValue = detailNameTMP;
            so.FindProperty("_detailDesc").objectReferenceValue = detailDescTMP;
            so.FindProperty("_detailPrice").objectReferenceValue = detailPriceTMP;
            so.FindProperty("_buyButton").objectReferenceValue = buyBtnComp;
            so.FindProperty("_detailRoot").objectReferenceValue = detailRoot;
            so.FindProperty("_feedbackText").objectReferenceValue = feedbackTMP;
            so.FindProperty("_feedbackGroup").objectReferenceValue = feedbackCG;
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_panelRect").objectReferenceValue = rootRT;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ViewsPath + "/Demo2_Shop.prefab");
            Debug.Log("[Maqui] Demo2_Shop.prefab created.");
        }

        // ── Demo 3: FrostedHUD ────────────────────────────────────────────────

        [MenuItem("Maqui/Create View Prefabs/Demo3 - FrostedHUD View", priority = 15)]
        public static void CreateDemo3FrostedHUDView()
        {
            var root = new GameObject("FrostedHUDView");
            SetFullScreen(root.AddComponent<RectTransform>());
            var cg = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<FrostedHUDView>();

            // TopBar
            var topBar = CreatePanel(root.transform, "TopBar", 0f, 1f, 1f, 1f, 0f, -70f, 0f, 0f);
            topBar.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.72f);

            var playerNameGO = CreateTMPGO(topBar.transform, "PlayerName", "ArcaneMage");
            var playerNameTMP = playerNameGO.GetComponent<TextMeshProUGUI>();
            playerNameTMP.fontSize = 18;
            playerNameTMP.fontStyle = FontStyles.Bold;
            playerNameTMP.color = Color.white;
            SetAnchors(playerNameGO.GetComponent<RectTransform>(), 0f, 0.45f, 0f, 1f, 14f, 4f, -4f, -4f);

            var zoneGO = CreateTMPGO(topBar.transform, "ZoneName", "Crimson Highlands");
            var zoneTMP = zoneGO.GetComponent<TextMeshProUGUI>();
            zoneTMP.fontSize = 13;
            zoneTMP.color = new Color(0.8f, 0.8f, 1f);
            zoneTMP.alignment = TextAlignmentOptions.Center;
            SetAnchors(zoneGO.GetComponent<RectTransform>(), 0.3f, 0.7f, 0f, 1f, 4f, 4f, -4f, -4f);

            var notifBtn = CreateButtonGO(topBar.transform, "NotifButton", "Bell");
            SetAnchors(notifBtn.GetComponent<RectTransform>(), 1f, 1f, 0.1f, 0.9f, -60f, 4f, -4f, -4f);
            notifBtn.GetComponent<Image>().color = Color.clear;
            var notifBtnComp = notifBtn.GetComponent<Button>();

            var badgeGO = CreatePanel(notifBtn.transform, "Badge", 1f, 1f, 1f, 1f);
            var badgeRT = badgeGO.GetComponent<RectTransform>();
            badgeRT.anchoredPosition = new Vector2(-6f, -6f);
            badgeRT.sizeDelta = new Vector2(18f, 18f);
            badgeGO.AddComponent<Image>().color = new Color(0.9f, 0.2f, 0.2f);
            badgeGO.SetActive(false);
            var countGO = CreateTMPGO(badgeGO.transform, "Count", "0");
            var countTMP = countGO.GetComponent<TextMeshProUGUI>();
            countTMP.fontSize = 9;
            countTMP.fontStyle = FontStyles.Bold;
            countTMP.color = Color.white;
            countTMP.alignment = TextAlignmentOptions.Center;
            SetAnchors(countGO.GetComponent<RectTransform>(), 0f, 1f, 0f, 1f);

            // XP Bar
            var xpBarGO = new GameObject("XPBar");
            xpBarGO.transform.SetParent(root.transform, false);
            var xpBarRT = xpBarGO.AddComponent<RectTransform>();
            xpBarRT.anchorMin = Vector2.zero;
            xpBarRT.anchorMax = Vector2.zero;
            xpBarRT.pivot = Vector2.zero;
            xpBarRT.anchoredPosition = new Vector2(8f, 8f);
            xpBarRT.sizeDelta = new Vector2(300f, 72f);
            xpBarGO.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.72f);
            var xpVlg = xpBarGO.AddComponent<VerticalLayoutGroup>();
            xpVlg.padding = new RectOffset(10, 10, 8, 8);
            xpVlg.spacing = 4;
            xpVlg.childForceExpandWidth = true;
            xpVlg.childForceExpandHeight = false;
            var xpSlider = CreateStyledSlider(xpBarGO.transform, "XPSlider", new Color(0.4f, 0.2f, 0.9f), 0.62f);
            var xpLabelGO = CreateTMPGO(xpBarGO.transform, "XPLabel", "62%");
            var xpLabelTMP = xpLabelGO.GetComponent<TextMeshProUGUI>();
            xpLabelTMP.fontSize = 12;
            xpLabelTMP.color = new Color(0.8f, 0.8f, 1f);
            xpLabelTMP.alignment = TextAlignmentOptions.Center;
            xpLabelGO.AddComponent<LayoutElement>().preferredHeight = 20f;

            // Settings button
            var settingsBtn = CreateButtonGO(root.transform, "SettingsBtn", "Settings");
            SetAnchors(settingsBtn.GetComponent<RectTransform>(), 1f, 1f, 0f, 0f, -130f, 8f, -8f, 52f);
            settingsBtn.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.72f);
            var settingsBtnComp = settingsBtn.GetComponent<Button>();

            var so = new SerializedObject(view);
            so.FindProperty("_playerName").objectReferenceValue = playerNameTMP;
            so.FindProperty("_zoneName").objectReferenceValue = zoneTMP;
            so.FindProperty("_notifButton").objectReferenceValue = notifBtnComp;
            so.FindProperty("_notifBadge").objectReferenceValue = badgeGO;
            so.FindProperty("_notifCount").objectReferenceValue = countTMP;
            so.FindProperty("_xpSlider").objectReferenceValue = xpSlider;
            so.FindProperty("_xpLabel").objectReferenceValue = xpLabelTMP;
            so.FindProperty("_settingsButton").objectReferenceValue = settingsBtnComp;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ViewsPath + "/Demo3_FrostedHUD.prefab");
            Debug.Log("[Maqui] Demo3_FrostedHUD.prefab created.");
        }

        // ── Demo 3: Notifications ─────────────────────────────────────────────

        [MenuItem("Maqui/Create View Prefabs/Demo3 - Notifications View", priority = 16)]
        public static void CreateDemo3NotificationsView()
        {
            var root = new GameObject("NotificationView");
            var rootRT = root.AddComponent<RectTransform>();
            SetCenteredModal(rootRT, 320f, 460f);
            var cg = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<NotificationView>();
            root.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.14f, 0.95f);

            var headerGO = CreateTMPGO(root.transform, "Header", "Notifications");
            var headerTMP = headerGO.GetComponent<TextMeshProUGUI>();
            headerTMP.fontSize = 18;
            headerTMP.fontStyle = FontStyles.Bold;
            headerTMP.color = Color.white;
            headerTMP.alignment = TextAlignmentOptions.MidlineLeft;
            SetAnchors(headerGO.GetComponent<RectTransform>(), 0f, 1f, 1f, 1f, 16f, -56f, -96f, 0f);

            var closeBtn = CreateButtonGO(root.transform, "CloseBtn", "X");
            SetAnchors(closeBtn.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -44f, -44f, -8f, -8f);
            var closeBtnComp = closeBtn.GetComponent<Button>();

            var clearBtn = CreateButtonGO(root.transform, "ClearBtn", "Clear");
            clearBtn.GetComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f);
            SetAnchors(clearBtn.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -92f, -44f, -52f, -8f);
            var clearBtnComp = clearBtn.GetComponent<Button>();

            // List
            var scrollGO = CreatePanel(root.transform, "ScrollView", 0f, 1f, 0f, 1f, 8f, 8f, -8f, -64f);
            var sr = scrollGO.AddComponent<ScrollRect>();
            sr.horizontal = false;
            var vp = CreatePanel(scrollGO.transform, "Viewport", 0f, 1f, 0f, 1f);
            vp.AddComponent<RectMask2D>();
            var listGO = CreatePanel(vp.transform, "List", 0f, 1f, 1f, 1f);
            var listRT = listGO.GetComponent<RectTransform>();
            listRT.pivot = new Vector2(0.5f, 1f);
            var vlg = listGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            listGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp.GetComponent<RectTransform>();
            sr.content = listRT;

            // NotifRow template
            var rowGO = new GameObject("NotifRow_Prefab");
            rowGO.transform.SetParent(root.transform, false);
            rowGO.AddComponent<RectTransform>();
            rowGO.SetActive(false);
            rowGO.AddComponent<Image>().color = new Color(0.15f, 0.13f, 0.22f);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 72f;
            var rowComp = rowGO.AddComponent<NotifRow>();
            var rIconGO = CreateTMPGO(rowGO.transform, "Icon", "!");
            rIconGO.GetComponent<TextMeshProUGUI>().fontSize = 20;
            rIconGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            SetAnchors(rIconGO.GetComponent<RectTransform>(), 0f, 0f, 0f, 1f, 4f, 4f, 36f, -4f);
            var rTitleGO = CreateTMPGO(rowGO.transform, "Title", "Notification");
            rTitleGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            rTitleGO.GetComponent<TextMeshProUGUI>().fontSize = 13;
            SetAnchors(rTitleGO.GetComponent<RectTransform>(), 0f, 0.75f, 0.5f, 1f, 42f, 2f, -4f, -4f);
            var rBodyGO = CreateTMPGO(rowGO.transform, "Body", "Notification body text.");
            rBodyGO.GetComponent<TextMeshProUGUI>().fontSize = 11;
            rBodyGO.GetComponent<TextMeshProUGUI>().color = new Color(0.75f, 0.75f, 0.85f);
            SetAnchors(rBodyGO.GetComponent<RectTransform>(), 0f, 0.75f, 0f, 0.5f, 42f, 2f, -4f, -2f);
            var rTimeGO = CreateTMPGO(rowGO.transform, "Time", "2m ago");
            rTimeGO.GetComponent<TextMeshProUGUI>().fontSize = 10;
            rTimeGO.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.6f, 0.7f);
            rTimeGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
            SetAnchors(rTimeGO.GetComponent<RectTransform>(), 0.75f, 1f, 0.3f, 0.7f, 4f, 0f, -8f, 0f);
            var soRow = new SerializedObject(rowComp);
            soRow.FindProperty("_icon").objectReferenceValue = rIconGO.GetComponent<TextMeshProUGUI>();
            soRow.FindProperty("_title").objectReferenceValue = rTitleGO.GetComponent<TextMeshProUGUI>();
            soRow.FindProperty("_body").objectReferenceValue = rBodyGO.GetComponent<TextMeshProUGUI>();
            soRow.FindProperty("_time").objectReferenceValue = rTimeGO.GetComponent<TextMeshProUGUI>();
            soRow.ApplyModifiedPropertiesWithoutUndo();

            var so = new SerializedObject(view);
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_panelRect").objectReferenceValue = rootRT;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtnComp;
            so.FindProperty("_clearButton").objectReferenceValue = clearBtnComp;
            so.FindProperty("_container").objectReferenceValue = listGO.GetComponent<Transform>();
            so.FindProperty("_rowPrefab").objectReferenceValue = rowComp;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ViewsPath + "/Demo3_Notifications.prefab");
            Debug.Log("[Maqui] Demo3_Notifications.prefab created.");
        }

        // ── Demo 3: Settings ──────────────────────────────────────────────────

        [MenuItem("Maqui/Create View Prefabs/Demo3 - Settings View", priority = 17)]
        public static void CreateDemo3SettingsView()
        {
            var root = new GameObject("SettingsView");
            var rootRT = root.AddComponent<RectTransform>();
            // Bottom sheet: full width, 420px tall, anchored bottom
            rootRT.anchorMin = new Vector2(0f, 0f);
            rootRT.anchorMax = new Vector2(1f, 0f);
            rootRT.pivot = new Vector2(0.5f, 0f);
            rootRT.anchoredPosition = Vector2.zero;
            rootRT.sizeDelta = new Vector2(0f, 420f);
            var cg = root.AddComponent<CanvasGroup>();
            var view = root.AddComponent<SettingsView>();
            root.AddComponent<Image>().color = new Color(0.07f, 0.06f, 0.14f, 0.97f);

            // Handle (decorative pill)
            var handle = CreatePanel(root.transform, "Handle", 0.35f, 0.65f, 1f, 1f, 0f, -20f, 0f, -12f);
            handle.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.5f);

            var headerGO = CreateTMPGO(root.transform, "Header", "Settings");
            headerGO.GetComponent<TextMeshProUGUI>().fontSize = 20;
            headerGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            headerGO.GetComponent<TextMeshProUGUI>().color = Color.white;
            SetAnchors(headerGO.GetComponent<RectTransform>(), 0f, 0.7f, 1f, 1f, 16f, -68f, 0f, -24f);

            var closeBtn = CreateButtonGO(root.transform, "CloseBtn", "X");
            SetAnchors(closeBtn.GetComponent<RectTransform>(), 1f, 1f, 1f, 1f, -44f, -68f, -8f, -28f);
            var closeBtnComp = closeBtn.GetComponent<Button>();

            // Volume rows
            var masterSlider = CreateLabelSliderRow(root.transform, "MasterVol", "Master Volume", 0.8f,
                new Vector2(8f, 280f), out var masterLabelTMP);
            var musicSlider = CreateLabelSliderRow(root.transform, "MusicVol", "Music Volume", 0.6f,
                new Vector2(8f, 200f), out var musicLabelTMP);

            // Toggle rows
            var fullscreenToggle = CreateToggleRow(root.transform, "Fullscreen", "Fullscreen", new Vector2(8f, 140f), true);
            var fpsToggle = CreateToggleRow(root.transform, "ShowFPS", "Show FPS", new Vector2(8f, 90f), false);

            var so = new SerializedObject(view);
            so.FindProperty("_canvasGroup").objectReferenceValue = cg;
            so.FindProperty("_panelRect").objectReferenceValue = rootRT;
            so.FindProperty("_closeButton").objectReferenceValue = closeBtnComp;
            so.FindProperty("_masterSlider").objectReferenceValue = masterSlider;
            so.FindProperty("_masterLabel").objectReferenceValue = masterLabelTMP;
            so.FindProperty("_musicSlider").objectReferenceValue = musicSlider;
            so.FindProperty("_musicLabel").objectReferenceValue = musicLabelTMP;
            so.FindProperty("_fullscreenToggle").objectReferenceValue = fullscreenToggle;
            so.FindProperty("_fpsToggle").objectReferenceValue = fpsToggle;
            so.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, ViewsPath + "/Demo3_Settings.prefab");
            Debug.Log("[Maqui] Demo3_Settings.prefab created.");
        }

        // ── Helpers: Specialised rows ─────────────────────────────────────────

        static Slider CreateLabelSliderRow(Transform parent, string name, string label, float value,
            Vector2 pos, out TextMeshProUGUI pctLabel)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 0f);
            rowRT.anchorMax = new Vector2(1f, 0f);
            rowRT.pivot = new Vector2(0.5f, 0f);
            rowRT.anchoredPosition = pos;
            rowRT.sizeDelta = new Vector2(-16f, 56f);

            var lblGO = CreateTMPGO(row.transform, "Label", label);
            lblGO.GetComponent<TextMeshProUGUI>().fontSize = 14;
            lblGO.GetComponent<TextMeshProUGUI>().color = Color.white;
            SetAnchors(lblGO.GetComponent<RectTransform>(), 0f, 0.6f, 0.5f, 1f, 0f, 2f, 0f, -2f);

            var pctGO = CreateTMPGO(row.transform, "PctLabel", Mathf.RoundToInt(value * 100) + "%");
            pctLabel = pctGO.GetComponent<TextMeshProUGUI>();
            pctLabel.fontSize = 13;
            pctLabel.color = new Color(0.7f, 0.7f, 0.9f);
            pctLabel.alignment = TextAlignmentOptions.MidlineRight;
            SetAnchors(pctGO.GetComponent<RectTransform>(), 0.6f, 1f, 0.5f, 1f, 0f, 2f, 0f, -2f);

            var slider = CreateStyledSlider(row.transform, "Slider", new Color(0.4f, 0.4f, 0.9f), value);
            var sliderLE = slider.GetComponent<LayoutElement>() ?? slider.gameObject.AddComponent<LayoutElement>();
            SetAnchors(slider.GetComponent<RectTransform>(), 0f, 1f, 0f, 0.5f, 0f, 4f, 0f, -4f);

            return slider;
        }

        static Toggle CreateToggleRow(Transform parent, string name, string label, Vector2 pos, bool isOn)
        {
            var row = new GameObject(name);
            row.transform.SetParent(parent, false);
            var rowRT = row.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 0f);
            rowRT.anchorMax = new Vector2(1f, 0f);
            rowRT.pivot = new Vector2(0.5f, 0f);
            rowRT.anchoredPosition = pos;
            rowRT.sizeDelta = new Vector2(-16f, 44f);

            var lbl = CreateTMPGO(row.transform, "Label", label);
            lbl.GetComponent<TextMeshProUGUI>().fontSize = 14;
            lbl.GetComponent<TextMeshProUGUI>().color = Color.white;
            SetAnchors(lbl.GetComponent<RectTransform>(), 0f, 0.7f, 0f, 1f, 0f, 2f, 0f, -2f);

            var tGO = CreatePanel(row.transform, "Toggle", 1f, 1f, 0.15f, 0.85f, -56f, 0f, -4f, 0f);
            var toggle = tGO.AddComponent<Toggle>();
            var tBg = tGO.AddComponent<Image>();
            tBg.color = isOn ? new Color(0.2f, 0.5f, 0.9f) : new Color(0.3f, 0.3f, 0.4f);
            var ck = CreatePanel(tGO.transform, "Checkmark", 0.1f, 0.9f, 0.1f, 0.9f);
            var ckImg = ck.AddComponent<Image>();
            ckImg.color = Color.white;
            toggle.targetGraphic = tBg;
            toggle.graphic = ckImg;
            toggle.isOn = isOn;
            return toggle;
        }

        // ── Helpers: UI primitives ────────────────────────────────────────────

        static GameObject CreatePanel(Transform parent, string name,
            float xMin = 0f, float xMax = 1f, float yMin = 0f, float yMax = 1f,
            float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            SetAnchors(go.GetComponent<RectTransform>(), xMin, xMax, yMin, yMax, left, bottom, right, top);
            return go;
        }

        static GameObject CreateTMPGO(Transform parent, string name, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            return go;
        }

        static GameObject CreateButtonGO(Transform parent, string name, string label)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.25f, 0.25f, 0.35f);
            go.AddComponent<Button>();
            var lblGO = new GameObject("Label", typeof(RectTransform));
            lblGO.transform.SetParent(go.transform, false);
            var rt = lblGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = lblGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return go;
        }

        static Slider CreateStyledSlider(Transform parent, string name, Color fillColor, float value)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 24f;
            var slider = go.AddComponent<Slider>();
            var bgGO = CreatePanel(go.transform, "Background", 0f, 1f, 0.25f, 0.75f);
            bgGO.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.3f);
            var fillAreaGO = CreatePanel(go.transform, "Fill Area", 0f, 1f, 0.25f, 0.75f, 5f, 0f, -5f, 0f);
            var fillGO = CreatePanel(fillAreaGO.transform, "Fill", 0f, value, 0f, 1f);
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = fillColor;
            slider.fillRect = fillGO.GetComponent<RectTransform>();
            slider.targetGraphic = fillImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            return slider;
        }

        // ── Helpers: Layout ───────────────────────────────────────────────────

        static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetCenteredModal(RectTransform rt, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void SetAnchors(RectTransform rt,
            float xMin, float xMax, float yMin, float yMax,
            float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(right, top);
        }

        // ── Helpers: SerializeField ───────────────────────────────────────────

        static void SetObjArrayRef<T>(SerializedObject so, string propName, T[] items) where T : Object
        {
            var arr = so.FindProperty(propName);
            if (arr == null) { Debug.LogWarning("[Maqui] Property '" + propName + "' not found"); return; }
            arr.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        static void SavePrefab(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}

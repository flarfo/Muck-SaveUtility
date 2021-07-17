using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SaveUtility
{
    [HarmonyPatch]
    class UIManager
    {
        private static bool uiUpdated = false;
        private static bool pauseUIUpdated = false;
        private static bool canSave = true;
        private static bool serverHost = false;

        private static float time = 0.0f;

        public static Texture backgroundImage;

        internal static GameObject selectionGUI;
        internal static GameObject buttonExtraGUI;
        internal static GameObject verifyDelete;
        internal static GameObject backgroundDarkGUI;

        internal static Dictionary<string, SaveButton> saveButtons = new Dictionary<string, SaveButton>();

        internal static string curEditSave;

        public static bool useAutoSave = false;

        [HarmonyPatch(typeof(MenuUI), "JoinLobby")]
        static void Postfix(MenuUI __instance)
        {
            if (!uiUpdated && serverHost)
            {
                LoadManager.worldSaves = SaveSystem.GetAllSaves();
                Vector2 uiSize = new Vector2(500, 750);

                //gets lobby UI parent
                GameObject.Find("LobbyDetails").GetComponent<RectTransform>().sizeDelta = new Vector2(250, 408);

                //creates selection UI parent with size/layer/transform
                selectionGUI = new GameObject("selectionGUI", new[] { typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(RawImage) });

                selectionGUI.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                selectionGUI.layer = 5;
                selectionGUI.transform.parent = __instance.lobbyUi.transform;

                //sets UI background image
                selectionGUI.GetComponent<RectTransform>().sizeDelta = uiSize;
                selectionGUI.GetComponent<RawImage>().texture = backgroundImage;
                selectionGUI.GetComponent<RawImage>().color = new Color32(137, 104, 61, 255);

                //maybe only make dark when delete button is clicked, "Are you sure?" "Delete" "Cancel"
                
                //creates extra button UI for when button is right clicked
                //maybe reposition pop-up GUI for deleting/rename to be to the side of normal GUI
                buttonExtraGUI = new GameObject("buttonExtraGUI", new[] { typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(RawImage), typeof(VerticalLayoutGroup) });

                buttonExtraGUI.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                buttonExtraGUI.layer = 5;
                buttonExtraGUI.transform.parent = __instance.lobbyUi.transform;

                buttonExtraGUI.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 250);
                buttonExtraGUI.GetComponent<RawImage>().texture = backgroundImage;
                buttonExtraGUI.GetComponent<RawImage>().color = new Color32(137, 104, 61, 255);

                //FOR RENAMINING, LOOK AT JOIN BUTTON CHILDREN

                buttonExtraGUI.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(15, 15, 15, 15);
                buttonExtraGUI.GetComponent<VerticalLayoutGroup>().spacing = 2;
                buttonExtraGUI.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                buttonExtraGUI.GetComponent<VerticalLayoutGroup>().childControlWidth = true;

                GameObject saveText = new GameObject("saveText", new[] { typeof(TextMeshProUGUI) } );
                saveText.transform.parent = buttonExtraGUI.transform;
                saveText.GetComponent<TextMeshProUGUI>().fontSize = 35;
                saveText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

                saveText.GetComponent<TextMeshProUGUI>().color = new Color(0, 0, 0, 1);
                saveText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

                Button deleteButton = UnityEngine.Object.Instantiate(__instance.startBtn, buttonExtraGUI.transform).GetComponent<Button>();
                deleteButton.onClick = new Button.ButtonClickedEvent();
                deleteButton.onClick.AddListener(delegate ()
                {
                    verifyDelete.SetActive(true);
                    backgroundDarkGUI.SetActive(true);
                });
                deleteButton.GetComponentInChildren<TextMeshProUGUI>().text = "Delete";

                Button closeButton = UnityEngine.Object.Instantiate(__instance.startBtn, buttonExtraGUI.transform).GetComponent<Button>();
                closeButton.onClick = new Button.ButtonClickedEvent();
                closeButton.onClick.AddListener(delegate ()
                {
                    buttonExtraGUI.SetActive(false);
                });
                closeButton.GetComponentInChildren<TextMeshProUGUI>().text = "Close";

                /*GameObject rename = new GameObject("rename", new[] { typeof(RectTransform), 
                    typeof(CanvasRenderer), typeof(VerticalLayoutGroup) });

                rename.transform.parent = buttonExtraGUI.transform;

                Button renameButton = UnityEngine.Object.Instantiate(__instance.startBtn, rename.transform).GetComponent<Button>();

                renameButton.onClick = new Button.ButtonClickedEvent();
                renameButton.onClick.AddListener(delegate ()
                {
                    Debug.Log("Save Renamed!");

                    //create text input field
                    //saveButtons[curEditSave].UpdateText(text);

                    buttonExtraGUI.SetActive(false);
                    backgroundDarkGUI.SetActive(false);
                });
                renameButton.GetComponentInChildren<TextMeshProUGUI>().text = "Rename";

                GameObject renameInput = new GameObject("renameInput", new[] { typeof(RectTransform), 
                    typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField) });

                renameInput.transform.parent = rename.transform;

                GameObject textArea = new GameObject("textAra", new[] { typeof(RectTransform), typeof(RectMask2D) });

                textArea.transform.parent = renameInput.transform;*/

                buttonExtraGUI.SetActive(false);

                backgroundDarkGUI = new GameObject("backgroundDark", new[] { typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(RawImage) });

                backgroundDarkGUI.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                backgroundDarkGUI.GetComponent<RectTransform>().sizeDelta = new Vector2(Screen.width, Screen.height);

                backgroundDarkGUI.GetComponent<RawImage>().color = new Color32(0, 0, 0, 150);
                backgroundDarkGUI.layer = 5;
                backgroundDarkGUI.transform.parent = __instance.lobbyUi.transform;

                backgroundDarkGUI.SetActive(false);

                verifyDelete = new GameObject("verifyDelete", new[] { typeof(RectTransform),
                    typeof(CanvasRenderer), typeof(RawImage), typeof(VerticalLayoutGroup) });

                verifyDelete.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                verifyDelete.layer = 5;
                verifyDelete.transform.parent = __instance.lobbyUi.transform;

                verifyDelete.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 250);
                verifyDelete.GetComponent<RawImage>().texture = backgroundImage;
                verifyDelete.GetComponent<RawImage>().color = new Color32(137, 104, 61, 255);

                verifyDelete.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(15, 15, 15, 15);
                verifyDelete.GetComponent<VerticalLayoutGroup>().spacing = 2;
                verifyDelete.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                verifyDelete.GetComponent<VerticalLayoutGroup>().childControlWidth = true;

                GameObject deleteText = new GameObject("deleteText", new[] { typeof(TextMeshProUGUI) });
                deleteText.transform.parent = verifyDelete.transform;
                deleteText.GetComponent<TextMeshProUGUI>().text = "Are you sure?";
                deleteText.GetComponent<TextMeshProUGUI>().fontSize = 35;
                deleteText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

                deleteText.GetComponent<TextMeshProUGUI>().color = new Color(0, 0, 0, 1);
                deleteText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

                Button yesButton = UnityEngine.Object.Instantiate(__instance.startBtn, verifyDelete.transform).GetComponent<Button>();
                yesButton.onClick = new Button.ButtonClickedEvent();
                yesButton.onClick.AddListener(delegate ()
                {
                    Debug.Log("Save Deleted!");

                    if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", curEditSave)))
                    {
                        File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Saves", curEditSave));
                    }

                    GameObject.Destroy(saveButtons[curEditSave].gameObject);
                    saveButtons.Remove(curEditSave);

                    if (LoadManager.currentSave == curEditSave)
                    {
                        LoadManager.currentSave = "";
                        LoadManager.hasSaveLoaded = false;
                    }

                    buttonExtraGUI.SetActive(false);
                    backgroundDarkGUI.SetActive(false);
                    verifyDelete.SetActive(false);
                });
                yesButton.GetComponentInChildren<TextMeshProUGUI>().text = "Yes";

                Button noButton = UnityEngine.Object.Instantiate(__instance.startBtn, verifyDelete.transform).GetComponent<Button>();
                noButton.onClick = new Button.ButtonClickedEvent();
                noButton.onClick.AddListener(delegate ()
                {
                    buttonExtraGUI.SetActive(false);
                    backgroundDarkGUI.SetActive(false);
                    verifyDelete.SetActive(false);
                });
                noButton.GetComponentInChildren<TextMeshProUGUI>().text = "No";

                verifyDelete.SetActive(false);

                //creates mask object, stops scroll from being fully rendered
                GameObject maskRect = new GameObject("maskRect", new[] { typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(RawImage), typeof(Mask) });

                maskRect.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                maskRect.transform.parent = selectionGUI.transform;

                maskRect.GetComponent<Mask>().rectTransform.sizeDelta = uiSize;
                maskRect.GetComponent<Mask>().showMaskGraphic = false;

                //creates scroll object with scrollrect
                GameObject scrollGUI = new GameObject("scrollGUI", new[] { typeof(RectTransform), typeof(CanvasRenderer), 
                    typeof(ScrollRect) });

                scrollGUI.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                scrollGUI.transform.parent = maskRect.transform;

                scrollGUI.GetComponent<ScrollRect>().GetComponent<RectTransform>().sizeDelta = uiSize;
                scrollGUI.GetComponent<ScrollRect>().horizontal = false;
                scrollGUI.GetComponent<ScrollRect>().movementType = ScrollRect.MovementType.Elastic;

                //creates scrollContent (array of buttons parented to scrollGUI)
                GameObject scrollContent = new GameObject("scrollContent", new[] { typeof(RectTransform), typeof(CanvasRenderer), 
                    typeof(VerticalLayoutGroup)});

                scrollContent.GetComponent<RectTransform>().position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
                scrollContent.transform.parent = scrollGUI.transform;

                scrollContent.GetComponent<RectTransform>().sizeDelta = uiSize;
                scrollContent.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(15, 15, 15, 15);
                scrollContent.GetComponent<VerticalLayoutGroup>().spacing = 2;
                scrollContent.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
                scrollContent.GetComponent<VerticalLayoutGroup>().childControlWidth = true;

                //updates scrollGUI content variables
                scrollGUI.GetComponent<ScrollRect>().content = scrollContent.GetComponent<RectTransform>();

                //iterate through worldsaves, create new button for each world save
                if (LoadManager.worldSaves.Length > 0)
                {
                    for (int i = 0; i < LoadManager.worldSaves.Length; i++)
                    {
                        if (i > 8)
                        {
                            //extends scrollcontent UI for each button above 8 so as to not compress buttons together
                            scrollContent.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 750+(i*30));
                        }

                        //create a new world save button 
                        Button newButton = UnityEngine.Object.Instantiate(__instance.startBtn, scrollContent.transform).GetComponent<Button>();
                        newButton.onClick = new Button.ButtonClickedEvent();
                        newButton.gameObject.AddComponent<SaveButton>();
                        newButton.GetComponentInChildren<TextMeshProUGUI>().text = LoadManager.worldSaves[i];
                    }

                    //creates none button that allows players to deselect saves
                    Button noneButton = UnityEngine.Object.Instantiate(__instance.startBtn, scrollContent.transform
                            .transform).GetComponent<Button>();
                    noneButton.GetComponentInChildren<TextMeshProUGUI>().text = "None";

                    noneButton.onClick = new Button.ButtonClickedEvent();
                    noneButton.onClick.AddListener(delegate ()
                    {
                        LoadManager.currentSave = "";
                        LoadManager.hasSaveLoaded = false;
                        selectionGUI.SetActive(false);
                    });
                }
                else
                {
                    //if there are no saves, creates no saves button
                    Button noSaveButton = UnityEngine.Object.Instantiate(__instance.startBtn, scrollContent
                            .transform).GetComponent<Button>();
                    noSaveButton.GetComponentInChildren<TextMeshProUGUI>().text = "No Saves :(";

                    noSaveButton.onClick = new Button.ButtonClickedEvent();
                    noSaveButton.onClick.AddListener(delegate ()
                    {
                        selectionGUI.SetActive(false);
                    });
                }

                selectionGUI.SetActive(false);

                //adds load save button, which displays the selection GUI when clicked
                Button component = UnityEngine.Object.Instantiate(__instance.startBtn,
                    GameObject.Find("LobbyDetails").transform).GetComponent<Button>();

                component.GetComponentInChildren<TextMeshProUGUI>().text = "Load Save";

                component.onClick = new Button.ButtonClickedEvent();
                component.onClick.AddListener(delegate ()
                {
                    Debug.Log("LOAD SAVE BUTTON CLICKED");

                    selectionGUI.SetActive(!selectionGUI.activeSelf);
                });

                uiUpdated = true;
            }
        }
        
        [HarmonyPatch(typeof(OtherInput), "Pause")]
        [HarmonyPostfix]
        static void UpdatePauseUI()
        {
            if (!pauseUIUpdated)
            {
                Button saveButton = UnityEngine.Object.Instantiate(GameObject.Find("ResumeBtn"), GameObject.Find("PauseUI").transform).GetComponent<Button>();

                saveButton.gameObject.transform.SetSiblingIndex(2);

                saveButton.GetComponentInChildren<TextMeshProUGUI>().text = "Save";
                saveButton.onClick = new Button.ButtonClickedEvent();
                saveButton.onClick.AddListener(delegate ()
                {
                    Debug.Log("SAVE BUTTON CLICKED");

                    if (canSave)
                    {
                        World.Save();
                        canSave = false;
                        time = 0f;
                    }
                });

                pauseUIUpdated = true;
            }
            
        }
        
        [HarmonyPatch(typeof(OtherInput), "Update")]
        [HarmonyPostfix]
        static void SaveTimer()
        {
            time += Time.deltaTime;

            if (time >= 60f)
            {
                canSave = true;
            }
        }

        [HarmonyPatch(typeof(MenuUI), "StartLobby")]
        [HarmonyPrefix]
        static void SetHost()
        {
            serverHost = true;
        }

        [HarmonyPatch(typeof(MenuUI), "Start")]
        static void Prefix()
        {
            saveButtons.Clear();
            canSave = true;
            time = 0f;
            pauseUIUpdated = false;
            uiUpdated = false;
            serverHost = false;
        }

        [HarmonyPatch(typeof(Settings), "Start")]
        [HarmonyPostfix]
        static void UpdateSettings(Settings __instance)
        {
            if (PlayerPrefs.HasKey("autosave"))
            {
                useAutoSave = Convert.ToBoolean(PlayerPrefs.GetInt("autosave"));
            }

            GameObject autoSave = GameObject.Instantiate(__instance.tutorial.gameObject, __instance.tutorial.transform.parent);
            autoSave.GetComponentInChildren<TextMeshProUGUI>().text = "Auto Save";

            autoSave.GetComponentInChildren<MyBoolSetting>().SetSetting(useAutoSave);

            autoSave.GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            autoSave.GetComponentInChildren<Button>().onClick.AddListener(delegate ()
            {
                useAutoSave = !useAutoSave;
                autoSave.GetComponentInChildren<MyBoolSetting>().SetSetting(useAutoSave);
                PlayerPrefs.SetInt("autosave", Convert.ToInt32(useAutoSave));
            });

        }
    }

    public class SaveButton : MonoBehaviour, IPointerClickHandler
    {
        void Start()
        {
            UIManager.saveButtons.Add(GetComponentInChildren<TextMeshProUGUI>().text, this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                LoadManager.currentSave = GetComponentInChildren<TextMeshProUGUI>().text;
                LoadManager.hasSaveLoaded = true;
                UIManager.selectionGUI.SetActive(false);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                UIManager.buttonExtraGUI.transform.position = new Vector3(eventData.position.x+200, eventData.position.y-125);
                UIManager.buttonExtraGUI.SetActive(true);

                UIManager.curEditSave = GetComponentInChildren<TextMeshProUGUI>().text;

                if (UIManager.curEditSave.Length > 12)
                {
                    UIManager.buttonExtraGUI.GetComponentInChildren<TextMeshProUGUI>().text = GetComponentInChildren<TextMeshProUGUI>().text.Substring(0,12) + "...";
                }
                else
                {
                    UIManager.buttonExtraGUI.GetComponentInChildren<TextMeshProUGUI>().text = GetComponentInChildren<TextMeshProUGUI>().text;
                }
                
            }
        }

        public void UpdateText(string text)
        {
            GetComponentInChildren<TextMeshProUGUI>().text = text;
        }
    }
}

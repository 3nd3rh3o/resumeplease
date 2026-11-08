using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class GameMenuUI : MonoBehaviour
{
    private enum Page { Hidden, Main, Pause, Saves, Load, Options, Loading, Ending }
    private GameSession session;
    private GameObject canvasObject;
    private RectTransform content;
    private Text heading, status;
    private Page page;
    private InputAction pause;
    private bool ownsPause;
    private bool pauseActive;
    private float oldTimeScale;
    private CursorLockMode oldCursorLock;
    private bool oldCursorVisible;
    private InputActionRebindingExtensions.RebindingOperation rebinding;
    private InputAction rebindAction;
    private bool rebindWasEnabled;
    private int ignorePauseThroughFrame;
    private string originalBindings;
    private GameOptionsData draft;
    private readonly List<Resolution> resolutions = new List<Resolution>();
    private DayReport endingReport;
    public bool IsOpen => page != Page.Hidden;
    public bool IsEnding => page == Page.Ending;
    public bool IsRebinding => rebinding != null;

    public void Initialize(GameSession owner)
    {
        session = owner;
        pause = session.Catalog.inputActions.FindAction("Player/Pause", false);
        if (pause == null) { pause = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape"); ownsPause = true; }
        pause.Enable();
        BuildView();
        canvasObject.SetActive(false);
    }

    private void Update()
    {
        if (rebinding != null || Time.frameCount <= ignorePauseThroughFrame || page == Page.Loading || page == Page.Main || page == Page.Ending || !pause.WasPressedThisFrame()) return;
        if (page == Page.Hidden)
        {
            var scenario = FindAnyObjectByType<ScenarioController>();
            if (scenario != null && scenario.CanSave) ShowPause();
        }
        else if (page == Page.Pause) Resume();
        else Back();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        var events = GetComponentInChildren<EventSystem>(true);
        if (events == null)
        {
            var obj = new GameObject("Menu Input", typeof(EventSystem)); obj.transform.SetParent(transform, false);
            obj.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
    }

    private void BuildView()
    {
        canvasObject = new GameObject("Game Menus", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 30000;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;
        var background = new GameObject("Background", typeof(RectTransform), typeof(Image)); background.transform.SetParent(canvasObject.transform, false);
        MenuWidgets.Stretch(background.GetComponent<RectTransform>()); background.GetComponent<Image>().color = new Color(.035f,.05f,.075f,.98f);
        heading = MenuWidgets.Label(canvasObject.transform, "Resume, Please !", 34);
        SetBounds(heading.rectTransform, new Vector2(.18f,.86f), new Vector2(.82f,.96f));
        var scrollObject = new GameObject("Menu Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(canvasObject.transform, false);
        SetBounds(scrollObject.GetComponent<RectTransform>(), new Vector2(.23f,.16f), new Vector2(.77f,.84f));
        scrollObject.GetComponent<Image>().color = new Color(.07f,.095f,.13f);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D)); viewport.transform.SetParent(scrollObject.transform, false);
        MenuWidgets.Stretch(viewport.GetComponent<RectTransform>());
        var items = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)); items.transform.SetParent(viewport.transform, false);
        content = items.GetComponent<RectTransform>(); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
        var layout = items.GetComponent<VerticalLayoutGroup>(); layout.padding = new RectOffset(16,16,12,12); layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true; layout.childForceExpandHeight = false;
        items.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollObject.GetComponent<ScrollRect>(); scroll.content = content; scroll.viewport = viewport.GetComponent<RectTransform>(); scroll.horizontal = false; scroll.scrollSensitivity = 30;
        status = MenuWidgets.Label(canvasObject.transform, "", 18); SetBounds(status.rectTransform, new Vector2(.18f,.02f), new Vector2(.82f,.14f));
    }

    private static void SetBounds(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero; }

    private void Screen(Page next, string title)
    {
        foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        page = next; heading.text = title; status.text = "";
        canvasObject.SetActive(true); EnsureEventSystem();
        content.anchoredPosition = Vector2.zero;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void ShowMain()
    {
        PrepareNavigation();
        Screen(Page.Main,"Resume, Please !");
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        MenuWidgets.Button(content,"Nouvelle partie",session.NewGame);
        SaveSlot latest=null;
        try { latest=session.Latest(); } catch (Exception error) { status.text=error.Message; }
        var continued=MenuWidgets.Button(content,latest==null?"Continuer — aucune sauvegarde":"Continuer — "+latest.data.dayTitle,()=>session.Load(latest.id));
        continued.interactable=latest!=null;
        MenuWidgets.Button(content,"Charger une sauvegarde",ShowLoad);
        MenuWidgets.Button(content,"Options",()=>ShowOptions(true));
        MenuWidgets.Button(content,"Quitter",session.Quit);
    }

    private void BeginPause()
    {
        if (pauseActive) return;
        oldTimeScale=Time.timeScale;oldCursorLock=Cursor.lockState;oldCursorVisible=Cursor.visible;
        Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;pauseActive=true;
    }
    public void ShowPause()
    {
        if (endingReport != null) { ShowEnding(endingReport); return; }
        BeginPause(); Screen(Page.Pause,"Pause");
        MenuWidgets.Button(content,"Reprendre",Resume);
        MenuWidgets.Button(content,"Sauvegarder",ShowSave);
        MenuWidgets.Button(content,"Charger une sauvegarde",ShowLoad);
        MenuWidgets.Button(content,"Options",()=>ShowOptions(true));
        MenuWidgets.Button(content,"Menu principal",session.MainMenu);
        MenuWidgets.Button(content,"Quitter",session.Quit);
    }

    public void Resume()
    {
        if (endingReport != null) { ShowEnding(endingReport); return; }
        CancelOptions();
        if(pauseActive) {Time.timeScale=oldTimeScale;Cursor.lockState=oldCursorLock;Cursor.visible=oldCursorVisible;pauseActive=false;}
        page=Page.Hidden;canvasObject.SetActive(false);
    }
    public void PrepareNavigation()
    {
        endingReport = null;
        CancelOptions();
        if(pauseActive) {Time.timeScale=oldTimeScale;pauseActive=false;}
        Screen(Page.Loading,"Chargement…");
    }
    public void HideLoading()
    {
        if(page!=Page.Loading) return;
        page=Page.Hidden;canvasObject.SetActive(false);
    }
    private void Back()
    {
        CancelOptions();
        if(endingReport != null) ShowEnding(endingReport);
        else if(pauseActive) ShowPause(); else ShowMain();
    }

    public void ShowEnding(DayReport report)
    {
        if (report == null || !report.EndsCampaign) return;
        endingReport = report;
        BeginPause();
        Screen(Page.Ending, report.EndingTitle);
        MenuWidgets.Label(content, report.Ending == CampaignEnding.Finale ? "FIN DE LA CAMPAGNE" : "FIN PRÉMATURÉE", 24);
        MenuWidgets.Label(content, report.EndingMessage);
        MenuWidgets.Label(content, report.ToDisplayText(), 18);
        MenuWidgets.Button(content, "Charger une sauvegarde antérieure", ShowLoad);
        MenuWidgets.Button(content, "Nouvelle partie", session.NewGame);
        MenuWidgets.Button(content, "Menu principal", session.MainMenu);
        MenuWidgets.Button(content, "Quitter", session.Quit);
    }

    public void ShowError(string message)
    {
        if(!IsOpen) ShowPause();
        status.text=message;
    }
    private void ShowSave()
    {
        Screen(Page.Saves,"Sauvegarder la partie");
        MenuWidgets.Label(content,"Une nouvelle sauvegarde sera créée. Les autres emplacements sont conservés.");
        var name=MenuWidgets.Input(content,FindAnyObjectByType<ScenarioController>().DayDefinition.title);
        MenuWidgets.Button(content,"Créer la sauvegarde",()=>{if(session.SaveManual(name.text))status.text="Sauvegarde créée.";});
        MenuWidgets.Button(content,"Retour",Back);
    }
    private void ShowLoad()
    {
        Screen(Page.Load,"Charger une sauvegarde");
        try
        {
            var slots=session.GetSlots();
            if(slots.Count==0) MenuWidgets.Label(content,"Aucune sauvegarde disponible.");
            foreach(var slot in slots)
            {
                if(!slot.IsValid) {MenuWidgets.Label(content,"Sauvegarde illisible — "+slot.error,18);continue;}
                string date=DateTime.Parse(slot.data.savedAtUtc,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                string phase=slot.data.day.completedReport != null && slot.data.day.completedReport.EndsCampaign ? "Fin de campagne" : slot.data.day.phase==SavedDayPhase.Briefing?"Consignes":slot.data.day.phase==SavedDayPhase.Review?"Revue des dossiers":slot.data.day.phase==SavedDayPhase.Interview?"Entretien":"Journée terminée";
                MenuWidgets.Label(content,slot.data.name+"\n"+slot.data.dayTitle+" — "+phase+"\n"+date+(slot.recovered?"\nCopie de secours récupérée":""));
                MenuWidgets.Button(content,"Charger",()=>session.Load(slot.id));
            }
        }
        catch(Exception error) {status.text="Lecture des sauvegardes impossible : "+error.Message;}
        MenuWidgets.Button(content,"Retour",Back);
    }

    private void ShowOptions(bool fresh)
    {
        if(fresh)
        {
            originalBindings=session.Catalog.inputActions.SaveBindingOverridesAsJson();
            draft=JsonUtility.FromJson<GameOptionsData>(JsonUtility.ToJson(session.Options.Current));
            resolutions.Clear();
            var seen=new HashSet<string>();
            foreach(var resolution in UnityEngine.Screen.resolutions)
                if(seen.Add(resolution.width+"x"+resolution.height+"@"+Mathf.RoundToInt((float)resolution.refreshRateRatio.value)))resolutions.Add(resolution);
            bool currentFound=false;
            foreach(var resolution in resolutions) if(resolution.width==draft.width&&resolution.height==draft.height)currentFound=true;
            if(!currentFound)resolutions.Add(new Resolution{width=draft.width,height=draft.height,refreshRateRatio=new RefreshRate{numerator=(uint)Mathf.Max(1,draft.refreshRate),denominator=1}});
        }
        Screen(Page.Options,"Options");
        MenuWidgets.Label(content,"Affichage",24);
        var captions=new List<string>();int selected=0;int refreshDifference=int.MaxValue;
        for(int i=0;i<resolutions.Count;i++)
        {
            var resolution=resolutions[i];captions.Add(resolution.width+" × "+resolution.height+" — "+Mathf.RoundToInt((float)resolution.refreshRateRatio.value)+" Hz");
            int difference=Mathf.Abs(Mathf.RoundToInt((float)resolution.refreshRateRatio.value)-draft.refreshRate);
            if(resolution.width==draft.width&&resolution.height==draft.height&&difference<refreshDifference){selected=i;refreshDifference=difference;}
        }
        var size=MenuWidgets.Dropdown(content,captions.ToArray(),selected);
        draft.refreshRate=Mathf.RoundToInt((float)resolutions[selected].refreshRateRatio.value);
        size.onValueChanged.AddListener(index=>{var resolution=resolutions[index];draft.width=resolution.width;draft.height=resolution.height;draft.refreshRate=Mathf.RoundToInt((float)resolution.refreshRateRatio.value);});
        FullScreenMode[] modes={FullScreenMode.Windowed,FullScreenMode.FullScreenWindow,FullScreenMode.ExclusiveFullScreen};
        int modeIndex=Array.IndexOf(modes,draft.mode);if(modeIndex<0)modeIndex=0;
        draft.mode=modes[modeIndex];
        var mode=MenuWidgets.Dropdown(content,new[]{"Fenêtré","Plein écran sans bordure","Plein écran exclusif"},modeIndex);
        mode.onValueChanged.AddListener(index=>draft.mode=modes[index]);
        var vsync=MenuWidgets.Dropdown(content,new[]{"Synchronisation verticale : désactivée","Synchronisation verticale : activée"},draft.vSync?1:0);
        vsync.onValueChanged.AddListener(index=>draft.vSync=index==1);
        MenuWidgets.Label(content,"Touches du clavier",24);
        var map=session.Catalog.inputActions.FindActionMap("Player");
        foreach(var action in map.actions)
        {
            if(action.name!="Move"&&action.name!="Interact"&&action.name!="Pause")continue;
            for(int i=0;i<action.bindings.Count;i++)
            {
                int index=i;var binding=action.bindings[i];
                if(binding.isComposite||!(binding.path??"").StartsWith("<Keyboard>",StringComparison.OrdinalIgnoreCase))continue;
                string direction=binding.name=="up"?"haut":binding.name=="down"?"bas":binding.name=="left"?"gauche":binding.name=="right"?"droite":binding.name;
                string label=action.name=="Move"?"Déplacement "+direction:action.name=="Interact"?"Interagir":"Pause";
                MenuWidgets.Button(content,label+" : "+action.GetBindingDisplayString(index,InputBinding.DisplayStringOptions.DontIncludeInteractions),()=>StartRebind(action,index));
            }
        }
        MenuWidgets.Button(content,"Rétablir les touches par défaut",()=>{session.Catalog.inputActions.RemoveAllBindingOverrides();ShowOptions(false);status.text="Touches rétablies. Appliquer pour conserver les changements.";});
        MenuWidgets.Button(content,"Appliquer",()=>
        {
            if(session.Options.Save(draft,out var error)) {originalBindings=session.Catalog.inputActions.SaveBindingOverridesAsJson();status.text="Options enregistrées.";}
            else status.text=error;
        });
        MenuWidgets.Button(content,"Retour sans appliquer",Back);
        if(!string.IsNullOrEmpty(session.Options.LastError))status.text=session.Options.LastError;
    }

    private void StartRebind(InputAction action,int index)
    {
        if(rebinding!=null)return;
        string before=session.Catalog.inputActions.SaveBindingOverridesAsJson();
        rebindAction=action;rebindWasEnabled=action.enabled;action.Disable();
        status.text="Appuyez sur une touche. Échap pour annuler.";
        rebinding=action.PerformInteractiveRebinding(index).WithExpectedControlType("Button")
            .WithControlsHavingToMatchPath("<Keyboard>/*").WithCancelingThrough("<Keyboard>/escape")
            .OnCancel(operation=>{FinishRebind();ShowOptions(false);status.text="Modification annulée.";})
            .OnComplete(operation=>
            {
                string path=action.bindings[index].effectivePath;
                bool conflict=false;
                foreach(var other in action.actionMap.actions)
                {
                    if(other.name!="Move"&&other.name!="Interact"&&other.name!="Pause")continue;
                    for(int j=0;j<other.bindings.Count;j++)
                        if(!(other==action&&j==index)&&!other.bindings[j].isComposite&&string.Equals(other.bindings[j].effectivePath,path,StringComparison.OrdinalIgnoreCase))conflict=true;
                }
                if(conflict)session.Options.RestoreBindings(before);
                FinishRebind();ShowOptions(false);
                status.text=conflict?"Cette touche est déjà utilisée. Choisissez-en une autre.":"Touche modifiée. Appliquer pour enregistrer.";
            });
        rebinding.Start();
    }

    private void FinishRebind()
    {
        ignorePauseThroughFrame=Time.frameCount+1;
        rebinding?.Dispose();rebinding=null;
        if(rebindWasEnabled)rebindAction?.Enable();rebindAction=null;
    }
    private void CancelOptions()
    {
        if(rebinding!=null) {rebinding.Cancel();FinishRebind();}
        if(originalBindings!=null) {session.Options.RestoreBindings(originalBindings);originalBindings=null;}
    }
    private void OnDestroy()
    {
        rebinding?.Dispose();
        if(ownsPause)pause?.Dispose();
        if(pauseActive)Time.timeScale=oldTimeScale;
    }
}

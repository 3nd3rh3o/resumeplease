using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class MotivationLettersReview : IInteractible
{
    [UnityEngine.Serialization.FormerlySerializedAs("cvPanelRenderer")]
    [SerializeField] private PanelRenderer letterPanelRenderer;
    [SerializeField] private float focusDistance = 0.8f;
    [SerializeField] private float focusedFontSize = 1f;

    [SerializeField] private PanelRenderer pcPanelRenderer;

    private ScrollView letterScrollView;
    private Label documentTitleLabel;
    private readonly List<TaggedDocumentText> fields = new List<TaggedDocumentText>();
    private VisualElement letterRoot;
    private TaggedDocumentText hoveredField;
    private int hoveredLink = -1;
    private Candidate current;
    private bool focus = false;
    private PanelRenderer.VersionedUIReloadCallback uiReloadCallback;
    private Camera focusCamera;
    [SerializeField]
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private static readonly Quaternion FocusRotationOffset = Quaternion.identity;
    private bool hasSavedPanelTransform;

    public bool IsFocused => focus;
    public Candidate CurrentCandidate => current;
    public Vector2 ScrollOffset { get => letterScrollView == null ? Vector2.zero : letterScrollView.scrollOffset; set { if (letterScrollView != null) letterScrollView.scrollOffset = value; } }

    public void Clear()
    {
        if (focus)
            RestorePanel();
        current = null;
        UpdateLetterText();
        if (letterScrollView != null)
            letterScrollView.scrollOffset = Vector2.zero;
        var animator = GetComponentInParent<Animator>();
        if (animator != null)
            animator.SetBool("HasMotivationLetter", false);
    }

    private void Awake()
    {
        requireSeated = true;

        uiReloadCallback = OnUIReloaded;
        if (letterPanelRenderer != null)
            letterPanelRenderer.RegisterUIReloadCallback(uiReloadCallback);
    }

    private void OnDestroy()
    {
        if (letterPanelRenderer != null && uiReloadCallback != null)
            letterPanelRenderer.UnregisterUIReloadCallback(uiReloadCallback);
    }

    private void OnUIReloaded(PanelRenderer renderer, VisualElement root, int version)
    {
        letterRoot = root;
        letterScrollView = root.Q<ScrollView>("letter-scroll");
        documentTitleLabel = root.Q<Label>("document-title");
        if (letterScrollView == null)
            Debug.LogError("ScrollView UI Toolkit introuvable : letter-scroll", this);

        if (documentTitleLabel == null)
            Debug.LogError("Label UI Toolkit introuvable : document-title", this);

        UpdateLetterText();

        if (focus)
            SetLetterFontSize(focusedFontSize);
    }

    private void Update()
    {
        var animator = GetComponentInParent<Animator>();
        if (animator != null)
            animator.SetBool("HasMotivationLetter", current?.m_motivationLetter != null);
    }

    private void LateUpdate()
    {
        if (focus && letterPanelRenderer != null && focusCamera != null)
        {
            var panelTransform = letterPanelRenderer.transform;
            panelTransform.position = focusCamera.transform.position + focusCamera.transform.forward * focusDistance;
            panelTransform.rotation = focusCamera.transform.rotation * FocusRotationOffset;
        }

        UpdateProjectedPointer();
    }

    public bool HasMotivationLetter()
    {
        return current?.m_motivationLetter != null;
    }

    public void ReceiveMotivationLetter(ref Candidate candidate)
    {
        current = candidate;
        UpdateLetterText();

        if (current == null || current.m_motivationLetter == null)
        {
            Debug.LogError("Le candidat reçu n'a pas de lettre de motivation assignée.", this);
            return;
        }

        Debug.Log("Lettre de motivation reçue.", this);
    }

    private void UpdateLetterText()
    {
        if (letterScrollView == null)
            return;

        letterScrollView.contentContainer.Clear();
        fields.Clear();
        hoveredField = null;
        hoveredLink = -1;
        if (documentTitleLabel != null)
            documentTitleLabel.text = "LETTRE DE MOTIVATION";

        if (current == null)
            return;

        var contentStyle = letterScrollView.contentContainer.style;
        contentStyle.flexDirection = FlexDirection.Column;
        contentStyle.alignItems = Align.Stretch;
        contentStyle.paddingTop = 0;
        contentStyle.paddingBottom = 0;
        contentStyle.paddingLeft = 0;
        contentStyle.paddingRight = 0;

        if (current.m_motivationLetter == null)
            return;

        AddLetterText(current.m_motivationLetter.m_content);
    }

    private void AddLetterText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var field = new TaggedDocumentText("Lettre de motivation", text, focus ? focusedFontSize : 1f);
        letterScrollView.Add(field.Label);
        fields.Add(field);
    }

    private void UpdateProjectedPointer()
    {
        if (GameSession.InputBlocked) { SetHoveredField(null, -1); return; }
        var mouse = Mouse.current;
        var camera = focusCamera != null ? focusCamera : Camera.main;
        TaggedDocumentText field = null;
        int link = -1;
        if (mouse != null && camera != null && letterPanelRenderer != null &&
            letterRoot?.panel != null && letterScrollView != null && current != null)
        {
            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            // The UI belongs to the referenced PanelRenderer GameObject.
            // UI worldTransform is relative to that PanelRenderer's GameObject.
            var documentToWorld = letterPanelRenderer.transform.localToWorldMatrix;
            if (RayIntersectsElement(ray, documentToWorld, letterScrollView.contentViewport))
            {
                foreach (var candidate in fields)
                {
                    if (!TryProjectPointer(ray, documentToWorld, candidate.Label, out var point))
                        continue;
                    link = candidate.HitTest(point);
                    if (link < 0)
                        continue;
                    field = candidate;
                    break;
                }
            }
        }

        SetHoveredField(field, link);
        if (field != null && mouse.leftButton.wasPressedThisFrame)
            OpenFieldDocument(field, link);
    }

    private void OpenFieldDocument(TaggedDocumentText field, int link)
    {
        var documentId = field.DocumentId(link);

        var documents = current?.m_motivationLetter?.uIDocuments;
        if (documents == null || documentId < 0 || documentId >= documents.Count || documents[documentId] == null)
        {
            Debug.LogWarning($"Aucun document UI pour la balise {documentId} du champ {field.FieldName}.", this);
            return;
        }

        if (pcPanelRenderer == null)
        {
            Debug.LogWarning("Assigne le PanelRenderer du PC pour afficher les documents de la lettre de motivation.", this);
            return;
        }

        pcPanelRenderer.visualTreeAsset = documents[documentId];
    }

    private static bool RayIntersectsElement(Ray worldRay, Matrix4x4 documentToWorld, VisualElement element)
    {
        return TryProjectPointer(worldRay, documentToWorld, element, out _);
    }

    private static bool TryProjectPointer(Ray worldRay, Matrix4x4 documentToWorld, VisualElement element, out Vector2 localPoint)
    {
        localPoint = default;
        // Include the UI layout, pivot, pixel scale, and the actual sheet transform.
        var worldToElement = (documentToWorld * element.worldTransform).inverse;
        var origin = worldToElement.MultiplyPoint3x4(worldRay.origin);
        var direction = worldToElement.MultiplyVector(worldRay.direction);
        if (Mathf.Abs(direction.z) < 0.000001f)
            return false;

        var distance = -origin.z / direction.z;
        if (distance < 0f)
            return false;

        var point = origin + direction * distance;
        localPoint = new Vector2(point.x, point.y);
        return element.visible && element.ContainsPoint(localPoint);
    }

    private void SetHoveredField(TaggedDocumentText field, int link)
    {
        if (hoveredField == field && hoveredLink == link)
            return;
        hoveredField?.SetHoveredLink(-1);
        hoveredField = field;
        hoveredLink = link;
        hoveredField?.SetHoveredLink(link);
    }

    private void SetLetterFontSize(float fontSize)
    {
        if (documentTitleLabel != null)
            documentTitleLabel.style.fontSize = fontSize;

        foreach (var field in fields)
            field.Label.style.fontSize = fontSize;
    }
    
    public override void OnInteract()
    {
        if (current?.m_motivationLetter != null)
        {
            if (focus)
                RestorePanel();
            else
                FocusPanel();
        }
        else
        {
            Debug.Log("No motivation letter to review.");
        }
    }

    private void FocusPanel()
    {
        if (letterPanelRenderer == null)
        {
            Debug.LogError("Impossible de focaliser la lettre de motivation : aucun PanelRenderer n'est assigné.", this);
            return;
        }

        var camera = Camera.main;
        if (camera == null)
        {
            Debug.LogError("Impossible de focaliser la lettre de motivation : aucune caméra avec le tag MainCamera.", this);
            return;
        }

        var panelTransform = letterPanelRenderer.transform;
        focusCamera = camera;
        originalLocalPosition = panelTransform.localPosition;
        originalLocalRotation = panelTransform.localRotation;
        hasSavedPanelTransform = true;
        UpdateLetterText();
        panelTransform.position = camera.transform.position + camera.transform.forward * focusDistance;
        panelTransform.rotation = camera.transform.rotation * FocusRotationOffset;

        SetLetterFontSize(focusedFontSize);

        focus = true;
    }

    private void RestorePanel()
    {
        if (letterPanelRenderer == null || !hasSavedPanelTransform)
            return;

        var panelTransform = letterPanelRenderer.transform;
        panelTransform.localPosition = originalLocalPosition;
        panelTransform.localRotation = originalLocalRotation;

        SetLetterFontSize(1f);

        focusCamera = null;
        hasSavedPanelTransform = false;
        focus = false;
        UpdateLetterText();
    }

    private void OnDisable()
    {
        SetHoveredField(null, -1);
        if (focus)
            RestorePanel();
    }
}

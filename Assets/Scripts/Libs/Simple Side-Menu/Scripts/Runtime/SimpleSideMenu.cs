using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/Simple Side-Menu")]
public class SimpleSideMenu : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IInitializePotentialDragHandler
{
    #region Fields
    private Vector2 closedPosition, openPosition, startPosition, previousPosition, releaseVelocity, dragVelocity;
    public GameObject overlay { get; private set; }
    public GameObject blur { get; private set; }
    private RectTransform rectTransform;
    private float thresholdStateChangeDistance = 10f, previousTime;
    private bool dragging, potentialDrag;
    public Material blurMaterial;

    public Placement placement = Placement.Left;
    public State defaultState = State.Closed;
    public float transitionSpeed = 10f;
    public float thresholdDragSpeed = 0f;
    public float thresholdDraggedFraction = 0.5f;
    public GameObject handle = null;
    public bool handleDraggable = true;
    public bool menuDraggable = false;
    public bool handleToggleStateOnPressed = true;
    public bool useOverlay = true;
    public Color overlayColour = new Color(0, 0, 0, 0.25f);
    public bool useBlur = false;
    public int blurRadius = 10;
    public bool overlayCloseOnPressed = true;

    public event Action<State> onStateUpdate;
    #endregion

    #region Properties
    public State CurrentState { get; private set; }
    public State TargetState { get; private set; }
    public float StateProgress { get { return ((rectTransform.anchoredPosition - closedPosition).magnitude / ((placement == Placement.Left || placement == Placement.Right) ? rectTransform.rect.width : rectTransform.rect.height)); } }
    #endregion

    #region Enumerators
    public enum Placement
    {
        Left,
        Right,
        Top,
        Bottom
    }
    public enum State
    {
        Closed,
        Open
    }
    #endregion

    #region Methods
    private void Start() => Setup();
    private void Update()
    {
        OnStateUpdate();
        OnOverlayUpdate();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        potentialDrag = (handleDraggable && eventData.pointerEnter == handle) || (menuDraggable && eventData.pointerEnter == gameObject);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = potentialDrag;
        startPosition = previousPosition = eventData.position;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        releaseVelocity = dragVelocity;
        OnTargetUpdate();
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (dragging)
        {
            var scaleFactor = 1F;
            if (FindObjectOfType<Canvas>().TryGetComponent(out CanvasScaler canvasScaler))
            {
                var kLogBase = 2;
                var referenceResolution = canvasScaler.referenceResolution;
                float logWidth = Mathf.Log(Screen.width / referenceResolution.x, kLogBase);
                float logHeight = Mathf.Log(Screen.height / referenceResolution.y, kLogBase);
                float logWeightedAverage = Mathf.Lerp(logWidth, logHeight, canvasScaler.matchWidthOrHeight);
                scaleFactor = Mathf.Pow(kLogBase, logWeightedAverage);
            }

            var displacement = ((TargetState == State.Closed) ? closedPosition : openPosition) + (eventData.position - startPosition) / scaleFactor;
            float x = (placement == Placement.Left || placement == Placement.Right) ? displacement.x : rectTransform.anchoredPosition.x;
            float y = (placement == Placement.Top || placement == Placement.Bottom) ? displacement.y : rectTransform.anchoredPosition.y;

            Vector2 min = new Vector2(Math.Min(closedPosition.x, openPosition.x), Math.Min(closedPosition.y, openPosition.y));
            Vector2 max = new Vector2(Math.Max(closedPosition.x, openPosition.x), Math.Max(closedPosition.y, openPosition.y));

            rectTransform.anchoredPosition = new Vector2(Mathf.Clamp(x, min.x, max.x), Mathf.Clamp(y, min.y, max.y));
        }
    }

    private bool Validate()
    {
        bool valid = true;
        rectTransform = GetComponent<RectTransform>();

        if (transitionSpeed <= 0)
        {
            Debug.LogError("<b>[SimpleSideMenu]</b> Transition speed cannot be less than or equal to zero.", gameObject);
            valid = false;
        }
        if (handle != null && handleDraggable && handle.transform.parent != rectTransform)
        {
            Debug.LogError("<b>[SimpleSideMenu]</b> The drag handle must be a child of the side menu in order for it to be draggable.", gameObject);
            valid = false;
        }
        if (handleToggleStateOnPressed && handle.GetComponent<Button>() == null)
        {
            Debug.LogError("<b>[SimpleSideMenu]</b> The handle must have a \"Button\" component attached to it in order for it to be able to toggle the state of the side menu when pressed.", gameObject);
            valid = false;
        }
        return valid;
    }
    public void Setup()
    {
        if (!Validate()) throw new Exception("Invalid inspector input.");

        //Placement
        switch (placement)
        {
            case Placement.Left:
                rectTransform.pivot = new Vector2(1, 0.5f);
                closedPosition = new Vector2(0, rectTransform.localPosition.y);
                openPosition = new Vector2(rectTransform.rect.width, rectTransform.localPosition.y);
                break;
            case Placement.Right:
                rectTransform.pivot = new Vector2(0, 0.5f);
                closedPosition = new Vector2(0, rectTransform.localPosition.y);
                openPosition = new Vector2(-1 * rectTransform.rect.width, rectTransform.localPosition.y);
                break;
            case Placement.Top:
                rectTransform.pivot = new Vector2(0.5f, 0);
                closedPosition = new Vector2(rectTransform.localPosition.x, 0);
                openPosition = new Vector2(rectTransform.localPosition.x, -1 * rectTransform.rect.height);
                break;
            case Placement.Bottom:
                rectTransform.pivot = new Vector2(0.5f, 1);
                closedPosition = new Vector2(rectTransform.localPosition.x, 0);
                openPosition = new Vector2(rectTransform.localPosition.x, rectTransform.rect.height);
                break;
        }

        //Default State
        CurrentState = TargetState = defaultState;
        rectTransform.anchoredPosition = (defaultState == State.Closed) ? closedPosition : openPosition;

        //Drag Handle
        if (handle != null)
        {
            //Toggle State on Pressed
            if (handleToggleStateOnPressed)
            {
                handle.GetComponent<Button>().onClick.AddListener(delegate { ToggleState(); });
            }
            foreach (Text text in handle.GetComponentsInChildren<Text>())
            {
                if (text.gameObject != handle) text.raycastTarget = false;
            }
        }

        //Overlay
        if (useOverlay)
        {
            overlay = new GameObject(gameObject.name + " (Overlay)");
            overlay.transform.parent = transform.parent;
            overlay.transform.SetSiblingIndex(transform.GetSiblingIndex());

            if (useBlur)
            {
                blur = new GameObject(gameObject.name + " (Blur)");
                blur.transform.parent = transform.parent;
                blur.transform.SetSiblingIndex(transform.GetSiblingIndex());

                RectTransform blurRectTransform = blur.AddComponent<RectTransform>();
                blurRectTransform.anchorMin = Vector2.zero;
                blurRectTransform.anchorMax = Vector2.one;
                blurRectTransform.offsetMin = Vector2.zero;
                blurRectTransform.offsetMax = Vector2.zero;
                Image blurImage = blur.AddComponent<Image>();
                blurImage.raycastTarget = false;
                blurImage.material = new Material(blurMaterial);
                blurImage.material.SetInt("_Radius", 0);
            }

            RectTransform overlayRectTransform = overlay.AddComponent<RectTransform>();
            overlayRectTransform.anchorMin = Vector2.zero;
            overlayRectTransform.anchorMax = Vector2.one;
            overlayRectTransform.offsetMin = Vector2.zero;
            overlayRectTransform.offsetMax = Vector2.zero;
            overlayRectTransform.localScale = Vector3.one;
            Image overlayImage = overlay.AddComponent<Image>();
            overlayImage.color = (defaultState == State.Open) ? overlayColour : Color.clear;
            overlayImage.raycastTarget = overlayCloseOnPressed;
            Button overlayButton = overlay.AddComponent<Button>();
            overlayButton.transition = Selectable.Transition.None;
            overlayButton.onClick.AddListener(delegate { Close(); });
        }
    }

    private void OnTargetUpdate()
    {
        if (releaseVelocity.magnitude > thresholdDragSpeed)
        {
            if (placement == Placement.Left)
            {
                if (releaseVelocity.x > 0)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
            else if (placement == Placement.Right)
            {
                if (releaseVelocity.x < 0)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
            else if (placement == Placement.Top)
            {
                if (releaseVelocity.y < 0)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
            else
            {
                if (releaseVelocity.y > 0)
                {
                    Open();
                }
                else
                {
                    Close();
                }
            }
        }
        else
        {
            float nextStateProgress = (TargetState == State.Open) ? 1 - StateProgress : StateProgress;

            if (nextStateProgress > thresholdDraggedFraction)
            {
                ToggleState();
            }
        }
    }
    private void OnStateUpdate()
    {
        if (dragging)
        {
            Vector2 mousePosition = Input.mousePosition;
            dragVelocity = (mousePosition - previousPosition) / (Time.time - previousTime);
            previousPosition = mousePosition;
            previousTime = Time.time;
        }
        else
        {
            Vector2 targetPosition = (TargetState == State.Closed) ? closedPosition : openPosition;

            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.unscaledDeltaTime * transitionSpeed);
            if ((rectTransform.anchoredPosition - targetPosition).magnitude <= thresholdStateChangeDistance)
            {
                CurrentState = TargetState;
            }
        }
    }
    private void OnOverlayUpdate()
    {
        if (useOverlay)
        {
            overlay.GetComponent<Image>().raycastTarget = overlayCloseOnPressed && (TargetState == State.Open);
            overlay.GetComponent<Image>().color = new Color(overlayColour.r, overlayColour.g, overlayColour.b, overlayColour.a * StateProgress);

            if (useBlur)
            {
                blur.GetComponent<Image>().material.SetInt("_Radius", (int)(blurRadius * StateProgress));
            }
        }
    }

    public void ToggleState()
    {
        if (TargetState == State.Closed) Open();
        else if (TargetState == State.Open) Close();
    }
    public void Open()
    {
        TargetState = State.Open;
        onStateUpdate?.Invoke(TargetState);
    }
    public void Close()
    {
        TargetState = State.Closed;
        onStateUpdate?.Invoke(TargetState);
    }
    #endregion
}
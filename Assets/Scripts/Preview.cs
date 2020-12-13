using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Zooms the attached image in or out.
/// Attach this script to scrollview content panel.
/// All anchors and pivots set to 0.5.
/// Position under mouse remains there.
/// </summary>

public class Preview : MonoBehaviour, IScrollHandler
{

    //Make sure these values are evenly divisible by scaleIncrement
    [SerializeField] float _minimumScale = 0.5f;
    [SerializeField] float _initialScale = 1f;
    [SerializeField] float _maximumScale = 3f;
    /////////////////////////////////////////////
    [SerializeField] float _scaleIncrement = .5f;
    /////////////////////////////////////////////

    [HideInInspector] Vector3 _scale;
    public event System.Action<Vector3> onZoomChanged;

    [HideInInspector] public RectTransform _thisTransform;

    private void Awake() => Reset();
    public void Reset()
    {
        _thisTransform = transform as RectTransform;

        _scale.Set(_initialScale, _initialScale, 1f);
        _thisTransform.localScale = _scale;
    }

    public void OnScroll(PointerEventData eventData)
    {
        Vector2 relativeMousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_thisTransform, Input.mousePosition, null, out relativeMousePosition);

        float delta = eventData.scrollDelta.y;

        if (delta > 0 && _scale.x < _maximumScale)
        {   //zoom in

            _scale.Set(_scale.x + _scaleIncrement, _scale.y + _scaleIncrement, 1f);
            _thisTransform.localScale = _scale;
            _thisTransform.anchoredPosition -= (relativeMousePosition * _scaleIncrement);
        }

        else if (delta < 0 && _scale.x > _minimumScale)
        {   //zoom out

            _scale.Set(_scale.x - _scaleIncrement, _scale.y - _scaleIncrement, 1f);
            _thisTransform.localScale = _scale;
            _thisTransform.anchoredPosition += (relativeMousePosition * _scaleIncrement);
        }
    }

    float lastDelta;
    void LateUpdate()
    {
        if (lastDelta != 0 && Input.mouseScrollDelta.y == 0) onZoomChanged?.Invoke(_scale);
        lastDelta = Input.mouseScrollDelta.y;
    }
}


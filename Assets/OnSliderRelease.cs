using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OnSliderRelease : MonoBehaviour, IPointerUpHandler
{
    Slider slider;
    float oldValue;

    public Slider.SliderEvent onValueEndedChange;

    void Start()
    {
        slider = GetComponent<Slider>();
        oldValue = slider.value;
    }

    public void OnPointerUp(PointerEventData _)
    {
        if (slider.value == oldValue) return;

        onValueEndedChange?.Invoke(slider.value);
        oldValue = slider.value;
    }
}

using UnityEngine;
using UnityEngine.UI;

public class RaiseButtonHelper : MonoBehaviour
{
    public PokerGameManager manager;      // Перетащи сюда PokerGameManager
    public Slider raiseSlider;            // Перетащи сюда свой Slider

    public void Raise()
    {
        int value = Mathf.RoundToInt(raiseSlider.value);
        manager.PublicRaiseWithAmount(value);
    }
}

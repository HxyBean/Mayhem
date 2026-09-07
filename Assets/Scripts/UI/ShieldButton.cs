using UnityEngine;
using UnityEngine.EventSystems;

// Nút Khiên của Knight - giữ để tăng chống chịu, thả tay để hạ khiên.
public class ShieldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private KnightCombat knightCombat;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (knightCombat != null) knightCombat.OnShieldPressed();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (knightCombat != null) knightCombat.OnShieldReleased();
    }
}

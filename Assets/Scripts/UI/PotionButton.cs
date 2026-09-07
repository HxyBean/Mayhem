using UnityEngine;

public class PotionButton : DragAimButton
{
    [SerializeField] private Gun gunScript;

    protected override bool CanStartDrag()
    {
        return gunScript != null && gunScript.CanThrowPotion();
    }

    protected override void OnConfirm(Vector3 targetPosition)
    {
        if (gunScript != null) gunScript.ThrowPotion(targetPosition);
    }
}

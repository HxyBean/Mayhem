using UnityEngine;

public class BombButton : DragAimButton
{
    [SerializeField] private Gun gunScript;

    protected override bool CanStartDrag()
    {
        return gunScript != null && gunScript.CanThrowBomb();
    }

    protected override void OnConfirm(Vector3 targetPosition)
    {
        if (gunScript != null) gunScript.ThrowBomb(targetPosition);
    }
}

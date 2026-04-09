namespace Wander.Character.Attack
{
    /// <summary>
    /// Implemented by AttackBridge. Called by AttackAnimEventProxy to forward Animation Events.
    /// </summary>
    public interface IAttackAnimEventReceiver
    {
        void OnComboWindowOpen();
        void OnComboWindowClose();
        void OnHitboxActivate();
        void OnHitboxDeactivate();
    }
}
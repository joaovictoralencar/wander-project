using UnityEngine;

namespace Wander.Character.Attack
{
    /// <summary>
    /// Place this on the same GameObject as the Animator.
    /// Animation Events call these methods; the proxy forwards them to AttackBridge.
    /// </summary>
    public class AttackAnimEventProxy : MonoBehaviour
    {
        private IAttackAnimEventReceiver _receiver;

        private void Awake()
        {
            _receiver = GetComponentInParent<IAttackAnimEventReceiver>();
            if (_receiver == null)
                Debug.LogWarning($"[AttackAnimEventProxy] No IAttackAnimEventReceiver found on parents of '{gameObject.name}'.", this);
        }

        // ── Called by Animation Events on attack clips ──

        public void OnComboWindowOpen()    => _receiver?.OnComboWindowOpen();
        public void OnComboWindowClose()   => _receiver?.OnComboWindowClose();
        public void OnHitboxActivate()     => _receiver?.OnHitboxActivate();
        public void OnHitboxDeactivate()   => _receiver?.OnHitboxDeactivate();
    }
}
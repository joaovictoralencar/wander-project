using System.Text;
using UnityEngine;
using LukeyB.DeepStats.User;
using System.Collections.Generic;

namespace LukeyB.DeepStats.Demo
{
    public class ScaledModifiersController : MonoBehaviour
    {
        public DeepStatsMB Stats;
        public float nearbyRadius = 5f;

        private Rigidbody rb;

        private Collider[] _colliders = new Collider[20];
        private StringBuilder _sb = new StringBuilder();
        private GUIStyle _style;

        // Start is called before the first frame update
        void Awake()
        {
            rb = GetComponent<Rigidbody>();

            _style = new GUIStyle();
        }

        private void FixedUpdate()
        {
            float moveHorizontal = Input.GetAxis("Horizontal");
            float moveVertical = Input.GetAxis("Vertical");

            Stats.DeepStats.UpdateFinalValues(null);
            var speed = Stats.DeepStats[StatType.MovementSpeed];
            Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);
            rb.MovePosition(rb.position + movement * speed * Time.fixedDeltaTime);

            ScanForTrees();
            ScanForWater();
        }

        private void ScanForTrees()
        {
            var hitCount = Physics.OverlapSphereNonAlloc(transform.position, nearbyRadius, _colliders);
            var totalNearbyTrees = 0;

            for (var i = 0; i < hitCount; i++)
            {
                if (_colliders[i].gameObject.GetComponent<DemoTree>())
                {
                    totalNearbyTrees++;
                }
            }

            Stats.DeepStats.SetScaler(ModifierScaler.NearbyTrees, totalNearbyTrees);
        }

        private void ScanForWater()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out var hit) && hit.collider.gameObject.GetComponent<DemoWater>())
            {
                Stats.DeepStats.SetScaler(ModifierScaler.InWater, 1);
            }
            else
            {
                Stats.DeepStats.SetScaler(ModifierScaler.InWater, 0);
            }
        }

        private void OnGUI()
        {
            _style.fontSize = (int)(Screen.width / 60f);

            Stats.DeepStats.UpdateFinalValues(null);
            _sb.Clear();

            _sb.Append("Move using the arrow keys\n- You move faster for each nearby tree (up to 3 max)\n- You move slower if you are in water\nLook at the Modifiers on the Player GameObject to see how these modifiers are configured\n\n");
            _sb.Append($"Current Speed: {Stats.DeepStats[StatType.MovementSpeed].ToString("0.##")}");

            GUI.Label(new Rect(40, 40, 300, 50), _sb.ToString(), _style);
        }

        private void OnDrawGizmosSelected()
        {
            var c = Color.red;
            c.a = 0.45f;
            Gizmos.color = c;
            Gizmos.DrawSphere(transform.position, nearbyRadius);
        }
    }
}
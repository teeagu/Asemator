using UnityEngine;

namespace Asemation
{
    public class PlayerTest : MonoBehaviour
    {
        Asemator asemator;

        void Start()
        {
            asemator = GetComponent<Asemator>();
        }

        void Update()
        {
            Vector2 speed = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * 5 * Time.deltaTime;

            if (speed.magnitude > 0)
            {
                if (!asemator.IsPlaying(tag: "run")) asemator.SetAnimation(tag: "run", loop: true);
                transform.position += (Vector3)speed;
            }

            else if (!asemator.IsPlaying(tag: "idle")) asemator.SetAnimation(tag: "idle", loop: true);
        }
    }
}
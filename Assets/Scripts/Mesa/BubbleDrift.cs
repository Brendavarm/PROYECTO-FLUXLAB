using UnityEngine;

namespace FluxLab
{
    public class BubbleDrift : MonoBehaviour
    {
        public float speed = 36f;
        public float amplitude = 10f;
        public float limit = 160f;

        RectTransform rect;
        Vector2 origin;
        float phase;

        void Start()
        {
            rect = GetComponent<RectTransform>();
            origin = rect.anchoredPosition;
            phase = Random.Range(0f, 6.28f);
        }

        void Update()
        {
            if (rect == null)
                return;

            origin.y += speed * Time.deltaTime;
            if (origin.y > limit)
                origin.y = -24f;

            float sway = Mathf.Sin(Time.time * 1.6f + phase) * amplitude;
            rect.anchoredPosition = origin + new Vector2(sway, 0f);
        }
    }
}

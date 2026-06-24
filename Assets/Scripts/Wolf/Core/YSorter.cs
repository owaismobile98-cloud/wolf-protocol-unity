using UnityEngine;

namespace Wolf.Protocol
{
    /// <summary>
    /// 2.5D depth sort: higher world Y draws behind lower Y (sortingOrder from -Y).
    /// </summary>
    [DisallowMultipleComponent]
    public class YSorter : MonoBehaviour
    {
        public const float SortScale = 10f;

        public bool IsStatic;
        public int Offset;

        SpriteRenderer _sprite;

        void Awake()
        {
            _sprite = GetComponent<SpriteRenderer>();
        }

        void Start() => UpdateSorting();

        void LateUpdate()
        {
            if (!IsStatic) UpdateSorting();
        }

        public void UpdateSorting()
        {
            if (_sprite == null) return;
            _sprite.sortingOrder = Mathf.RoundToInt(-transform.position.y * SortScale) + Offset;
        }

        public static int OrderForPosition(Vector3 position, int offset = 0)
        {
            return Mathf.RoundToInt(-position.y * SortScale) + offset;
        }
    }
}

using System.Collections;
using UnityEngine;

namespace TrashSorting
{
    /// <summary>Area trigger di dalam tempat sampah yang mengecek jenis sampah.</summary>
    [RequireComponent(typeof(Collider))]
    public class TrashBin : MonoBehaviour
    {
        public TrashType acceptedType;
        public Renderer highlightRenderer;

        Color _baseColor;

        void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            if (highlightRenderer) _baseColor = highlightRenderer.material.color;
        }

        void OnTriggerEnter(Collider other)
        {
            var item = other.GetComponentInParent<TrashItem>();
            if (item == null || item.IsSorted || GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            bool correct = item.type == acceptedType;
            GameManager.Instance.ReportSort(item, acceptedType, correct);

            if (correct)
            {
                item.MarkSorted();
                Flash(new Color(0.2f, 1f, 0.3f));
            }
            else
            {
                item.ResetToSpawn();
                Flash(new Color(1f, 0.2f, 0.2f));
            }
        }

        void Flash(Color c)
        {
            if (!highlightRenderer) return;
            StopAllCoroutines();
            StartCoroutine(FlashRoutine(c));
        }

        IEnumerator FlashRoutine(Color c)
        {
            var mat = highlightRenderer.material;
            mat.color = c;
            yield return new WaitForSeconds(0.4f);
            mat.color = _baseColor;
        }
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace TrashSorting
{
    /// <summary>
    /// Sampah yang bisa diambil dengan controller (XRGrabInteractable).
    /// Ganti tampilannya dengan model 3D dari aset kamu: taruh model sebagai child,
    /// lalu matikan MeshRenderer bawaan.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class TrashItem : MonoBehaviour
    {
        public string displayName = "Sampah";
        public TrashType type = TrashType.Plastik;
        [TextArea] public string info = "";

        public bool IsSorted { get; private set; }

        Vector3 _spawnPos;
        Quaternion _spawnRot;
        Rigidbody _rb;
        XRGrabInteractable _grab;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _grab = GetComponent<XRGrabInteractable>();
            _spawnPos = transform.position;
            _spawnRot = transform.rotation;
        }

        void Update()
        {
            // Kalau jatuh keluar map, kembalikan ke posisi awal
            if (transform.position.y < -2f) ResetToSpawn();
        }

        public void ForceRelease()
        {
            if (_grab != null && _grab.isSelected && _grab.interactionManager != null)
                _grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grab);
        }

        public void ResetToSpawn()
        {
            ForceRelease();
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_spawnPos, _spawnRot);
        }

        public void MarkSorted()
        {
            IsSorted = true;
            ForceRelease();
            _grab.enabled = false;
            Destroy(gameObject, 0.05f);
        }
    }
}

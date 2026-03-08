using System.Collections.Generic;
using UnityEngine;

namespace OneDayGame.Presentation.Gameplay
{
    public static class DamagePopupPool
    {
        private static readonly Queue<DamagePopupView> Pool = new Queue<DamagePopupView>();
        private static Transform s_root;

        public static void Spawn(Vector3 position, string text, Color color)
        {
            EnsureRoot();
            DamagePopupView popup = null;
            while (Pool.Count > 0 && popup == null)
            {
                popup = Pool.Dequeue();
            }

            if (popup == null)
            {
                var popupObject = new GameObject("DamagePopup");
                popupObject.transform.SetParent(s_root, false);
                popup = popupObject.AddComponent<DamagePopupView>();
                popup.SetDestroyOnComplete(false);
            }

            popup.transform.position = position;
            popup.Completed -= OnPopupCompleted;
            popup.Completed += OnPopupCompleted;
            popup.Initialize(text, color);
        }

        private static void OnPopupCompleted(DamagePopupView popup)
        {
            if (popup == null)
            {
                return;
            }

            popup.Completed -= OnPopupCompleted;
            popup.transform.SetParent(s_root, false);
            Pool.Enqueue(popup);
        }

        private static void EnsureRoot()
        {
            if (s_root != null)
            {
                return;
            }

            var root = GameObject.Find("DamagePopupRoot") ?? new GameObject("DamagePopupRoot");
            s_root = root.transform;
        }
    }
}

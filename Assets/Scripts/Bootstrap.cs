using UnityEngine;

namespace BuildATower
{
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Auto()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            Launch();
        }

        public static void Launch()
        {
            var go = new GameObject("BuildATower");
            go.AddComponent<GameManager>();
        }
    }
}

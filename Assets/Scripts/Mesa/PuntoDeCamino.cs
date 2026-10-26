using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Punto del camino, colocado en la escena. El relato se lee en el Inspector.
    /// </summary>
    public class PuntoDeCamino : MonoBehaviour
    {
        public int orden = 1;
        public string titulo = "Punto";
        [TextArea(2, 5)] public string relato = "";

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.98f, 0.82f, 0.38f, 0.45f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = new Color(0.45f, 0.24f, 0.08f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = Matrix4x4.identity;

            PuntoDeCamino[] todos = FindObjectsByType<PuntoDeCamino>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < todos.Length; i++)
            {
                if (todos[i].orden != orden + 1)
                    continue;
                Gizmos.color = new Color(0.85f, 0.42f, 0.12f, 1f);
                Gizmos.DrawLine(transform.position, todos[i].transform.position);
            }
        }
    }
}

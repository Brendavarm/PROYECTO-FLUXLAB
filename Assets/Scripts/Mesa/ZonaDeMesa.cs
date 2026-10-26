using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Recuadro de la Mesa del Aprendiz. Marca una zona del nivel 1 en la escena.
    /// </summary>
    public class ZonaDeMesa : MonoBehaviour
    {
        public string nombre = "Zona";
        [TextArea(2, 4)] public string detalle = "";

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.93f, 0.78f, 0.55f, 0.28f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = new Color(0.42f, 0.24f, 0.12f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}

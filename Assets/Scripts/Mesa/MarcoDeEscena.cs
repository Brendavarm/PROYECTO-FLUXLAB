using UnityEngine;

namespace FluxLab
{
    /// <summary>
    /// Los recuadros se ven en el panel Scene. Al pulsar Play no tapan la mesa.
    /// </summary>
    public class MarcoDeEscena : MonoBehaviour
    {
        void Awake()
        {
            Renderer[] render = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < render.Length; i++)
                render[i].enabled = false;
        }
    }
}

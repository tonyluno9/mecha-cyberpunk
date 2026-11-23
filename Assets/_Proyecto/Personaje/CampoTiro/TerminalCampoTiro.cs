using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T85: la terminal holográfica del campo de tiro, a un lado de donde aparece el jugador. Un pedestal con un panel inclinado; al acercarse
    /// (<see cref="ReglaCampoTiro.MetrosLetrero"/>) aparece el letrero "PRESIONA E PARA CONFIGURAR". La tecla E abre el menú (la lee el corredor).
    /// </summary>
    public class TerminalCampoTiro : MonoBehaviour
    {
        public const string TextoLetrero = "PRESIONA E PARA CONFIGURAR";
        private const float AlturaPedestal = 1.0f;
        private const float EscalaMundo = 0.01f;
        private const int TamanoLetra = 40;
        private static readonly Vector2 TamanoCaja = new Vector2(520f, 90f);

        private GameObject letrero;

        /// <summary>True si el letrero está visible ahora.</summary>
        public bool LetreroVisible => letrero != null && letrero.activeSelf;
        public Vector3 PosicionLetrero => letrero != null ? letrero.transform.position : transform.position;

        public static TerminalCampoTiro Crear(Transform sala, Vector3 local, Material holo)
        {
            GameObject raiz = new GameObject("Terminal");
            raiz.transform.SetParent(sala, false);
            raiz.transform.localPosition = local;
            // Mira hacia el jugador (que está en -X de la terminal y mira a +Z).
            raiz.transform.localRotation = Quaternion.Euler(0f, -25f, 0f);
            TerminalCampoTiro terminal = raiz.AddComponent<TerminalCampoTiro>();
            Pieza(raiz.transform, "Pedestal", PrimitiveType.Cube, new Vector3(0f, AlturaPedestal * 0.5f, 0f), new Vector3(0.5f, AlturaPedestal, 0.4f), holo);
            GameObject panel = Pieza(raiz.transform, "Panel", PrimitiveType.Cube, new Vector3(0f, AlturaPedestal + 0.3f, -0.05f), new Vector3(1.1f, 0.6f, 0.04f), holo);
            panel.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);

            terminal.letrero = new GameObject("Letrero");
            terminal.letrero.transform.SetParent(raiz.transform, false);
            terminal.letrero.transform.localPosition = new Vector3(0f, AlturaPedestal + 1.0f, 0f);
            terminal.letrero.transform.localScale = Vector3.one * EscalaMundo;
            Canvas canvas = terminal.letrero.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)terminal.letrero.transform).sizeDelta = TamanoCaja;
            Text texto = TrazoH10.Etiqueta(terminal.letrero.transform, "Texto", TextoLetrero, TamanoLetra, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(texto.rectTransform);
            terminal.letrero.SetActive(false);
            return terminal;
        }

        private void LateUpdate()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Transform quien = jugador != null ? jugador.Raiz : null;
            bool cerca = quien != null && ReglaCampoTiro.EstaCercaDeLaTerminal(quien.position, transform.position);
            if (letrero.activeSelf != cerca)
            {
                letrero.SetActive(cerca);
            }
            Camera camara = Camera.main;
            if (cerca && camara != null)
            {
                letrero.transform.rotation = Quaternion.LookRotation(letrero.transform.position - camara.transform.position, Vector3.up);
            }
        }

        private static GameObject Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 local, Vector3 escala, Material material)
        {
            GameObject objeto = GameObject.CreatePrimitive(tipo);
            objeto.name = nombre;
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = local;
            objeto.transform.localScale = escala;
            Destroy(objeto.GetComponent<Collider>());
            Renderer dibujo = objeto.GetComponent<Renderer>();
            dibujo.sharedMaterial = material;
            dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return objeto;
        }
    }
}

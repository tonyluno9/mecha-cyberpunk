using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// T-1492: marca en pantalla al dron que te dispara. Mientras el dron avisa y poco después de disparar dibuja un cuadro rojo con "DRON" sobre él;
    /// si está fuera de pantalla o detrás, el cuadro va en el borde con una flecha hacia él. Lo agrega <see cref="DronMundo"/> al activarse.
    /// Se dibuja con IMGUI para no depender de la UI de otra área (Trafico no referencia UI).
    /// </summary>
    public class MarcadorDronHud : MonoBehaviour
    {
        private const float Lado = 44f;

        private DronMundo dron;

        /// <summary>True si el marcador se está dibujando.</summary>
        public bool Visible { get; private set; }

        /// <summary>Posición del marcador en pantalla (px, origen arriba a la izquierda), si <see cref="Visible"/>.</summary>
        public Vector2 Posicion { get; private set; }

        /// <summary>True si el dron está fuera de pantalla y el marcador va en el borde.</summary>
        public bool EnBorde { get; private set; }

        /// <summary>Ángulo de la flecha del borde (0 arriba, 90 derecha).</summary>
        public float GradosFlecha { get; private set; }

        private void Awake() => dron = GetComponent<DronMundo>();

        private void Update()
        {
            Visible = false;
            if (dron == null || !dron.Amenazando)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            Camera cam = jugador != null && jugador.Camara != null ? jugador.Camara : Camera.main;
            if (cam == null)
            {
                return;
            }
            Vector3 vista = cam.WorldToViewportPoint(transform.position);
            Posicion = CalculoMarcadorDron.Colocar(vista, Screen.width, Screen.height, out bool fuera, out float grados);
            EnBorde = fuera;
            GradosFlecha = grados;
            Visible = true;
        }

        private void OnGUI()
        {
            if (!Visible || Event.current.type != EventType.Repaint)
            {
                return;
            }
            Color anterior = GUI.color;
            Color rojo = new Color(1f, 0.15f, 0.12f, 0.95f);
            GUI.color = rojo;
            Rect caja = new Rect(Posicion.x - Lado * 0.5f, Posicion.y - Lado * 0.5f, Lado, Lado);
            const float g = 3f;
            GUI.DrawTexture(new Rect(caja.x, caja.y, caja.width, g), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(caja.x, caja.yMax - g, caja.width, g), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(caja.x, caja.y, g, caja.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(caja.xMax - g, caja.y, g, caja.height), Texture2D.whiteTexture);
            if (EnBorde)
            {
                // Flecha: un cuadrito que se adelanta hacia donde está el dron.
                float rad = GradosFlecha * Mathf.Deg2Rad;
                Vector2 punta = Posicion + new Vector2(Mathf.Sin(rad), -Mathf.Cos(rad)) * (Lado * 0.5f + 12f);
                GUI.DrawTexture(new Rect(punta.x - 7f, punta.y - 7f, 14f, 14f), Texture2D.whiteTexture);
            }
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12, fontStyle = FontStyle.Bold };
            estilo.normal.textColor = rojo;
            GUI.Label(new Rect(caja.x - 10f, caja.yMax, caja.width + 20f, 18f), "DRON", estilo);
            GUI.color = anterior;
        }
    }
}

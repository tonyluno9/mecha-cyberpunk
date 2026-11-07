using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Línea de efectos de los consumibles (T-685), abajo a la izquierda, justo arriba de las casillas de objetos del HUD nuevo (T-700):
    /// mientras un efecto está activo (bebida, venda, café) muestra cuánto le queda. Los objetos y sus teclas los dibuja <see cref="HudH10"/>.
    /// Se arma en código y lo agrega <see cref="HudAPie"/> al nacer.
    /// </summary>
    public class HudConsumibles : MonoBehaviour
    {
        private const float Alto = 36f;
        private const float Margen = HudH10.Margen;
        // Arriba de las casillas de objetos: 62 de margen + 114 de casilla + 10 de aire.
        private const float DesdeAbajo = 186f;

        private HudAPie hud;
        private Text texto;
        private ConsumiblesJugador consumibles;
        private ServicioInventario inventario;
        private VidaAPie vida;

        public Text Texto => texto;
        public bool Armado => texto != null;

        private void Start()
        {
            hud = GetComponent<HudAPie>();
            if (hud == null || hud.GrupoVida == null)
            {
                enabled = false;
                return;
            }
            // Abajo a la izquierda, en el canvas del HUD (la vida y el escudo van arriba).
            var objeto = new GameObject("Consumibles", typeof(RectTransform));
            RectTransform rect = (RectTransform)objeto.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(900f, Alto);
            rect.anchoredPosition = new Vector2(Margen, DesdeAbajo);
            texto = objeto.AddComponent<Text>();
            ConfigHudH10 estilo = ConfigHudH10.Actual;
            texto.font = estilo != null && estilo.Fuente != null ? estilo.Fuente : KitUI.Estilo.Fuente;
            texto.fontSize = 24;
            texto.alignment = TextAnchor.LowerLeft;
            texto.color = new Color(0.55f, 0.88f, 0.88f, 0.95f);
            texto.raycastTarget = false;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.text = string.Empty;
        }

        private void Update()
        {
            if (texto == null)
            {
                return;
            }
            if (consumibles == null)
            {
                IJugador j = Registro<IJugador>.Actual;
                consumibles = j != null && j.Raiz != null ? j.Raiz.GetComponent<ConsumiblesJugador>() : null;
                vida = j != null && j.Raiz != null ? j.Raiz.GetComponent<VidaAPie>() : null;
                if (consumibles == null)
                {
                    texto.text = string.Empty;
                    return;
                }
            }
            IJugador actual = Registro<IJugador>.Actual;
            texto.text = actual != null && actual.Modo == ModoJugador.APie ? Linea() : string.Empty;
        }

        private string Linea()
        {
            var partes = new System.Collections.Generic.List<string>();
            if (vida != null && vida.InvulnerableRestante > 0f)
            {
                partes.Add("SIN DAÑO " + vida.InvulnerableRestante.ToString("0.0") + "s");
            }
            if (consumibles.Curando)
            {
                partes.Add("CURANDO");
            }
            if (consumibles.VelocidadRestante > 0f)
            {
                partes.Add("RAPIDO " + consumibles.VelocidadRestante.ToString("0") + "s");
            }
            return string.Join("   ", partes);
        }
    }
}

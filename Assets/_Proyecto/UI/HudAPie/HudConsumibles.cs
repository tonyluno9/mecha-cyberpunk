using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Línea del consumible elegido (T-685), abajo a la izquierda: "[TAB] PAPITAS x2 · F USAR". Si hay un efecto
    /// activo (bebida, venda, café) muestra cuánto le queda. Se arma en código y lo agrega
    /// <see cref="HudAPie"/> al nacer.
    /// </summary>
    public class HudConsumibles : MonoBehaviour
    {
        private const float Alto = 44f;
        private const float Margen = 32f;

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
            rect.sizeDelta = new Vector2(640f, Alto);
            rect.anchoredPosition = new Vector2(Margen, Margen);
            texto = objeto.AddComponent<Text>();
            texto.font = hud.TextoVida != null ? hud.TextoVida.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 32;
            texto.alignment = TextAnchor.LowerLeft;
            texto.color = new Color(1f, 0.55f, 0.15f, 1f);
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
            DatosConsumible c = consumibles.Seleccionado;
            if (c != null)
            {
                int n = Registro<ServicioInventario>.Actual != null ? Registro<ServicioInventario>.Actual.Cantidad(c) : 0;
                partes.Add("[TAB] " + c.Nombre.ToUpperInvariant() + " x" + n + " · F USAR");
            }
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

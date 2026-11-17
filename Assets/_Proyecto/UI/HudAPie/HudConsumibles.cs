using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Rueda;
using UnityEngine;
using UnityEngine.InputSystem;
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
        private Text textoConsumo;
        private readonly System.Collections.Generic.List<string> idsDisponibles = new System.Collections.Generic.List<string>();
        private ConsumiblesJugador consumibles;
        private ServicioInventario inventario;
        private VidaAPie vida;

        public Text Texto => texto;
        public bool Armado => texto != null;
        /// <summary>T-1315: la línea "[B] BEBIDA x2" del objeto que consume la tecla.</summary>
        public Text TextoConsumo => textoConsumo;
        /// <summary>T-1315: el objeto equipado que consumiría la tecla ahora, o null.</summary>
        public DatosConsumible ObjetoEquipado { get; private set; }

        private void OnEnable() => AccionConsumirEquipado.Accion.Enable();

        private void OnDisable() => AccionConsumirEquipado.Accion.Disable();

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
            // T-1315: arriba de la línea de efectos, la tecla y el objeto que consume.
            var objetoConsumo = new GameObject("ConsumoEquipado", typeof(RectTransform));
            RectTransform rectConsumo = (RectTransform)objetoConsumo.transform;
            rectConsumo.SetParent(transform, false);
            rectConsumo.anchorMin = rectConsumo.anchorMax = new Vector2(0f, 0f);
            rectConsumo.pivot = new Vector2(0f, 0f);
            rectConsumo.sizeDelta = new Vector2(900f, Alto);
            rectConsumo.anchoredPosition = new Vector2(Margen, DesdeAbajo + Alto);
            textoConsumo = objetoConsumo.AddComponent<Text>();
            textoConsumo.font = texto.font;
            textoConsumo.fontSize = texto.fontSize;
            textoConsumo.alignment = TextAnchor.LowerLeft;
            textoConsumo.color = new Color(0.78f, 0.98f, 0.98f, 0.95f);
            textoConsumo.raycastTarget = false;
            textoConsumo.horizontalOverflow = HorizontalWrapMode.Overflow;
            textoConsumo.text = string.Empty;
            Outline contorno = objetoConsumo.AddComponent<Outline>();
            contorno.effectColor = new Color(0f, 0f, 0f, 0.9f);
            contorno.effectDistance = new Vector2(2f, -2f);
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
                    textoConsumo.text = string.Empty;
                    ObjetoEquipado = null;
                    return;
                }
            }
            IJugador actual = Registro<IJugador>.Actual;
            texto.text = actual != null && actual.Modo == ModoJugador.APie ? Linea() : string.Empty;
            ActualizarConsumo(actual != null && actual.Modo == ModoJugador.APie);
        }

        // T-1315: elige el objeto equipado que consume la tecla, lo muestra con su tecla real y lo consume al pulsarla.
        private void ActualizarConsumo(bool aPie)
        {
            ObjetoEquipado = null;
            if (aPie)
            {
                var disponibles = consumibles.Disponibles;
                idsDisponibles.Clear();
                for (int i = 0; i < disponibles.Count; i++)
                {
                    idsDisponibles.Add(disponibles[i].Id);
                }
                int elegido = EquipoRueda.ElegirConsumible(idsDisponibles, i => consumibles.Sirve(disponibles[i]));
                ObjetoEquipado = elegido >= 0 ? disponibles[elegido] : null;
            }
            if (ObjetoEquipado == null)
            {
                textoConsumo.text = string.Empty;
                return;
            }
            InputAction accion = AccionConsumirEquipado.Accion;
            string tecla = TeclasHud.Texto(accion, "B").ToUpperInvariant();
            ServicioInventario servicio = Registro<ServicioInventario>.Actual;
            int cantidad = servicio != null ? servicio.Cantidad(ObjetoEquipado) : 0;
            textoConsumo.text = "[" + tecla + "] " + ObjetoEquipado.Nombre.ToUpperInvariant() + " x" + cantidad;
            if (accion.WasPressedThisFrame())
            {
                consumibles.Usar(ObjetoEquipado);
            }
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

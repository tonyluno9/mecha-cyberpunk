using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Interaccion;
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

        /// <summary>T-1492: el objeto de la ranura (0 = la de E, 1 = la de C), o null si está vacía.</summary>
        public DatosConsumible Equipado(int ranura) => ranura >= 0 && ranura < RanurasConsumibles.Cantidad ? equipados[ranura] : null;

        private readonly DatosConsumible[] equipados = new DatosConsumible[RanurasConsumibles.Cantidad];
        private readonly System.Collections.Generic.List<DatosConsumible> llevados = new System.Collections.Generic.List<DatosConsumible>();
        private InteraccionAPie interaccion;

        private void OnEnable() => AccionConsumirEquipado.Habilitar();

        private void OnDisable()
        {
            AccionConsumirEquipado.Deshabilitar();
            if (consumibles != null)
            {
                consumibles.Curado -= AlCurar;
                consumibles = null;
            }
        }

        // T-63: segundos que se muestra "+N VIDA" tras la última curación.
        private const float SegundosAviso = 2f;
        private readonly AvisoCuracion aviso = new AvisoCuracion();

        private void AlCurar(float curado) => aviso.Sumar(curado, SegundosAviso);

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
                if (consumibles != null)
                {
                    consumibles.Curado -= AlCurar;
                    consumibles.Curado += AlCurar;
                }
                vida = j != null && j.Raiz != null ? j.Raiz.GetComponent<VidaAPie>() : null;
                interaccion = j != null && j.Raiz != null ? j.Raiz.GetComponentInChildren<InteraccionAPie>() : null;
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
            ActualizarConsumo(actual != null ? actual.Modo : ModoJugador.Mecha);
        }

        // T-1492: no se usan objetos con una pantalla, el menú o la pausa abiertos (E y C se usan ahí para otras cosas).
        private static bool EntradaLibre() => !Pausa.Activa && PilaPantallas.Vacia && MenuCyberfix.Abiertos == 0 && Time.timeScale > 0.01f;

        // T-1315: elige el objeto equipado que consume la tecla, lo muestra con su tecla real y lo consume al pulsarla.
        // T-1492: las dos ranuras (E y C a pie; Z y X en carro). Muestra cada una con su tecla real, la usa al pulsarla y, con Alt apretado, cambia su objeto.
        private void ActualizarConsumo(ModoJugador modo)
        {
            bool carro = modo == ModoJugador.Carro;
            for (int r = 0; r < equipados.Length; r++)
            {
                equipados[r] = null;
            }
            if (modo == ModoJugador.APie || carro)
            {
                ResolverRanuras();
            }
            bool hayInteraccion = interaccion != null && interaccion.TipoCandidato != TipoCandidatoAPie.Ninguno;
            bool libre = EntradaLibre();
            bool alt = AccionConsumirEquipado.Cambiar.IsPressed();
            for (int r = 0; r < equipados.Length; r++)
            {
                InputAction accion = carro ? AccionConsumirEquipado.DeCarro(r) : AccionConsumirEquipado.De(r);
                if (libre)
                {
                    AccionRanura que = ReglaUsoConsumibles.Decidir(r, accion.WasPressedThisFrame(), alt, modo, hayInteraccion);
                    if (que == AccionRanura.Cambiar)
                    {
                        RanurasConsumibles.Rotar(r, idsDisponibles);
                        ResolverRanuras();
                    }
                    else if (que == AccionRanura.Usar && equipados[r] != null)
                    {
                        consumibles.Usar(equipados[r]);
                    }
                }
            }
            ObjetoEquipado = equipados[0];
            // T-1529: la línea "[E] BEBIDA x2" repetía lo que ya dicen las casillas. T-63: ahí se muestra cuánto curó el último consumible.
            aviso.Avanzar(Time.unscaledDeltaTime);
            textoConsumo.text = aviso.Visible ? "+" + Mathf.RoundToInt(aviso.Total) + " VIDA" : string.Empty;
        }

        // Qué objeto hay en cada ranura con lo que lleva ahora (sin los que se quitaron de la rueda).
        private void ResolverRanuras()
        {
            var disponibles = consumibles.Disponibles;
            idsDisponibles.Clear();
            llevados.Clear();
            for (int i = 0; i < disponibles.Count; i++)
            {
                if (EquipoRueda.Equipado(disponibles[i].Id))
                {
                    idsDisponibles.Add(disponibles[i].Id);
                    llevados.Add(disponibles[i]);
                }
            }
            string[] ids = RanurasConsumibles.Efectivos(idsDisponibles);
            for (int r = 0; r < equipados.Length; r++)
            {
                equipados[r] = null;
                for (int i = 0; ids[r] != null && i < llevados.Count; i++)
                {
                    if (llevados[i].Id == ids[r])
                    {
                        equipados[r] = llevados[i];
                        break;
                    }
                }
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

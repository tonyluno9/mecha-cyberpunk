using System;
using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Mapa;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// T43: barras de vida de los enemigos a pie. Una barra pequeña sobre la cabeza (visible solo tras recibir daño, se oculta a los
    /// segundos y al morir) y una barra grande con nombre arriba de la pantalla para los jefes.
    /// Es genérico: busca a los enemigos en <c>Registro&lt;IMarcaMapa&gt;</c> (Core: tipo Enemigo o Jefe), toma su <see cref="IVida"/> de la
    /// misma marca y escucha su evento Cambio para saber cuándo recibió daño. No referencia IA ni Tráfico. No saca barras sobre civiles
    /// (no son marcas de enemigo) ni sobre el jugador. Se crea sola al cargar la primera escena.
    /// </summary>
    public sealed class BarrasVidaEnemigos : MonoBehaviour
    {
        private const float SegundosBusqueda = 0.5f;
        private const float AlturaExtraCabeza = 0.3f;
        private const float AlturaPorDefecto = 2f;
        private static readonly Vector2 TamanoPequena = new Vector2(64f, 8f);
        private static readonly Vector2 TamanoGrande = new Vector2(620f, 22f);
        private static readonly Color Turquesa = new Color(0.30f, 0.92f, 0.90f, 1f);
        private static readonly Color Rojo = new Color(1f, 0.22f, 0.28f, 1f);
        private static readonly Color Fondo = new Color(0.01f, 0.03f, 0.05f, 0.75f);

        private sealed class Entrada
        {
            public IMarcaMapa Marca;
            public IVida Vida;
            public Component Componente;
            public Collider Colision;
            public bool EsJefe;
            public string Nombre;
            public float UltimoDanio = -1f;
            public float VidaPrevia;
            public Action AlCambiar;
            public Barra Pequena;
            public bool Visible;
        }

        private sealed class Barra
        {
            public RectTransform Raiz;
            public RectTransform Relleno;
            public Text Texto;
            public CanvasGroup Grupo;
        }

        private static BarrasVidaEnemigos actual;
        private readonly List<Entrada> entradas = new List<Entrada>();
        private readonly List<Entrada> pendientesQuitar = new List<Entrada>();
        private readonly RaycastHit[] golpes = new RaycastHit[12];
        private readonly Stack<Barra> libres = new Stack<Barra>();
        private RectTransform lienzo;
        private Barra barraJefe;
        private float proximaBusqueda;
        private int visiblesPequenas;
        private bool jefeVisible;
        private string jefeNombre = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => actual = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (actual != null)
            {
                return;
            }
            GameObject objeto = new GameObject("BarrasVidaEnemigos");
            DontDestroyOnLoad(objeto);
            objeto.AddComponent<BarrasVidaEnemigos>();
        }

        private void Awake()
        {
            actual = this;
            Canvas canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4;
            CanvasScaler escala = gameObject.AddComponent<CanvasScaler>();
            escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            escala.referenceResolution = new Vector2(1920f, 1080f);
            escala.matchWidthOrHeight = 0.5f;
            lienzo = (RectTransform)transform;
            barraJefe = CrearBarra(TamanoGrande, true);
            barraJefe.Raiz.anchorMin = barraJefe.Raiz.anchorMax = new Vector2(0.5f, 1f);
            barraJefe.Raiz.pivot = new Vector2(0.5f, 1f);
            barraJefe.Raiz.anchoredPosition = new Vector2(0f, -56f);
            barraJefe.Raiz.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < entradas.Count; i++)
            {
                Desuscribir(entradas[i]);
            }
            entradas.Clear();
            if (actual == this)
            {
                actual = null;
            }
        }

        private void LateUpdate()
        {
            if (Time.unscaledTime >= proximaBusqueda)
            {
                proximaBusqueda = Time.unscaledTime + SegundosBusqueda;
                Buscar();
            }

            Camera camara = ObtenerCamara();
            bool ocultarTodo = camara == null || Pausa.Activa || HudCinematica.Activa;
            float ahora = Time.time;
            visiblesPequenas = 0;
            Entrada mejorJefe = null;
            float mejorSeg = float.MaxValue;

            for (int i = 0; i < entradas.Count; i++)
            {
                Entrada e = entradas[i];
                if (e.Componente == null)
                {
                    pendientesQuitar.Add(e);
                    continue;
                }
                float seg = ReglaBarraEnemigo.SegundosDesdeDanio(ahora, e.UltimoDanio);
                if (e.EsJefe && !ocultarTodo && ReglaBarraEnemigo.JefeEnCombate(e.Vida.EstaKO, seg) && seg < mejorSeg)
                {
                    mejorSeg = seg;
                    mejorJefe = e;
                }
                if (ocultarTodo || !e.Marca.Visible)
                {
                    Ocultar(e);
                    continue;
                }
                ActualizarPequena(e, camara, seg);
            }

            for (int i = 0; i < pendientesQuitar.Count; i++)
            {
                Quitar(pendientesQuitar[i]);
            }
            pendientesQuitar.Clear();

            ActualizarJefe(mejorJefe, mejorSeg);
        }

        private void Buscar()
        {
            IReadOnlyList<IMarcaMapa> marcas = Registro<IMarcaMapa>.Todos;
            IJugador jugador = Registro<IJugador>.Actual;
            for (int i = 0; i < marcas.Count; i++)
            {
                IMarcaMapa marca = marcas[i];
                if (marca == null || !IconosMapa.EsEnemigo(marca.Tipo) || Existe(marca))
                {
                    continue;
                }
                IVida vida = marca as IVida;
                Component componente = marca as Component;
                if (vida == null || componente == null)
                {
                    continue;
                }
                if (jugador != null && jugador.Raiz != null && componente.transform.IsChildOf(jugador.Raiz))
                {
                    continue;
                }
                Entrada e = new Entrada
                {
                    Marca = marca,
                    Vida = vida,
                    Componente = componente,
                    Colision = componente.GetComponentInChildren<Collider>(),
                    EsJefe = marca.Tipo == TipoMarcaMapa.Jefe,
                    VidaPrevia = vida.Actual,
                };
                ILecturaRival lectura = marca as ILecturaRival;
                e.Nombre = lectura != null && !string.IsNullOrEmpty(lectura.Nombre) ? lectura.Nombre : componente.name;
                e.AlCambiar = () => AlCambiarVida(e);
                vida.Cambio += e.AlCambiar;
                entradas.Add(e);
            }
        }

        private bool Existe(IMarcaMapa marca)
        {
            for (int i = 0; i < entradas.Count; i++)
            {
                if (ReferenceEquals(entradas[i].Marca, marca))
                {
                    return true;
                }
            }
            return false;
        }

        private static void AlCambiarVida(Entrada e)
        {
            float valor = e.Vida.Actual;
            if (valor < e.VidaPrevia)
            {
                e.UltimoDanio = Time.time;
            }
            e.VidaPrevia = valor;
        }

        private void Desuscribir(Entrada e)
        {
            if (e.AlCambiar != null && e.Vida != null)
            {
                e.Vida.Cambio -= e.AlCambiar;
                e.AlCambiar = null;
            }
            Ocultar(e);
        }

        private void Quitar(Entrada e)
        {
            Desuscribir(e);
            entradas.Remove(e);
        }

        private void ActualizarPequena(Entrada e, Camera camara, float seg)
        {
            Vector3 cabeza = PosicionCabeza(e);
            Vector3 vista = camara.WorldToScreenPoint(cabeza);
            float distancia = Vector3.Distance(camara.transform.position, cabeza);
            bool enFrente = vista.z > 0.1f;
            // Solo se paga el rayo si la barra se vería por lo demás.
            bool tapado = enFrente && ReglaBarraEnemigo.BarraPequenaVisible(e.Vida.EstaKO, seg, distancia, false)
                && Tapado(camara.transform.position, cabeza, e.Componente.transform);
            if (!enFrente || !ReglaBarraEnemigo.BarraPequenaVisible(e.Vida.EstaKO, seg, distancia, tapado))
            {
                Ocultar(e);
                return;
            }
            if (e.Pequena == null)
            {
                e.Pequena = libres.Count > 0 ? libres.Pop() : CrearBarra(TamanoPequena, false);
            }
            Barra b = e.Pequena;
            if (!b.Raiz.gameObject.activeSelf)
            {
                b.Raiz.gameObject.SetActive(true);
            }
            e.Visible = true;
            visiblesPequenas++;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(lienzo, vista, null, out local);
            b.Raiz.anchoredPosition = local;
            b.Raiz.localScale = Vector3.one * ReglaBarraEnemigo.Escala(distancia);
            b.Grupo.alpha = ReglaBarraEnemigo.Opacidad(seg, ReglaBarraEnemigo.SegundosVisibleSinDanio);
            PonerFraccion(b, e.Vida);
        }

        private void ActualizarJefe(Entrada e, float seg)
        {
            jefeVisible = e != null;
            if (!jefeVisible)
            {
                if (barraJefe.Raiz.gameObject.activeSelf)
                {
                    barraJefe.Raiz.gameObject.SetActive(false);
                }
                return;
            }
            if (!barraJefe.Raiz.gameObject.activeSelf)
            {
                barraJefe.Raiz.gameObject.SetActive(true);
            }
            jefeNombre = e.Nombre;
            barraJefe.Texto.text = e.Nombre.ToUpperInvariant();
            barraJefe.Grupo.alpha = ReglaBarraEnemigo.Opacidad(seg, ReglaBarraEnemigo.SegundosVisibleJefe);
            PonerFraccion(barraJefe, e.Vida);
        }

        private static void PonerFraccion(Barra b, IVida vida)
        {
            float f = vida.Maxima > 0f ? Mathf.Clamp01(vida.Actual / vida.Maxima) : 0f;
            b.Relleno.anchorMax = new Vector2(f, 1f);
        }

        private void Ocultar(Entrada e)
        {
            if (e.Pequena != null)
            {
                e.Pequena.Raiz.gameObject.SetActive(false);
                libres.Push(e.Pequena);
                e.Pequena = null;
            }
            e.Visible = false;
        }

        private static Vector3 PosicionCabeza(Entrada e)
        {
            if (e.Colision != null && e.Colision.enabled)
            {
                Bounds b = e.Colision.bounds;
                return new Vector3(b.center.x, b.max.y + AlturaExtraCabeza, b.center.z);
            }
            return e.Componente.transform.position + Vector3.up * (AlturaPorDefecto + AlturaExtraCabeza);
        }

        private bool Tapado(Vector3 origen, Vector3 destino, Transform dueno)
        {
            Vector3 dir = destino - origen;
            float largo = dir.magnitude;
            if (largo < 0.1f)
            {
                return false;
            }
            int n = Physics.RaycastNonAlloc(origen, dir / largo, golpes, largo - 0.05f, ~0, QueryTriggerInteraction.Ignore);
            IJugador jugador = Registro<IJugador>.Actual;
            Transform raizJugador = jugador != null ? jugador.Raiz : null;
            for (int i = 0; i < n; i++)
            {
                Transform t = golpes[i].transform;
                if (t.IsChildOf(dueno))
                {
                    continue;
                }
                if (raizJugador != null && t.IsChildOf(raizJugador))
                {
                    continue;
                }
                return true;
            }
            return false;
        }

        private static Camera ObtenerCamara()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Camera c = jugador != null ? jugador.Camara : null;
            return c != null ? c : Camera.main;
        }

        private Barra CrearBarra(Vector2 tamano, bool grande)
        {
            Barra b = new Barra();
            GameObject raiz = new GameObject(grande ? "BarraJefe" : "BarraEnemigo", typeof(RectTransform), typeof(CanvasGroup));
            raiz.transform.SetParent(transform, false);
            b.Raiz = (RectTransform)raiz.transform;
            b.Raiz.sizeDelta = tamano;
            b.Grupo = raiz.GetComponent<CanvasGroup>();
            b.Grupo.blocksRaycasts = false;
            b.Grupo.interactable = false;

            Image marco = CrearImagen(b.Raiz, "Marco", Turquesa);
            Estirar(marco.rectTransform, 0f);
            Image fondo = CrearImagen(b.Raiz, "Fondo", Fondo);
            Estirar(fondo.rectTransform, grande ? 2f : 1f);
            Image vida = CrearImagen(b.Raiz, "Vida", Rojo);
            b.Relleno = vida.rectTransform;
            Estirar(b.Relleno, grande ? 3f : 1.5f);
            b.Relleno.pivot = new Vector2(0f, 0.5f);

            if (grande)
            {
                GameObject t = new GameObject("Nombre", typeof(RectTransform), typeof(Text));
                t.transform.SetParent(b.Raiz, false);
                RectTransform rt = (RectTransform)t.transform;
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 4f);
                rt.sizeDelta = new Vector2(0f, 28f);
                b.Texto = t.GetComponent<Text>();
                b.Texto.font = KitUI.Estilo.Fuente;
                b.Texto.fontSize = 22;
                b.Texto.alignment = TextAnchor.LowerCenter;
                b.Texto.color = Turquesa;
                b.Texto.raycastTarget = false;
            }
            return b;
        }

        private static Image CrearImagen(RectTransform padre, string nombre, Color color)
        {
            GameObject o = new GameObject(nombre, typeof(RectTransform), typeof(Image));
            o.transform.SetParent(padre, false);
            Image img = o.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static void Estirar(RectTransform rt, float margen)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margen, margen);
            rt.offsetMax = new Vector2(-margen, -margen);
        }

        /// <summary>Depuración: cuántas barras hay, de quién y cuáles se ven. Se invoca por reflexión.</summary>
        public static string DepurarBarras()
        {
            if (actual == null)
            {
                return "BarrasVidaEnemigos: no existe todavia (se crea al cargar la primera escena)";
            }
            StringBuilder sb = new StringBuilder();
            sb.Append("BarrasVidaEnemigos: seguidos=").Append(actual.entradas.Count)
                .Append(" pequenasVisibles=").Append(actual.visiblesPequenas)
                .Append(" jefeGrande=").Append(actual.jefeVisible ? actual.jefeNombre : "no");
            float ahora = Time.time;
            for (int i = 0; i < actual.entradas.Count; i++)
            {
                Entrada e = actual.entradas[i];
                if (e.Componente == null)
                {
                    continue;
                }
                float seg = ReglaBarraEnemigo.SegundosDesdeDanio(ahora, e.UltimoDanio);
                sb.Append("\n  ").Append(e.Nombre).Append(e.EsJefe ? " [JEFE]" : "")
                    .Append(" vida=").Append(e.Vida.Actual.ToString("0")).Append('/').Append(e.Vida.Maxima.ToString("0"))
                    .Append(" sinDanio=").Append(seg > 9999f ? "nunca" : seg.ToString("0.0") + "s")
                    .Append(" visible=").Append(e.Visible);
            }
            return sb.ToString();
        }
    }
}

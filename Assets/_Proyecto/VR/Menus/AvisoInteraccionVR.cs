using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Hud;
using TMPro;
using UnityEngine;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// V6: en VR el aviso de interactuar del HUD de pantalla (el cuadrito "E · Hablar" del HUD H10) no se ve. Este componente lo pone como un
    /// letrero chico de TextMeshPro en el mundo, flotando sobre lo que se usaría con B (el candidato de <see cref="InteraccionAPie"/>: Tino, máquinas,
    /// el Doc, la terminal del campo de tiro, puertas...) y de cara a la cabeza, con la tecla del control ("B · Hablar", <see cref="TeclaVR"/>).
    /// Los carros y motos dicen <see cref="MenusVR.TextoNoDisponible"/>. Lo que no se puede usar no sale aquí: ese aviso ya lo muestra el HUD a pie
    /// en el panel frente a la cabeza (MenusVR). Se agrega solo al jugador cuando el rig VR está armado (<see cref="AdaptadorVR.Listo"/>);
    /// sin VR no existe y en PC nada cambia.
    /// </summary>
    [DefaultExecutionOrder(160)]
    public class AvisoInteraccionVR : MonoBehaviour
    {
        public const string NombreLetrero = "LetreroInteraccionVR";
        /// <summary>Tamaño de letra de TextMeshPro 3D (1 ≈ 0.1 m de renglón): unos 4 cm de mayúscula a la escala 1.</summary>
        private const float TamanoLetra = 0.6f;
        private const float GrosorContorno = 0.2f;
        private const int OrdenDibujo = 5;
        private const int MaximoSubidas = 2;
        private static readonly Color ColorTexto = new Color(0.55f, 1f, 0.95f, 1f);
        private static readonly Color ColorNoDisponible = new Color(1f, 0.55f, 0.3f, 1f);
        private static readonly Color ColorContorno = new Color(0f, 0.05f, 0.08f, 1f);

        private AdaptadorVR adaptador;
        private InteraccionAPie interaccion;
        private JugadorAPie jugador;
        private GameObject letrero;
        private TextMeshPro texto;
        private object candidato;
        private Vector3 puntoCandidato;
        private readonly List<Renderer> piezas = new List<Renderer>();
        private readonly List<Collider> choques = new List<Collider>();
        private readonly List<Renderer> temporalRenderers = new List<Renderer>();
        private readonly List<Collider> temporalColliders = new List<Collider>();

        /// <summary>True si el letrero se ve ahora.</summary>
        public bool Visible => letrero != null && letrero.activeSelf;

        /// <summary>El texto del letrero ("B · Hablar").</summary>
        public string Texto => texto != null ? texto.text : string.Empty;

        /// <summary>El letrero (para las pruebas); null si no se creó.</summary>
        public Transform Letrero => letrero != null ? letrero.transform : null;

        // ---- Arranque ----

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RevisarActual()
        {
            AdaptadorVR.Listo -= AlListo;
            AdaptadorVR.Listo += AlListo;
            if (AdaptadorVR.Actual != null && AdaptadorVR.Actual.Armado)
            {
                AlListo(AdaptadorVR.Actual);
            }
        }

        /// <summary>Agrega el letrero al jugador del rig (no duplica). Público para las pruebas.</summary>
        public static void AlListo(AdaptadorVR rig)
        {
            if (rig != null && rig.GetComponent<AvisoInteraccionVR>() == null && rig.GetComponent<InteraccionAPie>() != null)
            {
                rig.gameObject.AddComponent<AvisoInteraccionVR>();
            }
        }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            interaccion = GetComponent<InteraccionAPie>();
            jugador = GetComponent<JugadorAPie>();
            if (adaptador == null || interaccion == null)
            {
                Debug.LogError("[VR] AvisoInteraccionVR sin AdaptadorVR o InteraccionAPie en " + name, this);
                enabled = false;
                return;
            }
            CrearLetrero();
        }

        // Fuera del jugador: el adaptador deja en "solo sombra" todo renderer que cuelga del jugador.
        private void CrearLetrero()
        {
            letrero = new GameObject(NombreLetrero);
            texto = letrero.AddComponent<TextMeshPro>();
            texto.text = string.Empty;
            texto.fontSize = TamanoLetra;
            texto.alignment = TextAlignmentOptions.Center;
            texto.textWrappingMode = TextWrappingModes.NoWrap;
            texto.overflowMode = TextOverflowModes.Overflow;
            texto.fontStyle = FontStyles.Bold;
            texto.color = ColorTexto;
            texto.sortingOrder = OrdenDibujo;
            texto.rectTransform.sizeDelta = new Vector2(2f, 0.3f);
            if (texto.font == null)
            {
                Debug.LogWarning("[VR] TextMeshPro sin fuente por defecto (TMP Settings): el letrero de interactuar no se verá.");
            }
            else
            {
                texto.outlineWidth = GrosorContorno;
                texto.outlineColor = ColorContorno;
            }
            MeshRenderer dibujo = letrero.GetComponent<MeshRenderer>();
            if (dibujo != null)
            {
                dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                dibujo.receiveShadows = false;
            }
            letrero.SetActive(false);
        }

        private void OnDisable()
        {
            if (letrero != null)
            {
                letrero.SetActive(false);
            }
            candidato = null;
        }

        private void OnDestroy()
        {
            if (letrero != null)
            {
                Destroy(letrero);
            }
        }

        private void LateUpdate()
        {
            if (letrero == null)
            {
                // Se fue con la escena en la que nació (el letrero no cuelga del jugador): se vuelve a hacer.
                CrearLetrero();
                candidato = null;
            }
            bool noDisponible = false;
            string mensaje = string.Empty;
            bool puede = ModoVR.Activo && adaptador != null && adaptador.Armado && adaptador.Camara != null && !Pausa.Activa
                && !MenusVR.MenuAbierto && (jugador == null || jugador.Modo == ModoJugador.APie);
            if (puede)
            {
                mensaje = Mensaje(out noDisponible);
            }
            object actual = mensaje.Length > 0 ? Candidato() : null;
            if (actual == null)
            {
                if (letrero.activeSelf)
                {
                    letrero.SetActive(false);
                }
                candidato = null;
                return;
            }
            Vector3 punto = PuntoCandidato();
            if (!ReferenceEquals(actual, candidato))
            {
                candidato = actual;
                BuscarPiezas(actual as Component, punto);
            }
            puntoCandidato = punto;

            bool hay = MedirPiezas(punto, out Bounds bounds);
            Vector3 posicion = ReglaAvisoInteraccionVR.Posicion(hay, bounds, punto);
            Vector3 cabeza = adaptador.Camara.transform.position;
            letrero.transform.SetPositionAndRotation(posicion, ReglaAvisoInteraccionVR.Giro(posicion, cabeza));
            letrero.transform.localScale = Vector3.one * ReglaAvisoInteraccionVR.Escala(Vector3.Distance(posicion, cabeza));
            if (texto.text != mensaje)
            {
                texto.text = mensaje;
            }
            texto.color = noDisponible ? ColorNoDisponible : ColorTexto;
            if (!letrero.activeSelf)
            {
                letrero.SetActive(true);
            }
        }

        /// <summary>
        /// El texto del letrero para el candidato de ahora (vacío si no hay o no se puede usar): lo mismo que dice el HUD H10 en pantalla
        /// (<see cref="ReglaAyudasH10.TextoInteraccion"/>) pero con B; los carros y motos, "NO DISPONIBLE EN VR".
        /// </summary>
        private string Mensaje(out bool noDisponible)
        {
            noDisponible = false;
            var c = new ContextoAyudaH10 { Modo = ModoJugador.APie };
            switch (interaccion.TipoCandidato)
            {
                case TipoCandidatoAPie.Puerta:
                    c.Candidato = TipoCandidatoInteraccion.Puerta;
                    c.NombreCandidato = interaccion.LugarCandidato != null ? interaccion.LugarCandidato.Nombre : null;
                    break;
                case TipoCandidatoAPie.Montable:
                    IMontable montable = interaccion.MontableCandidato;
                    if (montable == null)
                    {
                        return string.Empty;
                    }
                    if (montable.Modo == ModoJugador.Carro)
                    {
                        // VR4: en VR no se maneja; al pulsar B sale el mismo aviso frente a la cabeza.
                        noDisponible = true;
                        return MenusVR.TextoNoDisponible;
                    }
                    c.Candidato = TipoCandidatoInteraccion.Montable;
                    c.NombreCandidato = montable.Nombre;
                    break;
                case TipoCandidatoAPie.Interactuable:
                    IInteractuable usable = interaccion.InteractuableCandidato;
                    if (usable == null)
                    {
                        return string.Empty;
                    }
                    c.Candidato = TipoCandidatoInteraccion.Interactuable;
                    c.TextoInteractuable = usable.Texto;
                    c.PuedeUsarInteractuable = usable.PuedeUsar;
                    break;
                default:
                    return string.Empty;
            }
            return TeclaVR.ConTecla(ReglaAyudasH10.TextoInteraccion(c));
        }

        private object Candidato()
        {
            switch (interaccion.TipoCandidato)
            {
                case TipoCandidatoAPie.Puerta: return interaccion.PuertaCandidata;
                case TipoCandidatoAPie.Montable: return interaccion.MontableCandidato;
                case TipoCandidatoAPie.Interactuable: return interaccion.InteractuableCandidato;
                default: return null;
            }
        }

        private Vector3 PuntoCandidato()
        {
            switch (interaccion.TipoCandidato)
            {
                case TipoCandidatoAPie.Puerta:
                    return interaccion.PuertaCandidata != null ? interaccion.PuertaCandidata.Posicion : puntoCandidato;
                case TipoCandidatoAPie.Montable:
                    return interaccion.MontableCandidato != null ? interaccion.MontableCandidato.PuntoSubida : puntoCandidato;
                case TipoCandidatoAPie.Interactuable:
                    return interaccion.InteractuableCandidato != null ? interaccion.InteractuableCandidato.Posicion : puntoCandidato;
                default:
                    return puntoCandidato;
            }
        }

        // Las piezas (renderers, o colliders si no hay) del objeto que se usa, cerca de su punto. Si el componente no tiene, sube hasta dos padres.
        private void BuscarPiezas(Component objeto, Vector3 punto)
        {
            piezas.Clear();
            choques.Clear();
            if (objeto == null)
            {
                return;
            }
            Transform t = objeto.transform;
            for (int nivel = 0; t != null && nivel <= MaximoSubidas && piezas.Count == 0 && choques.Count == 0; nivel++, t = t.parent)
            {
                if (t == transform || transform.IsChildOf(t))
                {
                    break; // nunca medir al jugador ni a lo que lo contiene
                }
                t.GetComponentsInChildren(false, temporalRenderers);
                foreach (Renderer r in temporalRenderers)
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || r.gameObject == letrero)
                    {
                        continue;
                    }
                    if (r.enabled && ReglaAvisoInteraccionVR.PiezaCuenta(r.bounds, punto))
                    {
                        piezas.Add(r);
                    }
                }
                if (piezas.Count > 0)
                {
                    break;
                }
                t.GetComponentsInChildren(false, temporalColliders);
                foreach (Collider c in temporalColliders)
                {
                    if (c.enabled && ReglaAvisoInteraccionVR.PiezaCuenta(c.bounds, punto))
                    {
                        choques.Add(c);
                    }
                }
            }
            temporalRenderers.Clear();
            temporalColliders.Clear();
        }

        // Bounds de las piezas en este cuadro (los NPC se mueven y animan).
        private bool MedirPiezas(Vector3 punto, out Bounds bounds)
        {
            bounds = default;
            bool hay = false;
            foreach (Renderer r in piezas)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy)
                {
                    continue;
                }
                Juntar(ref bounds, ref hay, r.bounds);
            }
            if (!hay)
            {
                foreach (Collider c in choques)
                {
                    if (c == null || !c.enabled || !c.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    Juntar(ref bounds, ref hay, c.bounds);
                }
            }
            return hay;
        }

        private static void Juntar(ref Bounds bounds, ref bool hay, Bounds pieza)
        {
            if (!hay)
            {
                bounds = pieza;
                hay = true;
            }
            else
            {
                bounds.Encapsulate(pieza);
            }
        }
    }
}

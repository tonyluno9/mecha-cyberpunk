using System.Collections.Generic;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR2: el arma de fuego en la mano (la de <see cref="ArmasAPie"/>, con su modelo real: W01 con su corredera, cargador y mira) va en la mano
    /// derecha del rig, con el cañón hacia adelante del control. El gatillo derecho dispara por <see cref="PistolaAPie.DispararExterno"/>: la bala
    /// sale de la boca del arma hacia donde apunta, con el mismo cargador, cadencia, daño, sonidos, impactos y trazos que con el ratón.
    /// Recargar es A (binding de VR1 sobre la acción Recargar, que ya lee PistolaAPie). Mientras se ve el arma se oculta el modelo del control derecho.
    /// Al armarse pone la pistola en la mano si el jugador estaba con los puños.
    /// </summary>
    [DefaultExecutionOrder(120)]
    public class PistolaVR : MonoBehaviour
    {
        /// <summary>Dónde queda la empuñadura (pivote del arma) en el espacio del control (m).</summary>
        public static readonly Vector3 PosicionEnMano = Vector3.zero;
        /// <summary>Giro del arma respecto al control, en X (°; negativo levanta el cañón). El grip de OpenXR ya apunta como el índice estirado.</summary>
        public const float InclinacionGrados = 0f;
        /// <summary>Si es true, al armarse el rig se pone la pistola en la mano (si se tiene y se estaba con los puños).</summary>
        public static bool EquiparAlArmar = true;

        private const float LargoBocaSinModelo = 0.15f;
        private const float DuracionDestello = 0.05f;
        private const float MaximaIntensidadLuz = 1.2f;
        private const float MaximoAlcanceLuz = 2.5f;
        private const float AmplitudVibracionTiro = 0.45f;
        private const float SegundosVibracionTiro = 0.06f;
        private const float SegundosParaEquipar = 3f;
        private const string NombreModeloControl = "Modelo";
        private const string NombreBateriaNueva = "Bateria_Nueva";

        private sealed class ArmaEnMano
        {
            public GameObject Sosten;
            public Transform Boca;
            public Light Luz;
        }

        private AdaptadorVR adaptador;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private ManoVR mano;
        private Transform bocaMano;
        private readonly Dictionary<ArmaAPie, ArmaEnMano> copias = new Dictionary<ArmaAPie, ArmaEnMano>();
        private ArmaEnMano mostrada;
        private ArmaAPie armaMostrada = ArmaAPie.Punos;
        private float finDestello = -1f;
        private float segundosEquipando;
        private bool pendienteEquipar;

        /// <summary>Raíz del arma visible en la mano derecha (null con los puños).</summary>
        public Transform Arma => mostrada != null ? mostrada.Sosten.transform : null;
        /// <summary>De dónde salen las balas: la boca del arma visible (o un punto delante del control si el arma no tiene modelo).</summary>
        public Transform Boca => mostrada != null ? mostrada.Boca : bocaMano;
        public bool ArmaVisible => mostrada != null && mostrada.Sosten.activeSelf;
        public ArmaAPie ArmaMostrada => armaMostrada;
        /// <summary>Tiros que salieron con el gatillo VR.</summary>
        public int DisparosHechos { get; private set; }
        /// <summary>VR5: true para no poner el arma activa en la mano (la granada solo se ve mientras el grip la toma, <see cref="ArmasEspecialesVR"/>).</summary>
        public bool OcultarArma { get; set; }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            if (adaptador == null || pistola == null || armas == null)
            {
                Debug.LogWarning("[VR] PistolaVR sin AdaptadorVR, PistolaAPie o ArmasAPie en " + name + ".", this);
                enabled = false;
                return;
            }
            pendienteEquipar = EquiparAlArmar;
        }

        // En LateUpdate: después de que AdaptadorVR acomoda el rig (el cuerpo puede girar en Update y arrastrar la mano un cuadro).
        private void LateUpdate()
        {
            if (adaptador == null || !adaptador.Armado)
            {
                if (adaptador == null)
                {
                    Destroy(this);
                }
                return;
            }
            Equipar();
            Actualizar();
            if (QuiereDisparar())
            {
                Disparar();
            }
            if (mostrada != null && mostrada.Luz != null)
            {
                mostrada.Luz.enabled = Time.time < finDestello;
            }
        }

        // Con los puños al armarse, saca la pistola (solo una vez; luego manda la rueda de armas).
        private void Equipar()
        {
            if (!pendienteEquipar)
            {
                return;
            }
            segundosEquipando += Time.deltaTime;
            if (armas.ArmaActiva != ArmaAPie.Punos || segundosEquipando > SegundosParaEquipar)
            {
                pendienteEquipar = false;
                return;
            }
            if (armas.PuedeCambiar && armas.TieneArma(ArmaAPie.Pistola) && armas.Equipar(ArmaAPie.Pistola))
            {
                pendienteEquipar = false;
            }
        }

        private bool QuiereDisparar()
        {
            // VR4: con un menú abierto o el láser sobre un botón, el gatillo es clic de UI, no disparo.
            if (MechaCyberpunk.VR.Menus.MenusVR.BloqueaDisparo)
            {
                return false;
            }
            if (!PistolaAPie.EsDeFuego(armas.ArmaActiva))
            {
                return false;
            }
            DatosArma d = pistola.DatosActuales;
            return d != null && d.Automatica ? EntradaVR.Sostenido(EntradaVR.GatilloDerecho) : EntradaVR.Pulso(EntradaVR.GatilloDerecho);
        }

        /// <summary>Un tiro desde el cañón del arma en la mano derecha. Devuelve si salió (cadencia, balas y recarga mandan).</summary>
        public bool Disparar()
        {
            Actualizar();
            if (!PistolaAPie.EsDeFuego(armas.ArmaActiva) || !pistola.DispararExterno())
            {
                return false;
            }
            DisparosHechos++;
            finDestello = Time.time + DuracionDestello;
            if (mano != null)
            {
                mano.Vibrar(AmplitudVibracionTiro, SegundosVibracionTiro);
            }
            return true;
        }

        /// <summary>Pone en la mano el arma activa (o nada con los puños) y le da a PistolaAPie la boca de donde salen las balas.</summary>
        public void Actualizar()
        {
            if (mano == null)
            {
                mano = adaptador.ManoDerecha;
                if (mano == null)
                {
                    return;
                }
                bocaMano = new GameObject("BocaManoVR").transform;
                bocaMano.SetParent(mano.transform, false);
                bocaMano.localPosition = Vector3.forward * LargoBocaSinModelo;
            }
            ArmaAPie activa = armas.ArmaActiva;
            ArmaEnMano nueva = PistolaAPie.EsDeFuego(activa) && !OcultarArma ? Obtener(activa) : null;
            if (nueva != mostrada)
            {
                if (mostrada != null)
                {
                    mostrada.Sosten.SetActive(false);
                    if (mostrada.Luz != null)
                    {
                        mostrada.Luz.enabled = false;
                    }
                }
                mostrada = nueva;
                if (mostrada != null)
                {
                    mostrada.Sosten.SetActive(true);
                }
                MostrarControl(mostrada == null);
            }
            armaMostrada = mostrada != null ? activa : ArmaAPie.Punos;
            // En VR el ratón no dispara: siempre hay boca externa (con los puños PistolaAPie no dispara de todos modos).
            pistola.BocaExterna = Boca;
        }

        private ArmaEnMano Obtener(ArmaAPie arma)
        {
            if (copias.TryGetValue(arma, out ArmaEnMano copia))
            {
                return copia;
            }
            copia = Crear(arma);
            copias[arma] = copia;
            return copia;
        }

        // Copia el visual del arma del cuerpo (el modelo real, con sus scripts de animación) en la mano derecha del rig.
        private ArmaEnMano Crear(ArmaAPie arma)
        {
            GameObject original = armas.Visual(arma);
            if (original == null)
            {
                return null;
            }
            var sosten = new GameObject("ArmaVR_" + arma);
            sosten.SetActive(false);
            sosten.transform.SetParent(mano.transform, false);
            sosten.transform.localPosition = PosicionEnMano;
            sosten.transform.localRotation = Quaternion.Euler(InclinacionGrados, 0f, 0f);
            sosten.AddComponent<IgnorarOcultarVR>();

            GameObject visual = Instantiate(original, sosten.transform, false);
            visual.name = original.name;
            visual.SetActive(true);
            visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            visual.transform.localScale = Dividir(original.transform.lossyScale, sosten.transform.lossyScale);
            foreach (Collider c in visual.GetComponentsInChildren<Collider>(true))
            {
                Destroy(c);
            }
            foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
            {
                // AnimacionPistola crea su cargador de repuesto en Awake: el que venía copiado sobra.
                if (t != visual.transform && t.name == NombreBateriaNueva)
                {
                    Destroy(t.gameObject);
                }
            }
            // El cuerpo se pone en solo sombra (AdaptadorVR): la copia sí se ve.
            foreach (Renderer r in visual.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            Light luz = null;
            foreach (Light l in visual.GetComponentsInChildren<Light>(true))
            {
                l.enabled = false;
                l.intensity = Mathf.Min(l.intensity, MaximaIntensidadLuz);
                l.range = Mathf.Min(l.range, MaximoAlcanceLuz);
                if (luz == null)
                {
                    luz = l;
                }
            }

            // La boca: el mismo punto que en el arma del cuerpo (su luz), con el cañón (+Z del arma) como dirección.
            var boca = new GameObject("BocaVR").transform;
            boca.SetParent(sosten.transform, false);
            Transform bocaOriginal = pistola.BocaDe(arma);
            if (bocaOriginal != null && bocaOriginal != original.transform && bocaOriginal.IsChildOf(original.transform))
            {
                boca.position = visual.transform.TransformPoint(original.transform.InverseTransformPoint(bocaOriginal.position));
            }
            else
            {
                boca.localPosition = Vector3.forward * LargoBocaSinModelo;
            }
            boca.localRotation = Quaternion.identity;
            return new ArmaEnMano { Sosten = sosten, Boca = boca, Luz = luz };
        }

        private static Vector3 Dividir(Vector3 a, Vector3 b)
        {
            return new Vector3(
                Mathf.Abs(b.x) > 1e-5f ? a.x / b.x : a.x,
                Mathf.Abs(b.y) > 1e-5f ? a.y / b.y : a.y,
                Mathf.Abs(b.z) > 1e-5f ? a.z / b.z : a.z);
        }

        private void MostrarControl(bool visible)
        {
            Transform modelo = mano != null ? mano.transform.Find(NombreModeloControl) : null;
            if (modelo != null)
            {
                modelo.gameObject.SetActive(visible);
            }
        }

        private void OnDisable()
        {
            if (pistola != null && pistola.BocaExterna != null && (pistola.BocaExterna == bocaMano || EsBocaPropia(pistola.BocaExterna)))
            {
                pistola.BocaExterna = null;
            }
            if (mostrada != null && mostrada.Sosten != null)
            {
                mostrada.Sosten.SetActive(false);
            }
            mostrada = null;
            MostrarControl(true);
        }

        private bool EsBocaPropia(Transform boca)
        {
            foreach (ArmaEnMano c in copias.Values)
            {
                if (c != null && c.Boca == boca)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnDestroy()
        {
            foreach (ArmaEnMano c in copias.Values)
            {
                if (c != null && c.Sosten != null)
                {
                    Destroy(c.Sosten);
                }
            }
            copias.Clear();
            if (bocaMano != null)
            {
                Destroy(bocaMano.gameObject);
            }
        }
    }
}

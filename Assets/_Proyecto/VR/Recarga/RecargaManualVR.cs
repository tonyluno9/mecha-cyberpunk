using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.VR.Menus;
using UnityEngine;
using UnityEngine.Rendering;

namespace MechaCyberpunk.VR
{
    /// <summary>
    /// VR27: recarga a mano. A (derecho) suelta el cargador del arma en la mano (una copia de su pieza "Bateria"/cargador, o de un pedazo del arma
    /// si no tiene, cae con física y suena); mientras está fuera el arma no dispara. La mano izquierda en la cadera izquierda + grip izquierdo toma uno
    /// nuevo (aparece en la mano); acercarlo a menos de <see cref="ReglaRecargaManualVR.DistanciaMeter"/> del arma lo mete: suena la recarga, vibra y
    /// recarga de verdad con la reserva (<see cref="CargadorPistola.EmpezarRecarga"/>, más rápida). Sin balas de reserva (o lleno) A no suelta nada.
    /// Para no quedar atorado: A otra vez con el cargador fuera recarga como en PC, y cambiar de arma lo vuelve a poner. La lógica está en
    /// <see cref="ReglaRecargaManualVR"/>. Lo agrega <see cref="ArranqueCombateVR"/>.
    /// </summary>
    [DefaultExecutionOrder(105)]
    public class RecargaManualVR : MonoBehaviour
    {
        /// <summary>Con el cargador metido a mano la recarga dura esto de la normal (el gesto ya tomó su tiempo).</summary>
        public const float FactorRecargaManual = 0.25f;

        private const string NombrePieza = "Bateria";
        private const string NombreRepuesto = "Bateria_Nueva";
        private const float EscalaSinPieza = 0.4f;
        private const float MasaCargador = 0.25f;
        private const float SegundosEnPiso = 10f;
        private const float LadoMinimoCaja = 0.02f;
        private const float AmplitudSoltar = 0.3f;
        private const float SegundosSoltar = 0.05f;
        private const float AmplitudTomar = 0.25f;
        private const float SegundosTomar = 0.05f;
        private const float AmplitudMeter = 0.7f;
        private const float SegundosMeter = 0.1f;

        private readonly ReglaRecargaManualVR regla = new ReglaRecargaManualVR();
        private AdaptadorVR adaptador;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private PistolaVR pistolaVR;
        private ArmaAPie armaAnterior = ArmaAPie.Punos;
        private GameObject piezaOculta;
        private GameObject enMano;
        private CargadorPistola cargadorRapido;
        private float factorAntes = 1f;

        public EstadoCargadorVR Estado => regla.Estado;
        /// <summary>El cargador nuevo en la mano izquierda (null si no hay).</summary>
        public GameObject CargadorEnMano => enMano;
        /// <summary>El último cargador que cayó al piso (para las pruebas).</summary>
        public GameObject UltimoSoltado { get; private set; }
        /// <summary>Cargadores metidos a mano.</summary>
        public int Metidos { get; private set; }

        private void Awake()
        {
            adaptador = GetComponent<AdaptadorVR>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            pistolaVR = GetComponent<PistolaVR>();
            if (adaptador == null || pistola == null || armas == null || pistolaVR == null)
            {
                Debug.LogWarning("[VR] RecargaManualVR sin AdaptadorVR, PistolaAPie, ArmasAPie o PistolaVR en " + name + ".", this);
                enabled = false;
            }
        }

        private void OnDisable()
        {
            Reponer();
            RestaurarFactor();
        }

        private void Update()
        {
            if (adaptador == null)
            {
                Destroy(this);
                return;
            }
            if (!adaptador.Armado)
            {
                return;
            }
            if (cargadorRapido != null && !cargadorRapido.Recargando)
            {
                RestaurarFactor();
            }
            ArmaAPie activa = armas.ArmaActiva;
            if (activa != armaAnterior)
            {
                armaAnterior = activa;
                Reponer();
            }
            bool conCargador = PistolaAPie.EsDeFuego(activa) && activa != ArmaAPie.Granada && pistola.Cargador != null;
            if (!conCargador)
            {
                if (regla.Estado != EstadoCargadorVR.EnArma)
                {
                    Reponer();
                }
                pistolaVR.SinCargador = false;
                return;
            }
            if (Pausa.Activa || MenusVR.MenuAbierto || (adaptador.Jugador != null && adaptador.Jugador.Modo != ModoJugador.APie))
            {
                return; // en el carro A es del manejo
            }
            ManoVR izq = adaptador.ManoIzquierda;
            bool puede = ReglaRecargaManualVR.PuedeRecargar(pistola.Balas, pistola.TamanoCargador, pistola.Reserva, pistola.Recargando);
            bool pulsoA = EntradaVR.Pulso(EntradaVR.BotonA);
            bool pulsoGrip = EntradaVR.Pulso(EntradaVR.GripIzquierdo) && !MechaCyberpunk.VR.Guia.GestosProtesisVR.EnPose;
            bool enCadera = pulsoGrip && GestosArmaVR.ZonaDe(adaptador, izq) == ZonaArmaVR.CaderaIzquierda;
            float distancia = regla.Estado == EstadoCargadorVR.EnMano && izq != null ? Vector3.Distance(izq.Palma, PuntoArma()) : float.MaxValue;
            if (enCadera && regla.Estado == EstadoCargadorVR.Fuera)
            {
                GestosArmaVR.TomarGrip(false);
            }
            AccionRecargaVR accion = regla.Avanzar(puede, pulsoA, pulsoGrip, EntradaVR.Sostenido(EntradaVR.GripIzquierdo), enCadera, distancia);
            switch (accion)
            {
                case AccionRecargaVR.SoltarCargador:
                    SoltarDelArma();
                    break;
                case AccionRecargaVR.TomarCargador:
                    TomarNuevo(izq);
                    break;
                case AccionRecargaVR.TirarCargador:
                    TirarDeLaMano(izq);
                    break;
                case AccionRecargaVR.MeterCargador:
                    Meter(izq);
                    break;
                case AccionRecargaVR.RecargarDirecto:
                    Reponer();
                    Recargar(1f);
                    break;
            }
            pistolaVR.SinCargador = regla.Estado != EstadoCargadorVR.EnArma;
        }

        // La empuñadura del arma en la mano derecha (donde entra el cargador).
        private Vector3 PuntoArma()
        {
            if (pistolaVR.Arma != null)
            {
                return pistolaVR.Arma.position;
            }
            return adaptador.ManoDerecha != null ? adaptador.ManoDerecha.transform.position : transform.position;
        }

        private void SoltarDelArma()
        {
            Transform pieza = BuscarPieza();
            GameObject copia = Copiar(pieza != null ? pieza : PiezaSustituta(), pieza == null);
            if (pieza != null)
            {
                piezaOculta = pieza.gameObject;
                piezaOculta.SetActive(false);
            }
            if (copia != null)
            {
                Vector3 velocidad = adaptador.ManoDerecha != null ? adaptador.ManoDerecha.Velocidad : Vector3.zero;
                Caer(copia, velocidad);
                UltimoSoltado = copia;
                Sonidos.ReproducirEn(SonidosArmasT1472.CasquilloGrande, copia.transform.position);
            }
            VibrarMano(adaptador.ManoDerecha, AmplitudSoltar, SegundosSoltar);
        }

        private void TomarNuevo(ManoVR izq)
        {
            if (izq == null)
            {
                return;
            }
            Transform pieza = BuscarPieza(true);
            enMano = Copiar(pieza != null ? pieza : PiezaSustituta(), pieza == null);
            if (enMano != null)
            {
                enMano.transform.SetPositionAndRotation(izq.Palma, izq.transform.rotation);
                enMano.transform.SetParent(izq.transform, true);
            }
            Sonidos.Reproducir(SonidosArmasT1472.Sacar);
            VibrarMano(izq, AmplitudTomar, SegundosTomar);
        }

        private void TirarDeLaMano(ManoVR izq)
        {
            if (enMano == null)
            {
                return;
            }
            enMano.transform.SetParent(null, true);
            Caer(enMano, izq != null ? izq.Velocidad : Vector3.zero);
            UltimoSoltado = enMano;
            enMano = null;
        }

        private void Meter(ManoVR izq)
        {
            Reponer();
            Metidos++;
            Recargar(FactorRecargaManual);
            VibrarMano(izq, AmplitudMeter, SegundosMeter);
            VibrarMano(adaptador.ManoDerecha, AmplitudMeter, SegundosMeter);
        }

        // Recarga real por la API del cargador (la reserva y la animación como en PC); con factor < 1 dura menos (se restaura al terminar).
        private bool Recargar(float factor)
        {
            CargadorPistola c = pistola.Cargador;
            if (c == null)
            {
                return false;
            }
            RestaurarFactor();
            factorAntes = c.FactorRecarga;
            c.FactorRecarga = factorAntes * factor;
            cargadorRapido = c;
            bool empezo = c.EmpezarRecarga();
            if (!empezo)
            {
                RestaurarFactor();
                return false;
            }
            Sonidos.Reproducir(PistolaAPie.IdRecarga(armas.ArmaActiva));
            return true;
        }

        private void RestaurarFactor()
        {
            if (cargadorRapido != null)
            {
                cargadorRapido.FactorRecarga = factorAntes;
                cargadorRapido = null;
            }
        }

        // El cargador vuelve al arma: se ve la pieza y se quita el de la mano.
        private void Reponer()
        {
            regla.Reiniciar();
            if (piezaOculta != null)
            {
                piezaOculta.SetActive(true);
                piezaOculta = null;
            }
            if (enMano != null)
            {
                Destroy(enMano);
                enMano = null;
            }
            if (pistolaVR != null)
            {
                pistolaVR.SinCargador = false;
            }
        }

        // La pieza del cargador en el arma de la mano: "Bateria" (W01) o algo que se llame cargador/batería/magazine.
        private Transform BuscarPieza(bool aunqueOculta = false)
        {
            Transform arma = pistolaVR.Arma;
            if (arma == null)
            {
                return null;
            }
            Transform parecida = null;
            foreach (Transform t in arma.GetComponentsInChildren<Transform>(true))
            {
                if (t == arma || t.name == NombreRepuesto || (!aunqueOculta && !t.gameObject.activeInHierarchy))
                {
                    continue;
                }
                if (t.GetComponentInChildren<Renderer>(true) == null)
                {
                    continue;
                }
                if (t.name == NombrePieza)
                {
                    return t;
                }
                string n = t.name.ToLowerInvariant();
                if (parecida == null && (n.Contains("cargador") || n.Contains("bateria") || n.Contains("magazine") || n.Contains("_mag")))
                {
                    parecida = t;
                }
            }
            return parecida;
        }

        // Sin pieza de cargador: el modelo del arma entero (se copia más chico).
        private Transform PiezaSustituta()
        {
            Transform arma = pistolaVR.Arma;
            return arma != null && arma.childCount > 0 ? arma.GetChild(0) : null;
        }

        // Una copia solo visual de la pieza (sin scripts, colliders ni luces), en su misma pose y tamaño del mundo.
        private GameObject Copiar(Transform pieza, bool achicar)
        {
            if (pieza == null)
            {
                return null;
            }
            GameObject copia = Instantiate(pieza.gameObject, pieza.position, pieza.rotation);
            copia.name = "CargadorVR";
            copia.transform.localScale = pieza.lossyScale * (achicar ? EscalaSinPieza : 1f);
            copia.SetActive(true);
            foreach (MonoBehaviour m in copia.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Destroy(m);
            }
            foreach (Collider c in copia.GetComponentsInChildren<Collider>(true))
            {
                Destroy(c);
            }
            foreach (Light l in copia.GetComponentsInChildren<Light>(true))
            {
                Destroy(l);
            }
            foreach (Transform t in copia.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == NombreRepuesto)
                {
                    t.gameObject.SetActive(false);
                }
            }
            copia.SetActive(true);
            foreach (Renderer r in copia.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = ShadowCastingMode.On;
            }
            copia.AddComponent<IgnorarOcultarVR>();
            return copia;
        }

        // Le pone una caja del tamaño de su modelo y un Rigidbody, que no choque con el jugador, y se borra al rato.
        private void Caer(GameObject copia, Vector3 velocidad)
        {
            Renderer[] rs = copia.GetComponentsInChildren<Renderer>();
            var caja = copia.AddComponent<BoxCollider>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++)
                {
                    b.Encapsulate(rs[i].bounds);
                }
                Vector3 tam = copia.transform.InverseTransformVector(b.size);
                caja.center = copia.transform.InverseTransformPoint(b.center);
                caja.size = new Vector3(Mathf.Max(Mathf.Abs(tam.x), LadoMinimoCaja), Mathf.Max(Mathf.Abs(tam.y), LadoMinimoCaja),
                    Mathf.Max(Mathf.Abs(tam.z), LadoMinimoCaja));
            }
            foreach (Collider propio in GetComponentsInChildren<Collider>())
            {
                Physics.IgnoreCollision(caja, propio);
            }
            var cuerpo = copia.AddComponent<Rigidbody>();
            cuerpo.mass = MasaCargador;
            cuerpo.interpolation = RigidbodyInterpolation.Interpolate;
            cuerpo.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            cuerpo.linearVelocity = velocidad;
            Destroy(copia, SegundosEnPiso);
        }

        private static void VibrarMano(ManoVR mano, float amplitud, float segundos)
        {
            if (mano != null)
            {
                mano.Vibrar(amplitud, segundos);
            }
        }
    }
}

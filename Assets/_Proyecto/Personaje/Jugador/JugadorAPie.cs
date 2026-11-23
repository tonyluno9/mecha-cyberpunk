using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Personaje.Jugador
{
    /// <summary>
    /// El jugador a pie en el mundo: implementa IJugador en la raíz (que tiene el CharacterController)
    /// y se registra en Registro&lt;IJugador&gt; mientras está habilitado. También sube y baja de un
    /// <see cref="IMontable"/>: mientras va dentro apaga lo suyo y su raíz sigue al asiento.
    /// T-412: es <see cref="IGuardable"/> con la clave <c>jugador</c> (escena, posición, giro y arma en la mano). Dentro de un carro guarda el
    /// punto de bajada junto al carro. Al cargar en la misma escena se coloca ahí, sobre el piso, y la aparición por id ya no lo mueve.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class JugadorAPie : MonoBehaviour, IJugador, IGuardable, IModeloCambiable
    {
        public const string Clave = "jugador";

        [Tooltip("Cámara hija del jugador.")]
        [SerializeField] private Camera camara;
        [Tooltip("Al cargar, desde qué altura sobre el punto guardado se busca el piso (m).")]
        [Min(0.1f)] [SerializeField] private float alturaBusquedaPiso = 1.5f;
        [Tooltip("Al cargar, hasta qué distancia hacia abajo se busca el piso (m).")]
        [Min(0.1f)] [SerializeField] private float distanciaBusquedaPiso = 4f;
        [Tooltip("Bug de CONTINUAR: si la posición guardada no es válida, radio (m) en el que se busca el punto seguro más cercano.")]
        [Min(0f)] [SerializeField] private float radioPuntoSeguro = 12f;
        [Tooltip("Separación (m) entre los puntos que se prueban al buscar el punto seguro.")]
        [Min(0.25f)] [SerializeField] private float pasoPuntoSeguro = 1.5f;
        [Tooltip("Debajo de esta altura (m, mundo) un piso ya está fuera del mapa.")]
        [SerializeField] private float alturaMinimaMapa = -20f;

        private CharacterController controlador;
        private ControladorAPie controladorAPie;
        private ArmasAPie armas;
        private AudioListener oyente;
        private GameObject modelo;
        private Camera camaraActual;
        private bool avisoDuplicadoDado;
        private AnimacionCarro animacionCarro;
        private LanzamientoMoto lanzamientoMoto;
        private Rigidbody cuerpoMontable;
        private Vector3 ultimaVelocidadMontable;

        public Transform Raiz => transform;
        public ModoJugador Modo { get; private set; } = ModoJugador.APie;
        public Camera Camara => camaraActual != null ? camaraActual : camara;

        /// <summary>El montable en el que va el jugador, o null si va a pie.</summary>
        public IMontable MontableActual { get; private set; }
        private RedSeguridadJugador red;
        /// <summary>Por qué se descartó la última posición guardada (Valida si se usó).</summary>
        public ReglaPosicionSegura.Motivo UltimaCargaMotivo { get; private set; } = ReglaPosicionSegura.Motivo.Valida;
        /// <summary>True si la última carga movió al jugador a un punto distinto del guardado (el seguro más cercano).</summary>
        public bool UltimaCargaCorregida { get; private set; }

        /// <summary>Se dispara al cambiar de modo, con el modo nuevo.</summary>
        public event Action<ModoJugador> ModoCambio;

        private void Awake()
        {
            controlador = GetComponent<CharacterController>();
            controladorAPie = GetComponent<ControladorAPie>();
            red = GetComponent<RedSeguridadJugador>();
            if (red == null)
            {
                red = gameObject.AddComponent<RedSeguridadJugador>();
            }
            armas = GetComponent<ArmasAPie>();
            if (camara == null)
            {
                Debug.LogError("[Personaje] Falta la cámara en JugadorAPie de " + name, this);
                enabled = false;
                return;
            }
            oyente = camara.GetComponent<AudioListener>();
            // Tercera persona (T-400): el modelo se oculta en el carro. Las manos de primera persona quedan apagadas en el prefab.
            Animator animador = GetComponentInChildren<Animator>(true);
            modelo = animador != null ? animador.gameObject : null;
            camaraActual = camara;
            animacionCarro = GetComponent<AnimacionCarro>();
            if (animacionCarro == null)
            {
                animacionCarro = gameObject.AddComponent<AnimacionCarro>();
            }
            if (GetComponent<GestosJugador>() == null)
            {
                gameObject.AddComponent<GestosJugador>();
            }
            if (GetComponent<ApuntarDesdeVehiculo>() == null)
            {
                gameObject.AddComponent<ApuntarDesdeVehiculo>();
            }
            if (GetComponent<ConsumiblesJugador>() == null)
            {
                gameObject.AddComponent<ConsumiblesJugador>();
            }
            if (GetComponent<MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis>() == null)
            {
                gameObject.AddComponent<MechaCyberpunk.Personaje.Habilidades.HabilidadesProtesis>();
            }
            lanzamientoMoto = GetComponent<LanzamientoMoto>();
            if (lanzamientoMoto == null)
            {
                lanzamientoMoto = gameObject.AddComponent<LanzamientoMoto>();
            }
        }

        /// <summary>T-520: el modelo del jugador cambió (mujer u hombre); lo que se oculta en el carro es el del nuevo.</summary>
        public void AlCambiarModelo(Animator nuevo)
        {
            modelo = nuevo != null ? nuevo.gameObject : null;
        }

        private void OnEnable()
        {
            if (Registro<IJugador>.Cantidad > 0 && !avisoDuplicadoDado)
            {
                avisoDuplicadoDado = true;
                Debug.LogWarning("[Personaje] Ya hay un IJugador registrado; solo debe haber uno (" + name + ").", this);
            }
            Registro<IJugador>.Agregar(this);
            Registro<IGuardable>.Agregar(this);
            EventosMontaje.SalidaForzada += AlSalidaForzada;
            vidaSuscrita = GetComponent<VidaAPie>();
            if (vidaSuscrita != null)
            {
                vidaSuscrita.KO += AlCaerKO;
            }
        }

        private void OnDisable()
        {
            if (vidaSuscrita != null)
            {
                vidaSuscrita.KO -= AlCaerKO;
                vidaSuscrita = null;
            }
            Registro<IJugador>.Quitar(this);
            Registro<IGuardable>.Quitar(this);
            EventosMontaje.SalidaForzada -= AlSalidaForzada;
        }

        // T-1307: si las balas lo tumban dentro del carro o la moto, se baja en cuanto el vehículo lo deje (a pie sigue el flujo normal de KO).
        private const float SegundosReintentoBajarKO = 0.25f;
        private const float SegundosMaximosBajarKO = 15f;
        private VidaAPie vidaSuscrita;

        private void AlCaerKO()
        {
            if (MontableActual != null && isActiveAndEnabled)
            {
                StartCoroutine(BajarTrasKO());
            }
        }

        private IEnumerator BajarTrasKO()
        {
            float fin = Time.time + SegundosMaximosBajarKO;
            WaitForSeconds espera = new WaitForSeconds(SegundosReintentoBajarKO);
            while (MontableActual != null && Time.time < fin)
            {
                if (Bajar())
                {
                    yield break;
                }
                yield return espera;
            }
        }

        // T-444: el carro explota (o algo igual) y saca al jugador de golpe.
        private void AlSalidaForzada(IMontable montable)
        {
            if (montable != null && ReferenceEquals(montable, MontableActual))
            {
                // T-696: en moto y rápido, la salida forzada es un choque: el conductor sale volando (la velocidad es la de antes del choque).
                bool enMoto = animacionCarro != null && animacionCarro.EnMoto;
                Vector3 velocidad = ultimaVelocidadMontable;
                if (Bajar() && enMoto && lanzamientoMoto != null)
                {
                    lanzamientoMoto.Lanzar(velocidad);
                }
            }
        }

        public string ClaveGuardado => Clave;

        /// <summary>T-1722 (T79): mientras tenga valor, el guardado escribe esta posición (la tienda de Tino) en vez de la del campo de tiro.</summary>
        public static (Vector3 posicion, float giro, ArmaAPie arma)? GuardadoFijo { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarGuardadoFijo() => GuardadoFijo = null;

        public string Guardar()
        {
            Vector3 posicion = transform.position;
            Vector3 frente = transform.forward;
            if (MontableActual != null)
            {
                // En el carro: el punto de bajada junto a la puerta, mirando hacia donde va el carro.
                posicion = MontableActual.PuntoSubida;
                if (MontableActual.Asiento != null)
                {
                    frente = MontableActual.Asiento.forward;
                }
            }
            frente.y = 0f;
            var datos = new PosicionGuardada
            {
                escena = SceneManager.GetActiveScene().path,
                posicion = posicion,
                giro = frente.sqrMagnitude > 0f ? Quaternion.LookRotation(frente.normalized, Vector3.up).eulerAngles.y : 0f,
                arma = armas != null ? armas.ArmaActiva.ToString() : ArmaAPie.Punos.ToString(),
            };
            if (GuardadoFijo.HasValue)
            {
                datos.posicion = GuardadoFijo.Value.posicion;
                datos.giro = GuardadoFijo.Value.giro;
                datos.arma = GuardadoFijo.Value.arma.ToString();
            }
            return JsonUtility.ToJson(datos);
        }

        public void Cargar(string datos)
        {
            PosicionGuardada leida = null;
            try
            {
                leida = JsonUtility.FromJson<PosicionGuardada>(datos);
            }
            catch (Exception)
            {
                leida = null;
            }
            if (leida == null)
            {
                Debug.LogWarning("[Personaje] La posición guardada del jugador no se entiende; no se mueve.", this);
                return;
            }
            // Otra escena: la posición no sirve aquí.
            if (leida.escena != SceneManager.GetActiveScene().path || MontableActual != null)
            {
                return;
            }
            // Bug de CONTINUAR: partidas viejas guardaron puntos que ya no existen (el cuarto, las zonas de la expansión, fuera del muro).
            // Solo se usa un punto con piso sólido, dentro del mapa y libre; si no, el más cercano que lo sea; si no, el inicio de la escena.
            Physics.SyncTransforms();
            UltimaCargaMotivo = Evaluar(leida.posicion, out Vector3 piso);
            UltimaCargaCorregida = false;
            if (UltimaCargaMotivo != ReglaPosicionSegura.Motivo.Valida)
            {
                if (!BuscarPuntoSeguro(leida.posicion, out piso))
                {
                    Debug.LogWarning("[Personaje] La posición guardada " + leida.posicion.ToString("F1") + " no sirve (" + UltimaCargaMotivo + ") y no hay punto seguro cerca: aparece en el inicio.", this);
                    return;
                }
                UltimaCargaCorregida = true;
                Debug.LogWarning("[Personaje] La posición guardada " + leida.posicion.ToString("F1") + " no sirve (" + UltimaCargaMotivo + "): aparece en el punto seguro " + piso.ToString("F1") + ".", this);
            }
            Pose pose = new Pose(piso, Quaternion.Euler(0f, leida.giro, 0f));
            AparicionJugador aparicion = GetComponent<AparicionJugador>();
            if (aparicion != null)
            {
                aparicion.Cancelar();
            }
            Colocar(pose);
            red.FijarSeguro(pose);
            bool hayArma = Enum.TryParse(leida.arma, out ArmaAPie arma);
            if (isActiveAndEnabled)
            {
                StartCoroutine(TerminarCarga(pose, hayArma, arma));
            }
        }

        // Un cuadro después ya cargó el inventario: pone el arma y vuelve a colocarlo por si algo lo movió al arrancar.
        private IEnumerator TerminarCarga(Pose pose, bool hayArma, ArmaAPie arma)
        {
            yield return null;
            if (MontableActual == null)
            {
                Colocar(pose);
            }
            if (hayArma && armas != null)
            {
                armas.Equipar(arma);
            }
        }

        /// <summary>Mide un punto para aparecer: piso debajo, altura, dentro del mapa y si el jugador cabe. Devuelve el punto en el piso.</summary>
        public ReglaPosicionSegura.Motivo Evaluar(Vector3 punto, out Vector3 enPiso)
        {
            bool hayPiso = BuscarPiso(punto, out enPiso);
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            bool dentro = territorio == null || !string.IsNullOrEmpty(territorio.ZonaEn(enPiso));
            bool libre = hayPiso && Cabe(enPiso);
            return ReglaPosicionSegura.Evaluar(hayPiso, enPiso.y, alturaMinimaMapa, dentro, libre);
        }

        /// <summary>El punto válido más cercano a <paramref name="punto"/> (hasta radioPuntoSeguro m).</summary>
        public bool BuscarPuntoSeguro(Vector3 punto, out Vector3 seguro)
        {
            foreach (Vector3 candidato in ReglaPosicionSegura.Candidatos(punto, radioPuntoSeguro, pasoPuntoSeguro))
            {
                if (Evaluar(candidato, out seguro) == ReglaPosicionSegura.Motivo.Valida)
                {
                    return true;
                }
            }
            seguro = punto;
            return false;
        }

        // El piso sólido más cercano bajo el punto, sin contar al jugador ni triggers.
        private bool BuscarPiso(Vector3 punto, out Vector3 piso)
        {
            RaycastHit[] golpes = Physics.RaycastAll(punto + Vector3.up * alturaBusquedaPiso, Vector3.down,
                alturaBusquedaPiso + distanciaBusquedaPiso, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float mejor = float.MaxValue;
            piso = punto;
            bool hay = false;
            foreach (RaycastHit golpe in golpes)
            {
                if (golpe.collider.transform.root == transform.root || golpe.distance >= mejor)
                {
                    continue;
                }
                mejor = golpe.distance;
                piso = golpe.point;
                hay = true;
            }
            return hay;
        }

        // Si la cápsula del jugador cabe parada en ese punto del piso (sin chocar con paredes, carros ni muebles).
        private bool Cabe(Vector3 piso)
        {
            float radio = controlador != null ? controlador.radius : 0.4f;
            float alto = controlador != null ? controlador.height : 1.8f;
            Vector3 abajo = piso + Vector3.up * (radio + 0.05f);
            Vector3 arriba = piso + Vector3.up * Mathf.Max(radio + 0.05f, alto - radio);
            foreach (Collider c in Physics.OverlapCapsule(abajo, arriba, radio * 0.95f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.root != transform.root)
                {
                    return false;
                }
            }
            return true;
        }

        // Se guarda la velocidad del montable en cada paso de física, antes de que corra la física: es la de antes de un choque.
        private void FixedUpdate()
        {
            if (MontableActual != null && cuerpoMontable != null)
            {
                ultimaVelocidadMontable = cuerpoMontable.linearVelocity;
            }
        }

        private void LateUpdate()
        {
            if (MontableActual == null || MontableActual.Asiento == null)
            {
                return;
            }
            Transform asiento = MontableActual.Asiento;
            transform.SetPositionAndRotation(asiento.position, asiento.rotation);
        }

        public void Colocar(Pose pose)
        {
            if (MontableActual != null)
            {
                // Dentro de un montable el CharacterController sigue apagado: solo se mueve el transform.
                transform.SetPositionAndRotation(pose.position, pose.rotation);
                return;
            }
            controlador.enabled = false;
            transform.SetPositionAndRotation(pose.position, pose.rotation);
            controlador.enabled = true;
            if (controladorAPie != null)
            {
                controladorAPie.ReiniciarMovimiento();
            }
        }

        /// <summary>Sube al montable si puede. Apaga el control a pie, la cámara y el oído propios y toma los del montable.</summary>
        public bool Subir(IMontable montable)
        {
            if (montable == null || MontableActual != null || Modo != ModoJugador.APie)
            {
                return false;
            }
            Vector3 posicionPrevia = transform.position;
            Vector3 frentePrevio = transform.forward;
            // El oído propio se apaga antes de que el montable prenda el suyo: Unity avisa si hay dos prendidos a la vez.
            bool oyenteEstabaPrendido = oyente != null && oyente.enabled;
            if (oyenteEstabaPrendido)
            {
                oyente.enabled = false;
            }
            if (!montable.IntentarSubir(this))
            {
                if (oyenteEstabaPrendido)
                {
                    oyente.enabled = true;
                }
                return false;
            }

            MontableActual = montable;
            cuerpoMontable = montable.Asiento != null ? montable.Asiento.GetComponentInParent<Rigidbody>() : null;
            ultimaVelocidadMontable = Vector3.zero;
            controladorAPie.ActivarControl(false);
            controladorAPie.enabled = false;
            if (armas != null)
            {
                armas.enabled = false;
            }
            controlador.enabled = false;
            camara.enabled = false;
            if (oyente != null)
            {
                oyente.enabled = false;
            }
            // T-622: en carro el modelo se queda y maneja (Carro_Entrar/Manejar); en el mecha se oculta.
            bool animaCarro = montable.Modo == ModoJugador.Carro && animacionCarro != null && animacionCarro.Subir(montable, posicionPrevia, frentePrevio);
            if (modelo != null && !animaCarro)
            {
                modelo.SetActive(false);
            }
            camaraActual = montable.Camara;
            Modo = montable.Modo;
            Debug.Log("[Personaje] Sube a " + montable.Nombre + " (" + Modo + ")");
            ModoCambio?.Invoke(Modo);
            return true;
        }

        private const float PasoBuscarSalida = 0.05f;
        private const int MaxChoquesSalida = 16;
        private readonly Collider[] choquesSalida = new Collider[MaxChoquesSalida];

        /// <summary>
        /// T-976: el punto más cercano a <paramref name="deseada"/>, yendo en línea recta hacia <paramref name="segura"/> (la salida que dio el vehículo), donde cabe la
        /// cápsula del jugador. Mira como <paramref name="deseada"/>; si en ningún punto cabe, devuelve la segura con ese giro.
        /// </summary>
        private Pose SalidaLibreHacia(Pose deseada, Pose segura)
        {
            float distancia = Vector3.Distance(deseada.position, segura.position);
            int pasos = Mathf.CeilToInt(distancia / PasoBuscarSalida);
            for (int i = 0; i <= pasos; i++)
            {
                Vector3 punto = Vector3.Lerp(deseada.position, segura.position, pasos > 0 ? (float)i / pasos : 1f);
                if (CabeEn(punto))
                {
                    return new Pose(punto, deseada.rotation);
                }
            }
            return new Pose(segura.position, deseada.rotation);
        }

        private bool CabeEn(Vector3 pie)
        {
            float radio = controlador.radius;
            Vector3 centro = pie + controlador.center;
            float mitad = Mathf.Max(0f, controlador.height * 0.5f - radio);
            // Un poco despegada del piso para no contar el suelo.
            Vector3 abajo = centro - Vector3.up * (mitad - controlador.stepOffset * 0.5f);
            Vector3 arriba = centro + Vector3.up * mitad;
            int cantidad = Physics.OverlapCapsuleNonAlloc(abajo, arriba, radio, choquesSalida, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < cantidad; i++)
            {
                if (!choquesSalida[i].transform.IsChildOf(transform))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Baja del montable si puede. Se coloca en la pose de salida y recupera lo suyo.</summary>
        public bool Bajar()
        {
            if (MontableActual == null)
            {
                return false;
            }
            IMontable montable = MontableActual;
            if (!montable.IntentarBajar(out Pose salida))
            {
                return false;
            }

            MontableActual = null;
            if (animacionCarro != null)
            {
                // T-976: de la moto se baja donde termina el clip (si cabe), para que el cuerpo no se deslice ni gire al final.
                if (animacionCarro.SalidaMoto(salida, out Pose finClip))
                {
                    salida = SalidaLibreHacia(finClip, salida);
                }
                animacionCarro.Bajar(montable, salida);
            }
            Colocar(salida);
            if (animacionCarro != null)
            {
                animacionCarro.TrasColocar();
            }
            controladorAPie.enabled = true;
            // T-1307: si bajó porque lo tumbaron (KO), no recupera el control.
            VidaAPie vida = GetComponent<VidaAPie>();
            controladorAPie.ActivarControl(vida == null || !vida.EstaKO);
            if (armas != null)
            {
                armas.enabled = true;
            }
            camara.enabled = true;
            if (oyente != null)
            {
                oyente.enabled = true;
            }
            if (modelo != null)
            {
                modelo.SetActive(true);
            }
            camaraActual = camara;
            Modo = ModoJugador.APie;
            Debug.Log("[Personaje] Baja de " + montable.Nombre);
            ModoCambio?.Invoke(Modo);
            return true;
        }
    }
}

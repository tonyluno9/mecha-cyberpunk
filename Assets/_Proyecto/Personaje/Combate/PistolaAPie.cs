using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Vida;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// Pistola del jugador a pie. Con la pistola en la mano, Atacar lanza un rayo desde el centro de la cámara y otro desde la boca del arma hasta ese punto (T-400):
    /// el primer collider que no sea del propio jugador (se ignoran los triggers) decide. Si tiene
    /// <see cref="IRecibeDanio"/> en sus padres recibe daño de tipo Disparo; si no, el tiro se queda en la pared y deja una
    /// marca. R recarga y, con el cargador vacío, Atacar empieza la recarga sola. El atacante es null: la persona no es IPeleador.
    /// </summary>
    [RequireComponent(typeof(ControladorAPie))]
    [RequireComponent(typeof(VidaAPie))]
    public class PistolaAPie : MonoBehaviour
    {
        private const int MaxImpactos = 16;

        [SerializeField] private DatosPistola datos;
        [SerializeField] private InputActionReference atacar;
        [SerializeField] private InputActionReference recargar;
        [Tooltip("Visual de la pistola, hijo de Manos. Se mueve hacia atrás con el retroceso.")]
        [SerializeField] private Transform visual;
        [Tooltip("Luz breve en la boca del cañón.")]
        [SerializeField] private Light luzBoca;
        [Tooltip("Visual de la escopeta (T-403). Se mueve hacia atrás con el retroceso.")]
        [SerializeField] private Transform visualEscopeta;
        [SerializeField] private Light luzBocaEscopeta;
        [Tooltip("Visual del fusil de asalto (T-403, T-416).")]
        [SerializeField] private Transform visualFusil;
        [SerializeField] private Light luzBocaFusil;
        [Tooltip("Visual del sniper (T-416).")]
        [SerializeField] private Transform visualSniper;
        [SerializeField] private Light luzBocaSniper;
        [SerializeField] private Material materialMarca;
        [Tooltip("Cuánto dura la luz del cañón (s).")]
        [Min(0.01f)] [SerializeField] private float duracionLuz = 0.05f;
        [Tooltip("Cuánto retrocede la pistola en cada disparo (m).")]
        [Min(0f)] [SerializeField] private float retroceso = 0.08f;
        [Tooltip("Cuánto tarda la pistola en volver a su lugar tras el retroceso (s).")]
        [Min(0.01f)] [SerializeField] private float duracionRetroceso = 0.12f;
        [Tooltip("Cuánto dura la marca donde pegó un tiro (s).")]
        [Min(0.1f)] [SerializeField] private float duracionMarca = 2f;
        [Tooltip("Marcas de impacto visibles a la vez; al pasarse se reusa la más vieja.")]
        [Range(1, 32)] [SerializeField] private int maxMarcas = 16;
        [Tooltip("Lado de la marca de impacto (m).")]
        [Min(0.01f)] [SerializeField] private float tamanoMarca = 0.08f;

        private ControladorAPie controlador;
        private VidaAPie vida;
        private ArmasAPie armas;
        private const int CantidadArmas = 5;
        private readonly CargadorPistola[] cargadores = new CargadorPistola[CantidadArmas];
        private readonly DatosArma[] datosArmas = new DatosArma[CantidadArmas];
        private readonly Transform[] visuales = new Transform[CantidadArmas];
        private readonly Light[] luces = new Light[CantidadArmas];
        private readonly Vector3[] reposos = new Vector3[CantidadArmas];
        private DatosArma pistolaBase;
        private ArsenalJugador arsenal;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactos];
        private float inicioRetroceso = -100f;
        private float finLuz = -1f;
        private ArmaAPie armaRetroceso = ArmaAPie.Pistola;
        private Transform[] marcas;
        private float[] finMarcas;
        private int siguienteMarca;
        private bool enInterior;
        private float finEsperaCalor = -1f;

        public DatosPistola Datos => datos;
        // El cargador del arma de fuego en la mano; con los puños, el de la pistola.
        private CargadorPistola cargador => cargadores[Indice(armas != null ? armas.ArmaActiva : ArmaAPie.Pistola)];
        public CargadorPistola Cargador => cargador;
        public int Balas => cargador != null ? cargador.Balas : 0;
        public int TamanoCargador => cargador != null ? cargador.Tamano : 0;
        public bool Recargando => cargador != null && cargador.Recargando;
        /// <summary>Datos del arma de fuego en la mano (T-403).</summary>
        public DatosArma DatosActuales => datosArmas[Indice(armas != null ? armas.ArmaActiva : ArmaAPie.Pistola)];

        public static bool EsDeFuego(ArmaAPie arma) => arma != ArmaAPie.Punos;

        private static int Indice(ArmaAPie arma) => arma == ArmaAPie.Punos ? (int)ArmaAPie.Pistola : (int)arma;

        /// <summary>Se dispara al disparar, al hacer clic vacío y al empezar o terminar la recarga.</summary>
        public event Action Cambio;
        /// <summary>Se dispara en cada tiro, haya dado a algo o no.</summary>
        public event Action Disparo;
        /// <summary>Se dispara en cada tiro con el arma y la posición de la boca (T-420).</summary>
        public event Action<ArmaAPie, Vector3> Fogonazo;
        /// <summary>Se dispara en cada bala o perdigón que pega a algo (T-420).</summary>
        public event Action<ArmaAPie, ImpactoBala> BalaImpacto;

        private readonly SeguidorImplantes implantes = new SeguidorImplantes();

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            vida = GetComponent<VidaAPie>();
            armas = GetComponent<ArmasAPie>();
            if (datos == null || atacar == null || recargar == null || armas == null)
            {
                Debug.LogError("[Personaje] Faltan referencias en PistolaAPie de " + name, this);
                enabled = false;
                return;
            }
            pistolaBase = DatosArma.Crear("pistola_base", "Pistola", ArmaAPie.Pistola, 0, 0, datos.Danio, 1, datos.BalasPorCargador,
                datos.TiempoRecarga, datos.TiempoEntreDisparos, false, 0f, 0f, datos.Alcance);
            visuales[(int)ArmaAPie.Pistola] = visual;
            visuales[(int)ArmaAPie.Escopeta] = visualEscopeta;
            visuales[(int)ArmaAPie.Fusil] = visualFusil;
            visuales[(int)ArmaAPie.Sniper] = visualSniper;
            luces[(int)ArmaAPie.Pistola] = luzBoca;
            luces[(int)ArmaAPie.Escopeta] = luzBocaEscopeta;
            luces[(int)ArmaAPie.Fusil] = luzBocaFusil;
            luces[(int)ArmaAPie.Sniper] = luzBocaSniper;
            for (int i = 1; i < CantidadArmas; i++)
            {
                if (visuales[i] != null)
                {
                    reposos[i] = visuales[i].localPosition;
                }
                if (luces[i] != null)
                {
                    luces[i].enabled = false;
                }
            }
            RevisarArmas();
            CrearMarcas();
            if (GetComponent<SensacionDisparo>() == null)
            {
                gameObject.AddComponent<SensacionDisparo>();
            }
        }

        // Crea o rehace el cargador de cada arma de fuego cuando cambia su nivel (el cargador nuevo viene lleno).
        private void RevisarArmas()
        {
            if (arsenal == null)
            {
                arsenal = Registro<ArsenalJugador>.Actual;
            }
            for (int i = (int)ArmaAPie.Pistola; i < CantidadArmas; i++)
            {
                ArmaAPie arma = (ArmaAPie)i;
                DatosArma nuevos = arsenal != null ? arsenal.Datos(arma) : null;
                if (nuevos == null && arma == ArmaAPie.Pistola && (arsenal == null || arsenal.PistolaAlInicio))
                {
                    nuevos = pistolaBase;
                }
                if (nuevos == datosArmas[i])
                {
                    continue;
                }
                datosArmas[i] = nuevos;
                if (nuevos == null)
                {
                    cargadores[i] = null;
                    continue;
                }
                CargadorPistola c = new CargadorPistola(nuevos.Cargador, nuevos.TiempoEntreDisparos, nuevos.Recarga);
                c.Disparo += AlCambiar;
                c.RecargaIniciada += AlCambiar;
                c.RecargaTerminada += AlCambiar;
                c.ClicVacio += () =>
                {
                    c.EmpezarRecarga();
                    Cambio?.Invoke();
                };
                cargadores[i] = c;
                Cambio?.Invoke();
            }
        }

        private void OnEnable()
        {
            implantes.Activar();
            atacar.action.Enable();
            recargar.action.Enable();
            EventosLugares.JugadorEntro += AlEntrarLugar;
            EventosLugares.JugadorSalio += AlSalirLugar;
        }

        private void OnDisable()
        {
            implantes.Desactivar();
            EventosLugares.JugadorEntro -= AlEntrarLugar;
            EventosLugares.JugadorSalio -= AlSalirLugar;
            atacar.action.Disable();
            recargar.action.Disable();
        }

        private void OnDestroy()
        {
            if (marcas == null)
            {
                return;
            }
            foreach (Transform marca in marcas)
            {
                if (marca != null)
                {
                    Destroy(marca.gameObject);
                }
            }
        }

        private void Update()
        {
            RevisarArmas();
            ReproducirDiferidos();
            float factorRecarga = EfectosAPie.FactorRecarga(implantes.Efectos);
            for (int i = 0; i < CantidadArmas; i++)
            {
                if (cargadores[i] != null)
                {
                    cargadores[i].FactorRecarga = factorRecarga;
                }
                cargadores[i]?.Avanzar(Time.deltaTime);
            }
            ArmaAPie activa = armas.ArmaActiva;
            CargadorPistola actual = EsDeFuego(activa) ? cargadores[(int)activa] : null;
            bool puede = actual != null && !vida.EstaKO && controlador.ControlActivo;
            if (puede)
            {
                if (recargar.action.WasPressedThisFrame() && actual.EmpezarRecarga())
                {
                    Sonidos.Reproducir(IdRecarga(activa));
                }
                DatosArma d = datosArmas[(int)activa];
                bool pidio = d.Automatica ? atacar.action.IsPressed() : atacar.action.WasPressedThisFrame();
                if (!pidio && disparabaAutomatica && activa == ArmaAPie.Fusil)
                {
                    Sonidos.ReproducirEn("ARM_FUS_RAFAGA_COLA", transform.position);
                }
                disparabaAutomatica = pidio && d.Automatica;
                if (pidio && actual.IntentarDisparar())
                {
                    Disparar(activa, d);
                }
                else if (pidio && !actual.Recargando && actual.Balas <= 0 && Time.time >= finEsperaVacio)
                {
                    finEsperaVacio = Time.time + EsperaClicVacio;
                    Sonidos.Reproducir("ARM_VACIO");
                }
            }
            ActualizarVisual();
        }

        // Un interior es cualquier lugar que no sea la calle.
        private void AlEntrarLugar(IdLugar lugar) => enInterior = lugar != IdLugar.Ninguno && lugar != IdLugar.Calle;

        private void AlSalirLugar(IdLugar lugar) => enInterior = false;

        // Cada disparo en la calle suma calor, como máximo una vez cada SegundosEntreCalor.
        private void SumarCalor()
        {
            if (enInterior || Time.time < finEsperaCalor || datos.CalorPorDisparo <= 0f)
            {
                return;
            }
            finEsperaCalor = Time.time + datos.SegundosEntreCalor;
            Registro<ICalor>.Actual?.Sumar(datos.CalorPorDisparo, MotivoCalor.Pelea);
        }

        // T-442: sonidos que suenan un poco después del disparo (casquillo, bombeo de la escopeta, cerrojo del sniper).
        private const float SegundosCasquillo = 0.25f;
        private const float SegundosBombeo = 0.45f;
        private const float SegundosCerrojo = 0.5f;
        private const float AlturaCasquillo = 0.05f;
        private struct SonidoDiferido
        {
            public float Cuando;
            public string Id;
            public Vector3 Posicion;
        }
        private readonly System.Collections.Generic.List<SonidoDiferido> diferidos = new System.Collections.Generic.List<SonidoDiferido>();
        private bool disparabaAutomatica;

        private const float EsperaClicVacio = 0.3f;
        private float finEsperaVacio;

        private static string IdDisparo(ArmaAPie arma)
        {
            return arma == ArmaAPie.Escopeta ? "ARM_ESC_DISPARO" : arma == ArmaAPie.Fusil ? "ARM_FUS_DISPARO"
                : arma == ArmaAPie.Sniper ? "ARM_SNI_DISPARO" : "ARM_PIS_DISPARO";
        }

        private static string IdRecarga(ArmaAPie arma)
        {
            return arma == ArmaAPie.Escopeta ? "ARM_ESC_RECARGA_CARTUCHO" : arma == ArmaAPie.Fusil ? "ARM_FUS_RECARGA"
                : arma == ArmaAPie.Sniper ? "ARM_SNI_RECARGA" : "ARM_PIS_RECARGA";
        }

        // T-442: programa un sonido para dentro de unos segundos.
        private void Diferir(string id, float segundos, Vector3 posicion)
        {
            diferidos.Add(new SonidoDiferido { Cuando = Time.time + segundos, Id = id, Posicion = posicion });
        }

        private void ReproducirDiferidos()
        {
            for (int i = diferidos.Count - 1; i >= 0; i--)
            {
                if (Time.time >= diferidos[i].Cuando)
                {
                    Sonidos.ReproducirEn(diferidos[i].Id, diferidos[i].Posicion);
                    diferidos.RemoveAt(i);
                }
            }
        }

        // Lo que suena un poco después de cada disparo: el casquillo al caer y el bombeo o el cerrojo de las armas que los tienen.
        private void ProgramarSonidosTrasDisparo(ArmaAPie arma)
        {
            Diferir("ARM_CASQUILLO", SegundosCasquillo, transform.position + Vector3.up * AlturaCasquillo);
            if (arma == ArmaAPie.Escopeta)
            {
                Diferir("ARM_ESC_BOMBEO", SegundosBombeo, transform.position);
            }
            else if (arma == ArmaAPie.Sniper)
            {
                Diferir("ARM_SNI_CERROJO", SegundosCerrojo, transform.position);
            }
        }

        // Metal: lo que se puede montar (carros) suena distinto al concreto.
        private static string IdImpactoSinCarne(Collider collider) =>
            collider.GetComponentInParent<IMontable>() != null ? "ARM_BALA_IMPACTO_METAL" : "ARM_BALA_IMPACTO_CONCRETO";

        private void Disparar(ArmaAPie arma, DatosArma d)
        {
            bool sonoImpacto = false;
            // Tercera persona: un rayo desde el centro de la cámara halla el punto y otro, desde la boca del arma, llega a ese punto.
            // La escopeta lanza varios perdigones en un cono; la dispersión es el semiángulo del cono.
            Transform camara = controlador.Camara;
            Light luz = luces[(int)arma];
            Transform visualArma = visuales[(int)arma];
            Transform boca = luz != null ? luz.transform : (visualArma != null ? visualArma : camara);
            Vector3 punto = camara.position + camara.forward * d.Alcance;
            if (BuscarImpacto(camara.position, camara.forward, d.Alcance, out RaycastHit centro))
            {
                punto = centro.point;
            }
            Vector3 origen = boca.position;
            Vector3 haciaPunto = punto - origen;
            float largo = haciaPunto.magnitude;
            Vector3 direccionBase = largo > 0.01f ? haciaPunto / largo : camara.forward;
            float cono = controlador.Apuntando ? d.DispersionApuntando : d.Dispersion;

            Sonidos.ReproducirEn(IdDisparo(arma), origen);
            EventosRuido.PublicarDisparo(origen);
            for (int i = 0; i < d.Perdigones; i++)
            {
                Vector3 direccion = direccionBase;
                if (cono > 0f)
                {
                    Vector3 eje = Vector3.Cross(direccionBase, UnityEngine.Random.onUnitSphere);
                    if (eje.sqrMagnitude > 0.0001f)
                    {
                        direccion = Quaternion.AngleAxis(cono * Mathf.Sqrt(UnityEngine.Random.value), eje.normalized) * direccionBase;
                    }
                }
                if (BuscarImpacto(origen, direccion, d.Alcance, out RaycastHit mejor))
                {
                    IRecibeDanio objetivo = mejor.collider.GetComponentInParent<IRecibeDanio>();
                    bool mato = false;
                    if (objetivo != null)
                    {
                        mato = objetivo.RecibirDanio(new InfoDanio(d.Danio, TipoDanio.Disparo, mejor.point, direccion, null)).CausoKO;
                    }
                    BalaImpacto?.Invoke(arma, new ImpactoBala(mejor.point, mejor.normal, objetivo != null, mato));
                    if (!sonoImpacto)
                    {
                        sonoImpacto = true;
                        Sonidos.ReproducirEn(objetivo != null ? "ARM_BALA_IMPACTO_CARNE" : IdImpactoSinCarne(mejor.collider), mejor.point);
                    }
                    PonerMarca(mejor.point, mejor.normal);
                }
            }

            ProgramarSonidosTrasDisparo(arma);
            SumarCalor();
            inicioRetroceso = Time.time;
            armaRetroceso = arma;
            finLuz = Time.time + duracionLuz;
            Fogonazo?.Invoke(arma, origen);
            Disparo?.Invoke();
        }

        // El primer collider que no sea del propio jugador; los triggers se ignoran.
        private bool BuscarImpacto(Vector3 origen, Vector3 direccion, float alcance, out RaycastHit mejor)
        {
            int cantidad = Physics.RaycastNonAlloc(origen, direccion, impactos, alcance, ~0, QueryTriggerInteraction.Ignore);
            bool hay = false;
            mejor = default;
            for (int i = 0; i < cantidad; i++)
            {
                RaycastHit impacto = impactos[i];
                if (impacto.collider.transform.root == transform.root)
                {
                    continue;
                }
                if (!hay || impacto.distance < mejor.distance)
                {
                    mejor = impacto;
                    hay = true;
                }
            }
            return hay;
        }


        private void AlCambiar() => Cambio?.Invoke();

        private void ActualizarVisual()
        {
            for (int i = 1; i < CantidadArmas; i++)
            {
                bool dispara = i == (int)armaRetroceso;
                if (luces[i] != null)
                {
                    luces[i].enabled = dispara && Time.time < finLuz;
                }
                if (visuales[i] != null)
                {
                    float t = dispara ? Mathf.Clamp01((Time.time - inicioRetroceso) / duracionRetroceso) : 1f;
                    visuales[i].localPosition = reposos[i] - Vector3.forward * (retroceso * (1f - t));
                }
            }
            if (marcas == null)
            {
                return;
            }
            for (int i = 0; i < marcas.Length; i++)
            {
                if (marcas[i].gameObject.activeSelf && Time.time >= finMarcas[i])
                {
                    marcas[i].gameObject.SetActive(false);
                }
            }
        }

        private void CrearMarcas()
        {
            marcas = new Transform[maxMarcas];
            finMarcas = new float[maxMarcas];
            for (int i = 0; i < maxMarcas; i++)
            {
                GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marca.name = "MarcaImpacto";
                Destroy(marca.GetComponent<Collider>());
                marca.transform.localScale = new Vector3(tamanoMarca, tamanoMarca, 0.01f);
                MeshRenderer render = marca.GetComponent<MeshRenderer>();
                if (materialMarca != null)
                {
                    render.sharedMaterial = materialMarca;
                }
                render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                marca.SetActive(false);
                marcas[i] = marca.transform;
            }
        }

        private void PonerMarca(Vector3 punto, Vector3 normal)
        {
            Transform marca = marcas[siguienteMarca];
            finMarcas[siguienteMarca] = Time.time + duracionMarca;
            siguienteMarca = (siguienteMarca + 1) % marcas.Length;
            marca.position = punto + normal * 0.01f;
            marca.rotation = Quaternion.LookRotation(-normal);
            marca.gameObject.SetActive(true);
        }
    }
}

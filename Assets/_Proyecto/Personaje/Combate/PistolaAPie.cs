using System;
using MechaCyberpunk.Core;
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
        private CargadorPistola cargador;
        private readonly RaycastHit[] impactos = new RaycastHit[MaxImpactos];
        private Vector3 posicionReposo;
        private float inicioRetroceso = -100f;
        private float finLuz = -1f;
        private Transform[] marcas;
        private float[] finMarcas;
        private int siguienteMarca;
        private bool enInterior;
        private float finEsperaCalor = -1f;

        public DatosPistola Datos => datos;
        public CargadorPistola Cargador => cargador;
        public int Balas => cargador != null ? cargador.Balas : 0;
        public int TamanoCargador => cargador != null ? cargador.Tamano : 0;
        public bool Recargando => cargador != null && cargador.Recargando;

        /// <summary>Se dispara al disparar, al hacer clic vacío y al empezar o terminar la recarga.</summary>
        public event Action Cambio;
        /// <summary>Se dispara en cada tiro, haya dado a algo o no.</summary>
        public event Action Disparo;

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
            cargador = new CargadorPistola(datos.BalasPorCargador, datos.TiempoEntreDisparos, datos.TiempoRecarga);
            cargador.Disparo += AlCambiar;
            cargador.RecargaIniciada += AlCambiar;
            cargador.RecargaTerminada += AlCambiar;
            cargador.ClicVacio += AlClicVacio;
            if (visual != null)
            {
                posicionReposo = visual.localPosition;
            }
            if (luzBoca != null)
            {
                luzBoca.enabled = false;
            }
            CrearMarcas();
        }

        private void OnEnable()
        {
            atacar.action.Enable();
            recargar.action.Enable();
            EventosLugares.JugadorEntro += AlEntrarLugar;
            EventosLugares.JugadorSalio += AlSalirLugar;
        }

        private void OnDisable()
        {
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
            cargador.Avanzar(Time.deltaTime);
            bool puede = !vida.EstaKO && controlador.ControlActivo && armas.ArmaActiva == ArmaAPie.Pistola;
            if (puede)
            {
                if (recargar.action.WasPressedThisFrame())
                {
                    cargador.EmpezarRecarga();
                }
                if (atacar.action.WasPressedThisFrame() && cargador.IntentarDisparar())
                {
                    Disparar();
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

        private void Disparar()
        {
            // Tercera persona: un rayo desde el centro de la cámara halla el punto y otro, desde la boca del arma, llega a ese punto.
            Transform camara = controlador.Camara;
            Transform boca = luzBoca != null ? luzBoca.transform : (visual != null ? visual : camara);
            Vector3 punto = camara.position + camara.forward * datos.Alcance;
            if (BuscarImpacto(camara.position, camara.forward, datos.Alcance, out RaycastHit centro))
            {
                punto = centro.point;
            }
            Vector3 origen = boca.position;
            Vector3 haciaPunto = punto - origen;
            float largo = haciaPunto.magnitude;
            Vector3 direccion = largo > 0.01f ? haciaPunto / largo : camara.forward;

            bool hayImpacto = BuscarImpacto(origen, direccion, largo + 0.3f, out RaycastHit mejor);

            if (hayImpacto)
            {
                IRecibeDanio objetivo = mejor.collider.GetComponentInParent<IRecibeDanio>();
                if (objetivo != null)
                {
                    objetivo.RecibirDanio(new InfoDanio(datos.Danio, TipoDanio.Disparo, mejor.point, direccion, null));
                }
                PonerMarca(mejor.point, mejor.normal);
            }

            SumarCalor();
            inicioRetroceso = Time.time;
            finLuz = Time.time + duracionLuz;
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

        private void AlClicVacio()
        {
            cargador.EmpezarRecarga();
            Cambio?.Invoke();
        }

        private void AlCambiar() => Cambio?.Invoke();

        private void ActualizarVisual()
        {
            if (luzBoca != null)
            {
                luzBoca.enabled = Time.time < finLuz;
            }
            if (visual != null)
            {
                float t = Mathf.Clamp01((Time.time - inicioRetroceso) / duracionRetroceso);
                visual.localPosition = posicionReposo - Vector3.forward * (retroceso * (1f - t));
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

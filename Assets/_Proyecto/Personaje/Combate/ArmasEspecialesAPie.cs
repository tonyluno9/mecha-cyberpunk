using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>
    /// T-1013: dispara las armas especiales a pie, que <see cref="PistolaAPie"/> le pasa cada cuadro con su cargador:
    /// - Lanzagranadas: cada clic gasta un tiro y lanza una granada en arco hacia la mira, que explota al chocar.
    /// - Granada: al apuntar se ve el arco por donde va a volar; cada clic lanza una granada con la mano que rebota y explota al acabarse la mecha.
    /// - Rifle de plasma: mantener el disparo carga 3 s (una bola de energía crece en la boca); luego sale un rayo de 5 s que sigue a la mira, atraviesa a la
    ///   gente y daña todo lo que toca (para en las paredes). La lógica está en <see cref="ReglaRayoPlasma"/> y <see cref="ReglaBalistica"/>.
    /// También hace girar el tambor del revólver y del lanzagranadas en cada tiro (<see cref="TamborArma"/>).
    /// </summary>
    [RequireComponent(typeof(PistolaAPie))]
    [DefaultExecutionOrder(OrdenEjecucion)]
    public class ArmasEspecialesAPie : MonoBehaviour
    {
        /// <summary>Después de la animación, del giro del cuerpo y del IK del arma (que corren en LateUpdate con orden menor).</summary>
        public const int OrdenEjecucion = 1000;
        private const float TicDanioRayo = 0.1f;
        private const int MaxToques = 32;
        private const float AlturaManoGranada = 1.45f;
        private const float AdelanteManoGranada = 0.35f;
        private const float LatidoRayo = 18f;
        private const float AnchoLaser = 0.01f;
        private const float AnchoMarcador = 0.05f;
        private const int PuntosMarcador = 32;
        private const float LevantarMarcador = 0.05f;
        private const float IntensidadBola = 1.4f;
        private static readonly int ColorBase = Shader.PropertyToID("_BaseColor");

        private ControladorAPie controlador;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private MechaCyberpunk.Personaje.Vida.VidaAPie vida;
        private ConfigArmasEspeciales cfg;
        private ReglaRayoPlasma plasma;
        private DatosArma datosPlasma;
        private bool pulsadoAntes;
        private LineRenderer arco;
        private LineRenderer laser;
        private LineRenderer marcador;
        private EfectoRayoPlasma rayo;
        private int bucleChispeo = Sonidos.SinBucle;
        private int bucleRayo = Sonidos.SinBucle;
        private int bucleCapaRayo = Sonidos.SinBucle; // T-1532: ARM_W07_RAYO_CAPA_EXTRA
        /// <summary>T-1309: ID del chispeo en bucle mientras carga el rifle de plasma.</summary>
        public const string IdChispeoPlasma = SonidosArmasT1472.PlasmaCarga;
        /// <summary>T-1309: ID del disparo del rayo de plasma.</summary>
        public const string IdDisparoPlasma = SonidosArmasT1472.PlasmaDisparo;
        private Transform bolaCarga;
        private Light luzRayo;
        private Vector3[] puntosArco;
        private float siguienteZumbido;
        private float acumuladoTic;
        private Vector3 finRayo;
        private readonly RaycastHit[] toques = new RaycastHit[MaxToques];
        private readonly HashSet<IRecibeDanio> tocados = new HashSet<IRecibeDanio>();
        private readonly List<TamborArma> tambores = new List<TamborArma>();

        /// <summary>En qué va el rifle de plasma.</summary>
        public EstadoPlasma EstadoPlasma => plasma != null ? plasma.Estado : EstadoPlasma.Reposo;

        /// <summary>Carga del rifle de plasma (0 a 1).</summary>
        public float CargaPlasma => plasma != null ? plasma.Carga : 0f;

        /// <summary>Segundos de rayo que quedan.</summary>
        public float RestanteRayo => plasma != null ? plasma.RestanteRayo : 0f;

        /// <summary>Donde termina el rayo ahora (pared o alcance).</summary>
        public Vector3 FinRayo => finRayo;

        /// <summary>True si se está dibujando el arco de la granada.</summary>
        public bool ArcoVisible => arco != null && arco.enabled;

        /// <summary>T-1491: True si se dibuja el marcador (círculo del radio de la explosión) donde caerá la granada.</summary>
        public bool MarcadorVisible => marcador != null && marcador.enabled;

        /// <summary>T-1491: centro del marcador de caída (el último punto del arco).</summary>
        public Vector3 PuntoMarcador { get; private set; }

        /// <summary>T-1337: True si se dibuja la línea de mira del rifle de plasma (apuntando, antes y durante la carga).</summary>
        public bool LaserVisible => laser != null && laser.enabled;

        /// <summary>La última granada o proyectil lanzado (para las pruebas).</summary>
        public ProyectilAPie UltimoProyectil { get; private set; }

        /// <summary>
        /// VR5: boca del arma en la mano VR. Si no es null, el lanzagranadas y el rifle de plasma disparan desde aquí hacia su +Z (sin la mira de la
        /// cámara) y el botón lo da <see cref="GatilloExterno"/> en vez del ratón. Sin VR queda en null y todo sigue como antes.
        /// </summary>
        public Transform BocaExterna { get; set; }

        /// <summary>VR5: true mientras el gatillo del control VR está abajo (solo cuenta con <see cref="BocaExterna"/>).</summary>
        public bool GatilloExterno { get; set; }

        /// <summary>
        /// VR51 (solo VR, con <see cref="BocaExterna"/>): con true el arco y el marcador de caída del lanzagranadas se ven sin el botón de
        /// apuntar (el gatillo izquierdo del control).
        /// </summary>
        public bool ArcoExterno { get; set; }

        /// <summary>
        /// VR51 (solo VR, con <see cref="BocaExterna"/>): con true la carga llena del plasma espera a <see cref="DisparoExterno"/> en vez de
        /// soltar el rayo sola (<see cref="ReglaRayoPlasma.RetenerCarga"/>).
        /// </summary>
        public bool RetenerCargaExterna { get; set; }

        /// <summary>VR51 (solo VR): el gatillo que suelta el rayo con la carga llena cuando <see cref="RetenerCargaExterna"/>.</summary>
        public bool DisparoExterno { get; set; }

        /// <summary>VR51: true con la carga del plasma llena esperando el disparo.</summary>
        public bool CargaPlasmaLlena => plasma != null && plasma.CargaLlena;

        /// <summary>VR5: la bola de energía de la carga del plasma (para que VR la deje visible).</summary>
        public Transform BolaCarga => bolaCarga;

        private void Awake()
        {
            controlador = GetComponent<ControladorAPie>();
            pistola = GetComponent<PistolaAPie>();
            armas = GetComponent<ArmasAPie>();
            vida = GetComponent<MechaCyberpunk.Personaje.Vida.VidaAPie>();
            cfg = ConfigArmasEspeciales.Actual;
            arco = Linea("ArcoGranada", cfg.AnchoArco, cfg.ColorArco);
            laser = Linea("MiraLaserPlasma", AnchoLaser, cfg.ColorRayo);
            laser.positionCount = 2;
            marcador = Linea("MarcadorCaidaGranada", AnchoMarcador, cfg.ColorArco);
            marcador.loop = true;
            marcador.positionCount = PuntosMarcador;
            rayo = EfectoRayoPlasma.Crear(transform, cfg.MaterialRayo, cfg.ColorRayo, cfg.AnchoRayo);
            puntosArco = new Vector3[Mathf.Max(2, cfg.PuntosArco)];
            GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(bola.GetComponent<Collider>());
            bola.name = "CargaPlasma";
            bola.transform.SetParent(transform, false);
            Renderer r = bola.GetComponent<Renderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (cfg.MaterialRayo != null)
            {
                // Copia propia teñida del color del rayo (el material es blanco HDR y la esfera no tiene colores de vértice).
                r.material = new Material(cfg.MaterialRayo);
                r.material.SetColor(ColorBase, cfg.ColorRayo * IntensidadBola);
            }
            bolaCarga = bola.transform;
            bolaCarga.gameObject.SetActive(false);
            luzRayo = new GameObject("LuzPlasma").AddComponent<Light>();
            luzRayo.transform.SetParent(transform, false);
            luzRayo.type = LightType.Point;
            luzRayo.color = cfg.ColorRayo;
            luzRayo.range = 4f;
            luzRayo.enabled = false;
        }

        private void Start()
        {
            // Los tambores del revólver y del lanzagranadas (un hijo "Tambor" en su visual).
            foreach (ArmaAPie arma in new[] { ArmaAPie.Revolver, ArmaAPie.Lanzagranadas })
            {
                GameObject visual = armas != null ? armas.Visual(arma) : null;
                Transform tambor = visual != null ? Buscar(visual.transform, TamborArma.NombreTambor) : null;
                if (tambor != null)
                {
                    TamborArma t = tambor.GetComponent<TamborArma>();
                    if (t == null)
                    {
                        t = tambor.gameObject.AddComponent<TamborArma>();
                    }
                    t.Iniciar(arma, visual.transform);
                    tambores.Add(t);
                }
            }
        }

        private void OnEnable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo += AlDisparar;
            }
        }

        private void OnDisable()
        {
            if (pistola != null)
            {
                pistola.Fogonazo -= AlDisparar;
            }
            Soltar();
        }

        private void OnDestroy()
        {
            if (arco != null) Destroy(arco.gameObject);
            if (laser != null) Destroy(laser.gameObject);
            if (marcador != null) Destroy(marcador.gameObject);
            if (rayo != null) Destroy(rayo.gameObject);
        }

        private void AlDisparar(ArmaAPie arma, Vector3 boca)
        {
            foreach (TamborArma t in tambores)
            {
                if (t != null && t.Arma == arma)
                {
                    t.Girar(TamborArma.RecamarasPorOmision, cfg.SegundosGiroTambor); // T92: el cargador va al doble, pero el tambor sigue con sus 6 recámaras
                }
            }
        }

        private static Transform Buscar(Transform raiz, string nombre)
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == nombre)
                {
                    return t;
                }
            }
            return null;
        }

        private LineRenderer Linea(string nombre, float ancho, Color color)
        {
            LineRenderer l = new GameObject(nombre).AddComponent<LineRenderer>();
            l.useWorldSpace = true;
            l.widthMultiplier = ancho;
            l.startColor = color;
            l.endColor = color;
            l.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            l.receiveShadows = false;
            l.sharedMaterial = cfg.MaterialRayo != null ? cfg.MaterialRayo : new Material(Shader.Find("Sprites/Default"));
            l.enabled = false;
            return l;
        }

        /// <summary>
        /// Un cuadro con un arma especial en la mano: <paramref name="cargador"/> es su cargador (gasta un tiro por granada o por rayo) y
        /// <paramref name="pulsado"/> si el botón de disparar está abajo.
        /// </summary>
        public void Actualizar(ArmaAPie arma, CargadorPistola cargador, DatosArma d, bool pulsado)
        {
            // Solo se anota: el disparo y lo que se ve van en LateUpdate, cuando el arma ya está donde la dejó la animación y el giro de la mira
            // (en Update la boca aún está donde miraba el cuerpo el cuadro anterior).
            pendiente = true;
            armaPendiente = arma;
            cargadorPendiente = cargador;
            datosPendientes = d;
            pulsadoPendiente = pulsado;
        }

        private bool pendiente;
        private ArmaAPie armaPendiente;
        private CargadorPistola cargadorPendiente;
        private DatosArma datosPendientes;
        private bool pulsadoPendiente;

        // ---- T-1561: la granada se suelta en el cuadro del lanzamiento, no al apretar ----

        private VisualPersonaje visualGranada;
        private CubiertaYRodada cubiertaGranada;
        private bool granadaPendiente;
        private float granadaEn;
        private ArmaAPie armaGranada;
        private DatosArma datosGranada;

        /// <summary>T-1561: True entre apretar y soltar la granada (la animación de lanzar está en curso y la granada sigue en la mano).</summary>
        public bool GranadaEnLaMano => granadaPendiente;

        /// <summary>T-1561: segundos que faltan para que la mano suelte la granada (0 si no hay lanzamiento).</summary>
        public float SegundosParaSoltar => granadaPendiente ? Mathf.Max(0f, granadaEn - Time.time) : 0f;

        // Arranca el lanzamiento con PJ01_Granada_DePie o PJ01_Granada_Agachado; sin Animator con el disparador, la granada sale al instante como antes.
        private void IniciarGranada(ArmaAPie arma, DatosArma d)
        {
            if (visualGranada == null)
            {
                visualGranada = GetComponentInChildren<VisualPersonaje>(false);
            }
            if (cubiertaGranada == null)
            {
                cubiertaGranada = GetComponent<CubiertaYRodada>();
            }
            bool agachado = cubiertaGranada != null && cubiertaGranada.AgachadoLibre;
            if (visualGranada == null || !visualGranada.isActiveAndEnabled || !visualGranada.LanzarGranada(agachado))
            {
                LanzarGranada(arma, d);
                return;
            }
            granadaPendiente = true;
            granadaEn = Time.time + ReglasTransicionesT1561.SegundosHastaSoltarGranada(agachado);
            armaGranada = arma;
            datosGranada = d;
        }

        private void AvanzarGranada()
        {
            if (!granadaPendiente)
            {
                return;
            }
            if (visualGranada == null || visualGranada.Caido || !visualGranada.GranadaEnCurso)
            {
                granadaPendiente = false; // el daño, la muerte o un cambio de arma cortaron el lanzamiento: la granada no sale
                return;
            }
            if (Time.time >= granadaEn)
            {
                granadaPendiente = false;
                LanzarGranada(armaGranada, datosGranada);
            }
        }

        private void LateUpdate()
        {
            AvanzarGranada();
            if (!pendiente)
            {
                // T-1309: un cuadro sin el plasma en la mano (dejó de apuntar o soltó el arma) corta la carga y su chispeo.
                if (ChispeoSonando)
                {
                    plasma?.Cancelar();
                    ApagarPlasma();
                }
                return;
            }
            pendiente = false;
            Procesar(armaPendiente, cargadorPendiente, datosPendientes, pulsadoPendiente);
        }

        private void Procesar(ArmaAPie arma, CargadorPistola cargador, DatosArma d, bool pulsado)
        {
            if (BocaExterna != null)
            {
                pulsado = GatilloExterno && !controlador.EntradaBloqueada; // VR5: el gatillo del control en vez del ratón
            }
            bool flanco = pulsado && !pulsadoAntes;
            pulsadoAntes = pulsado;
            if (d.TipoDisparo != TipoDisparoArma.Rayo && plasma != null && plasma.Estado != EstadoPlasma.Reposo)
            {
                plasma.Cancelar();
            }
            switch (d.TipoDisparo)
            {
                case TipoDisparoArma.Proyectil:
                    ApagarPlasma();
                    DibujarArco(arma, d);
                    if (flanco && cargador.IntentarDisparar())
                    {
                        LanzarProyectil(arma, d);
                    }
                    break;
                case TipoDisparoArma.Lanzada:
                    ApagarPlasma();
                    DibujarArco(arma, d);
                    if (flanco && !granadaPendiente && cargador.IntentarDisparar())
                    {
                        IniciarGranada(arma, d);
                    }
                    break;
                case TipoDisparoArma.Rayo:
                    arco.enabled = false;
                    marcador.enabled = false;
                    Plasma(arma, cargador, d, pulsado);
                    DibujarLaser(arma, d);
                    break;
            }
        }

        /// <summary>Sin arma especial en la mano: se esconde el arco y se corta el plasma.</summary>
        public void Soltar()
        {
            granadaPendiente = false;
            pendiente = false;
            pulsadoAntes = false;
            if (arco != null)
            {
                arco.enabled = false;
            }
            if (laser != null)
            {
                laser.enabled = false;
            }
            if (marcador != null)
            {
                marcador.enabled = false;
            }
            plasma?.Cancelar();
            ApagarPlasma();
        }

        // VR5: la boca de donde sale el disparo especial: la del arma en la mano VR si hay, si no la del arma del cuerpo.
        private Transform BocaDisparo(ArmaAPie arma) => BocaExterna != null ? BocaExterna : pistola.BocaDe(arma);

        // VR5: hasta dónde llega la línea de mira del plasma desde el cañón en la mano (lo primero que toca que no sea el jugador).
        private Vector3 FinLaserExterno(Vector3 origen, float alcance)
        {
            Vector3 direccion = BocaExterna.forward;
            int n = Physics.RaycastNonAlloc(origen, direccion, toques, alcance, ~0, QueryTriggerInteraction.Ignore);
            float cerca = alcance;
            for (int i = 0; i < n; i++)
            {
                if (toques[i].collider.transform.root != transform.root && toques[i].distance < cerca)
                {
                    cerca = toques[i].distance;
                }
            }
            return origen + direccion * cerca;
        }

        // ---- Lanzagranadas y granada ----

        private Vector3 OrigenLanzada()
        {
            Vector3 adelante = controlador.Camara != null ? Vector3.ProjectOnPlane(controlador.Camara.forward, Vector3.up).normalized : transform.forward;
            return transform.position + Vector3.up * AlturaManoGranada + adelante * AdelanteManoGranada + transform.right * 0.2f;
        }

        private Vector3 VelocidadDe(ArmaAPie arma, DatosArma d, out Vector3 origen)
        {
            if (d.TipoDisparo == TipoDisparoArma.Lanzada)
            {
                origen = OrigenLanzada();
                Vector3 mira = controlador.Camara != null ? controlador.Camara.forward : transform.forward;
                return ReglaBalistica.Elevar(mira, cfg.ElevacionLanzada) * d.VelocidadProyectil;
            }
            Transform boca = BocaDisparo(arma);
            origen = boca != null ? boca.position : transform.position + Vector3.up * AlturaManoGranada;
            if (BocaExterna != null)
            {
                return BocaExterna.forward * d.VelocidadProyectil; // VR5: sale por el cañón del arma en la mano
            }
            Vector3 punto = pistola.PuntoDeMira(d.Alcance);
            ReglaBalistica.DireccionHacia(origen, punto, d.VelocidadProyectil, Physics.gravity.y, out Vector3 direccion);
            return direccion * d.VelocidadProyectil;
        }

        private void LanzarProyectil(ArmaAPie arma, DatosArma d)
        {
            Vector3 velocidad = VelocidadDe(arma, d, out Vector3 origen);
            if (BocaExterna == null)
            {
                controlador.MirarAlDisparar();
            }
            Sonidos.Reproducir(PistolaAPie.IdDisparo(arma)); // T-1472: 2D, es el disparo del jugador
            UltimoProyectil = ProyectilAPie.Lanzar(origen, velocidad, d.RadioExplosion, d.Danio, true, 0f, 0f, transform.root);
            pistola.AvisarDisparoEspecial(arma, origen);
        }

        private void LanzarGranada(ArmaAPie arma, DatosArma d)
        {
            Vector3 velocidad = VelocidadDe(arma, d, out Vector3 origen);
            controlador.MirarAlDisparar();
            SoltarGranadaEn(arma, d, origen, velocidad);
        }

        private void SoltarGranadaEn(ArmaAPie arma, DatosArma d, Vector3 origen, Vector3 velocidad)
        {
            Sonidos.Reproducir(SonidosArmasT1472.GranadaSeguro); // T-1472: quita el seguro y lanza
            UltimoProyectil = ProyectilAPie.Lanzar(origen, velocidad, d.RadioExplosion, d.Danio, false, cfg.MechaGranada, cfg.Rebote, transform.root);
            pistola.AvisarDisparoEspecial(arma, origen);
        }

        /// <summary>
        /// VR5: lanza la granada de la mano desde <paramref name="origen"/> con <paramref name="velocidad"/> (la de la mano VR), sin la animación:
        /// gasta una del cargador y tiene el mismo daño, mecha, rebote, sonido y avisos que con el ratón. Devuelve si salió.
        /// </summary>
        public bool LanzarGranadaExterna(Vector3 origen, Vector3 velocidad)
        {
            if (!isActiveAndEnabled || pistola == null || !pistola.enabled || armas == null || controlador == null
                || !controlador.ControlActivo || controlador.EntradaBloqueada || (vida != null && vida.EstaKO))
            {
                return false;
            }
            ArmaAPie arma = armas.ArmaActiva;
            DatosArma d = pistola.DatosActuales;
            CargadorPistola cargador = pistola.Cargador;
            if (d == null || cargador == null || d.TipoDisparo != TipoDisparoArma.Lanzada || !cargador.IntentarDisparar())
            {
                return false;
            }
            SoltarGranadaEn(arma, d, origen, velocidad);
            return true;
        }

        // Al apuntar se ve el arco hasta lo primero que toca.
        private void DibujarArco(ArmaAPie arma, DatosArma d)
        {
            if (!controlador.Apuntando && !(BocaExterna != null && ArcoExterno))
            {
                arco.enabled = false;
                marcador.enabled = false;
                return;
            }
            Vector3 velocidad = VelocidadDe(arma, d, out Vector3 origen);
            ReglaBalistica.Arco(origen, velocidad, Physics.gravity, cfg.SegundosArco, puntosArco);
            int usados = puntosArco.Length;
            bool cae = false;
            Vector3 normalCaida = Vector3.up;
            for (int i = 1; i < puntosArco.Length; i++)
            {
                Vector3 tramo = puntosArco[i] - puntosArco[i - 1];
                float largo = tramo.magnitude;
                if (largo > 0.0001f && Physics.Raycast(puntosArco[i - 1], tramo / largo, out RaycastHit golpe, largo, ~0, QueryTriggerInteraction.Ignore)
                    && golpe.collider.transform.root != transform.root)
                {
                    puntosArco[i] = golpe.point;
                    normalCaida = golpe.normal;
                    cae = true;
                    usados = i + 1;
                    break;
                }
            }
            arco.positionCount = usados;
            arco.SetPositions(puntosArco);
            arco.enabled = true;
            PonerMarcador(cae, puntosArco[usados - 1], normalCaida, d.RadioExplosion);
        }

        // T-1491: un círculo del radio de la explosión donde cae la granada (sobre la superficie que toca); sin choque no hay marcador.
        private void PonerMarcador(bool cae, Vector3 punto, Vector3 normal, float radio)
        {
            marcador.enabled = cae;
            if (!cae)
            {
                return;
            }
            PuntoMarcador = punto;
            Vector3 eje = Vector3.Cross(normal, Mathf.Abs(normal.y) > 0.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 otro = Vector3.Cross(normal, eje);
            float r = Mathf.Max(0.3f, radio);
            for (int i = 0; i < PuntosMarcador; i++)
            {
                float a = Mathf.PI * 2f * i / PuntosMarcador;
                marcador.SetPosition(i, punto + normal * LevantarMarcador + (eje * Mathf.Cos(a) + otro * Mathf.Sin(a)) * r);
            }
        }

        // ---- Rifle de plasma ----

        // T-1337: la mira del rifle de plasma, como el arco del lanzagranadas: apuntando, una línea fina de la boca al punto que marca la cámara.
        // Se apaga al salir el rayo (el rayo ya es la línea).
        private void DibujarLaser(ArmaAPie arma, DatosArma d)
        {
            // VR5: con el arma en la mano VR la línea sale siempre del cañón hacia donde apunta (no hay botón de apuntar).
            bool ver = (controlador.Apuntando || BocaExterna != null) && EstadoPlasma != EstadoPlasma.Rayo;
            if (ver)
            {
                Transform boca = BocaDisparo(arma);
                Vector3 origen = boca != null ? boca.position : transform.position + Vector3.up * AlturaManoGranada;
                laser.SetPosition(0, origen);
                laser.SetPosition(1, BocaExterna != null ? FinLaserExterno(origen, d.Alcance) : pistola.PuntoDeMira(d.Alcance));
            }
            laser.enabled = ver;
        }

        private void Plasma(ArmaAPie arma, CargadorPistola cargador, DatosArma d, bool pulsado)
        {
            if (plasma == null || datosPlasma != d)
            {
                plasma = new ReglaRayoPlasma(d.SegundosCarga, d.SegundosRayo);
                datosPlasma = d;
            }
            EstadoPlasma antes = plasma.Estado;
            bool faltoEnergia = false;
            plasma.RetenerCarga = BocaExterna != null && RetenerCargaExterna; // VR51; en PC siempre false
            bool empezo = plasma.Avanzar(Time.deltaTime, pulsado, () =>
            {
                bool hay = cargador.IntentarDisparar();
                faltoEnergia = !hay;
                return hay;
            }, DisparoExterno);
            // T-1532: GATILLO al apretar, SIN_ENERGIA si la carga acaba sin celda (los bucles de carga y rayo siguen más abajo).
            EventoSonidoPlasma sonidos = ReglaSonidoPlasma.Evaluar(antes, plasma.Estado, empezo, faltoEnergia);
            if ((sonidos & EventoSonidoPlasma.Gatillo) != 0)
            {
                Sonidos.Reproducir(SonidosArmasT1472.PlasmaGatillo);
            }
            if ((sonidos & EventoSonidoPlasma.SinEnergia) != 0)
            {
                PararChispeo();
                Sonidos.Reproducir(SonidosArmasT1472.PlasmaSinEnergia);
            }
            if ((sonidos & EventoSonidoPlasma.Enfriar) != 0)
            {
                Sonidos.Reproducir(SonidosArmasT1472.PlasmaEnfriar);
            }
            Transform boca = BocaDisparo(arma);
            Vector3 origen = boca != null ? boca.position : transform.position + Vector3.up * AlturaManoGranada;
            if (plasma.Estado == EstadoPlasma.Cargando)
            {
                if (antes != EstadoPlasma.Cargando)
                {
                    IniciarChispeo(BocaDisparo(arma));
                }
                float c = plasma.Carga;
                bolaCarga.gameObject.SetActive(true);
                bolaCarga.position = origen;
                bolaCarga.localScale = Vector3.one * (cfg.TamanoCarga * c * (0.9f + 0.1f * Mathf.Sin(Time.time * LatidoRayo)));
                luzRayo.enabled = true;
                luzRayo.transform.position = origen;
                luzRayo.intensity = cfg.IntensidadLuzRayo * c;
                return;
            }
            if (plasma.Estado == EstadoPlasma.Rayo)
            {
                if (empezo)
                {
                    PararChispeo();
                    Sonidos.Reproducir(IdDisparoPlasma); // T-1472: el disparo del jugador suena en 2D
                    PararRayoSonido();
                    Transform bocaRayo = BocaDisparo(arma) != null ? BocaDisparo(arma) : transform;
                    bucleRayo = Sonidos.ReproducirBucle(SonidosArmasT1472.PlasmaRayo, bocaRayo);
                    bucleCapaRayo = Sonidos.ReproducirBucle(SonidosArmasT1472.PlasmaCapaRayo, bocaRayo); // T-1532
                    pistola.AvisarDisparoEspecial(arma, origen);
                    siguienteZumbido = Time.time;
                    acumuladoTic = TicDanioRayo;
                }
                if (BocaExterna == null)
                {
                    controlador.MirarAlDisparar();
                }
                Rayo(origen, d);
                return;
            }
            ApagarPlasma();
        }

        private void Rayo(Vector3 origen, DatosArma d)
        {
            // VR5: con el arma en la mano VR el rayo sigue al cañón, no a la mira de la cámara.
            Vector3 punto = BocaExterna != null ? origen + BocaExterna.forward * d.Alcance : pistola.PuntoDeMira(d.Alcance);
            Vector3 hacia = punto - origen;
            Vector3 direccion = hacia.sqrMagnitude > 0.0001f ? hacia.normalized : transform.forward;
            float largo = d.Alcance;
            bool pega = false;
            tocados.Clear();
            int n = Physics.SphereCastNonAlloc(origen, cfg.RadioRayo, direccion, toques, d.Alcance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(toques, 0, n, Comparador);
            int atravesadas = 0;
            for (int i = 0; i < n; i++)
            {
                RaycastHit t = toques[i];
                if (t.collider.transform.root == transform.root)
                {
                    continue;
                }
                IRecibeDanio objetivo = t.collider.GetComponentInParent<IRecibeDanio>();
                bool esAlguien = objetivo != null && !(objetivo is IObjetoInerte);
                if (objetivo != null)
                {
                    tocados.Add(objetivo);
                }
                if (!esAlguien)
                {
                    // T-1324: la gente no lo para; paredes y objetos solo si ya atravesó el máximo (ReglaDanioRayo).
                    if (!ReglaDanioRayo.AtraviesaOtra(atravesadas, cfg.ParedesQueAtraviesa))
                    {
                        pega = true;
                        largo = t.distance > 0f ? t.distance : 0f;
                        break;
                    }
                    atravesadas++;
                }
            }
            finRayo = origen + direccion * largo;
            acumuladoTic += Time.deltaTime;
            if (acumuladoTic >= TicDanioRayo)
            {
                float danio = d.Danio * acumuladoTic;
                acumuladoTic = 0f;
                foreach (IRecibeDanio objetivo in tocados)
                {
                    IVida vidaObjetivo = objetivo as IVida;
                    float cantidad = ReglaDanioRayo.Calcular(danio, objetivo is IObjetoInerte, vidaObjetivo != null ? vidaObjetivo.Actual : 0f,
                        cfg.MultiplicadorObjetos, cfg.FactorMataGente);
                    objetivo.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Disparo, finRayo, direccion, null));
                }
            }
            if (Time.time >= siguienteZumbido)
            {
                siguienteZumbido = Time.time + cfg.SegundosZumbido;
                Sonidos.ReproducirEn("ARM_BALA_ZUMBIDO", origen);
            }
            rayo.Mostrar(origen, finRayo, pega);
            bolaCarga.gameObject.SetActive(true);
            bolaCarga.position = origen;
            bolaCarga.localScale = Vector3.one * cfg.TamanoCarga * 0.7f;
            luzRayo.enabled = true;
            luzRayo.transform.position = finRayo - direccion * 0.3f;
            luzRayo.intensity = cfg.IntensidadLuzRayo;
        }

        private static readonly IComparer<RaycastHit> Comparador = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        // T-1309: chispeo eléctrico en bucle mientras carga; se corta al disparar, soltar, cancelar o cambiar de arma.
        private void IniciarChispeo(Transform sigue)
        {
            PararChispeo();
            bucleChispeo = Sonidos.ReproducirBucle(IdChispeoPlasma, sigue != null ? sigue : transform);
        }

        private void PararChispeo()
        {
            if (bucleChispeo != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleChispeo);
                bucleChispeo = Sonidos.SinBucle;
            }
        }

        /// <summary>True mientras suena el chispeo de la carga del plasma (para pruebas).</summary>
        public bool ChispeoSonando => bucleChispeo != Sonidos.SinBucle;

        // T-1472: el rayo continuo suena en bucle mientras dura y se corta al acabar, soltar o cambiar de arma.
        private void PararRayoSonido()
        {
            if (bucleRayo != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleRayo);
                bucleRayo = Sonidos.SinBucle;
            }
            if (bucleCapaRayo != Sonidos.SinBucle)
            {
                Sonidos.DetenerBucle(bucleCapaRayo);
                bucleCapaRayo = Sonidos.SinBucle;
            }
        }

        /// <summary>True mientras suena el bucle del rayo de plasma (para pruebas).</summary>
        public bool RayoSonando => bucleRayo != Sonidos.SinBucle;

        private void ApagarPlasma()
        {
            PararChispeo();
            PararRayoSonido();
            // El rayo no se apaga de golpe: EfectoRayoPlasma se desvanece solo si deja de recibir Mostrar.
            if (bolaCarga != null)
            {
                bolaCarga.gameObject.SetActive(false);
            }
            if (luzRayo != null)
            {
                luzRayo.enabled = false;
            }
        }
    }
}

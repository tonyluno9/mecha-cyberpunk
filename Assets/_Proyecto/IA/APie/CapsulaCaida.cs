using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1621: la cápsula de caída CP01 de Nova. Cae del cielo con su enemigo dentro (animación CP01_Abrir en un Animation Legacy), rebota, la tapa sale volando, las paredes se abren,
    /// las piezas se vuelven holograma cian y desaparecen, y el enemigo sale con su clip *_Capsula_Salida (o el dron con su vuelo por script) y pasa a su IA normal.
    /// Todos los tiempos salen de <see cref="CalculoCapsulaCaida"/> (cuadros a 24 fps contados desde que empieza la caída), no de keys del clip.
    ///
    /// Uso: <see cref="Lanzar(GameObject, Vector3, Quaternion, float, Action{GameObject})"/> con el enemigo ya instanciado (activo o apagado; si le pasas un prefab de asset lo instancia).
    /// Mientras dura, el enemigo queda oculto y sin IA, sin colisiones y sin scripts (se guardan y se devuelven tal cual estaban); se le mueve al punto de la cápsula y al terminar
    /// el clip se le devuelve todo, se avisa por <c>alTerminar</c> y la cápsula se destruye. El clip de salida se juega con un PlayableGraph encima del Animator (como <see cref="CaidaNova"/>),
    /// así que no hay estado "SalidaCapsula" que tocar en los controladores: al terminar el clip el Animator vuelve a su controlador (Idle/búsqueda).
    /// </summary>
    public class CapsulaCaida : MonoBehaviour
    {
        // Sonidos que ya existen en el catálogo (no se agregaron IDs nuevos).
        public const string IdImpacto = "ROB_PES_LLEGADA";
        public const string IdTapa = "ROB_PES_HIDRAULICA";
        public const string IdHolograma = "MUN_HOLOGRAMA";
        public const string IdZumbidoDron = "CAL_DRON_ZUMBIDO";

        public const string NombreClip = "CP01_Abrir";

        private const float MetrosBuscarSuelo = 400f;
        private const float MargenTecho = 1f;
        private const float SegundosMinimosSinClip = 1f;

        private static readonly int IdHolo = Shader.PropertyToID("_Holo");
        private static readonly int IdFade = Shader.PropertyToID("_Fade");

        [Tooltip("La tapa (CP01_Tapa): su holograma empieza en el cuadro 46.")]
        [SerializeField] private Transform tapa;

        [Tooltip("La base y las 4 paredes (CP01_Base, CP01_Pared_*): su holograma empieza en el cuadro 58.")]
        [SerializeField] private Transform[] baseYParedes;

        [Tooltip("El Animation Legacy con el clip CP01_Abrir como clip por omisión.")]
        [SerializeField] private Animation animacion;

        private enum Modo
        {
            Vacia,
            Robot,
            Dron
        }

        private sealed class Pieza
        {
            public Transform Raiz;
            public Renderer[] Renderers;
            public int Inicio;
            public CalculoCapsulaCaida.CurvaHolo Curva;
            public bool SonoInicio;
            public bool Lista;
        }

        private readonly List<Pieza> piezas = new List<Pieza>();
        private readonly List<MonoBehaviour> scriptsApagados = new List<MonoBehaviour>();
        private readonly List<Collider> colisionesApagadas = new List<Collider>();
        private readonly List<Renderer> renderersOcultos = new List<Renderer>();
        private readonly List<(Animator Animador, AnimatorCullingMode Modo)> animadores = new List<(Animator, AnimatorCullingMode)>();
        private MaterialPropertyBlock bloque; // se crea en Awake: Unity no deja crearlo en el inicializador del campo

        private Modo modo = Modo.Vacia;
        private GameObject enemigo;
        private Animator animadorEnemigo;
        private AnimationClip clipSalida;
        private Action<GameObject> alTerminar;
        private Vector3 punto;
        private Quaternion rotacion;
        private bool enemigoEstabaApagado;
        private float tiempo;
        private bool impacto;
        private bool sonoTapa;
        private bool visible;
        private bool clipIniciado;
        private PlayableGraph grafo;
        private AnimationClipPlayable reproduccion;

        /// <summary>True cuando ya devolvió el enemigo a su IA (la cápsula se destruye en ese mismo cuadro).</summary>
        public bool Terminada { get; private set; }

        /// <summary>El enemigo que viene dentro (null si la cápsula va vacía).</summary>
        public GameObject Enemigo => enemigo;

        /// <summary>Segundos desde que empezó la caída.</summary>
        public float Segundos => tiempo;

        /// <summary>Cuadro actual a 24 fps (1 = inicio de la caída).</summary>
        public float Frame => CalculoCapsulaCaida.FrameDeSegundos(tiempo);

        /// <summary>True una vez que el robot o el dron ya se ven dentro de la cápsula.</summary>
        public bool EnemigoVisible => visible;

        // ------------------------------------------------------------------ API pública

        /// <summary>
        /// Cae una cápsula en <paramref name="punto"/> (en el piso), con <paramref name="enemigo"/> dentro, a la escala <paramref name="escala"/> (ver <see cref="CalculoCapsulaCaida.EscalaDe"/>).
        /// <paramref name="enemigo"/> puede ser una instancia ya en la escena (activa o apagada) o un prefab de asset (se instancia). Se coloca en <paramref name="punto"/> con <paramref name="rotacion"/>.
        /// <paramref name="alTerminar"/> recibe al enemigo cuando ya está libre y con su IA. Devuelve null si falta la config o el prefab de la cápsula.
        /// </summary>
        public static CapsulaCaida Lanzar(GameObject enemigo, Vector3 punto, Quaternion rotacion, float escala, Action<GameObject> alTerminar = null)
        {
            return Crear(enemigo, punto, rotacion, escala, alTerminar, false);
        }

        /// <summary>Igual que la anterior, con la escala de la tabla según el tipo del enemigo (1 si no se reconoce).</summary>
        public static CapsulaCaida Lanzar(GameObject enemigo, Vector3 punto, Quaternion rotacion, Action<GameObject> alTerminar = null)
        {
            TipoCapsulaCaida? tipo = TipoDe(enemigo);
            return Crear(enemigo, punto, rotacion, tipo.HasValue ? CalculoCapsulaCaida.EscalaDe(tipo.Value) : 1f, alTerminar, false);
        }

        /// <summary>
        /// Cápsula con un dron dentro: aparece en el cuadro 39 a 0.1 m, queda quieto hasta el 52, sube a 1.6 m en el 64, sigue con el vaivén hasta el 112 y ahí se le devuelve su IA
        /// de vuelo normal (DronMundo / CalculoVueloDron): todos los scripts del dron se apagan mientras dura y se encienden al final. Con 0 o negativa, la escala sale de la tabla por el nombre del dron.
        /// </summary>
        public static CapsulaCaida LanzarDron(GameObject dron, Vector3 punto, Quaternion rotacion, float escala = 0f, Action<GameObject> alTerminar = null)
        {
            if (escala <= 0f)
            {
                escala = CalculoCapsulaCaida.TipoDronDeNombre(dron != null ? dron.name : null, out TipoCapsulaCaida tipo) ? CalculoCapsulaCaida.EscalaDe(tipo) : 0.3f;
            }
            return Crear(dron, punto, rotacion, escala, alTerminar, true);
        }

        /// <summary>El tipo de enemigo de Nova (E15, E16, E17, E18) o de dron (DR01, DR02_a, E13, DR04_a) del objeto, según los nombres de su controlador, avatar y modelo. Null si no lleva cápsula.</summary>
        public static TipoCapsulaCaida? TipoDe(GameObject objeto)
        {
            if (objeto == null)
            {
                return null;
            }
            if (CalculoCapsulaCaida.TipoDronDeNombre(objeto.name, out TipoCapsulaCaida dron))
            {
                return dron;
            }
            var nombres = new List<string> { objeto.name };
            bool perro = false;
            foreach (PandilleroAPie p in objeto.GetComponentsInChildren<PandilleroAPie>(true))
            {
                if (p.Datos != null)
                {
                    nombres.Add(p.Datos.name);
                    perro |= p.Datos.PerroRobot;
                }
            }
            foreach (Animator a in objeto.GetComponentsInChildren<Animator>(true))
            {
                if (a.runtimeAnimatorController != null)
                {
                    nombres.Add(a.runtimeAnimatorController.name);
                }
                if (a.avatar != null)
                {
                    nombres.Add(a.avatar.name);
                }
            }
            foreach (Transform hijo in objeto.transform)
            {
                nombres.Add(hijo.name);
                foreach (Transform nieto in hijo)
                {
                    nombres.Add(nieto.name);
                }
            }
            switch (ReglaSonidoEnemigo.Tipo(nombres, perro))
            {
                case TipoSonoroEnemigo.Centinela: return TipoCapsulaCaida.E15;
                case TipoSonoroEnemigo.RobotPesado: return TipoCapsulaCaida.E16;
                case TipoSonoroEnemigo.Perro: return TipoCapsulaCaida.E17;
                case TipoSonoroEnemigo.Elite: return TipoCapsulaCaida.E18;
                default: return null;
            }
        }

        /// <summary>
        /// Lo usa <see cref="CaidaNova.Iniciar"/> para los refuerzos de Nova que llegan del cielo: si el objeto es un E15, E16, E17 o E18, hay config con clips y sobra espacio libre
        /// arriba del piso (la caída mide 30 m por la escala), lo baja al piso y le lanza una cápsula. Devuelve false si no aplica (entonces sigue la caída vieja con E1x_Caida_*).
        /// </summary>
        public static bool IntentarDesdeCielo(GameObject objeto)
        {
            ConfigCapsulaCaida config = ConfigCapsulaCaida.Actual;
            TipoCapsulaCaida? tipo = TipoDe(objeto);
            if (config == null || config.PrefabCapsula == null || !tipo.HasValue || CalculoCapsulaCaida.EsDron(tipo.Value) || ClipDe(config, tipo.Value) == null)
            {
                return false;
            }
            float escala = CalculoCapsulaCaida.EscalaDe(tipo.Value);
            Vector3 suelo = objeto.transform.position;
            if (!TocarPiso(objeto, objeto.transform.position + Vector3.up * MargenTecho, Vector3.down, MetrosBuscarSuelo, out RaycastHit abajo))
            {
                return false;
            }
            suelo = abajo.point;
            // Que la cápsula (30 m por la escala) no atraviese un techo: si lo hay más abajo, cae el robot por su camino viejo.
            if (TocarPiso(objeto, suelo + Vector3.up * MargenTecho, Vector3.up, CalculoCapsulaCaida.AlturaCaida * escala + MargenTecho, out _))
            {
                return false;
            }
            return Lanzar(objeto, suelo, objeto.transform.rotation, escala, null) != null;
        }

        /// <summary>
        /// Depuración: lanza una cápsula de cada tipo (E16, E18, E15, E17 y los cuatro drones) en fila, a 14 m entre sí, centrada en <paramref name="centro"/> (se le busca el piso). Usa los prefabs de la config.
        /// Devuelve cuántas lanzó.
        /// </summary>
        public static int ProbarTodos(Vector3 centro)
        {
            ConfigCapsulaCaida config = ConfigCapsulaCaida.Actual;
            if (config == null || config.PrefabsPrueba == null)
            {
                Debug.LogError("[IA] CapsulaCaida.ProbarTodos: falta ConfigCapsulaCaida o sus prefabs de prueba (corre CapsulasCaidaT1621.Aplicar).");
                return 0;
            }
            int total = config.PrefabsPrueba.Length;
            int lanzadas = 0;
            for (int i = 0; i < total; i++)
            {
                Vector3 donde = centro + Vector3.right * ((i - (total - 1) * 0.5f) * 14f);
                if (ProbarUno((TipoCapsulaCaida)i, donde) != null)
                {
                    lanzadas++;
                }
            }
            return lanzadas;
        }

        /// <summary>Depuración: lanza una cápsula del tipo dado en <paramref name="donde"/> (se le busca el piso), mirando hacia -Z, con la escala de la tabla. Null si falta algo.</summary>
        public static CapsulaCaida ProbarUno(TipoCapsulaCaida tipo, Vector3 donde)
        {
            ConfigCapsulaCaida config = ConfigCapsulaCaida.Actual;
            int indice = (int)tipo;
            if (config == null || config.PrefabsPrueba == null || indice >= config.PrefabsPrueba.Length || config.PrefabsPrueba[indice] == null)
            {
                Debug.LogError("[IA] CapsulaCaida.ProbarUno: falta el prefab de prueba de " + tipo);
                return null;
            }
            if (Physics.Raycast(donde + Vector3.up * 2f, Vector3.down, out RaycastHit golpe, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                donde = golpe.point;
            }
            Quaternion giro = Quaternion.LookRotation(Vector3.back);
            float escala = CalculoCapsulaCaida.EscalaDe(tipo);
            GameObject prefab = config.PrefabsPrueba[indice];
            return CalculoCapsulaCaida.EsDron(tipo) ? LanzarDron(prefab, donde, giro, escala) : Lanzar(prefab, donde, giro, escala);
        }

        // ------------------------------------------------------------------ Creación

        private static AnimationClip ClipDe(ConfigCapsulaCaida config, TipoCapsulaCaida tipo)
        {
            switch (tipo)
            {
                case TipoCapsulaCaida.E15: return config.SalidaE15;
                case TipoCapsulaCaida.E16: return config.SalidaE16;
                case TipoCapsulaCaida.E18: return config.SalidaE18;
                case TipoCapsulaCaida.E17: return config.SalidaE17;
                default: return null;
            }
        }

        // El primer golpe del rayo que no sea del propio objeto (sus colisiones aún no se han apagado).
        private static bool TocarPiso(GameObject propio, Vector3 origen, Vector3 direccion, float distancia, out RaycastHit mejor)
        {
            RaycastHit[] golpes = Physics.RaycastAll(origen, direccion, distancia, ~0, QueryTriggerInteraction.Ignore);
            mejor = default;
            float cerca = float.MaxValue;
            bool hay = false;
            foreach (RaycastHit g in golpes)
            {
                if (g.collider.transform.IsChildOf(propio.transform) || g.distance >= cerca)
                {
                    continue;
                }
                cerca = g.distance;
                mejor = g;
                hay = true;
            }
            return hay;
        }

        private static CapsulaCaida Crear(GameObject enemigo, Vector3 punto, Quaternion rotacion, float escala, Action<GameObject> alTerminar, bool dron)
        {
            ConfigCapsulaCaida config = ConfigCapsulaCaida.Actual;
            if (config == null || config.PrefabCapsula == null)
            {
                Debug.LogError("[IA] CapsulaCaida: falta ConfigCapsulaCaida en Resources o su prefab de cápsula (corre CapsulasCaidaT1621.Aplicar).");
                return null;
            }
            AnimationClip clip = null;
            if (enemigo != null && !dron)
            {
                TipoCapsulaCaida? tipo = TipoDe(enemigo);
                clip = tipo.HasValue ? ClipDe(config, tipo.Value) : null;
                if (clip == null)
                {
                    Debug.LogWarning("[IA] CapsulaCaida: " + enemigo.name + " no tiene clip de salida de cápsula; sale sin animación propia.");
                }
            }
            if (enemigo != null && !enemigo.scene.IsValid())
            {
                enemigo = Instantiate(enemigo, punto, rotacion); // era un prefab de asset
            }
            GameObject objeto = Instantiate(config.PrefabCapsula, punto, rotacion);
            objeto.name = "CP01_Capsula";
            objeto.transform.localScale = Vector3.one * Mathf.Max(0.01f, escala);
            CapsulaCaida capsula = objeto.GetComponent<CapsulaCaida>();
            if (capsula == null || !capsula.enabled)
            {
                Debug.LogError("[IA] CapsulaCaida: el prefab de la cápsula no lleva el script CapsulaCaida o le falta el Animation con CP01_Abrir.");
                Destroy(objeto);
                return null;
            }
            capsula.Preparar(enemigo, dron ? Modo.Dron : (enemigo != null ? Modo.Robot : Modo.Vacia), clip, punto, rotacion, alTerminar);
            return capsula;
        }

        private static int activas;

        /// <summary>T-historia-libre-23nov: cápsulas que hay ahora en escena (cayendo o abriéndose); el modo libre no lanza más de su tope.</summary>
        public static int Activas => activas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarActivas() => activas = 0;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                activas++;
            }
            bloque = new MaterialPropertyBlock();
            if (tapa == null || baseYParedes == null || baseYParedes.Length == 0)
            {
                BuscarPiezasPorNombre();
            }
            if (animacion == null)
            {
                animacion = GetComponent<Animation>();
            }
            if (animacion == null || animacion.clip == null || tapa == null)
            {
                Debug.LogError("[IA] CapsulaCaida (" + name + "): falta el Animation con CP01_Abrir o la tapa.");
                enabled = false;
                return;
            }
            animacion.playAutomatically = false;
            animacion.cullingType = AnimationCullingType.AlwaysAnimate;
            AgregarPieza(tapa, CalculoCapsulaCaida.FrameHoloTapa);
            foreach (Transform t in baseYParedes)
            {
                AgregarPieza(t, CalculoCapsulaCaida.FrameHoloBase);
            }
        }

        private void BuscarPiezasPorNombre()
        {
            var resto = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("CP01_Tapa"))
                {
                    tapa = t;
                }
                else if (t.name.EndsWith("CP01_Base") || t.name.Contains("CP01_Pared"))
                {
                    resto.Add(t);
                }
            }
            baseYParedes = resto.ToArray();
        }

        private void AgregarPieza(Transform raiz, int inicio)
        {
            if (raiz == null)
            {
                return;
            }
            piezas.Add(new Pieza
            {
                Raiz = raiz,
                Renderers = raiz.GetComponentsInChildren<Renderer>(true),
                Inicio = inicio,
                Curva = new CalculoCapsulaCaida.CurvaHolo(() => UnityEngine.Random.value)
            });
        }

        private void Preparar(GameObject objetoEnemigo, Modo nuevoModo, AnimationClip clip, Vector3 donde, Quaternion giro, Action<GameObject> terminar)
        {
            modo = nuevoModo;
            enemigo = objetoEnemigo;
            clipSalida = clip;
            punto = donde;
            rotacion = giro;
            alTerminar = terminar;
            if (enemigo != null)
            {
                enemigoEstabaApagado = !enemigo.activeSelf;
                AdormecerEnemigo();
            }
            // El FBX de la cápsula viene a 30 fps y la línea de tiempo está en cuadros a 24 fps (la de Blender): se reproduce más lento para que cuadren.
            if (animacion.clip != null && animacion.clip.frameRate > 0f)
            {
                animacion[animacion.clip.name].speed = CalculoCapsulaCaida.Fps / animacion.clip.frameRate;
            }
            animacion.Play();
        }

        // Todo lo que podría estorbar o verse mientras el enemigo está "dentro": scripts, colisiones y mallas se apagan y se guardan para devolverlos.
        private void AdormecerEnemigo()
        {
            foreach (Collider c in enemigo.GetComponentsInChildren<Collider>(true))
            {
                if (c.enabled)
                {
                    c.enabled = false;
                    colisionesApagadas.Add(c);
                }
            }
            foreach (MonoBehaviour m in enemigo.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (m != null && m.enabled && m != this)
                {
                    m.enabled = false;
                    scriptsApagados.Add(m);
                }
            }
            foreach (Renderer r in enemigo.GetComponentsInChildren<Renderer>(true))
            {
                if (r.enabled)
                {
                    r.enabled = false;
                    renderersOcultos.Add(r);
                }
            }
            foreach (Animator a in enemigo.GetComponentsInChildren<Animator>(true))
            {
                animadores.Add((a, a.cullingMode));
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate; // sin mallas visibles Unity pararía la animación
            }
            enemigo.transform.SetPositionAndRotation(punto, rotacion);
            VisualPersonaje visual = enemigo.GetComponentInChildren<VisualPersonaje>(true);
            animadorEnemigo = visual != null && visual.Animador != null ? visual.Animador : enemigo.GetComponentInChildren<Animator>(true);
        }

        private void DespertarEnemigo()
        {
            foreach (Collider c in colisionesApagadas)
            {
                if (c != null)
                {
                    c.enabled = true;
                }
            }
            foreach (Renderer r in renderersOcultos)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }
            foreach ((Animator animador, AnimatorCullingMode modoCulling) in animadores)
            {
                if (animador != null)
                {
                    animador.cullingMode = modoCulling;
                }
            }
            foreach (MonoBehaviour m in scriptsApagados)
            {
                if (m != null)
                {
                    m.enabled = true;
                }
            }
            colisionesApagadas.Clear();
            renderersOcultos.Clear();
            scriptsApagados.Clear();
            animadores.Clear();
        }

        // ------------------------------------------------------------------ Línea de tiempo

        private void Update() => Avanzar(Time.deltaTime);

        /// <summary>Avanza la línea de tiempo <paramref name="dt"/> segundos (lo llama Update; público para las pruebas).</summary>
        public void Avanzar(float dt)
        {
            if (Terminada)
            {
                return;
            }
            tiempo += dt;
            float frame = Frame;
            if (!impacto && frame >= CalculoCapsulaCaida.FrameImpacto)
            {
                Impactar();
            }
            if (!sonoTapa && frame >= CalculoCapsulaCaida.FrameTapa)
            {
                sonoTapa = true;
                Sonidos.ReproducirEn(IdTapa, transform.position);
            }
            if (!visible && frame >= CalculoCapsulaCaida.FrameVisible)
            {
                Mostrar();
            }
            ActualizarHolograma(frame);
            if (modo == Modo.Robot)
            {
                AvanzarRobot(frame);
            }
            else if (modo == Modo.Dron)
            {
                AvanzarDron(frame);
            }
            else if (frame >= CalculoCapsulaCaida.FrameHoloBase + CalculoCapsulaCaida.FramesHolo)
            {
                Terminar();
            }
        }

        private void Impactar()
        {
            impacto = true;
            Sonidos.ReproducirEn(IdImpacto, punto);
            DestelloLlegadaNova.Lanzar(punto + Vector3.up * 0.2f, Resources.Load<ConfigRespuestaNova>("ConfigRespuestaNova"), 0f);
            if (modo == Modo.Robot)
            {
                IniciarClip();
            }
        }

        private void IniciarClip()
        {
            clipIniciado = true;
            if (enemigoEstabaApagado)
            {
                enemigo.SetActive(true); // los scripts ya están apagados: no corre su IA
            }
            if (clipSalida == null || animadorEnemigo == null || !animadorEnemigo.isActiveAndEnabled)
            {
                return;
            }
            grafo = PlayableGraph.Create("CapsulaCaida");
            grafo.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput salida = AnimationPlayableOutput.Create(grafo, "Salida", animadorEnemigo);
            reproduccion = AnimationClipPlayable.Create(grafo, clipSalida);
            salida.SetSourcePlayable(reproduccion);
            grafo.Play();
        }

        private void Mostrar()
        {
            visible = true;
            if (enemigo == null)
            {
                return;
            }
            foreach (Renderer r in renderersOcultos)
            {
                if (r != null)
                {
                    r.enabled = true;
                }
            }
            if (modo == Modo.Dron)
            {
                if (enemigoEstabaApagado)
                {
                    enemigo.SetActive(true);
                }
                Sonidos.ReproducirEn(IdZumbidoDron, punto);
            }
        }

        private void ActualizarHolograma(float frame)
        {
            foreach (Pieza p in piezas)
            {
                if (p.Lista)
                {
                    continue;
                }
                float d = frame - p.Inicio;
                if (d < 0f)
                {
                    continue;
                }
                if (!p.SonoInicio)
                {
                    p.SonoInicio = true;
                    Sonidos.ReproducirEn(IdHolograma, p.Raiz.position);
                }
                CalculoCapsulaCaida.EstadoHolo e = p.Curva.Evaluar(d);
                bloque.SetFloat(IdHolo, e.Holo);
                bloque.SetFloat(IdFade, e.Fade);
                foreach (Renderer r in p.Renderers)
                {
                    r.SetPropertyBlock(bloque);
                }
                if (e.Desactivar)
                {
                    p.Raiz.gameObject.SetActive(false);
                    p.Lista = true;
                }
            }
        }

        private void AvanzarRobot(float frame)
        {
            float inicioClip = CalculoCapsulaCaida.SegundosDeFrame(CalculoCapsulaCaida.FrameInicioClip);
            float duracion = clipSalida != null ? clipSalida.length : SegundosMinimosSinClip;
            if (clipIniciado && tiempo - inicioClip >= duracion && TodasLasPiezasListas())
            {
                Terminar();
            }
        }

        private void AvanzarDron(float frame)
        {
            if (enemigo == null)
            {
                // El dron se retiró o se destruyó mientras la cápsula lo bajaba: la cápsula termina sola.
                Terminar();
                return;
            }
            if (visible)
            {
                float altura = CalculoCapsulaCaida.AlturaDron(frame);
                Quaternion giro = rotacion * Quaternion.Euler(0f, CalculoCapsulaCaida.GiroDron(frame), 0f);
                enemigo.transform.SetPositionAndRotation(punto + Vector3.up * altura, giro);
            }
            if (frame >= CalculoCapsulaCaida.FrameDronFin && TodasLasPiezasListas())
            {
                Terminar();
            }
        }

        private bool TodasLasPiezasListas()
        {
            foreach (Pieza p in piezas)
            {
                if (!p.Lista)
                {
                    return false;
                }
            }
            return true;
        }

        private void Terminar()
        {
            if (Terminada)
            {
                return;
            }
            Terminada = true;
            LiberarGrafo();
            if (enemigo != null)
            {
                DespertarEnemigo();
                if (modo == Modo.Robot)
                {
                    enemigo.transform.SetPositionAndRotation(punto, rotacion);
                }
                alTerminar?.Invoke(enemigo);
            }
            Destroy(gameObject);
        }

        private void LiberarGrafo()
        {
            if (grafo.IsValid())
            {
                grafo.Destroy();
            }
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                activas = Mathf.Max(0, activas - 1);
            }
            LiberarGrafo();
            if (!Terminada && enemigo != null)
            {
                DespertarEnemigo(); // la cápsula se quitó a la mitad: que el enemigo no se quede sin IA ni mallas
            }
        }
    }
}

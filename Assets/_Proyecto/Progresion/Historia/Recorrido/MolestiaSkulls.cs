using System;
using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Movimiento;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T-1625 (T10, paso 3): dos SKULLS le salen al paso al jugador, lo empujan y lo tiran; se ríen y se van, y él se levanta con la animación de
    /// levantarse de <see cref="AtropelloJugador"/> (la misma del atropello). Durante la escena el jugador NO puede atacar: puños y armas quedan apagados
    /// y el intento de pegar saca un aviso discreto. Los SKULLS se llevan con <see cref="PandilleroAPie.Acosando"/> (la IA solo les pone la gravedad) y se
    /// mueven con <see cref="PandilleroAPie.MoverPlano"/>; se destruyen lejos de la vista.
    /// T-1656 (T31): salen del callejón, esperan parados a media calle, vienen cuando el jugador camina (con rodeo de obstáculos) y lo tiran donde lo
    /// encuentren. Sin voz ni subtítulos (<see cref="HablanSkulls"/>). Caminan con el clip de PJ01 a la velocidad del clip, sin deslizarse.
    /// </summary>
    public class MolestiaSkulls : MonoBehaviour
    {
        public const string SonidoInsulto = "SKU_INSULTO";
        public const string SonidoRisa = "SKU_RISA_BURLA";
        public const string SonidoProvocar = "SKU_PROVOCAR";
        private const float SegundosInsulto = 1.6f;
        private const float SegundosRisa = 2.2f;
        private const float SegundosHuida = 5f;
        private const float SegundosMaximosLevantarse = 7f;
        private const float SegundosAvisoRepetido = 3f;
        private const float GradosGiroPorSegundo = 360f;

        // T-1656: las rapideces salen del clip de caminar de PJ01 (1.293 m/s): el visual escala el Animator contra esa medida y los pies no patinan.
        private const float FactorSalir = 1.7f;       // ~2.2 m/s: sigue siendo "Caminar" sin verse apresurado (tope del visual: 2.2x)
        private const float FactorVenir = 1.7f;
        private const float RapidezCorrer = 5.5f;     // pasa al estado Correr (umbral 5.5) con el clip de correr
        private const float RapidezAlcanzar = 6.2f;   // si el jugador está lejos, vienen corriendo
        private const float DistanciaAlcanzar = 7f;
        private const float DistanciaPlantarse = 3.2f;
        private const float DistanciaContacto = 0.8f;
        private const float MargenEspera = 0.5f;
        private const float MetrosParaVenir = 1.5f;
        private const float SegundosMovimientoParaVenir = 0.6f;
        private const float RapidezJugadorMoviendose = 0.5f;
        private const float SegundosHastaContacto = 0.4f; // PJ01_Pandillero_Empujar (23 cuadros): la mano llega al pecho a ~0.4 s
        private const float SegundosMaximosSalir = 30f;
        private const float SegundosMaximosEspera = 60f;
        private const float SegundosMaximosVenir = 25f;
        private const float SegundosEntreFrases = 3.4f;
        private const float AlturaBoca = 1.7f;
        private const float RadioRodeo = 0.35f;
        private const float LargoRodeo = 1.6f;
        private const float RadioLlegadaRuta = 0.6f;
        private static readonly float[] AngulosRodeo = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f };
        private static readonly float[] AngulosPared = { 0f, 45f, -45f, 90f, -90f };
        private static readonly RaycastHit[] golpesRodeo = new RaycastHit[12];
        private static readonly Vector3[] esquinasRuta = new Vector3[48];
        private static readonly Color ColorSkull = new Color(1f, 0.25f, 0.85f);
        private static readonly string[] FrasesPlatica =
        {
            "¿Y ese quién es?", "Mira nomás, un pelado perdido.", "Ni se ha dado cuenta.", "Va solito, el valiente.", "Pa' mí que trae lana."
        };
        private static readonly string[] FrasesEnfrente = { "¿A dónde tan solito, pelado?", "Aquí se paga el paso." };

        /// <summary>
        /// T-1656: los SKULLS hablan (subtítulo y voz) solo si esto es true. Apagado a pedido de Tony hasta que diga qué audios usan; para reactivar basta
        /// ponerlo en true (por ejemplo desde un bootstrap o una prueba).
        /// </summary>
        public static bool HablanSkulls { get; set; }

        private readonly List<PandilleroAPie> skulls = new List<PandilleroAPie>();
        private readonly List<Behaviour> apagados = new List<Behaviour>();
        private readonly Dictionary<PandilleroAPie, Ruta> rutas = new Dictionary<PandilleroAPie, Ruta>();
        private ControladorAPie controlador;
        private AtropelloJugador atropello;
        private Transform jugador;
        private bool controlAntes;
        private float ultimoAviso = -100f;
        private Coroutine escena;
        private Vector3 puntoSalida;
        private Vector3 puntoEspera;
        private bool hayEspera;
        private Vector3 jugadorBase;

        private sealed class Ruta
        {
            public readonly NavMeshPath Camino = new NavMeshPath();
            public int Indice;
            public int Cuenta;
            public float Proxima;
        }

        /// <summary>Depuración: qué fase va ("salen", "esperan", "vienen", "plantan", "empujan", "huyen", "") para las pruebas y capturas.</summary>
        public string Fase { get; private set; } = string.Empty;

        /// <summary>
        /// Respaldo cuando no hay marcador Historia_CallejonSkulls: un punto a unos 15-20 m de <paramref name="inicio"/>, a un lado de la calle (perpendicular a
        /// <paramref name="adelante"/>), con piso debajo y sin colisionadores encima. Devuelve false si no encuentra hueco (quien llama usa otra cosa).
        /// </summary>
        public static bool BuscarSalidaRespaldo(Vector3 inicio, Vector3 adelante, out Vector3 punto)
        {
            adelante = Plano(adelante);
            adelante = adelante.sqrMagnitude > 0.0001f ? adelante.normalized : Vector3.forward;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            float[] distancias = { 17f, 15f, 20f, 18f };
            float[] avances = { 0f, 4f, -4f, 8f, -8f };
            foreach (float d in distancias)
            {
                foreach (float a in avances)
                {
                    for (int signo = -1; signo <= 1; signo += 2)
                    {
                        Vector3 candidato = inicio + lado * (d * signo) + adelante * a;
                        if (HuecoLibre(candidato, out punto))
                        {
                            return true;
                        }
                    }
                }
            }
            punto = default;
            return false;
        }

        /// <summary>
        /// T-1656: respaldo cuando no hay marcador Historia_EsperaSkulls: un punto a media calle, unos 5-10 m adelante de <paramref name="inicio"/> (Inicio_Calle,
        /// de cara a la calle), con piso debajo y sin colisionadores encima. Devuelve false si no encuentra hueco.
        /// </summary>
        public static bool BuscarEsperaRespaldo(Vector3 inicio, Vector3 adelante, out Vector3 punto)
        {
            adelante = Plano(adelante);
            adelante = adelante.sqrMagnitude > 0.0001f ? adelante.normalized : Vector3.forward;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            float[] distancias = { 7f, 6f, 8f, 5f, 9f, 10f };
            float[] laterales = { 0f, 2.5f, -2.5f, 5f, -5f };
            foreach (float d in distancias)
            {
                foreach (float l in laterales)
                {
                    if (HuecoLibre(inicio + adelante * d + lado * l, out punto))
                    {
                        return true;
                    }
                }
            }
            punto = default;
            return false;
        }

        private static bool HuecoLibre(Vector3 candidato, out Vector3 punto)
        {
            if (Physics.Raycast(candidato + Vector3.up * 8f, Vector3.down, out RaycastHit golpe, 20f, ~0, QueryTriggerInteraction.Ignore)
                && !Physics.CheckCapsule(golpe.point + Vector3.up * 0.4f, golpe.point + Vector3.up * 1.7f, 0.4f, ~0, QueryTriggerInteraction.Ignore))
            {
                punto = golpe.point + Vector3.up * 0.05f;
                return true;
            }
            punto = default;
            return false;
        }

        /// <summary>True mientras la escena corre.</summary>
        public bool EnCurso => escena != null;

        /// <summary>True si los puños y las armas del jugador están apagados por la escena.</summary>
        public bool AtaqueBloqueado => apagados.Count > 0;

        /// <summary>Los SKULLS de la escena (para pruebas).</summary>
        public IReadOnlyList<PandilleroAPie> Skulls => skulls;

        /// <summary>Se avisa al terminar (el jugador ya se levantó y los SKULLS se fueron).</summary>
        public event Action Terminada;

        /// <summary>
        /// Arranca la escena. <paramref name="prefabs"/> trae uno o dos prefabs de SKULL; <paramref name="distancia"/> es qué tan adelante del jugador salen.
        /// Devuelve false (sin hacer nada) si falta el jugador o los prefabs: quien llama sigue con la historia.
        /// </summary>
        public bool Iniciar(IReadOnlyList<GameObject> prefabs, float distancia) => Iniciar(prefabs, distancia, null, null);

        /// <summary>
        /// T-1652: como <see cref="Iniciar(IReadOnlyList{GameObject}, float)"/> pero los SKULLS salen de <paramref name="salida"/> (el callejón junto a la casa).
        /// Con null salen <paramref name="distancia"/> metros adelante del jugador, como antes.
        /// </summary>
        public bool Iniciar(IReadOnlyList<GameObject> prefabs, float distancia, Vector3? salida) => Iniciar(prefabs, distancia, salida, null);

        /// <summary>
        /// T-1656: como la anterior pero con <paramref name="espera"/>, el punto a media calle donde esperan hasta que el jugador camine. Con null
        /// esperan a medio camino entre la salida y el jugador.
        /// </summary>
        public bool Iniciar(IReadOnlyList<GameObject> prefabs, float distancia, Vector3? salida, Vector3? espera)
        {
            IJugador j = Registro<IJugador>.Actual;
            if (escena != null || j == null || j.Raiz == null || prefabs == null || prefabs.Count == 0 || prefabs[0] == null)
            {
                return false;
            }
            jugador = j.Raiz;
            controlador = jugador.GetComponentInChildren<ControladorAPie>(true);
            atropello = jugador.GetComponent<AtropelloJugador>();
            if (atropello == null && controlador != null)
            {
                atropello = jugador.gameObject.AddComponent<AtropelloJugador>();
            }
            if (controlador == null || atropello == null)
            {
                return false;
            }
            Vector3 adelante = Vector3.ProjectOnPlane(jugador.forward, Vector3.up).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, adelante);
            for (int i = 0; i < 2; i++)
            {
                GameObject prefab = prefabs[Mathf.Min(i, prefabs.Count - 1)] != null ? prefabs[Mathf.Min(i, prefabs.Count - 1)] : prefabs[0];
                Vector3 baseSalida = salida.HasValue ? salida.Value : jugador.position + adelante * distancia;
                Vector3 donde = Suelo(baseSalida + lado * (i == 0 ? -0.8f : 0.8f));
                Vector3 mira = Plano(jugador.position - donde);
                Quaternion giro = mira.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(mira.normalized, Vector3.up) : Quaternion.LookRotation(-adelante, Vector3.up);
                GameObject objeto = Instantiate(prefab, donde, giro);
                objeto.name = "SkullRecorrido_" + (i + 1);
                PandilleroAPie p = objeto.GetComponent<PandilleroAPie>() ?? objeto.GetComponentInChildren<PandilleroAPie>(true);
                if (p == null)
                {
                    Destroy(objeto);
                    continue;
                }
                p.Acosando = true;
                if (p.Visual != null)
                {
                    // Sin pose de arma: caminan con las manos sueltas como cualquier peatón.
                    p.Visual.Apuntando = false;
                    p.Visual.Agachado = false;
                    p.Visual.TipoArma = TipoArmaVisual.Ninguna;
                }
                skulls.Add(p);
            }
            if (skulls.Count == 0)
            {
                return false;
            }
            puntoSalida = skulls[0].transform.position;
            hayEspera = espera.HasValue;
            puntoEspera = hayEspera ? Suelo(espera.Value) : Vector3.Lerp(puntoSalida, jugador.position, 0.5f);
            jugadorBase = jugador.position;
            escena = StartCoroutine(Correr());
            return true;
        }

        /// <summary>Corta la escena (salto de paso o fin de partida): quita a los SKULLS y devuelve ataque y control.</summary>
        public void Cancelar()
        {
            if (escena != null)
            {
                StopCoroutine(escena);
                escena = null;
            }
            Limpiar();
        }

        private void OnDestroy() => Cancelar();

        private IEnumerator Correr()
        {
            BloquearAtaque();
            controlAntes = controlador.ControlActivo;
            // 1. Salen del callejón y caminan (con el caminar de PJ01) a esperar a media calle. El jugador sigue libre de moverse (no de pegar).
            Fase = "salen";
            float inicio = Time.time;
            float proximaFrase = Time.time + 0.5f;
            int frase = 0;
            while (Time.time - inicio < SegundosMaximosSalir)
            {
                bool todos = true;
                for (int i = 0; i < skulls.Count; i++)
                {
                    PandilleroAPie p = skulls[i];
                    todos &= Avanzar(p, PuestoEspera(i), Rapidez(p, FactorSalir), MargenEspera);
                }
                if (todos)
                {
                    break;
                }
                if (Time.time >= proximaFrase)
                {
                    proximaFrase = Time.time + SegundosEntreFrases;
                    Platicar(frase++);
                }
                yield return null;
            }
            // 2. Esperan parados a media calle, de cara al jugador, hasta que él empieza a caminar.
            Fase = "esperan";
            Vector3 antes = jugador.position;
            jugadorBase = antes;
            float segundosMoviendose = 0f;
            float inicioEspera = Time.time;
            while (Time.time - inicioEspera < SegundosMaximosEspera)
            {
                foreach (PandilleroAPie p in Vivos())
                {
                    p.MoverPlano(Vector3.zero);
                    p.MirarHacia(Plano(jugador.position - p.transform.position), GradosGiroPorSegundo * 0.5f);
                }
                float dt = Mathf.Max(Time.deltaTime, 0.0001f);
                float rapidezJugador = Plano(jugador.position - antes).magnitude / dt;
                antes = jugador.position;
                segundosMoviendose = rapidezJugador > RapidezJugadorMoviendose ? segundosMoviendose + dt : 0f;
                if (Plano(jugador.position - jugadorBase).magnitude > MetrosParaVenir || segundosMoviendose >= SegundosMovimientoParaVenir)
                {
                    break;
                }
                yield return null;
            }
            // 3. Vienen hacia él cruzando la calle (esquivando lo que haya) hasta tenerlo cerca.
            Fase = "vienen";
            float inicioVenir = Time.time;
            while (Time.time - inicioVenir < SegundosMaximosVenir && DistanciaMinima() > DistanciaPlantarse)
            {
                for (int i = 0; i < skulls.Count; i++)
                {
                    PandilleroAPie p = skulls[i];
                    if (p == null)
                    {
                        continue;
                    }
                    Vector3 haciaJugador = Plano(jugador.position - p.transform.position);
                    float rapidez = haciaJugador.magnitude > DistanciaAlcanzar ? RapidezAlcanzar : Rapidez(p, FactorVenir);
                    Vector3 lado = haciaJugador.sqrMagnitude > 0.0001f ? Vector3.Cross(Vector3.up, haciaJugador.normalized) : Vector3.zero;
                    Avanzar(p, jugador.position + lado * (i == 0 ? 0f : 1.1f), rapidez, 0.1f);
                }
                yield return null;
            }
            // 4. Se le ponen enfrente, desde el ángulo por donde llegaron: uno a 0.8 m y el otro a un lado. El jugador no se acomoda.
            Fase = "plantan";
            controlador.ActivarControl(false);
            List<PandilleroAPie> vivos = Vivos();
            PandilleroAPie empujador = vivos.Count > 0 ? vivos[0] : null;
            PandilleroAPie otro = vivos.Count > 1 ? vivos[1] : null;
            Vector3 vino = empujador != null ? Plano(empujador.transform.position - jugador.position) : -Plano(jugador.forward);
            vino = vino.sqrMagnitude > 0.0001f ? vino.normalized : -Plano(jugador.forward).normalized;
            vino = DireccionLibreDesdeJugador(vino, DistanciaContacto + 0.4f);
            Vector3 lateral = Vector3.Cross(Vector3.up, vino);
            Vector3 puestoFrente = jugador.position + vino * DistanciaContacto;
            Vector3 puestoOtro = jugador.position + vino * 1.5f + lateral * 1.1f;
            float limitePlantarse = Time.time + 6f;
            while (Time.time < limitePlantarse)
            {
                bool listos = IrAlPuesto(empujador, puestoFrente, 0.12f);
                listos &= IrAlPuesto(otro, puestoOtro, 0.3f);
                if (listos)
                {
                    break;
                }
                yield return null;
            }
            AlinearFrenteAlJugador();
            foreach (PandilleroAPie p in Vivos())
            {
                p.MoverPlano(Vector3.zero);
            }
            // 5. Le dicen algo (en silencio mientras HablanSkulls sea false).
            if (empujador != null)
            {
                Frase(empujador, FrasesEnfrente[0], VocesPandilla.Provocar);
            }
            yield return Esperar(SegundosInsulto);
            if (otro != null)
            {
                Frase(otro, FrasesEnfrente[1], VocesPandilla.Insulto);
                yield return Esperar(0.9f);
            }
            // 6. Lo empujan: el clip de empujar y, en el cuadro del contacto, el derribo del jugador (ni antes ni después).
            Fase = "empujan";
            Vector3 direccion = empujador != null ? Plano(jugador.position - empujador.transform.position) : jugador.forward;
            if (empujador != null)
            {
                AlinearFrenteAlJugador();
                if (empujador.Visual == null || !EstadosNpcT1560.Gestear(empujador.Visual, GestoNpcT1560.Empujar))
                {
                    empujador.Visual?.Golpear(false);
                }
                if (HablanSkulls)
                {
                    Sonidos.ReproducirEn(SonidoInsulto, empujador.transform.position);
                }
                yield return Esperar(SegundosHastaContacto);
            }
            controlador.ActivarControl(controlAntes); // un instante, para que el empujón lo mueva; el derribo bloquea caminar y pegar
            Aviso(TablaRecorrido.AvisoSinAtacar);
            atropello.Derribar(direccion);
            yield return Esperar(0.7f);
            // 7. Se ríen un momento y se van CORRIENDO por donde llegaron.
            Fase = "huyen";
            foreach (PandilleroAPie p in Vivos())
            {
                if (HablanSkulls)
                {
                    Sonidos.ReproducirEn(SonidoRisa, p.transform.position);
                }
                p.Visual?.Burlar();
            }
            yield return Esperar(SegundosRisa);
            float finHuida = Time.time + SegundosHuida;
            while (Time.time < finHuida)
            {
                foreach (PandilleroAPie p in Vivos())
                {
                    if (Plano(puntoSalida - p.transform.position).sqrMagnitude < 1f)
                    {
                        Vector3 lejos = Plano(p.transform.position - jugador.position).normalized;
                        p.MirarHacia(lejos, GradosGiroPorSegundo);
                        p.MoverPlano(lejos * RapidezCorrer);
                    }
                    else
                    {
                        Avanzar(p, puntoSalida, RapidezCorrer, 0.5f);
                    }
                }
                yield return null;
            }
            // 8. Espera a que se levante (AtropelloJugador lo levanta solo) y lo suelta.
            float limiteLevantar = Time.time + SegundosMaximosLevantarse;
            while (atropello != null && atropello.Derribado && Time.time < limiteLevantar)
            {
                yield return null;
            }
            Fase = string.Empty;
            escena = null;
            Limpiar();
            Terminada?.Invoke();
        }

        // Camina hacia el puesto (con rodeo) girando de frente a donde va; true si ya llegó (o no existe).
        private bool IrAlPuesto(PandilleroAPie p, Vector3 puesto, float margen)
        {
            if (p == null)
            {
                return true;
            }
            if (Avanzar(p, puesto, Rapidez(p, FactorVenir), margen))
            {
                p.MirarHacia(Plano(jugador.position - p.transform.position), GradosGiroPorSegundo);
                return true;
            }
            return false;
        }

        // m/s para moverse con el caminar de PJ01 a <factor> veces el ritmo del clip (el visual acelera el Animator con esa escala; sin patinar hasta ~2.2x).
        private static float Rapidez(PandilleroAPie p, float factor)
        {
            float escala = p != null && p.Visual != null ? p.Visual.EscalaClips : 1f;
            return ReglasAnimacion.VelocidadClipCaminarSinImplante * escala * factor;
        }

        // Dónde espera cada SKULL: el punto de espera, uno a cada lado (a lo ancho de la mirada hacia el jugador).
        private Vector3 PuestoEspera(int indice)
        {
            Vector3 haciaJugador = Plano(jugadorBase - puntoEspera);
            Vector3 lado = Vector3.Cross(Vector3.up, haciaJugador.sqrMagnitude > 0.01f ? haciaJugador.normalized : Vector3.forward);
            return puntoEspera + lado * (indice == 0 ? -0.8f : 0.8f);
        }

        // Mueve a <p> hacia <meta> con la velocidad dada, mirando a donde va. True (y se queda quieto) si ya está a <margen>.
        private bool Avanzar(PandilleroAPie p, Vector3 meta, float rapidez, float margen)
        {
            if (p == null)
            {
                return true;
            }
            if (Plano(meta - p.transform.position).magnitude <= margen)
            {
                p.MoverPlano(Vector3.zero);
                return true;
            }
            Vector3 dir = DireccionHacia(p, meta);
            if (dir.sqrMagnitude < 0.0001f)
            {
                p.MoverPlano(Vector3.zero);
                return false;
            }
            p.MirarHacia(dir, GradosGiroPorSegundo);
            // Mientras gira va más despacio para no deslizarse de lado.
            float alineado = Mathf.Clamp01((Vector3.Dot(Plano(p.transform.forward).normalized, dir) + 0.3f) / 1.3f);
            p.MoverPlano(dir * rapidez * Mathf.Max(0.15f, alineado));
            return false;
        }

        // Dirección (plano, unitaria) para ir de <p> a <meta>: NavMesh si la ciudad lo tiene; si no, línea recta con rodeo por SphereCast.
        private Vector3 DireccionHacia(PandilleroAPie p, Vector3 meta)
        {
            Vector3 pos = p.transform.position;
            Vector3 directa = Plano(meta - pos);
            if (directa.sqrMagnitude < 0.0001f)
            {
                return Vector3.zero;
            }
            directa.Normalize();
            if (!rutas.TryGetValue(p, out Ruta ruta))
            {
                ruta = new Ruta();
                rutas[p] = ruta;
            }
            if (Time.time >= ruta.Proxima)
            {
                ruta.Proxima = Time.time + 0.5f;
                ruta.Cuenta = 0;
                if (NavMesh.SamplePosition(pos, out NavMeshHit desde, 2f, NavMesh.AllAreas) && NavMesh.SamplePosition(meta, out NavMeshHit hasta, 2.5f, NavMesh.AllAreas)
                    && NavMesh.CalculatePath(desde.position, hasta.position, NavMesh.AllAreas, ruta.Camino) && ruta.Camino.status == NavMeshPathStatus.PathComplete)
                {
                    ruta.Cuenta = ruta.Camino.GetCornersNonAlloc(esquinasRuta);
                    ruta.Indice = 1;
                }
            }
            if (ruta.Cuenta > 1)
            {
                while (ruta.Indice < ruta.Cuenta - 1 && Plano(esquinasRuta[ruta.Indice] - pos).magnitude < RadioLlegadaRuta)
                {
                    ruta.Indice++;
                }
                Vector3 haciaEsquina = Plano(esquinasRuta[Mathf.Min(ruta.Indice, ruta.Cuenta - 1)] - pos);
                if (haciaEsquina.magnitude > 0.1f)
                {
                    return haciaEsquina.normalized;
                }
            }
            foreach (float grados in AngulosRodeo)
            {
                Vector3 candidata = Quaternion.AngleAxis(grados, Vector3.up) * directa;
                if (Libre(p, candidata, LargoRodeo))
                {
                    return candidata;
                }
            }
            return directa;
        }

        // False si un colisionador (que no sea el propio SKULL, el otro, ni el jugador) estorba en el camino.
        private bool Libre(PandilleroAPie p, Vector3 dir, float largo)
        {
            Vector3 origen = p.transform.position + Vector3.up * 0.9f;
            int n = Physics.SphereCastNonAlloc(origen, RadioRodeo, dir, golpesRodeo, largo, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = golpesRodeo[i].collider;
                if (c == null || golpesRodeo[i].distance <= 0f)
                {
                    continue; // ya encimado al arrancar (banqueta, prop rozando): no cuenta como estorbo hacia adelante
                }
                Transform t = c.transform;
                if (t.IsChildOf(p.transform) || (jugador != null && t.IsChildOf(jugador)) || c.GetComponentInParent<PandilleroAPie>() != null)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        // Si hay una pared a <largo> metros del jugador en <dir>, busca otro ángulo libre (±45, ±90) para que el SKULL no quede dentro de algo.
        private Vector3 DireccionLibreDesdeJugador(Vector3 dir, float largo)
        {
            Vector3 origen = jugador.position + Vector3.up * 0.9f;
            foreach (float grados in AngulosPared)
            {
                Vector3 candidata = Quaternion.AngleAxis(grados, Vector3.up) * dir;
                if (!Physics.SphereCast(origen, 0.3f, candidata, out RaycastHit golpe, largo, ~0, QueryTriggerInteraction.Ignore)
                    || golpe.collider.transform.IsChildOf(jugador) || golpe.collider.GetComponentInParent<PandilleroAPie>() != null)
                {
                    return candidata;
                }
            }
            return dir;
        }

        /// <summary>T-1656: texto para depurar: fase, posiciones de los SKULLS y del jugador, y distancia de cada uno al jugador (invocable por reflexión).</summary>
        public static string DepurarEstado()
        {
            MolestiaSkulls m = FindFirstObjectByType<MolestiaSkulls>();
            return m == null ? "MolestiaSkulls: no hay escena en curso." : m.Estado();
        }

        /// <summary>T-1656: lo mismo que <see cref="DepurarEstado"/> para esta instancia.</summary>
        public string Estado()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("MolestiaSkulls fase='").Append(Fase).Append("' enCurso=").Append(EnCurso);
            sb.Append(" espera=").Append(puntoEspera.ToString("F1")).Append(hayEspera ? " (marcador/respaldo)" : " (medio camino)");
            sb.Append(" salida=").Append(puntoSalida.ToString("F1"));
            if (jugador != null)
            {
                sb.Append(" jugador=").Append(jugador.position.ToString("F1"));
            }
            for (int i = 0; i < skulls.Count; i++)
            {
                PandilleroAPie p = skulls[i];
                if (p == null)
                {
                    continue;
                }
                sb.Append(" | skull").Append(i + 1).Append('=').Append(p.transform.position.ToString("F1"));
                if (jugador != null)
                {
                    sb.Append(" d=").Append(Plano(jugador.position - p.transform.position).magnitude.ToString("F1")).Append(" m");
                }
            }
            return sb.ToString();
        }

        // Una frase de la plática: habla uno y luego el otro (solo si HablanSkulls).
        private void Platicar(int indice)
        {
            List<PandilleroAPie> vivos = Vivos();
            if (vivos.Count == 0)
            {
                return;
            }
            PandilleroAPie quien = vivos[indice % vivos.Count];
            Frase(quien, FrasesPlatica[indice % FrasesPlatica.Length], VocesPandilla.Ambiente);
        }

        // T-1656: sin voz ni subtítulos mientras HablanSkulls sea false (Tony dirá qué audios usan).
        private void Frase(PandilleroAPie quien, string texto, string idVoz)
        {
            if (quien == null || !HablanSkulls)
            {
                return;
            }
            Subtitulos.Publicar("SKULL", texto, quien.transform.position + Vector3.up * AlturaBoca, 2.8f, ColorSkull);
            VocesPandilla.Decir(quien.transform, idVoz, true);
        }

        private void Update()
        {
            // Un clic mientras no puede pegar: aviso discreto.
            if (apagados.Count > 0 && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Aviso(TablaRecorrido.AvisoSinAtacar);
            }
        }

        private void Aviso(string texto)
        {
            if (Time.time - ultimoAviso < SegundosAvisoRepetido)
            {
                return;
            }
            ultimoAviso = Time.time;
            MechaCyberpunk.UI.Hud.ColaAvisos.Publicar(texto, MechaCyberpunk.UI.Hud.PrioridadAviso.Normal, MechaCyberpunk.UI.Hud.TipoAviso.Malo, 3f);
        }

        // Puños y armas (a pie) apagados mientras dura la escena; se vuelven a prender igual.
        private void BloquearAtaque()
        {
            apagados.Clear();
            Transform raiz = jugador;
            foreach (GolpeAPie g in raiz.GetComponentsInChildren<GolpeAPie>(true))
            {
                Apagar(g);
            }
            foreach (PistolaAPie g in raiz.GetComponentsInChildren<PistolaAPie>(true))
            {
                Apagar(g);
            }
            foreach (ArmasEspecialesAPie g in raiz.GetComponentsInChildren<ArmasEspecialesAPie>(true))
            {
                Apagar(g);
            }
        }

        private void Apagar(Behaviour componente)
        {
            if (componente != null && componente.enabled)
            {
                componente.enabled = false;
                apagados.Add(componente);
            }
        }

        private void Limpiar()
        {
            foreach (Behaviour b in apagados)
            {
                if (b != null)
                {
                    b.enabled = true;
                }
            }
            apagados.Clear();
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    Destroy(p.gameObject);
                }
            }
            skulls.Clear();
            rutas.Clear();
            if (controlador != null && !controlador.ControlActivo && controlAntes)
            {
                controlador.ActivarControl(true);
            }
        }

        private List<PandilleroAPie> Vivos()
        {
            var lista = new List<PandilleroAPie>(skulls.Count);
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    lista.Add(p);
                }
            }
            return lista;
        }

        private float DistanciaMinima()
        {
            float menor = float.MaxValue;
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    menor = Mathf.Min(menor, Plano(jugador.position - p.transform.position).magnitude);
                }
            }
            return menor;
        }

        private void AlinearFrenteAlJugador()
        {
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    Vector3 hacia = Plano(jugador.position - p.transform.position);
                    if (hacia.sqrMagnitude > 0.0001f)
                    {
                        p.transform.rotation = Quaternion.LookRotation(hacia.normalized, Vector3.up);
                    }
                }
            }
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            if (Physics.Raycast(punto + Vector3.up * 6f, Vector3.down, out RaycastHit golpe, 14f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point + Vector3.up * 0.05f;
            }
            return punto;
        }
    }
}

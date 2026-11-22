using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using UnityEngine;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T12 y T15): dispara las escenas del barrio (gente hablando y bullying de SKULLS y SNAKES) cerca del jugador. Se crea solo al cargar una
    /// escena y no hace nada si no hay jugador. Nunca dos a la vez, con tiempo mínimo entre escenas, sin repetir la misma seguida y no en misión
    /// protegida ni cinemática (<see cref="ReglaEscenasBarrio"/>). Los guiones son <see cref="EscenaBarrio"/> en Resources/Barrio.
    /// Depuración: <see cref="ForzarEscena"/>, <see cref="ForzarBullying"/>, <see cref="ListarEscenas"/>.
    /// </summary>
    public class DirectorEscenasBarrio : MonoBehaviour
    {
        public const string CarpetaRecursos = "Barrio";

        [Header("Frecuencia")]
        [Tooltip("Segundos tras cargar antes de la primera escena (s).")]
        [Min(0f)] [SerializeField] private float primeraEspera = 30f;
        [Tooltip("Mínimo entre el fin de una escena y la siguiente (s).")]
        [Min(0f)] [SerializeField] private float espaciadoMinimo = 30f;
        [Tooltip("Máximo entre el fin de una escena y la siguiente (s).")]
        [Min(0f)] [SerializeField] private float espaciadoMaximo = 70f;
        [Tooltip("Mínimo entre dos bullyings de la misma pandilla (s).")]
        [Min(0f)] [SerializeField] private float esperaBullyingMinima = 60f;
        [Tooltip("Máximo entre dos bullyings de la misma pandilla (s).")]
        [Min(0f)] [SerializeField] private float esperaBullyingMaxima = 180f;
        [Tooltip("Cada cuántos segundos revisa si toca una escena (s).")]
        [Min(0.2f)] [SerializeField] private float periodoRevision = 1.5f;
        [Tooltip("Si faltan actores, reintenta en estos segundos (s).")]
        [Min(1f)] [SerializeField] private float reintentoSinActores = 8f;

        [Header("Dónde (m del jugador)")]
        [Min(0f)] [SerializeField] private float distanciaMinima = 20f;
        [Min(1f)] [SerializeField] private float distanciaMaxima = 40f;
        [Tooltip("Con la escena forzada se busca en este radio (m).")]
        [Min(1f)] [SerializeField] private float distanciaMaximaForzada = 70f;
        [Tooltip("Máximo entre el agresor y su víctima (m).")]
        [Min(1f)] [SerializeField] private float distanciaVictima = 40f;
        [Tooltip("Mínimo entre el agresor y su víctima, para que no aparezca encima (m).")]
        [Min(0f)] [SerializeField] private float distanciaVictimaMinima = 3f;
        [Tooltip("Entre los dos agresores (m).")]
        [Min(1f)] [SerializeField] private float distanciaEntreAgresores = 10f;
        [Tooltip("Entre dos personas que platican (m).")]
        [Min(1f)] [SerializeField] private float distanciaEntrePersonas = 8f;

        private static DirectorEscenasBarrio instancia;
        private static EscenaBarrio[] catalogo;

        private readonly List<AnimacionCivilMundo> civiles = new List<AnimacionCivilMundo>();
        private readonly List<ReglaEscenasBarrio.Candidata> candidatas = new List<ReglaEscenasBarrio.Candidata>();
        private ReglaEscenasBarrio regla;
        private EjecutorEscenaBarrio actual;
        private Coroutine rutina;
        private float proximaRevision;

        /// <summary>Si es false no sale ninguna escena sola (las forzadas sí). Para pruebas que no quieren sorpresas.</summary>
        public static bool Habilitado { get; set; } = true;

        public static DirectorEscenasBarrio Instancia => instancia;
        public static bool HayEscenaEnCurso => instancia != null && instancia.actual != null;
        public static string IdEscenaActual => HayEscenaEnCurso ? instancia.actual.Escena.Id : string.Empty;
        /// <summary>Cuántas escenas han empezado desde que arrancó el juego.</summary>
        public static int Empezadas { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            instancia = null;
            catalogo = null;
            Habilitado = true;
            Empezadas = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CrearAlCargar() => Asegurar();

        private static DirectorEscenasBarrio Asegurar()
        {
            if (instancia != null)
            {
                return instancia;
            }
            GameObject objeto = new GameObject("DirectorEscenasBarrio");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<DirectorEscenasBarrio>();
        }

        private static EscenaBarrio[] Catalogo()
        {
            if (catalogo == null || catalogo.Length == 0)
            {
                catalogo = Resources.LoadAll<EscenaBarrio>(CarpetaRecursos);
            }
            return catalogo;
        }

        private void Awake()
        {
            if (instancia != null && instancia != this)
            {
                Destroy(gameObject);
                return;
            }
            instancia = this;
            regla = new ReglaEscenasBarrio(() => Random.value, primeraEspera, espaciadoMinimo, espaciadoMaximo, esperaBullyingMinima, esperaBullyingMaxima);
        }

        private void OnDestroy()
        {
            CortarActual();
            if (instancia == this)
            {
                instancia = null;
            }
        }

        private void Update()
        {
            if (!Habilitado || Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + periodoRevision;
            if (!regla.PuedeIntentar(Time.time, Bloqueado()))
            {
                return;
            }
            IntentarElegida();
        }

        private static bool Bloqueado()
        {
            IJugador j = Registro<IJugador>.Actual;
            return j == null || j.Raiz == null || MisionProtegida.Activa || CinematicaEnCurso.Activa;
        }

        // ---------- elegir y arrancar ----------

        private void IntentarElegida()
        {
            EscenaBarrio[] todas = Catalogo();
            if (todas.Length == 0)
            {
                regla.Reintentar(Time.time, reintentoSinActores * 4f);
                return;
            }
            candidatas.Clear();
            foreach (EscenaBarrio e in todas)
            {
                candidatas.Add(new ReglaEscenasBarrio.Candidata { Id = e.Id, EsBullying = e.Tipo == TipoEscenaBarrio.Bullying, Pandilla = (int)e.Pandilla, Peso = e.Peso });
            }
            const int intentos = 4;
            for (int i = 0; i < intentos; i++)
            {
                string id = regla.Elegir(candidatas, Time.time);
                if (id == null)
                {
                    break;
                }
                EscenaBarrio escena = Buscar(id);
                EjecutorEscenaBarrio.Reparto reparto = escena != null ? ArmarReparto(escena, false) : null;
                if (reparto != null)
                {
                    Arrancar(escena, reparto);
                    return;
                }
                candidatas.RemoveAll(c => c.Id == id);
                if (candidatas.Count == 0)
                {
                    break;
                }
            }
            regla.Reintentar(Time.time, reintentoSinActores);
        }

        private static EscenaBarrio Buscar(string id)
        {
            foreach (EscenaBarrio e in Catalogo())
            {
                if (e != null && e.Id == id)
                {
                    return e;
                }
            }
            return null;
        }

        private void Arrancar(EscenaBarrio escena, EjecutorEscenaBarrio.Reparto reparto)
        {
            CortarActual();
            regla.Empezar(escena.Id);
            actual = new EjecutorEscenaBarrio(escena, reparto);
            Empezadas++;
            Debug.Log("[Barrio] escena " + escena.Id + " empieza (" + escena.Tipo + ")");
            rutina = StartCoroutine(Correr(actual));
        }

        private IEnumerator Correr(EjecutorEscenaBarrio ejecutor)
        {
            yield return ejecutor.Correr();
            Terminada(ejecutor);
        }

        private void Terminada(EjecutorEscenaBarrio ejecutor)
        {
            ejecutor.Limpiar();
            Debug.Log("[Barrio] escena " + ejecutor.Escena.Id + " termina (" + ejecutor.Motivo + ")");
            if (actual == ejecutor)
            {
                actual = null;
                rutina = null;
                regla.Terminar(Time.time, ejecutor.EsBullying, (int)ejecutor.Escena.Pandilla);
            }
        }

        private void CortarActual()
        {
            if (rutina != null)
            {
                StopCoroutine(rutina);
                rutina = null;
            }
            if (actual != null)
            {
                EjecutorEscenaBarrio viejo = actual;
                actual = null;
                viejo.Abortar();
                viejo.Limpiar();
                Debug.Log("[Barrio] escena " + viejo.Escena.Id + " termina (" + viejo.Motivo + ")");
                if (regla != null)
                {
                    regla.Terminar(Time.time, false, 0);
                }
            }
        }

        // ---------- actores ----------

        private EjecutorEscenaBarrio.Reparto ArmarReparto(EscenaBarrio escena, bool forzada)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (jugador == null || jugador.Raiz == null)
            {
                return null;
            }
            Vector3 p = jugador.Raiz.position;
            float min = forzada ? 0f : distanciaMinima;
            float max = forzada ? distanciaMaximaForzada : distanciaMaxima;
            ReunirCiviles();
            return escena.Tipo == TipoEscenaBarrio.Bullying ? ArmarBullying(escena, p, min, max) : ArmarNormal(escena, p, min, max);
        }

        private void ReunirCiviles()
        {
            civiles.Clear();
            foreach (AnimacionCivilMundo c in FindObjectsByType<AnimacionCivilMundo>(FindObjectsSortMode.None))
            {
                if (ActorCivilBarrio.Sirve(c))
                {
                    civiles.Add(c);
                }
            }
        }

        private EjecutorEscenaBarrio.Reparto ArmarNormal(EscenaBarrio escena, Vector3 jugador, float min, float max)
        {
            List<AnimacionCivilMundo> posibles = new List<AnimacionCivilMundo>();
            foreach (AnimacionCivilMundo c in civiles)
            {
                float d = Plano(c.transform.position, jugador);
                if (d >= min && d <= max)
                {
                    posibles.Add(c);
                }
            }
            PreferirMarcador(escena.Id, posibles);
            if (posibles.Count == 0)
            {
                return null;
            }
            AnimacionCivilMundo uno = posibles[Random.Range(0, posibles.Count)];
            AnimacionCivilMundo otro = MasCercano(uno.transform.position, civiles, uno, 0f, distanciaEntrePersonas);
            return new EjecutorEscenaBarrio.Reparto
            {
                Persona1 = new ActorCivilBarrio(uno),
                Persona2 = otro != null ? new ActorCivilBarrio(otro) : null,
                Centro = uno.transform.position
            };
        }

        private EjecutorEscenaBarrio.Reparto ArmarBullying(EscenaBarrio escena, Vector3 jugador, float min, float max)
        {
            List<PandilleroAPie> libres = new List<PandilleroAPie>();
            foreach (PandilleroAPie p in PandilleroAPie.Activos)
            {
                if (Libre(p, escena.Pandilla))
                {
                    float d = Plano(p.transform.position, jugador);
                    if (d >= min && d <= max)
                    {
                        libres.Add(p);
                    }
                }
            }
            if (libres.Count == 0)
            {
                return null;
            }
            // Hay pocos civiles: se toma el pandillero libre que tenga un civil más cerca (no uno al azar que quizá no tenga a nadie).
            PandilleroAPie a1 = null;
            AnimacionCivilMundo victima = null;
            float mejorVictima = float.MaxValue;
            foreach (PandilleroAPie p in libres)
            {
                PandilleroAPie desde = p;
                AnimacionCivilMundo c = MasCercano(p.transform.position, civiles, null, distanciaVictimaMinima, distanciaVictima, civil => CaminoLibre(desde, civil));
                float d = c != null ? (c.transform.position - p.transform.position).sqrMagnitude : float.MaxValue;
                if (c != null && d < mejorVictima)
                {
                    mejorVictima = d;
                    a1 = p;
                    victima = c;
                }
            }
            if (victima == null)
            {
                return null;
            }
            PandilleroAPie a2 = null;
            if (escena.Agresores > 1)
            {
                float mejor = distanciaEntreAgresores * distanciaEntreAgresores;
                foreach (PandilleroAPie p in libres)
                {
                    float d = (p.transform.position - a1.transform.position).sqrMagnitude;
                    if (p != a1 && d <= mejor && PuedenJuntos(a1, p))
                    {
                        mejor = d;
                        a2 = p;
                    }
                }
            }
            return new EjecutorEscenaBarrio.Reparto
            {
                Agresor1 = a1,
                Agresor2 = a2,
                Victima = new ActorCivilBarrio(victima),
                Centro = victima.transform.position
            };
        }

        // T-1633: libre = en pie, sin pelear con el jugador y de la pandilla de la escena. Los SKULLS fijos de la ciudad están siempre en un
        // GrupoAcoso (Acosando=true): ese era el motivo de que el bullying nunca encontrara a nadie. Ahora se cuentan si su grupo puede prestarlos
        // (acosando y con al menos otro compañero que se queda).
        private static bool Libre(PandilleroAPie p, IdPandilla pandilla)
        {
            if (p == null || !p.isActiveAndEnabled || p.EstaKO || p.EsHostil || p.EsJefe || p.Pandilla != pandilla
                || p.Datos == null || p.Datos.EsPolicia || p.Datos.PerroRobot || p.Datos.JefeCuerpoACuerpo)
            {
                return false;
            }
            if (p.Acosando)
            {
                GrupoAcoso g = p.GetComponentInParent<GrupoAcoso>();
                return g != null && !g.EsPrestado(p) && g.CupoPrestable >= 1;
            }
            // Con el jugador en su territorio los maleantes de calle (SNAKES de Neón Alto incluidos) tienen Alerta pero solo provocan: durante la escena
            // quedan en Acosando (su IA no los mueve) y no se vuelven hostiles por estar cerca; solo si el jugador interviene.
            return !p.Alerta || p.EsMaleanteDeCalle;
        }

        // Dos del mismo GrupoAcoso solo si el grupo conserva a alguien más.
        private static bool PuedenJuntos(PandilleroAPie a, PandilleroAPie b)
        {
            GrupoAcoso g = a.GetComponentInParent<GrupoAcoso>();
            return g == null || g != b.GetComponentInParent<GrupoAcoso>() || g.CupoPrestable >= 2;
        }

        private const float AlturaRayoCamino = 0.9f;
        private const float RadioRayoCamino = 0.25f;

        /// <summary>
        /// T-1652: true si entre el pandillero y el civil no hay pared, puesto ni carro (los pandilleros caminan en línea recta, no hay NavMesh).
        /// Los demás personajes (CharacterController, pandilleros, civiles) no cuentan como obstáculo.
        /// </summary>
        public static bool CaminoLibre(PandilleroAPie desde, AnimacionCivilMundo hasta)
        {
            if (desde == null || hasta == null)
            {
                return false;
            }
            Vector3 a = desde.transform.position + Vector3.up * AlturaRayoCamino;
            Vector3 b = hasta.transform.position + Vector3.up * AlturaRayoCamino;
            Vector3 d = b - a;
            float largo = d.magnitude;
            if (largo < 0.5f)
            {
                return true;
            }
            RaycastHit[] golpes = Physics.SphereCastAll(a, RadioRayoCamino, d / largo, largo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit g in golpes)
            {
                Collider c = g.collider;
                // distance 0: la esfera ya tocaba algo al salir (la pared o la banca junto al pandillero); eso no tapa el camino.
                if (c == null || g.distance <= 0f || c.transform.IsChildOf(desde.transform) || c.transform.IsChildOf(hasta.transform)
                    || c.GetComponentInParent<CharacterController>() != null || c.GetComponentInParent<PandilleroAPie>() != null
                    || c.GetComponentInParent<AnimacionCivilMundo>() != null)
                {
                    continue;
                }
                return false;
            }
            return true;
        }

        private static AnimacionCivilMundo MasCercano(Vector3 desde, List<AnimacionCivilMundo> lista, AnimacionCivilMundo excepto, float min, float max,
            System.Func<AnimacionCivilMundo, bool> admite = null)
        {
            AnimacionCivilMundo mejor = null;
            float menor = max * max;
            float minimo2 = min * min;
            foreach (AnimacionCivilMundo c in lista)
            {
                if (c == excepto || c == null)
                {
                    continue;
                }
                float d = (c.transform.position - desde).sqrMagnitude;
                if (d >= minimo2 && d <= menor && (admite == null || admite(c)))
                {
                    menor = d;
                    mejor = c;
                }
            }
            return mejor;
        }

        // Si hay un marcador de esta escena (o genérico) con civiles dentro de su radio, se queda solo con esos.
        private static void PreferirMarcador(string idEscena, List<AnimacionCivilMundo> posibles)
        {
            List<AnimacionCivilMundo> enMarcador = new List<AnimacionCivilMundo>();
            foreach (MarcadorEscenaBarrio m in MarcadorEscenaBarrio.Activos)
            {
                if (!string.IsNullOrEmpty(m.IdEscena) && m.IdEscena != idEscena)
                {
                    continue;
                }
                foreach (AnimacionCivilMundo c in posibles)
                {
                    if (Plano(c.transform.position, m.transform.position) <= m.Radio && !enMarcador.Contains(c))
                    {
                        enMarcador.Add(c);
                    }
                }
            }
            if (enMarcador.Count > 0)
            {
                posibles.Clear();
                posibles.AddRange(enMarcador);
            }
        }

        private static float Plano(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // ---------- depuración (para menú y eval en Play) ----------

        /// <summary>Depuración: corta lo que haya y arranca la escena <paramref name="id"/> con actores en hasta 70 m. Devuelve "ok ..." o por qué no.</summary>
        public static string ForzarEscena(string id)
        {
            if (!Application.isPlaying)
            {
                return "solo en Play";
            }
            DirectorEscenasBarrio d = Asegurar();
            EscenaBarrio escena = Buscar(id);
            if (escena == null)
            {
                return "no existe la escena '" + id + "' (usa ListarEscenas)";
            }
            return d.ForzarInterno(escena);
        }

        /// <summary>Depuración y pruebas: corta lo que haya y arranca <paramref name="escena"/> (un guion que no tiene que estar en Resources).</summary>
        public static string ForzarEscena(EscenaBarrio escena)
        {
            if (!Application.isPlaying)
            {
                return "solo en Play";
            }
            return escena == null ? "escena nula" : Asegurar().ForzarInterno(escena);
        }

        /// <summary>Depuración: arranca un bullying al azar que se pueda armar ahora. Devuelve "ok ..." o por qué no.</summary>
        public static string ForzarBullying() => ForzarBullying(null);

        /// <summary>Depuración: como <see cref="ForzarBullying()"/> pero solo de una pandilla (Calaveras = SKULLS, Viboras = SNAKES); null = cualquiera.</summary>
        public static string ForzarBullying(IdPandilla? pandilla)
        {
            if (!Application.isPlaying)
            {
                return "solo en Play";
            }
            DirectorEscenasBarrio d = Asegurar();
            List<EscenaBarrio> bullyings = new List<EscenaBarrio>();
            foreach (EscenaBarrio e in Catalogo())
            {
                if (e != null && e.Tipo == TipoEscenaBarrio.Bullying && (pandilla == null || e.Pandilla == pandilla.Value))
                {
                    bullyings.Add(e);
                }
            }
            while (bullyings.Count > 0)
            {
                int i = Random.Range(0, bullyings.Count);
                string r = d.ForzarInterno(bullyings[i]);
                if (r.StartsWith("ok"))
                {
                    return r;
                }
                bullyings.RemoveAt(i);
            }
            return "ningún bullying se pudo armar (faltan pandilleros libres o civiles cerca)";
        }

        /// <summary>Depuración: lista los IDs de las escenas (y los escribe en el log).</summary>
        public static string[] ListarEscenas()
        {
            EscenaBarrio[] todas = Catalogo();
            string[] ids = new string[todas.Length];
            for (int i = 0; i < todas.Length; i++)
            {
                ids[i] = todas[i].Id + " (" + todas[i].Tipo + (todas[i].Tipo == TipoEscenaBarrio.Bullying ? ", " + todas[i].Pandilla : string.Empty) + ")";
            }
            Debug.Log("[Barrio] escenas: " + string.Join(", ", ids));
            return ids;
        }

        private string ForzarInterno(EscenaBarrio escena)
        {
            if (regla == null)
            {
                return "director sin iniciar";
            }
            CortarActual();
            EjecutorEscenaBarrio.Reparto reparto = ArmarReparto(escena, true);
            if (reparto == null)
            {
                return "sin actores para '" + escena.Id + "' (" + (escena.Tipo == TipoEscenaBarrio.Bullying ? "pandilleros " + escena.Pandilla + " libres y un civil cerca" : "un civil cerca") + ")";
            }
            Arrancar(escena, reparto);
            return "ok " + escena.Id;
        }
    }
}

using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.AI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T94: la conducta de un SKULL del goteo de la fase 0 del modo libre (<see cref="GoteoSkullsLibre"/>). Aparece lejos y fuera de la vista y viene a
    /// buscar al jugador por el NavMesh. El que pelea a golpes (la mayoría) se queda con esta conducta: se acerca, pega con el alcance, el daño y el ritmo
    /// de sus datos (<see cref="PandilleroAPie.Datos"/>) y, como en una pandilla, solo <c>MaxAtacando</c> pegan a la vez (los demás esperan alrededor).
    /// El armado (pistola) solo se acerca: al llegar a su radio de detección suelta el turno y pelea con su IA de siempre, con su "hogar" siempre en
    /// donde está para que la correa no lo regrese al punto donde apareció; si el jugador se le va más lejos que la correa, vuelve a buscarlo.
    /// </summary>
    public class ConductaSkullLibre : MonoBehaviour, IConductaAPie
    {
        private const float GradosGiroPorSegundo = 540f;
        private const float SegundosEntreRutas = 0.5f;
        private const float MetrosParaTrotar = 6f;
        private const float MetrosEsquina = 0.5f;
        private const float AlineadoMinimo = 0.15f;

        private PandilleroAPie pandillero;
        private GoteoSkullsLibre goteo;
        private NavMeshPath ruta;
        private float proximaRuta;
        private float contacto = -1f;
        private float proximoGolpe;
        private bool controla = true;

        /// <summary>True si llegó con pistola.</summary>
        public bool Armado { get; private set; }

        public bool Controla => controla;

        /// <summary>El pandillero que lleva (lo cachea en Awake).</summary>
        public PandilleroAPie Pandillero => pandillero;

        /// <summary>Lo pone el goteo justo después de instanciar.</summary>
        public void Iniciar(GoteoSkullsLibre dueno, bool armado)
        {
            goteo = dueno;
            Armado = armado;
            controla = true;
        }

        private void Awake()
        {
            pandillero = GetComponent<PandilleroAPie>();
            ruta = new NavMeshPath();
            if (pandillero == null)
            {
                Debug.LogError("[Historia] T94: ConductaSkullLibre sin PandilleroAPie en " + name, this);
                enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (goteo != null)
            {
                goteo.SoltarTurno(this);
            }
        }

        // El armado: decide cuándo soltar el turno a su IA y le sostiene el hogar (corre aunque la conducta no controle).
        private void Update()
        {
            if (!Armado || pandillero == null || pandillero.EstaKO || pandillero.Datos == null || pandillero.Jugador == null)
            {
                return;
            }
            float distancia = Plano(pandillero.Jugador.position - transform.position).magnitude;
            if (controla && distancia <= pandillero.Datos.RadioDeteccion)
            {
                controla = false;
            }
            else if (!controla && distancia > pandillero.Datos.Correa)
            {
                controla = true;
            }
            if (!controla)
            {
                pandillero.Configurar(transform.position, null); // la correa nunca lo jala de regreso
            }
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            Transform jugador = p.Jugador;
            if (p.Datos == null || jugador == null || !p.JugadorAtacable || CinematicaEnCurso.Activa)
            {
                p.MoverPlano(Vector3.zero);
                if (goteo != null)
                {
                    goteo.SoltarTurno(this);
                }
                return;
            }
            if (!Armado && p.Visual != null)
            {
                // A golpes: sin pose de arma.
                p.Visual.Apuntando = false;
                p.Visual.Agachado = false;
                p.Visual.TipoArma = TipoArmaVisual.Ninguna;
            }
            Vector3 aJugador = Plano(jugador.position - transform.position);
            float distancia = aJugador.magnitude;
            if (Armado)
            {
                Caminar(p, jugador.position, distancia);
                return;
            }
            ResolverContacto(p, jugador);
            bool turno = goteo == null || goteo.PedirTurno(this, p.Datos.MaxAtacando, distancia <= p.Datos.RadioFormacion * 2f);
            float pararse = turno ? Mathf.Max(0.1f, p.Datos.AlcanceGolpe - p.Datos.MargenLlegada) : p.Datos.RadioFormacion;
            if (distancia > pararse)
            {
                Caminar(p, jugador.position, distancia);
            }
            else
            {
                p.MoverPlano(Vector3.zero);
                p.MirarHacia(aJugador, GradosGiroPorSegundo);
            }
            if (turno && contacto < 0f && distancia <= p.Datos.AlcanceGolpe && Time.time >= proximoGolpe)
            {
                p.AnimarGolpe(false);
                contacto = Time.time + p.Datos.PreparacionGolpe;
                proximoGolpe = contacto + p.Datos.RecargaGolpe;
            }
        }

        private void ResolverContacto(PandilleroAPie p, Transform jugador)
        {
            if (contacto < 0f || Time.time < contacto)
            {
                return;
            }
            contacto = -1f;
            Vector3 dir = Plano(jugador.position - transform.position);
            if (dir.magnitude <= p.Datos.AlcanceGolpe + p.Datos.MargenImpacto)
            {
                p.DaniarJugador(p.Datos.DanioGolpe, dir.normalized);
            }
        }

        private void Caminar(PandilleroAPie p, Vector3 meta, float distancia)
        {
            Vector3 dir = Direccion(transform.position, meta);
            if (dir.sqrMagnitude < 0.0001f)
            {
                p.MoverPlano(Vector3.zero);
                return;
            }
            float escala = p.Visual != null ? p.Visual.EscalaClips : 1f;
            float rapidez = (distancia > MetrosParaTrotar ? ReglasAnimacion.VelocidadPJ01Trote : ReglasAnimacion.VelocidadPJ01Caminar) * escala;
            p.MirarHacia(dir, GradosGiroPorSegundo);
            float alineado = Mathf.Clamp01((Vector3.Dot(Plano(transform.forward).normalized, dir) + 0.3f) / 1.3f);
            p.MoverPlano(dir * rapidez * Mathf.Max(AlineadoMinimo, alineado));
        }

        // Hacia la siguiente esquina de la ruta del NavMesh (recalculada cada medio segundo); en línea recta si no hay NavMesh.
        private Vector3 Direccion(Vector3 desde, Vector3 meta)
        {
            if (Time.time >= proximaRuta)
            {
                proximaRuta = Time.time + SegundosEntreRutas;
                if (!NavMesh.CalculatePath(desde, meta, NavMesh.AllAreas, ruta))
                {
                    ruta.ClearCorners();
                }
            }
            Vector3[] esquinas = ruta.corners;
            for (int k = 0; k < esquinas.Length; k++)
            {
                Vector3 d = Plano(esquinas[k] - desde);
                if (d.magnitude > MetrosEsquina)
                {
                    return d.normalized;
                }
            }
            Vector3 recta = Plano(meta - desde);
            return recta.sqrMagnitude > 0.0001f ? recta.normalized : Vector3.zero;
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }
    }
}

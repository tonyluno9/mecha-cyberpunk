using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1107: una riña entre dos <see cref="PandillaAPie"/> (SNAKES contra SKULLS) frente a frente. Cada pandillero lleva una <see cref="ConductaRina"/>.
    /// Estados: Rina (se pegan entre ellos), Liberada (el jugador le pegó a alguno o entró a 6 m: los dos bandos lo atacan como cualquier grupo)
    /// y Terminada (un bando cayó: los que quedan vuelven a su punto). Se rearma cuando el jugador está a más de 80 m y nadie la ve.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class RinaPandillas : MonoBehaviour
    {
        public enum Estado { Rina, Liberada, Terminada }

        [SerializeField] private PandillaAPie bandoA;
        [SerializeField] private PandillaAPie bandoB;

        private readonly List<PandilleroAPie> miembros = new List<PandilleroAPie>();
        private readonly List<ConductaRina> conductas = new List<ConductaRina>();
        private readonly List<Pose> poses = new List<Pose>();
        private readonly List<ReglaRina.Candidato> listaA = new List<ReglaRina.Candidato>();
        private readonly List<ReglaRina.Candidato> listaB = new List<ReglaRina.Candidato>();
        private bool jugadorGolpeo;
        private bool listo;

        public Estado EstadoActual { get; private set; } = Estado.Rina;
        public PandillaAPie BandoA => bandoA;
        public PandillaAPie BandoB => bandoB;
        public IReadOnlyList<PandilleroAPie> Miembros => miembros;
        /// <summary>Cuántas veces se rearmó la riña.</summary>
        public int Rearmes { get; private set; }

        private void Awake()
        {
            if (bandoA == null || bandoB == null)
            {
                Debug.LogError("[IA] Faltan los dos bandos en RinaPandillas de " + name, this);
                enabled = false;
                return;
            }
            // Siempre están (de día y de noche), no se retiran y no piden refuerzos: el orden de ejecución deja esto antes del OnEnable de las pandillas.
            foreach (PandillaAPie bando in new[] { bandoA, bandoB })
            {
                bando.ModoMision = true;
                bando.TerritorioSuprimido = true;
                foreach (PandilleroAPie p in bando.GetComponentsInChildren<PandilleroAPie>(true))
                {
                    miembros.Add(p);
                    poses.Add(new Pose(p.transform.position, p.transform.rotation));
                    ConductaRina c = p.GetComponent<ConductaRina>();
                    if (c == null)
                    {
                        c = p.gameObject.AddComponent<ConductaRina>();
                    }
                    c.Iniciar(this);
                    conductas.Add(c);
                    p.Acosando = true;
                    p.DanioRecibido += AlRecibirDanio;
                }
            }
            listo = true;
        }

        private void OnDestroy()
        {
            foreach (PandilleroAPie p in miembros)
            {
                if (p != null)
                {
                    p.DanioRecibido -= AlRecibirDanio;
                }
            }
        }

        private void AlRecibirDanio(PandilleroAPie quien, InfoDanio info)
        {
            if (EstadoActual == Estado.Rina && !ConductaRina.GolpeDeRinaEnCurso)
            {
                jugadorGolpeo = true;
            }
        }

        private void Update()
        {
            if (!listo)
            {
                return;
            }
            IJugador jugador = Registro<IJugador>.Actual;
            bool hayJugador = jugador != null && jugador.Raiz != null;
            bool aPie = hayJugador && jugador.Modo == ModoJugador.APie;
            float distancia = hayJugador ? DistanciaMinimaAlJugador(jugador.Raiz.position) : float.MaxValue;
            if (EstadoActual == Estado.Rina)
            {
                if (ReglaRina.DebeSoltar(distancia, aPie, jugadorGolpeo))
                {
                    Soltar(Estado.Liberada);
                }
                else if (BandoCaido(bandoA, listaA) || BandoCaido(bandoB, listaB))
                {
                    Soltar(Estado.Terminada);
                }
                return;
            }
            if (ReglaRina.DebeRearmar(distancia, AlgunoVisible()))
            {
                Rearmar();
            }
        }

        private float DistanciaMinimaAlJugador(Vector3 jugador)
        {
            float menor = float.MaxValue;
            foreach (PandilleroAPie p in miembros)
            {
                if (p == null || !p.isActiveAndEnabled || p.EstaKO)
                {
                    continue;
                }
                Vector3 d = p.transform.position - jugador;
                d.y = 0f;
                menor = Mathf.Min(menor, d.magnitude);
            }
            return menor;
        }

        private static bool BandoCaido(PandillaAPie bando, List<ReglaRina.Candidato> lista)
        {
            lista.Clear();
            foreach (PandilleroAPie p in bando.Pandilleros)
            {
                lista.Add(ConductaRina.Describir(p));
            }
            return ReglaRina.BandoCaido(lista);
        }

        private bool AlgunoVisible()
        {
            Camera camara = Camera.main;
            if (camara == null)
            {
                return false;
            }
            foreach (PandilleroAPie p in miembros)
            {
                if (p == null || !p.isActiveAndEnabled)
                {
                    continue;
                }
                Vector3 v = camara.WorldToViewportPoint(p.transform.position + Vector3.up);
                if (v.z > 0f && v.x > 0f && v.x < 1f && v.y > 0f && v.y < 1f)
                {
                    return true;
                }
            }
            return false;
        }

        // Los dos bandos dejan la riña: vuelve su IA normal. Liberada: van contra el jugador. Terminada: regresan a su punto.
        private void Soltar(Estado nuevo)
        {
            EstadoActual = nuevo;
            jugadorGolpeo = false;
            foreach (PandillaAPie bando in new[] { bandoA, bandoB })
            {
                bando.TerritorioSuprimido = false;
            }
            foreach (PandilleroAPie p in miembros)
            {
                p.Acosando = false;
            }
            foreach (ConductaRina c in conductas)
            {
                c.Soltar();
            }
            foreach (PandillaAPie bando in new[] { bandoA, bandoB })
            {
                PandilleroAPie voz = PrimeroEnPie(bando);
                if (nuevo == Estado.Liberada && voz != null)
                {
                    bando.Avisar(voz, "¡Es el de afuera!", true);
                }
                foreach (PandilleroAPie p in bando.Pandilleros)
                {
                    if (p != null && p.isActiveAndEnabled && !p.EstaKO)
                    {
                        p.Alerta = nuevo == Estado.Liberada;
                    }
                }
            }
            Debug.Log("[IA] Riña " + name + ": " + nuevo);
        }

        private static PandilleroAPie PrimeroEnPie(PandillaAPie bando)
        {
            foreach (PandilleroAPie p in bando.Pandilleros)
            {
                if (p != null && p.isActiveAndEnabled && !p.EstaKO)
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>Todos reviven en su punto, de frente al otro bando, y la riña empieza de nuevo.</summary>
        public void Rearmar()
        {
            foreach (PandillaAPie bando in new[] { bandoA, bandoB })
            {
                bando.TerritorioSuprimido = true;
            }
            for (int i = 0; i < miembros.Count; i++)
            {
                PandilleroAPie p = miembros[i];
                p.gameObject.SetActive(true);
                p.Reaparecer();
                p.transform.rotation = poses[i].rotation;
                p.Acosando = true;
                p.Alerta = false;
                conductas[i].Retomar();
            }
            jugadorGolpeo = false;
            Rearmes++;
            EstadoActual = Estado.Rina;
            Debug.Log("[IA] Riña " + name + ": rearmada.");
        }
    }
}

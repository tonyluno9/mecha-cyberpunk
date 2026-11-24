using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using UnityEngine;
using UnityEngine.AI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// VR24 (paso <see cref="PasoRecorrido.PeleaSkulls"/>): tras C03, en el punto de inicio, salen 3 SKULLS normales (los prefabs de
    /// <see cref="DatosRecorrido"/>, con sus clips y voces), lo encaran y pelean a puños. Igual que <see cref="MolestiaSkulls"/>, se llevan con
    /// <see cref="PandilleroAPie.Acosando"/> (su IA solo pone la gravedad; la pistola no sale) y este componente los mueve; el jugador sí les pega
    /// (su daño entra normal por <see cref="PandilleroAPie.RecibirDanio"/>). Pega uno a la vez y nunca dejan KO al jugador (<see cref="ReglaPeleaInicio"/>).
    /// Avisa <see cref="Terminada"/> cuando caen los 3.
    /// </summary>
    public class PeleaInicioSkulls : MonoBehaviour
    {
        private const float GradosGiroPorSegundo = 360f;
        private const float SegundosEntreRutas = 0.5f;
        private const float MetrosLejosParaReubicar = 30f;
        private const float SegundosLejosParaReubicar = 10f;
        private const float MetrosReubicar = 6f;
        private const float MargenContacto = 0.5f;

        private readonly List<PandilleroAPie> skulls = new List<PandilleroAPie>();
        private readonly List<NavMeshPath> rutas = new List<NavMeshPath>();
        private readonly List<float> proximaRuta = new List<float>();
        private readonly List<float> lejosDesde = new List<float>();
        private Transform jugador;
        private IVida vidaJugador;
        private int turno = -1;
        private float proximoGolpe;
        private float contacto = -1f;
        private int quienPega = -1;
        private bool terminada;
        private Vector3 haciaElGrupo = Vector3.forward;

        /// <summary>Se avisa una vez, cuando caen los 3.</summary>
        public event Action Terminada;

        /// <summary>Los SKULLS de la pelea (pruebas).</summary>
        public IReadOnlyList<PandilleroAPie> Skulls => skulls;

        public bool EnCurso => skulls.Count > 0 && !terminada;

        /// <summary>Cuántos ya cayeron.</summary>
        public int Vencidos
        {
            get
            {
                int n = 0;
                foreach (PandilleroAPie p in skulls)
                {
                    if (p == null || p.EstaKO)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        /// <summary>
        /// Arma la pelea delante de <paramref name="inicio"/> (mirando hacia <paramref name="adelante"/>). False sin jugador o sin prefabs
        /// (quien llama sigue con la historia).
        /// </summary>
        public bool Iniciar(IReadOnlyList<GameObject> prefabs, Vector3 inicio, Vector3 adelante)
        {
            IJugador j = Registro<IJugador>.Actual;
            if (skulls.Count > 0 || j == null || j.Raiz == null || prefabs == null || prefabs.Count == 0 || prefabs[0] == null)
            {
                return false;
            }
            jugador = j.Raiz;
            vidaJugador = jugador.GetComponentInChildren<IVida>();
            int total = ReglaPeleaInicio.CantidadSkulls;
            for (int i = 0; i < total; i++)
            {
                GameObject prefab = prefabs[i % prefabs.Count] != null ? prefabs[i % prefabs.Count] : prefabs[0];
                Vector3 donde = Suelo(ReglaPeleaInicio.Aparicion(inicio, adelante, i, total));
                Vector3 mira = Plano(jugador.position - donde);
                Quaternion giro = mira.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(mira.normalized, Vector3.up) : Quaternion.identity;
                GameObject objeto = Instantiate(prefab, donde, giro);
                objeto.name = "SkullPeleaInicio_" + (i + 1);
                PandilleroAPie p = objeto.GetComponent<PandilleroAPie>() ?? objeto.GetComponentInChildren<PandilleroAPie>(true);
                if (p == null)
                {
                    Destroy(objeto);
                    continue;
                }
                p.Acosando = true;
                if (p.Visual != null)
                {
                    // A puños: sin pose de arma.
                    p.Visual.Apuntando = false;
                    p.Visual.Agachado = false;
                    p.Visual.TipoArma = TipoArmaVisual.Ninguna;
                }
                skulls.Add(p);
                rutas.Add(new NavMeshPath());
                proximaRuta.Add(0f);
                lejosDesde.Add(-1f);
            }
            if (skulls.Count == 0)
            {
                return false;
            }
            haciaElGrupo = Plano(skulls[0].transform.position - jugador.position);
            turno = 0;
            proximoGolpe = Time.time + 1.5f;
            VocesPandilla.Decir(skulls[0].transform, VocesPandilla.Provocar, true);
            Debug.Log("[Historia] VR24: salen " + skulls.Count + " SKULLS a pelear en el inicio.");
            return true;
        }

        /// <summary>Quita a los SKULLS (salto de paso o fin de partida).</summary>
        public void Cancelar()
        {
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null)
                {
                    Destroy(p.gameObject);
                }
            }
            skulls.Clear();
            rutas.Clear();
            proximaRuta.Clear();
            lejosDesde.Clear();
            terminada = true;
        }

        /// <summary>Deja de llevar a los SKULLS sin quitarlos (ya cayeron: se quedan tirados y se limpian solos como cualquier cuerpo).</summary>
        public void Soltar()
        {
            skulls.Clear();
            rutas.Clear();
            proximaRuta.Clear();
            lejosDesde.Clear();
            terminada = true;
        }

        private void OnDestroy()
        {
            if (!terminada)
            {
                Cancelar();
            }
        }

        private void Update()
        {
            if (terminada || skulls.Count == 0 || jugador == null)
            {
                return;
            }
            if (ReglaPeleaInicio.Terminada(Vencidos, skulls.Count))
            {
                terminada = true;
                Debug.Log("[Historia] VR24: los 3 SKULLS cayeron.");
                Terminada?.Invoke();
                return;
            }
            if (CinematicaEnCurso.Activa)
            {
                Parar();
                return;
            }
            bool[] vivos = new bool[skulls.Count];
            for (int i = 0; i < skulls.Count; i++)
            {
                vivos[i] = skulls[i] != null && !skulls[i].EstaKO;
            }
            if (turno < 0 || !vivos[turno])
            {
                turno = ReglaPeleaInicio.SiguienteTurno(turno, vivos);
            }
            for (int i = 0; i < skulls.Count; i++)
            {
                if (vivos[i])
                {
                    Mover(i, i == turno);
                }
            }
            ResolverContacto();
            PandilleroAPie atacante = turno >= 0 ? skulls[turno] : null;
            if (atacante != null && contacto < 0f && ReglaPeleaInicio.PuedeGolpear(Plano(jugador.position - atacante.transform.position).magnitude, Time.time, proximoGolpe))
            {
                atacante.AnimarGolpe(false);
                contacto = Time.time + ReglaPeleaInicio.SegundosHastaContacto;
                quienPega = turno;
                proximoGolpe = Time.time + ReglaPeleaInicio.SegundosEntreGolpes;
                turno = ReglaPeleaInicio.SiguienteTurno(turno, vivos);
            }
        }

        private void ResolverContacto()
        {
            if (contacto < 0f || Time.time < contacto)
            {
                return;
            }
            contacto = -1f;
            PandilleroAPie p = quienPega >= 0 && quienPega < skulls.Count ? skulls[quienPega] : null;
            if (p == null || p.EstaKO)
            {
                return;
            }
            Vector3 dir = Plano(jugador.position - p.transform.position);
            if (dir.magnitude > ReglaPeleaInicio.DistanciaGolpe + MargenContacto)
            {
                return; // se apartó a tiempo
            }
            float danio = vidaJugador != null ? ReglaPeleaInicio.Danio(vidaJugador.Actual, vidaJugador.Maxima) : ReglaPeleaInicio.DanioGolpe;
            if (danio > 0f)
            {
                p.DaniarJugador(danio, dir.normalized);
            }
        }

        // El de turno va hasta el jugador; los demás esperan rodeándolo. Siempre de cara a él.
        private void Mover(int i, bool deTurno)
        {
            PandilleroAPie p = skulls[i];
            Vector3 aJugador = Plano(jugador.position - p.transform.position);
            float distancia = aJugador.magnitude;
            VigilarLejos(i, p, distancia);
            Vector3 meta = deTurno ? jugador.position : ReglaPeleaInicio.Puesto(jugador.position, haciaElGrupo, i, skulls.Count);
            float margen = deTurno ? ReglaPeleaInicio.DistanciaPararse : 0.4f;
            float falta = deTurno ? distancia : Plano(meta - p.transform.position).magnitude;
            if (falta <= margen)
            {
                p.MoverPlano(Vector3.zero);
                p.MirarHacia(aJugador, GradosGiroPorSegundo);
                return;
            }
            Vector3 dir = Direccion(i, p.transform.position, meta);
            float rapidez = Rapidez(p, falta > 6f);
            p.MirarHacia(deTurno || falta > 1.5f ? dir : aJugador, GradosGiroPorSegundo);
            float alineado = Mathf.Clamp01((Vector3.Dot(Plano(p.transform.forward).normalized, dir) + 0.3f) / 1.3f);
            p.MoverPlano(dir * rapidez * Mathf.Max(0.15f, alineado));
        }

        // Si uno quedó atorado muy lejos (sin camino), se le acerca al jugador fuera de la pelea para que se pueda terminar.
        private void VigilarLejos(int i, PandilleroAPie p, float distancia)
        {
            if (distancia < MetrosLejosParaReubicar)
            {
                lejosDesde[i] = -1f;
                return;
            }
            if (lejosDesde[i] < 0f)
            {
                lejosDesde[i] = Time.time;
                return;
            }
            if (Time.time - lejosDesde[i] >= SegundosLejosParaReubicar)
            {
                lejosDesde[i] = -1f;
                Vector3 punto = jugador.position + (haciaElGrupo.sqrMagnitude > 0.0001f ? haciaElGrupo.normalized : Vector3.forward) * MetrosReubicar;
                if (NavMesh.SamplePosition(punto, out NavMeshHit golpe, 5f, NavMesh.AllAreas))
                {
                    punto = golpe.position;
                }
                p.Reubicar(Suelo(punto));
            }
        }

        // Hacia la siguiente esquina de la ruta del NavMesh (recalculada cada medio segundo); en línea recta si no hay NavMesh.
        private Vector3 Direccion(int i, Vector3 desde, Vector3 meta)
        {
            NavMeshPath ruta = rutas[i];
            if (Time.time >= proximaRuta[i])
            {
                proximaRuta[i] = Time.time + SegundosEntreRutas;
                if (!NavMesh.CalculatePath(desde, meta, NavMesh.AllAreas, ruta))
                {
                    ruta.ClearCorners();
                }
            }
            Vector3[] esquinas = ruta.corners;
            for (int k = 0; k < esquinas.Length; k++)
            {
                Vector3 d = Plano(esquinas[k] - desde);
                if (d.magnitude > 0.5f)
                {
                    return d.normalized;
                }
            }
            Vector3 recta = Plano(meta - desde);
            return recta.sqrMagnitude > 0.0001f ? recta.normalized : Vector3.zero;
        }

        private void Parar()
        {
            foreach (PandilleroAPie p in skulls)
            {
                if (p != null && !p.EstaKO)
                {
                    p.MoverPlano(Vector3.zero);
                }
            }
        }

        private static float Rapidez(PandilleroAPie p, bool trote)
        {
            float escala = p != null && p.Visual != null ? p.Visual.EscalaClips : 1f;
            return (trote ? ReglasAnimacion.VelocidadPJ01Trote : ReglasAnimacion.VelocidadPJ01Caminar) * escala;
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        private static Vector3 Suelo(Vector3 punto)
        {
            if (Physics.Raycast(punto + Vector3.up * 3f, Vector3.down, out RaycastHit golpe, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                return golpe.point + Vector3.up * 0.05f;
            }
            return punto;
        }
    }
}

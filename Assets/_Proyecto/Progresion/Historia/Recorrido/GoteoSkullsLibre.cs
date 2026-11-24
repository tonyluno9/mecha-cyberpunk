using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using UnityEngine;
using UnityEngine.AI;

namespace MechaCyberpunk.Progresion.Recorrido
{
    /// <summary>
    /// T94: pone a los SKULLS del goteo de la fase 0 del modo libre. <see cref="DirectorModoLibre"/> decide cuántos y cuándo (<see cref="ReglaFaseSkulls"/>)
    /// y este componente los instancia en grupo lejos del jugador y fuera de su vista (<see cref="ReglaPuntoRefuerzo"/>, entre
    /// <see cref="ParametrosRespuesta.MetrosMinimosRefuerzo"/> y <see cref="ParametrosRespuesta.MetrosMaximosAparicionSkulls"/>, con camino por el NavMesh),
    /// les pone <see cref="ConductaSkullLibre"/> y los cuenta. Los vivos se publican en <see cref="DirectorRespuesta.PonerVivosExtraLibre"/> para que el
    /// tope de enemigos del modo libre (24 PC / 12 VR) los incluya, también cuando ya llegó la policía. Prefabs: los SKULLS de <see cref="DatosRecorrido"/>
    /// (a golpes) y el SkullRefuerzo de Resources (a golpes o armado con pistola, siempre en alerta). Código común de PC y VR.
    /// </summary>
    public class GoteoSkullsLibre : MonoBehaviour
    {
        public const string RecursoArmado = "SkullRefuerzo";
        public const string PrefijoNombre = "SkullGoteo_";
        private const int IntentosPunto = 16;
        private const float MetrosMuestreoNavMesh = 4f;
        private const float MetrosEntreDelGrupo = 1.6f;
        private const float MetrosMuestreoGrupo = 1.5f;
        private const float AlturaSobreSuelo = 0.05f;
        private const float SegundosEntreRevisiones = 0.5f;
        private const float FactorLejosParaQuitar = 2f;
        private const float GradosPorVuelta = 360f;

        private ParametrosRespuesta numeros;
        private readonly List<GameObject> prefabsGolpes = new List<GameObject>();
        private GameObject prefabArmado;
        private bool prefabsCargados;
        private bool avisoSinPrefabs;
        private readonly List<PandilleroAPie> vivos = new List<PandilleroAPie>();
        private readonly HashSet<PandilleroAPie> todos = new HashSet<PandilleroAPie>();
        private readonly List<ConductaSkullLibre> atacando = new List<ConductaSkullLibre>();
        private NavMeshPath ruta;
        private float proximaRevision;
        private int publicados = -1;

        /// <summary>Se avisa cuando cae un SKULL del goteo.</summary>
        public event Action<PandilleroAPie> SkullVencido;

        /// <summary>SKULLS del goteo en pie.</summary>
        public int Vivos => vivos.Count;

        /// <summary>SKULLS del goteo que han salido (armados incluidos).</summary>
        public int Generados { get; private set; }

        /// <summary>De esos, cuántos salieron armados.</summary>
        public int GeneradosArmados { get; private set; }

        public void Configurar(ParametrosRespuesta parametros) => numeros = parametros;

        /// <summary>True si el pandillero salió de este goteo (el director no lo cuenta dos veces).</summary>
        public bool EsDelGoteo(PandilleroAPie p) => p != null && todos.Contains(p);

        private void Awake() => ruta = new NavMeshPath();

        private void OnDestroy()
        {
            foreach (PandilleroAPie p in vivos)
            {
                if (p != null)
                {
                    p.Vencido -= AlVencer;
                }
            }
            vivos.Clear();
            DirectorRespuesta.PonerVivosExtraLibre(0);
        }

        private void Update()
        {
            if (Time.time < proximaRevision)
            {
                return;
            }
            proximaRevision = Time.time + SegundosEntreRevisiones;
            Revisar();
        }

        /// <summary>
        /// Pone un grupo de <paramref name="cuantos"/> SKULLS; <paramref name="armado"/> decide uno por uno si trae pistola. Devuelve cuántos salieron
        /// (0 si no hubo jugador, prefabs o un punto válido).
        /// </summary>
        public int Generar(int cuantos, Func<bool> armado)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            if (cuantos <= 0 || jugador == null || jugador.Raiz == null || numeros == null)
            {
                return 0;
            }
            CargarPrefabs();
            if (prefabArmado == null && prefabsGolpes.Count == 0)
            {
                if (!avisoSinPrefabs)
                {
                    avisoSinPrefabs = true;
                    Debug.LogWarning("[Historia] T94: no hay prefabs de SKULLS para el goteo (DatosRecorrido ni Resources/" + RecursoArmado + ").");
                }
                return 0;
            }
            Vector3 posJugador = jugador.Raiz.position;
            Vector3 adelante = Adelante(jugador.Raiz);
            if (!BuscarPunto(posJugador, adelante, out Vector3 centro))
            {
                return 0;
            }
            int salieron = 0;
            for (int i = 0; i < cuantos; i++)
            {
                bool conArma = armado != null && armado() && prefabArmado != null;
                GameObject prefab = conArma ? prefabArmado : ElegirGolpes();
                if (prefab == null)
                {
                    continue;
                }
                Vector3 punto = PuntoDelGrupo(centro, i, cuantos);
                Vector3 mira = posJugador - punto;
                mira.y = 0f;
                Quaternion giro = mira.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(mira.normalized, Vector3.up) : Quaternion.identity;
                GameObject objeto = Instantiate(prefab, punto, giro);
                objeto.name = PrefijoNombre + (Generados + 1) + (conArma ? "_Pistola" : "_Golpes");
                PandilleroAPie p = objeto.GetComponent<PandilleroAPie>();
                if (p == null)
                {
                    p = objeto.GetComponentInChildren<PandilleroAPie>(true);
                }
                if (p == null)
                {
                    Destroy(objeto);
                    continue;
                }
                ConductaSkullLibre conducta = p.gameObject.AddComponent<ConductaSkullLibre>();
                conducta.Iniciar(this, conArma);
                p.ImponerConducta(conducta);
                p.Vencido += AlVencer;
                vivos.Add(p);
                todos.Add(p);
                Generados++;
                GeneradosArmados += conArma ? 1 : 0;
                salieron++;
            }
            if (salieron > 0)
            {
                VocesPandilla.Decir(vivos[vivos.Count - 1].transform, VocesPandilla.Provocar, true);
                Publicar();
                if (numeros.Registrar)
                {
                    Debug.Log("[Historia] T94: goteo de SKULLS, salen " + salieron + " a " + Vector3.Distance(centro, posJugador).ToString("F0")
                        + " m (vivos " + vivos.Count + ", armados en total " + GeneradosArmados + "/" + Generados + ").");
                }
            }
            return salieron;
        }

        /// <summary>
        /// Turno de pegar de un SKULL a golpes: a lo más <paramref name="maximo"/> a la vez. <paramref name="cerca"/>: si ya no está cerca suelta el turno.
        /// </summary>
        public bool PedirTurno(ConductaSkullLibre quien, int maximo, bool cerca)
        {
            for (int i = atacando.Count - 1; i >= 0; i--)
            {
                ConductaSkullLibre c = atacando[i];
                PandilleroAPie p = c != null ? c.Pandillero : null;
                if (p == null || p.EstaKO)
                {
                    atacando.RemoveAt(i);
                }
            }
            if (!cerca)
            {
                atacando.Remove(quien);
                return true; // lejos: que se acerque hasta la distancia de pegar (al llegar pide turno)
            }
            if (atacando.Contains(quien))
            {
                return true;
            }
            if (atacando.Count < Mathf.Max(1, maximo))
            {
                atacando.Add(quien);
                return true;
            }
            return false;
        }

        public void SoltarTurno(ConductaSkullLibre quien) => atacando.Remove(quien);

        private void AlVencer(PandilleroAPie p)
        {
            if (p != null)
            {
                p.Vencido -= AlVencer;
                vivos.Remove(p);
            }
            Publicar();
            SkullVencido?.Invoke(p);
        }

        // Quita los destruidos y a los que se quedaron muy lejos (fuera de la vista; no cuentan como bajas) y publica los vivos.
        private void Revisar()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3? posJugador = jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
            Vector3 adelante = posJugador.HasValue ? Adelante(jugador.Raiz) : Vector3.forward;
            for (int i = vivos.Count - 1; i >= 0; i--)
            {
                PandilleroAPie p = vivos[i];
                if (p == null || p.EstaKO)
                {
                    vivos.RemoveAt(i);
                    continue;
                }
                if (numeros != null && posJugador.HasValue
                    && ReglaPuntoRefuerzo.Vale(posJugador.Value, adelante, p.transform.position, numeros.MetrosMaximosAparicionSkulls * FactorLejosParaQuitar, numeros.GradosVistaRefuerzo))
                {
                    p.Vencido -= AlVencer;
                    vivos.RemoveAt(i);
                    Destroy(p.gameObject);
                }
            }
            Publicar();
        }

        private void Publicar()
        {
            if (vivos.Count != publicados)
            {
                publicados = vivos.Count;
                DirectorRespuesta.PonerVivosExtraLibre(publicados);
            }
        }

        private void CargarPrefabs()
        {
            if (prefabsCargados)
            {
                return;
            }
            prefabsCargados = true;
            prefabArmado = Resources.Load<GameObject>(RecursoArmado);
            DatosRecorrido datos = Resources.Load<DatosRecorrido>(DatosRecorrido.NombreRecurso);
            if (datos != null && datos.PrefabsSkull != null)
            {
                foreach (GameObject g in datos.PrefabsSkull)
                {
                    if (g != null)
                    {
                        prefabsGolpes.Add(g);
                    }
                }
            }
            if (prefabArmado != null)
            {
                prefabsGolpes.Add(prefabArmado); // el SkullRefuerzo también sirve a golpes (la conducta no lo deja sacar el arma)
            }
        }

        private GameObject ElegirGolpes()
        {
            if (prefabsGolpes.Count == 0)
            {
                return prefabArmado;
            }
            return prefabsGolpes[Mathf.Min(prefabsGolpes.Count - 1, (int)(UnityEngine.Random.value * prefabsGolpes.Count))];
        }

        // La cámara (en VR, la cabeza) dice hacia dónde mira; sin cámara, el jugador.
        private static Vector3 Adelante(Transform raiz)
        {
            Camera cam = Camera.main;
            return cam != null ? cam.transform.forward : raiz.forward;
        }

        // Un punto del NavMesh lejos, fuera de la vista y con camino hasta el jugador. Si no hay NavMesh, cualquier suelo que cumpla lejos y fuera de la vista.
        private bool BuscarPunto(Vector3 jugador, Vector3 adelante, out Vector3 punto)
        {
            float minimo = numeros.MetrosMinimosRefuerzo;
            float maximo = Mathf.Max(minimo, numeros.MetrosMaximosAparicionSkulls);
            for (int pasada = 0; pasada < 2; pasada++)
            {
                for (int i = 0; i < IntentosPunto; i++)
                {
                    float angulo = UnityEngine.Random.value * GradosPorVuelta;
                    float distancia = Mathf.Lerp(minimo, maximo, UnityEngine.Random.value);
                    Vector3 candidato = jugador + Quaternion.Euler(0f, angulo, 0f) * Vector3.forward * distancia;
                    if (pasada == 0)
                    {
                        if (!NavMesh.SamplePosition(candidato, out NavMeshHit golpe, MetrosMuestreoNavMesh, NavMesh.AllAreas))
                        {
                            continue;
                        }
                        candidato = golpe.position;
                        if (!ReglaPuntoRefuerzo.Vale(jugador, adelante, candidato, minimo, numeros.GradosVistaRefuerzo)
                            || !NavMesh.CalculatePath(candidato, jugador, NavMesh.AllAreas, ruta) || ruta.status != NavMeshPathStatus.PathComplete)
                        {
                            continue;
                        }
                        punto = candidato + Vector3.up * AlturaSobreSuelo;
                        return true;
                    }
                    if (ReglaPuntoRefuerzo.Vale(jugador, adelante, candidato, minimo, numeros.GradosVistaRefuerzo)
                        && Physics.Raycast(candidato + Vector3.up * MetrosMuestreoNavMesh, Vector3.down, out RaycastHit suelo, MetrosMuestreoNavMesh * 2f, ~0, QueryTriggerInteraction.Ignore)
                        && !Physics.CheckSphere(suelo.point + Vector3.up, MetrosEntreDelGrupo * 0.25f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        punto = suelo.point + Vector3.up * AlturaSobreSuelo;
                        return true;
                    }
                }
            }
            punto = jugador;
            return false;
        }

        private static Vector3 PuntoDelGrupo(Vector3 centro, int i, int total)
        {
            if (i == 0 || total <= 1)
            {
                return centro;
            }
            float angulo = GradosPorVuelta * i / total;
            Vector3 punto = centro + Quaternion.Euler(0f, angulo, 0f) * Vector3.forward * MetrosEntreDelGrupo;
            if (NavMesh.SamplePosition(punto, out NavMeshHit golpe, MetrosMuestreoGrupo, NavMesh.AllAreas))
            {
                return golpe.position + Vector3.up * AlturaSobreSuelo;
            }
            return centro;
        }
    }
}

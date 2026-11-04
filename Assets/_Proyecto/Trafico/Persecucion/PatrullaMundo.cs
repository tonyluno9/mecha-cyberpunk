using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Una patrulla (T-341): recorre el <see cref="GrafoCalles"/> hacia el cruce más cercano al jugador, siempre sobre el eje de las calles,
    /// y se queda donde la distancia de seguimiento se lo permita. Sus luces rojas y azules alternan con <see cref="ConfigPersecucion.PeriodoSirena"/>.
    /// Sin colliders.
    /// </summary>
    public class PatrullaMundo : MonoBehaviour
    {
        private const float GradosPorSegundoGiro = 360f;
        private const float DistanciaMinimaDeNodo = 0.01f;
        // T-533: de cerca deja la calle y va directo hacia el jugador (como en GTA), y por el megáfono le grita cada tanto.
        private const float DistanciaDirecta = 28f;
        private const float DistanciaMegafono = 35f;
        private const float SegundosEntreMegafono = 7f;
        // T-581: de cerca solo va directo si no hay una pared en medio; si no, regresa por donde vino hasta la calle.
        private const float AlturaVista = 1f;
        private const float MargenAlJugador = 3f;
        private const float DistanciaEntreMigas = 2f;
        private const float LlegadaAMiga = 0.3f;
        private const float MargenDelante = 1.5f;
        private const int MaximoDeMigas = 60;
        private int bucleSirena = Sonidos.SinBucle;
        private float proximoMegafono;

        [Tooltip("Luces de la sirena: la primera es roja y la segunda azul; alternan.")]
        [SerializeField] private Light luzRoja;
        [SerializeField] private Light luzAzul;

        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private GrafoCalles calles;
        private Func<Vector3?> posicionJugador;
        private readonly List<int> camino = new List<int>();
        private int indiceEnCamino;
        private int nodoMeta = -1;
        private Vector3 posicion;
        private readonly List<Vector3> rastro = new List<Vector3>();

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, GrafoCalles grafo, int nodoInicial, Func<Vector3?> dondeEstaElJugador)
        {
            calculo = reglas;
            config = numeros;
            calles = grafo;
            posicionJugador = dondeEstaElJugador;
            posicion = grafo.Nodo(nodoInicial);
            camino.Clear();
            camino.Add(nodoInicial);
            indiceEnCamino = 0;
            nodoMeta = nodoInicial;
            rastro.Clear();
            transform.position = posicion;
            if (bucleSirena == Sonidos.SinBucle)
            {
                bucleSirena = Sonidos.ReproducirBucle("CAL_PATRULLA_SIRENA", transform);
            }
        }

        /// <summary>True si va directo al jugador (ya dejó la calle).</summary>
        public bool VaDirecto { get; private set; }

        private void OnDisable()
        {
            Sonidos.DetenerBucle(bucleSirena);
            bucleSirena = Sonidos.SinBucle;
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            Sirena();
            Vector3? jugador = posicionJugador?.Invoke();
            if (!jugador.HasValue)
            {
                return;
            }
            float distancia = DistanciaPlano(posicion, jugador.Value);
            if (distancia <= DistanciaMegafono && Time.time >= proximoMegafono)
            {
                proximoMegafono = Time.time + SegundosEntreMegafono;
                Sonidos.ReproducirEn("CAL_PATRULLA_MEGAFONO", transform.position);
            }
            VaDirecto = distancia <= DistanciaDirecta && CaminoLibre(jugador.Value, distancia);
            if (VaDirecto)
            {
                Directo(jugador.Value, Time.deltaTime);
                return;
            }
            if (rastro.Count > 0)
            {
                Regresar(Time.deltaTime);
                return;
            }
            int nuevaMeta = calles.MasCercano(jugador.Value);
            if (nuevaMeta != nodoMeta || indiceEnCamino >= camino.Count)
            {
                Replanear(nuevaMeta);
            }
            Avanzar(jugador.Value, Time.deltaTime);
        }

        // De cerca: directo hacia el jugador hasta la distancia de seguimiento. Al alejarse vuelve a la calle por el nodo más cercano.
        private void Directo(Vector3 jugador, float dt)
        {
            float paso = calculo.Paso(false, DistanciaPlano(posicion, jugador), dt);
            Vector3 hacia = jugador - posicion;
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.0001f)
            {
                Vector3 direccion = hacia.normalized;
                if (rastro.Count == 0 || Vector3.Distance(rastro[rastro.Count - 1], posicion) >= DistanciaEntreMigas)
                {
                    if (rastro.Count >= MaximoDeMigas)
                    {
                        rastro.RemoveAt(0);
                    }
                    rastro.Add(posicion);
                }
                posicion += direccion * paso;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direccion, Vector3.up), GradosPorSegundoGiro * dt);
            }
            transform.position = posicion;
            int cercano = calles.MasCercano(posicion);
            camino.Clear();
            camino.Add(cercano);
            indiceEnCamino = 0;
            nodoMeta = -1;
        }

        // T-581: nada sólido entre la patrulla y el jugador (se queda MargenAlJugador antes de él: si va en un carro, el carro no cuenta).
        private bool CaminoLibre(Vector3 jugador, float distancia)
        {
            float largo = distancia - MargenAlJugador;
            if (largo <= MargenDelante)
            {
                return true;
            }
            Vector3 origen = posicion + Vector3.up * AlturaVista;
            Vector3 destino = new Vector3(jugador.x, origen.y, jugador.z);
            Vector3 direccion = (destino - origen).normalized;
            return !Physics.Raycast(origen, direccion, largo, ~0, QueryTriggerInteraction.Ignore);
        }

        // T-581: de vuelta a la calle por las migas que dejó al ir directo (nunca en línea recta por donde no sabe qué hay).
        private void Regresar(float dt)
        {
            Vector3 miga = rastro[rastro.Count - 1];
            Vector3 hacia = miga - posicion;
            hacia.y = 0f;
            float paso = calculo.Paso(false, float.MaxValue, dt);
            if (hacia.magnitude <= Mathf.Max(paso, LlegadaAMiga))
            {
                posicion = new Vector3(miga.x, posicion.y, miga.z);
                rastro.RemoveAt(rastro.Count - 1);
            }
            else
            {
                Vector3 direccion = hacia.normalized;
                posicion += direccion * paso;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direccion, Vector3.up), GradosPorSegundoGiro * dt);
            }
            transform.position = posicion;
            int cercano = calles.MasCercano(posicion);
            camino.Clear();
            camino.Add(cercano);
            indiceEnCamino = 0;
            nodoMeta = -1;
        }

        // La patrulla puede estar a medio tramo: el camino nuevo parte del nodo al que iba (o del último nodo si ya iba para atrás).
        private void Replanear(int meta)
        {
            int desde = camino[Mathf.Min(indiceEnCamino, camino.Count - 1)];
            List<int> nuevo = calles.Camino(desde, meta);
            if (nuevo.Count == 0)
            {
                return;
            }
            camino.Clear();
            camino.AddRange(nuevo);
            indiceEnCamino = 0;
            nodoMeta = meta;
        }

        private void Avanzar(Vector3 jugador, float dt)
        {
            float restante = calculo.Paso(false, DistanciaPlano(posicion, jugador), dt);
            while (restante > 0f && indiceEnCamino < camino.Count)
            {
                Vector3 nodo = calles.Nodo(camino[indiceEnCamino]);
                Vector3 haciaNodo = nodo - posicion;
                float distancia = haciaNodo.magnitude;
                if (distancia <= DistanciaMinimaDeNodo || distancia <= restante)
                {
                    posicion = nodo;
                    restante -= distancia;
                    indiceEnCamino++;
                    continue;
                }
                Vector3 direccion = haciaNodo / distancia;
                posicion += direccion * restante;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direccion, Vector3.up), GradosPorSegundoGiro * dt);
                restante = 0f;
            }
            transform.position = posicion;
        }

        private void Sirena()
        {
            bool rojaPrendida = Mathf.FloorToInt(Time.time / config.PeriodoSirena) % 2 == 0;
            if (luzRoja != null)
            {
                luzRoja.enabled = rojaPrendida;
            }
            if (luzAzul != null)
            {
                luzAzul.enabled = !rojaPrendida;
            }
        }

        private static float DistanciaPlano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}

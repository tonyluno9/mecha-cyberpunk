using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Una patrulla (T-341, T-587): recorre el <see cref="GrafoCalles"/> hacia el cruce más cercano al jugador, siempre sobre el eje de las calles.
    /// Solo sabe dónde estás mientras te ve (a menos de <see cref="ConfigPersecucion.RadioVista"/> m y sin pared en medio); si te pierde de vista va a donde
    /// te vio por última vez. Las que "cortan el paso" se adelantan al cruce hacia donde corres. Al bajar el calor <see cref="Retirar"/> la manda por la calle
    /// hasta salir de la vista, donde desaparece. Sus luces rojas y azules alternan con <see cref="ConfigPersecucion.PeriodoSirena"/>. Sin colliders.
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
        private const float AlturaOjos = 2.2f;
        private const float MargenAlJugador = 3f;
        private const float DistanciaEntreMigas = 2f;
        private const float LlegadaAMiga = 0.3f;
        private const float MargenDelante = 1.5f;
        private const int MaximoDeMigas = 60;
        // T-587: velocidad del jugador suavizada y alcance de la cámara para saber si la patrulla se ve.
        private const float SuavizadoVelocidad = 5f;
        private const float VelocidadImposible = 80f;
        private const float AlcanceCamara = 400f;
        private const float SegundosMinimosDeRetiro = 1f;
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
        private Vector3 ultimaVista;
        private Vector3 velocidadJugador;
        private Vector3? jugadorAnterior;
        private int nodoRetiro = -1;
        private float segundosRetirandose;

        /// <summary>True si va directo al jugador (ya dejó la calle).</summary>
        public bool VaDirecto { get; private set; }

        /// <summary>True mientras ve al jugador (cerca y sin pared en medio).</summary>
        public bool VeAlJugador { get; private set; }

        /// <summary>Donde vio al jugador por última vez (a donde va si lo pierde).</summary>
        public Vector3 UltimaVista => ultimaVista;

        /// <summary>Nodo del grafo al que se dirige por la calle (-1 si va directo o regresa).</summary>
        public int NodoMeta => nodoMeta;

        /// <summary>True si esta patrulla se adelanta al cruce hacia donde corre el jugador (en vez de ir a donde está).</summary>
        public bool Intercepta { get; set; }

        /// <summary>True si ya se está yendo: ignora al jugador y sale de la vista por la calle.</summary>
        public bool Retirando { get; private set; }

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
            Retirando = false;
            VeAlJugador = false;
            jugadorAnterior = null;
            velocidadJugador = Vector3.zero;
            // La policía sabe por la radio dónde fue el aviso: ahí va primero.
            ultimaVista = posicionJugador?.Invoke() ?? posicion;
            transform.position = posicion;
            if (bucleSirena == Sonidos.SinBucle)
            {
                bucleSirena = Sonidos.ReproducirBucle("CAL_PATRULLA_SIRENA", transform);
            }
        }

        /// <summary>T-587: deja de perseguir y se va por la calle al nodo más lejano del jugador; desaparece cuando ya no se ve.</summary>
        public void Retirar()
        {
            if (Retirando || calles == null)
            {
                return;
            }
            Retirando = true;
            VaDirecto = false;
            VeAlJugador = false;
            segundosRetirandose = 0f;
            Vector3 referencia = posicionJugador?.Invoke() ?? ultimaVista;
            nodoRetiro = calles.MasLejano(referencia);
            nodoMeta = -1;
            Sonidos.DetenerBucle(bucleSirena);
            bucleSirena = Sonidos.SinBucle;
            PonerLuces(false, false);
        }

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
            Vector3? jugador = posicionJugador?.Invoke();
            if (Retirando)
            {
                Retirada(jugador, Time.deltaTime);
                return;
            }
            Sirena();
            if (!jugador.HasValue)
            {
                return;
            }
            float dt = Time.deltaTime;
            float distanciaReal = DistanciaPlano(posicion, jugador.Value);
            bool libre = CaminoLibre(jugador.Value, distanciaReal, AlturaVista);
            VeAlJugador = distanciaReal <= config.RadioVista && CaminoLibre(jugador.Value, distanciaReal, AlturaOjos);
            MedirVelocidad(jugador.Value, dt);
            if (VeAlJugador)
            {
                ultimaVista = jugador.Value;
            }
            Vector3 objetivo = ultimaVista;
            float distancia = DistanciaPlano(posicion, objetivo);
            if (VeAlJugador && distancia <= DistanciaMegafono && Time.time >= proximoMegafono)
            {
                proximoMegafono = Time.time + SegundosEntreMegafono;
                Sonidos.ReproducirEn("CAL_PATRULLA_MEGAFONO", transform.position);
            }
            VaDirecto = VeAlJugador && distancia <= DistanciaDirecta && libre;
            if (VaDirecto)
            {
                Directo(objetivo, dt);
                return;
            }
            if (rastro.Count > 0)
            {
                Regresar(dt);
                return;
            }
            Vector3 destino = Intercepta && VeAlJugador
                ? ReglaVistaPolicia.PuntoDeCorte(objetivo, velocidadJugador, config.SegundosAdelantoCorte, config.VelocidadMinimaCorte)
                : objetivo;
            int nuevaMeta = calles.MasCercano(destino);
            if (nuevaMeta != nodoMeta || indiceEnCamino >= camino.Count)
            {
                Replanear(nuevaMeta);
            }
            Avanzar(distancia, dt);
        }

        // Velocidad del jugador suavizada (m/s): con ella las patrullas que cortan el paso saben hacia dónde corre.
        private void MedirVelocidad(Vector3 jugador, float dt)
        {
            if (jugadorAnterior.HasValue && dt > 0f)
            {
                Vector3 instantanea = (jugador - jugadorAnterior.Value) / dt;
                instantanea.y = 0f;
                if (instantanea.magnitude < VelocidadImposible)
                {
                    velocidadJugador = Vector3.Lerp(velocidadJugador, instantanea, 1f - Mathf.Exp(-SuavizadoVelocidad * dt));
                }
            }
            jugadorAnterior = jugador;
        }

        // T-587: de camino a la salida de la ciudad; desaparece solo fuera de la vista de la cámara y lejos del jugador.
        private void Retirada(Vector3? jugador, float dt)
        {
            segundosRetirandose += dt;
            if (rastro.Count > 0)
            {
                Regresar(dt);
            }
            else
            {
                if (nodoMeta != nodoRetiro)
                {
                    Replanear(nodoRetiro);
                }
                Avanzar(float.MaxValue, dt);
            }
            bool lejos = !jugador.HasValue || DistanciaPlano(posicion, jugador.Value) >= config.DistanciaRetiro;
            bool sinVerse = lejos && !ReglaVistaPolicia.LoVe(Camera.main, posicion, AlcanceCamara);
            if ((sinVerse && segundosRetirandose >= SegundosMinimosDeRetiro) || segundosRetirandose >= config.SegundosMaximosRetiro)
            {
                Destroy(gameObject);
            }
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
        private bool CaminoLibre(Vector3 jugador, float distancia, float altura)
        {
            float largo = distancia - MargenAlJugador;
            if (largo <= MargenDelante)
            {
                return true;
            }
            Vector3 origen = posicion + Vector3.up * altura;
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

        private void Avanzar(float distanciaAlObjetivo, float dt)
        {
            float restante = calculo.Paso(false, distanciaAlObjetivo, dt);
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
            PonerLuces(rojaPrendida, !rojaPrendida);
        }

        private void PonerLuces(bool roja, bool azul)
        {
            if (luzRoja != null)
            {
                luzRoja.enabled = roja;
            }
            if (luzAzul != null)
            {
                luzAzul.enabled = azul;
            }
        }

        private static float DistanciaPlano(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}

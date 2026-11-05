using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Calle;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Crea y retira a los perseguidores según el calor (T-341). Escucha <c>ICalor.NivelCambio</c> (el calor aparece cuando el jugador ya está en escena, por eso
    /// se engancha al registro) y deja drones y patrullas con las cantidades de <see cref="CalculoPersecucion"/>. Al entrar el jugador a la casa borra el calor
    /// y por lo tanto retira a todos. Las calles salen de las manzanas (hijos "Manzana_N") del objeto padre, normalmente Barrio.
    /// </summary>
    public class GestorPersecucion : MonoBehaviour
    {
        private const string PrefijoManzana = "Manzana_";
        private const float LadoManzana = 80f;          // medida del barrio (CreadorBarrio, m)
        private const float AlturaCalles = 0.2f;        // sobre la banqueta (m)
        private const float GradosPorVuelta = 360f;
        public const float DistanciaSalidaPatrulla = 80f;
        private const int IntentosDeSalidaDron = 8;
        private const float SeparacionDeSalida = 20f;
        private const float AlcanceCamara = 400f;

        [SerializeField] private ConfigPersecucion config;
        [SerializeField] private DronMundo prefabDron;
        [SerializeField] private PatrullaMundo prefabPatrulla;

        private readonly List<DronMundo> drones = new List<DronMundo>();
        private readonly List<PatrullaMundo> patrullas = new List<PatrullaMundo>();
        private readonly List<Component> retirandose = new List<Component>();
        private bool huboContacto;
        private float valorAnterior;
        private CalculoPersecucion calculo;
        private GrafoCalles calles;
        private ICalor calor;
        private System.Random azar;

        public int Drones => drones.Count;
        public int Patrullas => patrullas.Count;

        /// <summary>Cuántos perseguidores se están yendo (T-587): ya no cuentan en <see cref="Drones"/> ni <see cref="Patrullas"/>.</summary>
        public int Retirandose => retirandose.Count;

        /// <summary>Segundos que lleva la policía sin verte desde la última vez que te vio (T-587).</summary>
        public float SegundosSinVer { get; private set; }

        /// <summary>True si ya te dieron por perdido (el calor baja más rápido).</summary>
        public bool Perdido => config != null && ReglaVistaPolicia.Perdido(SegundosSinVer, config.SegundosParaPerder) && huboContacto;

        /// <summary>Lo usan el creador de editor y las pruebas, antes de que el gestor se active.</summary>
        public void Configurar(ConfigPersecucion numeros, DronMundo dron, PatrullaMundo patrulla)
        {
            config = numeros;
            prefabDron = dron;
            prefabPatrulla = patrulla;
        }

        private void Awake()
        {
            if (config == null || prefabDron == null || prefabPatrulla == null)
            {
                Debug.LogError("[Trafico] GestorPersecucion necesita config y los prefabs de dron y patrulla.", this);
                enabled = false;
                return;
            }
            calculo = config.Crear();
            azar = new System.Random(341);
        }

        private void OnEnable()
        {
            Registro<ICalor>.Agregado += Enlazar;
            Registro<ICalor>.Quitado += Desenlazar;
            EventosLugares.JugadorEntro += AlEntrarALugar;
            if (Registro<ICalor>.Actual != null)
            {
                Enlazar(Registro<ICalor>.Actual);
            }
        }

        private void OnDisable()
        {
            Registro<ICalor>.Agregado -= Enlazar;
            Registro<ICalor>.Quitado -= Desenlazar;
            EventosLugares.JugadorEntro -= AlEntrarALugar;
            Desenlazar(calor);
            Reconciliar(0);
            // Apagado el gestor ya no hay quien vea irse a los que se retiraban: se quitan de golpe.
            foreach (Component c in retirandose)
            {
                if (c != null)
                {
                    Destroy(c.gameObject);
                }
            }
            retirandose.Clear();
        }

        // T-587: si ninguna patrulla te ve durante un rato (y antes alguna te vio), te dan por perdido y el calor baja más rápido.
        private void Update()
        {
            retirandose.RemoveAll(c => c == null);
            if (calor == null || calor.Nivel <= 0 || config == null)
            {
                SegundosSinVer = 0f;
                huboContacto = false;
                valorAnterior = calor != null ? calor.Valor : 0f;
                return;
            }
            bool ve = false;
            foreach (PatrullaMundo p in patrullas)
            {
                ve |= p != null && p.VeAlJugador;
            }
            if (ve)
            {
                huboContacto = true;
                SegundosSinVer = 0f;
            }
            else if (huboContacto)
            {
                SegundosSinVer += Time.deltaTime;
            }
            if (calor.Valor > valorAnterior + 0.01f)
            {
                SegundosSinVer = 0f; // un delito nuevo: la policía vuelve a estar encima
            }
            float extra = ReglaVistaPolicia.Enfriamiento(SegundosSinVer, config.SegundosParaPerder, config.EnfriamientoSinVista, Time.deltaTime);
            if (extra > 0f)
            {
                calor.Enfriar(extra);
            }
            valorAnterior = calor.Valor;
        }

        private void Enlazar(ICalor nuevo)
        {
            Desenlazar(calor);
            calor = nuevo;
            calor.NivelCambio += Reconciliar;
            Reconciliar(calor.Nivel);
        }

        private void Desenlazar(ICalor viejo)
        {
            if (viejo != null && ReferenceEquals(viejo, calor))
            {
                calor.NivelCambio -= Reconciliar;
                calor = null;
                Reconciliar(0);
            }
        }

        private void AlEntrarALugar(IdLugar lugar)
        {
            if (lugar == IdLugar.Casa)
            {
                Registro<ICalor>.Actual?.Borrar();
            }
        }

        // Deja exactamente las cantidades que pide el nivel: crea las que faltan y retira las que sobran.
        private void Reconciliar(int nivel)
        {
            int dronesPedidos = calculo == null ? 0 : calculo.Drones(nivel);
            int patrullasPedidas = calculo == null ? 0 : calculo.Patrullas(nivel);
            while (drones.Count < dronesPedidos)
            {
                CrearDron();
            }
            Retirar(drones, dronesPedidos);
            while (patrullas.Count < patrullasPedidas)
            {
                CrearPatrulla();
            }
            Retirar(patrullas, patrullasPedidas);
        }

        // T-587: los que sobran no desaparecen a la vista: se van por la calle (o por el aire) y se borran solos al salir de la vista.
        private void Retirar<T>(List<T> lista, int cantidad) where T : Component
        {
            while (lista.Count > cantidad)
            {
                int ultimo = lista.Count - 1;
                T quien = lista[ultimo];
                lista.RemoveAt(ultimo);
                if (quien is PatrullaMundo patrulla)
                {
                    patrulla.Retirar();
                }
                else if (quien is DronMundo dron)
                {
                    dron.Retirar();
                }
                retirandose.Add(quien);
            }
        }

        private void CrearDron()
        {
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            Camera camara = Camera.main;
            Vector3 lejos = centro;
            for (int intento = 0; intento < IntentosDeSalidaDron; intento++)
            {
                float angulo = (float)azar.NextDouble() * GradosPorVuelta * Mathf.Deg2Rad;
                lejos = centro + new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * config.DistanciaAparicionDron + Vector3.up * config.AlturaDron;
                if (!ReglaVistaPolicia.LoVe(camara, lejos, AlcanceCamara))
                {
                    break; // T-587: sale fuera de la vista
                }
            }
            DronMundo dron = Instantiate(prefabDron, lejos, Quaternion.identity, transform);
            dron.Preparar(calculo, config, PosicionDelJugador);
            drones.Add(dron);
        }

        private void CrearPatrulla()
        {
            if (calles == null)
            {
                calles = ArmarCalles();
            }
            if (calles == null)
            {
                return; // T-598: sin manzanas (escena de prueba sin barrio) no hay calles por donde salga una patrulla; los drones sí salen
            }
            Vector3 centro = PosicionDelJugador() ?? transform.position;
            PatrullaMundo patrulla = Instantiate(prefabPatrulla, transform);
            patrulla.Preparar(calculo, config, calles, NodoDeSalida(centro), PosicionDelJugador);
            patrulla.Intercepta = patrullas.Count % 2 == 1; // T-587: cada segunda patrulla corta el paso
            patrullas.Add(patrulla);
        }

        /// <summary>Arma el grafo de calles con las manzanas (hijos "Manzana_") de un barrio (T-451: el tráfico de calle lo comparte).</summary>
        /// <summary>T-533: la patrulla sale en el nodo más cercano a <see cref="DistanciaSalidaPatrulla"/> m del jugador (no al otro lado del barrio).</summary>
        private int NodoDeSalida(Vector3 centro)
        {
            // T-587: de preferencia un nodo a más de DistanciaMinimaSalida m y fuera de la vista de la cámara; si no hay, el más cercano a la distancia de salida.
            Camera camara = Camera.main;
            int mejor = calles.MasLejano(centro);
            float error = float.MaxValue;
            for (int pasada = 0; pasada < 2 && error == float.MaxValue; pasada++)
            {
                for (int i = 0; i < calles.Cantidad; i++)
                {
                    Vector3 d = calles.Nodo(i) - centro;
                    d.y = 0f;
                    if (pasada == 0 && (d.magnitude < config.DistanciaMinimaSalida || ReglaVistaPolicia.LoVe(camara, calles.Nodo(i), AlcanceCamara)))
                    {
                        continue;
                    }
                    if (pasada == 0 && HayPatrullaCerca(calles.Nodo(i)))
                    {
                        continue; // no salen dos patrullas encimadas
                    }
                    float e = Mathf.Abs(d.magnitude - DistanciaSalidaPatrulla);
                    if (e < error)
                    {
                        error = e;
                        mejor = i;
                    }
                }
            }
            return mejor;
        }

        private bool HayPatrullaCerca(Vector3 punto)
        {
            foreach (PatrullaMundo p in patrullas)
            {
                Vector3 d = p.transform.position - punto;
                d.y = 0f;
                if (d.magnitude < SeparacionDeSalida)
                {
                    return true;
                }
            }
            return false;
        }

        public static GrafoCalles ArmarCallesDe(Transform barrio)
        {
            List<Vector2> centros = new List<Vector2>();
            foreach (Transform hijo in barrio)
            {
                if (hijo.name.StartsWith(PrefijoManzana))
                {
                    centros.Add(new Vector2(hijo.position.x, hijo.position.z));
                }
            }
            return centros.Count > 0 ? GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles) : null;
        }

        private GrafoCalles ArmarCalles()
        {
            List<Vector2> centros = new List<Vector2>();
            foreach (Transform hijo in transform.parent != null ? transform.parent : transform)
            {
                if (hijo.name.StartsWith(PrefijoManzana))
                {
                    centros.Add(new Vector2(hijo.position.x, hijo.position.z));
                }
            }
            if (centros.Count == 0)
            {
                return null;
            }
            GrafoCalles grafo = GrafoCalles.DeManzanas(centros, LadoManzana, AlturaCalles);
            TraficoCalle.AlargarHaciaLasAfueras(grafo); // T-587: las patrullas llegan y se van por las calles de las afueras, lejos de la vista
            return grafo;
        }

        private static Vector3? PosicionDelJugador()
        {
            IJugador jugador = Registro<IJugador>.Actual;
            return jugador != null && jugador.Raiz != null ? jugador.Raiz.position : (Vector3?)null;
        }
    }
}

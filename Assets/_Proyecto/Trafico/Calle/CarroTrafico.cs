using System;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using MechaCyberpunk.Trafico.Persecucion;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Calle
{
    /// <summary>
    /// Un carro del tráfico de la calle (T-451): anda por el grafo de calles del barrio por su carril derecho, elige al azar la calle que sigue en
    /// cada esquina, frena si hay otro carro o el jugador adelante, pita cuando lleva rato bloqueado y se detiene un rato (y pita) si oye un
    /// disparo (<see cref="EventosRuido"/>). Se mueve con el transform (cuerpo cinemático): el carro del jugador rebota contra él y se daña.
    /// </summary>
    public class CarroTrafico : MonoBehaviour
    {
        private const float AnchoCarril = 2.2f;
        private const float LargoCarro = 4.5f;
        private const float VelocidadCambioCarril = 6f;
        private const float VelocidadMinimaPasaCerca = 4f;
        private const float SegundosEntrePasaCerca = 4f;
        private const float AlturaDelCentro = 0.45f;

        /// <summary>Los carros de tráfico que andan ahora (para que se vean entre sí).</summary>
        public static readonly List<CarroTrafico> Activos = new List<CarroTrafico>();

        private ConfigTraficoCalle config;
        private GrafoCalles calles;
        private CalculoTraficoCalle calculo;
        private System.Random azar;
        private Func<Vector3?> posicionJugador;
        private int nodoAnterior = -1;
        private int nodoDestino;
        private Vector3 eje;
        private Vector3 desvio;
        private Vector3 direccion = Vector3.forward;
        private float proximoPasaCerca;
        private int pitidos;
        private Renderer carroceria;

        private static readonly Color[] ColoresCarroceria =
        {
            new Color(0.75f, 0.1f, 0.1f), new Color(0.1f, 0.35f, 0.75f), new Color(0.9f, 0.75f, 0.1f), new Color(0.15f, 0.55f, 0.35f),
            new Color(0.85f, 0.85f, 0.88f), new Color(0.3f, 0.3f, 0.35f), new Color(0.75f, 0.35f, 0.7f),
        };

        public float Velocidad => calculo != null ? calculo.Velocidad : 0f;
        public bool EnPanico => calculo != null && calculo.EnPanico;
        public bool Bloqueado => calculo != null && calculo.Bloqueado;
        /// <summary>Cuántas veces ha pitado.</summary>
        public int Pitidos => pitidos;
        public int NodoDestino => nodoDestino;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar() => Activos.Clear();

        /// <summary>Lo prepara el administrador: números, calles, nodo de salida, azar y cómo saber dónde está el jugador.</summary>
        public void Preparar(ConfigTraficoCalle numeros, GrafoCalles grafo, int nodoInicial, System.Random azarCarro, Func<Vector3?> dondeEstaElJugador)
        {
            config = numeros;
            calles = grafo;
            azar = azarCarro;
            posicionJugador = dondeEstaElJugador;
            calculo = numeros.Crear();
            eje = grafo.Nodo(nodoInicial);
            nodoAnterior = nodoInicial;
            nodoDestino = ElegirSiguiente(nodoInicial, -1);
            direccion = DireccionA(nodoDestino);
            desvio = Derecha(direccion) * config.SeparacionCarril;
            transform.SetPositionAndRotation(Posicion(), Quaternion.LookRotation(direccion, Vector3.up));
            PintarCarroceria();
        }

        // Cada carro sale de un color al azar (la carrocería es el hijo "Carroceria").
        private void PintarCarroceria()
        {
            if (carroceria == null)
            {
                Transform hijo = transform.Find("Carroceria");
                carroceria = hijo != null ? hijo.GetComponent<Renderer>() : null;
            }
            if (carroceria == null)
            {
                return;
            }
            Color color = ColoresCarroceria[azar.Next(ColoresCarroceria.Length)];
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            carroceria.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", color);
            bloque.SetColor("_Color", color);
            carroceria.SetPropertyBlock(bloque);
            Transform cabina = transform.Find("Cabina");
            Renderer rc = cabina != null ? cabina.GetComponent<Renderer>() : null;
            if (rc != null)
            {
                MaterialPropertyBlock bloqueCabina = new MaterialPropertyBlock();
                Color oscuro = color * 0.8f;
                oscuro.a = 1f;
                bloqueCabina.SetColor("_BaseColor", oscuro);
                bloqueCabina.SetColor("_Color", oscuro);
                rc.SetPropertyBlock(bloqueCabina);
            }
        }

        private void OnEnable()
        {
            Activos.Add(this);
            EventosRuido.Disparo += AlOirDisparo;
        }

        private void OnDisable()
        {
            Activos.Remove(this);
            EventosRuido.Disparo -= AlOirDisparo;
        }

        /// <summary>Un disparo: si está cerca el carro se asusta (se detiene y pita).</summary>
        public void AlOirDisparo(Vector3 posicion)
        {
            if (calculo == null || config == null || !isActiveAndEnabled)
            {
                return;
            }
            Vector3 d = posicion - transform.position;
            d.y = 0f;
            if (d.magnitude <= config.RadioSusto)
            {
                calculo.Asustar(config.SegundosSusto);
            }
        }

        private void Update()
        {
            if (calculo == null)
            {
                return;
            }
            float dt = Time.deltaTime;
            Vector3? jugador = posicionJugador?.Invoke();
            float velocidad = calculo.Avanzar(dt, DistanciaAlObstaculo(jugador));
            Mover(velocidad, dt);
            if (calculo.TomarPitido())
            {
                pitidos++;
                Sonidos.ReproducirEn("VEH_CLAXON_TRAFICO", transform.position);
            }
            if (jugador.HasValue && velocidad >= VelocidadMinimaPasaCerca && Time.time >= proximoPasaCerca
                && Distancia(jugador.Value, transform.position) <= config.DistanciaPasaCerca)
            {
                proximoPasaCerca = Time.time + SegundosEntrePasaCerca;
                Sonidos.ReproducirEn("VEH_PASA_CERCA", transform.position);
            }
        }

        private void Mover(float velocidad, float dt)
        {
            Vector3 haciaNodo = calles.Nodo(nodoDestino) - eje;
            haciaNodo.y = 0f;
            if (haciaNodo.magnitude <= config.MargenNodo)
            {
                int llegado = nodoDestino;
                nodoDestino = ElegirSiguiente(llegado, nodoAnterior);
                nodoAnterior = llegado;
                haciaNodo = calles.Nodo(nodoDestino) - eje;
                haciaNodo.y = 0f;
            }
            if (haciaNodo.sqrMagnitude > 0.0001f)
            {
                direccion = haciaNodo.normalized;
            }
            eje += direccion * (velocidad * dt);
            desvio = Vector3.MoveTowards(desvio, Derecha(direccion) * config.SeparacionCarril, VelocidadCambioCarril * dt);
            Quaternion meta = Quaternion.LookRotation(direccion, Vector3.up);
            transform.SetPositionAndRotation(Posicion(), Quaternion.RotateTowards(transform.rotation, meta, config.GiroMaximo * dt));
        }

        private Vector3 Posicion() => new Vector3(eje.x + desvio.x, AlturaDelCentro, eje.z + desvio.z);

        private Vector3 DireccionA(int nodo)
        {
            Vector3 d = calles.Nodo(nodo) - eje;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }

        private static Vector3 Derecha(Vector3 adelante) => Vector3.Cross(Vector3.up, adelante);

        // La calle que sigue: al azar entre las que salen del nodo, sin regresar por donde vino salvo que no haya otra.
        private int ElegirSiguiente(int nodo, int deDonde)
        {
            IReadOnlyList<int> vecinos = calles.Vecinos(nodo);
            List<int> opciones = new List<int>();
            foreach (int v in vecinos)
            {
                if (v != deDonde)
                {
                    opciones.Add(v);
                }
            }
            if (opciones.Count == 0)
            {
                return vecinos.Count > 0 ? vecinos[0] : nodo;
            }
            return opciones[azar.Next(opciones.Count)];
        }

        // Qué tan lejos está lo que le estorba adelante (otro carro de tráfico o el jugador, en su carril); float.MaxValue si nada.
        private float DistanciaAlObstaculo(Vector3? jugador)
        {
            float menor = float.MaxValue;
            Vector3 adelante = transform.forward;
            Vector3 derecha = transform.right;
            for (int i = 0; i < Activos.Count; i++)
            {
                CarroTrafico otro = Activos[i];
                if (otro != this)
                {
                    menor = Mathf.Min(menor, DistanciaDelante(otro.transform.position, adelante, derecha));
                }
            }
            if (jugador.HasValue)
            {
                menor = Mathf.Min(menor, DistanciaDelante(jugador.Value, adelante, derecha));
            }
            return menor;
        }

        private float DistanciaDelante(Vector3 punto, Vector3 adelante, Vector3 derecha)
        {
            Vector3 hacia = punto - transform.position;
            hacia.y = 0f;
            float delante = Vector3.Dot(hacia, adelante);
            if (delante <= 0f || delante > config.DistanciaFrenado + LargoCarro || Mathf.Abs(Vector3.Dot(hacia, derecha)) > AnchoCarril)
            {
                return float.MaxValue;
            }
            return Mathf.Max(0f, delante - LargoCarro);
        }

        private static float Distancia(Vector3 a, Vector3 b)
        {
            a.y = b.y;
            return Vector3.Distance(a, b);
        }
    }
}

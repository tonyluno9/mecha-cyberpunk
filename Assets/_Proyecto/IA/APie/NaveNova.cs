using System;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1498: una nave de Nova (NV01 o NV03): llega desde lejos, se queda flotando a pocos metros del piso, suelta soldados uno por uno (avisa con
    /// <see cref="Soltar"/>; quien la creó los baja por la cuerda) y cuando el último tocó el piso se va y se destruye. Solo vuela y avisa: no dispara.
    /// </summary>
    public class NaveNova : MonoBehaviour
    {
        public enum Fase { Llegando, Descargando, Yendose }

        private const float DistanciaLlegada = 0.3f;
        private const float AjusteFrenado = 1.5f;
        private const float AceleracionSalida = 12f;
        private const float SegundosMaximosSalida = 20f;
        private const float DistanciaSalidaCorta = 3f;
        private const float SegundosDeGraciaAlIrse = 1.5f;
        private const float AmplitudFlote = 0.25f;
        private const float FrecuenciaFlote = 0.6f;
        private const float GiroMaximo = 90f; // °/s

        private Vector3 flote;
        private Vector3 salida;
        private float velocidad;
        private float segundosEntreSoldados;
        private int total;
        private float segundosDescenso;
        private float tiempoFase;
        private float proximaSuelta;
        private float velocidadSalida;
        private Vector3 baseFlote;

        /// <summary>Se avisa por cada soldado que hay que bajar: (nave, número de soldado empezando en 0).</summary>
        public event Action<NaveNova, int> Soltar;

        public Fase Actual { get; private set; } = Fase.Llegando;
        public int Soltados { get; private set; }
        public Vector3 PuntoFlote => flote;

        /// <summary>El punto de donde cuelga la cuerda, local a la nave (debajo del casco).</summary>
        public Vector3 AnclaLocal => new Vector3(0f, -1f, 0f);

        /// <summary>Crea la nave con su modelo, en <paramref name="desde"/>, rumbo a <paramref name="flote"/>.</summary>
        public static NaveNova Crear(GameObject modelo, Vector3 desde, Vector3 flote, Vector3 salida, float velocidad, int soldados, float segundosEntreSoldados, float segundosDescenso)
        {
            GameObject objeto = Instantiate(modelo, desde, Quaternion.identity);
            objeto.name = "Nova_Nave";
            NaveNova nave = objeto.AddComponent<NaveNova>();
            nave.flote = flote;
            nave.baseFlote = flote;
            nave.salida = salida;
            nave.velocidad = velocidad;
            nave.total = soldados;
            nave.segundosEntreSoldados = segundosEntreSoldados;
            nave.segundosDescenso = segundosDescenso;
            Vector3 rumbo = flote - desde;
            rumbo.y = 0f;
            if (rumbo.sqrMagnitude > 0.01f)
            {
                objeto.transform.rotation = Quaternion.LookRotation(rumbo);
            }
            return nave;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            tiempoFase += dt;
            switch (Actual)
            {
                case Fase.Llegando:
                    Llegar(dt);
                    break;
                case Fase.Descargando:
                    Descargar();
                    break;
                default:
                    Irse(dt);
                    break;
            }
        }

        private void Llegar(float dt)
        {
            Vector3 hacia = flote - transform.position;
            float distancia = hacia.magnitude;
            if (distancia <= DistanciaLlegada)
            {
                transform.position = flote;
                Cambiar(Fase.Descargando);
                proximaSuelta = 0f;
                return;
            }
            float rapidez = Mathf.Min(velocidad, distancia * AjusteFrenado + 1f);
            transform.position += hacia / distancia * Mathf.Min(rapidez * dt, distancia);
            Mirar(hacia, dt);
        }

        private void Descargar()
        {
            transform.position = baseFlote + Vector3.up * (Mathf.Sin(tiempoFase * FrecuenciaFlote * Mathf.PI * 2f) * AmplitudFlote);
            if (Soltados < total && tiempoFase >= proximaSuelta)
            {
                proximaSuelta = tiempoFase + segundosEntreSoldados;
                int numero = Soltados;
                Soltados++;
                Soltar?.Invoke(this, numero);
            }
            if (Soltados >= total && tiempoFase >= proximaSuelta - segundosEntreSoldados + segundosDescenso + SegundosDeGraciaAlIrse)
            {
                Cambiar(Fase.Yendose);
            }
        }

        private void Irse(float dt)
        {
            velocidadSalida = Mathf.Min(velocidad, velocidadSalida + AceleracionSalida * dt);
            Vector3 hacia = salida - transform.position;
            float distancia = hacia.magnitude;
            if (distancia <= DistanciaSalidaCorta || tiempoFase >= SegundosMaximosSalida)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += hacia / distancia * Mathf.Min(velocidadSalida * dt, distancia);
            Mirar(hacia, dt);
        }

        private void Cambiar(Fase fase)
        {
            Actual = fase;
            tiempoFase = 0f;
        }

        private void Mirar(Vector3 hacia, float dt)
        {
            hacia.y = 0f;
            if (hacia.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(hacia), GiroMaximo * dt);
            }
        }
    }
}

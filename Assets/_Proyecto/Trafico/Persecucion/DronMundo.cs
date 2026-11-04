using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Trafico.Datos;
using UnityEngine;

namespace MechaCyberpunk.Trafico.Persecucion
{
    /// <summary>
    /// Un dron de la policía (T-341): vuela sobre el jugador y se le acerca hasta la distancia de seguimiento, con su luz apuntando hacia abajo.
    /// Sin colliders: no estorba ni empuja nada. El daño y la captura quedan fuera de la tarea.
    /// </summary>
    public class DronMundo : MonoBehaviour
    {
        private const float GradosPorSegundoGiro = 180f;

        /// <summary>Fuente del azar (0 a 1) de los disparos. Las pruebas la cambian.</summary>
        public static Func<float> Azar = () => UnityEngine.Random.value;

        private const float AlturaPecho = 1.2f;

        private Light[] luces;
        private Color[] coloresLuces;
        private float proximoTiro;
        private float finAviso = -1f;
        private CalculoPersecucion calculo;
        private ConfigPersecucion config;
        private Func<Vector3?> posicionJugador;

        public void Preparar(CalculoPersecucion reglas, ConfigPersecucion numeros, Func<Vector3?> dondeEstaElJugador)
        {
            calculo = reglas;
            config = numeros;
            posicionJugador = dondeEstaElJugador;
        }

        private void Awake()
        {
            luces = GetComponentsInChildren<Light>(true);
            coloresLuces = new Color[luces.Length];
            for (int i = 0; i < luces.Length; i++)
            {
                coloresLuces[i] = luces[i].color;
            }
        }

        // Con calor suficiente avisa 0.3 s con su luz en rojo y dispara al jugador a pie; a quien va en carro no le dispara.
        private void Disparar()
        {
            ICalor calor = Registro<ICalor>.Actual;
            IJugador jugador = Registro<IJugador>.Actual;
            bool puede = calor != null && calor.Nivel >= config.NivelDisparoDron && jugador != null && jugador.Raiz != null
                && jugador.Modo == ModoJugador.APie;
            IRecibeDanio objetivo = puede ? jugador.Raiz.GetComponentInChildren<IRecibeDanio>() : null;
            IVida vida = puede ? jugador.Raiz.GetComponentInChildren<IVida>() : null;
            if (objetivo == null || (vida != null && vida.EstaKO))
            {
                finAviso = -1f;
                PonerLuces(false);
                return;
            }
            if (finAviso < 0f)
            {
                if (Time.time >= proximoTiro)
                {
                    finAviso = Time.time + config.AvisoDron;
                    PonerLuces(true);
                }
                return;
            }
            if (Time.time < finAviso)
            {
                return;
            }
            finAviso = -1f;
            PonerLuces(false);
            proximoTiro = Time.time + config.SegundosEntreTirosDron;
            if (Azar() <= config.AciertoDron)
            {
                Vector3 pecho = jugador.Raiz.position + Vector3.up * AlturaPecho;
                Vector3 direccion = (pecho - transform.position).normalized;
                objetivo.RecibirDanio(new InfoDanio(config.DanioDron, TipoDanio.Disparo, pecho, direccion, null));
            }
        }

        private void PonerLuces(bool rojo)
        {
            for (int i = 0; i < luces.Length; i++)
            {
                luces[i].color = rojo ? Color.red : coloresLuces[i];
            }
        }

        /// <summary>True si ya se va: sube y se aleja del jugador hasta salir de la vista (T-587).</summary>
        public bool Retirando { get; private set; }

        /// <summary>T-587: deja de perseguir; desaparece cuando ya no se ve y está lejos (o tras <see cref="ConfigPersecucion.SegundosMaximosRetiro"/> s).</summary>
        public void Retirar()
        {
            Retirando = true;
            segundosRetirandose = 0f;
            finAviso = -1f;
            PonerLuces(false);
        }

        private float segundosRetirandose;

        private void Retirada(Vector3? jugador, float dt)
        {
            segundosRetirandose += dt;
            Vector3 lejos = jugador.HasValue ? transform.position - jugador.Value : transform.forward;
            lejos.y = 0f;
            Vector3 direccion = (lejos.sqrMagnitude > 0.001f ? lejos.normalized : transform.forward) + Vector3.up * 0.25f;
            transform.position += direccion.normalized * (calculo.Paso(true, float.MaxValue, dt));
            bool distante = !jugador.HasValue || Vector3.Distance(transform.position, jugador.Value) >= config.DistanciaRetiro;
            if ((distante && !ReglaVistaPolicia.LoVe(Camera.main, transform.position, RetiroAlcanceCamara)) || segundosRetirandose >= config.SegundosMaximosRetiro)
            {
                Destroy(gameObject);
            }
        }

        private const float RetiroAlcanceCamara = 400f;

        private void Update()
        {
            Vector3? jugador = posicionJugador?.Invoke();
            if (calculo == null)
            {
                return;
            }
            if (Retirando)
            {
                Retirada(jugador, Time.deltaTime);
                return;
            }
            if (!jugador.HasValue)
            {
                return;
            }
            Vector3 meta = jugador.Value + Vector3.up * config.AlturaDron;
            Vector3 haciaMeta = meta - transform.position;
            float paso = calculo.Paso(true, haciaMeta.magnitude, Time.deltaTime);
            if (paso > 0f)
            {
                transform.position += haciaMeta.normalized * paso;
            }
            Disparar();
            Vector3 haciaJugador = jugador.Value - transform.position;
            haciaJugador.y = 0f;
            if (haciaJugador.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(haciaJugador, Vector3.up), GradosPorSegundoGiro * Time.deltaTime);
            }
        }
    }
}

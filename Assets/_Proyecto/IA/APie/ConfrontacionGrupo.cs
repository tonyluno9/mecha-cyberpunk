using System.Collections.Generic;
using System.Text;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1659 (T41, sobre T-1651): la confrontación de un grupo de maleantes de calle (SKULLS o SNAKES). Mide la distancia al jugador, mueve
    /// <see cref="ReglaConfrontacion"/> y reparte los papeles: todos miran al jugador; solo el <see cref="Acercador"/> se acerca caminando lento y empuja.
    /// Ya no hablan al confrontar. Lo crea y lo avanza <see cref="PandillaAPie"/>.
    /// </summary>
    public sealed class ConfrontacionGrupo
    {
        private static readonly List<ConfrontacionGrupo> todos = new List<ConfrontacionGrupo>();
        private const float DistanciaExtraContacto = 0.3f;

        private readonly PandillaAPie grupo;
        private readonly ConfigConfrontacion config;
        private readonly ReglaConfrontacion regla;
        private float proximoEmpujon;
        private float ultimaDistancia = float.MaxValue;

        public ConfigConfrontacion Config => config;
        public ReglaConfrontacion.Fase Fase => regla.Actual;
        public float Distancia => ultimaDistancia;
        public float SegundosEnFase => regla.SegundosEnFase;
        public float SegundosCerca => regla.SegundosCerca;
        public int Empujones => regla.Empujones;

        /// <summary>El miembro más cercano al jugador ahora mismo.</summary>
        public PandilleroAPie Orador { get; private set; }

        /// <summary>El único que se acerca y empuja (Acercamiento y Empujon); null en las demás fases.</summary>
        public PandilleroAPie Acercador { get; private set; }

        /// <summary>True en Mirando, Acercamiento y Empujon: los pandilleros te miran en vez de volver a su lugar.</summary>
        public bool EnConfrontacion => regla.Actual == ReglaConfrontacion.Fase.Mirando || regla.Actual == ReglaConfrontacion.Fase.Acercamiento
            || regla.Actual == ReglaConfrontacion.Fase.Empujon;

        public ConfrontacionGrupo(PandillaAPie grupo, ConfigConfrontacion config)
        {
            this.grupo = grupo;
            this.config = config;
            regla = new ReglaConfrontacion(new ReglaConfrontacion.Parametros(config.DistanciaDeteccion, config.SegundosCerca, config.DistanciaCerca,
                config.DistanciaDejarDeSeguir, config.SegundosRetirada, config.EmpujonesMaximos, config.DistanciaAcercamiento + DistanciaExtraContacto));
            todos.Add(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarRegistro() => todos.Clear();

        /// <summary>Quita este grupo de la lista de depuración (lo llama <see cref="PandillaAPie"/> al destruirse).</summary>
        public void Olvidar() => todos.Remove(this);

        public void Avanzar(float dt, PandilleroAPie[] miembros)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            bool valido = jugador != null && jugador.Raiz != null && jugador.Modo == ModoJugador.APie;
            float menor = float.MaxValue;
            bool alerta = false;
            bool hostil = false;
            Orador = null;
            foreach (PandilleroAPie p in miembros)
            {
                if (p == null || !p.gameObject.activeSelf || p.EstaKO || p.Acosando)
                {
                    continue;
                }
                alerta |= p.Alerta;
                hostil |= p.EsHostil;
                if (valido)
                {
                    Vector3 d = p.transform.position - jugador.Raiz.position;
                    d.y = 0f;
                    float m = d.magnitude;
                    if (m < menor)
                    {
                        menor = m;
                        Orador = p;
                    }
                }
            }
            ultimaDistancia = valido ? menor : float.MaxValue;
            ReglaConfrontacion.Fase antes = regla.Actual;
            regla.Avanzar(dt, new ReglaConfrontacion.Entrada { Distancia = ultimaDistancia, Alerta = valido && alerta, Hostil = hostil });
            if (regla.Actual != antes)
            {
                AlCambiarFase();
            }
            ElegirAcercador();
        }

        // El que se acerca es el más cercano cuando empieza el acercamiento y se mantiene; si cae o se apaga, toma su lugar el más cercano.
        private void ElegirAcercador()
        {
            bool acercando = regla.Actual == ReglaConfrontacion.Fase.Acercamiento || regla.Actual == ReglaConfrontacion.Fase.Empujon;
            if (!acercando)
            {
                Acercador = null;
            }
            else if (Acercador == null || !Acercador.gameObject.activeSelf || Acercador.EstaKO || Acercador.Acosando)
            {
                Acercador = Orador;
            }
        }

        private void AlCambiarFase()
        {
            if (regla.Actual == ReglaConfrontacion.Fase.Empujon)
            {
                proximoEmpujon = Time.time + 1.5f;
            }
            Debug.Log("[Confrontacion] " + grupo.name + " " + regla.Actual);
        }

        /// <summary>T-1659: los maleantes de calle ya no hablan al confrontar; siempre false (queda por compatibilidad).</summary>
        public bool TomarFrase(PandilleroAPie quien, out string idVoz, out string texto)
        {
            idVoz = null;
            texto = null;
            return false;
        }

        /// <summary>Si a <paramref name="quien"/> le toca empujar (fase Empujon, es el que se acerca, pasó el intervalo). <paramref name="tira"/> true cada N empujones: además de empujar, te tira.</summary>
        public bool TomarEmpujon(PandilleroAPie quien, out bool tira)
        {
            tira = false;
            if (regla.Actual != ReglaConfrontacion.Fase.Empujon || quien != Acercador || Time.time < proximoEmpujon)
            {
                return false;
            }
            proximoEmpujon = Time.time + config.SegundosEntreEmpujones;
            regla.RegistrarEmpujon();
            tira = regla.Empujones % config.EmpujonesParaTirar == 0;
            return true;
        }

        /// <summary>Publica el subtítulo de una frase del grupo.</summary>
        public void PublicarSubtitulo(Transform hablante, string texto)
        {
            if (!VocesPandilla.VocesSkullsActivas && grupo.Pandilla != IdPandilla.Viboras)
            {
                return; // T-1655: sin subtítulos de SKULLS
            }
            Subtitulos.Publicar(config.NombreHablante, texto, hablante.position + Vector3.up * 1.6f, 0f, config.ColorHablante);
        }

        public override string ToString()
        {
            return grupo.name + " fase=" + regla.Actual + " t=" + regla.SegundosEnFase.ToString("F1") + " cerca=" + regla.SegundosCerca.ToString("F1") + "/" + config.SegundosCerca.ToString("F0")
                + " dist=" + (ultimaDistancia >= float.MaxValue ? "-" : ultimaDistancia.ToString("F1")) + " empujones=" + regla.Empujones
                + " acercador=" + (Acercador != null ? Acercador.name : "-");
        }

        /// <summary>Depuración (Play, por reflexión): una línea por grupo con confrontación: fase, segundos cerca, distancia y quién es el que se acerca.</summary>
        public static string DepurarConfrontacion()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ConfrontacionGrupo g in todos)
            {
                if (g != null && g.grupo != null && g.grupo.Presentes > 0)
                {
                    sb.AppendLine(g.ToString() + " presentes=" + g.grupo.Presentes);
                }
            }
            return sb.Length == 0 ? "sin grupos" : sb.ToString().TrimEnd();
        }
    }
}

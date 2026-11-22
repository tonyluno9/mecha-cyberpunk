using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1651 (T26): la confrontación de un grupo de maleantes de calle (SKULLS o SNAKES). Mide la distancia al jugador, mueve <see cref="ReglaConfrontacion"/>
    /// y reparte los turnos: una sola frase a la vez (cada <see cref="ConfigConfrontacion.SegundosEntreFrases"/>) y un solo empujón a la vez, siempre del
    /// miembro más cercano (el orador). Lo crea y lo avanza <see cref="PandillaAPie"/>.
    /// </summary>
    public sealed class ConfrontacionGrupo
    {
        private readonly PandillaAPie grupo;
        private readonly ConfigConfrontacion config;
        private readonly ReglaConfrontacion regla;
        private float proximaFrase;
        private float proximoEmpujon;
        private int contadorFrases;
        private bool ultimoAvisoDado;
        private int ultimaFrase = -1;
        private float ultimaDistancia = float.MaxValue;

        public ConfigConfrontacion Config => config;
        public ReglaConfrontacion.Fase Fase => regla.Actual;
        public float Distancia => ultimaDistancia;
        public float SegundosEnFase => regla.SegundosEnFase;
        public int Empujones => regla.Empujones;

        /// <summary>El miembro más cercano al jugador: el único que habla y empuja.</summary>
        public PandilleroAPie Orador { get; private set; }

        /// <summary>True en Advertencia y Empujon: los pandilleros provocan en vez de volver a su lugar.</summary>
        public bool EnConfrontacion => regla.Actual == ReglaConfrontacion.Fase.Advertencia || regla.Actual == ReglaConfrontacion.Fase.Empujon;

        public ConfrontacionGrupo(PandillaAPie grupo, ConfigConfrontacion config)
        {
            this.grupo = grupo;
            this.config = config;
            regla = new ReglaConfrontacion(new ReglaConfrontacion.Parametros(config.DistanciaDeteccion, config.SegundosAdvertencia, config.DistanciaQuedarse,
                config.DistanciaDejarDeSeguir, config.SegundosRetirada, config.EmpujonesMaximos));
        }

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
        }

        private void AlCambiarFase()
        {
            float ahora = Time.time;
            if (regla.Actual == ReglaConfrontacion.Fase.Advertencia)
            {
                proximaFrase = ahora + 0.8f;
                contadorFrases = 0;
                ultimoAvisoDado = false;
            }
            else if (regla.Actual == ReglaConfrontacion.Fase.Empujon)
            {
                proximoEmpujon = ahora + 1.5f;
                proximaFrase = Mathf.Max(proximaFrase, ahora + 2f);
            }
            Debug.Log("[Confrontacion] " + grupo.name + " " + regla.Actual);
        }

        /// <summary>Si le toca hablar a <paramref name="quien"/> ahora (es el orador y pasó el intervalo), devuelve el ID de voz y el texto del subtítulo.</summary>
        public bool TomarFrase(PandilleroAPie quien, out string idVoz, out string texto)
        {
            idVoz = null;
            texto = null;
            if (!EnConfrontacion || quien != Orador || Time.time < proximaFrase)
            {
                return false;
            }
            proximaFrase = Time.time + config.SegundosEntreFrases;
            bool snakes = grupo.Pandilla == IdPandilla.Viboras;
            bool ultimo = regla.Actual == ReglaConfrontacion.Fase.Advertencia && !ultimoAvisoDado && regla.SegundosAdvertenciaRestantes <= config.SegundosEntreFrases;
            if (ultimo)
            {
                ultimoAvisoDado = true;
                idVoz = VocesPandilla.UltimoAviso;
                texto = FrasesConfrontacion.Elegir(snakes ? FrasesConfrontacion.UltimoAvisoSnakes : FrasesConfrontacion.UltimoAvisoSkulls, ref ultimaFrase);
            }
            else if (regla.Actual == ReglaConfrontacion.Fase.Empujon || contadorFrases % 2 == 1)
            {
                idVoz = VocesPandilla.Insulto;
                texto = FrasesConfrontacion.Elegir(snakes ? FrasesConfrontacion.InsultoSnakes : FrasesConfrontacion.InsultoSkulls, ref ultimaFrase);
            }
            else
            {
                idVoz = VocesPandilla.Provocar;
                texto = FrasesConfrontacion.Elegir(snakes ? FrasesConfrontacion.ProvocarSnakes : FrasesConfrontacion.ProvocarSkulls, ref ultimaFrase);
            }
            contadorFrases++;
            return true;
        }

        /// <summary>Si a <paramref name="quien"/> le toca empujar (fase Empujon, es el orador, pasó el intervalo). <paramref name="tira"/> true cada N empujones: además de empujar, te tira.</summary>
        public bool TomarEmpujon(PandilleroAPie quien, out bool tira)
        {
            tira = false;
            if (regla.Actual != ReglaConfrontacion.Fase.Empujon || quien != Orador || Time.time < proximoEmpujon)
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
            Subtitulos.Publicar(config.NombreHablante, texto, hablante.position + Vector3.up * 1.6f, 0f, config.ColorHablante);
        }

        public override string ToString()
        {
            return grupo.name + " fase=" + regla.Actual + " t=" + regla.SegundosEnFase.ToString("F1") + " dist=" + (ultimaDistancia >= float.MaxValue ? "-" : ultimaDistancia.ToString("F1"))
                + " empujones=" + regla.Empujones + " orador=" + (Orador != null ? Orador.name : "-");
        }
    }
}

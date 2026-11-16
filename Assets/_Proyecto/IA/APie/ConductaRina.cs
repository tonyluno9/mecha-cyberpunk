using System.Collections.Generic;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1107: conducta de un pandillero en la riña (SNAKES contra SKULLS). Mientras <see cref="Controla"/> busca al rival vivo más cercano de otra
    /// pandilla, se le acerca y le pega con su alcance y ritmo normales pero con el 35% del daño (<see cref="ReglaRina"/>), y de vez en cuando grita.
    /// Lo suelta y lo rearma <see cref="RinaPandillas"/>.
    /// </summary>
    public class ConductaRina : MonoBehaviour, IConductaAPie
    {
        private const float SegundosBusqueda = 0.4f;
        private const float GradosGiro = 540f;
        private const float AnguloMinimoGolpe = 35f;

        private enum Fase { Libre, Preparando, Recargando }

        private RinaPandillas rina;
        private bool controla = true;
        private PandilleroAPie rival;
        private Fase fase;
        private float restante;
        private float proximaBusqueda;
        private float proximoGrito;
        private readonly List<ReglaRina.Candidato> candidatos = new List<ReglaRina.Candidato>();

        /// <summary>True solo mientras se aplica el golpe de riña: la riña no confunde ese daño con uno del jugador.</summary>
        public static bool GolpeDeRinaEnCurso { get; private set; }

        public bool Controla => controla;
        public PandilleroAPie Rival => rival;

        public void Iniciar(RinaPandillas dueno)
        {
            rina = dueno;
            proximoGrito = ReglaRina.PrimerGrito(Time.time, Random.value);
        }

        /// <summary>La riña termina o el jugador se mete: este pandillero vuelve a su IA normal.</summary>
        public void Soltar()
        {
            controla = false;
            rival = null;
        }

        /// <summary>La riña empieza de nuevo.</summary>
        public void Retomar()
        {
            controla = true;
            rival = null;
            fase = Fase.Libre;
            restante = 0f;
            proximoGrito = ReglaRina.PrimerGrito(Time.time, Random.value);
        }

        public void Actualizar(PandilleroAPie p, float dt)
        {
            if (rina == null || p.Datos == null)
            {
                p.MoverPlano(Vector3.zero);
                return;
            }
            if (Time.time >= proximoGrito)
            {
                // Si otro de la riña gritó hace menos de 1.5 s, espera un poco (no se enciman).
                if (rina.PedirGrito())
                {
                    p.Gritar(ReglaRina.Grito(p.Pandilla, Random.value));
                    proximoGrito = ReglaRina.SiguienteGrito(Time.time, Random.value);
                }
                else
                {
                    proximoGrito = Time.time + ReglaRina.SeparacionGritos * (0.5f + Random.value);
                }
            }
            if (fase == Fase.Libre && (rival == null || !ReglaRina.RivalValido(Describir(rival), p.Pandilla) || Time.time >= proximaBusqueda))
            {
                proximaBusqueda = Time.time + SegundosBusqueda;
                rival = Elegir(p);
            }
            if (rival == null)
            {
                p.MoverPlano(Vector3.zero);
                return;
            }

            Vector3 hacia = rival.transform.position - p.transform.position;
            hacia.y = 0f;
            float distancia = hacia.magnitude;
            Vector3 dir = distancia > 0.0001f ? hacia / distancia : p.transform.forward;
            p.MirarHacia(dir, GradosGiro);
            float alcance = p.Datos.AlcanceGolpe;
            switch (fase)
            {
                case Fase.Libre:
                    if (distancia > alcance * 0.9f)
                    {
                        p.MoverPlano(dir * p.Datos.Velocidad);
                    }
                    else
                    {
                        p.MoverPlano(Vector3.zero);
                        if (Vector3.Angle(p.transform.forward, dir) <= AnguloMinimoGolpe)
                        {
                            fase = Fase.Preparando;
                            restante = p.Datos.PreparacionGolpe;
                            p.AnimarGolpe(false);
                        }
                    }
                    break;
                case Fase.Preparando:
                    p.MoverPlano(Vector3.zero);
                    restante -= dt;
                    if (restante <= 0f)
                    {
                        if (ReglaRina.RivalValido(Describir(rival), p.Pandilla) && distancia <= alcance + p.Datos.MargenImpacto)
                        {
                            Golpear(p, rival, dir);
                        }
                        fase = Fase.Recargando;
                        restante = p.Datos.RecargaGolpe;
                    }
                    break;
                default:
                    p.MoverPlano(Vector3.zero);
                    restante -= dt;
                    if (restante <= 0f)
                    {
                        fase = Fase.Libre;
                    }
                    break;
            }
        }

        private static void Golpear(PandilleroAPie atacante, PandilleroAPie objetivo, Vector3 dir)
        {
            GolpeDeRinaEnCurso = true;
            try
            {
                atacante.DaniarA(objetivo, ReglaRina.Danio(atacante.Datos.DanioGolpe), dir);
            }
            finally
            {
                GolpeDeRinaEnCurso = false;
            }
        }

        private PandilleroAPie Elegir(PandilleroAPie p)
        {
            IReadOnlyList<PandilleroAPie> miembros = rina.Miembros;
            candidatos.Clear();
            for (int i = 0; i < miembros.Count; i++)
            {
                candidatos.Add(Describir(miembros[i]));
            }
            int indice = ReglaRina.ElegirRival(p.transform.position, p.Pandilla, candidatos);
            return indice >= 0 ? miembros[indice] : null;
        }

        /// <summary>Lo que la regla necesita saber de un pandillero.</summary>
        public static ReglaRina.Candidato Describir(PandilleroAPie p)
        {
            return new ReglaRina.Candidato
            {
                Posicion = p.transform.position,
                Pandilla = p.Pandilla,
                EnPie = p.isActiveAndEnabled && !p.EstaKO,
                VidaFraccion = p.VidaFraccion
            };
        }
    }
}

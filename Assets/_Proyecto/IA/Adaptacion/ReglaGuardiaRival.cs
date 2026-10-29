using System;
using System.Collections.Generic;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// Regla pura de la GUARDIA del rival (T-201, D-tony-196): Abajo, EsperandoReaccion y Sosteniendo. Cuando el golpe del jugador pasa de
    /// Listo a Preparando tira el azar contra la probabilidad de su nivel; si sale, tras <c>reaccion</c> segundos pide sostener, pero solo si
    /// el rival está libre en ese momento. Sostiene hasta <c>sostenerTrasImpacto</c> segundos después de que el golpe sale de Preparando.
    /// El tiempo entra como <c>dt</c> y el azar se inyecta.
    /// </summary>
    public class ReglaGuardiaRival
    {
        private const float Tolerancia = 1e-5f;

        private readonly IReadOnlyList<float> probabilidades;
        private readonly float reaccion;
        private readonly float sostenerTrasImpacto;
        private readonly Func<float> azar;
        private EstadoGuardiaRival estado = EstadoGuardiaRival.Abajo;
        private bool preparandoAntes;
        private bool sinLectura = true;
        private bool esperandoImpacto;
        private float espera;
        private float resto;

        /// <param name="probabilidades">Probabilidad de cubrirse por nivel (índice 0 = nivel I) (0 a 1).</param>
        /// <param name="reaccion">Segundos entre ver empezar el golpe y pedir la guardia (s).</param>
        /// <param name="sostenerTrasImpacto">Segundos que sostiene tras el impacto del golpe (s).</param>
        /// <param name="azar">Devuelve un número de 0 a 1.</param>
        public ReglaGuardiaRival(IReadOnlyList<float> probabilidades, float reaccion, float sostenerTrasImpacto, Func<float> azar)
        {
            this.probabilidades = probabilidades;
            this.reaccion = reaccion;
            this.sostenerTrasImpacto = sostenerTrasImpacto;
            this.azar = azar;
        }

        public EstadoGuardiaRival Estado => estado;
        /// <summary>Si pide la guardia arriba.</summary>
        public bool Sosteniendo => estado == EstadoGuardiaRival.Sosteniendo;

        /// <summary>Vuelve a Abajo y olvida el golpe visto (el rival perdió el control).</summary>
        public void Reiniciar()
        {
            estado = EstadoGuardiaRival.Abajo;
            sinLectura = true;
            esperandoImpacto = false;
        }

        /// <param name="dt">Segundos desde la llamada anterior.</param>
        /// <param name="nivel">Nivel de GUARDIA (0 = no la tiene).</param>
        /// <param name="golpePreparando">Si el golpe del jugador está en Preparando.</param>
        /// <param name="rivalLibre">Si el rival puede cubrirse: golpe en Listo, sin Detenido, sin aviso de habilidad, con control.</param>
        /// <returns>Si debe tener la guardia pedida.</returns>
        public bool Actualizar(float dt, int nivel, bool golpePreparando, bool rivalLibre)
        {
            bool nuevo = golpePreparando && !preparandoAntes && !sinLectura;
            bool salio = !golpePreparando && preparandoAntes;
            preparandoAntes = golpePreparando;
            sinLectura = false;

            if (estado == EstadoGuardiaRival.Abajo && nuevo && nivel > 0 && probabilidades != null && probabilidades.Count > 0)
            {
                int indice = Math.Min(nivel, probabilidades.Count) - 1;
                if (azar() < probabilidades[indice])
                {
                    estado = EstadoGuardiaRival.EsperandoReaccion;
                    espera = reaccion;
                    // El golpe se vio en este instante: el tiempo de este frame no cuenta.
                    dt = 0f;
                }
            }

            if (estado == EstadoGuardiaRival.EsperandoReaccion)
            {
                espera -= dt;
                if (espera <= Tolerancia)
                {
                    if (rivalLibre)
                    {
                        estado = EstadoGuardiaRival.Sosteniendo;
                        esperandoImpacto = golpePreparando;
                        resto = sostenerTrasImpacto;
                        salio = false;
                    }
                    else
                    {
                        estado = EstadoGuardiaRival.Abajo;
                    }
                }
                return Sosteniendo;
            }

            if (estado == EstadoGuardiaRival.Sosteniendo)
            {
                if (golpePreparando)
                {
                    // Un golpe nuevo (o el mismo, sin salir aún) alarga el sostener hasta su impacto.
                    esperandoImpacto = true;
                }
                else
                {
                    if (salio)
                    {
                        // El tiempo de sostener empieza a contar desde el cuadro siguiente al impacto.
                        esperandoImpacto = false;
                        resto = sostenerTrasImpacto;
                    }
                    else if (!esperandoImpacto)
                    {
                        resto -= dt;
                        if (resto <= Tolerancia)
                        {
                            estado = EstadoGuardiaRival.Abajo;
                        }
                    }
                }
            }
            return Sosteniendo;
        }
    }
}

using System;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.IA.Datos;

namespace MechaCyberpunk.IA.Adaptacion
{
    /// <summary>
    /// Regla pura (T-199, D-tony-194): el nivel de cada adaptación según lo que el rival recuerda, y cómo cambia la memoria al terminar
    /// una pelea. Sin tiempo ni azar.
    /// </summary>
    public static class ReglaAdaptacion
    {
        private const int Tipos = 3;

        /// <summary>El mayor nivel cuyo umbral alcanza la evidencia, sin pasar de nivelMaximo. Memoria null: 0.</summary>
        public static int Nivel(MemoriaRival memoria, TipoAdaptacion tipo, DatosAdaptacionRival datos)
        {
            if (memoria == null || datos == null)
            {
                return 0;
            }
            return NivelDeEvidencia(Evidencia(memoria, tipo), datos);
        }

        /// <summary>
        /// Actualiza la memoria con una pelea terminada y devuelve un cambio por cada tipo cuyo nivel cambió.
        /// <paramref name="vistos"/> se indexa por TipoAdaptacion (largo 3).
        /// </summary>
        public static List<CambioAdaptacion> Aprender(MemoriaRival memoria, bool[] vistos, bool ganoElJugador, DatosAdaptacionRival datos)
        {
            List<CambioAdaptacion> cambios = new List<CambioAdaptacion>();
            if (memoria == null || datos == null)
            {
                return cambios;
            }
            if (memoria.evidencias == null || memoria.evidencias.Length != Tipos)
            {
                int[] nuevas = new int[Tipos];
                if (memoria.evidencias != null)
                {
                    Array.Copy(memoria.evidencias, nuevas, Math.Min(memoria.evidencias.Length, Tipos));
                }
                memoria.evidencias = nuevas;
            }

            int[] antes = new int[Tipos];
            for (int i = 0; i < Tipos; i++)
            {
                antes[i] = NivelDeEvidencia(memoria.evidencias[i], datos);
            }

            memoria.peleas++;
            memoria.derrotasSeguidas = ganoElJugador ? 0 : memoria.derrotasSeguidas + 1;
            bool puedeSubir = memoria.derrotasSeguidas < datos.DerrotasSinAprender;
            int tope = datos.UmbralesNivel[datos.NivelMaximo - 1];

            bool[] visto = new bool[Tipos];
            for (int i = 0; i < Tipos; i++)
            {
                visto[i] = vistos != null && i < vistos.Length && vistos[i];
                if (visto[i])
                {
                    if (puedeSubir)
                    {
                        memoria.evidencias[i]++;
                    }
                }
                else
                {
                    memoria.evidencias[i] -= datos.OlvidoPorPelea;
                }
                memoria.evidencias[i] = Math.Max(0, Math.Min(memoria.evidencias[i], tope));
            }

            LimitarActivas(memoria, visto, datos);

            for (int i = 0; i < Tipos; i++)
            {
                int despues = NivelDeEvidencia(memoria.evidencias[i], datos);
                if (despues != antes[i])
                {
                    cambios.Add(new CambioAdaptacion((TipoAdaptacion)i, antes[i], despues));
                }
            }
            return cambios;
        }

        private static void LimitarActivas(MemoriaRival memoria, bool[] visto, DatosAdaptacionRival datos)
        {
            int apagada = datos.UmbralesNivel[0] - 1;
            while (true)
            {
                int activas = 0;
                int elegido = -1;
                for (int i = 0; i < Tipos; i++)
                {
                    if (NivelDeEvidencia(memoria.evidencias[i], datos) < 1)
                    {
                        continue;
                    }
                    activas++;
                    if (elegido < 0 || EsMejorParaBajar(i, elegido, memoria, visto))
                    {
                        elegido = i;
                    }
                }
                if (activas <= datos.MaximoActivas)
                {
                    return;
                }
                memoria.evidencias[elegido] = apagada;
            }
        }

        // Baja primero el de menos evidencia; empate: el que no se vio en esta pelea; luego el que va después en el enum.
        private static bool EsMejorParaBajar(int candidato, int actual, MemoriaRival memoria, bool[] visto)
        {
            if (memoria.evidencias[candidato] != memoria.evidencias[actual])
            {
                return memoria.evidencias[candidato] < memoria.evidencias[actual];
            }
            if (visto[candidato] != visto[actual])
            {
                return !visto[candidato];
            }
            return candidato > actual;
        }

        private static int Evidencia(MemoriaRival memoria, TipoAdaptacion tipo)
        {
            int i = (int)tipo;
            return memoria.evidencias != null && i >= 0 && i < memoria.evidencias.Length ? memoria.evidencias[i] : 0;
        }

        private static int NivelDeEvidencia(int evidencia, DatosAdaptacionRival datos)
        {
            int nivel = 0;
            for (int n = 1; n <= datos.NivelMaximo; n++)
            {
                if (evidencia >= datos.UmbralesNivel[n - 1])
                {
                    nivel = n;
                }
            }
            return nivel;
        }
    }
}

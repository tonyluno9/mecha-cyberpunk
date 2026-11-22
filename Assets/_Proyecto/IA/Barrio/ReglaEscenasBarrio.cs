using System;
using System.Collections.Generic;

namespace MechaCyberpunk.IA.Barrio
{
    /// <summary>
    /// T-1624 (T12 y T15): reglas puras de cuándo y cuál escena del barrio sale. Nunca dos a la vez, tiempo mínimo entre escenas, no repite la
    /// misma seguida y cada pandilla espera entre bullyings. El tiempo y el azar llegan de fuera.
    /// </summary>
    public sealed class ReglaEscenasBarrio
    {
        public struct Candidata
        {
            public string Id;
            public bool EsBullying;
            public int Pandilla;
            public float Peso;
        }

        private const float PesoMinimo = 0.01f;
        private const float SegundosBase = 1.2f;
        private const float SegundosPorLetra = 0.055f;
        private const float SegundosMinimos = 1.8f;
        private const float SegundosMaximos = 7f;

        private readonly Func<float> azar;
        private readonly float espaciadoMin;
        private readonly float espaciadoMax;
        private readonly float esperaBullyingMin;
        private readonly float esperaBullyingMax;
        private readonly Dictionary<int, float> proximoBullying = new Dictionary<int, float>();
        private float proximaEscena;

        public bool EnCurso { get; private set; }
        public string UltimoId { get; private set; }

        public ReglaEscenasBarrio(Func<float> azar, float primeraEspera, float espaciadoMin, float espaciadoMax, float esperaBullyingMin, float esperaBullyingMax)
        {
            this.azar = azar ?? (() => 0.5f);
            this.espaciadoMin = espaciadoMin;
            this.espaciadoMax = Math.Max(espaciadoMin, espaciadoMax);
            this.esperaBullyingMin = esperaBullyingMin;
            this.esperaBullyingMax = Math.Max(esperaBullyingMin, esperaBullyingMax);
            proximaEscena = primeraEspera;
        }

        /// <summary>True si puede arrancar una escena ahora: ninguna en curso, nada la bloquea y ya pasó el tiempo mínimo.</summary>
        public bool PuedeIntentar(float ahora, bool bloqueado) => !EnCurso && !bloqueado && ahora >= proximaEscena;

        /// <summary>Elige una candidata al azar según su peso: sin repetir la última (si hay otra) y sin bullying de una pandilla que aún espera. Null si ninguna sirve.</summary>
        public string Elegir(IReadOnlyList<Candidata> candidatas, float ahora)
        {
            float total = 0f;
            for (int i = 0; i < candidatas.Count; i++)
            {
                if (Sirve(candidatas[i], ahora, candidatas.Count))
                {
                    total += Math.Max(PesoMinimo, candidatas[i].Peso);
                }
            }
            if (total <= 0f)
            {
                return null;
            }
            float punto = azar() * total;
            string ultimaValida = null;
            for (int i = 0; i < candidatas.Count; i++)
            {
                if (!Sirve(candidatas[i], ahora, candidatas.Count))
                {
                    continue;
                }
                ultimaValida = candidatas[i].Id;
                punto -= Math.Max(PesoMinimo, candidatas[i].Peso);
                if (punto <= 0f)
                {
                    return candidatas[i].Id;
                }
            }
            return ultimaValida;
        }

        private bool Sirve(Candidata c, float ahora, int cantidad)
        {
            if (cantidad > 1 && c.Id == UltimoId)
            {
                return false;
            }
            return !c.EsBullying || ahora >= EsperaBullying(c.Pandilla);
        }

        public float EsperaBullying(int pandilla) => proximoBullying.TryGetValue(pandilla, out float t) ? t : 0f;

        public void Empezar(string id)
        {
            EnCurso = true;
            UltimoId = id;
        }

        /// <summary>Termina la escena: fija cuándo puede salir la siguiente y, si era bullying, cuándo vuelve esa pandilla.</summary>
        public void Terminar(float ahora, bool eraBullying, int pandilla)
        {
            EnCurso = false;
            proximaEscena = ahora + Rango(espaciadoMin, espaciadoMax);
            if (eraBullying)
            {
                proximoBullying[pandilla] = ahora + Rango(esperaBullyingMin, esperaBullyingMax);
            }
        }

        /// <summary>No salió (faltaban actores): reintenta pronto sin gastar la espera de la pandilla.</summary>
        public void Reintentar(float ahora, float segundos)
        {
            EnCurso = false;
            proximaEscena = ahora + segundos;
        }

        public void Reiniciar(float primeraEspera)
        {
            EnCurso = false;
            UltimoId = null;
            proximoBullying.Clear();
            proximaEscena = primeraEspera;
        }

        private float Rango(float min, float max) => min + (max - min) * azar();

        /// <summary>Segundos que dura una línea en pantalla (para encadenar la siguiente): la pedida, el clip si hay, o según el texto. 0 si no hay texto.</summary>
        public static float DuracionLinea(float pedida, float duracionClip, string texto)
        {
            if (pedida > 0f)
            {
                return pedida;
            }
            if (duracionClip > 0f)
            {
                return duracionClip;
            }
            int letras = string.IsNullOrEmpty(texto) ? 0 : texto.Length;
            return letras == 0 ? 0f : Math.Min(SegundosMaximos, Math.Max(SegundosMinimos, SegundosBase + letras * SegundosPorLetra));
        }
    }
}

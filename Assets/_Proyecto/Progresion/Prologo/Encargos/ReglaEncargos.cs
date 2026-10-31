using System;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Encargos
{
    /// <summary>Los tipos de encargo del juego libre (T-426). Escoltar aliado y carreras quedan pendientes.</summary>
    public enum TipoEncargo
    {
        Cuota,
        Defender,
        Robar
    }

    /// <summary>
    /// Reglas puras de los encargos repetibles (T-426): enfriamiento por tipo, rotación de lo que ofrece el tablero, premios y el "a veces se resisten"
    /// de la cuota (determinista, para poder probarlo). No sabe nada de Unity más allá de JsonUtility.
    /// </summary>
    public sealed class ReglaEncargos
    {
        public const int Tipos = 3;
        public const float EnfriamientoExito = 300f;
        public const float EnfriamientoFallo = 60f;
        public const int CuotaPorLocal = 50;

        private readonly float[] espera = new float[Tipos];
        private readonly int[] hechos = new int[Tipos];
        private int siguiente;

        /// <summary>Biits que paga cada encargo al completarse.</summary>
        public static int Premio(TipoEncargo tipo)
        {
            switch (tipo)
            {
                case TipoEncargo.Cuota: return 100;
                case TipoEncargo.Defender: return 250;
                default: return 300;
            }
        }

        /// <summary>Cuántos "vencidos" de respeto vale terminar el encargo (cada uno suma el respeto de un rival vencido a tu zona).</summary>
        public static int RespetoUnidades(TipoEncargo tipo)
        {
            switch (tipo)
            {
                case TipoEncargo.Cuota: return 3;
                case TipoEncargo.Defender: return 5;
                default: return 4;
            }
        }

        public static string Nombre(TipoEncargo tipo)
        {
            switch (tipo)
            {
                case TipoEncargo.Cuota: return "COBRAR CUOTA";
                case TipoEncargo.Defender: return "DEFENDER ZONA";
                default: return "ROBAR CARRO";
            }
        }

        /// <summary>El local <paramref name="indiceLocal"/> se resiste a pagar una de cada tres veces (según cuántas cuotas llevas cobradas).</summary>
        public static bool SeResiste(int cuotasHechas, int indiceLocal) => (cuotasHechas * 7 + indiceLocal * 13 + 5) % 3 == 0;

        /// <summary>El tablero solo ofrece encargos con la historia en el juego libre y sin otro encargo en curso.</summary>
        public static bool Permitido(bool historiaLibre, bool hayEncargo) => historiaLibre && !hayEncargo;

        public bool Disponible(TipoEncargo tipo) => espera[(int)tipo] <= 0f;

        public float Espera(TipoEncargo tipo) => Mathf.Max(0f, espera[(int)tipo]);

        public int Hechos(TipoEncargo tipo) => hechos[(int)tipo];

        /// <summary>El enfriamiento más corto entre los tipos que aún esperan (0 si hay uno disponible).</summary>
        public float EsperaMinima()
        {
            float minima = float.MaxValue;
            for (int i = 0; i < Tipos; i++)
            {
                minima = Mathf.Min(minima, Mathf.Max(0f, espera[i]));
            }
            return minima;
        }

        public void Avanzar(float segundos)
        {
            for (int i = 0; i < Tipos; i++)
            {
                if (espera[i] > 0f)
                {
                    espera[i] = Mathf.Max(0f, espera[i] - segundos);
                }
            }
        }

        /// <summary>El tipo que toca ofrecer ahora (rota entre los disponibles). False si todos esperan.</summary>
        public bool TryProximo(out TipoEncargo tipo)
        {
            for (int k = 0; k < Tipos; k++)
            {
                int i = (siguiente + k) % Tipos;
                if (espera[i] <= 0f)
                {
                    tipo = (TipoEncargo)i;
                    return true;
                }
            }
            tipo = TipoEncargo.Cuota;
            return false;
        }

        /// <summary>Deja el tipo en espera y avanza la rotación al que sigue. Con éxito cuenta como hecho.</summary>
        public void Terminar(TipoEncargo tipo, bool exito)
        {
            int i = (int)tipo;
            espera[i] = exito ? EnfriamientoExito : EnfriamientoFallo;
            if (exito)
            {
                hechos[i]++;
            }
            siguiente = (i + 1) % Tipos;
        }

        [Serializable]
        private class Datos
        {
            public float[] espera = Array.Empty<float>();
            public int[] hechos = Array.Empty<int>();
            public int siguiente;
        }

        public string Guardar() => JsonUtility.ToJson(new Datos { espera = (float[])espera.Clone(), hechos = (int[])hechos.Clone(), siguiente = siguiente });

        /// <summary>Lee un guardado; si no se entiende deja todo como estaba y devuelve false.</summary>
        public bool Cargar(string texto)
        {
            Datos leidos = null;
            try
            {
                leidos = string.IsNullOrEmpty(texto) ? null : JsonUtility.FromJson<Datos>(texto);
            }
            catch (Exception)
            {
                // se devuelve false abajo
            }
            if (leidos == null)
            {
                return false;
            }
            for (int i = 0; i < Tipos; i++)
            {
                espera[i] = leidos.espera != null && i < leidos.espera.Length ? Mathf.Max(0f, leidos.espera[i]) : 0f;
                hechos[i] = leidos.hechos != null && i < leidos.hechos.Length ? Mathf.Max(0, leidos.hechos[i]) : 0;
            }
            siguiente = Mathf.Clamp(leidos.siguiente, 0, Tipos - 1);
            return true;
        }
    }
}

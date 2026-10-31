using System;
using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.Progresion.Prologo.Encargos
{
    /// <summary>
    /// Reglas puras de los ataques rivales a tus zonas (T-427): cada cuánto, cuándo se permiten, quién ataca, el aviso y el castigo.
    /// Guarda solo la espera que falta y los contadores.
    /// </summary>
    public sealed class ReglaAtaques
    {
        private float espera = -1f;
        private int defendidos;
        private int perdidos;

        /// <summary>Segundos que faltan para el siguiente ataque (-1 si aún no se sortea).</summary>
        public float Espera => espera;
        public int Defendidos => defendidos;
        public int Perdidos => perdidos;

        /// <summary>Espera hasta el siguiente ataque: al azar entre mínimo y máximo, dividida entre 1 + más por zona (menos la primera) + más por nivel de calor.</summary>
        public static float Intervalo(float minimo, float maximo, int zonasTuyas, int nivelCalor, float azar01, float masPorZona, float masPorCalor)
        {
            float basico = Mathf.Lerp(minimo, Mathf.Max(minimo, maximo), Mathf.Clamp01(azar01));
            float factor = 1f + masPorZona * Mathf.Max(0, zonasTuyas - 1) + masPorCalor * Mathf.Max(0, nivelCalor);
            return Mathf.Max(minimo * 0.25f, basico / factor);
        }

        /// <summary>No ocurre durante la historia, durante un encargo, con otro ataque en curso ni sin una zona que atacar.</summary>
        public static bool Permitido(bool historiaLibre, bool hayEncargo, bool hayAtaque, int zonasAtacables) =>
            historiaLibre && !hayEncargo && !hayAtaque && zonasAtacables > 0;

        /// <summary>Quién ataca: los SNAKES a las zonas del centro, los SKULLS a las del este (la pandilla vecina).</summary>
        public static IdPandilla Atacante(string zona) => zona == "neon" ? IdPandilla.Calaveras : IdPandilla.Viboras;

        public static string NombrePandilla(IdPandilla pandilla) => pandilla == IdPandilla.Calaveras ? "SKULLS" : "SNAKES";

        public static string Aviso(IdPandilla atacante, string nombreZona) => "¡LOS " + NombrePandilla(atacante) + " ATACAN " + nombreZona + "!";

        /// <summary>Índice de la zona atacada entre <paramref name="cuenta"/>, según un azar de 0 a 1.</summary>
        public static int Elegir(int cuenta, float azar01) => cuenta <= 0 ? -1 : Mathf.Clamp((int)(azar01 * cuenta), 0, cuenta - 1);

        /// <summary>La zona pasa al rival si su respeto, ya con el castigo, queda por debajo del umbral.</summary>
        public static bool PierdeZona(int respetoActual, int castigo, int umbral) => Mathf.Max(0, respetoActual - castigo) < umbral;

        public void Fijar(float segundos) => espera = Mathf.Max(0f, segundos);

        public void Avanzar(float segundos)
        {
            if (espera > 0f)
            {
                espera = Mathf.Max(0f, espera - segundos);
            }
        }

        public bool Listo => espera == 0f;

        public void Terminar(bool defendida)
        {
            espera = -1f;
            if (defendida)
            {
                defendidos++;
            }
            else
            {
                perdidos++;
            }
        }

        [Serializable]
        private class Datos
        {
            public float espera = -1f;
            public int defendidos;
            public int perdidos;
        }

        public string Guardar() => JsonUtility.ToJson(new Datos { espera = espera, defendidos = defendidos, perdidos = perdidos });

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
            espera = leidos.espera < 0f ? -1f : leidos.espera;
            defendidos = Mathf.Max(0, leidos.defendidos);
            perdidos = Mathf.Max(0, leidos.perdidos);
            return true;
        }
    }
}

using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1502: cuentas puras (sin escena) para cambiar los edificios de primitivas por modelos: qué es cada primitiva, si la malla es de Unity,
    /// hacia dónde mira el edificio y qué escala y cuántos tramos necesita un modelo para llenar el hueco del cubo.
    /// </summary>
    public static class CalculoEdificiosT1502
    {
        public const float EscalaMinima = 0.45f;
        public const float EscalaMaxima = 3.2f;
        public const float DistorsionMaxima = 2.4f;
        public const int MaxTramos = 6;
        private const float CostoPorTramo = 0.05f;

        public enum Tipo
        {
            Ninguno,
            /// <summary>Cubo "Edificio" de las salidas de las afueras (T-530): se reemplaza por modelos.</summary>
            EdificioAfueras,
            /// <summary>Cubo "Edificio" de un barrio, vestido con piezas del kit de fachadas y un LODGroup: se anota, el kit depende de él.</summary>
            EdificioConKit,
            /// <summary>Torre del fondo (NeonAlto, Horizonte): sin modelo de ese alto, va a assets-faltan.</summary>
            Torre,
            /// <summary>Muro largo o de local: el muro de la ciudad pide modelo exacto; los de locales sostienen el interior.</summary>
            Muro,
            /// <summary>Cuerpo de un local con interior o de un lugar: el interior depende de él, no se cambia.</summary>
            LocalConInterior,
            /// <summary>Otro bloque grande de primitiva.</summary>
            BloqueGrande
        }

        /// <summary>Verdadero si la ruta de la malla es una malla de Unity (cubo, esfera...) o una generada sin archivo de modelo.</summary>
        public static bool EsMallaPrimitiva(string rutaMalla)
        {
            if (string.IsNullOrEmpty(rutaMalla))
            {
                return true;
            }
            return rutaMalla.StartsWith("Library/") || rutaMalla.Contains("unity default resources") || rutaMalla.Contains("unity_builtin_extra");
        }

        /// <summary>Decide qué es una primitiva por su ruta en la jerarquía ("A/B/C") y el tamaño de su caja en el mundo.</summary>
        public static Tipo Clasificar(string ruta, Vector3 tam)
        {
            if (string.IsNullOrEmpty(ruta) || ruta.Contains("T1502_Modelo") || ruta.Contains("AMB_") || ruta.Contains("Niebla"))
            {
                return Tipo.Ninguno;
            }
            string[] partes = ruta.Split('/');
            string nombre = partes[partes.Length - 1];

            if (nombre == "Edificio")
            {
                return ruta.Contains("Afueras") ? Tipo.EdificioAfueras : Tipo.EdificioConKit;
            }
            if (ruta.Contains("Torre_") && (nombre == "Cuerpo" || nombre == "Remate"))
            {
                return Tipo.Torre;
            }
            if (nombre.StartsWith("Muro") || ruta.Contains("/Orillas/"))
            {
                if (tam.y >= 3f && Mathf.Max(tam.x, tam.z) >= 6f)
                {
                    return ruta.Contains("Locales/") || ruta.Contains("Armero") || ruta.Contains("Casino") ? Tipo.LocalConInterior : Tipo.Muro;
                }
                return Tipo.Ninguno;
            }
            if (nombre.StartsWith("Cuerpo") && (ruta.Contains("Taller") || ruta.Contains("Casa") || nombre.StartsWith("Cuerpo_T")))
            {
                return Tipo.LocalConInterior;
            }
            if (tam.y >= 6f && Mathf.Min(tam.x, tam.z) >= 4f && Mathf.Max(tam.x, tam.z) >= 6f && nombre != "Cuerpo")
            {
                return Tipo.BloqueGrande;
            }
            return Tipo.Ninguno;
        }

        /// <summary>Texto corto de para qué sirve cada tipo (va al listado).</summary>
        public static string Uso(Tipo tipo)
        {
            switch (tipo)
            {
                case Tipo.EdificioAfueras: return "edificio de las afueras";
                case Tipo.EdificioConKit: return "edificio del barrio con kit de fachadas";
                case Tipo.Torre: return "torre del fondo (skyline)";
                case Tipo.Muro: return "muro largo (muro de la ciudad)";
                case Tipo.LocalConInterior: return "local con interior o lugar";
                case Tipo.BloqueGrande: return "bloque grande";
                default: return "otro";
            }
        }

        /// <summary>Dirección cardinal (+-X o +-Z) más cercana a la dirección dada, en el plano del suelo.</summary>
        public static Vector3 Cardinal(Vector3 dir)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
            {
                return new Vector3(dir.x >= 0f ? 1f : -1f, 0f, 0f);
            }
            return new Vector3(0f, 0f, dir.z >= 0f ? 1f : -1f);
        }

        /// <summary>Verdadero si el ángulo (grados) es múltiplo de 90 con 1 grado de holgura.</summary>
        public static bool EsMultiplo90(float grados)
        {
            float r = Mathf.Abs(grados % 90f);
            return r < 1f || r > 89f;
        }

        /// <summary>
        /// Escala un modelo (medido sin girar: ancho x, alto y, fondo z; mira a +Z) para llenar un hueco de frente x fondo x alto,
        /// partiendo el frente en 'tramos' copias. Falla si alguna escala sale de los límites o el modelo queda muy estirado.
        /// </summary>
        public static bool Ajustar(Vector3 modelo, float frente, float fondo, float alto, out int tramos, out Vector3 escala, out float costo, float distorsionMaxima = DistorsionMaxima)
        {
            tramos = 0;
            escala = Vector3.one;
            costo = float.MaxValue;
            if (modelo.x <= 0.01f || modelo.y <= 0.01f || modelo.z <= 0.01f || frente <= 0.01f || fondo <= 0.01f || alto <= 0.01f)
            {
                return false;
            }
            for (int n = 1; n <= MaxTramos; n++)
            {
                float sx = frente / n / modelo.x;
                float sy = alto / modelo.y;
                float sz = fondo / modelo.z;
                float menor = Mathf.Min(sx, Mathf.Min(sy, sz));
                float mayor = Mathf.Max(sx, Mathf.Max(sy, sz));
                if (menor < EscalaMinima || mayor > EscalaMaxima || mayor / menor > distorsionMaxima)
                {
                    continue;
                }
                float c = Mathf.Log(mayor / menor) + CostoPorTramo * (n - 1);
                if (c < costo)
                {
                    costo = c;
                    tramos = n;
                    escala = new Vector3(sx, sy, sz);
                }
            }
            return tramos > 0;
        }
    }
}

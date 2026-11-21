using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Editor
{
    /// <summary>
    /// T-1544: cuentas puras (sin escena) para cambiar las casas generadas como caja por modelos reales: de qué barrio es una casa,
    /// si es un lugar del juego, cuánto del frente tapan las piezas del kit y qué escala y cuántas copias necesita un modelo.
    /// </summary>
    public static class CalculoCasasT1544
    {
        /// <summary>Cuánto se mete el modelo detrás del frente de la caja cuando el kit ya tapa el frente (m): el relieve del modelo no asoma entre las piezas.</summary>
        public const float RecesoConKit = 0.35f;
        /// <summary>Receso cuando el kit no tapa el frente: el modelo es la fachada que se ve (m).</summary>
        public const float RecesoSinKit = 0.02f;
        /// <summary>Fracción del ancho del frente tapada por piezas del kit a partir de la cual se considera que el kit es la fachada.</summary>
        public const float CoberturaKit = 0.6f;
        /// <summary>Grosor del panel que cubre un lado de un lugar (m).</summary>
        public const float GrosorPanel = 0.3f;
        public const float HuecoMaximoPiso = 1.5f;
        public const float HuecoMinimoPiso = 0.05f;
        /// <summary>Distancia en el plano del suelo desde la caja a la que una marca de lugar vuelve lugar a la casa (m).</summary>
        public const float MargenLugar = 2f;
        public const float AltoMinimoCasa = 3f;
        public const int MaxTramos = 6;
        public const int MaxPilas = 4;
        private const float CostoPorTramo = 0.05f;
        private const float CostoPorPila = 0.15f;

        public const string Estricto = "estricto";
        public const string Relajado = "relajado";

        /// <summary>Nombres de componentes que vuelven lugar del juego a la casa que los lleva o los tiene pegados.</summary>
        public static readonly string[] TiposLugar =
        {
            "NegocioPunto", "MarcaLugar", "GuaridaPunto", "PuertaAutomatica", "HojaPuerta", "HojasCorredizas", "MostradorTienda",
            "ZonaInterior", "LugarMundo", "PuertaSinHorario", "SillaClinica", "GarajeCarro", "PantallaTienda", "TableroEncargos",
            "BancoArmas", "RegaloImplanteDoc", "ServicioGuarida", "ServicioNegocios", "RecintoJefe", "PuestoGuardiasPesados"
        };

        /// <summary>Prefijos de nombre de objetos que marcan un lugar (no los accesos de techo ni las puertas decorativas del kit).</summary>
        public static readonly string[] PrefijosLugar = { "Lugar_", "Marca_", "Negocio_", "Guarida_", "Entrada_" };

        public static bool EsTipoLugar(string tipo)
        {
            return !string.IsNullOrEmpty(tipo) && System.Array.IndexOf(TiposLugar, tipo) >= 0;
        }

        public static bool EsNombreLugar(string nombre)
        {
            if (string.IsNullOrEmpty(nombre))
            {
                return false;
            }
            foreach (string p in PrefijosLugar)
            {
                if (nombre.StartsWith(p, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Clave de paleta de edificios (las de CalculoEdificiosT1512) según la ruta de la casa en la jerarquía.</summary>
        public static string BarrioDe(string ruta)
        {
            if (string.IsNullOrEmpty(ruta))
            {
                return "";
            }
            if (ruta.Contains("Nova"))
            {
                return "NeonAlto";
            }
            if (ruta.Contains("Mercado") || ruta.Contains("CalleDorada") || ruta.Contains("Tianguis"))
            {
                return "Tianguis";
            }
            if (ruta.Contains("Suburbios") || ruta.Contains("CalleDoc") || ruta.Contains("CalleK01") || ruta.Contains("Barrio"))
            {
                return "Suburbios";
            }
            return "";
        }

        /// <summary>Verdadero si la ruta es una torre del skyline (Torre_N): fuera del alcance de las casas.</summary>
        public static bool EsTorre(string ruta)
        {
            return !string.IsNullOrEmpty(ruta) && (ruta.Contains("/Torre_") || ruta.StartsWith("Torre_"));
        }

        /// <summary>Verdadero si el cuerpo es el de una casa o edificio de calle (no torre, no adorno).</summary>
        public static bool EsCuerpoDeCasa(string ruta, string nombre, string malla, string material, Vector3 tam)
        {
            if (EsTorre(ruta) || (!string.IsNullOrEmpty(ruta) && ruta.Contains("Tragamonedas")))
            {
                return false;
            }
            bool llamaCuerpo = nombre == "Cuerpo" || (!string.IsNullOrEmpty(malla) && malla.Contains("_Cuerpo_"));
            bool colorDeCuerpo = !string.IsNullOrEmpty(material) && (material.StartsWith("S2c_Cuerpo") || material.StartsWith("S2p_NovaCuerpo"));
            if (!llamaCuerpo && !colorDeCuerpo)
            {
                return false;
            }
            return tam.y >= AltoMinimoCasa && Mathf.Min(tam.x, tam.z) >= 1f;
        }

        /// <summary>Largo de la unión de los intervalos (min, max) recortados a [-ancho/2, ancho/2], entre el ancho: 0 a 1.</summary>
        public static float Cobertura(IList<Vector2> intervalos, float ancho)
        {
            if (intervalos == null || intervalos.Count == 0 || ancho <= 0.0001f)
            {
                return 0f;
            }
            List<Vector2> lista = new List<Vector2>();
            float mitad = ancho * 0.5f;
            foreach (Vector2 i in intervalos)
            {
                float a = Mathf.Max(Mathf.Min(i.x, i.y), -mitad);
                float b = Mathf.Min(Mathf.Max(i.x, i.y), mitad);
                if (b > a)
                {
                    lista.Add(new Vector2(a, b));
                }
            }
            lista.Sort((p, q) => p.x.CompareTo(q.x));
            float total = 0f;
            float fin = float.MinValue;
            foreach (Vector2 i in lista)
            {
                float desde = Mathf.Max(i.x, fin);
                if (i.y > desde)
                {
                    total += i.y - desde;
                }
                fin = Mathf.Max(fin, i.y);
            }
            return Mathf.Clamp01(total / ancho);
        }

        /// <summary>Receso del modelo detrás del frente de la caja según cuánto del frente tapa el kit.</summary>
        public static float Receso(float cobertura)
        {
            return cobertura >= CoberturaKit ? RecesoConKit : RecesoSinKit;
        }

        /// <summary>Límites de escala y estiramiento de cada nivel de ajuste: estricto (el de T-1502) y relajado (último recurso).</summary>
        private static void Limites(string nivel, out float escalaMinima, out float escalaMaxima, out float distorsion)
        {
            if (nivel == Relajado)
            {
                escalaMinima = 0.3f;
                escalaMaxima = 5f;
                distorsion = 4.5f;
            }
            else
            {
                escalaMinima = CalculoEdificiosT1502.EscalaMinima;
                escalaMaxima = CalculoEdificiosT1502.EscalaMaxima;
                distorsion = CalculoEdificiosT1502.DistorsionMaxima;
            }
        }

        /// <summary>
        /// Escala un modelo (ancho x, alto y, fondo z; mira a +Z) para llenar frente x fondo x alto con 'tramos' copias en fila y 'pilas' apiladas.
        /// Prefiere una sola pila. Falla si alguna escala sale de los límites del nivel o el estiramiento pasa del permitido.
        /// </summary>
        public static bool Ajustar(Vector3 modelo, float frente, float fondo, float alto, string nivel, out int tramos, out int pilas, out Vector3 escala, out float costo)
        {
            tramos = 0;
            pilas = 0;
            escala = Vector3.one;
            costo = float.MaxValue;
            if (modelo.x <= 0.01f || modelo.y <= 0.01f || modelo.z <= 0.01f || frente <= 0.01f || fondo <= 0.01f || alto <= 0.01f)
            {
                return false;
            }
            float escMin, escMax, distMax;
            Limites(nivel, out escMin, out escMax, out distMax);
            for (int p = 1; p <= MaxPilas; p++)
            {
                for (int n = 1; n <= MaxTramos; n++)
                {
                    float sx = frente / n / modelo.x;
                    float sy = alto / p / modelo.y;
                    float sz = fondo / modelo.z;
                    float menor = Mathf.Min(sx, Mathf.Min(sy, sz));
                    float mayor = Mathf.Max(sx, Mathf.Max(sy, sz));
                    if (menor < escMin || mayor > escMax || mayor / menor > distMax)
                    {
                        continue;
                    }
                    float c = Mathf.Log(mayor / menor) + CostoPorTramo * (n - 1) + CostoPorPila * (p - 1);
                    if (c < costo)
                    {
                        costo = c;
                        tramos = n;
                        pilas = p;
                        escala = new Vector3(sx, sy, sz);
                    }
                }
            }
            return tramos > 0;
        }

        /// <summary>
        /// Igual que Ajustar pero para un panel delgado: el fondo del modelo se aplasta al grosor y no cuenta en el estiramiento
        /// (solo importan el ancho y el alto de la fachada).
        /// </summary>
        public static bool AjustarPanel(Vector3 modelo, float ancho, float alto, float grosor, string nivel, out int tramos, out int pilas, out Vector3 escala, out float costo)
        {
            bool ok = Ajustar(modelo, ancho, modelo.z, alto, nivel, out tramos, out pilas, out escala, out costo);
            if (ok)
            {
                escala.z = Mathf.Max(grosor, 0.01f) / modelo.z;
            }
            return ok;
        }

        /// <summary>Verdadero si el punto (x, z de 'lado' y 'fondo' respecto del centro de una caja) cae dentro de ella con holgura 'margen'.</summary>
        public static bool DentroDeCaja(Vector3 local, Vector3 tam, float margen)
        {
            return Mathf.Abs(local.x) <= tam.x * 0.5f + margen && Mathf.Abs(local.y) <= tam.y * 0.5f + margen && Mathf.Abs(local.z) <= tam.z * 0.5f + margen;
        }

        /// <summary>Fracción del volumen de 'a' que queda dentro de 'b' (0 a 1). Cada lado de 'a' cuenta al menos 5 cm para que una pieza plana pese.</summary>
        public static float FraccionDentro(Bounds a, Bounds b)
        {
            Vector3 mitad = Vector3.Max(a.extents, Vector3.one * 0.025f);
            Vector3 aMin = a.center - mitad;
            Vector3 aMax = a.center + mitad;
            Vector3 dMin = Vector3.Max(aMin, b.min);
            Vector3 dMax = Vector3.Min(aMax, b.max);
            Vector3 d = dMax - dMin;
            if (d.x <= 0f || d.y <= 0f || d.z <= 0f)
            {
                return 0f;
            }
            Vector3 t = aMax - aMin;
            return Mathf.Clamp01((d.x * d.y * d.z) / (t.x * t.y * t.z));
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Las calles de la escena para el minimapa (T-701). No usa la geometría fija del barrio viejo (<see cref="GeometriaBarrio"/>): mira la escena viva,
    /// toma las huellas de los volúmenes altos y anchos (edificios, muros) y dibuja los contornos de las manzanas; lo que queda entre ellas son las
    /// calles. Así sigue sirviendo cuando la ciudad cambia, sin depender de nombres. Se busca al cambiar de escena y de nuevo a los pocos
    /// segundos (la ciudad se arma al cargar), y luego cada 30 s.
    /// </summary>
    public static class CatalogoCallesMapa
    {
        /// <summary>Lo menos que debe medir de alto un volumen para contar como edificio (m).</summary>
        public const float AlturaMinima = 4.5f;
        /// <summary>Lo menos que debe medir de lado el más corto de su huella (m): postes y letreros no cuentan.</summary>
        public const float LadoMinimo = 3.5f;
        /// <summary>Lo más alto que puede estar su base sobre el suelo (m): lo que cuelga en el aire no cuenta.</summary>
        public const float BaseMaxima = 2.5f;
        /// <summary>Lo más largo que puede ser un lado (m): lo gigante (cielo, límites del mundo) no es un edificio.</summary>
        public const float LadoMaximo = 150f;
        // La ciudad se arma al cargar y algo se crea por código: se busca de nuevo a 1, 2, 4... s y luego cada 30 s.
        private const float SegundosPrimeraBusqueda = 1f;
        private const float SegundosMaximos = 30f;

        private static List<CalculoCallesMapa.Tramo> tramos = new List<CalculoCallesMapa.Tramo>();
        private static List<CalculoCallesMapa.Tramo> edificios = new List<CalculoCallesMapa.Tramo>();
        private static int escenaBuscada = int.MinValue;
        private static float proximaBusqueda;
        private static float intervalo = SegundosPrimeraBusqueda;

        /// <summary>Los tramos de línea de los bordes de las calles de la escena (m: x este, y norte).</summary>
        public static IReadOnlyList<CalculoCallesMapa.Tramo> Tramos
        {
            get
            {
                Actualizar();
                return tramos;
            }
        }

        /// <summary>T-934: los lados de cada edificio de la escena (m: x este, y norte).</summary>
        public static IReadOnlyList<CalculoCallesMapa.Tramo> Edificios
        {
            get
            {
                Actualizar();
                return edificios;
            }
        }

        /// <summary>Fuerza buscar otra vez (las pruebas lo usan al armar una escena).</summary>
        public static void Reiniciar()
        {
            escenaBuscada = int.MinValue;
            proximaBusqueda = 0f;
            intervalo = SegundosPrimeraBusqueda;
            tramos = new List<CalculoCallesMapa.Tramo>();
            edificios = new List<CalculoCallesMapa.Tramo>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico() => Reiniciar();

        private static void Actualizar()
        {
            int escena = SceneManager.GetActiveScene().handle;
            bool cambio = escena != escenaBuscada;
            if (!cambio && Time.unscaledTime < proximaBusqueda)
            {
                return;
            }
            if (cambio)
            {
                intervalo = SegundosPrimeraBusqueda;
            }
            escenaBuscada = escena;
            proximaBusqueda = Time.unscaledTime + intervalo;
            intervalo = Mathf.Min(intervalo * 2f, SegundosMaximos);
            List<CalculoCallesMapa.Huella> huellas = Buscar();
            tramos = CalculoCallesMapa.ContornosDeManzanas(huellas);
            edificios = CalculoCallesMapa.LadosDeHuellas(huellas);
        }

        private static List<CalculoCallesMapa.Huella> Buscar()
        {
            List<CalculoCallesMapa.Huella> huellas = new List<CalculoCallesMapa.Huella>();
            foreach (Renderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Bounds mundo = r.bounds;
                if (mundo.size.y < AlturaMinima || mundo.min.y > BaseMaxima || Mathf.Min(mundo.size.x, mundo.size.z) < LadoMinimo
                    || Mathf.Max(mundo.size.x, mundo.size.z) > LadoMaximo * 1.5f)
                {
                    continue;
                }
                MeshFilter malla = r.GetComponent<MeshFilter>();
                if (malla == null || malla.sharedMesh == null)
                {
                    continue;
                }
                Bounds local = malla.sharedMesh.bounds;
                Transform t = r.transform;
                float y = local.center.y;
                CalculoCallesMapa.Huella huella = new CalculoCallesMapa.Huella(
                    Plano(t.TransformPoint(new Vector3(local.min.x, y, local.min.z))),
                    Plano(t.TransformPoint(new Vector3(local.max.x, y, local.min.z))),
                    Plano(t.TransformPoint(new Vector3(local.max.x, y, local.max.z))),
                    Plano(t.TransformPoint(new Vector3(local.min.x, y, local.max.z))));
                Vector2 m = huella.Medidas;
                if (Mathf.Min(m.x, m.y) >= LadoMinimo && Mathf.Max(m.x, m.y) <= LadoMaximo)
                {
                    huellas.Add(huella);
                }
            }
            return huellas;
        }

        private static Vector2 Plano(Vector3 mundo) => new Vector2(mundo.x, mundo.z);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Progresion.Opciones
{
    /// <summary>
    /// Las opciones del jugador (T-411, D-tony-243): implementa <see cref="IOpciones"/> y va en <c>Registro&lt;IOpciones&gt;</c>. Lee
    /// opciones.json de <c>persistentDataPath</c> al nacer (o la ruta de <see cref="FijarRuta"/>, para pruebas) y lo escribe en cada
    /// cambio. Aplica al instante: volúmenes por <see cref="Sonidos"/> (también cuando aparece el gestor), la calidad sobre una COPIA en
    /// memoria del asset de URP (el del proyecto no se toca y vuelve al destruirse el servicio), y la resolución y pantalla completa con
    /// <c>Screen.SetResolution</c> (no en el editor). Un archivo roto se ignora con un solo warning.
    /// </summary>
    public class ServicioOpciones : MonoBehaviour, IOpciones
    {
        public const string NombreArchivo = "opciones.json";

        private static string rutaForzada;
        private static bool avisoRoto;

        private ValoresOpciones valores;
        private UniversalRenderPipelineAsset original;
        private UniversalRenderPipelineAsset copia;
        private bool copiaPuesta;
        private ISonidos gestorAplicado;
        private readonly List<Vector2Int> resoluciones = new List<Vector2Int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            rutaForzada = null;
            avisoRoto = false;
        }

        /// <summary>Pruebas: lee y escribe aquí en vez de persistentDataPath. Null lo quita.</summary>
        public static void FijarRuta(string ruta)
        {
            rutaForzada = ruta;
        }

        public static string Ruta => !string.IsNullOrEmpty(rutaForzada) ? rutaForzada : Path.Combine(Application.persistentDataPath, NombreArchivo);

        public float Sensibilidad => Valores.sensibilidad;
        public bool InvertirY => Valores.invertirY;
        public NivelApuntadoAsistido ApuntadoAsistido => Valores.apuntadoAsistido;
        public CalidadGrafica Calidad => Valores.calidad;
        public bool PantallaCompleta => Valores.pantallaCompleta;
        /// <summary>La copia en memoria del asset de URP que cambia la calidad (null si el proyecto no usa URP).</summary>
        public UniversalRenderPipelineAsset CopiaRender => copia;
        /// <summary>Resoluciones de la pantalla sin repetir ancho x alto, de menor a mayor.</summary>
        public IReadOnlyList<Vector2Int> Resoluciones => resoluciones;

        public event Action Cambio;

        private ValoresOpciones Valores
        {
            get
            {
                if (valores == null)
                {
                    Leer();
                }
                return valores;
            }
        }

        public float Volumen(SeccionSonido seccion) => Valores.volumenes[(int)seccion];

        /// <summary>La resolución elegida (la nativa si no se eligió ninguna).</summary>
        public Vector2Int Resolucion
        {
            get
            {
                ValoresOpciones v = Valores;
                return v.resolucionAncho > 0 ? new Vector2Int(v.resolucionAncho, v.resolucionAlto) : Nativa();
            }
        }

        private void Awake()
        {
            Leer();
            JuntarResoluciones();
        }

        private void OnEnable()
        {
            Registro<IOpciones>.Agregar(this);
            AplicarTodo();
        }

        private void OnDisable()
        {
            Registro<IOpciones>.Quitar(this);
        }

        private void OnDestroy()
        {
            QuitarCopia();
        }

        private void Update()
        {
            // El gestor de sonidos puede nacer después (o cambiar con la escena): se le pasan los volúmenes una vez.
            ISonidos gestor = Registro<ISonidos>.Actual;
            if (gestor != null && gestor != gestorAplicado)
            {
                AplicarVolumenes();
            }
        }

        public void FijarSensibilidad(float valor)
        {
            Valores.sensibilidad = ReglaOpciones.RecortarSensibilidad(valor);
            Guardar();
        }

        public void FijarInvertirY(bool valor)
        {
            Valores.invertirY = valor;
            Guardar();
        }

        public void FijarApuntadoAsistido(NivelApuntadoAsistido nivel)
        {
            Valores.apuntadoAsistido = nivel;
            Guardar();
        }

        public void FijarVolumen(SeccionSonido seccion, float valor)
        {
            Valores.volumenes[(int)seccion] = ReglaOpciones.RecortarVolumen(valor);
            AplicarVolumenes();
            Guardar();
        }

        public void FijarCalidad(CalidadGrafica calidad)
        {
            Valores.calidad = calidad;
            AplicarCalidad();
            Guardar();
        }

        public void FijarResolucion(Vector2Int resolucion)
        {
            Valores.resolucionAncho = resolucion.x;
            Valores.resolucionAlto = resolucion.y;
            AplicarPantalla();
            Guardar();
        }

        public void FijarPantallaCompleta(bool valor)
        {
            Valores.pantallaCompleta = valor;
            AplicarPantalla();
            Guardar();
        }

        private void Leer()
        {
            string ruta = Ruta;
            string texto = null;
            try
            {
                if (File.Exists(ruta))
                {
                    texto = File.ReadAllText(ruta);
                }
            }
            catch (IOException e)
            {
                Debug.LogWarning("[Progresion] No se pudo leer " + ruta + ": " + e.Message);
            }
            if (!ReglaOpciones.DeJson(texto, out valores) && !string.IsNullOrWhiteSpace(texto) && !avisoRoto)
            {
                avisoRoto = true;
                Debug.LogWarning("[Progresion] " + ruta + " está roto; se usan las opciones por defecto.");
            }
        }

        private void Guardar()
        {
            string ruta = Ruta;
            try
            {
                string carpeta = Path.GetDirectoryName(ruta);
                if (!string.IsNullOrEmpty(carpeta))
                {
                    Directory.CreateDirectory(carpeta);
                }
                File.WriteAllText(ruta, ReglaOpciones.AJson(valores));
            }
            catch (IOException e)
            {
                Debug.LogWarning("[Progresion] No se pudo escribir " + ruta + ": " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.LogWarning("[Progresion] No se pudo escribir " + ruta + ": " + e.Message);
            }
            Cambio?.Invoke();
        }

        private void AplicarTodo()
        {
            AplicarVolumenes();
            AplicarCalidad();
            AplicarPantalla();
        }

        private void AplicarVolumenes()
        {
            ISonidos gestor = Registro<ISonidos>.Actual;
            gestorAplicado = gestor;
            if (gestor == null)
            {
                return;
            }
            for (int i = 0; i < ReglaOpciones.CantidadSecciones; i++)
            {
                gestor.FijarVolumen((SeccionSonido)i, Valores.volumenes[i]);
            }
        }

        private void AplicarCalidad()
        {
            if (!copiaPuesta)
            {
                original = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
                UniversalRenderPipelineAsset activo = original != null ? original : GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
                if (activo == null)
                {
                    return;
                }
                copia = Instantiate(activo);
                copia.name = activo.name + " (opciones)";
                QualitySettings.renderPipeline = copia;
                copiaPuesta = true;
            }
            CalidadGrafica c = Valores.calidad;
            copia.renderScale = ReglaOpciones.EscalaRender(c);
            copia.shadowDistance = ReglaOpciones.DistanciaSombras(c);
            copia.msaaSampleCount = ReglaOpciones.Msaa(c);
        }

        private void QuitarCopia()
        {
            if (!copiaPuesta)
            {
                return;
            }
            if (QualitySettings.renderPipeline == copia)
            {
                QualitySettings.renderPipeline = original;
            }
            Destroy(copia);
            copia = null;
            copiaPuesta = false;
        }

        private void AplicarPantalla()
        {
            if (Application.isEditor)
            {
                return;
            }
            Vector2Int r = Resolucion;
            FullScreenMode modo = Valores.pantallaCompleta ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.width != r.x || Screen.height != r.y || Screen.fullScreenMode != modo)
            {
                Screen.SetResolution(r.x, r.y, modo);
            }
        }

        private void JuntarResoluciones()
        {
            resoluciones.Clear();
            foreach (Resolution r in Screen.resolutions)
            {
                Vector2Int tamano = new Vector2Int(r.width, r.height);
                if (!resoluciones.Contains(tamano))
                {
                    resoluciones.Add(tamano);
                }
            }
            Vector2Int nativa = Nativa();
            if (!resoluciones.Contains(nativa))
            {
                resoluciones.Add(nativa);
            }
            Vector2Int elegida = Resolucion;
            if (!resoluciones.Contains(elegida))
            {
                resoluciones.Add(elegida);
            }
            resoluciones.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        }

        private static Vector2Int Nativa()
        {
            int ancho = Display.main != null ? Display.main.systemWidth : Screen.currentResolution.width;
            int alto = Display.main != null ? Display.main.systemHeight : Screen.currentResolution.height;
            if (ancho <= 0 || alto <= 0)
            {
                ancho = Screen.currentResolution.width;
                alto = Screen.currentResolution.height;
            }
            return new Vector2Int(ancho, alto);
        }
    }
}

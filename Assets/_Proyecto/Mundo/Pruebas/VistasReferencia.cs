using MechaCyberpunk.Mundo.Datos;
using MechaCyberpunk.Mundo.Reloj;
using UnityEngine;

namespace MechaCyberpunk.Mundo.Pruebas
{
    /// <summary>
    /// Cuatro vistas fijas del barrio (V1 a V4, <see cref="ConfigVistasReferencia"/>) para que todas las capturas de arte sean comparables.
    /// Se usan desde el editor vivo: <c>unity-vivo.sh eval 'MechaCyberpunk.Mundo.Pruebas.VistasReferencia.Ir(1, 22f); return 0;'</c>.
    /// </summary>
    public class VistasReferencia : MonoBehaviour
    {
        private const int AnchoMedicion = 640;
        private const int AltoMedicion = 360;
        private const int ProfundidadMedicion = 24;
        private const float LuminanciaRoja = 0.2126f;
        private const float LuminanciaVerde = 0.7152f;
        private const float LuminanciaAzul = 0.0722f;
        private const float UmbralSaturado = 0.98f;
        private const float PorcentajeTotal = 100f;

        [SerializeField] private ConfigVistasReferencia config;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError("[Mundo] VistasReferencia en '" + name + "' necesita su config.", this);
                enabled = false;
            }
        }

        /// <summary>
        /// Coloca la cámara libre en la vista (1 a 4) y pone el reloj del mundo a esa hora, en pausa.
        /// Devuelve false si falta la cámara libre, el reloj o la vista.
        /// </summary>
        public static bool Ir(int vista, float hora)
        {
            VistasReferencia vistas = FindFirstObjectByType<VistasReferencia>();
            CamaraLibre libre = FindFirstObjectByType<CamaraLibre>();
            RelojMundo reloj = FindFirstObjectByType<RelojMundo>();
            if (vistas == null || libre == null || reloj == null || vista < 1 || vista > vistas.config.Cantidad)
            {
                return false;
            }

            reloj.Pausar(true);
            reloj.AvanzarHasta(hora);
            CicloDiaNoche ciclo = FindFirstObjectByType<CicloDiaNoche>();
            if (ciclo != null)
            {
                ciclo.Aplicar(hora);
            }

            ConfigVistasReferencia.Vista datos = vistas.config[vista - 1];
            libre.PonerVuelo(datos.vuela);
            Vector3 raiz = datos.ojos - Vector3.up * vistas.config.AlturaOjos;
            libre.Colocar(new Pose(raiz, Quaternion.Euler(0f, datos.giro, 0f)), datos.inclinacion);
            return true;
        }

        /// <summary>
        /// Renderiza la cámara activa a 640 x 360 y devuelve la luminancia media (0 a 1) y el porcentaje de píxeles saturados
        /// (luminancia de 0.98 o más). Sirve para medir que el bloom no lava la imagen.
        /// </summary>
        public static string Medir()
        {
            CamaraLibre libre = FindFirstObjectByType<CamaraLibre>();
            if (libre == null)
            {
                return "sin cámara libre";
            }
            Camera camara = libre.Camara;
            RenderTexture textura = new RenderTexture(AnchoMedicion, AltoMedicion, ProfundidadMedicion);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = textura;
            camara.Render();
            camara.targetTexture = null;
            RenderTexture.active = textura;
            Texture2D copia = new Texture2D(AnchoMedicion, AltoMedicion, TextureFormat.RGB24, false);
            copia.ReadPixels(new Rect(0f, 0f, AnchoMedicion, AltoMedicion), 0, 0);
            copia.Apply();
            RenderTexture.active = anterior;

            Color[] pixeles = copia.GetPixels();
            double suma = 0.0;
            int saturados = 0;
            foreach (Color pixel in pixeles)
            {
                float luminancia = LuminanciaRoja * pixel.r + LuminanciaVerde * pixel.g + LuminanciaAzul * pixel.b;
                suma += luminancia;
                if (luminancia >= UmbralSaturado)
                {
                    saturados++;
                }
            }
            Destroy(copia);
            Destroy(textura);
            return "luminancia=" + (suma / pixeles.Length).ToString("F4") + " saturado=" + (PorcentajeTotal * saturados / pixeles.Length).ToString("F2") + "%";
        }
    }
}

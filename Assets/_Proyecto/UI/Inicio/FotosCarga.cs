using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// T100: la imagen de la pantalla de carga: UN frame de las cinemáticas (<see cref="FramesCarga"/>) por carga, al azar y nunca el
    /// mismo que en la carga anterior (<see cref="ReglaFotosCarga.ElegirSinRepetir"/>). Llena la pantalla como "cover": conserva la
    /// proporción y recorta lo que sobra (<see cref="ReglaFotosCarga.TamanoCubierto"/>), con un RectMask2D para que nada se salga del
    /// lienzo (en VR el lienzo va en la pantalla de cine). Solo se carga el frame elegido, no los 22.
    /// </summary>
    public class FotosCarga : MonoBehaviour
    {
        /// <summary>La carpeta de Resources de la que salen las imágenes de carga (solo los frames de T100, ya no L01-L06).</summary>
        public const string CarpetaRecursos = FramesCarga.CarpetaRecursos;
        private const float AnchoPantalla = 1920f;
        private const float AltoPantalla = 1080f;

        private static int ultima = -1;
        private static System.Random azar;

        private RawImage capa;
        private Texture2D foto;
        private int indice = -1;
        private Vector2 pantallaAplicada;

        /// <summary>El índice (en <see cref="FramesCarga.Nombres"/>) del frame de la última carga; -1 si aún no hubo.</summary>
        public static int UltimaElegida => ultima;
        /// <summary>Cuántos frames hay para elegir.</summary>
        public int Cantidad => FramesCarga.Cantidad;
        /// <summary>El índice del frame de esta carga.</summary>
        public int Indice => indice;
        /// <summary>La imagen que se ve.</summary>
        public Texture FotoActual => capa != null ? capa.texture : null;
        /// <summary>Escala de la foto (sin zoom: siempre 1).</summary>
        public float EscalaActual => capa != null ? capa.rectTransform.localScale.x : 1f;
        /// <summary>Tamaño de la foto en el lienzo (px de referencia).</summary>
        public Vector2 TamanoFoto => capa != null ? capa.rectTransform.sizeDelta : Vector2.zero;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            ultima = -1;
            azar = null;
        }

        /// <summary>Crea la imagen de carga bajo <paramref name="padre"/>; null si no encontró el frame en Resources.</summary>
        public static FotosCarga Crear(Transform padre)
        {
            if (azar == null)
            {
                azar = new System.Random(System.Environment.TickCount);
            }
            int elegido = ReglaFotosCarga.ElegirSinRepetir(FramesCarga.Cantidad, ultima, azar);
            Texture2D textura = elegido >= 0 ? Resources.Load<Texture2D>(FramesCarga.Ruta(elegido)) : null;
            if (textura == null)
            {
                Debug.LogWarning("[UI] Carga: no está el frame " + (elegido >= 0 ? FramesCarga.Ruta(elegido) : "(ninguno)") + " en Resources; la carga queda en negro.");
                return null;
            }
            ultima = elegido;
            RectTransform raiz = UIInicio.Nodo(padre, "FotosCarga");
            UIInicio.Estirar(raiz);
            raiz.gameObject.AddComponent<RectMask2D>();
            FotosCarga fotos = raiz.gameObject.AddComponent<FotosCarga>();
            fotos.indice = elegido;
            fotos.foto = textura;
            GameObject go = new GameObject("Foto", typeof(RectTransform));
            go.transform.SetParent(raiz, false);
            fotos.capa = go.AddComponent<RawImage>();
            fotos.capa.raycastTarget = false;
            fotos.capa.texture = textura;
            fotos.capa.color = Color.white;
            fotos.Ajustar();
            return fotos;
        }

        private void Update()
        {
            // El lienzo puede cambiar de tamaño (ventana, o en VR al pasar a la pantalla de cine): se reajusta solo si cambió.
            Ajustar();
        }

        private void Ajustar()
        {
            if (capa == null || foto == null)
            {
                return;
            }
            Rect r = ((RectTransform)transform).rect;
            Vector2 pantalla = new Vector2(r.width > 1f ? r.width : AnchoPantalla, r.height > 1f ? r.height : AltoPantalla);
            if (pantalla == pantallaAplicada)
            {
                return;
            }
            pantallaAplicada = pantalla;
            Vector2 tamano = ReglaFotosCarga.TamanoCubierto(foto.width, foto.height, pantalla.x, pantalla.y);
            UIInicio.Punto(capa.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, tamano);
            capa.rectTransform.localScale = Vector3.one;
        }
    }
}

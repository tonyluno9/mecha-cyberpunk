using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// T-1316: las ilustraciones de la pantalla de carga (L01-L06, en Resources/Carga/) a pantalla completa, una tras otra como en GTA:
    /// cada una se acerca y se desliza despacio (zoom y paneo) y la siguiente entra con un fundido. Empieza en una al azar.
    /// La regla del movimiento está en <see cref="ReglaFotosCarga"/>.
    /// </summary>
    public class FotosCarga : MonoBehaviour
    {
        public const string CarpetaRecursos = "Carga";
        private const float AnchoPantalla = 1920f;
        private const float AltoPantalla = 1080f;

        private Texture2D[] fotos;
        private RawImage[] capas;
        private int primera;
        private float tiempo;

        /// <summary>Cuántas fotos encontró.</summary>
        public int Cantidad => fotos != null ? fotos.Length : 0;
        /// <summary>La foto que domina ahora (la de arriba cuando termina su fundido).</summary>
        public Texture FotoActual => capas != null && capas[0].texture != null ? capas[0].texture : null;
        /// <summary>Escala de la foto actual (crece despacio).</summary>
        public float EscalaActual => capas != null ? capas[0].rectTransform.localScale.x : 1f;

        /// <summary>Crea la capa de fotos bajo <paramref name="padre"/>; null si no hay fotos en Resources/Carga.</summary>
        public static FotosCarga Crear(Transform padre)
        {
            Texture2D[] encontradas = Resources.LoadAll<Texture2D>(CarpetaRecursos);
            if (encontradas == null || encontradas.Length == 0)
            {
                return null;
            }
            System.Array.Sort(encontradas, (a, b) => string.CompareOrdinal(a.name, b.name));
            RectTransform raiz = UIInicio.Nodo(padre, "FotosCarga");
            UIInicio.Estirar(raiz);
            raiz.gameObject.AddComponent<RectMask2D>();
            FotosCarga fotos = raiz.gameObject.AddComponent<FotosCarga>();
            fotos.fotos = encontradas;
            fotos.primera = Random.Range(0, encontradas.Length);
            fotos.capas = new RawImage[2];
            for (int i = 1; i >= 0; i--)
            {
                GameObject go = new GameObject("Foto" + i, typeof(RectTransform));
                go.transform.SetParent(raiz, false);
                RawImage capa = go.AddComponent<RawImage>();
                capa.raycastTarget = false;
                fotos.capas[i] = capa;
            }
            fotos.Aplicar();
            return fotos;
        }

        private void Update()
        {
            tiempo += Time.unscaledDeltaTime;
            Aplicar();
        }

        private void Aplicar()
        {
            ReglaFotosCarga.Estado e = ReglaFotosCarga.Calcular(tiempo, fotos.Length, primera);
            // Capa 1 (abajo): la foto que sigue su movimiento; capa 0 (arriba): la que entra con el fundido.
            Poner(capas[1], e.Anterior, e.TiempoAnterior, 1f);
            Poner(capas[0], e.Actual, e.TiempoActual, e.AlfaActual);
        }

        private void Poner(RawImage capa, int indice, float t, float alfa)
        {
            if (indice < 0)
            {
                capa.enabled = false;
                return;
            }
            capa.enabled = true;
            capa.texture = fotos[indice];
            capa.color = new Color(1f, 1f, 1f, alfa);
            RectTransform r = capa.rectTransform;
            // Cubre la pantalla conservando la proporción de la foto.
            float aspecto = (float)fotos[indice].width / fotos[indice].height;
            Vector2 tamano = aspecto > AnchoPantalla / AltoPantalla
                ? new Vector2(AltoPantalla * aspecto, AltoPantalla)
                : new Vector2(AnchoPantalla, AnchoPantalla / aspecto);
            UIInicio.Punto(r, new Vector2(0.5f, 0.5f), ReglaFotosCarga.Paneo(indice, t), tamano);
            float escala = ReglaFotosCarga.Escala(t);
            r.localScale = new Vector3(escala, escala, 1f);
        }
    }
}

using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Iconos chicos de los implantes instalados (T-430), arriba a la derecha bajo el calor: un cuadro por espacio con su letra
    /// (O ojos, B brazos, P piernas, S sinapsis) y el nivel. Un espacio sin implante no se dibuja. La crea <see cref="HudAPie"/>.
    /// </summary>
    public class HudImplantes : MonoBehaviour
    {
        public const int OrdenCanvas = 10;
        private const float Lado = 40f;
        private const float Separacion = 6f;
        private const int Espacios = 4;
        private static readonly string[] Letras = { "O", "B", "P", "S" };

        private Canvas canvas;
        private GameObject[] cuadros;
        private Text[] textos;
        private readonly int[] niveles = new int[Espacios];

        /// <summary>Cuántos iconos están a la vista.</summary>
        public int Visibles
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Espacios; i++)
                {
                    n += cuadros != null && cuadros[i].activeSelf ? 1 : 0;
                }
                return n;
            }
        }

        /// <summary>El texto del icono del espacio ("O1"), o vacío si no se ve.</summary>
        public string TextoIcono(EspacioImplante espacio)
        {
            int i = (int)espacio;
            return cuadros != null && cuadros[i].activeSelf ? textos[i].text : string.Empty;
        }

        private void Awake()
        {
            canvas = KitUI.CrearCanvas(transform, "CanvasImplantes", OrdenCanvas);
            RanuraHud ranura = DisposicionHud.Implantes;
            RectTransform fila = (RectTransform)new GameObject("Fila", typeof(RectTransform)).transform;
            fila.SetParent(canvas.transform, false);
            KitUI.Colocar(fila, ranura, Espacios * Lado + (Espacios - 1) * Separacion);
            cuadros = new GameObject[Espacios];
            textos = new Text[Espacios];
            for (int i = 0; i < Espacios; i++)
            {
                Image fondo = KitUI.CrearImagen(fila, "Implante" + i, KitUI.Estilo.Fondo);
                KitUI.Colocar(fondo.rectTransform, new Vector2(1f, 1f), new Vector2(-i * (Lado + Separacion), 0f), new Vector2(Lado, Lado));
                KitUI.CrearBorde(fondo.rectTransform, KitUI.Estilo.BordePanel, KitUI.Estilo.Turquesa);
                Text texto = KitUI.CrearTexto(fondo.transform, "Letra", RolTexto.Hud, TextAnchor.MiddleCenter);
                texto.color = KitUI.Estilo.Turquesa;
                cuadros[i] = fondo.gameObject;
                textos[i] = texto;
                cuadros[i].SetActive(false);
            }
            for (int i = 0; i < Espacios; i++)
            {
                niveles[i] = -1;
            }
        }

        private void Update()
        {
            IImplantes implantes = Registro<IImplantes>.Actual;
            for (int i = 0; i < Espacios; i++)
            {
                EspacioImplante espacio = (EspacioImplante)i;
                int nivel = implantes != null ? implantes.Nivel(espacio) : 0;
                if (nivel == niveles[i])
                {
                    continue;
                }
                niveles[i] = nivel;
                cuadros[i].SetActive(nivel > 0);
                textos[i].text = Letras[i] + nivel;
            }
        }
    }
}

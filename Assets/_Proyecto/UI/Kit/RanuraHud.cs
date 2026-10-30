using UnityEngine;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>Lugar de un elemento del HUD: esquina, distancia al borde de esa esquina y alto (px a 1920x1080).</summary>
    public readonly struct RanuraHud
    {
        public EsquinaHud Esquina { get; }

        /// <summary>Distancia (px) del borde de arriba o de abajo al lado más cercano del elemento.</summary>
        public float DesdeBorde { get; }

        /// <summary>Alto del elemento (px).</summary>
        public float Alto { get; }

        /// <summary>Distancia (px) del borde izquierdo o derecho (0 en el centro).</summary>
        public float DesdeLado { get; }

        public RanuraHud(EsquinaHud esquina, float desdeBorde, float alto, float desdeLado)
        {
            Esquina = esquina;
            DesdeBorde = desdeBorde;
            Alto = alto;
            DesdeLado = desdeLado;
        }

        /// <summary>Ancla (y pivote) de la esquina.</summary>
        public Vector2 Ancla
        {
            get
            {
                switch (Esquina)
                {
                    case EsquinaHud.ArribaIzquierda: return new Vector2(0f, 1f);
                    case EsquinaHud.ArribaCentro: return new Vector2(0.5f, 1f);
                    case EsquinaHud.ArribaDerecha: return new Vector2(1f, 1f);
                    case EsquinaHud.AbajoIzquierda: return new Vector2(0f, 0f);
                    case EsquinaHud.AbajoCentro: return new Vector2(0.5f, 0f);
                    default: return new Vector2(1f, 0f);
                }
            }
        }

        /// <summary>anchoredPosition con el pivote en el ancla.</summary>
        public Vector2 Posicion
        {
            get
            {
                Vector2 a = Ancla;
                float x = a.x < 0.25f ? DesdeLado : a.x > 0.75f ? -DesdeLado : 0f;
                float y = a.y > 0.5f ? -DesdeBorde : DesdeBorde;
                return new Vector2(x, y);
            }
        }

        /// <summary>Distancia (px) del borde al lado lejano del elemento.</summary>
        public float Fin => DesdeBorde + Alto;
    }
}

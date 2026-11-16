using System.Collections.Generic;
using UnityEngine;

namespace MechaCyberpunk.UI.Rueda
{
    /// <summary>
    /// T-1010: los sprites de la rueda M30 hechos en código, en blanco para teñirlos con <c>Image.color</c>: el sector de anillo (relleno, y contorno con
    /// un brillo suave por fuera) apuntando hacia arriba, y el octágono del centro. Un texel por px de referencia; el pivote del sprite del sector es el
    /// centro de la rueda, así que cada sector se coloca en el centro y solo se gira.
    /// </summary>
    public static class DibujoRueda
    {
        /// <summary>Un par de sprites: el relleno y el contorno con brillo.</summary>
        public readonly struct Par
        {
            public readonly Sprite Relleno;
            public readonly Sprite Borde;

            public Par(Sprite relleno, Sprite borde)
            {
                Relleno = relleno;
                Borde = borde;
            }
        }

        private static readonly Dictionary<long, Par> sectores = new Dictionary<long, Par>();
        private static readonly Dictionary<long, Par> octagonos = new Dictionary<long, Par>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            sectores.Clear();
            octagonos.Clear();
        }

        /// <summary>
        /// El sector de anillo entre <paramref name="radioInterior"/> y <paramref name="radioExterior"/> (px) que cubre <paramref name="grados"/>, centrado
        /// hacia arriba, con un hueco de <paramref name="hueco"/> px con los vecinos, un contorno de <paramref name="grosor"/> px y un brillo de
        /// <paramref name="brillo"/> px por fuera.
        /// </summary>
        public static Par Sector(float radioInterior, float radioExterior, float grados, float hueco, float grosor, float brillo)
        {
            long clave = Clave(radioInterior, radioExterior, grados, hueco, grosor, brillo);
            if (sectores.TryGetValue(clave, out Par par) && par.Relleno != null)
            {
                return par;
            }
            float mitad = grados * 0.5f * Mathf.Deg2Rad;
            float margen = brillo + 2f;
            float xMax = radioExterior * Mathf.Sin(mitad) + margen;
            float yMin = radioInterior * Mathf.Cos(mitad) - margen;
            float yMax = radioExterior + margen;
            int ancho = Mathf.CeilToInt(xMax * 2f);
            int alto = Mathf.CeilToInt(yMax - yMin);
            Vector2 centro = new Vector2(ancho * 0.5f, -yMin);
            par = Pintar(ancho, alto, centro, p =>
            {
                float r = p.magnitude;
                float angulo = Mathf.Abs(Mathf.Atan2(p.x, p.y));
                float radial = Mathf.Min(r - radioInterior, radioExterior - r);
                float lateral = r * Mathf.Sin(Mathf.Clamp(mitad - angulo, -Mathf.PI * 0.5f, Mathf.PI * 0.5f)) - hueco * 0.5f;
                return Mathf.Min(radial, lateral);
            }, grosor, brillo);
            sectores[clave] = par;
            return par;
        }

        /// <summary>Octágono regular de lados planos arriba y abajo, con <paramref name="apotema"/> px del centro a cada lado.</summary>
        public static Par Octagono(float apotema, float grosor, float brillo)
        {
            long clave = Clave(apotema, grosor, brillo, 0f, 0f, 0f);
            if (octagonos.TryGetValue(clave, out Par par) && par.Relleno != null)
            {
                return par;
            }
            int lado = Mathf.CeilToInt((apotema / Mathf.Cos(22.5f * Mathf.Deg2Rad) + brillo + 2f) * 2f);
            Vector2 centro = new Vector2(lado * 0.5f, lado * 0.5f);
            par = Pintar(lado, lado, centro, p =>
            {
                float mayor = float.MinValue;
                for (int k = 0; k < 8; k++)
                {
                    float a = k * 45f * Mathf.Deg2Rad;
                    mayor = Mathf.Max(mayor, p.x * Mathf.Sin(a) + p.y * Mathf.Cos(a));
                }
                return apotema - mayor;
            }, grosor, brillo);
            octagonos[clave] = par;
            return par;
        }

        // distancia(p) > 0 dentro de la figura (px); p es relativo al centro de la rueda.
        private static Par Pintar(int ancho, int alto, Vector2 centro, System.Func<Vector2, float> distancia, float grosor, float brillo)
        {
            Texture2D relleno = Nueva(ancho, alto, "RuedaRelleno");
            Texture2D borde = Nueva(ancho, alto, "RuedaBorde");
            Color32[] r = new Color32[ancho * alto];
            Color32[] b = new Color32[ancho * alto];
            float mitadGrosor = grosor * 0.5f;
            for (int y = 0; y < alto; y++)
            {
                for (int x = 0; x < ancho; x++)
                {
                    float d = distancia(new Vector2(x + 0.5f, y + 0.5f) - centro);
                    float dentro = Mathf.Clamp01(d + 0.5f);
                    float linea = Mathf.Clamp01(mitadGrosor + 0.5f - Mathf.Abs(d - mitadGrosor));
                    float halo = d < 0f && brillo > 0f ? 0.45f * Mathf.Exp(d / (brillo * 0.45f)) : 0f;
                    int i = y * ancho + x;
                    r[i] = new Color32(255, 255, 255, (byte)(dentro * 255f));
                    b[i] = new Color32(255, 255, 255, (byte)(Mathf.Max(linea, halo) * 255f));
                }
            }
            relleno.SetPixels32(r);
            relleno.Apply(false, true);
            borde.SetPixels32(b);
            borde.Apply(false, true);
            Vector2 pivote = new Vector2(centro.x / ancho, centro.y / alto);
            return new Par(Sprite.Create(relleno, new Rect(0f, 0f, ancho, alto), pivote, 100f),
                Sprite.Create(borde, new Rect(0f, 0f, ancho, alto), pivote, 100f));
        }

        private static Texture2D Nueva(int ancho, int alto, string nombre)
        {
            return new Texture2D(ancho, alto, TextureFormat.RGBA32, false)
            {
                name = nombre,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
        }

        private static long Clave(float a, float b, float c, float d, float e, float f)
        {
            unchecked
            {
                long h = 17;
                foreach (float v in new[] { a, b, c, d, e, f })
                {
                    h = h * 31 + Mathf.RoundToInt(v * 100f);
                }
                return h;
            }
        }
    }
}

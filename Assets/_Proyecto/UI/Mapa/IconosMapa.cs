using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Mapa
{
    /// <summary>
    /// Las piezas que comparten el minimapa y el mapa grande (T-409), con sprites hechos por código (sin assets): círculo, anillo, engrane y
    /// casita. Enemigos en rojo (#FF3333, el jefe más grande), aliados del color de tu pandilla, El Mercado con Ƀ, I+D+I con +, el CIC con un
    /// engrane, la casa con una casita y el punto marcado como un anillo turquesa.
    /// </summary>
    public static class IconosMapa
    {
        public const string SimboloMercado = "Ƀ";
        public const string SimboloClinica = "+";
        private const int LadoTextura = 128;
        private const float GrosorAnillo = 0.22f;
        private const float GrosorBordeMinimapa = 0.025f;
        private const int DientesEngrane = 8;
        private const float RadioDientes = 0.95f;
        private const float RadioCuerpoEngrane = 0.7f;
        private const float RadioHuecoEngrane = 0.3f;
        private const float GrosorContornoPunto = 2f;
        private const float FraccionSimbolo = 0.62f;
        private const float FraccionPuntoMarcado = 0.35f;
        private const float FraccionEnemigo = 0.45f;
        private const float FraccionJefe = 0.75f;
        private const float BaseTecho = 0.15f;
        private const float AnchoTecho = 0.95f;
        private const float AnchoCuerpo = 0.6f;
        private const float PisoCuerpo = -0.85f;

        private static Sprite circulo;
        private static Sprite anillo;
        private static Sprite bordeMinimapa;
        private static Sprite engrane;
        private static Sprite casa;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            circulo = null;
            anillo = null;
            bordeMinimapa = null;
            engrane = null;
            casa = null;
        }

        public static Sprite Circulo => circulo != null ? circulo : circulo = CrearSprite("Mapa_Circulo", (x, y) => x * x + y * y <= 1f);

        public static Sprite Anillo => anillo != null ? anillo : anillo = CrearSprite("Mapa_Anillo", (x, y) => EnAnillo(x, y, GrosorAnillo));

        /// <summary>El borde del minimapa: un anillo delgado (2.5 % del radio, unos 3 px en 260).</summary>
        public static Sprite BordeMinimapa => bordeMinimapa != null ? bordeMinimapa : bordeMinimapa = CrearSprite("Mapa_Borde", (x, y) => EnAnillo(x, y, GrosorBordeMinimapa));

        public static Sprite Engrane => engrane != null ? engrane : engrane = CrearSprite("Mapa_Engrane", EnEngrane);

        public static Sprite Casa => casa != null ? casa : casa = CrearSprite("Mapa_Casa", EnCasa);

        /// <summary>Tamaño (px) de una marca de ese tipo, con <paramref name="lado"/> el de los iconos de lugar.</summary>
        public static float Tamano(TipoMarcaMapa tipo, float lado)
        {
            switch (tipo)
            {
                case TipoMarcaMapa.Enemigo: return lado * FraccionEnemigo;
                case TipoMarcaMapa.Jefe: return lado * FraccionJefe;
                default: return lado;
            }
        }

        /// <summary>La marca de un enemigo, un jefe o un lugar, centrada en su punto. Devuelve la Image de fondo (la roja en enemigos y jefes).</summary>
        public static Image Crear(Transform padre, TipoMarcaMapa tipo, float lado)
        {
            EstiloCyberfix e = KitUI.Estilo;
            bool esEnemigo = tipo == TipoMarcaMapa.Enemigo || tipo == TipoMarcaMapa.Jefe;
            Image fondo = Pieza(padre, "Marca_" + tipo, Circulo, esEnemigo ? e.Enemigo : e.Contorno, Tamano(tipo, lado));
            Outline borde = fondo.gameObject.AddComponent<Outline>();
            borde.effectColor = esEnemigo ? e.Contorno : e.Texto;
            borde.effectDistance = Vector2.one * (esEnemigo ? GrosorContornoPunto : 1f);
            float simbolo = lado * FraccionSimbolo;
            switch (tipo)
            {
                case TipoMarcaMapa.Mercado:
                    Simbolo(fondo.transform, SimboloMercado, simbolo);
                    break;
                case TipoMarcaMapa.Clinica:
                    Simbolo(fondo.transform, SimboloClinica, simbolo);
                    break;
                case TipoMarcaMapa.Cic:
                    Pieza(fondo.transform, "Engrane", Engrane, e.Texto, simbolo);
                    break;
                case TipoMarcaMapa.Casa:
                    Pieza(fondo.transform, "Casa", Casa, e.Texto, simbolo);
                    break;
            }
            return fondo;
        }

        /// <summary>Un aliado: un círculo del color dado con contorno oscuro.</summary>
        public static Image CrearAliado(Transform padre, float lado, Color color)
        {
            Image punto = Pieza(padre, "Aliado", Circulo, color, Tamano(TipoMarcaMapa.Enemigo, lado));
            Outline borde = punto.gameObject.AddComponent<Outline>();
            borde.effectColor = KitUI.Estilo.Contorno;
            borde.effectDistance = Vector2.one * GrosorContornoPunto;
            return punto;
        }

        /// <summary>El punto marcado: un anillo turquesa con un punto en medio.</summary>
        public static Image CrearPuntoMarcado(Transform padre, float lado)
        {
            Image aro = Pieza(padre, "PuntoMarcado", Anillo, KitUI.Estilo.Turquesa, lado);
            Outline borde = aro.gameObject.AddComponent<Outline>();
            borde.effectColor = KitUI.Estilo.Contorno;
            borde.effectDistance = Vector2.one * GrosorContornoPunto;
            Pieza(aro.transform, "Centro", Circulo, KitUI.Estilo.Turquesa, lado * FraccionPuntoMarcado);
            return aro;
        }

        /// <summary>Una imagen con sprite, centrada en su padre.</summary>
        public static Image Pieza(Transform padre, string nombre, Sprite sprite, Color color, float lado)
        {
            Image imagen = KitUI.CrearImagen(padre, nombre, color);
            imagen.sprite = sprite;
            KitUI.Colocar(imagen.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * lado);
            return imagen;
        }

        private static void Simbolo(Transform padre, string simbolo, float lado)
        {
            Text texto = KitUI.CrearTexto(padre, "Simbolo", RolTexto.Minimo, TextAnchor.MiddleCenter);
            texto.fontSize = Mathf.Max(KitUI.Estilo.Tamano(RolTexto.Minimo), Mathf.RoundToInt(lado));
            texto.fontStyle = FontStyle.Bold;
            texto.text = simbolo;
        }

        private static bool EnAnillo(float x, float y, float grosor)
        {
            float d2 = x * x + y * y;
            float dentro = 1f - grosor;
            return d2 <= 1f && d2 >= dentro * dentro;
        }

        private static bool EnEngrane(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y);
            if (d < RadioHuecoEngrane)
            {
                return false;
            }
            if (d <= RadioCuerpoEngrane)
            {
                return true;
            }
            // Dientes: la mitad de cada uno de los 8 sectores llega hasta el radio de los dientes.
            float sector = (Mathf.Atan2(y, x) / (2f * Mathf.PI) + 1f) * DientesEngrane;
            return d <= RadioDientes && sector - Mathf.Floor(sector) < 0.5f;
        }

        // Techo (triángulo) arriba y cuerpo (rectángulo) abajo, en el cuadrado de -1 a 1.
        private static bool EnCasa(float x, float y)
        {
            if (y >= BaseTecho)
            {
                return y <= 1f && Mathf.Abs(x) <= (1f - y) / (1f - BaseTecho) * AnchoTecho;
            }
            return y >= PisoCuerpo && Mathf.Abs(x) <= AnchoCuerpo;
        }

        // Textura blanca con alfa según la figura (x e y de -1 a 1 en el centro de cada píxel).
        private static Sprite CrearSprite(string nombre, Func<float, float, bool> dentro)
        {
            Texture2D textura = new Texture2D(LadoTextura, LadoTextura, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = nombre,
                hideFlags = HideFlags.DontSave,
            };
            Color32[] pixeles = new Color32[LadoTextura * LadoTextura];
            for (int j = 0; j < LadoTextura; j++)
            {
                for (int i = 0; i < LadoTextura; i++)
                {
                    float x = (i + 0.5f) / LadoTextura * 2f - 1f;
                    float y = (j + 0.5f) / LadoTextura * 2f - 1f;
                    pixeles[j * LadoTextura + i] = dentro(x, y) ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply();
            Sprite sprite = Sprite.Create(textura, new Rect(0f, 0f, LadoTextura, LadoTextura), new Vector2(0.5f, 0.5f));
            sprite.name = nombre;
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}

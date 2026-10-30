using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.UI.Kit
{
    /// <summary>
    /// El kit de UI de CYBERFIX (T-408, D-tony-238): crea canvas, panel, texto, botón, deslizador, conmutador, selector y las piezas del
    /// marcador con el <see cref="EstiloCyberfix"/> de la guía (docs/diseno/ui-cyberfix.md). Solo usa API de tiempo de juego, así que sirve
    /// igual desde un creador de editor que desde un MonoBehaviour. Sin estilo fijado usa uno con los valores de la guía.
    /// </summary>
    public static class KitUI
    {
        public const float AnchoReferencia = 1920f;
        public const float AltoReferencia = 1080f;
        public const float MezclaAnchoAlto = 0.5f;
        private const int LadoTriangulo = 64;
        private const float RaizDeDos = 1.41421356f;
        private const float GradosRombo = 45f;

        private static EstiloCyberfix estilo;
        private static Sprite triangulo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar()
        {
            estilo = null;
            triangulo = null;
        }

        /// <summary>El estilo en uso. Lo fija <see cref="FijarEstilo"/> (el HUD lo hace con EstiloCyberfix_Base.asset).</summary>
        public static EstiloCyberfix Estilo
        {
            get
            {
                if (estilo == null)
                {
                    estilo = ScriptableObject.CreateInstance<EstiloCyberfix>();
                    estilo.name = "EstiloCyberfix_Guia";
                    estilo.hideFlags = HideFlags.DontSave;
                }
                return estilo;
            }
        }

        public static void FijarEstilo(EstiloCyberfix nuevo)
        {
            if (nuevo != null)
            {
                estilo = nuevo;
            }
        }

        /// <summary>
        /// Canvas propio con su orden de dibujo (HUD 10, marcador 15, historia 20, minimapa 25, mapa 40...). Si el padre ya está en un canvas,
        /// queda anidado con su propio orden; si no, es Screen Space Overlay con el CanvasScaler de la guía (1920x1080, mezcla 0.5).
        /// </summary>
        public static Canvas CrearCanvas(Transform padre, string nombre, int orden)
        {
            bool anidado = padre != null && padre.GetComponentInParent<Canvas>(true) != null;
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Canvas canvas = objeto.AddComponent<Canvas>();
            if (anidado)
            {
                Estirar((RectTransform)objeto.transform);
                canvas.overrideSorting = true;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler escala = objeto.AddComponent<CanvasScaler>();
                escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                escala.referenceResolution = new Vector2(AnchoReferencia, AltoReferencia);
                escala.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                escala.matchWidthOrHeight = MezclaAnchoAlto;
            }
            canvas.sortingOrder = orden;
            objeto.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Panel negro translúcido con borde turquesa al 60 % de 2 px, del tamaño dado y centrado en su padre.</summary>
        public static RectTransform CrearPanel(Transform padre, string nombre, Vector2 tamano)
        {
            Image fondo = CrearImagen(padre, nombre, Estilo.Fondo);
            RectTransform rect = fondo.rectTransform;
            Colocar(rect, new Vector2(0.5f, 0.5f), Vector2.zero, tamano);
            CrearBorde(rect, Estilo.BordePanel, Estilo.BordeDePanel);
            return rect;
        }

        /// <summary>Texto del rol dado con la fuente, el color de texto y la sombra de la guía. Estira a su padre hasta que lo coloques.</summary>
        public static Text CrearTexto(Transform padre, string nombre, RolTexto rol, TextAnchor alineacion = TextAnchor.MiddleLeft)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Estirar((RectTransform)objeto.transform);
            Text texto = objeto.AddComponent<Text>();
            texto.font = Estilo.Fuente;
            texto.fontSize = Estilo.Tamano(rol);
            texto.alignment = alineacion;
            texto.color = Estilo.Texto;
            texto.horizontalOverflow = HorizontalWrapMode.Overflow;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.raycastTarget = false;
            texto.text = string.Empty;
            Sombrear(texto);
            return texto;
        }

        /// <summary>Le pone (o corrige) la sombra de la guía a un texto que ya existe.</summary>
        public static void Sombrear(Text texto)
        {
            Shadow sombra = texto.GetComponent<Shadow>();
            if (sombra == null)
            {
                sombra = texto.gameObject.AddComponent<Shadow>();
            }
            sombra.effectColor = Estilo.Sombra;
            sombra.effectDistance = Estilo.DistanciaSombra;
        }

        /// <summary>Aplica fuente, tamaño y sombra de la guía a un texto que ya existe (los del HUD hecho antes del kit).</summary>
        public static void AplicarEstilo(Text texto, RolTexto rol)
        {
            texto.font = Estilo.Fuente;
            texto.fontSize = Estilo.Tamano(rol);
            Sombrear(texto);
        }

        public static Image CrearImagen(Transform padre, string nombre, Color color)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            Image imagen = objeto.AddComponent<Image>();
            imagen.color = color;
            imagen.raycastTarget = false;
            return imagen;
        }

        /// <summary>
        /// Un rombo (cuadrado girado 45°) de <paramref name="diagonal"/> px de punta a punta, sobre un contorno oscuro del estilo para que se
        /// lea sobre cualquier color. Devuelve el rombo de color; el contorno es su padre, que es lo que se mueve.
        /// </summary>
        public static Image CrearRombo(Transform padre, string nombre, float diagonal, Color color)
        {
            Image contorno = CrearImagen(padre, nombre, Estilo.Contorno);
            RectTransform rectContorno = contorno.rectTransform;
            rectContorno.anchorMin = rectContorno.anchorMax = rectContorno.pivot = new Vector2(0.5f, 0.5f);
            rectContorno.sizeDelta = Vector2.one * ((diagonal + 2f * Estilo.GrosorContorno) / RaizDeDos);
            rectContorno.localRotation = Quaternion.Euler(0f, 0f, GradosRombo);
            Image rombo = CrearImagen(rectContorno, "Color", color);
            RectTransform rect = rombo.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * (diagonal / RaizDeDos);
            return rombo;
        }

        /// <summary>Una imagen a pantalla completa del color de fondo de pantalla (detrás de pausa y mapa).</summary>
        public static Image CrearFondoPantalla(Transform padre, string nombre)
        {
            Image fondo = CrearImagen(padre, nombre, Estilo.FondoPantalla);
            Estirar(fondo.rectTransform);
            return fondo;
        }

        /// <summary>Un triángulo con la punta hacia arriba (hacia +Y local), de <paramref name="lado"/> px, sobre un contorno oscuro (su padre).</summary>
        public static Image CrearTriangulo(Transform padre, string nombre, float lado, Color color)
        {
            Image contorno = CrearImagen(padre, nombre, Estilo.Contorno);
            contorno.sprite = SpriteTriangulo();
            RectTransform rectContorno = contorno.rectTransform;
            rectContorno.anchorMin = rectContorno.anchorMax = rectContorno.pivot = new Vector2(0.5f, 0.5f);
            rectContorno.sizeDelta = Vector2.one * (lado + 3f * Estilo.GrosorContorno);
            Image imagen = CrearImagen(rectContorno, "Color", color);
            imagen.sprite = SpriteTriangulo();
            RectTransform rect = imagen.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * lado;
            return imagen;
        }

        /// <summary>Botón de menú de 440x64 con los estados de la guía. <paramref name="accion"/> puede ser null.</summary>
        public static BotonCyberfix CrearBoton(Transform padre, string texto, Action accion)
        {
            BotonCyberfix boton = CrearBase<BotonCyberfix>(padre, "Boton_" + texto, Estilo.TamanoBotonMenu, texto, false);
            if (accion != null)
            {
                boton.AlPulsar.AddListener(() => accion());
            }
            return boton;
        }

        /// <summary>Fila de 760x56 con la etiqueta a la izquierda y el valor turquesa a la derecha; ← → y la rueda lo mueven.</summary>
        public static DeslizadorCyberfix CrearDeslizador(Transform padre, string etiqueta, float minimo, float maximo, float paso, float valor, Action<float> alCambiar)
        {
            DeslizadorCyberfix d = CrearBase<DeslizadorCyberfix>(padre, "Deslizador_" + etiqueta, Estilo.TamanoFila, etiqueta, true);
            d.Configurar(minimo, maximo, paso, valor);
            if (alCambiar != null)
            {
                d.CambioValor += alCambiar;
            }
            return d;
        }

        /// <summary>Fila de SÍ / NO; Enter, Espacio, ← → o el clic la cambian.</summary>
        public static ConmutadorCyberfix CrearConmutador(Transform padre, string etiqueta, bool valor, Action<bool> alCambiar)
        {
            ConmutadorCyberfix c = CrearBase<ConmutadorCyberfix>(padre, "Conmutador_" + etiqueta, Estilo.TamanoFila, etiqueta, true);
            c.Configurar(valor);
            if (alCambiar != null)
            {
                c.CambioValor += alCambiar;
            }
            return c;
        }

        /// <summary>Fila que elige una de <paramref name="opciones"/> con ← → o la rueda (sin dar la vuelta).</summary>
        public static SelectorCyberfix CrearSelector(Transform padre, string etiqueta, string[] opciones, int indice, Action<int> alCambiar)
        {
            SelectorCyberfix s = CrearBase<SelectorCyberfix>(padre, "Selector_" + etiqueta, Estilo.TamanoFila, etiqueta, true);
            s.Configurar(opciones, indice);
            if (alCambiar != null)
            {
                s.CambioValor += alCambiar;
            }
            return s;
        }

        /// <summary>T-413: fila que se escribe con el teclado (solo letras y espacios, hasta <paramref name="largoMaximo"/>).</summary>
        public static CampoTextoCyberfix CrearCampo(Transform padre, string etiqueta, int largoMaximo, string vacio)
        {
            CampoTextoCyberfix c = CrearBase<CampoTextoCyberfix>(padre, "Campo_" + etiqueta, Estilo.TamanoFila, etiqueta, true);
            c.Configurar(largoMaximo, vacio);
            return c;
        }

        /// <summary>Un solo EventSystem con InputSystemUIInputModule (el Input Manager viejo está apagado). Lo crea si falta.</summary>
        public static EventSystem AsegurarEventSystem()
        {
            EventSystem sistema = EventSystem.current != null ? EventSystem.current : Object.FindFirstObjectByType<EventSystem>();
            if (sistema == null)
            {
                sistema = new GameObject("EventSystem").AddComponent<EventSystem>();
            }
            StandaloneInputModule viejo = sistema.GetComponent<StandaloneInputModule>();
            if (viejo != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(viejo);
                }
                else
                {
                    Object.DestroyImmediate(viejo);
                }
            }
            InputSystemUIInputModule modulo = sistema.GetComponent<InputSystemUIInputModule>();
            if (modulo == null)
            {
                modulo = sistema.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            // T-410: un clic fuera de los botones no quita el elegido (el teclado sigue sabiendo dónde está).
            modulo.deselectOnBackgroundClick = false;
            return sistema;
        }

        /// <summary>Ancla, pivote, posición y tamaño de una vez; el pivote es el ancla (una esquina se mide desde esa esquina).</summary>
        public static void Colocar(RectTransform rect, Vector2 ancla, Vector2 posicion, Vector2 tamano)
        {
            rect.anchorMin = ancla;
            rect.anchorMax = ancla;
            rect.pivot = ancla;
            rect.anchoredPosition = posicion;
            rect.sizeDelta = tamano;
        }

        /// <summary>Coloca un elemento del HUD en su ranura de <see cref="DisposicionHud"/>.</summary>
        public static void Colocar(RectTransform rect, RanuraHud ranura, float ancho)
        {
            Colocar(rect, ranura.Ancla, ranura.Posicion, new Vector2(ancho, ranura.Alto));
        }

        public static void Estirar(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Cuatro tiras de <paramref name="grosor"/> px en el borde de <paramref name="rect"/>, bajo un objeto "Borde".</summary>
        public static GameObject CrearBorde(RectTransform rect, float grosor, Color color)
        {
            GameObject borde = new GameObject("Borde", typeof(RectTransform));
            borde.transform.SetParent(rect, false);
            Estirar((RectTransform)borde.transform);
            Tira(borde.transform, "Arriba", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, grosor), color);
            Tira(borde.transform, "Abajo", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, grosor), color);
            Tira(borde.transform, "Izquierda", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(grosor, 0f), color);
            Tira(borde.transform, "Derecha", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(grosor, 0f), color);
            return borde;
        }

        private static void Tira(Transform padre, string nombre, Vector2 anclaMin, Vector2 anclaMax, Vector2 tamano, Color color)
        {
            RectTransform rect = CrearImagen(padre, nombre, color).rectTransform;
            rect.anchorMin = anclaMin;
            rect.anchorMax = anclaMax;
            rect.pivot = new Vector2(Mathf.Approximately(anclaMin.x, anclaMax.x) ? anclaMin.x : 0.5f, Mathf.Approximately(anclaMin.y, anclaMax.y) ? anclaMin.y : 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = tamano;
        }

        // Fondo, borde de elegido, etiqueta y (en las filas) el valor turquesa a la derecha.
        private static T CrearBase<T>(Transform padre, string nombre, Vector2 tamano, string etiqueta, bool conValor) where T : BotonCyberfix
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            RectTransform rect = (RectTransform)objeto.transform;
            Colocar(rect, new Vector2(0.5f, 0.5f), Vector2.zero, tamano);
            Image fondo = objeto.AddComponent<Image>();
            fondo.color = Estilo.Fondo;
            fondo.raycastTarget = true;
            GameObject borde = CrearBorde(rect, Estilo.BordeElegido, Estilo.Naranja);
            Text texto = CrearTexto(rect, "Etiqueta", RolTexto.Boton, TextAnchor.MiddleLeft);
            texto.rectTransform.offsetMin = new Vector2(Estilo.Relleno, 0f);
            texto.rectTransform.offsetMax = new Vector2(-Estilo.Relleno, 0f);
            Text valor = null;
            if (conValor)
            {
                valor = CrearTexto(rect, "Valor", RolTexto.Boton, TextAnchor.MiddleRight);
                valor.rectTransform.offsetMin = new Vector2(Estilo.Relleno, 0f);
                valor.rectTransform.offsetMax = new Vector2(-Estilo.Relleno, 0f);
                valor.color = Estilo.Turquesa;
            }
            T control = objeto.AddComponent<T>();
            control.Iniciar(etiqueta, texto, valor, fondo, borde);
            return control;
        }

        /// <summary>Sprite de triángulo hecho por código (sin asset), con la punta arriba. Se comparte.</summary>
        public static Sprite SpriteTriangulo()
        {
            if (triangulo != null)
            {
                return triangulo;
            }
            Texture2D textura = new Texture2D(LadoTriangulo, LadoTriangulo, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "KitUI_Triangulo",
                hideFlags = HideFlags.DontSave,
            };
            Color32[] pixeles = new Color32[LadoTriangulo * LadoTriangulo];
            for (int y = 0; y < LadoTriangulo; y++)
            {
                float ancho = (1f - (float)y / (LadoTriangulo - 1)) * LadoTriangulo;
                float inicio = (LadoTriangulo - ancho) / 2f;
                for (int x = 0; x < LadoTriangulo; x++)
                {
                    bool dentro = x >= inicio && x <= inicio + ancho;
                    pixeles[y * LadoTriangulo + x] = dentro ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            textura.SetPixels32(pixeles);
            textura.Apply();
            triangulo = Sprite.Create(textura, new Rect(0f, 0f, LadoTriangulo, LadoTriangulo), new Vector2(0.5f, 0.5f));
            triangulo.hideFlags = HideFlags.DontSave;
            return triangulo;
        }
    }
}

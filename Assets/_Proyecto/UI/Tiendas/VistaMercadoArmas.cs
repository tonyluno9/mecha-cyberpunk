using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.UI.Armas;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Tiendas
{
    /// <summary>
    /// T-1012: la vista de El Mercado como el mockup M32_TiendaArmas.png, a la derecha de la lista de <see cref="PantallaTienda"/>: el nombre del arma en
    /// grande, su holograma 3D (el mismo de Tino, <see cref="VistaHologramaArma"/>) girando sobre una rejilla en perspectiva, las cuatro barras
    /// (<see cref="ReglaVistaMercado"/>) y el botón COMPRAR. También pone el dinero en grande sobre la lista y el título "EL MERCADO" al centro.
    /// La agrega <see cref="PantallaTienda"/> cuando la tienda vende armas; con otras tiendas se queda apagada.
    /// </summary>
    public class VistaMercadoArmas : MonoBehaviour
    {
        // Medidas en px de referencia (1920 x 1080), tomadas del mockup.
        public const float AnchoLista = 660f;
        public const float AltoDinero = 96f;
        private const float AltoNombre = 80f;
        private const float AnchoHolo = 1000f;
        private const float AltoHolo = 360f;
        private const float AltoRejilla = 150f;
        private const float AnchoBarra = 520f;
        private const float AltoBarra = 10f;
        private const float SeparacionBarras = 70f;
        private const float AnchoBoton = 290f;
        private const float AltoBoton = 78f;
        private const float GrosorMarca = 3f;
        private const float LargoMarca = 26f;
        private const string TextoComprar = "COMPRAR";
        private const string TextoSinDinero = "SIN DINERO";

        private RectTransform raiz;
        private RectTransform derecha;
        private Text titulo;
        private Text dinero;
        private Text nombre;
        private RawImage imagenHolo;
        private Text botonTexto;
        private Image[] botonBorde;
        private readonly Text[] etiquetas = new Text[ReglaVistaMercado.Barras];
        private readonly Text[] valores = new Text[ReglaVistaMercado.Barras];
        private readonly RectTransform[] rellenos = new RectTransform[ReglaVistaMercado.Barras];
        private VistaHologramaArma vista;
        private ConfigMenuArmas configArmas;
        private ArmaAPie armaMostrada = (ArmaAPie)(-1);
        private Action alComprar;
        private bool alcanza;

        /// <summary>True mientras la vista se está dibujando (la tienda vende armas).</summary>
        public bool Activa => raiz != null && raiz.gameObject.activeSelf;

        /// <summary>El nombre grande del arma elegida.</summary>
        public string NombreMostrado => nombre != null ? nombre.text : string.Empty;

        /// <summary>El dinero que se ve sobre la lista.</summary>
        public string DineroMostrado => dinero != null ? dinero.text : string.Empty;

        /// <summary>El texto del botón (COMPRAR o SIN DINERO).</summary>
        public string BotonMostrado => botonTexto != null ? botonTexto.text : string.Empty;

        /// <summary>El arma del holograma, o -1 si no hay.</summary>
        public ArmaAPie ArmaMostrada => armaMostrada;

        /// <summary>El holograma (para las pruebas).</summary>
        public VistaHologramaArma Holograma => vista;

        /// <summary>El número de la barra i (DAÑO, CARGADOR, CADENCIA, ALCANCE).</summary>
        public string ValorBarra(int i) => valores[i] != null ? valores[i].text : string.Empty;

        /// <summary>Cuánto se llena la barra i (0 a 1).</summary>
        public float LlenadoBarra(int i) => rellenos[i] != null ? rellenos[i].anchorMax.x : 0f;

        /// <summary>Arma la vista dentro de <paramref name="padre"/> (una vez). <paramref name="comprar"/> es lo que hace el botón.</summary>
        public void Armar(Transform padre, ConfigMenuArmas armas, Action comprar)
        {
            configArmas = armas;
            alComprar = comprar;
            if (raiz != null)
            {
                return;
            }
            raiz = TrazoH10.Nodo(padre, "VistaMercado");
            TrazoH10.Estirar(raiz);

            titulo = TrazoH10.Etiqueta(raiz, "TituloMercado", string.Empty, TrazoH10.TamanoTitulo, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
            TrazoH10.Colocar(titulo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(900f, 80f));

            dinero = TrazoH10.Etiqueta(raiz, "DineroGrande", string.Empty, TrazoH10.TamanoTitulo, TrazoH10.Turquesa);
            TrazoH10.Colocar(dinero.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(TrazoH10.MargenPantalla + 40f, -(VistaPantallaArriba() + 6f)), new Vector2(AnchoLista - 60f, AltoDinero));

            // La mitad derecha: nombre, holograma sobre la rejilla, barras y botón.
            float x0 = TrazoH10.MargenPantalla + AnchoLista + TrazoH10.SeparacionModulo;
            derecha = TrazoH10.Nodo(raiz, "Derecha");
            TrazoH10.Colocar(derecha, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            derecha.offsetMin = new Vector2(x0, VistaPantallaAbajo());
            derecha.offsetMax = new Vector2(-TrazoH10.MargenPantalla, -VistaPantallaArriba());
            Image separador = TrazoH10.Recta(derecha, "Separador", TrazoH10.TurquesaTenue, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(-TrazoH10.SeparacionModulo * 0.5f, 0f), new Vector2(TrazoH10.GrosorLinea, -40f));
            separador.raycastTarget = false;

            nombre = TrazoH10.Etiqueta(derecha, "NombreArma", string.Empty, TrazoH10.TamanoTitulo, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
            TrazoH10.Colocar(nombre.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(0f, AltoNombre));

            RectTransform rejilla = TrazoH10.Nodo(derecha, "Rejilla");
            TrazoH10.Colocar(rejilla, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(AltoNombre + AltoHolo - 70f)), new Vector2(AnchoHolo, AltoRejilla));
            Image piso = rejilla.gameObject.AddComponent<Image>();
            piso.sprite = SpriteRejilla();
            piso.color = TrazoH10.ConAlfa(TrazoH10.Turquesa, 0.75f);
            piso.raycastTarget = false;

            RectTransform areaHolo = TrazoH10.Nodo(derecha, "Holograma");
            TrazoH10.Colocar(areaHolo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -AltoNombre), new Vector2(AnchoHolo, AltoHolo));
            imagenHolo = new GameObject("Imagen", typeof(RectTransform)).AddComponent<RawImage>();
            imagenHolo.transform.SetParent(areaHolo, false);
            TrazoH10.Estirar(imagenHolo.rectTransform);
            imagenHolo.raycastTarget = false;
            imagenHolo.enabled = false;

            // Las cuatro barras, en dos columnas como el mockup.
            float yBarras = AltoNombre + AltoHolo + AltoRejilla - 70f + 40f;
            for (int i = 0; i < ReglaVistaMercado.Barras; i++)
            {
                int columna = i % 2;
                int fila = i / 2;
                Vector2 sitio = new Vector2(columna == 0 ? 0f : AnchoBarra + 60f, -(yBarras + fila * SeparacionBarras));
                Barra(i, sitio);
            }

            // El botón COMPRAR, abajo a la derecha, con marco amarillo.
            RectTransform boton = TrazoH10.Nodo(derecha, "BotonComprar");
            TrazoH10.Colocar(boton, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 10f), new Vector2(AnchoBoton, AltoBoton));
            Image fondoBoton = TrazoH10.Imagen(boton, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.85f), TrazoH10.Relleno);
            TrazoH10.Estirar(fondoBoton.rectTransform);
            fondoBoton.raycastTarget = true;
            Image lineaBoton = TrazoH10.Imagen(boton, "Linea", TrazoH10.Amarillo, TrazoH10.Linea);
            TrazoH10.Estirar(lineaBoton.rectTransform);
            botonBorde = new[] { lineaBoton };
            botonTexto = TrazoH10.Etiqueta(boton, "Texto", TextoComprar, TrazoH10.TamanoCifra, TrazoH10.Amarillo, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(botonTexto.rectTransform);
            EventTrigger disparador = boton.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry clic = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            clic.callback.AddListener(_ => alComprar?.Invoke());
            disparador.triggers.Add(clic);

            Marcas(raiz, TrazoH10.Turquesa);
            VestidoH10.Fuente(raiz);
            vista = gameObject.AddComponent<VistaHologramaArma>();
            raiz.gameObject.SetActive(false);
        }

        // La cabecera H10 ocupa la parte de arriba; los paneles empiezan bajo ella y terminan sobre la barra de teclas.
        private static float VistaPantallaArriba() => VestidoH10.AltoCabecera + TrazoH10.SeparacionModulo;

        private static float VistaPantallaAbajo() => VestidoH10.AltoPie + TrazoH10.SeparacionModulo;

        private void Barra(int i, Vector2 sitio)
        {
            etiquetas[i] = TrazoH10.Etiqueta(derecha, "Barra" + i, ReglaVistaMercado.Etiquetas[i], TrazoH10.TamanoSeccion, TrazoH10.Texto);
            TrazoH10.Colocar(etiquetas[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), sitio, new Vector2(AnchoBarra, 40f));
            valores[i] = TrazoH10.Etiqueta(derecha, "Valor" + i, string.Empty, TrazoH10.TamanoSeccion, TrazoH10.Texto, TextAnchor.MiddleRight);
            TrazoH10.Colocar(valores[i].rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), sitio, new Vector2(AnchoBarra, 40f));
            Image pista = TrazoH10.Imagen(derecha, "Pista" + i, TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.9f));
            TrazoH10.Colocar(pista.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), sitio + new Vector2(0f, -44f), new Vector2(AnchoBarra, AltoBarra));
            Image marco = TrazoH10.Imagen(pista.rectTransform, "Marco", TrazoH10.TurquesaTenue, TrazoH10.Linea);
            TrazoH10.Estirar(marco.rectTransform);
            Image relleno = TrazoH10.Imagen(pista.rectTransform, "Relleno", TrazoH10.Turquesa);
            rellenos[i] = relleno.rectTransform;
            rellenos[i].anchorMin = Vector2.zero;
            rellenos[i].anchorMax = new Vector2(0f, 1f);
            rellenos[i].offsetMin = new Vector2(2f, 2f);
            rellenos[i].offsetMax = new Vector2(-2f, -2f);
        }

        /// <summary>Pone el dinero grande arriba de la lista (dentro de su panel), como el mockup.</summary>
        public void ColocarDinero(RectTransform panelLista)
        {
            if (dinero == null || dinero.transform.parent == panelLista)
            {
                return;
            }
            dinero.transform.SetParent(panelLista, false);
            TrazoH10.Colocar(dinero.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-48f, AltoDinero));
        }

        /// <summary>Enciende o apaga la vista. Al apagarla quita el holograma y libera su textura.</summary>
        public void Encender(bool encender)
        {
            if (raiz == null)
            {
                return;
            }
            raiz.gameObject.SetActive(encender);
            if (dinero != null)
            {
                dinero.gameObject.SetActive(encender);
            }
            if (!encender)
            {
                Soltar();
            }
        }

        /// <summary>Quita el holograma (al cerrar la tienda).</summary>
        public void Soltar()
        {
            if (vista != null)
            {
                vista.Ocultar();
            }
            if (imagenHolo != null)
            {
                imagenHolo.enabled = false;
            }
            armaMostrada = (ArmaAPie)(-1);
        }

        /// <summary>Pinta el título, el dinero y el objeto elegido (un arma, su mejora o sus balas).</summary>
        public void Pintar(string tituloTienda, DatosObjeto objeto, int precio, ICartera cartera)
        {
            if (!Activa)
            {
                return;
            }
            titulo.text = tituloTienda;
            dinero.text = FormatoDinero.Texto(cartera != null ? cartera.Dinero : 0);
            alcanza = cartera != null && objeto != null && cartera.PuedePagar(precio);
            botonTexto.text = objeto == null || alcanza ? TextoComprar : TextoSinDinero;
            Color color = objeto == null || alcanza ? TrazoH10.Amarillo : TrazoH10.Coral;
            botonTexto.color = color;
            foreach (Image b in botonBorde)
            {
                b.color = color;
            }

            DatosArma datos = objeto as DatosArma;
            ArmaAPie arma = datos != null ? datos.Arma : (ArmaAPie)(-1);
            if (objeto is DatosMunicionArma balas)
            {
                arma = balas.Arma;
                ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
                datos = arsenal != null ? arsenal.Datos(arma) : null;
            }
            nombre.text = objeto != null ? objeto.Nombre.ToUpperInvariant() : string.Empty;
            BarraMercado[] barras = ReglaVistaMercado.De(datos);
            for (int i = 0; i < barras.Length; i++)
            {
                valores[i].text = datos != null ? barras[i].Valor.ToString() : "—";
                rellenos[i].anchorMax = new Vector2(barras[i].Llenado, 1f);
            }
            MostrarHolograma(arma);
        }

        private void MostrarHolograma(ArmaAPie arma)
        {
            if (arma == armaMostrada)
            {
                return;
            }
            armaMostrada = arma;
            EntradaArmaMenu entrada = null;
            if (configArmas != null)
            {
                foreach (EntradaArmaMenu e in configArmas.Armas)
                {
                    if (e != null && e.Arma == arma)
                    {
                        entrada = e;
                    }
                }
            }
            if (entrada == null)
            {
                vista.Ocultar();
                imagenHolo.enabled = false;
                return;
            }
            // La textura es de la proporción de la config: se recorta arriba y abajo para no deformar el arma.
            float proporcion = (AltoHolo / AnchoHolo) / ((float)configArmas.AltoTextura / configArmas.AnchoTextura);
            vista.FijarFraccionAltoVisible(proporcion);
            vista.Mostrar(entrada, configArmas);
            imagenHolo.texture = vista.Textura;
            imagenHolo.uvRect = new Rect(0f, (1f - proporcion) * 0.5f, 1f, proporcion);
            imagenHolo.material = configArmas.MaterialAditivoUI;
            imagenHolo.enabled = vista.Textura != null;
        }

        // Escuadras en las cuatro esquinas de la pantalla, como el mockup.
        private static void Marcas(RectTransform area, Color color)
        {
            Vector2[] esquinas = { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            foreach (Vector2 e in esquinas)
            {
                Vector2 dentro = new Vector2(e.x < 0.5f ? 14f : -14f, e.y < 0.5f ? 14f : -14f);
                Image h = TrazoH10.Imagen(area, "Escuadra", color);
                TrazoH10.Colocar(h.rectTransform, e, e, e, dentro, new Vector2(LargoMarca * 2f, GrosorMarca));
                Image v = TrazoH10.Imagen(area, "Escuadra", color);
                TrazoH10.Colocar(v.rectTransform, e, e, e, dentro, new Vector2(GrosorMarca, LargoMarca * 2f));
            }
        }

        // La rejilla del piso en perspectiva: líneas que se juntan al fondo y travesaños más juntos lejos. Blanca, para teñirla.
        private static Sprite rejilla;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Limpiar() => rejilla = null;

        private static Sprite SpriteRejilla()
        {
            if (rejilla != null)
            {
                return rejilla;
            }
            const int ancho = 1000;
            const int alto = 150;
            const int lineas = 22;
            const int travesanos = 7;
            const float fondo = 0.62f; // el borde de atrás mide esta parte del de adelante
            Texture2D t = new Texture2D(ancho, alto, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "RejillaMercado" };
            Color32[] px = new Color32[ancho * alto];
            for (int y = 0; y < alto; y++)
            {
                float v = y / (float)(alto - 1); // 0 adelante (abajo), 1 atrás (arriba)
                float escala = Mathf.Lerp(1f, fondo, v);
                for (int x = 0; x < ancho; x++)
                {
                    float u = ((x + 0.5f) / ancho - 0.5f) / escala + 0.5f; // posición en el piso
                    float a = 0f;
                    if (u >= 0f && u <= 1f)
                    {
                        float enLinea = Mathf.Abs(u * lineas - Mathf.Round(u * lineas)) * ancho * escala / lineas;
                        a = Mathf.Max(a, Mathf.Clamp01(1.4f - enLinea));
                        // Los travesaños se acercan con la profundidad.
                        float profundidad = Mathf.Pow(v, 0.7f) * travesanos;
                        float enTravesano = Mathf.Abs(profundidad - Mathf.Round(profundidad)) * alto / travesanos * 1.4f;
                        a = Mathf.Max(a, Mathf.Clamp01(1.4f - enTravesano));
                        a *= Mathf.Lerp(1f, 0.35f, v);
                    }
                    px[y * ancho + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            rejilla = Sprite.Create(t, new Rect(0f, 0f, ancho, alto), new Vector2(0.5f, 0.5f), 100f);
            return rejilla;
        }

        private void OnDestroy()
        {
            Soltar();
        }
    }
}

using System;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T-1722 (T79): la pantalla del campo de tiro, en el estilo H10 (<see cref="TrazoH10"/>): el menú de entrenamiento (arma, tipo de objetivo, cuántos,
    /// distancia, REINICIAR OBJETIVOS y SALIR) y el marcador de daño y precisión de la esquina. Esc cierra el menú.
    /// </summary>
    public class MenuCampoTiro : MonoBehaviour
    {
        public const int OrdenCanvas = 125;
        private static readonly Vector2 TamanoPanel = new Vector2(1100f, 620f);
        private static readonly Vector2 TamanoBoton = new Vector2(160f, 46f);
        private const float XEtiqueta = 30f;
        private const float XBotones = 220f;
        private const float Paso = 170f;
        private const float AltoPaso = 56f;
        private const float AnchoAccion = 340f;
        private static readonly Vector2 TamanoMarcador = new Vector2(620f, 90f);
        private static readonly Vector2 MargenMarcador = new Vector2(0f, -24f); // arriba al centro: a la izquierda se encimaba con la vida del HUD

        private static readonly ArmaAPie[] Armas =
        {
            ArmaAPie.Punos, ArmaAPie.Pistola, ArmaAPie.Revolver, ArmaAPie.Subfusil, ArmaAPie.Escopeta,
            ArmaAPie.Fusil, ArmaAPie.Sniper, ArmaAPie.Lanzagranadas, ArmaAPie.Granada, ArmaAPie.RiflePlasma
        };
        private static readonly string[] NombresArmas =
        {
            "PUÑOS", "PISTOLA", "REVÓLVER", "SUBFUSIL", "ESCOPETA", "FUSIL", "FRANCOTIRADOR", "LANZAGRANADAS", "GRANADA", "RIFLE PLASMA"
        };
        private static readonly TipoObjetivoTiro[] Tipos = (TipoObjetivoTiro[])Enum.GetValues(typeof(TipoObjetivoTiro));
        private static readonly DistanciaTiro[] Distancias = (DistanciaTiro[])Enum.GetValues(typeof(DistanciaTiro));

        private sealed class Casilla
        {
            public Image Fondo;
            public Image Linea;
            public Text Texto;
        }

        private GameObject raizMenu;
        private GameObject raizMarcador;
        private Text marcador;
        private readonly List<Casilla> casillasArma = new List<Casilla>();
        private readonly List<Casilla> casillasTipo = new List<Casilla>();
        private readonly List<Casilla> casillasCuantos = new List<Casilla>();
        private readonly List<Casilla> casillasDistancia = new List<Casilla>();
        private Text resumen;

        /// <summary>True si el panel del menú está visible.</summary>
        public bool Visible => raizMenu != null && raizMenu.activeSelf;

        private void Awake()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasCampoTiro", OrdenCanvas);
            RectTransform zonaMarcador = TrazoH10.Nodo(canvas.transform, "Marcador");
            TrazoH10.Colocar(zonaMarcador, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), MargenMarcador, TamanoMarcador);
            Image fondoMarcador = TrazoH10.Imagen(zonaMarcador, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPantalla, 0.9f), TrazoH10.Relleno);
            TrazoH10.Estirar(fondoMarcador.rectTransform);
            marcador = TrazoH10.Etiqueta(zonaMarcador, "Texto", string.Empty, TrazoH10.TamanoDato, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(marcador.rectTransform);
            raizMarcador = zonaMarcador.gameObject;
            raizMarcador.SetActive(false);

            Image panel = TrazoH10.Panel(canvas.transform, "Menu", TrazoH10.Turquesa, TrazoH10.ConAlfa(TrazoH10.FondoPantalla, 0.96f)); // la sala es blanca: fondo casi opaco
            TrazoH10.Colocar(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, TamanoPanel);
            raizMenu = panel.gameObject;
            ConstruirMenu(panel.transform);
            VestidoH10.Fuente(canvas.transform);
            raizMenu.SetActive(false);
        }

        private void ConstruirMenu(Transform padre)
        {
            Text titulo = TrazoH10.Etiqueta(padre, "Titulo", "CAMPO DE TIRO  -  ENTRENAMIENTO", TrazoH10.TamanoSeccion, TrazoH10.Amarillo, TextAnchor.MiddleLeft);
            Poner(titulo.rectTransform, XEtiqueta, 14f, 800f, 40f);
            Text ayuda = TrazoH10.Etiqueta(padre, "Ayuda", "ESC CIERRA EL MENÚ", TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.MiddleRight);
            Poner(ayuda.rectTransform, TamanoPanel.x - XEtiqueta - 300f, 14f, 300f, 40f);

            float y = 70f;
            Etiqueta(padre, "ARMA", y);
            for (int i = 0; i < Armas.Length; i++)
            {
                ArmaAPie arma = Armas[i];
                casillasArma.Add(Boton(padre, NombresArmas[i], XBotones + (i % 5) * Paso, y + (i / 5) * AltoPaso, TamanoBoton, () => CampoDeTiro.ElegirArma(arma)));
            }
            y += AltoPaso * 2f + 14f;
            Etiqueta(padre, "OBJETIVO", y);
            for (int i = 0; i < Tipos.Length; i++)
            {
                TipoObjetivoTiro tipo = Tipos[i];
                casillasTipo.Add(Boton(padre, ReglaCampoTiro.Nombre(tipo), XBotones + i * Paso, y, TamanoBoton, () => CampoDeTiro.PonerObjetivos(tipo, CampoDeTiro.CuantosElegidos, CampoDeTiro.DistanciaElegida)));
            }
            y += AltoPaso + 14f;
            Etiqueta(padre, "CUÁNTOS", y);
            for (int n = ReglaCampoTiro.MinimoObjetivos; n <= ReglaCampoTiro.MaximoObjetivos; n++)
            {
                int cuantos = n;
                casillasCuantos.Add(Boton(padre, n.ToString(), XBotones + (n - ReglaCampoTiro.MinimoObjetivos) * Paso, y, TamanoBoton, () => CampoDeTiro.PonerObjetivos(CampoDeTiro.TipoElegido, cuantos, CampoDeTiro.DistanciaElegida)));
            }
            y += AltoPaso + 14f;
            Etiqueta(padre, "DISTANCIA", y);
            string[] nombresDistancia = { "CERCA", "MEDIA", "LEJOS" };
            for (int i = 0; i < Distancias.Length; i++)
            {
                DistanciaTiro distancia = Distancias[i];
                casillasDistancia.Add(Boton(padre, nombresDistancia[i], XBotones + i * Paso, y, TamanoBoton, () => CampoDeTiro.PonerObjetivos(CampoDeTiro.TipoElegido, CampoDeTiro.CuantosElegidos, distancia)));
            }
            y += AltoPaso + 30f;
            Boton(padre, "REINICIAR OBJETIVOS", XBotones, y, new Vector2(AnchoAccion, TamanoBoton.y), CampoDeTiro.ReiniciarObjetivos);
            Casilla salir = Boton(padre, "SALIR DEL CAMPO", XBotones + AnchoAccion + 20f, y, new Vector2(AnchoAccion, TamanoBoton.y), () => CampoDeTiro.Salir());
            salir.Linea.color = TrazoH10.Coral;
            salir.Texto.color = TrazoH10.Coral;
            y += AltoPaso + 20f;
            resumen = TrazoH10.Etiqueta(padre, "Resumen", string.Empty, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.UpperLeft);
            Poner(resumen.rectTransform, XEtiqueta, y, TamanoPanel.x - XEtiqueta * 2f, 70f);
        }

        private static void Poner(RectTransform rect, float x, float y, float ancho, float alto)
        {
            TrazoH10.Colocar(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(ancho, alto));
        }

        private static void Etiqueta(Transform padre, string texto, float y)
        {
            Text etiqueta = TrazoH10.Etiqueta(padre, "Etiqueta_" + texto, texto, TrazoH10.TamanoDato, TrazoH10.TextoTenue, TextAnchor.MiddleLeft);
            Poner(etiqueta.rectTransform, XEtiqueta, y, XBotones - XEtiqueta, TamanoBoton.y);
        }

        private static Casilla Boton(Transform padre, string etiqueta, float x, float y, Vector2 tamano, Action alClic)
        {
            RectTransform caja = TrazoH10.Nodo(padre, "Boton_" + etiqueta);
            Poner(caja, x, y, tamano.x, tamano.y);
            Casilla casilla = new Casilla();
            casilla.Fondo = TrazoH10.Imagen(caja, "Fondo", TrazoH10.ConAlfa(TrazoH10.FondoPanel, 0.9f), TrazoH10.Relleno);
            TrazoH10.Estirar(casilla.Fondo.rectTransform);
            casilla.Fondo.raycastTarget = true;
            casilla.Linea = TrazoH10.Imagen(caja, "Linea", TrazoH10.TurquesaTenue, TrazoH10.Linea);
            TrazoH10.Estirar(casilla.Linea.rectTransform);
            casilla.Texto = TrazoH10.Etiqueta(caja, "Texto", etiqueta, TrazoH10.TamanoDato, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(casilla.Texto.rectTransform);
            Button boton = caja.gameObject.AddComponent<Button>();
            boton.transition = Selectable.Transition.None;
            boton.targetGraphic = casilla.Fondo;
            boton.onClick.AddListener(() => alClic());
            return casilla;
        }

        private static void Pintar(Casilla casilla, bool elegida)
        {
            casilla.Texto.color = elegida ? TrazoH10.Amarillo : TrazoH10.Turquesa;
            casilla.Linea.color = elegida ? TrazoH10.Amarillo : TrazoH10.TurquesaTenue;
        }

        public void Mostrar(bool visible)
        {
            if (raizMenu == null)
            {
                return;
            }
            raizMenu.SetActive(visible);
            if (visible)
            {
                Refrescar();
            }
        }

        public void MostrarMarcador(bool visible)
        {
            if (raizMarcador != null)
            {
                raizMarcador.SetActive(visible);
            }
        }

        public void PonerMarcador(string texto)
        {
            if (marcador != null && marcador.text != texto)
            {
                marcador.text = texto;
            }
        }

        /// <summary>Repinta lo elegido (amarillo) según el estado de <see cref="CampoDeTiro"/>.</summary>
        public void Refrescar()
        {
            if (resumen == null)
            {
                return;
            }
            for (int i = 0; i < casillasArma.Count; i++)
            {
                Pintar(casillasArma[i], Armas[i] == CampoDeTiro.ArmaElegida);
            }
            for (int i = 0; i < casillasTipo.Count; i++)
            {
                Pintar(casillasTipo[i], Tipos[i] == CampoDeTiro.TipoElegido);
            }
            for (int i = 0; i < casillasCuantos.Count; i++)
            {
                Pintar(casillasCuantos[i], ReglaCampoTiro.MinimoObjetivos + i == CampoDeTiro.CuantosElegidos);
            }
            for (int i = 0; i < casillasDistancia.Count; i++)
            {
                Pintar(casillasDistancia[i], Distancias[i] == CampoDeTiro.DistanciaElegida);
            }
            resumen.text = CampoDeTiro.TextoMarcador().Replace("\n", "     ");
        }

        private void Update()
        {
            if (CampoDeTiro.MenuAbierto && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CampoDeTiro.CerrarMenu();
            }
        }
    }
}

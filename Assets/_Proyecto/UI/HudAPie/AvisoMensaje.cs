using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.UI.Misiones;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// El aviso de un mensaje nuevo del teléfono (T-449): arriba a la derecha sale "MENSAJE DE EL DOC · T PARA LEER" con el texto un rato; si
    /// llegan varios se ven uno tras otro. Se esconde con el juego en pausa o en otra pantalla. Lo agrega <see cref="HudAPie"/> y construye su canvas.
    /// </summary>
    public class AvisoMensaje : MonoBehaviour
    {
        public const int OrdenCanvas = 13;
        public const string TextoEncabezado = "MENSAJE DE ";
        public const string TextoLeer = "T PARA LEER";

        // T-701: compacto y de línea fina como H10, bajo el objetivo, y se esconde a los pocos segundos (constantes: el prefab guardaba 6 s y 780 px).
        private const float SegundosVisible = 4.5f;
        private const float AnchoCuadro = 560f;
        private const float RellenoCuadro = 12f;
        private const int TamanoLetra = 22;
        private const float BajoElObjetivo = 52f;

        private readonly Queue<string> cola = new Queue<string>();
        private ServicioMensajes servicio;
        private GameObject cuadro;
        private RectTransform panelRect;
        private Text texto;
        private string textoPlano = string.Empty;
        private float fin;
        private PantallaTelefono telefono;
        private float proximaBusquedaTelefono;

        public bool Visible => cuadro != null && cuadro.activeSelf;
        public string Texto => textoPlano;
        public int EnCola => cola.Count;

        private void Awake()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasAvisoMensaje", OrdenCanvas);
            // T-701: un recuadro de línea fina turquesa sobre un fondo oscuro suave, bajo el objetivo (que va bajo el minimapa), pegado al margen derecho.
            Image fondo = KitUI.CrearImagen(canvas.transform, "Aviso", new Color(0.02f, 0.07f, 0.11f, 0.6f));
            panelRect = fondo.rectTransform;
            KitUI.Colocar(panelRect, new Vector2(1f, 1f), new Vector2(-DisposicionHud.MargenH10, -(DisposicionHud.Minimapa.Fin + BajoElObjetivo)), new Vector2(AnchoCuadro, 80f));
            panelRect.pivot = new Vector2(1f, 1f);
            Color turquesa = KitUI.Estilo.Turquesa;
            KitUI.CrearBorde(panelRect, 1.5f, new Color(turquesa.r, turquesa.g, turquesa.b, 0.8f));
            cuadro = panelRect.gameObject;
            texto = KitUI.CrearTexto(panelRect, "Texto", RolTexto.Ayuda, TextAnchor.UpperLeft);
            ConfigHudH10 estilo = ConfigHudH10.Actual;
            if (estilo != null && estilo.Fuente != null)
            {
                texto.font = estilo.Fuente;
            }
            texto.fontSize = TamanoLetra;
            texto.supportRichText = true;
            texto.horizontalOverflow = HorizontalWrapMode.Wrap;
            texto.verticalOverflow = VerticalWrapMode.Overflow;
            texto.color = new Color(0.93f, 0.97f, 0.97f, 1f);
            KitUI.Estirar(texto.rectTransform);
            texto.rectTransform.offsetMin = new Vector2(RellenoCuadro, RellenoCuadro * 0.6f);
            texto.rectTransform.offsetMax = new Vector2(-RellenoCuadro, -RellenoCuadro * 0.6f);
            cuadro.SetActive(false);
        }

        private void OnDisable()
        {
            Desvincular();
        }

        private void Update()
        {
            ServicioMensajes actual = Registro<ServicioMensajes>.Actual;
            if (!ReferenceEquals(actual, servicio))
            {
                Desvincular();
                servicio = actual;
                if (servicio != null)
                {
                    servicio.Recibido += AlRecibir;
                }
            }
            if (telefono == null && Time.unscaledTime >= proximaBusquedaTelefono)
            {
                proximaBusquedaTelefono = Time.unscaledTime + 1f;
                telefono = FindFirstObjectByType<PantallaTelefono>(FindObjectsInactive.Include);
            }
            // Con el teléfono abierto el mensaje se lee ahí: el aviso se esconde para no encimarse.
            bool puedeVerse = !Pausa.Activa && PilaPantallas.Vacia && (telefono == null || !telefono.Abierto);
            if (Visible && (!puedeVerse || Time.unscaledTime >= fin))
            {
                cuadro.SetActive(false);
            }
            if (!Visible && puedeVerse && cola.Count > 0)
            {
                textoPlano = cola.Dequeue();
                texto.text = Formatear(textoPlano);
                // El alto sigue al texto: una línea de encabezado y lo que mida el mensaje.
                Canvas.ForceUpdateCanvases();
                panelRect.sizeDelta = new Vector2(AnchoCuadro, texto.preferredHeight + RellenoCuadro * 1.2f);
                Sonidos.Reproducir(ColaAvisos.SonidoTexto);
                fin = Time.unscaledTime + SegundosVisible;
                cuadro.SetActive(true);
            }
        }

        // La primera línea (quién escribe y la tecla) va chica y turquesa; el mensaje, en blanco.
        private static string Formatear(string plano)
        {
            int salto = plano.IndexOf((char)10);
            if (salto < 0)
            {
                return plano;
            }
            return "<size=" + (TamanoLetra - 3) + "><color=#4DEBE6>" + plano.Substring(0, salto) + "</color></size>" + plano.Substring(salto);
        }

        private void AlRecibir(string id)
        {
            if (ReglaMensajes.Buscar(id, out ReglaMensajes.Mensaje mensaje))
            {
                cola.Enqueue(TextoEncabezado + mensaje.Remitente + " · " + TextoLeer + "\n" + mensaje.Texto);
            }
        }

        private void Desvincular()
        {
            if (servicio != null)
            {
                servicio.Recibido -= AlRecibir;
                servicio = null;
            }
        }
    }
}

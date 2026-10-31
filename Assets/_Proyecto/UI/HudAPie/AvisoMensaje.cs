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

        [Tooltip("Segundos que se ve cada mensaje (s).")]
        [Min(1f)] [SerializeField] private float segundosVisible = 6f;
        [Tooltip("Tamaño del cuadro (px a 1920x1080).")]
        [SerializeField] private Vector2 tamanoCuadro = new Vector2(780f, 120f);
        [Tooltip("Separación del borde de arriba y de la derecha (px).")]
        [Min(0f)] [SerializeField] private float margen = 40f;

        private readonly Queue<string> cola = new Queue<string>();
        private ServicioMensajes servicio;
        private GameObject cuadro;
        private Text texto;
        private float fin;
        private PantallaTelefono telefono;
        private float proximaBusquedaTelefono;

        public bool Visible => cuadro != null && cuadro.activeSelf;
        public string Texto => texto != null ? texto.text : string.Empty;
        public int EnCola => cola.Count;

        private void Awake()
        {
            Canvas canvas = KitUI.CrearCanvas(transform, "CanvasAvisoMensaje", OrdenCanvas);
            RectTransform panel = KitUI.CrearPanel(canvas.transform, "Aviso", tamanoCuadro);
            KitUI.Colocar(panel, new Vector2(1f, 1f), new Vector2(-margen - tamanoCuadro.x / 2f, -margen - tamanoCuadro.y / 2f), tamanoCuadro);
            cuadro = panel.gameObject;
            texto = KitUI.CrearTexto(panel, "Texto", RolTexto.Ayuda, TextAnchor.MiddleLeft);
            KitUI.Estirar(texto.rectTransform);
            texto.color = KitUI.Estilo.Naranja;
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
                texto.text = cola.Dequeue();
                fin = Time.unscaledTime + segundosVisible;
                cuadro.SetActive(true);
            }
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

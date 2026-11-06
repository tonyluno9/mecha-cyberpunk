using System;
using MechaCyberpunk.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    public enum TipoFilaOpcion
    {
        Selector,
        Deslizador,
        Conmutador,
    }

    /// <summary>
    /// Una fila de las opciones: etiqueta a la izquierda y, a la derecha, un selector de opciones (← →), un deslizador (← → o clic y
    /// arrastre en la barra) o un interruptor (← →, Enter o clic). Se elige como cualquier botón (flechas arriba y abajo, control o ratón)
    /// y avisa <see cref="Cambio"/> con el valor nuevo ya aplicado a la fila.
    /// </summary>
    public class FilaOpcion : Selectable, IPointerClickHandler, IDragHandler, IPointerDownHandler, ISubmitHandler
    {
        private const float AnchoControl = 470f;
        private const float AnchoPista = 300f;
        private const float AltoPista = 8f;
        private const float DiametroPerilla = 26f;
        private const float AnchoInterruptor = 96f;
        private const float AltoInterruptor = 40f;
        private const float AlfaFondo = 0.38f;
        private const float SegundosEncender = 0.14f;

        [SerializeField] private TipoFilaOpcion tipo;
        [SerializeField] private Text etiqueta;
        [SerializeField] private Text valorTexto;
        [SerializeField] private Image fondo;
        [SerializeField] private Image barra;
        [SerializeField] private Image pista;
        [SerializeField] private Image relleno;
        [SerializeField] private Image perilla;
        [SerializeField] private Image flechaIzquierda;
        [SerializeField] private Image flechaDerecha;
        [SerializeField] private Image interruptor;
        [SerializeField] private Color acento = new Color(0.10f, 0.92f, 0.98f, 1f);

        private string[] opciones = new string[0];
        private int indice;
        private float valor;
        private float minimo;
        private float maximo = 1f;
        private float paso = 0.05f;
        private bool activo;
        private bool dar;
        private float encendido;
        private bool elegido;
        private Func<float, string> formato;

        public TipoFilaOpcion Tipo => tipo;
        public int Indice => indice;
        public float Valor => valor;
        public bool Activo => activo;
        public string Etiqueta => etiqueta != null ? etiqueta.text : string.Empty;
        public string TextoValor => valorTexto != null ? valorTexto.text : string.Empty;
        public bool EstaElegida => elegido;

        /// <summary>Se dispara cuando el usuario cambia el valor (no cuando el menú lo fija por código).</summary>
        public event Action<FilaOpcion> Cambio;

        public static FilaOpcion CrearSelector(Transform padre, string nombre, string texto, string[] opciones, int inicial, bool darVuelta)
        {
            FilaOpcion fila = Crear(padre, nombre, texto, TipoFilaOpcion.Selector);
            fila.dar = darVuelta;
            fila.flechaIzquierda = UIInicio.Imagen(fila.transform, "FlechaIzq", UIInicio.Cian, SpritesInicio.Triangulo);
            fila.flechaIzquierda.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            UIInicio.Punto(fila.flechaIzquierda.rectTransform, new Vector2(1f, 0.5f), new Vector2(-AnchoControl - 14f, 0f), new Vector2(16f, 20f));
            fila.flechaDerecha = UIInicio.Imagen(fila.transform, "FlechaDer", UIInicio.Cian, SpritesInicio.Triangulo);
            UIInicio.Punto(fila.flechaDerecha.rectTransform, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(16f, 20f));
            fila.valorTexto = UIInicio.Texto(fila.transform, "Valor", string.Empty, UIInicio.FuenteTitulos, 38, UIInicio.Blanco, TextAnchor.MiddleCenter);
            UIInicio.Punto(fila.valorTexto.rectTransform, new Vector2(1f, 0.5f), new Vector2(-30f - (AnchoControl - 44f) * 0.5f, 0f), new Vector2(AnchoControl - 64f, 60f));
            fila.FijarOpciones(opciones, inicial);
            return fila;
        }

        public static FilaOpcion CrearDeslizador(Transform padre, string nombre, string texto, float minimo, float maximo, float paso, float inicial, Func<float, string> formato)
        {
            FilaOpcion fila = Crear(padre, nombre, texto, TipoFilaOpcion.Deslizador);
            fila.minimo = minimo;
            fila.maximo = maximo;
            fila.paso = paso;
            fila.formato = formato;
            fila.pista = UIInicio.Imagen(fila.transform, "Pista", new Color(1f, 1f, 1f, 0.16f));
            UIInicio.Punto(fila.pista.rectTransform, new Vector2(1f, 0.5f), new Vector2(-AnchoControl + AnchoPista * 0.5f + 8f, 0f), new Vector2(AnchoPista, AltoPista));
            fila.relleno = UIInicio.Imagen(fila.pista.transform, "Relleno", fila.acento, SpritesInicio.Blanco, true);
            UIInicio.Anclar(fila.relleno.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(0f, 0f));
            fila.perilla = UIInicio.Imagen(fila.pista.transform, "Perilla", UIInicio.Blanco, SpritesInicio.Circulo);
            UIInicio.Punto(fila.perilla.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(DiametroPerilla, DiametroPerilla));
            fila.valorTexto = UIInicio.Texto(fila.transform, "Valor", string.Empty, UIInicio.FuenteTitulos, 38, UIInicio.Blanco, TextAnchor.MiddleRight);
            UIInicio.Punto(fila.valorTexto.rectTransform, new Vector2(1f, 0.5f), new Vector2(-30f - 50f, 0f), new Vector2(110f, 60f));
            fila.FijarValor(inicial);
            return fila;
        }

        public static FilaOpcion CrearConmutador(Transform padre, string nombre, string texto, bool inicial)
        {
            FilaOpcion fila = Crear(padre, nombre, texto, TipoFilaOpcion.Conmutador);
            fila.interruptor = UIInicio.Imagen(fila.transform, "Interruptor", UIInicio.Cian, SpritesInicio.Panel);
            fila.interruptor.type = Image.Type.Sliced;
            UIInicio.Punto(fila.interruptor.rectTransform, new Vector2(1f, 0.5f), new Vector2(-30f - AnchoInterruptor * 0.5f, 0f), new Vector2(AnchoInterruptor, AltoInterruptor));
            fila.perilla = UIInicio.Imagen(fila.interruptor.transform, "Perilla", UIInicio.Blanco, SpritesInicio.Blanco);
            UIInicio.Punto(fila.perilla.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(AnchoInterruptor * 0.42f, AltoInterruptor - 10f));
            fila.valorTexto = UIInicio.Texto(fila.transform, "Valor", string.Empty, UIInicio.FuenteTitulos, 34, UIInicio.TextoSuave, TextAnchor.MiddleRight);
            UIInicio.Punto(fila.valorTexto.rectTransform, new Vector2(1f, 0.5f), new Vector2(-30f - AnchoInterruptor - 60f, 0f), new Vector2(120f, 60f));
            fila.FijarActivo(inicial);
            return fila;
        }

        private static FilaOpcion Crear(Transform padre, string nombre, string texto, TipoFilaOpcion tipo)
        {
            RectTransform raiz = UIInicio.Nodo(padre, nombre);
            Image zona = raiz.gameObject.AddComponent<Image>();
            zona.color = Color.clear;
            zona.raycastTarget = true;
            FilaOpcion fila = raiz.gameObject.AddComponent<FilaOpcion>();
            fila.transition = Transition.None;
            fila.targetGraphic = zona;
            fila.tipo = tipo;
            fila.fondo = UIInicio.Imagen(raiz, "Fondo", UIInicio.ConAlfa(fila.acento, 0f), SpritesInicio.DegradadoH);
            UIInicio.Estirar(fila.fondo.rectTransform);
            fila.barra = UIInicio.Imagen(raiz, "Barra", fila.acento, SpritesInicio.Blanco, true);
            UIInicio.Anclar(fila.barra.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 40f));
            fila.etiqueta = UIInicio.Texto(raiz, "Etiqueta", texto, UIInicio.FuenteTexto, 33, UIInicio.TextoSuave, TextAnchor.MiddleLeft);
            UIInicio.Anclar(fila.etiqueta.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), Vector2.zero);
            return fila;
        }

        public void FijarOpciones(string[] nuevas, int seleccionada)
        {
            opciones = nuevas ?? new string[0];
            FijarIndice(seleccionada);
        }

        /// <summary>Pone el índice sin avisar (el menú lo usa al cargar los valores guardados).</summary>
        public void FijarIndice(int nuevo)
        {
            indice = opciones.Length == 0 ? 0 : Mathf.Clamp(nuevo, 0, opciones.Length - 1);
            if (valorTexto != null && tipo == TipoFilaOpcion.Selector)
            {
                valorTexto.text = opciones.Length > 0 ? opciones[indice] : string.Empty;
            }
        }

        public void FijarValor(float nuevo)
        {
            valor = Mathf.Clamp(nuevo, minimo, maximo);
            Pintar();
        }

        public void FijarActivo(bool nuevo)
        {
            activo = nuevo;
            Pintar();
        }

        /// <summary>Mueve la fila un paso (+1 derecha, -1 izquierda). True si algo cambió o se usó la tecla.</summary>
        public bool Mover(int direccion)
        {
            switch (tipo)
            {
                case TipoFilaOpcion.Selector:
                {
                    int nuevo = ReglaInicio.MoverIndice(indice, direccion, opciones.Length, dar);
                    if (nuevo == indice)
                    {
                        return true;
                    }
                    FijarIndice(nuevo);
                    Avisar();
                    return true;
                }
                case TipoFilaOpcion.Deslizador:
                {
                    float nuevo = ReglaInicio.MoverValor(valor, direccion, paso, minimo, maximo);
                    if (!Mathf.Approximately(nuevo, valor))
                    {
                        FijarValor(nuevo);
                        Avisar();
                    }
                    return true;
                }
                default:
                {
                    bool nuevo = direccion > 0;
                    if (nuevo != activo)
                    {
                        FijarActivo(nuevo);
                        Avisar();
                    }
                    return true;
                }
            }
        }

        private void Alternar()
        {
            if (tipo == TipoFilaOpcion.Conmutador)
            {
                FijarActivo(!activo);
                Avisar();
            }
            else if (tipo == TipoFilaOpcion.Selector)
            {
                int nuevo = ReglaInicio.MoverIndice(indice, 1, opciones.Length, true);
                FijarIndice(nuevo);
                Avisar();
            }
        }

        private void Avisar()
        {
            Sonidos.Reproducir(BotonInicio.SonidoMover);
            Cambio?.Invoke(this);
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Left || eventData.moveDir == MoveDirection.Right)
            {
                Mover(eventData.moveDir == MoveDirection.Right ? 1 : -1);
                eventData.Use();
                return;
            }
            base.OnMove(eventData);
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Alternar();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (tipo != TipoFilaOpcion.Deslizador && eventData.button == PointerEventData.InputButton.Left)
            {
                Alternar();
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (tipo == TipoFilaOpcion.Deslizador)
            {
                MoverConPuntero(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (tipo == TipoFilaOpcion.Deslizador)
            {
                MoverConPuntero(eventData);
            }
        }

        private void MoverConPuntero(PointerEventData eventData)
        {
            if (pista == null)
            {
                return;
            }
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(pista.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                Rect rect = pista.rectTransform.rect;
                float fraccion = Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
                float nuevo = ReglaInicio.ValorDeFraccion(fraccion, paso, minimo, maximo);
                if (!Mathf.Approximately(nuevo, valor))
                {
                    FijarValor(nuevo);
                    Avisar();
                }
            }
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);
            if (IsInteractable() && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject, eventData);
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            elegido = true;
            Sonidos.Reproducir(BotonInicio.SonidoMover);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            elegido = false;
        }

        private void Update()
        {
            encendido = Mathf.MoveTowards(encendido, elegido ? 1f : 0f, Time.unscaledDeltaTime / SegundosEncender);
            if (fondo == null)
            {
                return;
            }
            float suave = encendido * encendido * (3f - 2f * encendido);
            fondo.color = UIInicio.ConAlfa(acento, AlfaFondo * suave);
            barra.color = UIInicio.ConAlfa(acento, suave);
            barra.rectTransform.localScale = new Vector3(1f, suave, 1f);
            etiqueta.color = Color.Lerp(UIInicio.TextoSuave, UIInicio.Blanco, suave);
            if (flechaIzquierda != null)
            {
                Color flecha = UIInicio.ConAlfa(acento, Mathf.Lerp(0.25f, 1f, suave));
                flechaIzquierda.color = flecha;
                flechaDerecha.color = flecha;
            }
        }

        private void Pintar()
        {
            if (valorTexto == null)
            {
                return;
            }
            if (tipo == TipoFilaOpcion.Deslizador)
            {
                float fraccion = Mathf.InverseLerp(minimo, maximo, valor);
                relleno.rectTransform.sizeDelta = new Vector2(AnchoPista * fraccion, 0f);
                perilla.rectTransform.anchoredPosition = new Vector2(AnchoPista * fraccion, 0f);
                valorTexto.text = formato != null ? formato(valor) : ReglaInicio.TextoVolumen(valor);
            }
            else if (tipo == TipoFilaOpcion.Conmutador)
            {
                interruptor.color = activo ? UIInicio.ConAlfa(acento, 0.85f) : new Color(1f, 1f, 1f, 0.2f);
                float recorrido = AnchoInterruptor - AnchoInterruptor * 0.42f - 10f;
                perilla.rectTransform.anchoredPosition = new Vector2(5f + (activo ? recorrido : 0f), 0f);
                valorTexto.text = activo ? "SÍ" : "NO";
                valorTexto.color = activo ? acento : UIInicio.TextoSuave;
            }
        }
    }
}

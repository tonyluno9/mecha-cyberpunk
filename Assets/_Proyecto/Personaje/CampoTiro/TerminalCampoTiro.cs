using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.CampoTiro
{
    /// <summary>
    /// T85: la terminal holográfica del campo de tiro, a un lado de donde aparece el jugador. Un pedestal con un panel inclinado; al acercarse
    /// (<see cref="ReglaCampoTiro.MetrosLetrero"/>) aparece el letrero "PRESIONA E PARA CONFIGURAR". La tecla E abre el menú (la lee el corredor).
    /// V11: más a un lado y más baja, para no tapar el arma. En VR (<see cref="ModoVR.Activo"/>) además es un <see cref="IInteractuable"/>:
    /// la B del control (la acción Interactuar) abre el menú y el letrero de interactuar de VR sale sobre ella, en lugar del de la E.
    /// Sin VR no se registra y todo queda como antes.
    /// </summary>
    public class TerminalCampoTiro : MonoBehaviour, IInteractuable
    {
        public const string TextoLetrero = "PRESIONA E PARA CONFIGURAR";
        /// <summary>Lo que dice el letrero de interactuar en VR ("B · CONFIGURAR CAMPO").</summary>
        public const string TextoInteractuar = "CONFIGURAR CAMPO";
        /// <summary>V11: antes 1.0 m; más baja para no tapar el arma.</summary>
        private const float AlturaPedestal = 0.7f;
        /// <summary>Altura del letrero sobre el pedestal (m).</summary>
        private const float AlturaLetrero = 0.85f;
        /// <summary>Punto de uso: un poco enfrente de la terminal, del lado del jugador (m).</summary>
        private const float FrenteUso = 0.6f;
        private const float EscalaMundo = 0.01f;
        private const int TamanoLetra = 40;
        private static readonly Vector2 TamanoCaja = new Vector2(520f, 90f);

        private GameObject letrero;
        private bool registrada;

        public Vector3 Posicion => transform.TransformPoint(new Vector3(0f, 0f, FrenteUso));
        public string Texto => TextoInteractuar;
        public bool PuedeUsar => true;

        /// <summary>Solo en VR (con la B): abre el menú si se puede, como la E del corredor.</summary>
        public void Usar()
        {
            if (CampoDeTiro.EstaDentro && !CampoDeTiro.EnTransicion && !CampoDeTiro.MenuAbierto && PilaPantallas.Vacia)
            {
                CampoDeTiro.AbrirMenu();
            }
        }

        /// <summary>True si el letrero está visible ahora.</summary>
        public bool LetreroVisible => letrero != null && letrero.activeSelf;
        public Vector3 PosicionLetrero => letrero != null ? letrero.transform.position : transform.position;

        public static TerminalCampoTiro Crear(Transform sala, Vector3 local, Material holo)
        {
            GameObject raiz = new GameObject("Terminal");
            raiz.transform.SetParent(sala, false);
            raiz.transform.localPosition = local;
            // Su cara (+Z local) mira hacia el jugador, que aparece en -X de la terminal y mira a +Z.
            raiz.transform.localRotation = Quaternion.Euler(0f, ReglaCampoTiro.GiroTerminal, 0f);
            TerminalCampoTiro terminal = raiz.AddComponent<TerminalCampoTiro>();
            Pieza(raiz.transform, "Pedestal", PrimitiveType.Cube, new Vector3(0f, AlturaPedestal * 0.5f, 0f), new Vector3(0.5f, AlturaPedestal, 0.4f), holo);
            GameObject panel = Pieza(raiz.transform, "Panel", PrimitiveType.Cube, new Vector3(0f, AlturaPedestal + 0.3f, -0.05f), new Vector3(1.1f, 0.6f, 0.04f), holo);
            panel.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);

            terminal.letrero = new GameObject("Letrero");
            terminal.letrero.transform.SetParent(raiz.transform, false);
            terminal.letrero.transform.localPosition = new Vector3(0f, AlturaPedestal + AlturaLetrero, 0f);
            terminal.letrero.transform.localScale = Vector3.one * EscalaMundo;
            Canvas canvas = terminal.letrero.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)terminal.letrero.transform).sizeDelta = TamanoCaja;
            Text texto = TrazoH10.Etiqueta(terminal.letrero.transform, "Texto", TextoLetrero, TamanoLetra, TrazoH10.Turquesa, TextAnchor.MiddleCenter);
            TrazoH10.Estirar(texto.rectTransform);
            terminal.letrero.SetActive(false);
            return terminal;
        }

        private void OnEnable() => Registrar(ModoVR.Activo);

        private void OnDisable() => Registrar(false);

        // En VR se usa con la acción Interactuar (B) por InteraccionAPie; en PC no se registra y la E la lee el corredor como siempre.
        private void Registrar(bool si)
        {
            if (si == registrada)
            {
                return;
            }
            registrada = si;
            if (si)
            {
                Registro<IInteractuable>.Agregar(this);
            }
            else
            {
                Registro<IInteractuable>.Quitar(this);
            }
        }

        private void LateUpdate()
        {
            Registrar(ModoVR.Activo);
            IJugador jugador = Registro<IJugador>.Actual;
            Transform quien = jugador != null ? jugador.Raiz : null;
            // En VR el letrero de la E no sale: sale el de interactuar de VR ("B · CONFIGURAR CAMPO").
            bool cerca = !ModoVR.Activo && quien != null && ReglaCampoTiro.EstaCercaDeLaTerminal(quien.position, transform.position);
            if (letrero.activeSelf != cerca)
            {
                letrero.SetActive(cerca);
            }
            Camera camara = Camera.main;
            if (cerca && camara != null)
            {
                letrero.transform.rotation = Quaternion.LookRotation(letrero.transform.position - camara.transform.position, Vector3.up);
            }
        }

        private static GameObject Pieza(Transform padre, string nombre, PrimitiveType tipo, Vector3 local, Vector3 escala, Material material)
        {
            GameObject objeto = GameObject.CreatePrimitive(tipo);
            objeto.name = nombre;
            objeto.transform.SetParent(padre, false);
            objeto.transform.localPosition = local;
            objeto.transform.localScale = escala;
            Destroy(objeto.GetComponent<Collider>());
            Renderer dibujo = objeto.GetComponent<Renderer>();
            dibujo.sharedMaterial = material;
            dibujo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return objeto;
        }
    }
}

using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Kit;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Hud
{
    /// <summary>
    /// Barra del escudo de energía (T-684): una barra delgada azul justo encima de la vida, con el mismo estilo (hueco oscuro,
    /// relleno plano). Se arma en código bajo el grupo de la vida, así sigue su visibilidad por modo. Al absorber daño
    /// la barra destella y salta un velo azul breve en toda la pantalla. La agrega <see cref="HudAPie"/> al nacer.
    /// </summary>
    public class HudEscudo : MonoBehaviour
    {
        private static readonly Color ColorEscudo = new Color(0.25f, 0.65f, 1f, 1f);
        private static readonly Color ColorDestello = new Color(0.9f, 1f, 1f, 1f);
        private static readonly Color ColorVelo = new Color(0.2f, 0.6f, 1f, 1f);
        private const float AltoBarra = 10f;
        private const float Separacion = 6f;
        private const float SegundosDestello = 0.3f;
        private const float AlphaVelo = 0.22f;
        private const float SaltoEscala = 0.12f;
        // Un tirón de cuadros no se come el destello entero.
        private const float MaxPasoDestello = 0.05f;

        private HudAPie hud;
        private RectTransform barra;
        private Image hueco;
        private Image relleno;
        private Text texto;
        private Image velo;
        private IEscudo escudo;
        private float destello;

        public Image Relleno => relleno;
        public Text Texto => texto;
        public Image Velo => velo;
        public RectTransform Barra => barra;
        public bool Armado => barra != null;
        public float DestelloRestante => destello;

        private void Start()
        {
            hud = GetComponent<HudAPie>();
            if (hud == null || hud.GrupoVida == null)
            {
                enabled = false;
                return;
            }
            Armar();
            Registro<IEscudo>.Agregado += AlAgregar;
            Registro<IEscudo>.Quitado += AlQuitar;
            Vincular(Registro<IEscudo>.Actual);
        }

        private void OnDestroy()
        {
            Registro<IEscudo>.Agregado -= AlAgregar;
            Registro<IEscudo>.Quitado -= AlQuitar;
            Desvincular();
        }

        private void Armar()
        {
            RectTransform grupo = (RectTransform)hud.GrupoVida.transform;
            barra = NuevoRect("Escudo", grupo);
            barra.anchorMin = barra.anchorMax = new Vector2(0f, 1f);
            barra.pivot = new Vector2(0f, 0f);
            barra.sizeDelta = new Vector2(grupo.sizeDelta.x, AltoBarra);
            barra.anchoredPosition = new Vector2(0f, Separacion);

            hueco = barra.gameObject.AddComponent<Image>();
            hueco.color = new Color(0f, 0f, 0f, 0.55f);
            hueco.raycastTarget = false;

            RectTransform rr = NuevoRect("Relleno", barra);
            rr.anchorMin = Vector2.zero;
            rr.anchorMax = Vector2.one;
            rr.offsetMin = rr.offsetMax = Vector2.zero;
            relleno = rr.gameObject.AddComponent<Image>();
            relleno.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), new Vector2(0.5f, 0.5f));
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = Image.FillMethod.Horizontal;
            relleno.fillOrigin = (int)Image.OriginHorizontal.Left;
            relleno.color = ColorEscudo;
            relleno.raycastTarget = false;

            // Número pequeño a la derecha, sobre la barra, con la misma fuente que la vida.
            RectTransform rt = NuevoRect("Texto", barra);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 0f);
            rt.offsetMax = new Vector2(-4f, 0f);
            texto = rt.gameObject.AddComponent<Text>();
            texto.font = hud.TextoVida != null ? hud.TextoVida.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texto.fontSize = 10;
            texto.alignment = TextAnchor.MiddleRight;
            texto.color = KitUI.Estilo.Contorno;
            texto.raycastTarget = false;

            // Velo azul a pantalla completa, detrás del resto del HUD.
            RectTransform rv = NuevoRect("VeloEscudo", transform);
            rv.anchorMin = Vector2.zero;
            rv.anchorMax = Vector2.one;
            rv.offsetMin = rv.offsetMax = Vector2.zero;
            rv.SetAsFirstSibling();
            velo = rv.gameObject.AddComponent<Image>();
            velo.color = new Color(ColorVelo.r, ColorVelo.g, ColorVelo.b, 0f);
            velo.raycastTarget = false;
        }

        private static RectTransform NuevoRect(string nombre, Transform padre)
        {
            GameObject objeto = new GameObject(nombre, typeof(RectTransform));
            objeto.transform.SetParent(padre, false);
            return (RectTransform)objeto.transform;
        }

        private void AlAgregar(IEscudo nuevo) => Vincular(nuevo);

        private void AlQuitar(IEscudo quitado)
        {
            if (quitado == escudo)
            {
                Vincular(Registro<IEscudo>.Actual);
            }
        }

        private void Vincular(IEscudo nuevo)
        {
            Desvincular();
            escudo = nuevo;
            barra.gameObject.SetActive(escudo != null && escudo.Maximo > 0f);
            if (escudo == null)
            {
                return;
            }
            escudo.Cambio += Pintar;
            escudo.Absorbio += AlAbsorber;
            Pintar();
        }

        private void Desvincular()
        {
            if (escudo != null)
            {
                escudo.Cambio -= Pintar;
                escudo.Absorbio -= AlAbsorber;
                escudo = null;
            }
        }

        private void Pintar()
        {
            if (escudo == null || barra == null)
            {
                return;
            }
            barra.gameObject.SetActive(escudo.Maximo > 0f);
            relleno.fillAmount = escudo.Maximo > 0f ? Mathf.Clamp01(escudo.Actual / escudo.Maximo) : 0f;
            texto.text = Mathf.CeilToInt(escudo.Actual).ToString();
        }

        private void AlAbsorber(float puntos)
        {
            destello = SegundosDestello;
        }

        private void Update()
        {
            if (barra == null)
            {
                return;
            }
            if (destello > 0f)
            {
                destello = Mathf.Max(0f, destello - Mathf.Min(Time.unscaledDeltaTime, MaxPasoDestello));
            }
            float t = destello / SegundosDestello;
            relleno.color = Color.Lerp(ColorEscudo, ColorDestello, t);
            barra.localScale = Vector3.one * (1f + SaltoEscala * t);
            velo.color = new Color(ColorVelo.r, ColorVelo.g, ColorVelo.b, AlphaVelo * t);
        }
    }
}

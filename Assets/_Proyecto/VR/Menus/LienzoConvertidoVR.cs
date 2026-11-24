using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Menus
{
    /// <summary>
    /// VR4: un Canvas de pantalla que se pasó a World Space, con lo que tenía antes para dejarlo igual al salir de VR.
    /// También guarda sus Canvas anidados (el láser busca en todos) y apaga sus GraphicRaycaster (el ratón no debe pegarle a un panel del mundo).
    /// </summary>
    public sealed class LienzoConvertidoVR
    {
        public Canvas Canvas { get; }
        public TipoLienzoVR Tipo { get; }

        /// <summary>El Canvas raíz y sus anidados (se refresca cada barrido).</summary>
        public List<Canvas> Lienzos { get; } = new List<Canvas>();

        private readonly RenderMode modoOriginal;
        private readonly Camera camaraOriginal;
        private readonly float distanciaPlanoOriginal;
        private readonly Vector3 posicionOriginal;
        private readonly Quaternion rotacionOriginal;
        private readonly Vector3 escalaOriginal;
        private readonly Vector2 anclaMinOriginal;
        private readonly Vector2 anclaMaxOriginal;
        private readonly Vector2 pivoteOriginal;
        private readonly Vector2 tamanoOriginal;
        private readonly List<GraphicRaycaster> apagados = new List<GraphicRaycaster>();
        private readonly List<GraphicRaycaster> temporal = new List<GraphicRaycaster>();
        private readonly List<Image> imagenes = new List<Image>();
        private readonly Dictionary<Transform, Vector3> encogidos = new Dictionary<Transform, Vector3>();

        public LienzoConvertidoVR(Canvas canvas, TipoLienzoVR tipo)
        {
            Canvas = canvas;
            Tipo = tipo;
            modoOriginal = canvas.renderMode;
            camaraOriginal = canvas.worldCamera;
            distanciaPlanoOriginal = canvas.planeDistance;
            Transform t = canvas.transform;
            posicionOriginal = t.localPosition;
            rotacionOriginal = t.localRotation;
            escalaOriginal = t.localScale;
            var rect = (RectTransform)t;
            anclaMinOriginal = rect.anchorMin;
            anclaMaxOriginal = rect.anchorMax;
            pivoteOriginal = rect.pivot;
            tamanoOriginal = rect.sizeDelta;
        }

        /// <summary>Distancia del Canvas del casco a la cámara del HUD (m): dentro de su near y far.</summary>
        public const float DistanciaPlanoCasco = 1f;

        /// <summary>
        /// Lo pasa a World Space con el tamaño de la pantalla de referencia (1920 x 1080 px). VR14: si es del casco, a Screen Space Camera
        /// sobre <paramref name="camaraCasco"/> (la cámara del HUD de casco, que dibuja a una textura de 1920 x 1080: mismo diseño que en PC).
        /// </summary>
        public void Convertir(Camera camaraCasco = null)
        {
            if (Tipo == TipoLienzoVR.Casco)
            {
                if (camaraCasco != null)
                {
                    Canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    Canvas.worldCamera = camaraCasco;
                    Canvas.planeDistance = DistanciaPlanoCasco;
                }
                Refrescar();
                return;
            }
            Canvas.renderMode = RenderMode.WorldSpace;
            Canvas.worldCamera = null;
            var rect = (RectTransform)Canvas.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(ReglaLienzosVR.AnchoReferencia, ReglaLienzosVR.AltoReferencia);
            Refrescar();
        }

        /// <summary>True si sigue como se convirtió (el juego no lo volvió a poner en pantalla).</summary>
        public bool EstaConvertido(Camera camaraCasco)
        {
            if (Tipo == TipoLienzoVR.Casco)
            {
                return camaraCasco == null || (Canvas.renderMode == RenderMode.ScreenSpaceCamera && Canvas.worldCamera == camaraCasco);
            }
            return Canvas.renderMode == RenderMode.WorldSpace;
        }

        /// <summary>
        /// Vuelve a buscar los Canvas anidados, apaga los GraphicRaycaster nuevos y encoge a 0 los anidados que son HUD de pantalla
        /// (mira del francotirador, barras de enemigos).
        /// </summary>
        public void Refrescar()
        {
            if (Canvas == null)
            {
                return;
            }
            Canvas.GetComponentsInChildren(true, Lienzos);
            foreach (Canvas anidado in Lienzos)
            {
                if (anidado == null || anidado == Canvas || !ReglaLienzosVR.EsOculto(anidado.name))
                {
                    continue;
                }
                Encoger(anidado.transform);
            }
            if (Tipo == TipoLienzoVR.Casco)
            {
                // VR14: la mira de PC y el aviso "E · interactuar" no van en el casco.
                Transform raiz = Canvas.transform;
                for (int i = 0; i < raiz.childCount; i++)
                {
                    Transform hijo = raiz.GetChild(i);
                    if (ReglaLienzosVR.EsGrupoOcultoCasco(hijo.name))
                    {
                        Encoger(hijo);
                    }
                }
                // VR25: los degradados negros de las esquinas del H10 (en el visor se ven como el borde de una pantalla).
                Canvas.GetComponentsInChildren(true, imagenes);
                foreach (Image imagen in imagenes)
                {
                    if (imagen != null && ReglaLienzosVR.EsSombraCasco(imagen.name))
                    {
                        Encoger(imagen.transform);
                    }
                }
                imagenes.Clear();
            }
            Canvas.GetComponentsInChildren(true, temporal);
            foreach (GraphicRaycaster g in temporal)
            {
                if (g != null && g.enabled)
                {
                    g.enabled = false;
                    if (!apagados.Contains(g))
                    {
                        apagados.Add(g);
                    }
                }
            }
        }

        private void Encoger(Transform t)
        {
            if (!encogidos.ContainsKey(t))
            {
                encogidos[t] = t.localScale;
            }
            if (t.localScale != Vector3.zero)
            {
                t.localScale = Vector3.zero;
            }
        }

        /// <summary>Pone el Canvas en una pose del mundo con una escala (m por px de referencia).</summary>
        public void Colocar(Vector3 posicion, Quaternion rotacion, float metrosPorPx)
        {
            Transform t = Canvas.transform;
            t.SetPositionAndRotation(posicion, rotacion);
            Vector3 escalaPadre = t.parent != null ? t.parent.lossyScale : Vector3.one;
            float padre = Mathf.Abs(escalaPadre.x) > 1e-6f ? escalaPadre.x : 1f;
            float local = metrosPorPx / padre;
            if (Mathf.Abs(t.localScale.x - local) > 1e-7f)
            {
                t.localScale = new Vector3(local, local, local);
            }
        }

        /// <summary>Lo deja como estaba antes de VR.</summary>
        public void Restaurar()
        {
            foreach (GraphicRaycaster g in apagados)
            {
                if (g != null)
                {
                    g.enabled = true;
                }
            }
            apagados.Clear();
            foreach (KeyValuePair<Transform, Vector3> par in encogidos)
            {
                if (par.Key != null)
                {
                    par.Key.localScale = par.Value;
                }
            }
            encogidos.Clear();
            if (Canvas == null)
            {
                return;
            }
            Canvas.renderMode = modoOriginal;
            Canvas.worldCamera = camaraOriginal;
            Canvas.planeDistance = distanciaPlanoOriginal;
            var rect = (RectTransform)Canvas.transform;
            rect.anchorMin = anclaMinOriginal;
            rect.anchorMax = anclaMaxOriginal;
            rect.pivot = pivoteOriginal;
            rect.sizeDelta = tamanoOriginal;
            rect.localPosition = posicionOriginal;
            rect.localRotation = rotacionOriginal;
            rect.localScale = escalaOriginal;
        }
    }
}

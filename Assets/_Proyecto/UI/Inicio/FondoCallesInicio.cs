using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MechaCyberpunk.UI.Inicio
{
    /// <summary>
    /// T-893: el fondo del menú de inicio con las calles reales del juego (K01, tianguis, Neón Alto, Nova): fotos de la ciudad que se acercan
    /// despacio y cambian con un corte suave cada <see cref="SegundosPorCalle"/>.
    /// T-898: las fotos solo cubren lo que tarda en cargar la escena Menu_Calles; en cuanto está lista se desvanecen y queda la cámara real.
    /// </summary>
    public class FondoCallesInicio : MonoBehaviour
    {
        public const string CarpetaResources = "Inicio/FondoCalle_";
        public const int Calles = 4;
        public const float SegundosPorCalle = 8f;
        public const string EscenaCalles = "Assets/_Proyecto/Escenas/Menu_Calles.unity";
        private const float SegundosAReal = 1f;
        private const float SegundosCorte = 0.7f;
        private const float EscalaInicial = 1.04f;
        private const float EscalaFinal = 1.14f;

        private Sprite[] fotos;
        private Image frente;
        private Image detras;
        private CalculoFondoCalles calculo;

        /// <summary>En modo batch (las pruebas) no se carga la ciudad, salvo que la prueba lo pida.</summary>
        public static bool CargarEnBatch { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void LimpiarEstatico() => CargarEnBatch = false;

        /// <summary>Avisa cuando la cámara real ya se ve.</summary>
        public event Action CalleRealLista;

        /// <summary>True cuando la cámara real sustituyó a las fotos.</summary>
        public bool CamaraReal { get; private set; }

        public int CantidadFotos => fotos != null ? fotos.Length : 0;
        public int Actual => calculo != null ? calculo.Actual : 0;

        public static FondoCallesInicio Crear(Transform padre)
        {
            RectTransform nodo = UIInicio.Nodo(padre, "FondoCalles");
            UIInicio.Estirar(nodo);
            nodo.SetAsFirstSibling();
            FondoCallesInicio f = nodo.gameObject.AddComponent<FondoCallesInicio>();
            f.Armar();
            return f;
        }

        private void Armar()
        {
            fotos = new Sprite[Calles];
            for (int i = 0; i < Calles; i++)
            {
                fotos[i] = Resources.Load<Sprite>(CarpetaResources + i);
                if (fotos[i] == null)
                {
                    Debug.LogError("[UI] FondoCallesInicio: falta la foto " + CarpetaResources + i, this);
                    enabled = false;
                    return;
                }
            }
            detras = Capa("Detras");
            frente = Capa("Frente");
            calculo = new CalculoFondoCalles(Calles, SegundosPorCalle);
            Mostrar(frente, fotos[0]);
        }

        private Image Capa(string nombre)
        {
            Image i = UIInicio.Imagen(transform, nombre, Color.white, null);
            UIInicio.Estirar(i.rectTransform);
            i.raycastTarget = false;
            return i;
        }

        private static void Mostrar(Image capa, Sprite foto)
        {
            capa.DOKill();
            capa.rectTransform.DOKill();
            capa.sprite = foto;
            capa.color = Color.white;
            capa.rectTransform.localScale = Vector3.one * EscalaInicial;
            capa.rectTransform.DOScale(EscalaFinal, SegundosPorCalle + SegundosCorte).SetEase(Ease.Linear);
        }

        private IEnumerator Start()
        {
            if (Application.isBatchMode && !CargarEnBatch)
            {
                yield break;
            }
#if UNITY_EDITOR
            // Como EscenaInicio: en el editor se carga por ruta, sin estar en Build Settings (la demo lleva solo sus tres escenas).
            AsyncOperation carga = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(EscenaCalles, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            if (SceneUtility.GetBuildIndexByScenePath(EscenaCalles) < 0)
            {
                yield break; // sin la escena en el build se quedan las fotos
            }
            AsyncOperation carga = SceneManager.LoadSceneAsync(EscenaCalles, LoadSceneMode.Additive);
#endif
            if (carga == null)
            {
                yield break;
            }
            yield return carga;
            Scene escena = SceneManager.GetSceneByPath(EscenaCalles);
            if (escena.IsValid())
            {
                SceneManager.SetActiveScene(escena); // su luz y su niebla
            }
            yield return null;
            CamaraReal = true;
            CalleRealLista?.Invoke();
            foreach (Image capa in new[] { frente, detras })
            {
                Image i = capa;
                DOTween.ToAlpha(() => i.color, c => i.color = c, 0f, SegundosAReal).SetTarget(i).SetUpdate(true).SetEase(Ease.InOutSine);
            }
            enabled = false;
        }

        private void Update()
        {
            if (!calculo.Avanzar(Time.unscaledDeltaTime))
            {
                return;
            }
            // La capa de atrás toma la foto nueva y la de enfrente se desvanece; luego cambian de papel.
            Mostrar(detras, fotos[calculo.Actual]);
            Image saliente = frente;
            DOTween.ToAlpha(() => saliente.color, c => saliente.color = c, 0f, SegundosCorte).SetTarget(saliente).SetEase(Ease.InOutSine);
            frente.transform.SetAsFirstSibling();
            detras.transform.SetAsFirstSibling();
            frente.transform.SetAsLastSibling();
            (frente, detras) = (detras, frente);
        }

        private void OnDestroy()
        {
            foreach (Image i in new[] { frente, detras })
            {
                if (i != null)
                {
                    i.DOKill();
                    i.rectTransform.DOKill();
                }
            }
        }
    }
}

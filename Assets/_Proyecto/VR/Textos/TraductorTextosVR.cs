using System.Collections.Generic;
using MechaCyberpunk.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MechaCyberpunk.VR.Textos
{
    /// <summary>
    /// VR28: en <see cref="ModoVR"/> ningún texto en pantalla muestra teclas de PC. Cada cuadro, después de que todos escriben sus textos
    /// (LateUpdate al final) y antes de dibujar, pasa por <see cref="TeclaVR.Traducir"/> (la tabla central) los Text de UGUI y los TMP_Text activos
    /// (y los TextMesh 3D) cuyo texto cambió: avisos de interactuar, tutoriales, consejos de carga, ayudas de tiendas y menús, cinemáticas. Solo vuelve a traducir
    /// un texto cuando su cadena cambia (comparación por referencia), y la lista de textos se rehace cada <see cref="CuadrosLista"/> cuadros.
    /// No toca los campos donde la persona escribe (InputField). Sin VR no hace nada; vive solo (DontDestroyOnLoad), creado al arrancar.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class TraductorTextosVR : MonoBehaviour
    {
        /// <summary>Cada cuántos cuadros se vuelve a buscar los textos activos.</summary>
        public const int CuadrosLista = 6;

        private readonly List<Text> textos = new List<Text>();
        private readonly List<TMP_Text> textosTmp = new List<TMP_Text>();
        private readonly List<TextMesh> textos3D = new List<TextMesh>();
        private readonly Dictionary<int, string> vistos = new Dictionary<int, string>();
        private readonly HashSet<int> ignorados = new HashSet<int>();
        private int cuadroLista = -CuadrosLista;

        /// <summary>El traductor (null antes de arrancar).</summary>
        public static TraductorTextosVR Actual { get; private set; }

        /// <summary>Cuántos textos cambió desde que arrancó (para las pruebas y el registro).</summary>
        public int Traducidos { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            Actual = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Arrancar()
        {
            Asegurar();
        }

        /// <summary>Crea el traductor si no existe (público para las pruebas).</summary>
        public static TraductorTextosVR Asegurar()
        {
            if (Actual != null)
            {
                return Actual;
            }
            var objeto = new GameObject("TraductorTextosVR");
            DontDestroyOnLoad(objeto);
            return objeto.AddComponent<TraductorTextosVR>();
        }

        private void Awake()
        {
            if (Actual != null && Actual != this)
            {
                Destroy(gameObject);
                return;
            }
            Actual = this;
        }

        private void OnDestroy()
        {
            if (Actual == this)
            {
                Actual = null;
            }
        }

        private void LateUpdate()
        {
            if (!ModoVR.Activo)
            {
                return;
            }
            Barrer(false);
        }

        /// <summary>Traduce ya los textos activos (forzar: vuelve a buscar la lista aunque no toque). Público para las pruebas.</summary>
        public void Barrer(bool forzar)
        {
            if (forzar || Time.frameCount - cuadroLista >= CuadrosLista)
            {
                cuadroLista = Time.frameCount;
                textos.Clear();
                textosTmp.Clear();
                textos3D.Clear();
                textos.AddRange(FindObjectsByType<Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
                textosTmp.AddRange(FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
                textos3D.AddRange(FindObjectsByType<TextMesh>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
                if (vistos.Count > 4 * (textos.Count + textosTmp.Count + textos3D.Count) + 256)
                {
                    vistos.Clear(); // los de escenas que ya se fueron
                }
            }
            for (int i = 0; i < textos.Count; i++)
            {
                Text t = textos[i];
                if (t == null)
                {
                    continue;
                }
                string actual = t.text;
                int id = t.GetInstanceID();
                if (YaVisto(id, actual) || Ignorar(id, t))
                {
                    continue;
                }
                string nuevo = TeclaVR.Traducir(actual);
                if (!string.Equals(nuevo, actual, System.StringComparison.Ordinal))
                {
                    t.text = nuevo;
                    Traducidos++;
                }
                vistos[id] = t.text;
            }
            for (int i = 0; i < textosTmp.Count; i++)
            {
                TMP_Text t = textosTmp[i];
                if (t == null)
                {
                    continue;
                }
                string actual = t.text;
                int id = t.GetInstanceID();
                if (YaVisto(id, actual) || Ignorar(id, t))
                {
                    continue;
                }
                string nuevo = TeclaVR.Traducir(actual);
                if (!string.Equals(nuevo, actual, System.StringComparison.Ordinal))
                {
                    t.text = nuevo;
                    Traducidos++;
                }
                vistos[id] = t.text;
            }
            // Los letreros 3D viejos (TextMesh: "PRESIONA E PARA CONFIGURAR").
            for (int i = 0; i < textos3D.Count; i++)
            {
                TextMesh t = textos3D[i];
                if (t == null)
                {
                    continue;
                }
                string actual = t.text;
                int id = t.GetInstanceID();
                // TextMesh devuelve una cadena nueva cada vez: se compara el contenido.
                if (string.IsNullOrEmpty(actual) || (vistos.TryGetValue(id, out string visto) && string.Equals(visto, actual, System.StringComparison.Ordinal)))
                {
                    continue;
                }
                string nuevo = TeclaVR.Traducir(actual);
                if (!string.Equals(nuevo, actual, System.StringComparison.Ordinal))
                {
                    t.text = nuevo;
                    Traducidos++;
                }
                vistos[id] = t.text;
            }
        }

        private bool YaVisto(int id, string actual)
        {
            if (string.IsNullOrEmpty(actual))
            {
                return true;
            }
            return vistos.TryGetValue(id, out string visto) && ReferenceEquals(visto, actual);
        }

        // Los campos donde se escribe (nombre de la pandilla...) no se traducen.
        private bool Ignorar(int id, Component t)
        {
            if (ignorados.Contains(id))
            {
                return true;
            }
            if (t.GetComponentInParent<InputField>(true) != null || t.GetComponentInParent<TMP_InputField>(true) != null)
            {
                ignorados.Add(id);
                return true;
            }
            return false;
        }
    }
}

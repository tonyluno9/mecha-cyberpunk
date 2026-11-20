using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MechaCyberpunk.Progresion.Prologo.Editor
{
    /// <summary>
    /// T-1514: sin cuarto (T-1496), la cama (dormir y guardar) y el espejo (editar personaje) eran hijos de Cuarto_Habitacion y quedaron apagados.
    /// Pasan a la puerta de la casa: dos interacciones sin modelo (su geometría greybox apagada) a los lados de Casa_Puerta, bajo la raíz
    /// "Casa_PuertaInteracciones". No hay cama ni espejo visibles en la calle: solo el aviso "E · Dormir" y "E · Mirarte al espejo" al acercarse.
    /// Idempotente. Batchmode: -executeMethod MechaCyberpunk.Progresion.Prologo.Editor.CamaEspejoEnLaPuertaT1514.Aplicar
    /// </summary>
    public static class CamaEspejoEnLaPuertaT1514
    {
        public const string RutaCiudad = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        public const string NombreRaiz = "Casa_PuertaInteracciones";
        public const string IdPuerta = "Casa_Puerta";
        /// <summary>Separación lateral de cada interacción respecto al centro de la puerta (m): la más cercana a donde miras es la que se ofrece.</summary>
        public const float SeparacionLateral = 1.3f;

        [MenuItem("Mecha Cyberpunk/T-1514/Cama y espejo en la puerta")]
        public static void Aplicar()
        {
            Scene escena = EditorSceneManager.OpenScene(RutaCiudad, OpenSceneMode.Single);
            Transform[] todas = escena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
            Transform puerta = todas.FirstOrDefault(t => t.name == IdPuerta);
            CamaJugador cama = todas.Select(t => t.GetComponent<CamaJugador>()).FirstOrDefault(c => c != null);
            EspejoJugador espejo = todas.Select(t => t.GetComponent<EspejoJugador>()).FirstOrDefault(c => c != null);
            if (puerta == null || cama == null || espejo == null)
            {
                Debug.LogError("[T-1514] Faltan " + IdPuerta + ", CamaJugador o EspejoJugador en Ciudad_Prologo.");
                return;
            }

            Transform raiz = todas.FirstOrDefault(t => t.name == NombreRaiz);
            if (raiz == null)
            {
                raiz = new GameObject(NombreRaiz).transform;
            }
            raiz.SetPositionAndRotation(puerta.position, puerta.rotation);
            Colocar(cama.transform, raiz, -SeparacionLateral);
            Colocar(espejo.transform, raiz, SeparacionLateral);
            EditorSceneManager.MarkSceneDirty(escena);
            EditorSceneManager.SaveScene(escena);
            Debug.Log("[T-1514] Cama y espejo en la puerta de la casa: " + puerta.position);
        }

        private static void Colocar(Transform objeto, Transform raiz, float lateral)
        {
            objeto.SetParent(raiz, true);
            objeto.SetPositionAndRotation(raiz.position + raiz.right * lateral, raiz.rotation);
            objeto.gameObject.SetActive(true);
            // Sin modelo: ni mallas ni colliders de lo greybox (que no se vea una cama en la calle ni estorbe el paso).
            foreach (MeshRenderer r in objeto.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (r.GetComponentInParent<Canvas>() == null)
                {
                    r.enabled = false;
                }
            }
            foreach (Collider c in objeto.GetComponentsInChildren<Collider>(true))
            {
                c.enabled = false;
            }
        }
    }
}

using UnityEditor;
using UnityEngine;
using MechaCyberpunk.Mundo.Ambiente;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-740: fotos del espejo del cuarto con PJ01 y los tres implantes nivel 2 puestos: docs/capturas/pj01-implantes-espejo-frente.png y pj01-implantes-espejo-lado.png (las piezas se prenden a mano: en edición no corre Update).
    /// Batchmode: -executeMethod MechaCyberpunk.Arte.Editor.FotosEspejoImplantesT740.Fotos
    /// </summary>
    public static class FotosEspejoImplantesT740
    {
        private const float Piso = 0.225f;
        private const string RutaIdle = "Assets/Arte/Personajes/Animaciones/Locomocion/Loco_Idle.fbx";

        [MenuItem("Mecha Cyberpunk/T-740/Fotos del espejo con implantes")]
        public static void Fotos()
        {
            ColocarS2.Abrir();
            EspejoReflejo espejo = Object.FindFirstObjectByType<EspejoReflejo>();
            float x = ColocarS2tEspejo.CentroX;
            float z = ColocarS2tEspejo.ParedZ;
            GameObject jugador = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab"));
            jugador.transform.SetPositionAndRotation(new Vector3(x - 0.15f, Piso, z + 1.3f), Quaternion.LookRotation(Vector3.forward));
            AnimationClip idle = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(RutaIdle))
            {
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { idle = c; }
            }
            Animator animador = jugador.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.VisualPersonaje>(true).Animador;
            idle.SampleAnimation(animador.gameObject, 0.5f);
            foreach (MechaCyberpunk.Personaje.Visual.ImplantesVisiblesPJ01.Pieza p in jugador.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.ImplantesVisiblesPJ01>(true).Piezas) { p.objeto.SetActive(p.nivel == 2); }
            Light relleno = new GameObject("RellenoFoto").AddComponent<Light>();
            relleno.type = LightType.Point;
            relleno.range = 6f;
            relleno.intensity = 6f;
            relleno.transform.position = new Vector3(x + 0.6f, 2.2f, z + 2.2f);
            foreach (Camera c in jugador.GetComponentsInChildren<Camera>(true)) { c.gameObject.SetActive(false); }
            var tomas = new[]
            {
                ("espejo-frente", new Vector3(x + 0.75f, 1.6f, z + 2.6f), new Vector3(x, 1.2f, z)),
                ("espejo-lado", new Vector3(x + 1.0f, 1.6f, z + 1.9f), new Vector3(x - 0.1f, 1.2f, z)),
            };
            ColocarS2t.FotoHora(22f, tomas[0].Item2, tomas[0].Item3, "Logs/pj01-espejo-implantes-previa.png", 60f);
            foreach (var (nombre, desde, hacia) in tomas)
            {
                GameObject ojo = new GameObject("OjoFoto");
                Camera cam = ojo.AddComponent<Camera>();
                cam.enabled = false;
                ojo.transform.SetPositionAndRotation(desde, Quaternion.LookRotation(hacia - desde));
                if (espejo != null && espejo.Actualizar(cam)) { espejo.Camara.Render(); }
                Object.DestroyImmediate(ojo);
                idle.SampleAnimation(animador.gameObject, 0.5f);
                ColocarS2t.FotoHora(22f, desde, hacia, ColocarS2t.CarpetaCapturas + "pj01-implantes-" + nombre + ".png", 60f);
            }
            espejo?.Soltar();
            Object.DestroyImmediate(relleno.gameObject);
            Object.DestroyImmediate(jugador);
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ColocarS2.RutaEscena, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }
    }
}

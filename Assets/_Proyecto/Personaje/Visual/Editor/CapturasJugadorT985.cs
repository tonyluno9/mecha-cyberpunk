using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MechaCyberpunk.Personaje.Visual.Editor
{
    /// <summary>
    /// T-985: una captura por cada clip PJ01_* (docs/capturas/t985/&lt;Clip&gt;.png) del modelo del jugador jugando ese clip EN SU ESTADO del Animator del jugador
    /// (ControladorBase + Override_Jugador): se recorre el controlador, se pone cada árbol de mezcla en la posición del clip y se fuerza el estado a un tiempo representativo.
    /// Batchmode: -executeMethod MechaCyberpunk.Personaje.Visual.Editor.CapturasJugadorT985.Todas
    /// </summary>
    public static class CapturasJugadorT985
    {
        private const string Carpeta = "docs/capturas/t985";

        private struct Destino
        {
            public int Capa;
            public string Estado;
            public List<(string Parametro, float Valor)> Parametros;
        }

        public static void Todas()
        {
            var controlador = AssetDatabase.LoadAssetAtPath<AnimatorController>(ConstructorControladorBase.RutaControlador);
            var over = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ConstructorControladorBase.RutaOverride("Jugador"));
            var pares = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            over.GetOverrides(pares);
            var mapa = new Dictionary<AnimationClip, AnimationClip>();
            foreach (var p in pares) { if (p.Key != null) { mapa[p.Key] = p.Value != null ? p.Value : p.Key; } }

            // clip efectivo -> primer estado que lo juega (prefiere estados sin árbol)
            var destinos = new Dictionary<string, Destino>();
            for (int capa = 0; capa < controlador.layers.Length; capa++)
            {
                foreach (ChildAnimatorState s in controlador.layers[capa].stateMachine.states)
                {
                    Recorrer(capa, s.state.name, s.state.motion, new List<(string, float)>(), mapa, destinos);
                }
            }

            Directory.CreateDirectory(Carpeta);
            var nombres = new List<string>(destinos.Keys);
            nombres.Sort();
            foreach (string nombre in nombres)
            {
                if (!nombre.StartsWith("PJ01_")) { continue; }
                Capturar(nombre, destinos[nombre], over);
            }
            Debug.Log("[Personaje] T-985: " + nombres.Count + " capturas en " + Carpeta);
        }

        private static void Recorrer(int capa, string estado, Motion m, List<(string, float)> parametros, Dictionary<AnimationClip, AnimationClip> mapa, Dictionary<string, Destino> destinos)
        {
            if (m is BlendTree arbol)
            {
                foreach (ChildMotion h in arbol.children)
                {
                    var sub = new List<(string, float)>(parametros);
                    if (arbol.blendType == BlendTreeType.Simple1D) { sub.Add((arbol.blendParameter, h.threshold)); }
                    else { sub.Add((arbol.blendParameter, h.position.x)); sub.Add((arbol.blendParameterY, h.position.y)); }
                    Recorrer(capa, estado, h.motion, sub, mapa, destinos);
                }
            }
            else if (m is AnimationClip c)
            {
                AnimationClip efectivo = mapa.TryGetValue(c, out AnimationClip o) ? o : c;
                if (!destinos.ContainsKey(efectivo.name))
                {
                    destinos[efectivo.name] = new Destino { Capa = capa, Estado = estado, Parametros = parametros };
                }
            }
        }

        private static float Fraccion(string clip)
        {
            if (clip.StartsWith("PJ01_Muerte_")) { return 0.9f; }
            if (clip.Contains("SaltoAterrizaje") || clip.Contains("SaltoInicio")) { return 0.4f; }
            if (clip.Contains("Telefono") && !clip.Contains("Ver")) { return 0.3f; }
            return 0.5f;
        }

        private static void Capturar(string clip, Destino d, AnimatorOverrideController over)
        {
            CapturaPersonajes.EscenaDePrueba();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CreadorVisualPersonaje.RutaPrefab("PJ01"));
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            foreach (MonoBehaviour mb in inst.GetComponentsInChildren<MonoBehaviour>(true)) { mb.enabled = false; }
            Animator a = inst.GetComponentInChildren<Animator>();
            a.runtimeAnimatorController = over;
            a.applyRootMotion = false;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            a.Rebind();
            // Los parámetros que dejan a la capa base en Idle y a las demás con su peso.
            for (int i = 0; i < a.layerCount; i++) { a.SetLayerWeight(i, i == d.Capa || i == 0 ? 1f : 0f); }
            foreach ((string p, float v) in d.Parametros) { a.SetFloat(p, v); }
            a.Play(d.Estado, d.Capa, Fraccion(clip));
            if (d.Capa != 0) { a.Play("Idle", 0, 0.3f); }
            a.Update(0f);
            a.Update(0.001f);
            Vector3 cadera = a.GetBoneTransform(HumanBodyBones.Hips).position;
            inst.transform.position += new Vector3(-cadera.x, 0f, -cadera.z);
            inst.transform.rotation = Quaternion.identity;
            // Cámara de 3/4: delante y a la derecha del personaje, cuerpo entero.
            Vector3 centro = new Vector3(0f, 0.9f, 0f);
            Camera c = CapturaPersonajes.ColocarCamara(centro - Vector3.up * 0.9f + Vector3.up * 0.15f, new Vector3(0.7f, 0f, 0.8f), 5.2f, 0.9f, 32f);
            CapturaPersonajes.Guardar(c, Carpeta + "/" + clip + ".png");
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MechaCyberpunk.Arte.Editor
{
    /// <summary>
    /// T-501: capturas de referencia con el editor en Play (Ciudad_Prologo abierta). Dos pasos para dar cuadros al juego entre medio:
    /// <c>Preparar</c> pone la hora del reloj, lleva al jugador al punto y guarda la pose de la cámara; <c>Capturar</c> renderiza 1920x1080
    /// con la cámara de captura (con posproceso) y guarda el PNG. Los tipos de Mundo se buscan por nombre (Arte.Editor no los referencia).
    /// </summary>
    public static class CapturaT501
    {
        private static Vector3 desde;
        private static Vector3 hacia;
        private static float fov = 60f;

        private static Component Buscar(string tipo)
        {
            return UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(m => m != null && m.GetType().Name == tipo);
        }

        private static void Llamar(Component c, string metodo, params object[] args)
        {
            if (c == null)
            {
                return;
            }
            c.GetType().GetMethod(metodo, BindingFlags.Public | BindingFlags.Instance)?.Invoke(c, args);
        }

        /// <summary>Hora (0-24), cámara en 'desde' mirando a 'hacia'. El jugador va a 'jugador' (para la lluvia y la silueta); y=-999 lo deja donde está.</summary>
        public static string Preparar(float hora, float dx, float dy, float dz, float hx, float hy, float hz, float campo, float jx, float jy, float jz, bool lluvia)
        {
            Component reloj = Buscar("RelojMundo");
            Llamar(reloj, "Pausar", true);
            Llamar(reloj, "AvanzarHasta", hora);
            Llamar(Buscar("CicloDiaNoche"), "Aplicar", hora);
            Type tipoLluvia = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("MechaCyberpunk.Mundo.Ambiente.LluviaSigueJugador")).FirstOrDefault(t => t != null);
            tipoLluvia?.GetProperty("Activa", BindingFlags.Public | BindingFlags.Static)?.SetValue(null, lluvia);
            if (jy > -900f)
            {
                GameObject jugador = GameObject.Find("JugadorAPie");
                if (jugador != null)
                {
                    CharacterController cc = jugador.GetComponent<CharacterController>();
                    if (cc != null) { cc.enabled = false; }
                    jugador.transform.position = new Vector3(jx, jy, jz);
                    jugador.transform.rotation = Quaternion.LookRotation(new Vector3(hx - jx, 0f, hz - jz));
                    if (cc != null) { cc.enabled = true; }
                }
            }
            desde = new Vector3(dx, dy, dz);
            hacia = new Vector3(hx, hy, hz);
            fov = campo;
            return "ok hora " + hora;
        }

        private static int cuadros;
        private static double inicio;
        private static string salida;
        private static float duracion;

        /// <summary>Cuenta cuadros en Play durante 'seg' segundos y escribe fps y ms en 'archivo' (la cámara del juego, no la de captura).</summary>
        public static string MedirFps(string archivo, float seg)
        {
            salida = archivo;
            duracion = seg;
            cuadros = 0;
            inicio = EditorApplication.timeSinceStartup;
            EditorApplication.update += ContarCuadros;
            return "midiendo";
        }

        private static void ContarCuadros()
        {
            cuadros++;
            double t = EditorApplication.timeSinceStartup - inicio;
            if (t >= duracion)
            {
                EditorApplication.update -= ContarCuadros;
                File.WriteAllText(salida, (cuadros / t).ToString("F1") + " fps, " + (1000.0 * t / cuadros).ToString("F2") + " ms, cuadros " + cuadros);
            }
        }

        public static string Capturar(string ruta)
        {
            GameObject go = new GameObject("CamaraT501");
            Camera cam = go.AddComponent<Camera>();
            UniversalAdditionalCameraData datos = go.AddComponent<UniversalAdditionalCameraData>();
            datos.renderPostProcessing = true;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1500f;
            cam.fieldOfView = fov;
            cam.transform.position = desde;
            cam.transform.LookAt(hacia);
            RenderTexture rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            foto.Apply();
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(foto);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(go);
            return "guardado " + ruta;
        }
    }
}

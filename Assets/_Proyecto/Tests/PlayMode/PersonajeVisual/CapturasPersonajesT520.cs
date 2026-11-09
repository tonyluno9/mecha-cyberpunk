using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-520: capturas 1920x1080 de cada personaje en Ciudad_Prologo (docs/capturas/T-520-N.png). [Explicit]: solo para revisar a ojo.
    /// 1 protagonista (mujer), 2 hombre, 3 aliado, 4 SKULLS, 5 jefe SKULLS, 6 vecina, 7 vendedor, 8 peatones, 9 DOC, 10 INVENTOR, 11 HACKER.
    /// Correr: -runTests -testPlatform PlayMode -testFilter MechaCyberpunk.Tests.CapturasPersonajesT520
    /// </summary>
    [Explicit("Solo para revisar capturas a ojo")]
    public class CapturasPersonajesT520
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const int Ancho = 1920;
        private const int Alto = 1080;
        private const float HoraCaptura = 21f;

        private Camera camara;
        private Light luz;

        private IEnumerator Cargar()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 90; i++) { yield return null; }
            GameObject c = new GameObject("CamaraCapturaT520");
            camara = c.AddComponent<Camera>();
            camara.fieldOfView = 38f;
            camara.nearClipPlane = 0.05f;
            camara.farClipPlane = 600f;
            camara.enabled = false;
            GameObject l = new GameObject("LuzCapturaT520");
            l.transform.SetParent(c.transform, false);
            luz = l.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.range = 14f;
            luz.intensity = 3.2f;
            luz.color = new Color(1f, 0.93f, 0.85f);
            luz.shadows = LightShadows.None;
            Directory.CreateDirectory("docs/capturas");
        }

        private static Transform Buscar(string nombre)
        {
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == nombre) { return t; }
            }
            return null;
        }

        private static void Activar(Transform t)
        {
            for (Transform p = t; p != null; p = p.parent) { p.gameObject.SetActive(true); }
        }

        private IEnumerator Foto(Vector3 objetivo, Vector3 delFrente, string archivo, float distancia = 3.3f, float altura = 1.25f, float mira = 0.95f)
        {
            Vector3 cabeza = objetivo + Vector3.up * mira;
            Vector3 deseada = objetivo + delFrente.normalized * distancia + Vector3.up * altura;
            // Se prueba desde varios lados (empezando por el de frente) hasta encontrar uno sin paredes ni mostradores entre la cámara y el personaje.
            float mejor = -1f;
            for (int paso = 0; paso < 12; paso++)
            {
                float grados = paso % 2 == 0 ? (paso / 2) * 30f : -((paso + 1) / 2) * 30f;
                for (float d = distancia; d >= 2.2f; d -= 0.7f)
                {
                    Vector3 candidata = objetivo + Quaternion.Euler(0f, grados, 0f) * delFrente.normalized * d + Vector3.up * altura;
                    // Los extremos quedan 0.55 m antes del personaje para no chocar con su propio collider.
                    Vector3 alCuerpo = objetivo + Vector3.up * 0.5f;
                    bool libre = !Physics.Linecast(candidata, cabeza + (candidata - cabeza).normalized * 0.55f, ~0, QueryTriggerInteraction.Ignore)
                        && !Physics.Linecast(candidata, alCuerpo + (candidata - alCuerpo).normalized * 0.55f, ~0, QueryTriggerInteraction.Ignore)
                        && !Physics.CheckSphere(candidata, 0.3f, ~0, QueryTriggerInteraction.Ignore);
                    float valor = libre ? 100f + d : 0f;
                    if (valor > mejor)
                    {
                        mejor = valor;
                        deseada = candidata;
                    }
                }
                if (mejor >= 100f + distancia - 0.01f) { break; }
            }
            luz.transform.position = deseada;
            camara.transform.position = deseada;
            camara.transform.LookAt(cabeza);
            yield return null;
            RenderTexture rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            foto.Apply();
            File.WriteAllBytes(Path.Combine("docs", "capturas", archivo), foto.EncodeToPNG());
            RenderTexture.active = null;
            camara.targetTexture = null;
            Object.Destroy(foto);
            rt.Release();
            Object.Destroy(rt);
        }

        private static Transform Visual(Transform raiz)
        {
            foreach (Animator a in raiz.GetComponentsInChildren<Animator>(true))
            {
                if (a.GetComponentInParent<MechaCyberpunk.Personaje.Visual.VisualPersonaje>() != null)
                {
                    return a.GetComponentInParent<MechaCyberpunk.Personaje.Visual.VisualPersonaje>().transform;
                }
            }
            return raiz;
        }

        [UnityTest]
        public IEnumerator Capturar_TodosLosPersonajes()
        {
            yield return Cargar();
            // Noche, como en la demo.
            Registro<IRelojMundo>.Actual?.AvanzarHasta(HoraCaptura);
            for (int i = 0; i < 20; i++) { yield return null; }

            // 1 y 2: el jugador, mujer y hombre.
            Transform jugador = Buscar("JugadorAPie");
            Assert.IsNotNull(jugador, "El jugador está en la escena");
            // A la calle principal (la misma de la medición de noche) para que se vea en un lugar con luz.
            CharacterController cc = jugador.GetComponent<CharacterController>();
            cc.enabled = false;
            jugador.SetPositionAndRotation(new Vector3(-30f, 0.25f, -2f), Quaternion.Euler(0f, 90f, 0f));
            cc.enabled = true;
            for (int i = 0; i < 15; i++) { yield return null; }
            Vector3 pies = jugador.position;
            Vector3 frente = jugador.forward;
            yield return Foto(pies, frente, "T-520-1.png");
            object perfil = Registro<IPerfilJugador>.Actual;
            System.Reflection.MethodInfo fijar = perfil.GetType().GetMethod("FijarApariencia");
            fijar.Invoke(perfil, new object[] { new AparienciaJugador(1, 0, 0) });
            for (int i = 0; i < 10; i++) { yield return null; }
            yield return Foto(pies, frente, "T-520-2.png");
            fijar.Invoke(perfil, new object[] { new AparienciaJugador(0, 0, 0) });
            for (int i = 0; i < 5; i++) { yield return null; }

            // 12: apuntando con la pistola y con el arma larga (IK de las manos), de lado.
            MechaCyberpunk.Personaje.Visual.VisualPersonaje vp = Visual(jugador).GetComponent<MechaCyberpunk.Personaje.Visual.VisualPersonaje>();
            vp.LeerDelPadre = false;
            vp.Apuntando = true;
            vp.TipoArma = MechaCyberpunk.Personaje.Visual.TipoArmaVisual.Pistola;
            for (int i = 0; i < 30; i++) { yield return null; }
            yield return Foto(pies, jugador.right, "T-520-12.png", 3f, 1.2f, 1.0f);
            vp.TipoArma = MechaCyberpunk.Personaje.Visual.TipoArmaVisual.ArmaLarga;
            for (int i = 0; i < 30; i++) { yield return null; }
            yield return Foto(pies, jugador.right, "T-520-13.png", 3f, 1.2f, 1.0f);
            vp.Apuntando = false;
            vp.TipoArma = MechaCyberpunk.Personaje.Visual.TipoArmaVisual.Ninguna;
            vp.LeerDelPadre = true;

            // 4 y 5: SKULLS y su jefe.
            Transform skulls = Buscar("Pandillero_1");
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t.name == "Pandillero_1" && t.parent != null && t.parent.name.StartsWith("Pandilla_Calaveras")) { skulls = t; break; }
            }
            Activar(skulls);
            for (int i = 0; i < 10; i++) { yield return null; }
            yield return Foto(skulls.position, skulls.forward, "T-520-4.png");
            Transform jefe = Buscar("PuntoJefe_tianguis").Find("Jefe");
            Activar(jefe);
            for (int i = 0; i < 10; i++) { yield return null; }
            yield return Foto(jefe.position, jefe.forward, "T-520-5.png", 3.8f, 1.4f, 1.05f);

            // 6, 7, 9, 10, 11: vecina, vendedor y los apoyos.
            yield return FotoDe("DonaLupe", "T-520-6.png");
            yield return FotoDe("Vendedor", "T-520-7.png");
            yield return FotoDe("Apoyo_JOEL", "T-520-9.png");
            yield return FotoDe("Apoyo_TINO", "T-520-10.png");
            yield return FotoDe("Apoyo_SERGIO", "T-520-11.png");

            // 8: cuatro peatones de las cuatro variantes en fila.
            GameObject prefabPeaton = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Trafico/Prefabs/Peaton.prefab");
            Vector3 origen = pies + frente * 6f + jugador.right * -2.4f;
            for (int i = 0; i < 4; i++)
            {
                GameObject p = Object.Instantiate(prefabPeaton, origen + jugador.right * (1.6f * i), Quaternion.LookRotation(frente));
                p.name = "PeatonCaptura" + i;
                p.GetComponent<MechaCyberpunk.Personaje.Visual.PeatonVariado>().Elegir(false); // sin azar: siempre la persona, no el robot
                MechaCyberpunk.Personaje.Visual.VarianteVisual v = p.GetComponentInChildren<MechaCyberpunk.Personaje.Visual.VarianteVisual>();
                var materiales = new Material[4];
                for (int k = 0; k < 4; k++) { materiales[k] = AssetDatabase.LoadAssetAtPath<Material>("Assets/Arte/Personajes/Materiales/woman2_Peaton" + (k + 1) + ".mat"); }
                v.Configurar(materiales, i, null);
                v.Aplicar();
                p.GetComponent("PeatonMundo").gameObject.SetActive(true);
            }
            for (int i = 0; i < 10; i++) { yield return null; }
            yield return Foto(origen + jugador.right * 2.4f, frente, "T-520-8.png", 5.5f, 1.4f, 0.9f);
        }

        private IEnumerator FotoDe(string nombre, string archivo)
        {
            Transform t = Buscar(nombre);
            Assert.IsNotNull(t, nombre);
            Transform v = Visual(t);
            // Se mira desde el lado al que el personaje está de frente, o desde donde haya espacio.
            yield return Foto(v.position, v.forward, archivo);
        }
    }
}

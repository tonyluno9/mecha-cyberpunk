using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Tests.Personaje;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// T-744: los dos primeros aliados de la pandilla son AL10 y AL11 (Tripo, Humanoid). Con el jugador real y entrada simulada: te siguen caminando animados
    /// (no en T-pose), miden 1.80 m animados y no se estiran ni se rompen al disparar. Las capturas salen a docs/capturas/s1e-T-744-*.png.
    /// </summary>
    public class AliadosAL10AL11T744Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string CarpetaCapturas = "docs/capturas/";

        private class TerritorioFalso : ITerritorio
        {
            public IReadOnlyList<string> Zonas => new[] { "fosa" };
            public string ZonaEn(Vector3 posicion) => "fosa";
            public string Nombre(string zona) => "SUBURBIOS";
            public IdPandilla Dueno(string zona) => IdPandilla.Jugador;
            public int Respeto(string zona) => 10;
            public bool JefeDisponible(string zona) => false;
            public void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe) { }
            public void RegistrarCaida(Vector3 posicion) { }
            public event Action<string> Cambio { add { } remove { } }
            public event Action<string, IdPandilla> Conquistada { add { } remove { } }
        }

        private Keyboard teclado;
        private GameObject piso;
        private GameObject jugador;
        private GameObject servicios;
        private GameObject pandillero;
        private PandillaJugador pandilla;
        private Func<float> azarAnterior;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            azarAnterior = PistolaPandillero.Azar;
            PistolaPandillero.Azar = () => 0f;
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            Registro<ITerritorio>.Agregar(new TerritorioFalso());
            servicios = new GameObject("ServiciosAliados");
            servicios.SetActive(false);
            pandilla = servicios.AddComponent<PandillaJugador>();
            var so = new SerializedObject(pandilla);
            so.FindProperty("prefabAliado").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/IA/Prefabs/Aliado.prefab");
            so.FindProperty("prefabAliadoSegundo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/IA/Prefabs/Aliado_AL11.prefab");
            so.ApplyModifiedPropertiesWithoutUndo();
            servicios.SetActive(true);
        }

        public override void TearDown()
        {
            PistolaPandillero.Azar = azarAnterior;
            foreach (AliadoAPie a in AliadoAPie.Activos.ToArray())
            {
                UnityEngine.Object.Destroy(a.gameObject);
            }
            UnityEngine.Object.Destroy(pandillero);
            UnityEngine.Object.Destroy(servicios);
            UnityEngine.Object.Destroy(jugador);
            UnityEngine.Object.Destroy(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static AliadoAPie Aliado(string nombre)
        {
            foreach (AliadoAPie a in AliadoAPie.Activos)
            {
                if (a.name == "Aliado_" + nombre) { return a; }
            }
            return null;
        }

        private static string MaterialDe(AliadoAPie a) => a.GetComponentInChildren<SkinnedMeshRenderer>(true).sharedMaterial.name;

        /// <summary>En T-pose las manos quedan a la altura de los hombros; animado de pie o caminando cuelgan por debajo.</summary>
        private static float CaidaDeLaMano(AliadoAPie a)
        {
            Animator animador = a.GetComponentInChildren<Animator>(true);
            Transform hombro = animador.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform mano = animador.GetBoneTransform(HumanBodyBones.LeftHand);
            return hombro.position.y - mano.position.y;
        }

        private IEnumerator ReclutarDos()
        {
            yield return Esperar(0.5f);
            Assert.IsTrue(pandilla.Reclutar("A"));
            Assert.IsTrue(pandilla.Reclutar("B"));
            yield return Esperar(1.5f);
            Assert.AreEqual(2, AliadoAPie.Activos.Count);
            foreach (AliadoAPie a in AliadoAPie.Activos)
            {
                a.GetComponentInChildren<Animator>(true).cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        [UnityTest]
        public IEnumerator PrimerAliadoEsAL10_SegundoEsAL11_Humanoid_Y_MidenUnoOchenta()
        {
            yield return ReclutarDos();
            Assert.AreEqual("AL10_Base", MaterialDe(Aliado("A")));
            Assert.AreEqual("AL11_Base", MaterialDe(Aliado("B")));
            foreach (string n in new[] { "A", "B" })
            {
                AliadoAPie a = Aliado(n);
                Animator animador = a.GetComponentInChildren<Animator>(true);
                Assert.IsTrue(animador.avatar != null && animador.avatar.isHuman && animador.avatar.isValid, n + ": avatar Humanoid válido");
                yield return Esperar(0.5f);
                float alto = EscalaJugadorT670Pruebas.AlturaAnimada(a.gameObject);
                Debug.Log("T744 altura animada " + a.name + " " + alto.ToString("F3"));
                Assert.AreEqual(1.8f, alto, 0.06f, n + ": mide 1.80 m animado");
                Assert.Greater(CaidaDeLaMano(a), 0.3f, n + ": no está en T-pose");
            }
        }

        [UnityTest]
        public IEnumerator Aliados_SiguenAlJugador_CaminandoAnimados()
        {
            yield return ReclutarDos();
            Press(teclado.wKey);
            yield return Esperar(2.5f);
            Transform raiz = Registro<IJugador>.Actual.Raiz;
            foreach (string n in new[] { "A", "B" })
            {
                AliadoAPie a = Aliado(n);
                Animator animador = a.GetComponentInChildren<Animator>(true);
                Assert.Greater(animador.GetFloat("Velocidad"), 0.5f, n + ": camina (parámetro Velocidad)");
                Assert.Greater(CaidaDeLaMano(a), 0.3f, n + ": caminando no está en T-pose");
            }
            yield return Capturar("s1e-T-744-caminando");
            yield return Esperar(1.5f);
            Release(teclado.wKey);
            yield return Esperar(3f);
            foreach (string n in new[] { "A", "B" })
            {
                Vector3 d = Aliado(n).transform.position - raiz.position;
                d.y = 0f;
                Assert.Less(d.magnitude, 8f, n + ": te sigue");
            }
            yield return Esperar(1f);
            Assert.Greater(CaidaDeLaMano(Aliado("A")), 0.3f, "Parado no está en T-pose");
            yield return Capturar("s1e-T-744-parados");
        }

        [UnityTest]
        public IEnumerator Aliados_Disparan_SinRomperseElModelo()
        {
            yield return ReclutarDos();
            Transform raiz = Registro<IJugador>.Actual.Raiz;
            pandillero = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero),
                raiz.position + raiz.forward * 10f + Vector3.up * 0.05f, Quaternion.identity);
            var so = new SerializedObject(pandillero.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
            so.ApplyModifiedPropertiesWithoutUndo();
            PandilleroAPie rival = pandillero.GetComponent<PandilleroAPie>();
            float vidaAntes = rival.Actual;
            float fin = Time.time + 8f;
            bool capturado = false;
            while (Time.time < fin && rival.Actual >= vidaAntes)
            {
                AliadoAPie a = Aliado("A");
                if (!capturado && a.GetComponentInChildren<Animator>(true).GetBool("Apuntando"))
                {
                    capturado = true;
                    yield return Esperar(0.3f);
                    yield return Capturar("s1e-T-744-disparando");
                }
                yield return null;
            }
            Assert.Less(rival.Actual, vidaAntes, "Los aliados le disparan al pandillero.");
            Assert.AreEqual(1.8f, EscalaJugadorT670Pruebas.AlturaAnimada(Aliado("A").gameObject), 0.08f, "Disparando conserva su altura");
        }

        // Foto con una cámara aparte: el jugador PJ01 de pie junto a los dos aliados.
        private IEnumerator Capturar(string nombre)
        {
            Transform raiz = Registro<IJugador>.Actual.Raiz;
            Vector3 centro = raiz.position + Vector3.up * 1f;
            foreach (AliadoAPie a in AliadoAPie.Activos) { centro += a.transform.position + Vector3.up; }
            centro /= 1 + AliadoAPie.Activos.Count;
            GameObject luzGo = new GameObject("LuzFoto");
            Light luz = luzGo.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.intensity = 1.5f;
            luzGo.transform.rotation = Quaternion.Euler(40f, 150f, 0f);
            GameObject camGo = new GameObject("CamFoto");
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.25f, 0.28f, 0.33f);
            cam.fieldOfView = 40f;
            Vector3 direccion = (raiz.forward + raiz.right * 0.6f).normalized;
            camGo.transform.position = centro + direccion * 7f + Vector3.up * 0.3f;
            camGo.transform.rotation = Quaternion.LookRotation(centro - camGo.transform.position);
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            yield return null;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            Directory.CreateDirectory(CarpetaCapturas);
            File.WriteAllBytes(CarpetaCapturas + nombre + ".png", tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
            UnityEngine.Object.Destroy(rt);
            UnityEngine.Object.Destroy(camGo);
            UnityEngine.Object.Destroy(luzGo);
        }
    }
}

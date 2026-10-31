using System;
using System.Collections;
using MechaCyberpunk.Combate.Lectura;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-424: al apuntar con clic derecho la cámara se jala suave hacia un enemigo cerca de la mira y la retícula lo marca.</summary>
    public class ApuntadoAsistidoPruebas : BaseJugadorAPie
    {
        private class OpcionesFalsas : IOpciones
        {
            public NivelApuntadoAsistido Nivel;
            public float Sensibilidad => 1f;
            public bool InvertirY => false;
            public NivelApuntadoAsistido ApuntadoAsistido => Nivel;
            public event Action Cambio { add { } remove { } }
        }

        private class EnemigoFalso : MonoBehaviour, ILecturaRival, IVida
        {
            public string Nombre => "Enemigo";
            public Transform Raiz => transform;
            public IVida Vida => this;
            public bool EsMecha => false;
            public bool Preparando => false;
            public float SegundosParaAtacar => -1f;
            public TipoAtaqueLeido Proximo => TipoAtaqueLeido.Golpe;
            public float Actual => 100f;
            public float Maxima => 100f;
            public bool EstaKO => false;
            public event Action Cambio { add { } remove { } }
            public event Action KO { add { } remove { } }
            private void OnEnable() => Registro<ILecturaRival>.Agregar(this);
            private void OnDisable() => Registro<ILecturaRival>.Quitar(this);
        }

        private OpcionesFalsas opciones;
        private GameObject enemigo;

        private void Preparar(NivelApuntadoAsistido nivel, Vector3 posicion)
        {
            opciones = new OpcionesFalsas { Nivel = nivel };
            Registro<IOpciones>.Agregar(opciones);
            enemigo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemigo.transform.position = posicion;
            enemigo.AddComponent<EnemigoFalso>();
        }

        private float AnguloAlEnemigo()
        {
            Camera camara = jugador.GetComponentInChildren<Camera>();
            Vector3 hacia = enemigo.transform.position + Vector3.up * 1.2f - camara.transform.position;
            return Vector3.Angle(camara.transform.forward, hacia);
        }

        [TearDown]
        public void LimpiarEnemigo()
        {
            if (enemigo != null)
            {
                Object.Destroy(enemigo);
            }
        }

        // Los canvas Overlay no salen al renderizar una cámara: se pasan un momento a Screen Space Camera (como CapturaUI de las pruebas de UI).
        private static void Capturar(string ruta, Camera camara)
        {
            const int ancho = 1920;
            const int alto = 1080;
            var cambiados = new System.Collections.Generic.List<Canvas>();
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    c.renderMode = RenderMode.ScreenSpaceCamera;
                    c.worldCamera = camara;
                    c.planeDistance = camara.nearClipPlane + 0.01f;
                    cambiados.Add(c);
                }
            }
            var rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
            var foto = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            RenderTexture anterior = RenderTexture.active;
            camara.targetTexture = rt;
            camara.aspect = (float)ancho / alto;
            Canvas.ForceUpdateCanvases();
            camara.Render();
            RenderTexture.active = rt;
            foto.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            foto.Apply();
            System.IO.File.WriteAllBytes(ruta, foto.EncodeToPNG());
            camara.targetTexture = null;
            RenderTexture.active = anterior;
            foreach (Canvas c in cambiados)
            {
                c.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            Object.Destroy(rt);
            Object.Destroy(foto);
        }

        [UnityTest]
        public IEnumerator Apuntando_ConEnemigoCerca_LaCamaraSeAcercaSuaveYLaMiraLoMarca()
        {
            yield return Calentar();
            Preparar(NivelApuntadoAsistido.Fuerte, new Vector3(2.5f, 1f, 18f));
            yield return Esperar(0.1f);
            float antes = AnguloAlEnemigo();
            ApuntadoAsistido asistido = jugador.GetComponent<ApuntadoAsistido>();
            Press(raton.rightButton);
            yield return Esperar(0.2f);
            Assert.IsTrue(asistido.HayBlanco, "El enemigo cerca de la mira se marca");
            Assert.Greater(AnguloAlEnemigo(), antes * 0.3f, "En 0.2 s no salta al blanco");
            yield return Esperar(1.5f);
            Assert.Less(AnguloAlEnemigo(), antes * 0.5f, "Con el tiempo la cámara se acerca al enemigo");
            if (Environment.GetEnvironmentVariable("CAPTURAR_T424") == "1")
            {
                Capturar(System.IO.Path.Combine("docs", "capturas", "T-424-1.png"), jugador.GetComponentInChildren<Camera>());
            }
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Apagado_NoJala()
        {
            yield return Calentar();
            Preparar(NivelApuntadoAsistido.Apagado, new Vector3(2.5f, 1f, 18f));
            yield return Esperar(0.1f);
            float antes = AnguloAlEnemigo();
            Press(raton.rightButton);
            yield return Esperar(1f);
            Assert.AreEqual(antes, AnguloAlEnemigo(), 0.5f, "Con el asistido apagado la cámara no se mueve sola");
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator ConMuroEnMedio_NoHayBlancoNiJalon()
        {
            yield return Calentar();
            Preparar(NivelApuntadoAsistido.Fuerte, new Vector3(2.5f, 1f, 18f));
            Caja(new Vector3(1.5f, 1.5f, 10f), new Vector3(8f, 3f, 0.5f));
            yield return Esperar(0.1f);
            float antes = AnguloAlEnemigo();
            Press(raton.rightButton);
            yield return Esperar(1f);
            Assert.IsFalse(jugador.GetComponent<ApuntadoAsistido>().HayBlanco);
            Assert.AreEqual(antes, AnguloAlEnemigo(), 0.5f);
            Release(raton.rightButton);
        }
    }
}

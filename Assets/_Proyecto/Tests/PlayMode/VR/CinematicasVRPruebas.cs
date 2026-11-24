using System;
using System.Collections;
using System.Reflection;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Cinematicas;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// V12-B: en VR la cabeza nunca la mueve una cinemática. Las cámaras de cinemática no van al visor: se dibujan en una textura que sale en
    /// <see cref="PantallaCineVR"/>, y al apagarse se quitan y recuperan su targetTexture. Los videos de cinemática salen en la misma pantalla.
    /// Casos reales sin la ciudad: la silla del Doc (<see cref="SillaClinica"/>) y la cinemática pendiente (CinematicaPendiente, por reflexión:
    /// este ensamblado no referencia MechaCyberpunk.Progresion.Recorrido).
    /// </summary>
    public class CinematicasVRPruebas : BaseJugadorVR
    {
        private const string RutaVideo = "Assets/Arte/Video/videologo.mp4";
        private const string RutaConfigSilla = "Assets/_Proyecto/Progresion/Apoyo/Datos/ConfigSillaClinica_Base.asset";
        private const float ToleranciaCabeza = 0.01f;
        private const float SegundosEsperaVideo = 8f;

        private static readonly Type TipoDatosCinematica =
            Type.GetType("MechaCyberpunk.Progresion.Recorrido.DatosCinematica, MechaCyberpunk.Progresion.Recorrido");
        private static readonly Type TipoCinematica =
            Type.GetType("MechaCyberpunk.Progresion.Recorrido.CinematicaPendiente, MechaCyberpunk.Progresion.Recorrido");

        private sealed class CarteraDoble : ICartera
        {
            public int Dinero { get; set; }
            public event Action<int, MotivoDinero> Cambio;
            public bool PuedePagar(int cantidad) => cantidad > 0 && Dinero >= cantidad;

            public bool IntentarPagar(int cantidad, MotivoDinero motivo)
            {
                if (!PuedePagar(cantidad))
                {
                    return false;
                }
                Dinero -= cantidad;
                Cambio?.Invoke(-cantidad, motivo);
                return true;
            }

            public void Abonar(int cantidad, MotivoDinero motivo)
            {
                Dinero += cantidad;
                Cambio?.Invoke(cantidad, motivo);
            }

            public void CargarDeuda(int cantidad, MotivoDinero motivo) => Abonar(-cantidad, motivo);
        }

        public override void Setup()
        {
            base.Setup();
            CamarasCineVR.Asegurar();
            VideosCineVR.Asegurar();
        }

        public override void TearDown()
        {
            // La cinemática apaga toda la entrada y la vuelve a prender al irse: se va ya, antes de que la base apague la entrada VR.
            GameObject cinematica = GameObject.Find("CinematicaPendiente");
            if (cinematica != null)
            {
                Object.DestroyImmediate(cinematica);
            }
            base.TearDown();
        }

        // ---------- Regla ----------

        [Test]
        public void ReglaCineVR_DecideQueCamaraEsDeCinematica()
        {
            Assert.IsTrue(ReglaCineVR.EsCamaraDeCine(false, false, false, true, true, false, 1f, 0f), "con jugador, encima de su cámara: cinemática");
            Assert.IsTrue(ReglaCineVR.EsCamaraDeCine(false, false, false, true, true, false, 0f, 0f), "a la misma profundidad también");
            Assert.IsFalse(ReglaCineVR.EsCamaraDeCine(false, false, false, true, true, false, -1f, 0f), "debajo de la del jugador no se ve en PC");
            Assert.IsFalse(ReglaCineVR.EsCamaraDeCine(true, false, false, true, true, true, 5f, 0f), "la cabeza nunca");
            Assert.IsFalse(ReglaCineVR.EsCamaraDeCine(false, true, false, true, true, true, 5f, 0f), "excluida nunca");
            Assert.IsFalse(ReglaCineVR.EsCamaraDeCine(false, false, true, true, true, false, 5f, 0f), "con su textura (minimapa, retrato) no");
            Assert.IsFalse(ReglaCineVR.EsCamaraDeCine(false, false, false, false, true, false, 5f, 0f), "Overlay de URP no");
            Assert.IsFalse(ReglaCineVR.EsCamaraDeCine(false, false, false, true, false, false, 5f, 0f), "sin jugador (menús) la cámara de la escena no");
            Assert.IsTrue(ReglaCineVR.EsCamaraDeCine(false, false, false, true, false, true, -1f, 0f), "la forzada (flashback) sí");
            Assert.IsTrue(ReglaCineVR.DibujaEnCamara(VideoRenderMode.CameraNearPlane));
            Assert.IsTrue(ReglaCineVR.DibujaEnCamara(VideoRenderMode.CameraFarPlane));
            Assert.IsFalse(ReglaCineVR.DibujaEnCamara(VideoRenderMode.RenderTexture));
            Assert.IsTrue(ReglaCineVR.VideoAlCine(true, false, true, false, true));
            Assert.IsFalse(ReglaCineVR.VideoAlCine(true, false, false, true, true), "parado no");
            Assert.IsFalse(ReglaCineVR.VideoAlCine(false, false, true, true, true), "sin jugador VR no (Inicio lo maneja aparte)");
            Assert.IsFalse(ReglaCineVR.VideoAlCine(true, false, true, false, false), "una pantalla del mundo no");
        }

        // ---------- Cámaras ----------

        [UnityTest]
        public IEnumerator CamaraDeCinematica_NoVaAlVisor_SaleEnLaPantallaYLaCabezaNoSeMueve()
        {
            yield return ArmarVR();
            Camera cabeza = adaptador.Camara;
            Vector3 posAntes = cabeza.transform.position;
            Quaternion rotAntes = cabeza.transform.rotation;

            Camera cine = NuevaCamara("CamaraCinematicaPrueba", cabeza.depth + 1f);
            yield return Cuadros(3);

            Assert.IsFalse(SistemaVR.VaAlVisor(cine), "La cámara de la cinemática no va al visor.");
            Assert.IsTrue(SistemaVR.VaAlVisor(cabeza), "El visor sigue con la cabeza del jugador.");
            Assert.IsTrue(CamarasCineVR.EstaRedirigida(cine), "Se manda a la pantalla de cine.");
            Assert.IsNotNull(cine.targetTexture, "Dibuja en una textura.");
            Assert.IsTrue(PantallaCineVR.Visible, "La pantalla de cine se ve.");
            Assert.AreSame(cine.targetTexture, PantallaCineVR.TexturaActual, "La pantalla muestra lo que ve la cámara de la cinemática.");
            float distancia = Vector3.Distance(PantallaCineVR.Centro, cabeza.transform.position);
            Debug.Log("[VR] Pantalla de cine a " + distancia.ToString("0.00") + " m de la cabeza.");
            Assert.AreEqual(PantallaCineVR.Distancia, distancia, 0.5f, "La pantalla queda a ~4 m frente a la cabeza.");

            // La cinemática mueve su cámara (como el asalto o el flashback): la cabeza no se entera.
            for (int i = 0; i < 30; i++)
            {
                cine.transform.position += new Vector3(0.2f, 0.05f, 0.1f);
                cine.transform.rotation *= Quaternion.Euler(0f, 3f, 1f);
                yield return null;
            }
            Assert.Less(Vector3.Distance(posAntes, cabeza.transform.position), ToleranciaCabeza, "La cabeza no se mueve.");
            Assert.Less(Quaternion.Angle(rotAntes, cabeza.transform.rotation), 0.5f, "La cabeza no gira.");
            Assert.IsFalse(SistemaVR.VaAlVisor(cine));
        }

        [UnityTest]
        public IEnumerator CamaraDeCinematica_AlApagarse_SeQuitaDeLaPantallaYRecuperaSuTextura()
        {
            yield return ArmarVR();
            Camera cine = NuevaCamara("CamaraCinematicaPrueba", adaptador.Camara.depth + 5f);
            yield return Cuadros(3);
            RenderTexture textura = cine.targetTexture;
            Assert.IsNotNull(textura);
            Assert.IsTrue(PantallaCineVR.Tiene(cine));

            cine.enabled = false;
            yield return Cuadros(2);
            Assert.IsNull(cine.targetTexture, "Recupera su targetTexture (null).");
            Assert.IsFalse(PantallaCineVR.Tiene(cine), "Se quita de la pantalla de cine.");
            Assert.IsFalse(CamarasCineVR.EstaRedirigida(cine));
            Assert.IsFalse(PantallaCineVR.Visible, "Sin cinemática la pantalla se apaga.");

            // Se vuelve a prender (otra toma): otra vez al cine.
            cine.enabled = true;
            yield return Cuadros(2);
            Assert.IsTrue(CamarasCineVR.EstaRedirigida(cine));
            Assert.IsFalse(SistemaVR.VaAlVisor(cine));

            // Se destruye (SillaClinica, AsaltoDinero): la pantalla se quita.
            Object.Destroy(cine.gameObject);
            yield return Cuadros(2);
            Assert.IsFalse(PantallaCineVR.Visible, "Al destruirse la cámara la pantalla se quita.");
            Assert.AreEqual(0, CamarasCineVR.Actual.Redirigidas);
        }

        [UnityTest]
        public IEnumerator CamaraConTexturaPropia_NoSeToca()
        {
            yield return ArmarVR();
            Camera retrato = NuevaCamara("CamaraRetratoPrueba", adaptador.Camara.depth + 3f);
            var propia = new RenderTexture(64, 64, 16);
            retrato.targetTexture = propia;
            yield return Cuadros(3);
            Assert.AreSame(propia, retrato.targetTexture, "Una cámara con su textura (minimapa, retrato, holograma) sigue igual.");
            Assert.IsFalse(CamarasCineVR.EstaRedirigida(retrato));
            Assert.IsFalse(PantallaCineVR.Tiene(retrato));
            retrato.targetTexture = null;
            propia.Release();
            Object.Destroy(propia);
        }

        [UnityTest]
        public IEnumerator SinJugador_LaCamaraDeLaEscenaNoSeToca_SalvoLaForzada()
        {
            // Sin jugador (Inicio, Menu_Calles, creación): la cámara de la escena es el fondo del menú y la maneja Inicio.
            yield return Cuadros(2);
            Camera escena = NuevaCamara("CamaraEscenaPrueba", 5f);
            yield return Cuadros(3);
            Assert.IsNull(escena.targetTexture, "Sin jugador la cámara de la escena no se manda al cine.");
            Assert.IsFalse(CamarasCineVR.EstaRedirigida(escena));
            Assert.IsFalse(SistemaVR.VaAlVisor(escena), "Pero tampoco va al visor.");

            // La del flashback (o una forzada) sí.
            CamarasCineVR.Actual.Forzar(escena);
            yield return Cuadros(2);
            Assert.IsTrue(CamarasCineVR.EstaRedirigida(escena), "La forzada va a la pantalla de cine.");
            Assert.AreSame(escena.targetTexture, PantallaCineVR.TexturaActual);
            Object.Destroy(escena.gameObject);
            yield return Cuadros(2);
            Assert.IsFalse(PantallaCineVR.Visible);
        }

        // ---------- Videos ----------

        [UnityTest]
        public IEnumerator VideoEnPlanoDeCamara_VaAUnaTextura_YAlApagarseVuelveASuModo()
        {
            yield return ArmarVR();
            VideoClip clip = CargarVideo();
            VideoPlayer video = NuevoVideo(clip, VideoRenderMode.CameraNearPlane);
            video.targetCamera = adaptador.Camara;
            VideosCineVR.Actual.BarrerAhora();
            yield return Cuadros(2);
            Assert.AreEqual(VideoRenderMode.RenderTexture, video.renderMode, "En VR el video no se dibuja pegado a la cara.");
            Assert.IsNotNull(video.targetTexture);
            Assert.IsTrue(VideosCineVR.Redirigido(video));

            video.enabled = false;
            yield return Cuadros(2);
            Assert.AreEqual(VideoRenderMode.CameraNearPlane, video.renderMode, "Al apagarse vuelve a su modo.");
            Assert.IsNull(video.targetTexture);
            Assert.IsFalse(VideosCineVR.Redirigido(video));
        }

        [UnityTest]
        public IEnumerator VideoDeCinematica_SaleEnLaPantallaDeCine()
        {
            yield return ArmarVR();
            VideoClip clip = CargarVideo();
            VideoPlayer video = NuevoVideo(clip, VideoRenderMode.CameraNearPlane);
            video.targetCamera = adaptador.Camara;
            VideosCineVR.Actual.BarrerAhora();
            yield return Cuadros(2);
            video.Play();
            yield return EsperarVideo(video);
            yield return Cuadros(2);
            Assert.IsTrue(VideosCineVR.EnCine(video), "El video sale en la pantalla de cine.");
            Assert.AreSame(video.targetTexture, PantallaCineVR.TexturaActual);

            video.Stop();
            yield return Cuadros(2);
            Assert.IsFalse(PantallaCineVR.Tiene(video), "Al parar se quita de la pantalla.");
        }

        // ---------- Casos reales ----------

        [UnityTest]
        public IEnumerator SillaDelDoc_LaCamaraCercanaSaleEnLaPantalla_YLaCabezaNoSeAnima()
        {
            yield return ArmarVR();
            ConfigSillaClinica config = AssetDatabase.LoadAssetAtPath<ConfigSillaClinica>(RutaConfigSilla);
            Assert.IsNotNull(config, "Existe la config de la silla.");
            if (Registro<ICartera>.Actual == null)
            {
                Registro<ICartera>.Agregar(new CarteraDoble { Dinero = 500 });
            }
            SillaClinica silla = CrearSilla(config);
            extras.Add(silla.gameObject);
            yield return Cuadros(2);
            Assert.IsTrue(silla.PuedeUsar, "Sano, la silla se ofrece como revisión.");

            silla.Usar();
            yield return Cuadros(3);
            Assert.IsTrue(silla.EnSecuencia);
            Camera cercana = silla.CamaraCercana;
            Assert.IsNotNull(cercana, "La silla prende su cámara cercana.");
            Assert.IsFalse(SistemaVR.VaAlVisor(cercana), "La cámara cercana no va al visor.");
            Assert.IsTrue(CamarasCineVR.EstaRedirigida(cercana), "Sale en la pantalla de cine.");
            Assert.AreSame(cercana.targetTexture, PantallaCineVR.TexturaActual);
            Assert.IsTrue(SistemaVR.VaAlVisor(adaptador.Camara), "El visor sigue con la cabeza del jugador.");

            // Sentado (la pose es de huesos y el brazo baja): la cabeza no la mueve la secuencia.
            yield return Segundos(0.6f);
            Vector3 cabezaSentado = adaptador.Camara.transform.position;
            yield return Segundos(1.2f);
            Assert.Less(Vector3.Distance(cabezaSentado, adaptador.Camara.transform.position), 0.1f, "La cabeza no se anima en la silla.");

            float limite = Time.realtimeSinceStartup + 15f;
            while (silla.EnSecuencia && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsFalse(silla.EnSecuencia, "La secuencia de la silla termina.");
            extras.Add(GameObject.Find("PantallaApoyo"));
            yield return Cuadros(2);
            Assert.AreEqual(0, CamarasCineVR.Actual.Redirigidas, "Al levantarse la cámara cercana se quita.");
            Assert.IsFalse(PantallaCineVR.Visible, "Y la pantalla de cine se apaga.");
        }

        [UnityTest]
        public IEnumerator CinematicaPendiente_SinVideo_NoMueveLaCabezaNiPrendeOtraCamaraEnElVisor()
        {
            yield return ArmarVR();
            Camera cabeza = adaptador.Camara;
            Vector3 recienArmado = cabeza.transform.position;
            yield return Segundos(0.5f); // la altura se calibra sola al armarse (sube a 1.72 m): se mide ya calibrada
            Vector3 posAntes = cabeza.transform.position;
            Debug.Log("[VR] Cabeza: al armarse " + recienArmado.ToString("F2") + ", calibrada " + posAntes.ToString("F2") + ".");
            ScriptableObject datos = NuevaCinematica("C99", 1.5f, null);
            Reproducir(datos);
            Assert.IsTrue(CinematicaEnCurso(), "La cinemática empezó.");
            extras.Add(GameObject.Find("CinematicaPendiente"));
            yield return Segundos(0.9f);

            Canvas lienzo = BuscarLienzo("CanvasCinematica");
            Assert.IsNotNull(lienzo, "La pantalla negra de CINEMÁTICA existe.");
            TipoLienzoVR? tipo = MenusVR.Actual != null ? MenusVR.Actual.TipoDe(lienzo) : null;
            Debug.Log("[VR] CanvasCinematica en VR: " + (tipo.HasValue ? tipo.Value.ToString() : "sin convertir")
                + ", a " + Vector3.Distance(lienzo.transform.position, cabeza.transform.position).ToString("0.00") + " m de la cabeza.");
            Assert.IsTrue(tipo.HasValue, "La pantalla negra de texto pasa a World Space (se ve en el visor).");
            Assert.AreEqual(RenderMode.WorldSpace, lienzo.renderMode);
            foreach (Camera c in Camera.allCameras)
            {
                if (c != cabeza)
                {
                    Assert.IsFalse(SistemaVR.VaAlVisor(c), "Solo la cabeza va al visor: " + c.name);
                }
            }
            Debug.Log("[VR] Cabeza en la cinemática: " + cabeza.transform.position.ToString("F2") + ".");
            Assert.Less(Vector3.Distance(posAntes, cabeza.transform.position), ToleranciaCabeza, "La cabeza no se mueve en la cinemática.");
            // El seguimiento de la cabeza sigue vivo en la cinemática (antes se apagaba con todas las acciones y la vista se pegaba a la cara).
            Vector3 mirarAntes = cabeza.transform.forward;
            PonerCabeza(new Vector3(0f, 1.6f, 0f), Quaternion.Euler(0f, 40f, 0f));
            yield return Cuadros(3);
            Assert.Greater(Vector3.Angle(mirarAntes, cabeza.transform.forward), 30f, "La cabeza real sigue moviendo la vista en la cinemática.");

            // B salta la cinemática en VR (con teclado es ENTER).
            int saltadas = MechaCyberpunk.VR.Cinematicas.SaltarCinematicaVR.Saltadas;
            yield return Pulsar(controlDer.secondaryButton);
            float limite = Time.realtimeSinceStartup + 5f;
            while (CinematicaEnCurso() && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.AreEqual(saltadas + 1, MechaCyberpunk.VR.Cinematicas.SaltarCinematicaVR.Saltadas, "B saltó la cinemática.");
            Assert.IsFalse(CinematicaEnCurso(), "La cinemática termina al saltarla con B.");
            yield return Cuadros(2);
            Assert.IsTrue(controlador.ControlActivo, "Al terminar vuelve el control.");
        }

        [UnityTest]
        public IEnumerator CinematicaPendiente_ConVideo_SaleEnLaPantallaDeCineYApagaSuNegro()
        {
            yield return ArmarVR();
            VideoClip clip = CargarVideo();
            ScriptableObject datos = NuevaCinematica("C98", 1f, clip);
            Reproducir(datos);
            GameObject objeto = GameObject.Find("CinematicaPendiente");
            Assert.IsNotNull(objeto);
            extras.Add(objeto);
            VideoPlayer video = objeto.GetComponent<VideoPlayer>();
            Assert.IsNotNull(video, "La cinemática con clip arma su VideoPlayer.");
            yield return EsperarVideo(video);
            yield return Cuadros(2);

            Canvas lienzo = BuscarLienzo("CanvasCinematica");
            Assert.IsTrue(VideosCineVR.EnCine(video), "El video de la cinemática sale en la pantalla de cine.");
            Assert.AreSame(video.targetTexture, PantallaCineVR.TexturaActual);
            Assert.IsNotNull(lienzo);
            Assert.IsFalse(lienzo.enabled, "Su negro y su video pegados a la cabeza no tapan la pantalla mientras corre el video.");
            Assert.AreEqual(VideoAudioOutputMode.AudioSource, video.audioOutputMode, "El audio sale igual que en PC.");

            yield return SaltarYEsperarFin();
            Assert.IsFalse(PantallaCineVR.Tiene(video), "Al terminar se quita de la pantalla.");
            Assert.IsFalse(PantallaCineVR.Visible);
        }

        // ---------- Ayudas ----------

        private Camera NuevaCamara(string nombre, float profundidad)
        {
            var objeto = new GameObject(nombre);
            objeto.transform.SetPositionAndRotation(new Vector3(4f, 3f, -4f), Quaternion.Euler(20f, -45f, 0f));
            extras.Add(objeto);
            Camera c = objeto.AddComponent<Camera>();
            c.depth = profundidad;
            return c;
        }

        private VideoPlayer NuevoVideo(VideoClip clip, VideoRenderMode modo)
        {
            var objeto = new GameObject("VideoCinematicaPrueba");
            extras.Add(objeto);
            VideoPlayer v = objeto.AddComponent<VideoPlayer>();
            v.playOnAwake = false;
            v.source = VideoSource.VideoClip;
            v.clip = clip;
            v.renderMode = modo;
            v.audioOutputMode = VideoAudioOutputMode.None;
            v.isLooping = true;
            return v;
        }

        private static VideoClip CargarVideo()
        {
            VideoClip clip = AssetDatabase.LoadAssetAtPath<VideoClip>(RutaVideo);
            if (clip == null)
            {
                Assert.Ignore("No hay " + RutaVideo + " importado (¿git lfs pull?): no se puede probar el video.");
            }
            return clip;
        }

        // Espera a que el video corra; si en este modo (batch sin decodificador) nunca arranca, la prueba se ignora (no pasa en falso).
        private static IEnumerator EsperarVideo(VideoPlayer video)
        {
            float limite = Time.realtimeSinceStartup + SegundosEsperaVideo;
            while (video != null && !video.isPlaying && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            if (video == null || !video.isPlaying)
            {
                Assert.Ignore("El video no arrancó en este modo del editor: solo se probó la redirección.");
            }
        }

        private static SillaClinica CrearSilla(ConfigSillaClinica config)
        {
            GameObject raiz = new GameObject("SillaClinicaVR");
            raiz.SetActive(false);
            raiz.transform.position = new Vector3(0f, 0f, 1.3f);
            Transform asiento = new GameObject("Asiento").transform;
            asiento.SetParent(raiz.transform, false);
            asiento.SetPositionAndRotation(new Vector3(0f, 0.55f, 2.2f), Quaternion.LookRotation(Vector3.back));
            Transform salida = new GameObject("Salida").transform;
            salida.SetParent(raiz.transform, false);
            salida.SetPositionAndRotation(new Vector3(0.9f, 0.05f, 2.2f), Quaternion.identity);
            GameObject brazo = new GameObject("Brazo");
            brazo.transform.SetParent(raiz.transform, false);
            brazo.transform.position = new Vector3(0f, 2.3f, 2.2f);
            SillaClinica silla = raiz.AddComponent<SillaClinica>();
            silla.Configurar(config, asiento, salida, brazo.transform);
            raiz.SetActive(true);
            return silla;
        }

        private static Canvas BuscarLienzo(string nombre)
        {
            GameObject objeto = GameObject.Find(nombre);
            if (objeto != null)
            {
                return objeto.GetComponent<Canvas>();
            }
            foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.name == nombre)
                {
                    return c;
                }
            }
            return null;
        }

        private static ScriptableObject NuevaCinematica(string id, float duracion, VideoClip video)
        {
            Assert.IsNotNull(TipoDatosCinematica, "Existe DatosCinematica.");
            ScriptableObject datos = ScriptableObject.CreateInstance(TipoDatosCinematica);
            TipoDatosCinematica.GetMethod("Configurar").Invoke(datos, new object[] { id, "PRUEBA VR", "La cabeza no se mueve.", duracion });
            if (video != null)
            {
                var serializado = new SerializedObject(datos);
                serializado.FindProperty("video").objectReferenceValue = video;
                serializado.ApplyModifiedPropertiesWithoutUndo();
            }
            return datos;
        }

        private static void Reproducir(ScriptableObject datos)
        {
            Assert.IsNotNull(TipoCinematica, "Existe CinematicaPendiente.");
            MethodInfo reproducir = TipoCinematica.GetMethod("Reproducir", BindingFlags.Public | BindingFlags.Static);
            reproducir.Invoke(null, new object[] { datos, null, null });
        }

        private static bool CinematicaEnCurso()
        {
            return (bool)TipoCinematica.GetProperty("EnCurso", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        private static bool SaltarCinematica()
        {
            return (bool)TipoCinematica.GetMethod("SaltarActual", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        }

        private static IEnumerator SaltarYEsperarFin()
        {
            float limite = Time.realtimeSinceStartup + 10f;
            while (CinematicaEnCurso() && !SaltarCinematica() && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            while (CinematicaEnCurso() && Time.realtimeSinceStartup < limite)
            {
                yield return null;
            }
            Assert.IsFalse(CinematicaEnCurso(), "La cinemática termina.");
            yield return Cuadros(2);
        }
    }
}

using System;
using System.Collections;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>Pruebas jugables de CarroViejo como IMontable y de su cámara, con un doble de jugador (sin la cámara libre).</summary>
    public class MontableCarroPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaCaptura1 = "docs/capturas/T-075-1.png";
        private const string RutaCaptura2 = "docs/capturas/T-075-2.png";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float RapidezParaBajar = 0.5f;
        private const float AlturaPersona = 1.8f;
        private const float RadioPersona = 0.4f;

        private sealed class DobleJugador : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
#pragma warning disable CS0067
            public event Action<ModoJugador> ModoCambio;
#pragma warning restore CS0067
            public void Colocar(Pose pose)
            {
                Raiz.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject carroObjeto;
        private CarroViejo carro;
        private Rigidbody cuerpo;
        private GameObject jugadorObjeto;
        private DobleJugador jugador;
        private readonly System.Collections.Generic.List<GameObject> extras = new System.Collections.Generic.List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPrueba";
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            jugadorObjeto = new GameObject("DobleJugador");
            jugador = new DobleJugador { Raiz = jugadorObjeto.transform };
        }

        public override void TearDown()
        {
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    UnityEngine.Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            if (carroObjeto != null)
            {
                UnityEngine.Object.DestroyImmediate(carroObjeto);
            }
            UnityEngine.Object.DestroyImmediate(jugadorObjeto);
            UnityEngine.Object.DestroyImmediate(piso);
            base.TearDown();
        }

        private void CrearCarro(Vector3 posicion)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Falta el prefab " + RutaPrefab);
            carroObjeto = UnityEngine.Object.Instantiate(prefab, posicion, Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            cuerpo = carroObjeto.GetComponent<Rigidbody>();
        }

        private GameObject Caja(string nombre, Vector3 centro, Vector3 tamano)
        {
            GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube);
            caja.name = nombre;
            caja.transform.position = centro;
            caja.transform.localScale = tamano;
            extras.Add(caja);
            return caja;
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        // Simula segundos de física (Time.fixedTime). No usa WaitForFixedUpdate: con él el Input System pierde el estado del teclado.
        private static IEnumerator Simular(float segundos, Action porPaso = null)
        {
            float fin = Time.fixedTime + segundos;
            while (Time.fixedTime < fin)
            {
                yield return null;
                porPaso?.Invoke();
            }
        }

        private IEnumerator Preparar(Vector3 posicion)
        {
            yield return Calentar();
            CrearCarro(posicion);
            yield return Simular(0.5f);
        }

        private static int Prendidas<T>() where T : Behaviour
        {
            int cuenta = 0;
            foreach (T componente in UnityEngine.Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (componente.enabled)
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        [UnityTest]
        public IEnumerator Subir_Desocupado_DevuelveTrueUnaVezYFalseSiYaEstaOcupado()
        {
            yield return Preparar(Vector3.zero);
            int avisos = 0;
            bool ultimo = false;
            carro.OcupadoCambio += ocupado => { avisos++; ultimo = ocupado; };
            Assert.IsTrue(carro.PuedeSubir);

            Assert.IsTrue(carro.IntentarSubir(jugador));
            Assert.IsTrue(carro.Ocupado);
            Assert.IsFalse(carro.PuedeSubir);
            Assert.IsFalse(carro.IntentarSubir(jugador));
            Assert.AreEqual(1, avisos);
            Assert.IsTrue(ultimo);
        }

        [UnityTest]
        public IEnumerator Camara_SoloEstaPrendidaMientrasEstaOcupado()
        {
            yield return Preparar(Vector3.zero);
            Assert.IsFalse(carro.Camara.enabled, "La cámara debe empezar apagada");
            Assert.IsFalse(carro.Camara.GetComponent<AudioListener>().enabled, "El listener debe empezar apagado");

            carro.IntentarSubir(jugador);
            Assert.IsTrue(carro.Camara.enabled);
            Assert.IsTrue(carro.Camara.GetComponent<AudioListener>().enabled);
            Assert.IsTrue(carro.TieneControl);

            yield return Simular(0.3f);
            Assert.IsTrue(carro.IntentarBajar(out _));
            Assert.IsFalse(carro.Camara.enabled);
            Assert.IsFalse(carro.Camara.GetComponent<AudioListener>().enabled);
            Assert.IsFalse(carro.TieneControl);
        }

        [UnityTest]
        public IEnumerator Registro_ElCarroEstaEnRegistroDeMontablesMientrasEstaActivo()
        {
            yield return Preparar(Vector3.zero);
            Assert.IsTrue(ToLista().Contains(carro));
            carroObjeto.SetActive(false);
            Assert.IsFalse(ToLista().Contains(carro));
            carroObjeto.SetActive(true);
            Assert.IsTrue(ToLista().Contains(carro));
            Assert.AreEqual(ModoJugador.Carro, carro.Modo);
            Assert.AreEqual("Carro viejo", carro.Nombre);
        }

        private static System.Collections.Generic.List<IMontable> ToLista()
        {
            return new System.Collections.Generic.List<IMontable>(Registro<IMontable>.Todos);
        }

        [UnityTest]
        public IEnumerator Manejar_SinOcuparWNoMueve_OcupadoWTresSegundosMasDe15Metros()
        {
            yield return Preparar(new Vector3(0f, 0f, -60f));
            Vector3 inicio = carroObjeto.transform.position;
            Press(teclado.wKey);
            yield return Simular(3f);
            Assert.Less((carroObjeto.transform.position - inicio).magnitude, 0.1f, "Sin ocupar, W movió el carro");
            Release(teclado.wKey);
            yield return Simular(0.3f);

            carro.IntentarSubir(jugador);
            inicio = carroObjeto.transform.position;
            Press(teclado.wKey);
            yield return Simular(3f);
            float recorrido = (carroObjeto.transform.position - inicio).magnitude;
            Assert.Greater(recorrido, 15f, "Ocupado, W 3 s solo movió " + recorrido + " m");
        }

        [UnityTest]
        public IEnumerator Bajar_EnMovimiento_NoBajaNiAvisa()
        {
            yield return Preparar(Vector3.zero);
            carro.IntentarSubir(jugador);
            int avisos = 0;
            carro.OcupadoCambio += _ => avisos++;
            cuerpo.linearVelocity = carroObjeto.transform.forward * 5f;
            yield return null;
            Assert.Greater(carro.Rapidez, RapidezParaBajar);

            Assert.IsFalse(carro.PuedeBajar);
            Assert.IsFalse(carro.IntentarBajar(out _));
            Assert.IsTrue(carro.Ocupado);
            Assert.AreEqual(0, avisos);
            Assert.IsTrue(carro.Camara.enabled);
        }

        [UnityTest]
        public IEnumerator Bajar_Detenido_DejaUnaPoseEnElPisoLibreDeObstaculos()
        {
            yield return Preparar(Vector3.zero);
            carro.IntentarSubir(jugador);
            int avisos = 0;
            bool ultimo = true;
            carro.OcupadoCambio += ocupado => { avisos++; ultimo = ocupado; };
            yield return Simular(0.5f);
            Assert.Less(carro.Rapidez, RapidezParaBajar);
            Assert.IsTrue(carro.PuedeBajar);

            Assert.IsTrue(carro.IntentarBajar(out Pose pose));
            Assert.IsFalse(carro.Ocupado);
            Assert.AreEqual(1, avisos);
            Assert.IsFalse(ultimo);

            Assert.IsTrue(Physics.Raycast(pose.position + Vector3.up * 0.05f, Vector3.down, out RaycastHit golpe, 1f),
                "Debajo de la pose no hay piso");
            Assert.Less(golpe.distance, 0.1f, "La pose está a " + golpe.distance + " m del piso");
            Assert.IsFalse(PersonaChoca(pose.position), "Una persona en la pose choca con algo");
            foreach (Collider colisionador in carroObjeto.GetComponentsInChildren<Collider>())
            {
                Assert.Greater((colisionador.ClosestPoint(pose.position) - pose.position).sqrMagnitude, 0.0001f,
                    "La pose está dentro de " + colisionador.name);
            }
            Assert.AreEqual(carroObjeto.transform.eulerAngles.y, pose.rotation.eulerAngles.y, 0.5f, "La pose no mira como el carro");
        }

        private static bool PersonaChoca(Vector3 pies)
        {
            return Physics.CheckCapsule(pies + Vector3.up * (RadioPersona + 0.05f), pies + Vector3.up * (AlturaPersona - RadioPersona),
                RadioPersona, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        [UnityTest]
        public IEnumerator Bajar_ConMuroPegadoALaPuertaDelConductor_SaleDelLadoDelCopiloto()
        {
            yield return Preparar(Vector3.zero);
            Vector3 puerta = carro.PuntoSubida;
            Caja("MuroConductor", puerta + new Vector3(-0.2f, 1f, 0f), new Vector3(0.5f, 2f, 3f));
            carro.IntentarSubir(jugador);
            yield return Simular(0.5f);

            Assert.IsTrue(carro.PuedeBajar, "Con la salida del copiloto libre debe poder bajar");
            Assert.IsTrue(carro.IntentarBajar(out Pose pose));
            Vector3 local = carroObjeto.transform.InverseTransformPoint(pose.position);
            Assert.Greater(local.x, 1f, "Debió bajar del lado del copiloto (x local " + local.x + ")");
            Assert.IsFalse(PersonaChoca(pose.position));
        }

        [UnityTest]
        public IEnumerator Bajar_ConMurosEnLosDosLados_NoPuedeBajar()
        {
            yield return Preparar(Vector3.zero);
            Vector3 puerta = carro.PuntoSubida;
            Caja("MuroConductor", puerta + new Vector3(-0.2f, 1f, 0f), new Vector3(0.5f, 2f, 3f));
            Caja("MuroCopiloto", new Vector3(-puerta.x + 0.2f, 1f, puerta.z), new Vector3(0.5f, 2f, 3f));
            carro.IntentarSubir(jugador);
            int avisos = 0;
            carro.OcupadoCambio += _ => avisos++;
            yield return Simular(0.5f);

            Assert.Less(carro.Rapidez, RapidezParaBajar);
            Assert.IsFalse(carro.PuedeBajar);
            Assert.IsFalse(carro.IntentarBajar(out _));
            Assert.IsTrue(carro.Ocupado);
            Assert.AreEqual(0, avisos);
        }

        [UnityTest]
        public IEnumerator Camara_CAlternaTerceraPersonaYOtraCRegresa()
        {
            yield return Preparar(Vector3.zero);
            carro.IntentarSubir(jugador);
            CamaraCarro camara = carro.CamaraDelCarro;
            Assert.IsFalse(camara.EsTerceraPersona, "Debe empezar en primera persona");
            yield return null;
            Vector3 ojos = camara.transform.position;
            Assert.AreEqual(carro.Asiento.position.y + 1.15f, ojos.y, 0.05f, "Los ojos no están 1.15 m sobre el asiento");

            PressAndRelease(teclado.cKey);
            yield return null;
            yield return null;
            Assert.IsTrue(camara.EsTerceraPersona);
            Vector3 atras = carroObjeto.transform.InverseTransformPoint(camara.transform.position);
            Assert.Less(atras.z, -4f, "La tercera persona debe estar detrás del carro: z local " + atras.z);
            Assert.Greater(atras.y, 2f, "La tercera persona debe estar arriba: y local " + atras.y);

            PressAndRelease(teclado.cKey);
            yield return null;
            yield return null;
            Assert.IsFalse(camara.EsTerceraPersona);
        }

        [UnityTest]
        public IEnumerator Camara_LaEleccionDeTerceraPersonaSeRecuerdaEntreSubidas()
        {
            yield return Preparar(Vector3.zero);
            carro.IntentarSubir(jugador);
            PressAndRelease(teclado.cKey);
            yield return null;
            yield return null;
            Assert.IsTrue(carro.CamaraDelCarro.EsTerceraPersona);

            yield return Simular(0.3f);
            Assert.IsTrue(carro.IntentarBajar(out _));
            Assert.IsTrue(carro.IntentarSubir(jugador));
            Assert.IsTrue(carro.CamaraDelCarro.EsTerceraPersona, "La tercera persona no se recordó");
        }

        [UnityTest]
        public IEnumerator Camara_TerceraPersonaConMuroDetras_QuedaDelLadoDelCarroSinChoques()
        {
            yield return Preparar(Vector3.zero);
            // Muro de 1 m de grueso detrás del carro: su cara cercana está en z = -3.
            Caja("MuroAtras", new Vector3(0f, 4f, -3.5f), new Vector3(30f, 8f, 1f));
            carro.IntentarSubir(jugador);
            PressAndRelease(teclado.cKey);
            yield return null;
            yield return null;
            yield return Simular(0.5f);

            Vector3 camara = carro.CamaraDelCarro.transform.position;
            Assert.Greater(camara.z, -3f, "La cámara quedó detrás del muro: z = " + camara.z);
            Vector3 foco = carroObjeto.transform.position + Vector3.up * 1.2f;
            Vector3 direccion = foco - camara;
            RaycastHit[] impactos = Physics.RaycastAll(camara, direccion.normalized, direccion.magnitude);
            foreach (RaycastHit impacto in impactos)
            {
                Assert.AreEqual(cuerpo, impacto.collider.attachedRigidbody, "Entre la cámara y el carro hay " + impacto.collider.name);
            }
        }

        [UnityTest]
        public IEnumerator Camara_PrimeraPersona_MiraHastaElLimiteYVuelveAlFrenteTrasLaEspera()
        {
            yield return Preparar(Vector3.zero);
            carro.IntentarSubir(jugador);
            CamaraCarro camara = carro.CamaraDelCarro;

            Set(raton.delta, new Vector2(100000f, 0f));
            yield return null;
            yield return null;
            Assert.AreEqual(110f, camara.Yaw, 0.01f, "Mirar a la derecha debe parar en 110°");

            Set(raton.delta, new Vector2(0f, 100000f));
            yield return null;
            yield return null;
            Assert.AreEqual(-60f, camara.Inclinacion, 0.01f, "Mirar arriba debe parar en 60°");

            yield return Simular(0.7f);
            Assert.AreEqual(110f, camara.Yaw, 0.01f, "Antes de 1.5 s sin mover no debe volver");
            yield return Simular(2.5f);
            Assert.AreEqual(0f, camara.Yaw, 0.01f, "Pasada la espera debe volver al frente");
            Assert.AreEqual(0f, camara.Inclinacion, 0.01f);
        }

        private IEnumerator Capturar(Camera camara, string ruta)
        {
            GameObject luzObjeto = new GameObject("LuzPrueba");
            extras.Add(luzObjeto);
            luzObjeto.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            luzObjeto.AddComponent<Light>().type = LightType.Directional;
            camara.clearFlags = CameraClearFlags.SolidColor;
            camara.backgroundColor = new Color(0.4f, 0.55f, 0.75f);

            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            yield return null;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D imagen = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            imagen.Apply();
            RenderTexture.active = null;
            camara.targetTexture = null;

            string completa = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ruta));
            Directory.CreateDirectory(Path.GetDirectoryName(completa));
            File.WriteAllBytes(completa, imagen.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(imagen);
            UnityEngine.Object.DestroyImmediate(rt);
            Assert.IsTrue(File.Exists(completa));
        }

        [UnityTest]
        public IEnumerator Captura_PrimeraYTerceraPersonaManejandoEnElPiso()
        {
            yield return Preparar(new Vector3(0f, 0f, -60f));
            // Marcas en el piso para ver el camino y la velocidad.
            for (int i = 0; i < 12; i++)
            {
                Caja("Marca" + i, new Vector3(i % 2 == 0 ? -3f : 3f, 0.5f, -50f + i * 4f), new Vector3(0.6f, 1f, 0.6f));
            }
            carro.IntentarSubir(jugador);
            Press(teclado.wKey);
            yield return Simular(1.5f);
            yield return Simular(1.5f);
            Assert.Greater(carro.Rapidez, 5f);

            yield return Capturar(carro.Camara, RutaCaptura1);

            PressAndRelease(teclado.cKey);
            yield return Simular(0.6f);
            Assert.IsTrue(carro.CamaraDelCarro.EsTerceraPersona);
            yield return Capturar(carro.Camara, RutaCaptura2);
        }
    }
}

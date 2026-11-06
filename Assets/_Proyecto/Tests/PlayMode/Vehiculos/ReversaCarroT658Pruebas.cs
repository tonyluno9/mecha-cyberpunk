using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>
    /// T-658: dar reversa se siente bien. Con teclas simuladas: frenar y esperar antes de retroceder, sin tirones al cambiar de sentido,
    /// dirección de carro real (con D en reversa la trompa se va a la izquierda), volante sin saltos y cámara que no se arremolina.
    /// </summary>
    public class ReversaCarroT658Pruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const int FramesEstables = 10;
        private const float DeltaTimeEstable = 0.05f;

        private sealed class DobleJugador : IJugador
        {
            public Transform Raiz { get; set; }
            public ModoJugador Modo => ModoJugador.APie;
            public Camera Camara => null;
#pragma warning disable CS0067
            public event Action<ModoJugador> ModoCambio;
#pragma warning restore CS0067
            public void Colocar(Pose pose) { Raiz.SetPositionAndRotation(pose.position, pose.rotation); }
        }

        /// <summary>Llama a una acción en cada paso de física: las muestras son por FixedUpdate, no por cuadro.</summary>
        private sealed class MuestreadorFijo : MonoBehaviour
        {
            public Action AlMuestrear;
            private void FixedUpdate() { AlMuestrear?.Invoke(); }
        }

        private Keyboard teclado;
        private GameObject piso;
        private GameObject carroObjeto;
        private GameObject jugadorObjeto;
        private DobleJugador jugador;
        private CarroViejo carro;
        private Rigidbody cuerpo;
        private MuestreadorFijo muestreador;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            MapasDePruebaVehiculos.Reiniciar(teclado);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugadorObjeto = new GameObject("DobleJugador");
            jugador = new DobleJugador { Raiz = jugadorObjeto.transform };
        }

        public override void TearDown()
        {
            if (carroObjeto != null) { Object.DestroyImmediate(carroObjeto); }
            Object.DestroyImmediate(jugadorObjeto);
            Object.DestroyImmediate(piso);
            MapasDePruebaVehiculos.AntesDelTearDown(teclado);
            base.TearDown();
            MapasDePruebaVehiculos.DespuesDelTearDown();
        }

        private static IEnumerator Calentar()
        {
            int estables = 0;
            for (int i = 0; i < 1000 && estables < FramesEstables; i++)
            {
                yield return null;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        // Simula segundos de física (Time.fixedTime). No usa WaitForFixedUpdate: con él el Input System pierde el estado del teclado.
        private static IEnumerator Simular(float segundos)
        {
            float fin = Time.fixedTime + segundos;
            while (Time.fixedTime < fin) { yield return null; }
        }

        private IEnumerator CrearCarro(bool conCamara)
        {
            yield return Calentar();
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab), new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            carro = carroObjeto.GetComponent<CarroViejo>();
            cuerpo = carroObjeto.GetComponent<Rigidbody>();
            if (conCamara)
            {
                Assert.IsTrue(carro.IntentarSubir(jugador));
            }
            else
            {
                carro.TomarControl(true);
            }
            muestreador = carroObjeto.AddComponent<MuestreadorFijo>();
            yield return Simular(1f);
        }

        private static void Guardar(string nombre, IEnumerable<string> lineas)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/" + nombre, lineas);
        }

        [UnityTest]
        public IEnumerator Reversa_DeAdelanteAAtras_FrenaEsperaYRetrocedeSinTirones()
        {
            yield return CrearCarro(false);
            cuerpo.linearVelocity = carroObjeto.transform.forward * 10f;
            var tiempos = new List<float>();
            var velocidades = new List<float>();
            muestreador.AlMuestrear = () => { tiempos.Add(Time.fixedTime); velocidades.Add(carro.Velocidad); };
            Press(teclado.sKey);
            yield return Simular(7f);
            Release(teclado.sKey);

            float tParado = -1f;
            float tReversa = -1f;
            for (int i = 0; i < velocidades.Count; i++)
            {
                if (tParado < 0f && velocidades[i] < 0.5f) { tParado = tiempos[i]; }
                if (tReversa < 0f && velocidades[i] < -0.5f) { tReversa = tiempos[i]; }
            }
            Assert.GreaterOrEqual(tParado, 0f, "Frenó hasta parar");
            Assert.GreaterOrEqual(tReversa, 0f, "Luego dio reversa");
            Assert.GreaterOrEqual(tReversa - tParado, 0.2f, "Se queda parado un momento antes de retroceder (" + (tReversa - tParado) + " s)");

            // Aceleración suavizada en ventanas de 0.1 s: sin cambios bruscos al pasar de frenar a retroceder.
            const int Ventana = 5;
            float aMax = 0f;
            float cambioMax = 0f;
            float aPrevia = 0f;
            bool primera = true;
            var lineas = new List<string>();
            for (int i = Ventana; i < velocidades.Count; i += Ventana)
            {
                float a = (velocidades[i] - velocidades[i - Ventana]) / (tiempos[i] - tiempos[i - Ventana]);
                lineas.Add(tiempos[i].ToString("F2") + " v=" + velocidades[i].ToString("F2") + " a=" + a.ToString("F2"));
                if (tiempos[i] >= tParado + 0.2f)
                {
                    aMax = Mathf.Max(aMax, Mathf.Abs(a));
                    if (!primera) { cambioMax = Mathf.Max(cambioMax, Mathf.Abs(a - aPrevia)); }
                    primera = false;
                    aPrevia = a;
                }
            }
            Guardar("t658-reversa.txt", lineas);
            Assert.LessOrEqual(aMax, 5f, "Aceleración máxima al retroceder (m/s²)");
            Assert.LessOrEqual(cambioMax, 3.5f, "Cambio de aceleración entre ventanas de 0.1 s (m/s² por 0.1 s)");
            float minima = float.MaxValue;
            foreach (float v in velocidades) { minima = Mathf.Min(minima, v); }
            Assert.GreaterOrEqual(minima, -5.5f, "Reversa no pasa de la velocidad de reversa");
            Assert.Less(minima, -2f, "Y sí retrocede");
        }

        [UnityTest]
        public IEnumerator Reversa_DeAtrasAAdelante_FrenaEsperaYAvanzaSinTirones()
        {
            yield return CrearCarro(false);
            cuerpo.linearVelocity = -carroObjeto.transform.forward * 4f;
            var tiempos = new List<float>();
            var velocidades = new List<float>();
            muestreador.AlMuestrear = () => { tiempos.Add(Time.fixedTime); velocidades.Add(carro.Velocidad); };
            Press(teclado.wKey);
            yield return Simular(5f);
            Release(teclado.wKey);

            float tParado = -1f;
            float tAdelante = -1f;
            for (int i = 0; i < velocidades.Count; i++)
            {
                if (tParado < 0f && velocidades[i] > -0.5f) { tParado = tiempos[i]; }
                if (tAdelante < 0f && velocidades[i] > 0.5f) { tAdelante = tiempos[i]; }
            }
            Assert.GreaterOrEqual(tParado, 0f, "Frenó la reversa");
            Assert.GreaterOrEqual(tAdelante, 0f, "Luego avanzó");
            Assert.GreaterOrEqual(tAdelante - tParado, 0.2f, "Se queda parado un momento antes de avanzar");
        }

        [UnityTest]
        public IEnumerator Reversa_ConD_LaTrompaSeVaALaIzquierdaComoUnCarroReal()
        {
            yield return CrearCarro(false);
            cuerpo.linearVelocity = -carroObjeto.transform.forward * 3f;
            Press(teclado.sKey);
            yield return Simular(1f);
            float rumboInicial = carroObjeto.transform.eulerAngles.y;
            var volantes = new List<float>();
            var giros = new List<float>();
            muestreador.AlMuestrear = () => { volantes.Add(carro.EntradaActual.Girar); giros.Add(cuerpo.angularVelocity.y); };
            Press(teclado.dKey);
            yield return Simular(2f);
            Release(teclado.dKey);
            Release(teclado.sKey);

            float cambio = Mathf.DeltaAngle(rumboInicial, carroObjeto.transform.eulerAngles.y);
            Assert.Less(cambio, -10f, "Con D en reversa la trompa gira a la izquierda (" + cambio + "°)");
            Assert.AreEqual(1f, volantes[volantes.Count - 1], 1e-3f, "El volante llega a tope");
            float pasoMax = 0f;
            for (int i = 1; i < volantes.Count; i++) { pasoMax = Mathf.Max(pasoMax, Mathf.Abs(volantes[i] - volantes[i - 1])); }
            Assert.LessOrEqual(pasoMax, 0.15f, "El volante no salta (máx. por paso de física: " + pasoMax + ")");
            float saltoGiroMax = 0f;
            for (int i = 1; i < giros.Count; i++) { saltoGiroMax = Mathf.Max(saltoGiroMax, Mathf.Abs(giros[i] - giros[i - 1])); }
            Assert.LessOrEqual(saltoGiroMax, 0.2f, "La velocidad de giro cambia sin saltos (rad/s por paso: " + saltoGiroMax + ")");
        }

        [UnityTest]
        public IEnumerator Reversa_ConLaCamaraDeTerceraPersona_NoSeArremolinaAlManiobrar()
        {
            yield return CrearCarro(true);
            CamaraCarro camara = carro.CamaraDelCarro;
            if (!camara.EsTerceraPersona)
            {
                Press(teclado.cKey);
                yield return null;
                Release(teclado.cKey);
                yield return null;
            }
            Assert.IsTrue(camara.EsTerceraPersona);
            cuerpo.linearVelocity = carroObjeto.transform.forward * 8f;
            Press(teclado.sKey);
            yield return Simular(3f);
            // Maniobra: reversa girando a un lado y luego al otro.
            var camaraYaw = new List<float>();
            var carroYaw = new List<float>();
            var tiempos = new List<float>();
            Press(teclado.dKey);
            float fin = Time.time + 3f;
            while (Time.time < fin) { Muestrear(camara, camaraYaw, carroYaw, tiempos); yield return null; }
            Release(teclado.dKey);
            Press(teclado.aKey);
            fin = Time.time + 3f;
            while (Time.time < fin) { Muestrear(camara, camaraYaw, carroYaw, tiempos); yield return null; }
            Release(teclado.aKey);
            Release(teclado.sKey);

            // Velocidad de giro en ventanas de 0.2 s para no depender de los cuadros.
            float camaraMax = 0f;
            float carroMax = 0f;
            var lineas = new List<string>();
            int j = 0;
            for (int i = 1; i < tiempos.Count; i++)
            {
                if (tiempos[i] - tiempos[j] < 0.2f) { continue; }
                float dt = tiempos[i] - tiempos[j];
                float rc = Mathf.Abs(Mathf.DeltaAngle(camaraYaw[j], camaraYaw[i])) / dt;
                float rk = Mathf.Abs(Mathf.DeltaAngle(carroYaw[j], carroYaw[i])) / dt;
                lineas.Add(tiempos[i].ToString("F2") + " camara=" + rc.ToString("F1") + " carro=" + rk.ToString("F1"));
                camaraMax = Mathf.Max(camaraMax, rc);
                carroMax = Mathf.Max(carroMax, rk);
                j = i;
            }
            Guardar("t658-camara.txt", lineas);
            Assert.Greater(carroMax, 10f, "El carro sí giró en la maniobra (" + carroMax + "°/s)");
            Assert.LessOrEqual(camaraMax, carroMax * 1.15f + 8f, "La cámara no gira más rápido que el carro (cámara " + camaraMax + "°/s, carro " + carroMax + "°/s)");
        }

        private void Muestrear(CamaraCarro camara, List<float> camaraYaw, List<float> carroYaw, List<float> tiempos)
        {
            tiempos.Add(Time.time);
            camaraYaw.Add(camara.Camara.transform.eulerAngles.y);
            carroYaw.Add(carroObjeto.transform.eulerAngles.y);
        }
    }
}

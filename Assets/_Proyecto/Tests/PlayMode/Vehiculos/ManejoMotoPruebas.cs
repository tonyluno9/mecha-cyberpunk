using System;
using System.Collections;
using MechaCyberpunk.Vehiculos.Carro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Vehiculos
{
    /// <summary>T-592: la moto (prefab real V08) acelera, gira inclinándose, frena con inercia hasta parar, no se vuelca y esconde el caballete al manejar.</summary>
    public class ManejoMotoPruebas : InputTestFixture
    {
        private const string RutaPrefab = "Assets/_Proyecto/Vehiculos/Prefabs/Carro_V08.prefab";
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";
        private const string RutaCaptura = "docs/capturas/T-592-moto.png";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;

        private Keyboard teclado;
        private GameObject piso;
        private GameObject motoObjeto;
        private CarroViejo moto;
        private ManejoMoto manejo;
        private Rigidbody cuerpo;
        private readonly System.Collections.Generic.List<GameObject> extras = new System.Collections.Generic.List<GameObject>();

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            MapasDePruebaVehiculos.Reiniciar(teclado);
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.name = "PisoPrueba";
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
        }

        public override void TearDown()
        {
            MapasDePruebaVehiculos.AntesDelTearDown(teclado);
            foreach (GameObject extra in extras)
            {
                if (extra != null)
                {
                    UnityEngine.Object.DestroyImmediate(extra);
                }
            }
            extras.Clear();
            if (motoObjeto != null)
            {
                UnityEngine.Object.DestroyImmediate(motoObjeto);
            }
            UnityEngine.Object.DestroyImmediate(piso);
            base.TearDown();
            MapasDePruebaVehiculos.DespuesDelTearDown();
        }

        private void CrearMoto(bool conControl)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            Assert.IsNotNull(prefab, "Falta " + RutaPrefab);
            motoObjeto = UnityEngine.Object.Instantiate(prefab, new Vector3(0f, 0.3f, 0f), Quaternion.identity);
            moto = motoObjeto.GetComponent<CarroViejo>();
            manejo = motoObjeto.GetComponent<ManejoMoto>();
            cuerpo = motoObjeto.GetComponent<Rigidbody>();
            Assert.IsNotNull(manejo, "El prefab de la moto lleva ManejoMoto");
            moto.TomarControl(conControl);
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

        private static IEnumerator Simular(float segundos, Action porPaso = null)
        {
            float fin = Time.fixedTime + segundos;
            while (Time.fixedTime < fin)
            {
                yield return null;
                porPaso?.Invoke();
            }
        }

        [UnityTest]
        public IEnumerator Moto_UsaConfigPropia_MasLigeraYAgilQueElCarro()
        {
            yield return Calentar();
            CrearMoto(false);
            GameObject carroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro);
            Rigidbody cuerpoCarro = carroPrefab.GetComponent<Rigidbody>();
            Assert.Less(cuerpo.mass, 400f, "una moto pesa mucho menos que el carro");
            Assert.Less(cuerpo.mass, cuerpoCarro.mass);
        }

        [UnityTest]
        public IEnumerator Moto_Acelera_ConW()
        {
            yield return Calentar();
            CrearMoto(true);
            yield return Simular(0.5f);
            Press(teclado.wKey);
            yield return Simular(3f);
            Assert.Greater(moto.Velocidad, 12f, "En 3 s la moto ya va fuerte: " + moto.Velocidad);
            Assert.Greater(motoObjeto.transform.position.z, 15f, "y avanzó hacia adelante");
        }

        [UnityTest]
        public IEnumerator Moto_EnCurva_SeInclinaHaciaElLado_SinVolcarse()
        {
            yield return Calentar();
            CrearMoto(true);
            cuerpo.linearVelocity = motoObjeto.transform.forward * 18f;
            Press(teclado.wKey);
            Press(teclado.dKey);
            float maxInclinacion = 0f;
            float minArriba = 1f;
            yield return Simular(2.5f, () =>
            {
                maxInclinacion = Mathf.Max(maxInclinacion, manejo.Inclinacion);
                minArriba = Mathf.Min(minArriba, motoObjeto.transform.up.y);
            });
            Assert.Greater(maxInclinacion, 8f, "Girando a la derecha se inclina a la derecha: " + maxInclinacion);
            Assert.Greater(Vector3.Dot(manejo.EnvoltorioVisual.up, motoObjeto.transform.right), 0.1f, "el modelo cae hacia la derecha");
            Assert.Greater(minArriba, 0.7f, "la física no se vuelca");
            Release(teclado.dKey);
            Press(teclado.aKey);
            yield return Simular(2f);
            Assert.Less(manejo.Inclinacion, -4f, "Y a la izquierda, al revés: " + manejo.Inclinacion);
        }

        [UnityTest]
        public IEnumerator Moto_FrenandoDeGolpe_ConGiro_NoSeVuelca()
        {
            yield return Calentar();
            CrearMoto(true);
            cuerpo.linearVelocity = motoObjeto.transform.forward * 26f;
            Press(teclado.sKey);
            Press(teclado.aKey);
            Press(teclado.spaceKey);
            float minArriba = 1f;
            yield return Simular(4f, () => minArriba = Mathf.Min(minArriba, motoObjeto.transform.up.y));
            Assert.Greater(minArriba, 0.7f, "Ni frenando a fondo con el volante girado se voltea: " + minArriba);
        }

        [UnityTest]
        public IEnumerator Moto_Frena_ConInercia_YTerminaDetenida()
        {
            yield return Calentar();
            CrearMoto(true);
            cuerpo.linearVelocity = motoObjeto.transform.forward * 20f;
            yield return Simular(0.2f);
            Press(teclado.sKey);
            yield return Simular(0.8f);
            Assert.Greater(moto.Velocidad, 8f, "Un segundo de freno no la para en seco (inercia): " + moto.Velocidad);
            Assert.Less(moto.Velocidad, 19f, "pero sí frena: " + moto.Velocidad);
            // Con el freno sostenido llega a cero (después daría reversa); se suelta al detenerse y se queda parada.
            float inicio = Time.fixedTime;
            while (moto.Velocidad > 0.5f && Time.fixedTime - inicio < 10f)
            {
                yield return null;
            }
            float segundosParaParar = Time.fixedTime - inicio;
            Release(teclado.sKey);
            Assert.Less(moto.Velocidad, 0.6f, "Con el freno sostenido se detiene: " + moto.Velocidad);
            Assert.Greater(segundosParaParar, 1.5f, "y tarda (inercia): " + segundosParaParar);
            Assert.Less(segundosParaParar, 9f, "pero para: " + segundosParaParar);
            yield return Simular(1.5f);
            Assert.Less(Mathf.Abs(moto.Velocidad), 0.8f, "Ya sin tocar nada se queda quieta: " + moto.Velocidad);
        }

        [UnityTest]
        public IEnumerator Caballete_SeEscondeAlManejar_YVuelveAlBajar()
        {
            yield return Calentar();
            CrearMoto(false);
            yield return Simular(0.5f);
            Assert.IsNotNull(manejo.FiltroModelo.sharedMesh);
            Mesh completa = manejo.FiltroModelo.sharedMesh;
            Assert.IsFalse(manejo.CaballeteOculto, "Parada y vacía se ve con su caballete");
            Assert.IsTrue(moto.IntentarSubir(null));
            yield return Simular(0.3f);
            Assert.IsTrue(manejo.CaballeteOculto, "Con alguien encima se esconde");
            Assert.AreNotSame(completa, manejo.FiltroModelo.sharedMesh, "con otra malla");
            Assert.Less(manejo.FiltroModelo.sharedMesh.GetIndexCount(0), completa.GetIndexCount(0), "que tiene menos triángulos");
            Assert.IsTrue(moto.IntentarBajar(out Pose _));
            yield return Simular(1.2f);
            Assert.IsFalse(manejo.CaballeteOculto, "Al bajar y quedar parada vuelve");
            Assert.AreSame(completa, manejo.FiltroModelo.sharedMesh);
        }

        [UnityTest]
        public IEnumerator Captura_MotoInclinadaEnCurva()
        {
            yield return Calentar();
            CrearMoto(true);
            Assert.IsTrue(moto.IntentarSubir(null));
            cuerpo.linearVelocity = motoObjeto.transform.forward * 16f;
            Press(teclado.wKey);
            Press(teclado.dKey);
            yield return Simular(1.6f);
            GameObject camObj = new GameObject("CamCaptura");
            extras.Add(camObj);
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.45f, 0.5f, 0.6f);
            Vector3 centro = motoObjeto.transform.position + Vector3.up * 0.7f;
            cam.transform.position = centro - motoObjeto.transform.forward * 3.8f + Vector3.up * 0.6f + motoObjeto.transform.right * 1.2f;
            cam.transform.LookAt(centro);
            GameObject sol = new GameObject("Sol");
            extras.Add(sol);
            Light luz = sol.AddComponent<Light>();
            luz.type = LightType.Directional;
            sol.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
            yield return null;
            CapturaPruebasVehiculos.Tomar(cam, RutaCaptura);
            Assert.IsTrue(System.IO.File.Exists(RutaCaptura));
            Assert.Greater(manejo.Inclinacion, 5f);
        }
    }
}

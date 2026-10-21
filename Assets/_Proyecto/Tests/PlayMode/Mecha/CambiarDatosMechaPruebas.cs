using System.Collections;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Movimiento;
using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>T-165: cambiar los DatosMecha de un mecha armado conserva el daño y todo lo que los lee se entera. Prefab real, entrada simulada.</summary>
    public class CambiarDatosMechaPruebas : InputTestFixture
    {
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const float DanioInicial = 40f;
        private const float VidaMaximaNueva = 130f;
        private const float DanioGolpeNuevo = 25f;
        private const float PreparacionNueva = 0.1f;
        private const float RecargaNueva = 0.3f;
        private const float VidaSaco = 1000f;
        private const float SegundosCaminando = 3f;
        private const float VentanaMedicion = 1f;
        private const float ToleranciaVelocidad = 0.05f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject mecha;
        private ControladorMecha controlador;
        private VidaMecha vida;
        private PantallaVidaCabina pantalla;
        private DatosMecha datosSerie;
        private DatosMecha datosNuevos;
        private DatosMecha datosSaco;
        private GameObject saco;
        private VidaMecha vidaSaco;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
            mecha = Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            controlador = mecha.GetComponent<ControladorMecha>();
            vida = mecha.GetComponent<VidaMecha>();
            pantalla = mecha.GetComponentInChildren<PantallaVidaCabina>();
            datosSerie = vida.Datos;

            datosNuevos = CrearDatos(VidaMaximaNueva, datosSerie.VelocidadMaxima + 1.5f, DanioGolpeNuevo,
                PreparacionNueva, RecargaNueva);
        }

        public override void TearDown()
        {
            Object.Destroy(mecha);
            Object.Destroy(piso);
            if (saco != null)
            {
                Object.Destroy(saco);
            }
            Object.Destroy(datosNuevos);
            if (datosSaco != null)
            {
                Object.Destroy(datosSaco);
            }
            base.TearDown();
        }

        private static DatosMecha CrearDatos(float vidaMaxima, float velocidad, float danio, float preparacion, float recarga)
        {
            DatosMecha d = ScriptableObject.CreateInstance<DatosMecha>();
            SerializedObject so = new SerializedObject(d);
            so.FindProperty("vidaMaxima").floatValue = vidaMaxima;
            so.FindProperty("velocidadMaxima").floatValue = velocidad;
            so.FindProperty("danioGolpe").floatValue = danio;
            so.FindProperty("preparacionGolpe").floatValue = preparacion;
            so.FindProperty("recargaGolpe").floatValue = recarga;
            so.ApplyModifiedPropertiesWithoutUndo();
            return d;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
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

        private void CrearSaco()
        {
            saco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            saco.name = "SacoCambioDatos";
            // Alto para quedar a la altura del barrido del golpe, que sale del centro del CharacterController.
            saco.transform.localScale = new Vector3(1f, 8f, 1f);
            saco.transform.position = new Vector3(0f, 4f, 2.5f);
            vidaSaco = saco.AddComponent<VidaMecha>();
            saco.AddComponent<SacoDePrueba>();
            datosSaco = CrearDatos(VidaSaco, 1f, 1f, 0.1f, 0.1f);
            vidaSaco.Configurar(datosSaco);
            Physics.SyncTransforms();
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CambiarDatos_ConDanio_ConservaElDanioYLaPantallaLoMuestra()
        {
            yield return Calentar();
            vida.FijarVida(datosSerie.VidaMaxima - DanioInicial);
            yield return null;
            Assert.AreEqual(60f, vida.Actual, 1e-3f);

            vida.CambiarDatos(datosNuevos);
            yield return null;

            Assert.AreEqual(VidaMaximaNueva, vida.Maxima, 1e-3f);
            Assert.AreEqual(VidaMaximaNueva - DanioInicial, vida.Actual, 1e-3f);
            Assert.AreEqual("VIDA 90", pantalla.Texto.text);
            Assert.AreEqual(90f / 130f, pantalla.Relleno.fillAmount, 1e-3f);
            Assert.AreSame(datosNuevos, controlador.Datos, "El controlador usa los mismos datos que la vida");
        }

        [UnityTest]
        public IEnumerator Golpe_TrasCambiarDatos_QuitaElDanioNuevoNoElViejo()
        {
            yield return Calentar();
            vida.CambiarDatos(datosNuevos);
            CrearSaco();
            yield return null;

            yield return Clic();
            yield return Esperar(PreparacionNueva + 0.25f);

            Assert.AreEqual(VidaSaco - DanioGolpeNuevo, vidaSaco.Actual, 1e-3f);
        }

        [UnityTest]
        public IEnumerator Golpe_TrasCambiarDatos_UsaLaNuevaRecarga()
        {
            yield return Calentar();
            vida.CambiarDatos(datosNuevos);
            CrearSaco();
            GolpeMecha golpe = mecha.GetComponent<GolpeMecha>();
            yield return null;

            yield return Clic();
            yield return Esperar(PreparacionNueva + RecargaNueva + 0.3f);

            Assert.AreEqual(FaseGolpe.Listo, golpe.Fase, "Con la recarga nueva (0.3 s) ya está listo; con la de serie seguiría recargando");
        }

        [UnityTest]
        public IEnumerator Velocidad_TrasCambiarDatos_LlegaALaNuevaVelocidadMaxima()
        {
            yield return Calentar();
            vida.FijarVida(datosSerie.VidaMaxima - DanioInicial);
            vida.CambiarDatos(datosNuevos);

            Press(teclado.wKey);
            float t = 0f;
            float inicioVentana = 0f;
            float zInicioVentana = 0f;
            bool ventanaAbierta = false;
            while (t < SegundosCaminando)
            {
                yield return null;
                t += Time.deltaTime;
                if (!ventanaAbierta && t >= SegundosCaminando - VentanaMedicion)
                {
                    ventanaAbierta = true;
                    inicioVentana = t;
                    zInicioVentana = mecha.transform.position.z;
                }
            }
            float velocidad = (mecha.transform.position.z - zInicioVentana) / (t - inicioVentana);
            Release(teclado.wKey);

            Assert.AreEqual(datosNuevos.VelocidadMaxima, velocidad, ToleranciaVelocidad);
            Assert.LessOrEqual(controlador.VelocidadLocal.magnitude, datosNuevos.VelocidadMaxima + 1e-3f);
        }

        [UnityTest]
        public IEnumerator CambiarDatos_ALosDeSerie_DejaLaVidaComoAntes()
        {
            yield return Calentar();
            vida.FijarVida(datosSerie.VidaMaxima - DanioInicial);
            vida.CambiarDatos(datosNuevos);
            yield return null;

            vida.CambiarDatos(datosSerie);
            yield return null;

            Assert.AreEqual(100f, vida.Maxima, 1e-3f);
            Assert.AreEqual(60f, vida.Actual, 1e-3f);
            Assert.AreEqual("VIDA 60", pantalla.Texto.text);
            Assert.AreSame(datosSerie, controlador.Datos);
        }

        [UnityTest]
        public IEnumerator CambiarDatos_ConNull_NoHaceNada()
        {
            yield return Calentar();
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("CambiarDatos recibió datos nulos"));
            vida.CambiarDatos(null);
            Assert.AreSame(datosSerie, vida.Datos);
            Assert.AreEqual(100f, vida.Actual, 1e-3f);
        }
    }
}

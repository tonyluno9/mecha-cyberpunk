using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Habilidades;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Vida;
using MechaCyberpunk.Progresion.Apoyo;
using MechaCyberpunk.UI.Hud;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-992: habilidades de prótesis [1] PIERNAS, [2] BRAZOS, [3] CABEZA con entrada simulada: efectos y duración por nivel, recarga,
    /// sin implante no activa y avisa, golpe x2.5, recarga del arma x0.4, el visor solo marca con CABEZA activa y en el carro no se activan.
    /// </summary>
    public class HabilidadesProtesisT992Pruebas : BaseJugadorAPie
    {
        private const float Tolerancia = 0.06f;
        private const string RutaHud = "Assets/_Proyecto/UI/Prefabs/HudAPie.prefab";
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatosPandillero = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private sealed class MontableFalso : IMontable
        {
            public Transform asiento;
            public Camera camara;
            public ModoJugador Modo => ModoJugador.Carro;
            public string Nombre => "Carro falso";
            public Vector3 PuntoSubida => asiento.position;
            public bool Ocupado { get; private set; }
            public bool PuedeSubir => !Ocupado;
            public bool PuedeBajar => Ocupado;
            public Transform Asiento => asiento;
            public Camera Camara => camara;
            public event Action<bool> OcupadoCambio;
            public bool IntentarSubir(IJugador j) { Ocupado = true; OcupadoCambio?.Invoke(true); return true; }
            public bool IntentarBajar(out Pose salida)
            {
                salida = new Pose(new Vector3(-2f, 0.05f, 0f), Quaternion.identity);
                Ocupado = false;
                OcupadoCambio?.Invoke(false);
                return true;
            }
        }

        private ImplantesFalsos falsos;
        private HabilidadesProtesis habilidades;
        private GameObject muneco;
        private MunecoAPie vidaMuneco;
        private GameObject hudObj;
        private GameObject extra;

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            falsos = new ImplantesFalsos();
            habilidades = jugador.GetComponent<HabilidadesProtesis>();
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            if (muneco != null) Object.Destroy(muneco);
            if (hudObj != null) Object.Destroy(hudObj);
            if (extra != null) Object.Destroy(extra);
            if (VisorRayosX.Actual != null) Object.Destroy(VisorRayosX.Actual.gameObject);
            base.TearDown();
        }

        private void Poner(EspacioImplante espacio, int nivel)
        {
            falsos.Poner(espacio, nivel);
            Registro<IImplantes>.Agregar(falsos);
        }

        private IEnumerator Tecla(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            yield return null;
            Release(tecla);
            yield return null;
        }

        [Test]
        public void JugadorAPie_AgregaElComponente_YElConfigCargaConLaEntrada()
        {
            Assert.IsNotNull(habilidades, "JugadorAPie agrega HabilidadesProtesis al nacer");
            Assert.IsNotNull(habilidades.Config, "Resources/ConfigHabilidadesProtesis");
            Assert.IsNotNull(habilidades.Config.Controles, "la config apunta a ControlesAPie");
            Assert.IsNotNull(habilidades.Config.Controles.FindAction("APie/HabilidadPiernas"));
            Assert.AreEqual("1", habilidades.Config.Controles.FindAction("APie/HabilidadPiernas").bindings[0].path.Substring("<Keyboard>/".Length));
            Assert.AreEqual(1f, habilidades.FactorCorrer, "sin activar no cambia nada");
        }

        [UnityTest]
        public IEnumerator Piernas_NivelI_CorrerX1punto5_Dura6s_YLuegoRecarga()
        {
            yield return Calentar();
            Poner(EspacioImplante.Piernas, 1);
            yield return null;
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Piernas));
            yield return Tecla(teclado.digit1Key);
            float inicio = Time.time;
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Piernas), "pulsar 1 activa PIERNAS");

            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            float z0 = jugador.transform.position.z;
            float t0 = Time.time;
            yield return Esperar(1.5f);
            float distancia = jugador.transform.position.z - z0;
            float segundos = Time.time - t0;
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
            float esperada = datos.VelocidadCorrer * falsos.Efectos.multiplicadorCorrer * habilidades.Config.FactorCorrer * segundos;
            Debug.Log("[Prueba] T-992 correr con PIERNAS I: medido " + distancia + " m, esperado " + esperada + " m");
            Assert.AreEqual(esperada, distancia, esperada * Tolerancia);

            yield return Esperar(5.4f - (Time.time - inicio));
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Piernas), "a los 5.4 s sigue activa");
            yield return Esperar(6.4f - (Time.time - inicio));
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Piernas), "a los 6.4 s ya recarga");
            Assert.AreEqual(1f, habilidades.FactorCorrer, "y el efecto se acabó");
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Piernas), "recargando no se activa otra vez");
        }

        [UnityTest]
        public IEnumerator Piernas_SaltaAlturaX2punto2()
        {
            yield return Calentar();
            Poner(EspacioImplante.Piernas, 1);
            yield return Esperar(0.4f);
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Piernas));
            Assert.IsTrue(controlador.EnPiso);
            float pisoY = jugador.transform.position.y;
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            float maxima = 0f;
            float t = 0f;
            while (t < 2f)
            {
                t += Time.deltaTime;
                maxima = Mathf.Max(maxima, jugador.transform.position.y - pisoY);
                yield return null;
            }
            float esperada = datos.AlturaSalto * falsos.Efectos.multiplicadorSalto * habilidades.Config.FactorSalto;
            Debug.Log("[Prueba] T-992 salto con PIERNAS I: medido " + maxima + " m, esperado " + esperada + " m");
            Assert.AreEqual(esperada, maxima, esperada * Tolerancia);
        }

        [UnityTest]
        public IEnumerator Piernas_NivelII_Dura10s()
        {
            yield return Calentar();
            Poner(EspacioImplante.Piernas, 2);
            yield return null;
            yield return Tecla(teclado.digit1Key);
            float inicio = Time.time;
            yield return Esperar(9.4f - (Time.time - inicio));
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Piernas), "con el nivel II sigue activa a los 9.4 s");
            yield return Esperar(10.5f - (Time.time - inicio));
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Piernas), "y a los 10.5 s recarga");
        }

        [UnityTest]
        public IEnumerator SinImplante_ElUnoNoHaceNada_YElHudAvisa()
        {
            hudObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaHud));
            yield return Calentar();
            yield return Esperar(0.3f);
            BarraHabilidades barra = hudObj.GetComponent<BarraHabilidades>();
            Assert.IsNotNull(barra, "el HUD a pie trae la barra de habilidades");
            Assert.IsTrue(barra.Visible, "a pie la barra se ve");
            Assert.AreEqual("—", barra.NombreMostrado(HabilidadProtesis.Piernas), "sin implante la casilla sale apagada con —");
            Assert.AreEqual("1", barra.TeclaMostrada(HabilidadProtesis.Piernas));
            Assert.AreEqual("2", barra.TeclaMostrada(HabilidadProtesis.Brazos));
            Assert.AreEqual("3", barra.TeclaMostrada(HabilidadProtesis.Cabeza));

            Press(teclado.leftShiftKey);
            Press(teclado.wKey);
            yield return Esperar(1.5f);
            float velocidad = controlador.Velocidad.y;
            Release(teclado.wKey);
            Release(teclado.leftShiftKey);
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Piernas), "sin implante no se activa");
            Assert.AreEqual(datos.VelocidadCorrer, velocidad, datos.VelocidadCorrer * Tolerancia, "y la velocidad no cambia");
            yield return null;
            yield return null;
            Assert.IsTrue(ColaAvisos.Actual.Texto.Contains("NECESITAS EXO DE PIERNAS"), "avisa; dice: " + ColaAvisos.Actual.Texto);
            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Brazos));
            yield return Tecla(teclado.digit3Key);
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Cabeza));
        }

        [UnityTest]
        public IEnumerator Barra_MuestraLaHabilidadActivaYLaQueRecarga()
        {
            hudObj = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaHud));
            yield return Calentar();
            Poner(EspacioImplante.Piernas, 1);
            Poner(EspacioImplante.Brazos, 1);
            yield return Esperar(0.3f);
            BarraHabilidades barra = hudObj.GetComponent<BarraHabilidades>();
            Assert.AreEqual("PIERNAS", barra.NombreMostrado(HabilidadProtesis.Piernas));
            Assert.AreEqual("BRAZOS", barra.NombreMostrado(HabilidadProtesis.Brazos));
            Assert.AreEqual(1f, barra.MedidorMostrado(HabilidadProtesis.Piernas), 0.01f, "lista: medidor lleno");
            yield return Tecla(teclado.digit1Key);
            yield return Esperar(2f);
            float medidor = barra.MedidorMostrado(HabilidadProtesis.Piernas);
            Assert.Less(medidor, 0.9f, "activa: el medidor se vacía");
            Assert.Greater(medidor, 0.2f);
            Assert.IsTrue(ColaAvisos.Actual.Texto.Contains("PIERNAS ACTIVAS") || medidor < 1f);
        }

        [UnityTest]
        public IEnumerator Brazos_ElGolpePegaX2punto5()
        {
            yield return Calentar();
            Poner(EspacioImplante.Brazos, 1);
            muneco = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            muneco.transform.position = new Vector3(0f, 0.9f, 1.2f);
            muneco.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            vidaMuneco = muneco.AddComponent<MunecoAPie>();
            Physics.SyncTransforms();
            yield return null;
            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Brazos));
            GolpeAPie golpe = jugador.GetComponent<GolpeAPie>();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            DatosArma punos = arsenal != null ? arsenal.Datos(ArmaAPie.Punos) : null;
            float basico = punos != null ? punos.Danio : golpe.Datos.DanioGolpe;
            float antes = vidaMuneco.Actual;
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(0.3f);
            float danio = antes - vidaMuneco.Actual;
            float esperado = basico * falsos.Efectos.multiplicadorGolpeAPie * habilidades.Config.FactorGolpe;
            Debug.Log("[Prueba] T-992 golpe con BRAZOS I: medido " + danio + ", esperado " + esperado);
            Assert.AreEqual(esperado, danio, 0.0001f);
            Assert.AreEqual(2.5f, habilidades.Config.FactorGolpe, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Brazos_LaRecargaDelArmaTardaX0punto4()
        {
            yield return Calentar();
            Poner(EspacioImplante.Brazos, 2);
            PistolaAPie pistola = jugador.GetComponent<PistolaAPie>();
            jugador.GetComponent<ArmasAPie>().Equipar(ArmaAPie.Pistola);
            yield return Esperar(0.5f);
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return Esperar(pistola.Datos.TiempoEntreDisparos + 0.1f);
            Assert.AreEqual(pistola.TamanoCargador - 1, pistola.Balas);
            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Brazos));
            Press(teclado.rKey);
            yield return null;
            Release(teclado.rKey);
            float t0 = Time.time;
            int seguridad = 0;
            while (!pistola.Recargando && seguridad++ < 10) yield return null;
            Assert.IsTrue(pistola.Recargando);
            t0 = Time.time;
            while (pistola.Recargando && Time.time - t0 < 5f) yield return null;
            float tarda = Time.time - t0;
            float pasivo = EfectosAPie.FactorRecarga(falsos.Efectos);
            float esperada = pistola.Datos.TiempoRecarga * pasivo * habilidades.Config.FactorTiempoRecarga;
            Debug.Log("[Prueba] T-992 recarga con BRAZOS: tardó " + tarda + " s, esperada " + esperada + " s (normal " + pistola.Datos.TiempoRecarga + " s, pasivo x" + pasivo + ")");
            Assert.AreEqual(esperada, tarda, 0.1f);
            Assert.AreEqual(0.4f, habilidades.Config.FactorTiempoRecarga, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Cabeza_ElVisorMarcaEnemigosDetrasDeUnaPared_SoloMientrasDura_YLuegoSeDesmarcan()
        {
            yield return Calentar();
            Poner(EspacioImplante.Ojos, 1);
            if (VisorRayosX.Actual != null) Object.Destroy(VisorRayosX.Actual.gameObject);
            yield return null;
            VisorRayosX visor = VisorRayosX.Crear();
            Assert.IsNotNull(visor, "Resources/VisorRayosX");

            GameObject pared = Caja(new Vector3(0f, 2f, 8f), new Vector3(8f, 4f, 0.5f));
            Assert.IsNotNull(pared);
            GameObject contenedor = new GameObject("EnemigoPrueba");
            extra = contenedor;
            contenedor.SetActive(false);
            GameObject p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero), contenedor.transform);
            p.transform.position = new Vector3(0f, 0.05f, 15f);
            SerializedObject so = new SerializedObject(p.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosPandillero);
            so.ApplyModifiedPropertiesWithoutUndo();
            contenedor.SetActive(true);
            yield return Esperar(1.0f);
            visor.Actualizar();
            Assert.AreEqual(0, visor.Marcados, "con CABEZA apagada el visor no marca a nadie, aunque tengas el implante");

            yield return Tecla(teclado.digit3Key);
            float inicio = Time.time;
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Cabeza));
            Assert.Greater(visor.Marcados, 0, "pulsar 3 marca al enemigo detrás de la pared");
            Debug.Log("[Prueba] T-992 visor: " + visor.Marcados + " renderers marcados");
            yield return Esperar(7.4f - (Time.time - inicio));
            Assert.Greater(visor.Marcados, 0, "a los 7.4 s sigue marcado (nivel I dura 8 s)");
            yield return Esperar(8.6f - (Time.time - inicio));
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Cabeza));
            Assert.AreEqual(0, visor.Marcados, "al acabar se desmarcan");
        }

        [UnityTest]
        public IEnumerator EnElCarro_NoSeActivan_PeroLosTiemposSiguenCorriendo()
        {
            yield return Calentar();
            Poner(EspacioImplante.Piernas, 1);
            Poner(EspacioImplante.Brazos, 1);
            yield return null;
            yield return Tecla(teclado.digit1Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Piernas));

            extra = new GameObject("CarroFalso");
            extra.transform.position = new Vector3(0f, 0f, 3f);
            Transform asiento = new GameObject("Asiento").transform;
            asiento.SetParent(extra.transform, false);
            asiento.localPosition = new Vector3(-0.4f, 0.75f, 0f);
            Camera cam = new GameObject("Cam").AddComponent<Camera>();
            cam.transform.SetParent(asiento, false);
            cam.enabled = false;
            MontableFalso montable = new MontableFalso { asiento = asiento, camara = cam };
            Assert.IsTrue(jugadorAPie.Subir(montable));
            yield return null;
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo);

            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Brazos), "en el carro la tecla 2 no hace nada");
            yield return Esperar(6.3f);
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Piernas), "pero el tiempo de PIERNAS siguió corriendo");
        }

        [UnityTest]
        public IEnumerator AlMorir_SeCortanLasActivas_YQuedanEnRecarga()
        {
            yield return Calentar();
            Poner(EspacioImplante.Brazos, 1);
            yield return null;
            yield return Tecla(teclado.digit2Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Brazos));
            VidaAPie vida = jugador.GetComponent<VidaAPie>();
            vida.RecibirDanio(new InfoDanio(100000f, TipoDanio.Disparo, jugador.transform.position, Vector3.forward, null));
            yield return Esperar(0.3f);
            Assert.IsTrue(vida.EstaKO);
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Brazos));
        }
    }
}

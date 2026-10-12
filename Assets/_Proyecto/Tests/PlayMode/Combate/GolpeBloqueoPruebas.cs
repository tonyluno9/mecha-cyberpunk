using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Brazos;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Pruebas;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Combate
{
    /// <summary>Pruebas jugables de T-012 sobre la escena real Pruebas_Tony: clic izquierdo, clic derecho, saco y atacante.</summary>
    public class GolpeBloqueoPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float LadoSaco = 4f;
        private const float Margen = 0.1f;

        private Keyboard teclado;
        private Mouse raton;
        private ControladorMecha controlador;
        private VidaMecha vidaJugador;
        private GolpeMecha golpe;
        private GuardiaMecha guardia;
        private AnimadorBrazos brazos;
        private VidaMecha vidaSaco;
        private AtacanteDePrueba atacante;
        private DatosMecha datos;
        private readonly List<string> consola = new List<string>();
        private readonly List<string> errores = new List<string>();

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            consola.Clear();
            errores.Clear();
            Application.logMessageReceived += GuardarLinea;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= GuardarLinea;
            VaciarEscenaDePruebas();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // La escena cargada sigue viva al terminar la prueba: se vacía para no estorbar a las pruebas siguientes.
        private static void VaciarEscenaDePruebas()
        {
            Scene escena = SceneManager.GetSceneByPath(RutaEscena);
            if (!escena.IsValid())
            {
                return;
            }
            foreach (GameObject raiz in escena.GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
        }

        private void GuardarLinea(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Log)
            {
                consola.Add(mensaje);
            }
            else if (tipo != LogType.Warning)
            {
                errores.Add(mensaje);
            }
        }

        private int Lineas(string fragmento)
        {
            int cuenta = 0;
            foreach (string linea in consola)
            {
                if (linea.Contains(fragmento))
                {
                    cuenta++;
                }
            }
            return cuenta;
        }

        // Carga la escena real. Por defecto el atacante queda apagado para que no moleste en las pruebas del saco.
        private IEnumerator CargarEscena(bool atacanteActivo = false)
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            controlador = Object.FindFirstObjectByType<ControladorMecha>();
            Assert.IsNotNull(controlador, "Falta el mecha del jugador");
            vidaJugador = controlador.GetComponent<VidaMecha>();
            golpe = controlador.GetComponent<GolpeMecha>();
            guardia = controlador.GetComponent<GuardiaMecha>();
            brazos = controlador.GetComponent<AnimadorBrazos>();
            Assert.IsNotNull(golpe, "Falta GolpeMecha en el prefab");
            Assert.IsNotNull(guardia, "Falta GuardiaMecha en el prefab");
            Assert.IsNotNull(brazos, "Falta AnimadorBrazos en el prefab");
            datos = vidaJugador.Datos;

            GameObject saco = GameObject.Find("SacoDePrueba");
            Assert.IsNotNull(saco, "Falta el saco de prueba");
            vidaSaco = saco.GetComponent<VidaMecha>();
            atacante = Object.FindFirstObjectByType<AtacanteDePrueba>();
            Assert.IsNotNull(atacante, "Falta el atacante de prueba");
            atacante.gameObject.SetActive(atacanteActivo);
            yield return Calentar();
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

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
        }

        // Pone la cara del saco más cercana al mecha a la distancia indicada (m desde el centro del mecha), de frente.
        private void ColocarSaco(float distanciaALaCara)
        {
            Vector3 lugar = controlador.transform.position + controlador.transform.forward * (distanciaALaCara + LadoSaco / 2f);
            lugar.y = LadoSaco / 2f;
            vidaSaco.transform.position = lugar;
            Physics.SyncTransforms();
        }

        private static float AnguloY(Quaternion giro) => Mathf.DeltaAngle(0f, giro.eulerAngles.y);

        [UnityTest]
        public IEnumerator Escena_TrasCargar_TieneSacoYAtacanteConLosDatosDeLaTarea()
        {
            yield return CargarEscena(true);
            Assert.AreEqual(LadoPelea.Rival, vidaSaco.GetComponent<PeleadorMecha>().Lado);
            Assert.IsNotNull(vidaSaco.GetComponent<SacoDePrueba>());
            Assert.AreEqual(10f, datos.DanioGolpe);
            Assert.AreEqual(3f, datos.AlcanceGolpe);

            DatosMecha datosAtacante = (DatosMecha)new SerializedObject(atacante).FindProperty("datos").objectReferenceValue;
            Assert.AreEqual(0.6f, datosAtacante.PreparacionGolpe, 1e-4f);

            SerializedProperty lista = new SerializedObject(controlador.GetComponent<PeleadorMecha>()).FindProperty("controlados");
            List<Object> controlados = new List<Object>();
            for (int i = 0; i < lista.arraySize; i++)
            {
                controlados.Add(lista.GetArrayElementAtIndex(i).objectReferenceValue);
            }
            CollectionAssert.Contains(controlados, golpe);
            CollectionAssert.Contains(controlados, guardia);
        }

        [UnityTest]
        public IEnumerator Golpe_ClicIzquierdo_BrazoDerechoSeEchaAtrasSaleAlFrenteYRegresa()
        {
            yield return CargarEscena();
            Transform brazo = brazos.BrazoDerecho;
            float reposo = brazo.localPosition.z;
            float minimo = reposo;
            float maximo = reposo;
            bool impacto = false;
            float zAlImpacto = 0f;
            golpe.Impacto += () => impacto = true;

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            float fin = Time.time + datos.PreparacionGolpe + datos.RecargaGolpe + 0.5f;
            while (Time.time < fin)
            {
                float z = brazo.localPosition.z;
                minimo = Mathf.Min(minimo, z);
                if (!impacto)
                {
                    zAlImpacto = z;
                }
                else
                {
                    maximo = Mathf.Max(maximo, z);
                }
                yield return null;
            }

            Assert.IsTrue(impacto, "Hubo impacto");
            Assert.Less(minimo, reposo - 0.1f, "Se echó atrás en la preparación");
            Assert.Greater(maximo, reposo + 0.3f, "Salió al frente después del impacto");
            Assert.Less(zAlImpacto, reposo, "Antes del impacto el brazo seguía atrás");
            Assert.AreEqual(reposo, brazo.localPosition.z, 0.02f, "Regresó a su sitio");
        }

        [UnityTest]
        public IEnumerator Golpe_ClicsSeguidos_SalenAlRitmoDeLaRecargaYNoSeEncimanSinMoverElBrazoDeMas()
        {
            yield return CargarEscena();
            ColocarSaco(1.5f);
            List<float> tiempos = new List<float>();
            golpe.Impacto += () => tiempos.Add(Time.time);

            float inicio = Time.time;
            float duracion = 4f;
            while (Time.time < inicio + duracion)
            {
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return null;
            }

            float ciclo = datos.PreparacionGolpe + datos.RecargaGolpe;
            int esperados = Mathf.FloorToInt(duracion / ciclo);
            Assert.GreaterOrEqual(tiempos.Count, esperados - 1, "No se pierden golpes");
            Assert.LessOrEqual(tiempos.Count, esperados + 1, "No salen más rápido que su ritmo");
            for (int i = 1; i < tiempos.Count; i++)
            {
                Assert.GreaterOrEqual(tiempos[i] - tiempos[i - 1], ciclo - 0.05f, "Entre impactos pasa al menos preparación + recarga");
            }
            Assert.AreEqual(vidaJugador.Maxima, vidaJugador.Actual, "Mis golpes no me dañan");
            Assert.AreEqual(tiempos.Count, Lineas("Saco de prueba recibió 10"), "Un aviso en consola por golpe");
        }

        [UnityTest]
        public IEnumerator Golpe_SacoACortaDistancia_Quita10ConAvisoEnConsolaYNuncaBajaMiVida()
        {
            yield return CargarEscena();
            ColocarSaco(2.5f);
            float vidaAntes = vidaSaco.Actual;

            yield return Clic();
            yield return Esperar(datos.PreparacionGolpe + 0.2f);

            Assert.AreEqual(vidaAntes - 10f, vidaSaco.Actual, 1e-4f);
            Assert.AreEqual(1, Lineas("Saco de prueba recibió 10 de daño"));
            Assert.AreEqual(vidaJugador.Maxima, vidaJugador.Actual, "Mis golpes no me dañan");
            Assert.IsFalse(vidaJugador.EstaKO);
        }

        [UnityTest]
        public IEnumerator Golpe_SacoMasLejosQueElAlcance_NoLeHaceNada()
        {
            yield return CargarEscena();
            ColocarSaco(datos.AlcanceGolpe + 0.5f);
            float vidaAntes = vidaSaco.Actual;
            int impactos = 0;
            golpe.Impacto += () => impactos++;

            yield return Clic();
            yield return Esperar(datos.PreparacionGolpe + 0.2f);

            Assert.AreEqual(1, impactos, "El golpe salió");
            Assert.AreEqual(vidaAntes, vidaSaco.Actual, "Pero no llegó al saco");
            Assert.AreEqual(0, Lineas("Saco de prueba recibió"));
            Assert.AreEqual(vidaJugador.Maxima, vidaJugador.Actual);
        }

        [UnityTest]
        public IEnumerator Golpe_SacoJustoDentroYJustoFueraDelAlcance_RespetaElLimite()
        {
            yield return CargarEscena();
            float vidaAntes = vidaSaco.Actual;

            ColocarSaco(datos.AlcanceGolpe - 0.05f);
            yield return Clic();
            yield return Esperar(datos.PreparacionGolpe + datos.RecargaGolpe + 0.1f);
            Assert.AreEqual(vidaAntes - 10f, vidaSaco.Actual, 1e-4f, "A 2.95 m sí");

            ColocarSaco(datos.AlcanceGolpe + 0.05f);
            yield return Clic();
            yield return Esperar(datos.PreparacionGolpe + datos.RecargaGolpe + 0.1f);
            Assert.AreEqual(vidaAntes - 10f, vidaSaco.Actual, 1e-4f, "A 3.05 m no");
        }

        [UnityTest]
        public IEnumerator Guardia_ClicDerechoSostenido_LosBrazosSeCruzanYElGolpeNoSale()
        {
            yield return CargarEscena();
            ColocarSaco(1.5f);
            float vidaAntes = vidaSaco.Actual;
            int impactos = 0;
            golpe.Impacto += () => impactos++;
            float yDerechoReposo = AnguloY(brazos.BrazoDerecho.localRotation);
            float xDerechoReposo = brazos.BrazoDerecho.localPosition.x;
            float xIzquierdoReposo = brazos.BrazoIzquierdo.localPosition.x;

            Press(raton.rightButton);
            yield return Esperar(datos.TiempoSubirGuardia + 0.3f);

            Assert.IsTrue(guardia.EstaArriba);
            Assert.AreEqual(1f, guardia.Progreso);
            Assert.Less(AnguloY(brazos.BrazoDerecho.localRotation), yDerechoReposo - 30f, "El derecho apunta a la izquierda");
            Assert.Greater(AnguloY(brazos.BrazoIzquierdo.localRotation), 30f, "El izquierdo apunta a la derecha");
            Assert.Less(brazos.BrazoDerecho.localPosition.x, xDerechoReposo - 0.3f, "El derecho cruza hacia el centro");
            Assert.Greater(brazos.BrazoIzquierdo.localPosition.x, xIzquierdoReposo + 0.3f, "El izquierdo cruza hacia el centro");

            yield return Clic();
            yield return Esperar(datos.PreparacionGolpe + 0.3f);
            Assert.AreEqual(0, impactos, "Con la guardia arriba el clic izquierdo no golpea");
            Assert.AreEqual(vidaAntes, vidaSaco.Actual);
            Assert.AreEqual(FaseGolpe.Listo, golpe.Fase);

            Release(raton.rightButton);
            yield return Esperar(datos.TiempoSubirGuardia + 0.4f);
            Assert.IsFalse(guardia.Levantada, "Al soltar la guardia baja");
            Assert.AreEqual(yDerechoReposo, AnguloY(brazos.BrazoDerecho.localRotation), 2f, "Brazos de vuelta");

            yield return Clic();
            yield return Esperar(datos.PreparacionGolpe + 0.2f);
            Assert.AreEqual(1, impactos, "Sin guardia vuelve a golpear");
        }

        [UnityTest]
        public IEnumerator Guardia_DuranteLaPreparacionDeUnGolpe_NoSubeHastaElImpacto()
        {
            yield return CargarEscena();
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            Press(raton.rightButton);
            yield return null;
            yield return null;

            Assert.AreEqual(FaseGolpe.Preparando, golpe.Fase);
            Assert.AreEqual(0f, guardia.Progreso, "No sube mientras el golpe prepara");

            yield return Esperar(datos.PreparacionGolpe + datos.TiempoSubirGuardia + 0.3f);
            Assert.IsTrue(guardia.EstaArriba, "Después del impacto sí sube");
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Atacante_SinGuardia_MiBarraBajaElDanioCompleto()
        {
            yield return CargarEscena(true);
            AcelerarAtacante();

            yield return EsperarImpactoDelAtacante();

            Assert.AreEqual(vidaJugador.Maxima - 10f, vidaJugador.Actual, 1e-4f);
            Assert.AreEqual(0, Lineas("Bloqueo"));
        }

        [UnityTest]
        public IEnumerator Atacante_ConGuardiaDesdeAntesDelAviso_BajaMenosYDiceBloqueoNormal()
        {
            yield return CargarEscena(true);
            AcelerarAtacante();
            Press(raton.rightButton);

            yield return EsperarImpactoDelAtacante();

            float esperado = vidaJugador.Maxima - 10f * (1f - datos.ReduccionBloqueo);
            Assert.AreEqual(esperado, vidaJugador.Actual, 1e-4f);
            Assert.AreEqual(1, Lineas("Bloqueo normal"));
            Assert.AreEqual(0, Lineas("Bloqueo perfecto"));
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Atacante_GuardiaEnLaUltimaParteDelAviso_NoBajaYDiceBloqueoPerfecto()
        {
            yield return CargarEscena(true);
            AcelerarAtacante();
            // Se sube la guardia tarde en el aviso para que llegue arriba menos de la ventana antes del impacto.
            float retraso = PreparacionDelAtacante() - datos.TiempoSubirGuardia - datos.VentanaBloqueoPerfecto / 2f;

            while (!atacante.EnAviso)
            {
                yield return null;
            }
            float inicioAviso = Time.time;
            while (Time.time < inicioAviso + retraso)
            {
                yield return null;
            }
            Press(raton.rightButton);
            yield return EsperarImpactoDelAtacante();

            Assert.AreEqual(vidaJugador.Maxima, vidaJugador.Actual, 1e-4f);
            Assert.AreEqual(1, Lineas("Bloqueo perfecto"));
            Assert.AreEqual(0, Lineas("Bloqueo normal"));
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Atacante_GuardiaDemasiadoTarde_ElGolpeEntraCompleto()
        {
            yield return CargarEscena(true);
            AcelerarAtacante();
            float retraso = PreparacionDelAtacante() - datos.TiempoSubirGuardia / 3f;

            while (!atacante.EnAviso)
            {
                yield return null;
            }
            float inicioAviso = Time.time;
            while (Time.time < inicioAviso + retraso)
            {
                yield return null;
            }
            Press(raton.rightButton);
            yield return EsperarImpactoDelAtacante();

            Assert.AreEqual(vidaJugador.Maxima - 10f, vidaJugador.Actual, 1e-4f, "La guardia aún subía");
            Assert.AreEqual(0, Lineas("Bloqueo"));
            Release(raton.rightButton);
        }

        [UnityTest]
        public IEnumerator Atacante_AlPrepararse_ElCuboSePintaDeNaranja()
        {
            yield return CargarEscena(true);
            AcelerarAtacante();
            MeshRenderer visual = atacante.GetComponent<MeshRenderer>();
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();

            visual.GetPropertyBlock(bloque);
            Color reposo = bloque.GetColor("_BaseColor");
            while (!atacante.EnAviso)
            {
                yield return null;
            }
            yield return null;
            visual.GetPropertyBlock(bloque);
            Color aviso = bloque.GetColor("_BaseColor");

            Assert.Greater(aviso.r, 0.8f);
            Assert.Greater(aviso.r, aviso.b + 0.5f, "Naranja");
            Assert.AreNotEqual(reposo, aviso);
        }

        [UnityTest]
        public IEnumerator Atacante_ConElJugadorFueraDeAlcance_NoLeHaceDanio()
        {
            yield return CargarEscena(true);
            AcelerarAtacante();
            controlador.enabled = false;
            CharacterController cc = controlador.GetComponent<CharacterController>();
            cc.enabled = false;
            controlador.transform.position = new Vector3(0f, 0.05f, -4f);
            cc.enabled = true;
            Physics.SyncTransforms();

            yield return EsperarImpactoDelAtacante();

            Assert.AreEqual(vidaJugador.Maxima, vidaJugador.Actual);
        }

        [UnityTest]
        public IEnumerator Ko_ElJugadorNoPuedeGolpearNiBloquear()
        {
            yield return CargarEscena();
            ColocarSaco(1.5f);
            int impactos = 0;
            golpe.Impacto += () => impactos++;
            float vidaSacoAntes = vidaSaco.Actual;

            vidaJugador.RecibirDanio(new InfoDanio(vidaJugador.Maxima * 2f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            yield return null;
            Assert.IsTrue(vidaJugador.EstaKO);
            Assert.IsFalse(golpe.enabled);
            Assert.IsFalse(guardia.enabled);

            yield return Clic();
            Press(raton.rightButton);
            yield return Esperar(datos.PreparacionGolpe + datos.TiempoSubirGuardia + 0.3f);
            Release(raton.rightButton);

            Assert.AreEqual(0, impactos, "En KO no golpea");
            Assert.AreEqual(vidaSacoAntes, vidaSaco.Actual);
            Assert.AreEqual(0f, guardia.Progreso, "En KO no bloquea");
            Assert.IsFalse(guardia.EstaArriba);
        }

        [UnityTest]
        public IEnumerator Soak_GolpesYGuardiaMezclados_NoDanErrores()
        {
            yield return CargarEscena(true);
            AcelerarAtacante();
            ColocarSaco(2f);
            float fin = Time.time + 12f;
            int ronda = 0;
            while (Time.time < fin)
            {
                ronda++;
                if (ronda % 4 == 0)
                {
                    Press(raton.rightButton);
                }
                else
                {
                    Release(raton.rightButton);
                }
                yield return Clic();
            }
            Release(raton.rightButton);
            yield return null;
            Assert.IsEmpty(errores, "Sin errores en la consola");
        }

        private float PreparacionDelAtacante()
        {
            DatosMecha datosAtacante = (DatosMecha)new SerializedObject(atacante).FindProperty("datos").objectReferenceValue;
            return datosAtacante.PreparacionGolpe;
        }

        // El atacante de la escena espera 5 s entre golpes; en las pruebas se acorta.
        private void AcelerarAtacante()
        {
            SerializedObject so = new SerializedObject(atacante);
            so.FindProperty("intervalo").floatValue = 0.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private IEnumerator EsperarImpactoDelAtacante()
        {
            while (!atacante.EnAviso)
            {
                yield return null;
            }
            while (atacante.EnAviso)
            {
                yield return null;
            }
            // El impacto ya ocurrió en el frame en que terminó el aviso; un frame más para que se asiente.
            yield return null;
        }
    }
}

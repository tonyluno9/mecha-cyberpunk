using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Golpe;
using MechaCyberpunk.Combate.Habilidades;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.IA.Rival;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Combate
{
    /// <summary>Pruebas jugables de T-175: los efectos de las 8 habilidades con mechas armados por código, sin escena.</summary>
    public class HabilidadesPruebas : InputTestFixture
    {
        private const string CarpetaHabilidades = "Assets/_Proyecto/Combate/Datos/Habilidades/DatosHabilidad_";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float VidaInicial = 100f;
        private const float Tolerancia = 0.01f;
        // Cuadros por segundo simulados: sin esto, en batchmode los cuadros duran décimas de milisegundo y los pasos
        // de movimiento caen por debajo del mínimo del CharacterController.
        private const int CuadrosPorSegundo = 60;

        private Mouse raton;
        private DatosMecha datos;
        private ConfigPresentacionRival configRival;
        private readonly List<Object> creados = new List<Object>();
        private readonly List<InputAction> acciones = new List<InputAction>();
        private readonly List<float> impactos = new List<float>();

        public override void Setup()
        {
            base.Setup();
            Time.captureFramerate = CuadrosPorSegundo;
            EventosPelea.LimpiarSuscriptores();
            raton = InputSystem.AddDevice<Mouse>();
            datos = NuevoDatos();
            configRival = ScriptableObject.CreateInstance<ConfigPresentacionRival>();
            creados.Add(configRival);
            impactos.Clear();
            EventosPelea.ImpactoRecibido += AlImpacto;

            GameObject suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            suelo.name = "Suelo";
            suelo.transform.position = new Vector3(0f, -0.5f, 0f);
            suelo.transform.localScale = new Vector3(200f, 1f, 200f);
            creados.Add(suelo);
        }

        public override void TearDown()
        {
            EventosPelea.ImpactoRecibido -= AlImpacto;
            EventosPelea.LimpiarSuscriptores();
            // Al revés de como se crearon: el mecha antes que el asset de su acción.
            for (int i = creados.Count - 1; i >= 0; i--)
            {
                if (creados[i] != null)
                {
                    Object.DestroyImmediate(creados[i]);
                }
            }
            creados.Clear();
            foreach (InputAction accion in acciones)
            {
                accion.Dispose();
            }
            acciones.Clear();
            DestruirEfectos();
            Time.captureFramerate = 0;
            base.TearDown();
        }

        private void AlImpacto(IPeleador objetivo, InfoDanio info, ResultadoDanio resultado)
        {
            impactos.Add(resultado.DanioAplicado);
        }

        private static void DestruirEfectos()
        {
            foreach (ProyectilHabilidad p in Object.FindObjectsByType<ProyectilHabilidad>(FindObjectsSortMode.None)) Object.DestroyImmediate(p.gameObject);
            foreach (DestelloCono d in Object.FindObjectsByType<DestelloCono>(FindObjectsSortMode.None)) Object.DestroyImmediate(d.gameObject);
        }

        // Ayudas de armado

        private static DatosHabilidad Cargar(string nombre)
        {
            DatosHabilidad habilidad = AssetDatabase.LoadAssetAtPath<DatosHabilidad>(CarpetaHabilidades + nombre + ".asset");
            Assert.IsNotNull(habilidad, "Falta el asset de la habilidad " + nombre);
            return habilidad;
        }

        private DatosMecha NuevoDatos()
        {
            DatosMecha d = ScriptableObject.CreateInstance<DatosMecha>();
            SerializedObject so = new SerializedObject(d);
            so.FindProperty("nombre").stringValue = "Mecha de prueba";
            so.FindProperty("vidaMaxima").floatValue = VidaInicial;
            so.ApplyModifiedPropertiesWithoutUndo();
            creados.Add(d);
            return d;
        }

        private GameObject CrearMecha(string nombre, Vector3 posicion, LadoPelea lado, bool lanzador = false, bool guardia = false)
        {
            GameObject mecha = new GameObject(nombre);
            mecha.SetActive(false);
            mecha.transform.position = posicion;
            CharacterController cc = mecha.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0f, 1f, 0f);
            mecha.AddComponent<VidaMecha>().Configurar(datos);
            PeleadorMecha peleador = mecha.AddComponent<PeleadorMecha>();
            SerializedObject so = new SerializedObject(peleador);
            so.FindProperty("lado").enumValueIndex = (int)lado;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (guardia)
            {
                GuardiaMecha g = mecha.AddComponent<GuardiaMecha>();
                InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
                creados.Add(asset);
                InputAction accion = asset.AddActionMap("Mecha").AddAction("Bloquear", InputActionType.Button, "<Mouse>/rightButton");
                acciones.Add(accion);
                SerializedObject soGuardia = new SerializedObject(g);
                soGuardia.FindProperty("bloquear").objectReferenceValue = InputActionReference.Create(accion);
                soGuardia.ApplyModifiedPropertiesWithoutUndo();
            }
            if (lanzador)
            {
                mecha.AddComponent<LanzadorHabilidades>();
            }
            mecha.SetActive(true);
            creados.Add(mecha);
            return mecha;
        }

        private GameObject CrearSacoGrande(Vector3 posicion)
        {
            GameObject saco = new GameObject("SacoGrande");
            saco.SetActive(false);
            saco.transform.position = posicion;
            BoxCollider caja = saco.AddComponent<BoxCollider>();
            caja.size = new Vector3(8f, 3f, 1f);
            saco.AddComponent<VidaMecha>().Configurar(datos);
            saco.AddComponent<PeleadorMecha>();
            saco.SetActive(true);
            creados.Add(saco);
            return saco;
        }

        private GameObject CrearRival(Vector3 posicion, GameObject objetivo)
        {
            GameObject rival = new GameObject("Rival");
            rival.SetActive(false);
            rival.transform.position = posicion;
            CharacterController cc = rival.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0f, 1f, 0f);
            rival.AddComponent<VidaMecha>().Configurar(datos);
            PeleadorMecha peleador = rival.AddComponent<PeleadorMecha>();

            GameObject brazo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(brazo.GetComponent<Collider>());
            brazo.name = "Brazo";
            brazo.transform.SetParent(rival.transform, false);
            brazo.transform.localPosition = new Vector3(0.6f, 1.2f, 0.3f);
            MeshRenderer pieza = brazo.GetComponent<MeshRenderer>();
            pieza.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            creados.Add(pieza.sharedMaterial);

            DatosRival datosRival = ScriptableObject.CreateInstance<DatosRival>();
            SerializedObject soDatos = new SerializedObject(datosRival);
            soDatos.FindProperty("mecha").objectReferenceValue = datos;
            soDatos.ApplyModifiedPropertiesWithoutUndo();
            creados.Add(datosRival);

            RivalBasico cerebro = rival.AddComponent<RivalBasico>();
            SerializedObject soRival = new SerializedObject(cerebro);
            soRival.FindProperty("datos").objectReferenceValue = datosRival;
            soRival.FindProperty("config").objectReferenceValue = configRival;
            soRival.FindProperty("objetivo").objectReferenceValue = objetivo;
            soRival.FindProperty("brazo").objectReferenceValue = brazo.transform;
            SerializedProperty piezas = soRival.FindProperty("piezasBrazo");
            piezas.arraySize = 1;
            piezas.GetArrayElementAtIndex(0).objectReferenceValue = pieza;
            soRival.FindProperty("controlAlArrancar").boolValue = true;
            soRival.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soPeleador = new SerializedObject(peleador);
            soPeleador.FindProperty("lado").enumValueIndex = (int)LadoPelea.Rival;
            SerializedProperty lista = soPeleador.FindProperty("controlados");
            lista.arraySize = 1;
            lista.GetArrayElementAtIndex(0).objectReferenceValue = cerebro;
            soPeleador.ApplyModifiedPropertiesWithoutUndo();

            rival.SetActive(true);
            creados.Add(rival);
            return rival;
        }

        private static IEnumerator Calentar()
        {
            Physics.SyncTransforms();
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
            Physics.SyncTransforms();
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private static InfoDanio Golpe(float cantidad)
        {
            return new InfoDanio(cantidad, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null);
        }

        private static int Contar<T>() where T : Object
        {
            return Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length;
        }

        private static VidaMecha VidaDe(GameObject mecha) => mecha.GetComponent<VidaMecha>();
        private static LanzadorHabilidades LanzadorDe(GameObject mecha) => mecha.GetComponent<LanzadorHabilidades>();

        // a) Misil

        [UnityTest]
        public IEnumerator MisilI_AUnSacoA10m_QuitaVeinteYElProyectilDesaparece()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject saco = CrearMecha("Saco", new Vector3(0f, 0f, 10f), LadoPelea.Rival);
            yield return Calentar();

            Assert.IsTrue(LanzadorDe(lanzador).Lanzar(Cargar("Misil"), 1, false));
            yield return Esperar(3f);

            Assert.AreEqual(VidaInicial - 20f, VidaDe(saco).Actual, Tolerancia);
            Assert.AreEqual(0, Contar<ProyectilHabilidad>(), "El proyectil sigue vivo");
            Assert.AreEqual(VidaInicial, VidaDe(lanzador).Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator MisilI_AUnSacoA30m_MasQueElAlcance_NoLeQuitaNada()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject saco = CrearMecha("Saco", new Vector3(0f, 0f, 30f), LadoPelea.Rival);
            yield return Calentar();

            LanzadorDe(lanzador).Lanzar(Cargar("Misil"), 1, false);
            yield return Esperar(3f);

            Assert.AreEqual(VidaInicial, VidaDe(saco).Actual, Tolerancia);
            Assert.AreEqual(0, Contar<ProyectilHabilidad>());
        }

        // b) Misil contra la guardia

        [UnityTest]
        public IEnumerator Misil_ContraGuardiaArriba_EntraConLaReduccionDelBloqueo()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject objetivo = CrearMecha("Objetivo", new Vector3(0f, 0f, 10f), LadoPelea.Rival, guardia: true);
            yield return Calentar();

            Press(raton.rightButton);
            yield return Esperar(0.6f);
            Assert.IsTrue(objetivo.GetComponent<GuardiaMecha>().EstaArriba, "La guardia no subió");

            LanzadorDe(lanzador).Lanzar(Cargar("Misil"), 1, false);
            yield return Esperar(2f);

            float esperado = 20f * (1f - datos.ReduccionBloqueo);
            Assert.AreEqual(VidaInicial - esperado, VidaDe(objetivo).Actual, Tolerancia);
        }

        // c) Salva

        [UnityTest]
        public IEnumerator SalvaI_TresProyectiles_UnSacoGrandeRecibe24SinPasarDe8PorProyectil()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject saco = CrearSacoGrande(new Vector3(0f, 1.5f, 10f));
            yield return Calentar();

            LanzadorDe(lanzador).Lanzar(Cargar("Salva"), 1, true);
            Assert.AreEqual(3, Contar<ProyectilHabilidad>());
            yield return Esperar(3f);

            Assert.AreEqual(VidaInicial - 24f, VidaDe(saco).Actual, Tolerancia);
            Assert.AreEqual(3, impactos.Count);
            foreach (float danio in impactos)
            {
                Assert.LessOrEqual(danio, 8f + Tolerancia);
            }
        }

        // d) Lanzallamas

        [UnityTest]
        public IEnumerator LanzallamasII_DentroDelCono_QuitaDiecisieteYMedio_FueraDelCono_Nada_ElPropioMecha_Nada()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject enfrente = CrearMecha("Enfrente", new Vector3(0f, 0f, 3f), LadoPelea.Rival);
            GameObject aLaDerecha = CrearMecha("ALaDerecha", new Vector3(3f, 0f, 0f), LadoPelea.Rival);
            yield return Calentar();

            LanzadorDe(lanzador).Lanzar(Cargar("Lanzallamas"), 2, false);
            Assert.AreEqual(1, Contar<DestelloCono>());
            yield return null;

            Assert.AreEqual(VidaInicial - 17.5f, VidaDe(enfrente).Actual, Tolerancia);
            Assert.AreEqual(VidaInicial, VidaDe(aLaDerecha).Actual, Tolerancia);
            Assert.AreEqual(VidaInicial, VidaDe(lanzador).Actual, Tolerancia);
            yield return Esperar(0.5f);
            Assert.AreEqual(0, Contar<DestelloCono>(), "El destello no se quitó");
        }

        // e) Martillo

        [UnityTest]
        public IEnumerator MartilloI_ConLaGuardiaArriba_EntraCompleto()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject objetivo = CrearMecha("Objetivo", new Vector3(0f, 0f, 2.2f), LadoPelea.Rival, guardia: true);
            yield return Calentar();

            Press(raton.rightButton);
            yield return Esperar(0.6f);
            Assert.IsTrue(objetivo.GetComponent<GuardiaMecha>().EstaArriba, "La guardia no subió");

            LanzadorDe(lanzador).Lanzar(Cargar("Martillo"), 1, false);
            yield return null;

            Assert.AreEqual(VidaInicial - 16f, VidaDe(objetivo).Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator MartilloI_ConLaGuardiaAbajo_EntraCompleto()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject objetivo = CrearMecha("Objetivo", new Vector3(0f, 0f, 2.2f), LadoPelea.Rival, guardia: true);
            yield return Calentar();

            LanzadorDe(lanzador).Lanzar(Cargar("Martillo"), 1, false);
            yield return null;

            Assert.AreEqual(VidaInicial - 16f, VidaDe(objetivo).Actual, Tolerancia);
        }

        // f) Descarga y aturdimiento

        [UnityTest]
        public IEnumerator DescargaI_AUnRivalConControl_QuitaSeisYLoAturdeUnSegundo()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject rival = CrearRival(new Vector3(0f, 0f, 3f), lanzador);
            yield return Calentar();
            PeleadorMecha peleador = rival.GetComponent<PeleadorMecha>();
            RivalBasico cerebro = rival.GetComponent<RivalBasico>();
            Assert.IsTrue(peleador.ControlActivo);
            int golpes = 0;
            cerebro.Impacto += () => golpes++;

            LanzadorDe(lanzador).Lanzar(Cargar("Descarga"), 1, false);
            yield return null;
            Vector3 alAturdirse = rival.transform.position;

            Assert.AreEqual(VidaInicial - 6f, VidaDe(rival).Actual, Tolerancia);
            Assert.IsTrue(peleador.EstaAturdido);
            Assert.IsTrue(peleador.ControlActivo, "El aturdimiento no debe cambiar ControlActivo");
            yield return Esperar(0.85f);
            Assert.IsTrue(peleador.EstaAturdido);
            Assert.IsFalse(cerebro.enabled, "El cerebro del rival debía estar apagado");
            Assert.AreEqual(0f, Vector3.Distance(alAturdirse, rival.transform.position), 0.001f, "El rival se movió aturdido");
            Assert.AreEqual(0, golpes, "El rival golpeó aturdido");
            Assert.AreEqual(VidaInicial, VidaDe(lanzador).Actual, Tolerancia);

            yield return Esperar(0.3f);
            Assert.IsFalse(peleador.EstaAturdido);
            Assert.IsTrue(cerebro.enabled, "El cerebro del rival no volvió");
            Assert.IsTrue(peleador.ControlActivo);
        }

        [UnityTest]
        public IEnumerator Descarga_SiElFlujoLeQuitaElControlAMedias_AlTerminarSigueSinControl()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject rival = CrearRival(new Vector3(0f, 0f, 3f), lanzador);
            yield return Calentar();
            PeleadorMecha peleador = rival.GetComponent<PeleadorMecha>();

            LanzadorDe(lanzador).Lanzar(Cargar("Descarga"), 1, false);
            yield return Esperar(0.4f);
            peleador.ActivarControl(false);
            yield return Esperar(1.0f);

            Assert.IsFalse(peleador.ControlActivo);
            Assert.IsFalse(peleador.EstaAturdido);
            Assert.IsFalse(rival.GetComponent<RivalBasico>().enabled, "El rival recuperó el control al terminar el aturdimiento");
        }

        [UnityTest]
        public IEnumerator Aturdir_ARivalPreparandoUnGolpe_LoCancelaYNoDejaElBrazoPegado()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador);
            GameObject rival = CrearRival(new Vector3(0f, 0f, 2.2f), lanzador);
            RivalBasico cerebro = rival.GetComponent<RivalBasico>();
            Quaternion reposo = Quaternion.identity;

            float limite = Time.time + 8f;
            while (!cerebro.EnAviso && Time.time < limite)
            {
                yield return null;
            }
            Assert.IsTrue(cerebro.EnAviso, "El rival nunca preparó un golpe");
            yield return Esperar(0.1f);
            Assert.IsTrue(cerebro.EnAviso, "El golpe terminó antes de aturdir");
            Assert.Greater(Quaternion.Angle(reposo, cerebro.Brazo.localRotation), 1f, "El brazo no se echó atrás");

            rival.GetComponent<PeleadorMecha>().Aturdir(1f);
            yield return null;

            Assert.IsFalse(cerebro.EnAviso);
            Assert.AreEqual(FaseGolpe.Listo, cerebro.Fase);
            Assert.Less(Quaternion.Angle(reposo, cerebro.Brazo.localRotation), 0.01f, "El brazo quedó pegado");
        }

        // g) Escudo

        [UnityTest]
        public IEnumerator EscudoI_ReduceElDanioLaMitadTresSegundosYDespuesNo()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            yield return Calentar();
            LanzadorHabilidades lanzador = LanzadorDe(mecha);

            Assert.IsTrue(lanzador.Lanzar(Cargar("Escudo"), 1, true));
            yield return null;
            Assert.IsTrue(lanzador.EscudoActivo);
            Assert.AreEqual(1, Contar<EscudoVisual>());
            VidaDe(mecha).RecibirDanio(Golpe(10f));
            Assert.AreEqual(VidaInicial - 5f, VidaDe(mecha).Actual, Tolerancia);

            yield return Esperar(3.1f);
            Assert.IsFalse(lanzador.EscudoActivo);
            Assert.AreEqual(1f, VidaDe(mecha).FactorEscudo);
            Assert.AreEqual(0, Contar<EscudoVisual>());
            VidaDe(mecha).RecibirDanio(Golpe(10f));
            Assert.AreEqual(VidaInicial - 15f, VidaDe(mecha).Actual, Tolerancia);
        }

        [UnityTest]
        public IEnumerator Escudo_ConLaPiedadDelRival_LosDosFactoresSeMultiplicanYLaPiedadSigue()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            yield return Calentar();
            VidaMecha vida = VidaDe(mecha);
            vida.MultiplicadorDanioRecibido = 1.5f;

            LanzadorDe(mecha).Lanzar(Cargar("Escudo"), 1, true);
            yield return null;
            vida.RecibirDanio(Golpe(10f));
            Assert.AreEqual(VidaInicial - 7.5f, vida.Actual, Tolerancia);

            yield return Esperar(3.1f);
            vida.RecibirDanio(Golpe(10f));
            Assert.AreEqual(VidaInicial - 7.5f - 15f, vida.Actual, Tolerancia);
            Assert.AreEqual(1.5f, vida.MultiplicadorDanioRecibido, 1e-5f);
        }

        [UnityTest]
        public IEnumerator Escudo_AlLlegarAKO_SeQuitaElFactorYLaEsfera()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            yield return Calentar();
            VidaMecha vida = VidaDe(mecha);

            LanzadorDe(mecha).Lanzar(Cargar("Escudo"), 1, true);
            yield return null;
            vida.RecibirDanio(Golpe(1000f));
            yield return null;

            Assert.IsTrue(vida.EstaKO);
            Assert.AreEqual(1f, vida.FactorEscudo);
            Assert.IsFalse(LanzadorDe(mecha).EscudoActivo);
            Assert.AreEqual(0, Contar<EscudoVisual>());
            Assert.IsFalse(LanzadorDe(mecha).Lanzar(Cargar("Misil"), 1, false), "En KO no debe lanzar");
            Assert.AreEqual(0, Contar<ProyectilHabilidad>());
        }

        // h) Embestida

        [UnityTest]
        public IEnumerator EmbestidaI_SinNadaEnfrente_Avanza5mEn0_3s()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            yield return Calentar();
            LanzadorHabilidades lanzador = LanzadorDe(mecha);

            float inicio = Time.time;
            Assert.IsTrue(lanzador.Lanzar(Cargar("Embestida"), 1, true));
            Assert.IsTrue(lanzador.Embistiendo);
            Assert.IsFalse(lanzador.Lanzar(Cargar("Embestida"), 1, true), "No puede embestir dos veces a la vez");
            float limite = Time.time + 2f;
            while (lanzador.Embistiendo && Time.time < limite)
            {
                yield return null;
            }

            Assert.IsFalse(lanzador.Embistiendo);
            Assert.AreEqual(5f, mecha.transform.position.z, 0.2f);
            Assert.AreEqual(0f, mecha.transform.position.x, 0.01f);
            Assert.AreEqual(0.3f, Time.time - inicio, 0.05f);
        }

        [UnityTest]
        public IEnumerator EmbestidaI_ContraUnaParedA2m_SeDetieneEnLaPared()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject pared = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pared.transform.position = new Vector3(0f, 1.5f, 2.5f);
            pared.transform.localScale = new Vector3(10f, 3f, 1f);
            creados.Add(pared);
            yield return Calentar();
            LanzadorHabilidades lanzador = LanzadorDe(mecha);

            lanzador.Lanzar(Cargar("Embestida"), 1, true);
            yield return Esperar(0.6f);

            Assert.IsFalse(lanzador.Embistiendo);
            float z = mecha.transform.position.z;
            Assert.Less(z, 2f, "Atravesó la pared");
            Assert.Greater(z, 1.2f, "Se detuvo lejos de la pared");
        }

        [UnityTest]
        public IEnumerator EmbestidaI_ContraUnSacoA3m_LeQuita14UnaVezYLoEmpuja()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject saco = CrearMecha("Saco", new Vector3(0f, 0f, 3f), LadoPelea.Rival);
            yield return Calentar();

            LanzadorDe(mecha).Lanzar(Cargar("Embestida"), 1, true);
            yield return Esperar(0.8f);

            Assert.AreEqual(VidaInicial - 14f, VidaDe(saco).Actual, Tolerancia);
            Assert.AreEqual(1, impactos.Count, "Debía pegar una sola vez");
            Assert.Greater(saco.transform.position.z, 4.5f, "No lo empujó");
            Assert.IsFalse(LanzadorDe(mecha).Embistiendo);
        }

        // i) Onda de calor

        [UnityTest]
        public IEnumerator OndaDeCalorI_UnSacoA2mAtras_TambienPierde15YSeEmpuja()
        {
            GameObject lanzador = CrearMecha("Lanzador", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            GameObject saco = CrearMecha("Saco", new Vector3(0f, 0f, -2f), LadoPelea.Rival);
            yield return Calentar();

            LanzadorDe(lanzador).Lanzar(Cargar("OndaDeCalor"), 1, true);
            yield return Esperar(0.5f);

            Assert.AreEqual(VidaInicial - 15f, VidaDe(saco).Actual, Tolerancia);
            Assert.Less(saco.transform.position.z, -3f, "No lo empujó hacia afuera");
            Assert.AreEqual(VidaInicial, VidaDe(lanzador).Actual, Tolerancia);
        }

        // j) Limpieza

        [UnityTest]
        public IEnumerator Lanzar50VecesSeguidas_NoDejaObjetosDeEfectoVivos()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador, lanzador: true);
            CrearMecha("Saco", new Vector3(0f, 0f, 4f), LadoPelea.Rival);
            yield return Calentar();
            string[] nombres = { "Misil", "Salva", "Lanzallamas", "OndaDeCalor", "Descarga", "Escudo", "Martillo", "Embestida" };
            DatosHabilidad[] todas = new DatosHabilidad[nombres.Length];
            for (int i = 0; i < nombres.Length; i++)
            {
                todas[i] = Cargar(nombres[i]);
            }

            int lanzadas = 0;
            for (int i = 0; i < 50; i++)
            {
                while (LanzadorDe(mecha).Embistiendo)
                {
                    yield return null;
                }
                if (LanzadorDe(mecha).Lanzar(todas[i % todas.Length], 1 + i % 4, i % 2 == 0))
                {
                    lanzadas++;
                }
                yield return null;
            }
            Assert.Greater(lanzadas, 25, "Casi ningún lanzamiento se ejecutó");

            yield return Esperar(4f);

            Assert.AreEqual(0, Contar<ProyectilHabilidad>(), "Proyectiles vivos");
            Assert.AreEqual(0, Contar<DestelloCono>(), "Destellos vivos");
            Assert.AreEqual(0, Contar<EscudoVisual>(), "Escudos vivos");
            Assert.AreEqual(0, Contar<EmpujeMecha>(), "Empujes vivos");
            Assert.IsFalse(LanzadorDe(mecha).EscudoActivo);
        }

        // Aturdir en PeleadorMecha

        private class ComponenteControlado : MonoBehaviour
        {
        }

        [UnityTest]
        public IEnumerator Aturdir_ApagaLosControladosYLosVuelveAEncender_SinSumarse()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador);
            mecha.SetActive(false);
            ComponenteControlado controlado = mecha.AddComponent<ComponenteControlado>();
            PeleadorMecha peleador = mecha.GetComponent<PeleadorMecha>();
            SerializedObject so = new SerializedObject(peleador);
            SerializedProperty lista = so.FindProperty("controlados");
            lista.arraySize = 1;
            lista.GetArrayElementAtIndex(0).objectReferenceValue = controlado;
            so.ApplyModifiedPropertiesWithoutUndo();
            mecha.SetActive(true);
            yield return Calentar();

            peleador.Aturdir(1.0f);
            Assert.IsFalse(controlado.enabled);
            yield return Esperar(0.5f);
            peleador.Aturdir(0.2f);
            yield return Esperar(0.4f);
            Assert.IsFalse(controlado.enabled, "Un aturdimiento corto no debe acortar el largo");
            yield return Esperar(0.25f);
            Assert.IsTrue(controlado.enabled);
            Assert.IsTrue(peleador.ControlActivo);
        }

        [UnityTest]
        public IEnumerator Aturdir_SinControl_NoHaceNada()
        {
            GameObject mecha = CrearMecha("Mecha", Vector3.zero, LadoPelea.Jugador);
            yield return Calentar();
            PeleadorMecha peleador = mecha.GetComponent<PeleadorMecha>();
            peleador.ActivarControl(false);

            peleador.Aturdir(1f);

            Assert.IsFalse(peleador.EstaAturdido);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MechaCyberpunk.Combate.Peleador;
using MechaCyberpunk.Combate.Vida;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Encendido;
using MechaCyberpunk.Mecha.Estado;
using MechaCyberpunk.Mecha.Llamado;
using MechaCyberpunk.Mecha.Montaje;
using MechaCyberpunk.Pelea.Flujo;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>T-103: el daño del mecha del jugador se queda entre peleas y se guarda como IGuardable "mecha".</summary>
    public class DanioPersistentePruebas : InputTestFixture
    {
        private const string RutaTony = "Assets/_Proyecto/Escenas/Pruebas_Tony.unity";
        private const string RutaCalle = "Assets/_Proyecto/Escenas/Tony/Pruebas_Calle.unity";
        private const string RutaRival = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const string RutaDatosJugador = "Assets/_Proyecto/Mecha/Datos/DatosMecha_Jugador.asset";
        private const string RutaConfig = "Assets/_Proyecto/Mecha/Datos/ConfigMechaDaniado_Base.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float EsperaMaxima = 30f;
        private const float DanioMortal = 100000f;
        private const float Margen = 0.01f;
        private const float DistanciaInicialAlTaller = 5f;
        private const float DistanciaParaSubir = 2f;
        private const float MaxSegundosCaminar = 10f;
        private const float MaxSegundosEncender = 10f;
        private const float MaxSegundosFrenar = 6f;
        private const float MargenEspera = 2f;
        private const float SegundosJugando = 0.5f;

        private Keyboard teclado;
        private DirectorPelea director;
        private readonly List<GameObject> creados = new List<GameObject>();
        private readonly List<ResultadoPelea> resultados = new List<ResultadoPelea>();

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            resultados.Clear();
            EventosPelea.PeleaTerminada += resultados.Add;
        }

        public override void TearDown()
        {
            EventosPelea.LimpiarSuscriptores();
            Registros.LimpiarTodos();
            base.TearDown();
        }

        [UnityTearDown]
        public IEnumerator Descargar()
        {
            foreach (GameObject objeto in creados)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            creados.Clear();
            foreach (GameObject raiz in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Object.Destroy(raiz);
            }
            yield return null;
            Registros.LimpiarTodos();
        }

        private static IEnumerator CargarEscena(string ruta)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ruta, new LoadSceneParameters(LoadSceneMode.Single));
            int estables = 0;
            int intentos = 0;
            while (estables < FramesEstables && intentos < MaxFramesCalentamiento)
            {
                yield return null;
                intentos++;
                estables = Time.deltaTime < DeltaTimeEstable ? estables + 1 : 0;
            }
        }

        private IEnumerator Tecla(Key tecla)
        {
            Press(teclado[tecla]);
            yield return null;
            Release(teclado[tecla]);
            yield return null;
        }

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator HastaEstado(EstadoFlujoPelea estado)
        {
            float limite = Time.time + EsperaMaxima;
            while (director.Estado != estado && Time.time < limite)
            {
                yield return null;
            }
            Assert.AreEqual(estado, director.Estado, "No llegó al estado esperado");
        }

        private static VidaMecha Vida(IPeleador peleador) => (VidaMecha)peleador.Vida;

        private static void Danar(VidaMecha vida, float cantidad)
        {
            vida.RecibirDanio(new InfoDanio(cantidad, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
        }

        // El daño persistente vive en el mecha de la historia (MechaEstacionado): la pelea de prueba de Pruebas_Calle lo usa.
        // P empieza la pelea y se espera a ¡PELEA!.
        private IEnumerator EmpezarPeleaEnPruebasCalle()
        {
            yield return CargarEscena(RutaCalle);
            director = Object.FindFirstObjectByType<DirectorPelea>();
            Assert.IsNotNull(director, "Falta FlujoPelea en Pruebas_Calle");
            yield return Tecla(Key.P);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
        }

        [UnityTest]
        public IEnumerator Revancha_ConJugadorHerido_EmpiezaConSuDanioYElRivalCompleto()
        {
            yield return EmpezarPeleaEnPruebasCalle();
            Danar(Vida(director.Rival), DanioMortal);
            Danar(Vida(director.Jugador), 30f);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);

            yield return Tecla(Key.R);

            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.AreEqual(70f, Vida(director.Jugador).Actual, Margen, "El jugador conserva su vida");
            Assert.AreEqual(Vida(director.Rival).Maxima, Vida(director.Rival).Actual, Margen, "El rival vuelve completo");
            Assert.IsFalse(Vida(director.Rival).EstaKO);
        }

        [UnityTest]
        public IEnumerator Directo_SinMechaPersistente_LaRevanchaRestauraLaVida()
        {
            yield return CargarEscena(RutaTony);
            director = Object.FindFirstObjectByType<DirectorPelea>();
            Assert.IsNotNull(director, "Falta FlujoPelea en Pruebas_Tony");
            Assert.IsNull(((Component)director.Jugador).GetComponent<MechaPersistente>(), "MechaJugador.prefab no lleva MechaPersistente");
            yield return Tecla(Key.P);
            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            Danar(Vida(director.Rival), DanioMortal);
            Danar(Vida(director.Jugador), 30f);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);

            yield return Tecla(Key.R);

            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            Assert.AreEqual(Vida(director.Jugador).Maxima, Vida(director.Jugador).Actual, Margen, "Sin persistencia, la revancha restaura la vida");
        }

        [UnityTest]
        public IEnumerator Revancha_TrasPerder_EmpiezaAl15PorCientoYLaPeleaSeJuega()
        {
            yield return EmpezarPeleaEnPruebasCalle();
            Danar(Vida(director.Jugador), DanioMortal);
            yield return HastaEstado(EstadoFlujoPelea.Resultado);
            Assert.IsTrue(Vida(director.Jugador).EstaKO);

            yield return Tecla(Key.R);

            Assert.AreEqual(EstadoFlujoPelea.Conteo, director.Estado);
            float piso = Vida(director.Jugador).Maxima * 0.15f;
            Assert.AreEqual(piso, Vida(director.Jugador).Actual, Margen, "Empieza al piso de emergencia");
            Assert.IsFalse(Vida(director.Jugador).EstaKO);

            yield return HastaEstado(EstadoFlujoPelea.Peleando);
            // (El control con la señal de pelea lo cubre FlujoPeleaPruebas: en Pruebas_Calle el mecha está estacionado, sin piloto.)
            int terminadas = resultados.Count;
            yield return Esperar(SegundosJugando);
            Assert.AreEqual(EstadoFlujoPelea.Peleando, director.Estado, "La pelea no termina sola");
            Assert.AreEqual(terminadas, resultados.Count);
        }

        [UnityTest]
        public IEnumerator PrepararPelea_RivalSinMechaPersistente_QuedaConVidaMaxima()
        {
            GameObject rival = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaRival));
            creados.Add(rival);
            yield return null;
            Assert.IsNull(rival.GetComponent<MechaPersistente>(), "Los rivales no llevan MechaPersistente");
            VidaMecha vida = rival.GetComponent<VidaMecha>();
            Danar(vida, 40f);
            Assert.Less(vida.Actual, vida.Maxima);

            rival.GetComponent<PeleadorMecha>().PrepararPelea(new Pose(Vector3.zero, Quaternion.identity));

            Assert.AreEqual(vida.Maxima, vida.Actual, Margen);
        }

        [UnityTest]
        public IEnumerator Guardable_EnPruebasCalle_GuardaCargaYRepara()
        {
            yield return CargarEscena(RutaCalle);
            List<IGuardable> mechas = new List<IGuardable>();
            foreach (IGuardable guardable in Registro<IGuardable>.Todos)
            {
                if (guardable.ClaveGuardado == "mecha")
                {
                    mechas.Add(guardable);
                }
            }
            Assert.AreEqual(1, mechas.Count, "Hay exactamente un guardable con clave mecha");
            MechaPersistente persistente = (MechaPersistente)mechas[0];
            VidaMecha vida = persistente.GetComponent<VidaMecha>();
            int reparados = 0;
            persistente.Reparado += () => reparados++;

            vida.FijarVida(40f);
            Assert.AreEqual(60f, persistente.Danio, Margen);
            string guardado = persistente.Guardar();
            persistente.Reparar();
            Assert.AreEqual(vida.Maxima, vida.Actual, Margen);
            Assert.AreEqual(1, reparados);
            Assert.AreEqual(0f, persistente.Danio, Margen);

            persistente.Cargar(guardado);
            Assert.AreEqual(40f, vida.Actual, Margen);

            persistente.Cargar("{\"vida\":0,\"vidaMaxima\":100}");
            Assert.AreEqual(15f, vida.Actual, Margen, "Vida 0 guardada carga al piso");

            vida.FijarVida(40f);
            LogAssert.Expect(LogType.Warning, new Regex("No se entendió"));
            persistente.Cargar("basura");
            Assert.AreEqual(40f, vida.Actual, Margen, "Texto que no se entiende no cambia nada");
        }

        [UnityTest]
        public IEnumerator Taller_MechaEstacionadoConDanio_ConservaLaVidaAlVolver()
        {
            yield return CargarEscena(RutaCalle);
            JugadorAPie jugador = Object.FindFirstObjectByType<JugadorAPie>();
            MontajeMecha montaje = Object.FindFirstObjectByType<MontajeMecha>();
            Assert.IsNotNull(jugador);
            Assert.IsNotNull(montaje);
            LlamadoMecha llamado = montaje.GetComponent<LlamadoMecha>();
            EncendidoMecha encendido = montaje.GetComponent<EncendidoMecha>();
            VidaMecha vida = montaje.GetComponent<VidaMecha>();
            Assert.IsNotNull(montaje.GetComponent<MechaPersistente>(), "El mecha estacionado hereda MechaPersistente");
            vida.FijarVida(50f);

            Transform mecha = montaje.transform;
            jugador.Colocar(new Pose(mecha.position + mecha.forward * DistanciaInicialAlTaller, Quaternion.LookRotation(-mecha.forward)));
            yield return null;

            // Camina hasta el mecha y sube con E.
            Press(teclado.wKey);
            float fin = Time.time + MaxSegundosCaminar;
            while (Time.time < fin && Vector3.Distance(jugador.Raiz.position, montaje.PuntoSubida) >= DistanciaParaSubir)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            yield return Tecla(Key.E);
            Assert.AreEqual(ModoJugador.Mecha, jugador.Modo, "La E frente al mecha lo sube");

            // Enciende: F, Espacio sostenido y Q.
            yield return Tecla(Key.F);
            Press(teclado.spaceKey);
            fin = Time.time + MaxSegundosEncender;
            while (Time.time < fin && encendido.Paso != PasoEncendido.Paso3)
            {
                yield return null;
            }
            Release(teclado.spaceKey);
            yield return Tecla(Key.Q);
            while (Time.time < fin && !encendido.EstaEncendido)
            {
                yield return null;
            }
            Assert.IsTrue(encendido.EstaEncendido);

            // Baja y lo manda al taller con M.
            fin = Time.time + MaxSegundosFrenar;
            while (Time.time < fin && !montaje.PuedeBajar)
            {
                yield return null;
            }
            yield return Tecla(Key.E);
            Assert.AreEqual(ModoJugador.APie, jugador.Modo, "La E baja del mecha");
            yield return Tecla(Key.M);
            Assert.AreEqual(EstadoLlamado.Regresando, llamado.Estado);
            fin = Time.time + llamado.Config.SegundosRegreso + MargenEspera;
            while (Time.time < fin && llamado.Estado != EstadoLlamado.EnTaller)
            {
                yield return null;
            }
            Assert.AreEqual(EstadoLlamado.EnTaller, llamado.Estado);

            Assert.AreEqual(50f, vida.Actual, Margen, "Vuelve al taller con su daño");
        }

        [UnityTest]
        public IEnumerator Registro_DosMechasPersistentes_SoloUnoQuedaRegistradoYAvisa()
        {
            LogAssert.Expect(LogType.Warning, new Regex("Ya hay un mecha registrado"));
            yield return null;
            DatosMecha datos = AssetDatabase.LoadAssetAtPath<DatosMecha>(RutaDatosJugador);
            MechaCyberpunk.Mecha.ConfigMechaDaniado config = AssetDatabase.LoadAssetAtPath<MechaCyberpunk.Mecha.ConfigMechaDaniado>(RutaConfig);
            Assert.IsNotNull(config, "Existe ConfigMechaDaniado_Base");

            MechaPersistente primero = CrearMechaMinimo("MechaA", datos, config);
            MechaPersistente segundo = CrearMechaMinimo("MechaB", datos, config);

            int cuenta = 0;
            foreach (IGuardable guardable in Registro<IGuardable>.Todos)
            {
                if (guardable.ClaveGuardado == "mecha")
                {
                    cuenta++;
                }
            }
            Assert.AreEqual(1, cuenta, "Solo uno queda registrado");
            Assert.IsTrue(Registro<IGuardable>.Todos.Contains(primero));
            Assert.IsFalse(Registro<IGuardable>.Todos.Contains(segundo));
            yield return null;
        }

        // Un mecha con solo vida, peleador y MechaPersistente; se arma inactivo para dar sus referencias antes del Awake.
        private MechaPersistente CrearMechaMinimo(string nombre, DatosMecha datos, MechaCyberpunk.Mecha.ConfigMechaDaniado config)
        {
            GameObject objeto = new GameObject(nombre);
            objeto.SetActive(false);
            creados.Add(objeto);
            VidaMecha vida = objeto.AddComponent<VidaMecha>();
            SerializedObject soVida = new SerializedObject(vida);
            soVida.FindProperty("datos").objectReferenceValue = datos;
            soVida.ApplyModifiedPropertiesWithoutUndo();
            objeto.AddComponent<PeleadorMecha>();
            MechaPersistente persistente = objeto.AddComponent<MechaPersistente>();
            SerializedObject soPersistente = new SerializedObject(persistente);
            soPersistente.FindProperty("config").objectReferenceValue = config;
            soPersistente.ApplyModifiedPropertiesWithoutUndo();
            objeto.SetActive(true);
            return persistente;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Vida;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace MechaCyberpunk.Tests
{
    /// <summary>
    /// T-1110: el jefe final de Nova (E14) en un piso de prueba con el jugador real y el recinto armado por código. El puñetazo conecta 25 a 2 m; el cañonazo pega
    /// 30 al jugador quieto y 0 al que se aparta 4 m de lado (A/D simuladas); el escudo de energía absorbe todo (TipoBloqueo.Perfecto), al romperse aturda 3 s y
    /// vuelve a los 10 s; la onda pega 20 a 5 m; al vencer al jefe se abre la barrera y si el jugador se aleja el jefe se reinicia.
    /// Con T1110_CAPTURAS=1 escribe docs/capturas/t-1110/.
    /// </summary>
    public class JefeFinalNovaT1110Pruebas : InputTestFixture
    {
        private const string RutaJugador = "Assets/_Proyecto/Personaje/Prefabs/JugadorAPie.prefab";
        private const string RutaJefe = "Assets/_Proyecto/IA/Prefabs/JefeFinalNova_E14.prefab";
        private const string RutaDatosJefe = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeFinalNovaE14.asset";
        private const string RutaGuardia = "Assets/_Proyecto/IA/Prefabs/GuardiaPesado_E12.prefab";
        private const string RutaBarrera = "Assets/_Proyecto/IA/Prefabs/Mat_BarreraEnergiaNova.mat";
        private const string CarpetaFotos = "docs/capturas/t-1110";

        private sealed class TerritorioFalso : ITerritorio
        {
            public readonly List<(Vector3 posicion, IdPandilla pandilla, bool jefe)> Vencidos = new List<(Vector3, IdPandilla, bool)>();
            public IReadOnlyList<string> Zonas => new string[0];
            public string ZonaEn(Vector3 posicion) => "refineria";
            public string Nombre(string zona) => zona;
            public IdPandilla Dueno(string zona) => IdPandilla.Nova;
            public int Respeto(string zona) => 0;
            public bool JefeDisponible(string zona) => false;
            public void RegistrarVencido(Vector3 posicion, IdPandilla pandilla, bool jefe) => Vencidos.Add((posicion, pandilla, jefe));
            public void RegistrarCaida(Vector3 posicion) { }
            public event Action<string> Cambio { add { } remove { } }
            public event Action<string, IdPandilla> Conquistada { add { } remove { } }
        }

        private Keyboard teclado;
        private GameObject piso;
        private GameObject jugador;
        private VidaAPie vidaJugador;
        private ConductaJefeFinalNova conducta;
        private PandilleroAPie jefe;
        private RecintoJefe recinto;
        private PuestoGuardiasPesados puesto;
        private readonly List<float> danios = new List<float>();
        private readonly List<GameObject> extras = new List<GameObject>();
        private float vidaPrevia;

        public override void Setup()
        {
            base.Setup();
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(400f, 1f, 400f);
            jugador = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugador), new Vector3(0f, 0.05f, 0f), Quaternion.identity);
            jugador.GetComponent<EscudoJugador>()?.Iniciar(0f, 0f, 0f);
            vidaJugador = jugador.GetComponent<VidaAPie>();
            danios.Clear();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            if (recinto != null)
            {
                Object.DestroyImmediate(recinto.gameObject);
            }
            else if (conducta != null)
            {
                Object.DestroyImmediate(conducta.transform.parent.gameObject);
            }
            if (puesto != null)
            {
                Object.DestroyImmediate(puesto.gameObject);
            }
            foreach (GameObject e in extras) Object.DestroyImmediate(e);
            Object.DestroyImmediate(jugador);
            Object.DestroyImmediate(piso);
            Registros.LimpiarTodos();
            EventosLugares.LimpiarSuscriptores();
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        // El jefe mira al jugador; con activar:true empieza a pelear de inmediato.
        private void CrearJefe(Vector3 posicion, bool activar = true)
        {
            conducta = ConstructorRecintoJefe.CrearJefe(posicion, jugador.transform.position - posicion, AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe),
                AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosJefe));
            jefe = conducta.GetComponent<PandilleroAPie>();
            Physics.SyncTransforms();
            if (activar)
            {
                conducta.Activar();
            }
            vidaPrevia = vidaJugador.Actual;
        }

        private void CrearRecinto(bool conGuardias = true)
        {
            GameObject prefabGuardia = AssetDatabase.LoadAssetAtPath<GameObject>(RutaGuardia);
            if (conGuardias)
            {
                GameObject raiz = new GameObject("PuestoGuardias_Prueba");
                ConductaGuardiaPesado[] guardias = new ConductaGuardiaPesado[2];
                for (int i = 0; i < 2; i++)
                {
                    GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(prefabGuardia, raiz.transform);
                    g.transform.position = new Vector3(i == 0 ? -6f : 6f, 0.05f, 8f);
                    guardias[i] = g.GetComponent<ConductaGuardiaPesado>();
                }
                puesto = raiz.AddComponent<PuestoGuardiasPesados>();
                SerializedObject so = new SerializedObject(puesto);
                so.FindProperty("guardias").arraySize = 2;
                so.FindProperty("guardias").GetArrayElementAtIndex(0).objectReferenceValue = guardias[0];
                so.FindProperty("guardias").GetArrayElementAtIndex(1).objectReferenceValue = guardias[1];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // La entrada está en z = 6, la zona va de z = 10 a 30 y el jefe espera en z = 26.
            recinto = ConstructorRecintoJefe.Crear(null, "Recinto_Prueba", new Vector3(0f, 0.05f, 6f), new Vector3(0f, 0.05f, 26f), 18f, 20f,
                AssetDatabase.LoadAssetAtPath<GameObject>(RutaJefe), AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatosJefe), puesto,
                AssetDatabase.LoadAssetAtPath<Material>(RutaBarrera));
            conducta = recinto.Jefe;
            jefe = conducta.GetComponent<PandilleroAPie>();
            Physics.SyncTransforms();
        }

        private void Teletransportar(Vector3 destino)
        {
            CharacterController cc = jugador.GetComponent<CharacterController>();
            cc.enabled = false;
            jugador.transform.position = destino;
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        private static IEnumerator Esperar(float s)
        {
            float fin = Time.time + s;
            while (Time.time < fin) yield return null;
        }

        // Espera a que el jugador reciba al menos n golpes (anota cada baja de vida) o pase el tiempo.
        private IEnumerator HastaDanios(int n, float maxSegundos)
        {
            float fin = Time.time + maxSegundos;
            while (danios.Count < n && Time.time < fin)
            {
                yield return null;
                Anotar();
            }
        }

        private void Anotar()
        {
            float ahora = vidaJugador.Actual;
            if (ahora < vidaPrevia - 0.001f) danios.Add(vidaPrevia - ahora);
            vidaPrevia = ahora;
        }

        private InfoDanio Golpe(float cantidad, TipoDanio tipo = TipoDanio.Golpe)
        {
            return new InfoDanio(cantidad, tipo, jefe.transform.position + Vector3.up * 1.2f, Vector3.forward, null);
        }

        // Baja la vida del jefe hasta la fracción dada (en la fase Asedio no hay escudo).
        private IEnumerator BajarVidaA(float fraccion)
        {
            jefe.RecibirDanio(Golpe(jefe.Maxima * (jefe.Actual / jefe.Maxima - fraccion)));
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Datos_VidaSeiscientosYVelocidadDosPuntoOcho()
        {
            yield return Esperar(0.3f);
            CrearJefe(new Vector3(0f, 0.05f, 30f), false);
            yield return null;
            Assert.AreEqual(600f, jefe.Maxima, 0.001f);
            Assert.AreEqual(2.8f, jefe.Datos.Velocidad, 0.001f);
            Assert.IsTrue(jefe.EsJefe);
            Assert.IsFalse(conducta.Regla.EscudoActivo, "sin escudo al principio");
            Assert.IsNotNull(conducta.Escudo, "tiene su escudo de energía");
        }

        [UnityTest]
        public IEnumerator Punetazo_ElJugadorQuietoA2m_PierdeVeinticinco()
        {
            yield return Esperar(0.4f);
            CrearJefe(new Vector3(0f, 0.05f, 2f));
            yield return HastaDanios(1, 3f);
            Assert.GreaterOrEqual(danios.Count, 1, "el puñetazo conectó");
            Assert.AreEqual(25f, danios[0], 0.01f);
        }

        [UnityTest]
        public IEnumerator Canonazo_ElJugadorQuieto_RecibeTreinta()
        {
            yield return Esperar(0.4f);
            CrearJefe(new Vector3(0f, 0.05f, 6f), false);
            conducta.Regla.ForzarIndice(2); // tercera acción del Asedio: el cañonazo
            conducta.Activar();
            float fin = Time.time + 2f;
            while (conducta.ArosPendientes == 0 && Time.time < fin) yield return null;
            Assert.AreEqual(1, conducta.ArosPendientes, "aparece un aro rojo en el piso");
            Assert.AreEqual(0f, vidaPrevia - vidaJugador.Actual, 0.001f, "antes de explotar no hay daño");
            yield return HastaDanios(1, 2.5f);
            Assert.AreEqual(1, danios.Count);
            Assert.AreEqual(30f, danios[0], 0.01f);
            yield return Esperar(0.3f);
            Anotar();
            Assert.AreEqual(1, danios.Count, "una sola explosión");
        }

        [UnityTest]
        public IEnumerator Canonazo_ElJugadorSeApartaCuatroMetrosDeLado_NoRecibeDanio()
        {
            yield return Esperar(0.4f);
            CrearJefe(new Vector3(0f, 0.05f, 6f), false);
            conducta.Regla.ForzarIndice(2);
            conducta.Activar();
            float fin = Time.time + 2f;
            while (conducta.ArosPendientes == 0 && Time.time < fin) yield return null;
            Assert.AreEqual(1, conducta.ArosPendientes);
            float x0 = jugador.transform.position.x;
            Press(teclado.dKey);
            while (conducta.ArosPendientes > 0 && Time.time < fin + 3f) yield return null;
            float desplazado = Mathf.Abs(jugador.transform.position.x - x0);
            Release(teclado.dKey);
            yield return Esperar(0.3f);
            Anotar();
            Assert.GreaterOrEqual(desplazado, 3f, "el jugador se apartó de lado (" + desplazado + " m)");
            Assert.AreEqual(0, danios.Count, "el cañonazo cayó en el piso vacío");
        }

        [UnityTest]
        public IEnumerator Escudo_A66PorCiento_AbsorbeTodoYDevuelvePerfecto()
        {
            yield return Esperar(0.3f);
            CrearJefe(new Vector3(0f, 0.05f, 25f));
            yield return BajarVidaA(0.65f);
            Assert.IsTrue(conducta.Regla.EscudoActivo, "al pasar de 66% se prende el escudo");
            Assert.AreEqual("¡Escudo activado!", jefe.GritoActual);
            Assert.IsNotNull(conducta.Escudo.Esfera, "tiene la esfera cian");
            float antes = jefe.Actual;
            ResultadoDanio r = jefe.RecibirDanio(Golpe(30f));
            Assert.AreEqual(TipoBloqueo.Perfecto, r.Bloqueo);
            Assert.AreEqual(antes, jefe.Actual, 0.001f, "un golpe con escudo deja la vida igual");
            Assert.AreEqual(110f, conducta.Regla.AguanteEscudo, 0.001f);
            ResultadoDanio disparo = jefe.RecibirDanio(Golpe(10f, TipoDanio.Disparo));
            Assert.AreEqual(TipoBloqueo.Perfecto, disparo.Bloqueo, "también las balas");
            Assert.AreEqual(antes, jefe.Actual, 0.001f);
        }

        [UnityTest]
        public IEnumerator Escudo_RomperloConDanio_LoAturdeTresSegundosYVuelveALosDiez()
        {
            yield return Esperar(0.3f);
            CrearJefe(new Vector3(0f, 0.05f, 25f));
            yield return BajarVidaA(0.65f);
            Assert.IsTrue(conducta.Regla.EscudoActivo);
            float antes = jefe.Actual;
            jefe.RecibirDanio(Golpe(100f));
            ResultadoDanio rompe = jefe.RecibirDanio(Golpe(50f));
            Assert.AreEqual(TipoBloqueo.Perfecto, rompe.Bloqueo, "el golpe que lo rompe también se absorbe");
            Assert.AreEqual(antes, jefe.Actual, 0.001f, "la vida sigue igual");
            float inicio = Time.time;
            yield return null;
            Assert.IsFalse(conducta.Regla.EscudoActivo);
            bool gritoVisto = jefe.GritoActual == "¡Sobrecarga!"; // el enfriamiento de los gritos (4 s) puede retrasarlo: se reintenta
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Aturdido, conducta.Regla.Actual);
            Assert.IsTrue(jefe.Castigable, "aturdido queda castigable");
            float vida = jefe.Actual;
            ResultadoDanio entra = jefe.RecibirDanio(Golpe(10f));
            Assert.AreEqual(TipoBloqueo.Ninguno, entra.Bloqueo);
            Assert.AreEqual(15f, vida - jefe.Actual, 0.01f, "roto y castigable: 10 x1.5");
            while (Time.time - inicio < 2.8f) { gritoVisto |= jefe.GritoActual == "¡Sobrecarga!"; yield return null; }
            Assert.AreEqual(ReglaJefeFinalNova.Accion.Aturdido, conducta.Regla.Actual, "a 2.8 s sigue aturdido");
            while (Time.time - inicio < 3.3f) { gritoVisto |= jefe.GritoActual == "¡Sobrecarga!"; yield return null; }
            Assert.AreNotEqual(ReglaJefeFinalNova.Accion.Aturdido, conducta.Regla.Actual, "a los 3 s se le pasa");
            Assert.IsFalse(jefe.Castigable);
            while (Time.time - inicio < 9.7f) { gritoVisto |= jefe.GritoActual == "¡Sobrecarga!"; yield return null; }
            Assert.IsTrue(gritoVisto, "el jefe grita ¡Sobrecarga! al romperse el escudo");
            Assert.IsFalse(conducta.Regla.EscudoActivo, "a 9.7 s todavía no vuelve");
            while (Time.time - inicio < 10.5f) yield return null;
            Assert.IsTrue(conducta.Regla.EscudoActivo, "a los 10 s vuelve (la vida sigue sobre 33%)");
        }

        [UnityTest]
        public IEnumerator Onda_ElJugadorA5m_Recibe20UnaSolaVez()
        {
            yield return Esperar(0.4f);
            CrearJefe(new Vector3(0f, 0.05f, 5f));
            yield return BajarVidaA(0.65f); // fase Escudo
            conducta.Regla.ForzarIndice(1); // la onda de choque
            float fin = Time.time + 1.5f;
            while (!conducta.OndaVisible && Time.time < fin + 1f) yield return null;
            Assert.IsTrue(conducta.OndaVisible, "el aro de la onda se ve");
            Assert.AreEqual(0f, vidaPrevia - vidaJugador.Actual, 0.001f, "antes de que el aro llegue no hay daño");
            yield return HastaDanios(1, 1.5f);
            Assert.AreEqual(1, danios.Count);
            Assert.AreEqual(20f, danios[0], 0.01f);
            yield return Esperar(0.6f);
            Anotar();
            Assert.AreEqual(1, danios.Count, "el aro pega una sola vez");
        }

        [UnityTest]
        public IEnumerator Recinto_AlEntrarElJugador_CierraLaBarreraGritaYProvocaALosGuardias()
        {
            yield return Esperar(0.4f);
            CrearRecinto();
            yield return Esperar(0.3f);
            Assert.IsFalse(recinto.Barrera.activeSelf, "la barrera empieza abierta");
            Assert.IsFalse(recinto.Cerrado);
            Teletransportar(new Vector3(0f, 0.05f, 14f));
            yield return Esperar(0.3f);
            Assert.IsTrue(recinto.Cerrado);
            Assert.IsTrue(recinto.Barrera.activeSelf, "se cierra la entrada");
            Assert.IsTrue(recinto.Barrera.GetComponent<Collider>().enabled && recinto.Barrera.activeInHierarchy, "la barrera tiene collider");
            Assert.IsTrue(conducta.Activo);
            Assert.AreEqual(RecintoJefe.GritoCierre, jefe.GritoActual);
            foreach (ConductaGuardiaPesado g in puesto.Guardias)
            {
                Assert.IsTrue(g.Provocado, g.name + " provocado");
            }
        }

        [UnityTest]
        public IEnumerator Recinto_AlVencerAlJefe_SeAbreLaBarreraYSeVanLosGuardias()
        {
            TerritorioFalso territorio = new TerritorioFalso();
            Registro<ITerritorio>.Agregar(territorio);
            yield return Esperar(0.4f);
            CrearRecinto();
            Teletransportar(new Vector3(0f, 0.05f, 14f));
            yield return Esperar(0.3f);
            Assert.IsTrue(recinto.Barrera.activeSelf);
            jefe.RecibirDanio(Golpe(5000f));
            yield return Esperar(0.4f);
            Assert.IsTrue(jefe.EstaKO);
            Assert.IsFalse(recinto.Barrera.activeSelf, "al morir el jefe se abre la barrera");
            Assert.IsFalse(recinto.Cerrado);
            Assert.AreEqual(1, territorio.Vencidos.Count, "un solo registro");
            Assert.AreEqual(IdPandilla.Nova, territorio.Vencidos[0].pandilla);
            Assert.IsTrue(territorio.Vencidos[0].jefe, "como jefe vencido");
            foreach (ConductaGuardiaPesado g in puesto.Guardias)
            {
                Assert.IsFalse(g.gameObject.activeSelf, g.name + " se fue");
            }
            yield return Esperar(0.5f);
            Assert.IsFalse(recinto.Barrera.activeSelf, "no se vuelve a cerrar");
        }

        [UnityTest]
        public IEnumerator Recinto_SiElJugadorSeAleja45m_ElJefeSeReiniciaConLaVidaLlena()
        {
            yield return Esperar(0.4f);
            CrearRecinto();
            Vector3 hogar = jefe.Hogar;
            Teletransportar(new Vector3(0f, 0.05f, 14f));
            yield return Esperar(0.3f);
            Assert.IsTrue(recinto.Cerrado);
            yield return BajarVidaA(0.5f);
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Escudo, conducta.Regla.Fase);
            puesto.Guardias[0].GetComponent<PandilleroAPie>().RecibirDanio(Golpe(30f, TipoDanio.Golpe));
            Teletransportar(new Vector3(0f, 0.05f, 20f - 45f)); // a 45 m del centro del recinto
            yield return Esperar(0.5f);
            Assert.IsFalse(recinto.Cerrado, "se abre la barrera");
            Assert.IsFalse(recinto.Barrera.activeSelf);
            Assert.IsFalse(conducta.Activo);
            Assert.AreEqual(jefe.Maxima, jefe.Actual, 0.001f, "vida llena");
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Asedio, conducta.Regla.Fase, "vuelve a la fase 1");
            Assert.IsFalse(conducta.Regla.EscudoActivo);
            Assert.Less(Vector3.Distance(jefe.transform.position, hogar), 1f, "en su punto");
            foreach (ConductaGuardiaPesado g in puesto.Guardias)
            {
                Assert.IsFalse(g.Provocado, g.name + " vuelve a guardia");
                Assert.AreEqual(g.GetComponent<PandilleroAPie>().Maxima, g.GetComponent<PandilleroAPie>().Actual, 0.001f, g.name + " con la vida llena");
            }
        }

        [UnityTest]
        public IEnumerator Recinto_SiElJugadorMuere_ElJefeSeReinicia()
        {
            yield return Esperar(0.4f);
            CrearRecinto(false);
            Teletransportar(new Vector3(0f, 0.05f, 14f));
            yield return Esperar(0.3f);
            Assert.IsTrue(recinto.Cerrado);
            jefe.RecibirDanio(Golpe(100f));
            vidaJugador.RecibirDanio(new InfoDanio(10000f, TipoDanio.Golpe, jugador.transform.position + Vector3.up, Vector3.forward, null));
            yield return Esperar(0.5f);
            Assert.IsFalse(recinto.Cerrado, "si el jugador muere se abre la barrera");
            Assert.AreEqual(jefe.Maxima, jefe.Actual, 0.001f);
        }

        // ---------------------------------------------------------------- capturas

        private static bool Capturando => Environment.GetEnvironmentVariable("T1110_CAPTURAS") == "1";

        [UnityTest]
        public IEnumerator Capturas_Fase1ConAroDeCanonazo()
        {
            if (!Capturando) Assert.Ignore("Solo escribe docs/capturas/t-1110/ con T1110_CAPTURAS=1");
            Directory.CreateDirectory(CarpetaFotos);
            yield return Esperar(0.4f);
            CrearJefe(new Vector3(0f, 0.05f, 7f), false);
            conducta.Regla.ForzarIndice(2);
            conducta.Activar();
            float fin = Time.time + 3f;
            while ((conducta.Regla.Actual != ReglaJefeFinalNova.Accion.AvisoCanon || conducta.Regla.ProgresoAviso < 0.6f) && Time.time < fin) yield return null;
            yield return Foto(CarpetaFotos + "/fase1-canonazo.png", new Vector3(5f, 3.2f, 0f));
        }

        [UnityTest]
        public IEnumerator Capturas_Fase2ConEscudoYOnda()
        {
            if (!Capturando) Assert.Ignore("Solo escribe docs/capturas/t-1110/ con T1110_CAPTURAS=1");
            Directory.CreateDirectory(CarpetaFotos);
            yield return Esperar(0.4f);
            CrearJefe(new Vector3(0f, 0.05f, 8f));
            yield return BajarVidaA(0.6f);
            jefe.RecibirDanio(Golpe(40f)); // el pulso del escudo
            yield return Foto(CarpetaFotos + "/fase2-escudo.png", new Vector3(10f, 3f, 3f));
            conducta.Regla.ForzarIndice(1);
            float fin = Time.time + 4f;
            while ((conducta.Regla.Actual != ReglaJefeFinalNova.Accion.Onda || conducta.Regla.RadioOnda < 4.5f) && Time.time < fin) yield return null;
            yield return Foto(CarpetaFotos + "/fase2-onda.png", new Vector3(8f, 4.5f, 2f));
        }

        [UnityTest]
        public IEnumerator Capturas_Fase3ConRafaga()
        {
            if (!Capturando) Assert.Ignore("Solo escribe docs/capturas/t-1110/ con T1110_CAPTURAS=1");
            Directory.CreateDirectory(CarpetaFotos);
            yield return Esperar(0.4f);
            CrearJefe(new Vector3(0f, 0.05f, 9f), false);
            yield return BajarVidaA(0.25f);
            Assert.AreEqual(ReglaJefeFinalNova.FaseJefe.Furia, conducta.Regla.Fase);
            conducta.Regla.ForzarIndice(2);
            conducta.Activar();
            float fin = Time.time + 3f;
            while (conducta.ArosPendientes < 3 && Time.time < fin) yield return null;
            yield return Esperar(0.5f);
            yield return Foto(CarpetaFotos + "/fase3-rafaga.png", new Vector3(7f, 4.5f, 2f));
        }

        private IEnumerator Foto(string ruta, Vector3 desplazamiento)
        {
            GameObject go = new GameObject("CamaraFoto");
            extras.Add(go);
            foreach (Camera otra in Camera.allCameras) otra.enabled = false; // la de la captura es Camera.main: el grito mira hacia ella
            Camera camara = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            camara.farClipPlane = 200f;
            Vector3 centro = (jefe.transform.position + jugador.transform.position) * 0.5f;
            go.transform.position = centro + desplazamiento;
            go.transform.LookAt(centro + Vector3.up * 1.0f);
            yield return null;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            camara.targetTexture = rt;
            camara.Render();
            RenderTexture.active = rt;
            Texture2D foto = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            foto.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            foto.Apply();
            camara.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(ruta, foto.EncodeToPNG());
            Object.Destroy(foto);
            Object.Destroy(rt);
        }
    }
}

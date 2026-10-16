using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MechaCyberpunk.Combate.Bloqueo;
using MechaCyberpunk.Combate.Golpe;
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

namespace MechaCyberpunk.Tests.IA
{
    /// <summary>
    /// Pruebas jugables de T-021 con el prefab real del rival y el MechaJugador real, sobre un piso creado en la prueba.
    /// El jugador mira a +Z desde el origen; el rival nace a 15 m mirándolo.
    /// </summary>
    public class RivalBasicoPruebas : InputTestFixture
    {
        private const string RutaPrefabJugador = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string RutaPrefabRival = "Assets/_Proyecto/IA/Prefabs/RivalBasico.prefab";
        private const string RutaConfig = "Assets/_Proyecto/IA/Datos/ConfigPresentacionRival_Base.asset";
        private const int FramesEstables = 10;
        private const int MaxFramesCalentamiento = 1000;
        private const float DeltaTimeEstable = 0.05f;
        private const float DistanciaInicial = 15f;
        private const float DistanciaDeGolpe = 2.3f;
        private const float AlturaPiso = 0.05f;
        private const float EsperaMaximaDeLlegada = 12f;
        private const float MargenColor = 0.05f;
        private const float MargenGrados = 0.05f;
        private const float GradosTirado = 90f;

        private Keyboard teclado;
        private Mouse raton;
        private GameObject piso;
        private GameObject jugador;
        private GameObject rivalObjeto;
        private VidaMecha vidaJugador;
        private VidaMecha vidaRival;
        private RivalBasico rival;
        private PeleadorMecha peleadorRival;
        private PresentacionRival presentacion;
        private DatosMecha datosJugador;
        private DatosMecha datosRival;
        private ConfigPresentacionRival config;
        private readonly List<string> avisos = new List<string>();

        public override void Setup()
        {
            base.Setup();
            EventosPelea.LimpiarSuscriptores();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
            avisos.Clear();
            Application.logMessageReceived += GuardarLinea;

            piso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piso.transform.position = new Vector3(0f, -0.5f, 0f);
            piso.transform.localScale = new Vector3(200f, 1f, 200f);
            config = AssetDatabase.LoadAssetAtPath<ConfigPresentacionRival>(RutaConfig);
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= GuardarLinea;
            Object.Destroy(rivalObjeto);
            Object.Destroy(jugador);
            Object.Destroy(piso);
            EventosPelea.LimpiarSuscriptores();
            base.TearDown();
        }

        private void GuardarLinea(string mensaje, string pila, LogType tipo)
        {
            if (tipo == LogType.Warning)
            {
                avisos.Add(mensaje);
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

        private static IEnumerator Esperar(float segundos)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator CrearJugador()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabJugador);
            jugador = Object.Instantiate(prefab, new Vector3(0f, AlturaPiso, 0f), Quaternion.identity);
            vidaJugador = jugador.GetComponent<VidaMecha>();
            datosJugador = vidaJugador.Datos;
            yield return null;
        }

        // El rival nace a la distancia indicada delante del jugador (a +Z), mirándolo. Tras un frame ya corrió su Start.
        private IEnumerator CrearRival(float distancia = DistanciaInicial)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabRival);
            rivalObjeto = Object.Instantiate(prefab, new Vector3(0f, AlturaPiso, distancia), Quaternion.Euler(0f, 180f, 0f));
            rival = rivalObjeto.GetComponent<RivalBasico>();
            vidaRival = rivalObjeto.GetComponent<VidaMecha>();
            peleadorRival = rivalObjeto.GetComponent<PeleadorMecha>();
            presentacion = rivalObjeto.GetComponent<PresentacionRival>();
            datosRival = rival.Datos.Mecha;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Control_ApagadoAntesDelStart_NoSeVuelveAQuitarAlDevolverlo()
        {
            yield return CrearJugador();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabRival);
            rivalObjeto = Object.Instantiate(prefab, new Vector3(0f, AlturaPiso, DistanciaInicial), Quaternion.Euler(0f, 180f, 0f));
            rival = rivalObjeto.GetComponent<RivalBasico>();
            peleadorRival = rivalObjeto.GetComponent<PeleadorMecha>();

            // Como el director cuando su Start corre antes que el del rival: lo prepara antes de que el rival tenga Start.
            peleadorRival.PrepararPelea(new Pose(rivalObjeto.transform.position, rivalObjeto.transform.rotation));
            yield return null;
            yield return null;
            Assert.IsFalse(peleadorRival.ControlActivo);
            Assert.IsFalse(rival.enabled, "Sigue apagado, sin haber corrido su Start");

            peleadorRival.ActivarControl(true);
            yield return null;
            yield return null;
            Assert.IsTrue(peleadorRival.ControlActivo, "Su Start no le quitó el control otra vez");
            Assert.IsTrue(rival.enabled);
            Assert.IsNotNull(rival.Objetivo, "Su Start sí corrió y buscó al jugador");
        }

        private IEnumerator Escenario(float distancia = DistanciaInicial)
        {
            yield return CrearJugador();
            yield return CrearRival(distancia);
            yield return Calentar();
        }

        private static void Teletransportar(GameObject objeto, Vector3 lugar)
        {
            CharacterController cc = objeto.GetComponent<CharacterController>();
            cc.enabled = false;
            objeto.transform.position = lugar;
            cc.enabled = true;
            Physics.SyncTransforms();
        }

        private static float DistanciaPlana(Transform a, Transform b)
        {
            Vector3 d = a.position - b.position;
            d.y = 0f;
            return d.magnitude;
        }

        private Color ColorDelBrazo()
        {
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();
            rival.PiezasBrazo[0].GetPropertyBlock(bloque);
            return bloque.GetColor("_BaseColor");
        }

        private static bool ColoresIguales(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < MargenColor && Mathf.Abs(a.g - b.g) < MargenColor && Mathf.Abs(a.b - b.b) < MargenColor;

        [UnityTest]
        public IEnumerator Arranque_SinControl_ElRivalEncuentraAlJugadorYNoSeMueveNiAtaca()
        {
            yield return Escenario();
            Vector3 inicio = rivalObjeto.transform.position;
            float vidaAntes = vidaJugador.Actual;

            Assert.AreSame(jugador, rival.Objetivo, "Encontró al jugador solo");
            Assert.IsFalse(peleadorRival.ControlActivo);
            Assert.IsFalse(rival.enabled);
            Assert.AreEqual(LadoPelea.Rival, peleadorRival.Lado);

            yield return Esperar(3f);

            Assert.AreEqual(0f, Vector3.Distance(inicio, rivalObjeto.transform.position), 0.01f, "No se movió");
            Assert.AreEqual(vidaAntes, vidaJugador.Actual, "No atacó");
        }

        [UnityTest]
        public IEnumerator ActivarControlFalse_DespuesDeTenerlo_ElRivalSeQuedaQuietoYNoAtaca()
        {
            yield return Escenario(DistanciaDeGolpe);
            peleadorRival.ActivarControl(true);
            yield return Esperar(0.3f);
            peleadorRival.ActivarControl(false);
            Vector3 inicio = rivalObjeto.transform.position;
            float vidaAntes = vidaJugador.Actual;

            yield return Esperar(4f);

            Assert.AreEqual(0f, Vector3.Distance(inicio, rivalObjeto.transform.position), 0.01f);
            Assert.AreEqual(vidaAntes, vidaJugador.Actual, "Sin control no golpea");
            Assert.AreEqual(FaseGolpe.Listo, rival.Fase);
        }

        [UnityTest]
        public IEnumerator Control_ElRivalLlegaAlJugadorYSeDetieneEntreLaDistanciaMinimaYLaDeAtaque()
        {
            yield return Escenario();
            DatosRival datos = rival.Datos;
            float minimaVista = float.MaxValue;
            peleadorRival.ActivarControl(true);

            float fin = Time.time + EsperaMaximaDeLlegada;
            while (Time.time < fin)
            {
                yield return null;
                minimaVista = Mathf.Min(minimaVista, DistanciaPlana(rivalObjeto.transform, jugador.transform));
            }

            float distanciaFinal = DistanciaPlana(rivalObjeto.transform, jugador.transform);
            Assert.GreaterOrEqual(minimaVista, datos.DistanciaMinima - 0.01f, "Nunca se acercó más que la mínima");
            Assert.GreaterOrEqual(distanciaFinal, datos.DistanciaMinima - 0.01f);
            Assert.LessOrEqual(distanciaFinal, datos.DistanciaAtaque + 0.01f, "Llegó hasta la distancia de ataque");
            Assert.Greater(distanciaFinal, 2f * 1f, "No atravesó al jugador: más lejos que los dos radios juntos");
            Assert.AreEqual(0f, rival.Velocidad, 0.05f, "Está detenido");
        }

        [UnityTest]
        public IEnumerator Golpe_AvisoNaranjaAlMenos055sYSoloDespuesBajaLaVidaDelJugadorExactamente10()
        {
            yield return Escenario(DistanciaDeGolpe);
            float vidaAntes = vidaJugador.Actual;
            float tiempoConAviso = 0f;
            float vidaDuranteElAviso = vidaAntes;
            bool impacto = false;
            rival.Impacto += () => impacto = true;
            peleadorRival.ActivarControl(true);

            float fin = Time.time + 8f;
            while (!impacto && Time.time < fin)
            {
                yield return null;
                if (ColoresIguales(ColorDelBrazo(), config.ColorAviso) && rival.EnAviso)
                {
                    tiempoConAviso += Time.deltaTime;
                    vidaDuranteElAviso = vidaJugador.Actual;
                }
            }
            yield return null;

            Assert.IsTrue(impacto, "El rival golpeó");
            Assert.GreaterOrEqual(tiempoConAviso, 0.55f, "El aviso se ve al menos 0.55 s");
            Assert.AreEqual(vidaAntes, vidaDuranteElAviso, "Durante el aviso no baja la vida");
            Assert.AreEqual(vidaAntes - datosRival.DanioGolpe, vidaJugador.Actual, 1e-4f);
            Assert.AreEqual(10f, datosRival.DanioGolpe);
            Assert.IsFalse(ColoresIguales(ColorDelBrazo(), config.ColorAviso), "Después del golpe el brazo vuelve a su color");
        }

        [UnityTest]
        public IEnumerator Aviso_BrazoSeEchaAtrasMientrasPreparaYVuelveDespues()
        {
            yield return Escenario(DistanciaDeGolpe);
            Quaternion reposo = rival.Brazo.localRotation;
            float maximoGiro = 0f;
            bool impacto = false;
            rival.Impacto += () => impacto = true;
            peleadorRival.ActivarControl(true);

            float fin = Time.time + 8f;
            while (!impacto && Time.time < fin)
            {
                yield return null;
                maximoGiro = Mathf.Max(maximoGiro, Quaternion.Angle(reposo, rival.Brazo.localRotation));
            }
            yield return null;

            Assert.Greater(maximoGiro, config.AnguloBrazoAtras * 0.8f, "El brazo se echó atrás");
            Assert.AreEqual(0f, Quaternion.Angle(reposo, rival.Brazo.localRotation), MargenGrados, "Y volvió al frente");
        }

        [UnityTest]
        public IEnumerator Bloqueo_GuardiaDesdeAntesDelAviso_ElGolpeEntraReducidoYEsBloqueoNormal()
        {
            yield return Escenario(DistanciaDeGolpe);
            ResultadoDanio resultado = ResultadoDanio.Ignorado;
            bool recibido = false;
            IPeleador identidadJugador = jugador.GetComponent<IPeleador>();
            EventosPelea.ImpactoRecibido += (objetivo, info, r) =>
            {
                if (objetivo == identidadJugador)
                {
                    resultado = r;
                    recibido = true;
                }
            };
            Press(raton.rightButton);
            yield return Esperar(datosJugador.TiempoSubirGuardia + 0.3f);
            Assert.IsTrue(jugador.GetComponent<GuardiaMecha>().EstaArriba, "La guardia ya estaba arriba");
            float vidaAntes = vidaJugador.Actual;
            peleadorRival.ActivarControl(true);

            float fin = Time.time + 8f;
            while (!recibido && Time.time < fin)
            {
                yield return null;
            }
            Release(raton.rightButton);

            Assert.IsTrue(recibido, "El golpe llegó");
            Assert.AreEqual(TipoBloqueo.Normal, resultado.Bloqueo);
            float esperado = datosRival.DanioGolpe * (1f - datosJugador.ReduccionBloqueo);
            Assert.AreEqual(esperado, resultado.DanioAplicado, 1e-4f);
            Assert.AreEqual(vidaAntes - esperado, vidaJugador.Actual, 1e-4f);
        }

        [UnityTest]
        public IEnumerator GolpeDelJugador_ClicIzquierdoACortaDistancia_BajaLaVidaDelRivalYLoHaceParpadear()
        {
            yield return Escenario(DistanciaDeGolpe);
            float vidaAntes = vidaRival.Actual;
            bool parpadeo = false;
            bool colorDeParpadeo = false;
            MaterialPropertyBlock bloque = new MaterialPropertyBlock();

            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            float fin = Time.time + datosJugador.PreparacionGolpe + 0.4f;
            while (Time.time < fin)
            {
                yield return null;
                if (presentacion.Parpadeando)
                {
                    parpadeo = true;
                    presentacion.Cuerpo.GetPropertyBlock(bloque);
                    colorDeParpadeo |= ColoresIguales(bloque.GetColor("_BaseColor"), config.ColorParpadeo);
                }
            }

            Assert.AreEqual(vidaAntes - datosJugador.DanioGolpe, vidaRival.Actual, 1e-4f);
            Assert.IsTrue(parpadeo, "Parpadeó");
            Assert.IsTrue(colorDeParpadeo, "Con el color de parpadeo");
            Assert.IsFalse(presentacion.Parpadeando, "Y el parpadeo termina");
            Assert.IsFalse(vidaRival.EstaKO);
        }

        [UnityTest]
        public IEnumerator Giro_ElJugadorSeMueve90GradosAlrededorDelRival_ElRivalGiraSinPasarDeSuVelocidadMaxima()
        {
            yield return Escenario();
            peleadorRival.ActivarControl(true);
            // Se detiene un momento para medir solo el giro: el jugador pasa a 90° del rival, a la misma distancia.
            Vector3 centro = rivalObjeto.transform.position;
            Teletransportar(jugador, new Vector3(centro.x + DistanciaInicial, AlturaPiso, centro.z));
            float vMax = datosRival.VelocidadGiroMaxima;

            float yawAnterior = rivalObjeto.transform.eulerAngles.y;
            float yawInicial = yawAnterior;
            float mayorRitmo = 0f;
            float fin = Time.time + 2.5f;
            while (Time.time < fin)
            {
                yield return null;
                float yaw = rivalObjeto.transform.eulerAngles.y;
                float cambio = Mathf.Abs(Mathf.DeltaAngle(yawAnterior, yaw));
                float limite = vMax * Time.deltaTime;
                Assert.LessOrEqual(cambio, limite + MargenGrados, "El giro de este frame no pasa del máximo");
                mayorRitmo = Mathf.Max(mayorRitmo, cambio / Time.deltaTime);
                yawAnterior = yaw;
            }

            Vector3 haciaJugador = jugador.transform.position - rivalObjeto.transform.position;
            float yawIdeal = Mathf.Atan2(haciaJugador.x, haciaJugador.z) * Mathf.Rad2Deg;
            Assert.Greater(Mathf.Abs(Mathf.DeltaAngle(yawInicial, rivalObjeto.transform.eulerAngles.y)), 60f, "Giró hacia el jugador");
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(yawIdeal, rivalObjeto.transform.eulerAngles.y)), 5f, "Y quedó mirándolo");
            Assert.Greater(mayorRitmo, vMax * 0.8f, "Giró cerca de su máximo, no más lento por otra causa");
        }

        [UnityTest]
        public IEnumerator Ritmo_CuatroGolpesSeguidos_LasEsperasVarianYQuedanEntreLaMinimaYLaMaxima()
        {
            yield return Escenario(DistanciaDeGolpe);
            DatosRival datos = rival.Datos;
            List<float> impactos = new List<float>();
            rival.Impacto += () => impactos.Add(Time.time);
            peleadorRival.ActivarControl(true);

            float fin = Time.time + 25f;
            float mayorFrame = 0f;
            while (impactos.Count < 4 && Time.time < fin)
            {
                yield return null;
                mayorFrame = Mathf.Max(mayorFrame, Time.deltaTime);
            }
            peleadorRival.ActivarControl(false);

            Assert.AreEqual(4, impactos.Count, "Cuatro golpes");
            float ciclo = datosRival.PreparacionGolpe + datosRival.RecargaGolpe;
            // El impacto se lee al final de un frame: cada marca de tiempo tiene hasta un frame de error, así que el margen crece con el frame más largo.
            float margenFrame = 0.1f + 2f * mayorFrame;
            float menor = float.MaxValue;
            float mayor = float.MinValue;
            for (int i = 1; i < impactos.Count; i++)
            {
                float espera = impactos[i] - impactos[i - 1] - ciclo;
                Assert.GreaterOrEqual(espera, datos.EsperaEntreAtaquesMin - margenFrame, "Espera " + i + " no menor que la mínima");
                Assert.LessOrEqual(espera, datos.EsperaEntreAtaquesMax + margenFrame, "Espera " + i + " no mayor que la máxima");
                menor = Mathf.Min(menor, espera);
                mayor = Mathf.Max(mayor, espera);
            }
            Assert.Greater(mayor - menor, 0.02f, "Las esperas no son todas iguales");
            Assert.AreEqual(vidaJugador.Maxima - 4f * datosRival.DanioGolpe, vidaJugador.Actual, 1e-3f, "Cada golpe quitó 10");
        }

        [UnityTest]
        public IEnumerator Ko_ElRivalNoSeMueveNiAtacaPor3sSeInclinaYNoRecibeMasDanio_PrepararPeleaLoDejaDePie()
        {
            yield return Escenario(DistanciaDeGolpe);
            peleadorRival.ActivarControl(true);
            yield return null;
            Vector3 posicion = rivalObjeto.transform.position;
            float vidaJugadorAntes = vidaJugador.Actual;

            ResultadoDanio mortal = vidaRival.RecibirDanio(
                new InfoDanio(vidaRival.Maxima * 2f, TipoDanio.Golpe, posicion, Vector3.forward, null));
            float inicioKO = Time.time;
            Assert.IsTrue(mortal.CausoKO);
            Assert.IsTrue(vidaRival.EstaKO);
            Assert.IsFalse(peleadorRival.ControlActivo);
            Assert.IsFalse(rival.enabled);

            float tiempoHasta60 = -1f;
            while (Time.time < inicioKO + 3f)
            {
                yield return null;
                if (tiempoHasta60 < 0f && Vector3.Angle(presentacion.Visual.up, Vector3.up) > 60f)
                {
                    tiempoHasta60 = Time.time - inicioKO;
                }
            }

            Assert.AreEqual(0f, Vector3.Distance(posicion, rivalObjeto.transform.position), 0.01f, "Tirado, no se mueve");
            Assert.AreEqual(vidaJugadorAntes, vidaJugador.Actual, "Tirado, no ataca");
            Assert.Greater(tiempoHasta60, 0f, "Se inclinó más de 60°");
            Assert.Less(tiempoHasta60, 1.5f, "En menos de 1.5 s");
            Assert.AreEqual(GradosTirado, Vector3.Angle(presentacion.Visual.up, Vector3.up), 1f, "Quedó tirado");

            peleadorRival.ActivarControl(true);
            Assert.IsFalse(peleadorRival.ControlActivo, "En KO no recupera el control");
            ResultadoDanio extra = vidaRival.RecibirDanio(new InfoDanio(10f, TipoDanio.Golpe, posicion, Vector3.forward, null));
            Assert.AreEqual(0f, extra.DanioAplicado);
            Assert.IsFalse(extra.CausoKO);
            Assert.AreEqual(TipoBloqueo.Ninguno, extra.Bloqueo);

            Pose pose = new Pose(new Vector3(4f, AlturaPiso, 20f), Quaternion.Euler(0f, 90f, 0f));
            peleadorRival.PrepararPelea(pose);
            yield return null;

            Assert.IsFalse(vidaRival.EstaKO);
            Assert.AreEqual(vidaRival.Maxima, vidaRival.Actual);
            Assert.AreEqual(0f, Vector3.Distance(pose.position, rivalObjeto.transform.position), 0.01f);
            Assert.AreEqual(0f, Quaternion.Angle(pose.rotation, rivalObjeto.transform.rotation), 0.5f);
            Assert.AreEqual(0f, Vector3.Angle(presentacion.Visual.up, Vector3.up), 0.01f, "De pie al instante");
            Assert.IsFalse(peleadorRival.ControlActivo, "Sin control");
            Assert.IsFalse(rival.enabled);
        }

        [UnityTest]
        public IEnumerator SinObjetivo_ElRivalAvisaUnaVezYSeQuedaQuieto()
        {
            LogAssert.Expect(LogType.Warning, new Regex("RivalBasico.*no tiene objetivo"));
            yield return CrearRival();
            yield return Calentar();
            Assert.IsNull(rival.Objetivo);
            peleadorRival.ActivarControl(true);
            Vector3 inicio = rivalObjeto.transform.position;

            yield return Esperar(2f);

            Assert.AreEqual(0f, Vector3.Distance(inicio, rivalObjeto.transform.position), 0.01f);
            int cuantos = 0;
            foreach (string aviso in avisos)
            {
                if (aviso.Contains("no tiene objetivo"))
                {
                    cuantos++;
                }
            }
            Assert.AreEqual(1, cuantos, "El aviso sale una sola vez");
        }

        [UnityTest]
        public IEnumerator AsignarObjetivo_DespuesDeNacerSinObjetivo_ElRivalVaHaciaElNuevoObjetivo()
        {
            LogAssert.Expect(LogType.Warning, new Regex("RivalBasico.*no tiene objetivo"));
            yield return CrearRival();
            yield return CrearJugador();
            // El jugador nació después del Start del rival: no lo encontró. Se lo damos a mano.
            yield return Calentar();
            Assert.IsNull(rival.Objetivo);
            rival.AsignarObjetivo(jugador);
            peleadorRival.ActivarControl(true);
            float antes = DistanciaPlana(rivalObjeto.transform, jugador.transform);

            yield return Esperar(2f);

            Assert.AreSame(jugador, rival.Objetivo);
            Assert.Less(DistanciaPlana(rivalObjeto.transform, jugador.transform), antes - 2f, "Se acercó");
        }

        [UnityTest]
        public IEnumerator ObjetivoEnKO_ElRivalDejaDeAtacar()
        {
            yield return Escenario(DistanciaDeGolpe);
            vidaJugador.RecibirDanio(new InfoDanio(vidaJugador.Maxima * 2f, TipoDanio.Golpe, Vector3.zero, Vector3.forward, null));
            Assert.IsTrue(vidaJugador.EstaKO);
            int golpes = 0;
            rival.Impacto += () => golpes++;
            peleadorRival.ActivarControl(true);

            yield return Esperar(4f);

            Assert.AreEqual(0, golpes, "No golpea a un jugador en KO");
        }
    }
}

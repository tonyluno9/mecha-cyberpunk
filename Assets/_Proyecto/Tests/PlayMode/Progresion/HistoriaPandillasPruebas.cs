using System;
using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.Aliados;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>
    /// T-406: la máquina de la historia de pandillas y el recorrido completo en Ciudad_Prologo hasta "CONTINUARÁ" (con EL DOC, EL INVENTOR y EL HACKER de T-418).
    /// Atajos anotados: los clics y E son entrada simulada; al jugador se le acerca con Colocar entre acciones; los pandilleros del tianguis y el jefe caen
    /// por daño directo (IRecibeDanio).
    /// </summary>
    public class HistoriaPandillasPruebas : InputTestFixture
    {
        private const string RutaEscena = "Assets/_Proyecto/Escenas/Ciudad_Prologo.unity";
        private const float DanoMatar = 100000f;

        private Keyboard teclado;
        private Mouse raton;

        public override void Setup()
        {
            base.Setup();
            teclado = InputSystem.AddDevice<Keyboard>();
            raton = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        [Test]
        public void MaquinaDePasos()
        {
            MaquinaHistoriaPandillas m = new MaquinaHistoriaPandillas();
            Assert.AreEqual(PasoHistoria.Despierta, m.Paso);
            Assert.IsFalse(m.Avisar(AvisoHistoria.PistolaTomada), "Un aviso fuera de orden no avanza.");
            Assert.AreEqual(PasoHistoria.Despierta, m.Paso);
            AvisoHistoria[] avisos = (AvisoHistoria[])Enum.GetValues(typeof(AvisoHistoria));
            PasoHistoria[] esperados =
            {
                PasoHistoria.PrimeraPelea, PasoHistoria.Pistola, PasoHistoria.Chato, PasoHistoria.Doc, PasoHistoria.Inventor, PasoHistoria.Tianguis,
                PasoHistoria.Jefe, PasoHistoria.NeonAlto, PasoHistoria.Continuara, PasoHistoria.Libre
            };
            for (int i = 0; i < avisos.Length; i++)
            {
                Assert.IsTrue(m.Avisar(avisos[i]));
                Assert.AreEqual(esperados[i], m.Paso);
            }
            Assert.IsFalse(m.Avisar(AvisoHistoria.SalioDeCasa), "Libre ya no avanza.");

            // Cargar un guardado en el paso Chato vuelve al paso Chato.
            GameObject objeto = new GameObject("DirectorPrueba");
            objeto.SetActive(false);
            DirectorPandillas director = objeto.AddComponent<DirectorPandillas>();
            director.Cargar("{\"paso\":\"Chato\"}");
            Assert.AreEqual(PasoHistoria.Chato, director.Paso);
            Assert.AreEqual("{\"paso\":\"Chato\"}", director.Guardar());
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no se entiende"));
            director.Cargar("basura");
            Assert.AreEqual(PasoHistoria.Chato, director.Paso, "Un guardado roto no cambia nada.");
            // T-418: los pasos nuevos se cargan por nombre; uno que ya no existe empieza en Despierta.
            director.Cargar("{\"paso\":\"NeonAlto\"}");
            Assert.AreEqual(PasoHistoria.NeonAlto, director.Paso);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no existe"));
            director.Cargar("{\"paso\":\"Mecha\"}");
            Assert.AreEqual(PasoHistoria.Despierta, director.Paso, "Un paso que ya no existe empieza en Despierta.");
            UnityEngine.Object.DestroyImmediate(objeto);
        }

        private static IEnumerator EsperarHasta(Func<bool> condicion, string que, float segundos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            while (!condicion())
            {
                Assert.Less(Time.realtimeSinceStartup, fin, "Tiempo agotado esperando: " + que);
                yield return null;
            }
        }

        private IEnumerator Pulsar(UnityEngine.InputSystem.Controls.ButtonControl tecla)
        {
            Press(tecla);
            yield return null;
            Release(tecla);
            yield return null;
        }

        private static void Acercar(Vector3 objetivo, float distancia)
        {
            IJugador jugador = Registro<IJugador>.Actual;
            Vector3 hacia = objetivo - jugador.Raiz.position;
            hacia.y = 0f;
            Vector3 dir = hacia.sqrMagnitude > 0.001f ? hacia.normalized : Vector3.forward;
            Vector3 pos = objetivo - dir * distancia;
            pos.y = objetivo.y + 0.1f;
            jugador.Colocar(new Pose(pos, Quaternion.LookRotation(dir, Vector3.up)));
        }

        // T-418: el jugador aparece frente al profe (donde mira), mirándolo, y lo usa con E.
        private IEnumerator UsarProfe(string id, float distancia)
        {
            MechaCyberpunk.Progresion.Apoyo.PersonajeApoyo profe = null;
            foreach (MechaCyberpunk.Progresion.Apoyo.PersonajeApoyo p in UnityEngine.Object.FindObjectsByType<MechaCyberpunk.Progresion.Apoyo.PersonajeApoyo>(FindObjectsSortMode.None))
            {
                profe = p.Datos.Id == id ? p : profe;
            }
            Assert.IsNotNull(profe, id);
            Vector3 frente = profe.transform.position + profe.transform.forward * distancia;
            if (Physics.Raycast(frente + Vector3.up * 3f, Vector3.down, out RaycastHit piso, 6f))
            {
                frente = piso.point;
            }
            frente.y += 0.1f;
            Vector3 hacia = profe.transform.position - frente;
            hacia.y = 0f;
            Registro<IJugador>.Actual.Colocar(new Pose(frente, Quaternion.LookRotation(hacia.normalized, Vector3.up)));
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Pulsar(teclado.eKey);
        }

        [UnityTest]
        public IEnumerator RecorridoHastaContinuara()
        {
            yield return EditorSceneManager.LoadSceneInPlayMode(RutaEscena, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            DirectorPandillas director = null;
            yield return EsperarHasta(() =>
            {
                director = UnityEngine.Object.FindFirstObjectByType<DirectorPandillas>();
                return director != null && director.Iniciado && Registro<IJugador>.Actual != null && Registro<ITerritorio>.Actual != null;
            }, "el director arrancó", 20f);
            IJugador jugador = Registro<IJugador>.Actual;
            ITerritorio territorio = Registro<ITerritorio>.Actual;
            Assert.AreEqual(PasoHistoria.Despierta, director.Paso);
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Assert.IsNotNull(arsenal);
            Assert.IsFalse(arsenal.Tiene(ArmaAPie.Pistola), "Al inicio solo tienes los puños.");

            // a. Salir de la casa caminando (W) hacia la puerta.
            Transform puerta = GameObject.Find("Casa_Puerta").transform;
            Vector3 dirPuerta = puerta.position - jugador.Raiz.position;
            dirPuerta.y = 0f;
            Debug.Log("[Prueba] cama " + jugador.Raiz.position + " puerta " + puerta.position);
            jugador.Colocar(new Pose(jugador.Raiz.position, Quaternion.LookRotation(dirPuerta.normalized, Vector3.up)));
            yield return null;
            Press(teclado.wKey);
            float finCamino = Time.realtimeSinceStartup + 20f;
            while (director.Paso == PasoHistoria.Despierta && Time.realtimeSinceStartup < finCamino)
            {
                yield return null;
            }
            Release(teclado.wKey);
            Debug.Log("[Prueba] fin camino " + jugador.Raiz.position);
            Assert.AreEqual(PasoHistoria.PrimeraPelea, director.Paso, "Caminar hacia la puerta saca de la casa.");

            // b. La primera pelea: clics sobre los Calaveras hasta vencerlos.
            float finPelea = Time.realtimeSinceStartup + 90f;
            while (director.Paso == PasoHistoria.PrimeraPelea)
            {
                Assert.Less(Time.realtimeSinceStartup, finPelea, "La primera pelea no terminó.");
                PandilleroAPie cercano = null;
                foreach (PandilleroAPie p in PandilleroAPie.Activos)
                {
                    if (p.name.StartsWith("CalaveraPunos") && !p.EstaKO && (cercano == null
                        || (p.transform.position - jugador.Raiz.position).sqrMagnitude < (cercano.transform.position - jugador.Raiz.position).sqrMagnitude))
                    {
                        cercano = p;
                    }
                }
                if (cercano != null)
                {
                    Acercar(cercano.transform.position, 1.2f);
                }
                yield return null;
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                for (int i = 0; i < 12; i++)
                {
                    yield return null;
                }
            }
            Assert.AreEqual(PasoHistoria.Pistola, director.Paso);

            // c. La pistola: E.
            ArmaEnPiso arma = UnityEngine.Object.FindFirstObjectByType<ArmaEnPiso>();
            Assert.IsNotNull(arma, "El último en caer suelta su pistola.");
            Acercar(arma.transform.position, 0.8f);
            yield return null;
            yield return null;
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Chato, "recoger la pistola", 5f);
            Assert.IsTrue(arsenal.Tiene(ArmaAPie.Pistola));
            StringAssert.Contains("CLIC DERECHO APUNTA", director.Pantalla.Ayuda);

            // d. El CHATO: E.
            Reclutable chato = null;
            foreach (Reclutable r in UnityEngine.Object.FindObjectsByType<Reclutable>(FindObjectsSortMode.None))
            {
                if (r.Nombre == "EL CHATO")
                {
                    chato = r;
                }
            }
            Assert.IsNotNull(chato);
            Acercar(chato.transform.position, 1.0f);
            yield return null;
            yield return null;
            yield return Pulsar(teclado.eKey);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Doc, "reclutar al CHATO", 5f);
            Assert.AreEqual(1, Registro<IPandillaJugador>.Actual.Cantidad);

            // d2. T-418: EL DOC y EL INVENTOR, con E.
            yield return UsarProfe(DirectorPandillas.IdDoc, 1.6f);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Inventor, "usar a EL DOC", 5f);
            yield return UsarProfe(DirectorPandillas.IdInventor, 2.2f);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Tianguis, "usar a EL INVENTOR", 5f);
            Assert.AreEqual(1, arsenal.Nivel(ArmaAPie.Pistola), "EL INVENTOR deja la PISTOLA I");

            // e. El tianguis: los Calaveras caen por daño directo hasta sacar al jefe.
            Time.timeScale = 4f;
            float finTianguis = Time.realtimeSinceStartup + 60f;
            while (director.Paso == PasoHistoria.Tianguis)
            {
                Assert.Less(Time.realtimeSinceStartup, finTianguis, "El tianguis no llegó a 70 de respeto: " + territorio.Respeto("tianguis"));
                foreach (PandillaAPie grupo in UnityEngine.Object.FindObjectsByType<PandillaAPie>(FindObjectsSortMode.None))
                {
                    foreach (PandilleroAPie p in grupo.Pandilleros)
                    {
                        if (p.gameObject.activeSelf && !p.EstaKO)
                        {
                            p.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, p.transform.position, Vector3.forward, null));
                        }
                    }
                }
                yield return null;
            }
            Assert.AreEqual(PasoHistoria.Jefe, director.Paso);

            // f. El jefe.
            JefePandilla punto = null;
            foreach (JefePandilla j in UnityEngine.Object.FindObjectsByType<JefePandilla>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (j.Zona == "tianguis")
                {
                    punto = j;
                }
            }
            Assert.IsNotNull(punto);
            yield return EsperarHasta(() => punto.Vivo, "aparece el jefe", 20f);
            punto.Jefe.RecibirDanio(new InfoDanio(DanoMatar, TipoDanio.Disparo, punto.Jefe.transform.position, Vector3.forward, null));

            // g. T-418: Neón Alto con EL HACKER y CONTINUARÁ.
            yield return EsperarHasta(() => director.Paso == PasoHistoria.NeonAlto, "conquista", 5f);
            Time.timeScale = 1f;
            Assert.AreEqual(IdPandilla.Jugador, territorio.Dueno("tianguis"));
            yield return UsarProfe(DirectorPandillas.IdHacker, 1.6f);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Continuara, "usar a EL HACKER", 5f);
            yield return EsperarHasta(() => director.Pantalla.ContinuaraVisible, "pantalla CONTINUARÁ", 30f);
            yield return new WaitForSecondsRealtime(1.1f);
            yield return Pulsar(teclado.enterKey);
            yield return EsperarHasta(() => director.Paso == PasoHistoria.Libre, "cerrar el continuará", 5f);
            Assert.IsFalse(director.Pantalla.ContinuaraVisible);
            Assert.AreEqual(1, Registro<IPandillaJugador>.Actual.Cantidad);
            Assert.IsTrue(arsenal.Tiene(ArmaAPie.Pistola));
        }
    }
}

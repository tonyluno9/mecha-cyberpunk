using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-746: un enemigo abatido con la pistola del jugador (entrada simulada) se queda tirado en el piso con su animación de caída,
    /// sin colisión ni disparos, y suelta dinero (se recoge al pasar), balas y su arma; los cuerpos tienen tope. Capturas con CAPTURAR_S1=1 en docs/capturas/s1e-T-746-*.png.
    /// </summary>
    public class CuerposBotinT746Pruebas : BaseJugadorAPie
    {
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaPolicia = "Assets/_Proyecto/IA/Prefabs/PoliciaAPie.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";
        private const string RutaDatosPolicia = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Policia.asset";

        private readonly List<GameObject> contenedores = new List<GameObject>();
        private DatosPandilleroAPie datosEnemigo;

        public override void Setup()
        {
            EventosBotin.LimpiarSuscriptores();
            ColocadorBotin.Suscribir();
            base.Setup();
            datosEnemigo = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(RutaDatos);
        }

        public override void TearDown()
        {
            ColocadorBotin.Azar = () => Random.value;
            foreach (GameObject c in contenedores) Object.Destroy(c);
            contenedores.Clear();
            foreach (DineroEnPiso d in Object.FindObjectsByType<DineroEnPiso>(FindObjectsSortMode.None)) Object.Destroy(d.gameObject);
            foreach (ArmaEnPiso a in Object.FindObjectsByType<ArmaEnPiso>(FindObjectsSortMode.None)) Object.Destroy(a.gameObject);
            foreach (MunicionEnPiso m in Object.FindObjectsByType<MunicionEnPiso>(FindObjectsSortMode.None)) Object.Destroy(m.gameObject);
            base.TearDown();
        }

        private PandilleroAPie Enemigo(string ruta, string rutaDatos, Vector3 posicion)
        {
            var contenedor = new GameObject("EnemigoPrueba");
            contenedores.Add(contenedor);
            contenedor.SetActive(false);
            GameObject p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), contenedor.transform);
            p.transform.position = posicion;
            var so = new SerializedObject(p.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DatosPandilleroAPie>(rutaDatos);
            so.ApplyModifiedPropertiesWithoutUndo();
            contenedor.SetActive(true);
            return p.GetComponent<PandilleroAPie>();
        }

        private static InfoDanio Muerte(PandilleroAPie p) => new InfoDanio(1000f, TipoDanio.Disparo, p.transform.position, Vector3.forward, null);

        [UnityTest]
        public IEnumerator Enemigo_AbatidoConLaPistola_SeQuedaTirado_YSueltaDinero()
        {
            yield return Calentar();
            Press(teclado.digit2Key);
            yield return null;
            Release(teclado.digit2Key);
            ArmasAPie armas = jugador.GetComponent<ArmasAPie>();
            float limite = Time.time + 3f;
            while (armas.ArmaActiva != ArmaAPie.Pistola && Time.time < limite) yield return null;
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva);
            yield return Esperar(0.3f);
            ICartera cartera = Registro<ICartera>.Actual;
            int antes = cartera.Dinero;
            float mx = jugador.GetComponent<MechaCyberpunk.Personaje.Movimiento.ControladorAPie>().Camara.position.x;
            PandilleroAPie enemigo = Enemigo(RutaPandillero, RutaDatos, new Vector3(mx, 0.05f, 12f));
            yield return Esperar(0.2f);
            Assert.IsFalse(enemigo.EstaKO);

            // Dispara con el botón izquierdo hasta abatirlo.
            float fin = Time.time + 25f;
            while (!enemigo.EstaKO && Time.time < fin)
            {
                if (jugador.GetComponent<PistolaAPie>().Balas <= 0)
                {
                    Press(teclado.rKey); // se queda sin balas: recarga con R
                    yield return null;
                    Release(teclado.rKey);
                    yield return Esperar(2.5f);
                    continue;
                }
                Vector3 haciaEnemigo = enemigo.transform.position - jugador.transform.position; // el enemigo se mueve: se apunta a él antes de cada tiro
                haciaEnemigo.y = 0f;
                jugador.transform.rotation = Quaternion.LookRotation(haciaEnemigo, Vector3.up);
                Press(raton.leftButton);
                yield return null;
                Release(raton.leftButton);
                yield return Esperar(0.2f);
            }
            Assert.IsTrue(enemigo.EstaKO, "La pistola del jugador lo abatió (vida enemigo " + enemigo.Actual + "/" + enemigo.Maxima + ", vida jugador " + jugador.GetComponent<MechaCyberpunk.Personaje.Vida.VidaAPie>().Actual + ", balas " + jugador.GetComponent<PistolaAPie>().Balas + ")");
            Vector3 donde = enemigo.transform.position;
            yield return Esperar(5f);

            // Sigue ahí, tirado, sin colisión ni disparos; no cuenta como vivo.
            Assert.IsTrue(enemigo.gameObject.activeSelf, "A los 5 s el cuerpo sigue en el piso");
            Assert.IsTrue(enemigo.EstaKO);
            Assert.IsFalse(enemigo.EnPelea, "No cuenta como vivo ni pelea");
            Assert.IsFalse(enemigo.GetComponent<CharacterController>().enabled, "Sin colisión contra el jugador");
            Assert.Less(Vector3.Distance(enemigo.transform.position, donde), 0.5f);
            Assert.Contains(enemigo, new List<PandilleroAPie>(PandilleroAPie.CuerposTirados));
            DineroEnPiso dinero = Object.FindFirstObjectByType<DineroEnPiso>();
            Assert.IsNotNull(dinero, "Suelta dinero");
            Assert.AreEqual(datosEnemigo.Dinero, dinero.Cantidad);
            CapturaS1.FotoMira("s1e-T-746-cuerpo-botin", jugador.transform, dinero.transform.position + Vector3.up * 0.2f, new Vector3(0.3f, 0.7f, -1f), 4f, 50f, 800);

            Vector3 posDinero = dinero.transform.position;
            // Camina hasta el dinero y lo recoge; pasa por donde cayó el cuerpo sin chocar.
            Press(teclado.wKey);
            fin = Time.time + 10f;
            while (!dinero.FueTomado && Time.time < fin) yield return null;
            Release(teclado.wKey);
            yield return null;
            Assert.IsTrue(dinero.FueTomado, "Se recoge al pasar (jugador en " + jugador.transform.position + ", dinero en " + posDinero + ")");
            Assert.AreEqual(antes + datosEnemigo.Dinero, cartera.Dinero);
        }

        [UnityTest]
        public IEnumerator Policia_Abatido_SeQuedaTirado_YSueltaDinero_CuerposConTope()
        {
            yield return Calentar();
            PandilleroAPie policia = Enemigo(RutaPolicia, RutaDatosPolicia, new Vector3(2f, 0.05f, 6f));
            yield return Esperar(0.3f);
            policia.RecibirDanio(Muerte(policia));
            yield return Esperar(6f);
            Assert.IsTrue(policia.EstaKO && policia.gameObject.activeSelf, "El policía muerto sigue tirado");
            Assert.IsNotNull(Object.FindFirstObjectByType<DineroEnPiso>(), "El policía suelta dinero");
            CapturaS1.FotoMira("s1e-T-746-policia-tirado", jugador.transform, policia.transform.position + Vector3.up * 0.3f, new Vector3(0.2f, 0.6f, -1f), 4f, 50f, 800);

            // Tope de cuerpos: el más viejo se retira primero.
            var todos = new List<PandilleroAPie> { policia };
            for (int i = 0; i < PandilleroAPie.MaximoCuerpos + 2; i++)
            {
                PandilleroAPie extra = Enemigo(RutaPandillero, RutaDatos, new Vector3(-6f + i * 1.2f, 0.05f, 14f));
                todos.Add(extra);
                yield return null;
                extra.RecibirDanio(Muerte(extra));
                yield return null;
            }
            yield return Esperar(0.3f);
            int tirados = 0;
            foreach (PandilleroAPie p in todos) tirados += p.gameObject.activeSelf && p.EstaKO ? 1 : 0;
            Assert.LessOrEqual(tirados, PandilleroAPie.MaximoCuerpos, "No pasan del tope");
            Assert.LessOrEqual(PandilleroAPie.CuerposTirados.Count, PandilleroAPie.MaximoCuerpos);
            Assert.IsFalse(policia.gameObject.activeSelf, "El cuerpo más viejo fue el primero en retirarse");
            Assert.IsTrue(todos[todos.Count - 1].gameObject.activeSelf, "El más nuevo se queda");
        }

        [UnityTest]
        public IEnumerator Enemigo_SueltaBalas_YSeRecogenAlPasar_ConAviso()
        {
            yield return Calentar();
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Assert.IsNotNull(arsenal);
            Assert.IsNotNull(arsenal.DarMejora(ArmaAPie.Escopeta), "El jugador tiene escopeta (usa balas de reserva)");
            ReservaBalas reserva = Registro<ReservaBalas>.Actual;
            Assert.IsNotNull(reserva);
            int antes = reserva.Cantidad(ArmaAPie.Escopeta);
            ColocadorBotin.Azar = () => 0f; // siempre suelta balas
            ArmaAPie? avisada = null;
            int avisadas = 0;
            System.Action<ArmaAPie, int> alRecoger = (a, n) => { avisada = a; avisadas = n; };
            MunicionEnPiso.Recogida += alRecoger;
            try
            {
                PandilleroAPie enemigo = Enemigo(RutaPandillero, RutaDatos, new Vector3(0f, 0.05f, 8f));
                yield return Esperar(0.2f);
                enemigo.RecibirDanio(Muerte(enemigo));
                yield return Esperar(0.4f);
                MunicionEnPiso municion = Object.FindFirstObjectByType<MunicionEnPiso>();
                Assert.IsNotNull(municion, "Suelta balas");
                Press(teclado.wKey);
                float fin = Time.time + 10f;
                while (!municion.FueTomada && Time.time < fin) yield return null;
                Release(teclado.wKey);
                yield return null;
                Assert.IsTrue(municion.FueTomada, "Se recogen al pasar");
                Assert.Greater(reserva.Cantidad(ArmaAPie.Escopeta), antes, "Suman a la reserva");
                Assert.AreEqual(ArmaAPie.Escopeta, avisada);
                Assert.Greater(avisadas, 0, "Avisa cuántas balas recogió (el HUD pone +N BALAS)");
            }
            finally
            {
                MunicionEnPiso.Recogida -= alRecoger;
            }
        }

        [UnityTest]
        public IEnumerator Policia_Mide180_JuntoAlJugador()
        {
            yield return Calentar();
            // Lejos del jugador (de pie, sin pelear) para medir la postura normal.
            PandilleroAPie policia = Enemigo(RutaPolicia, RutaDatosPolicia, new Vector3(150f, 0.05f, 150f));
            var pisoLejos = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pisoLejos.transform.position = new Vector3(150f, -0.5f, 150f);
            pisoLejos.transform.localScale = new Vector3(20f, 1f, 20f);
            policia.enabled = false; // quieto, de pie: se mide la postura normal, no corriendo ni apuntando
            foreach (Animator a in policia.GetComponentsInChildren<Animator>(true)) a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            yield return Esperar(1.5f);
            float alturaPolicia = Altura(policia.gameObject);
            float alturaJugador = Altura(jugador);
            Debug.Log("[T-746] policía " + alturaPolicia + " m, jugador " + alturaJugador + " m");
            Object.Destroy(pisoLejos);
            Assert.AreEqual(1.8f, alturaPolicia, 0.06f, "El policía mide 1.80 m animado");
            policia.transform.position = new Vector3(0.9f, 0.05f, 0.6f);
            yield return Esperar(0.3f);
            CapturaS1.Foto("s1e-T-746-policia-junto-a-pj01", jugador.transform, new Vector3(-0.4f, 1.5f, 3.2f), new Vector3(0.5f, 0.9f, 0.6f), 45f, 900, 700);
        }

        private static float Altura(GameObject raiz)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            var malla = new Mesh();
            foreach (SkinnedMeshRenderer r in raiz.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (!r.enabled || r.sharedMesh == null) continue;
                r.BakeMesh(malla, true);
                foreach (Vector3 v in malla.vertices)
                {
                    float y = r.transform.TransformPoint(v).y;
                    min = Mathf.Min(min, y);
                    max = Mathf.Max(max, y);
                }
            }
            Object.Destroy(malla);
            return max - min;
        }
    }
}

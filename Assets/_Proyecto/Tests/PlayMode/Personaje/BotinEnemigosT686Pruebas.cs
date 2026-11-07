using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.IA.Datos;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-686: al caer, un pandillero suelta su dinero (se recoge al pasar) y su arma (ArmaEnPiso, se recoge con E); el jefe suelta una escopeta;
    /// no se duplica una arma que ya está en el piso, y el pandillero sigue cayendo como antes. Capturas con CAPTURAR_S1=1 en docs/capturas/s1t-T-686-*.png.
    /// </summary>
    public class BotinEnemigosT686Pruebas : BaseJugadorAPie
    {
        private const string RutaPandillero = "Assets/_Proyecto/IA/Prefabs/PandilleroArmado.prefab";
        private const string RutaDatos = "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset";

        private GameObject contenedor;
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
            Object.Destroy(contenedor);
            foreach (DineroEnPiso d in Object.FindObjectsByType<DineroEnPiso>(FindObjectsSortMode.None)) Object.Destroy(d.gameObject);
            foreach (ArmaEnPiso a in Object.FindObjectsByType<ArmaEnPiso>(FindObjectsSortMode.None)) Object.Destroy(a.gameObject);
            base.TearDown();
        }

        private PandilleroAPie Enemigo(Vector3 posicion, bool jefe = false)
        {
            contenedor = new GameObject("EnemigoPrueba");
            contenedor.SetActive(false);
            GameObject p = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPandillero), contenedor.transform);
            p.transform.position = posicion;
            var so = new SerializedObject(p.GetComponent<PandilleroAPie>());
            so.FindProperty("datos").objectReferenceValue = datosEnemigo;
            so.ApplyModifiedPropertiesWithoutUndo();
            PandilleroAPie enemigo = p.GetComponent<PandilleroAPie>();
            if (jefe)
            {
                enemigo.MarcarComoJefe();
            }
            contenedor.SetActive(true);
            return enemigo;
        }

        private static InfoDanio Muerte(PandilleroAPie p) => new InfoDanio(1000f, TipoDanio.Disparo, p.transform.position, Vector3.forward, null);

        [UnityTest]
        public IEnumerator Pandillero_AlCaer_SueltaDineroYArma_SinAbonarSolo()
        {
            yield return Calentar();
            yield return Esperar(0.2f);
            ICartera cartera = Registro<ICartera>.Actual;
            int antes = cartera.Dinero;
            PandilleroAPie enemigo = Enemigo(new Vector3(0f, 0.05f, 12f));
            yield return Esperar(0.2f);
            Assert.IsFalse(enemigo.EstaKO);
            enemigo.RecibirDanio(Muerte(enemigo));
            yield return Esperar(0.3f);

            Assert.IsTrue(enemigo.EstaKO, "Sigue cayendo en KO como antes");
            Assert.AreEqual(antes, cartera.Dinero, "El dinero ya no se abona solo");
            DineroEnPiso dinero = Object.FindFirstObjectByType<DineroEnPiso>();
            Assert.IsNotNull(dinero, "Suelta dinero");
            Assert.AreEqual(datosEnemigo.Dinero, dinero.Cantidad);
            Assert.Less(Vector3.Distance(dinero.transform.position, enemigo.transform.position), 2f, "Cae junto al enemigo");
            ArmaEnPiso arma = Object.FindFirstObjectByType<ArmaEnPiso>();
            Assert.IsNotNull(arma, "Suelta su arma");
            Assert.AreEqual(ArmaAPie.Pistola, arma.Arma);
            Assert.Less(Vector3.Distance(arma.transform.position, enemigo.transform.position), 2f);
            Assert.IsTrue(arma.GetComponentsInChildren<Renderer>().Length > 0, "El arma tiene modelo");
            yield return Esperar(1.2f);
            CapturaS1.FotoMira("s1t-T-686-botin", jugador.transform, dinero.transform.position + Vector3.up * 0.2f, new Vector3(0.2f, 0.9f, -1f), 3.2f, 50f, 800);

            // El jugador camina hacia el dinero y lo recoge al pasar.
            Press(teclado.wKey);
            float fin = Time.time + 8f;
            while (!dinero.FueTomado && Time.time < fin)
            {
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Assert.IsTrue(dinero.FueTomado, "Se recoge al pasar");
            Assert.AreEqual(antes + datosEnemigo.Dinero, cartera.Dinero);

            // El arma se recoge con E (aquí, lo que hace la E sobre ella).
            arma.Usar();
            Assert.IsTrue(arma.FueTomada);
        }

        [UnityTest]
        public IEnumerator Jefe_SueltaEscopeta()
        {
            yield return Calentar();
            PandilleroAPie jefe = Enemigo(new Vector3(0f, 0.05f, 12f), true);
            yield return Esperar(0.2f);
            jefe.RecibirDanio(Muerte(jefe));
            yield return Esperar(0.3f);
            ArmaEnPiso arma = Object.FindFirstObjectByType<ArmaEnPiso>();
            Assert.IsNotNull(arma);
            Assert.AreEqual(ArmaAPie.Escopeta, arma.Arma, "El jefe suelta una escopeta");
        }

        [UnityTest]
        public IEnumerator ArmaYaEnElPiso_NoSeDuplica()
        {
            yield return Calentar();
            PandilleroAPie enemigo = Enemigo(new Vector3(0f, 0.05f, 12f));
            yield return Esperar(0.2f);
            var existente = new GameObject("PistolaDeLaHistoria");
            existente.transform.position = enemigo.transform.position + Vector3.right * 0.5f;
            existente.AddComponent<ArmaEnPiso>();
            yield return null;
            enemigo.RecibirDanio(Muerte(enemigo));
            yield return Esperar(0.3f);
            int cerca = 0;
            foreach (ArmaEnPiso a in Object.FindObjectsByType<ArmaEnPiso>(FindObjectsSortMode.None))
            {
                if ((a.transform.position - enemigo.transform.position).sqrMagnitude < 9f) cerca++;
            }
            Assert.AreEqual(1, cerca, "No se suelta una segunda pistola junto a la que ya está");
            Object.Destroy(existente);
        }
    }
}

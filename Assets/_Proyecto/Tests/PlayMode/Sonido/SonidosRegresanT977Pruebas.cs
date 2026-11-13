using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Sonido;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Sonido
{
    /// <summary>
    /// T-977: con el CatalogoSonidos real. Por zonas (calle, casino, mercado con la clínica adentro) suena un solo ambiente a la vez y el que
    /// toca; y pidiendo todos los que regresan a cada cuadro, ninguno se repite en exceso ni pasan de 12 voces.
    /// </summary>
    public class SonidosRegresanT977Pruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        private const float Cruce = 1.3f;
        private GameObject objeto;

        [SetUp]
        public void Preparar() => Registros.LimpiarTodos();

        [TearDown]
        public void Limpiar()
        {
            if (objeto != null) Object.Destroy(objeto);
            Registros.LimpiarTodos();
        }

        private GestorSonidos CrearGestor()
        {
            CatalogoSonidos catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            Assert.IsNotNull(catalogo);
            objeto = new GameObject("GestorSonidos");
            objeto.AddComponent<AudioListener>();
            GestorSonidos g = objeto.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        // Espera el cruce contando, cuadro a cuadro, cuántos ambientes suenan; devuelve el máximo.
        private IEnumerator Esperar(GestorSonidos g, float segundos, List<int> maximos)
        {
            float fin = Time.realtimeSinceStartup + segundos;
            int max = 0;
            while (Time.realtimeSinceStartup < fin)
            {
                max = Mathf.Max(max, g.AmbientesSonando(0.0005f));
                yield return null;
            }
            maximos.Add(max);
        }

        [UnityTest]
        public IEnumerator PorZonas_SuenaUnSoloAmbiente_YElQueToca()
        {
            GestorSonidos g = CrearGestor();
            List<int> maximos = new List<int>();
            yield return Esperar(g, Cruce, maximos);
            Assert.AreEqual("AMB_CALLE_NOCHE", g.AmbienteDeseado, "calle de noche");
            Assert.AreEqual(1, g.AmbientesSonando(0.0005f));
            float calle = g.VolumenAmbienteActual;

            EventosLugares.PublicarEntro(IdLugar.Casino);
            yield return Esperar(g, Cruce, maximos);
            Assert.AreEqual("AMB_CASINO", g.AmbienteDeseado);
            Assert.AreEqual(1, g.AmbientesSonando(0.0005f), "en el casino solo el del casino");
            float casino = g.VolumenAmbienteActual;

            EventosLugares.PublicarSalio(IdLugar.Casino);
            EventosLugares.PublicarEntro(IdLugar.MercadoNegro);
            yield return Esperar(g, Cruce, maximos);
            Assert.AreEqual(0, g.AmbientesSonando(0.0005f), "el mercado no tiene ambiente prendido: silencio, no el de la calle");

            EventosLugares.PublicarEntro(IdLugar.Clinica);
            yield return Esperar(g, Cruce, maximos);
            Assert.AreEqual("AMB_CLINICA", g.AmbienteDeseado);
            Assert.AreEqual(1, g.AmbientesSonando(0.0005f));
            float clinica = g.VolumenAmbienteActual;

            EventosLugares.PublicarSalio(IdLugar.Clinica);
            yield return Esperar(g, Cruce, maximos);
            Assert.AreEqual("AMB_MERCADO", g.AmbienteDeseado, "al salir de la clínica sigue dentro del mercado");

            EventosLugares.PublicarSalio(IdLugar.MercadoNegro);
            yield return Esperar(g, Cruce, maximos);
            Assert.AreEqual("AMB_CALLE_NOCHE", g.AmbienteDeseado);
            Assert.AreEqual(1, g.AmbientesSonando(0.0005f));

            foreach (int m in maximos) Assert.LessOrEqual(m, 2, "a lo más dos durante el cruce de 1 s");
            Debug.Log("[T-977] volumen de ambiente: calle " + calle + ", casino " + casino + ", clínica " + clinica);
            Assert.Greater(calle, casino, "la calle (archivo más quieto) lleva más volumen que el casino");
            Assert.Greater(casino, clinica, "y el casino más que la clínica (archivo más fuerte)");
        }

        [UnityTest]
        public IEnumerator PidiendoTodosACadaCuadro_NingunoSeRepiteEnExceso()
        {
            GestorSonidos g = CrearGestor();
            yield return null;
            string[] puntuales =
            {
                "VEH_PUERTA_ABRIR", "VEH_PUERTA_CERRAR", "ARM_BALA_IMPACTO_CONCRETO", "PAN_RIVAL_ALERTA", "PAN_JEFE_ENTRADA", "UI_OBJETIVO_NUEVO",
                "UI_OBJETIVO_CUMPLIDO", "JUG_PASO_ASFALTO", "CAL_SUBE_NIVEL", "ECO_DINERO_RECOGER", "MUN_PUERTA_CASA_ABRIR", "MUN_PUERTA_CASA_CERRAR",
                "ARM_PIS_DISPARO",
            };
            float inicio = Time.realtimeSinceStartup;
            int maxVoces = 0;
            int repetidos = 0;
            while (Time.realtimeSinceStartup - inicio < 6f)
            {
                foreach (string id in puntuales)
                {
                    // Cinco pandilleros te ven a la vez, etc.: cada ID se pide varias veces por cuadro.
                    for (int i = 0; i < 5; i++) g.ReproducirEn(id, Vector3.right * i);
                }
                maxVoces = Mathf.Max(maxVoces, g.VocesTotales);
                List<string> ids = g.IdsSonando();
                if (new HashSet<string>(ids).Count != ids.Count) repetidos++;
                yield return null;
            }
            float segundos = Time.realtimeSinceStartup - inicio;
            Assert.LessOrEqual(maxVoces, ReglaMezclaSonidos.MaxVocesTotal);
            Assert.AreEqual(0, repetidos, "nunca dos voces del mismo ID");
            foreach (string id in puntuales)
            {
                float minimo = Mathf.Max(ReglaMezclaSonidos.Para(id).SegundosMinimos, ReglaMezclaSonidos.SegundosMinimosPorId);
                int tope = Mathf.CeilToInt(segundos / minimo) + 1;
                Debug.Log("[T-977] " + id + " sonó " + g.Sonados(id) + " veces en " + segundos.ToString("0.0") + " s (tope " + tope + ")");
                Assert.Greater(g.Sonados(id), 0, id + " suena");
                Assert.LessOrEqual(g.Sonados(id), tope, id + " no se repite en exceso");
            }
            Assert.LessOrEqual(g.Sonados("PAN_RIVAL_ALERTA"), 4, "el grito de alerta, a lo más cada 2.5 s");
            Assert.AreEqual(1, g.Sonados("PAN_JEFE_ENTRADA"), "la entrada del jefe, una vez");
        }

        [UnityTest]
        public IEnumerator ElDerrape_EsUnBucleBajo_QueSeCorta()
        {
            GestorSonidos g = CrearGestor();
            yield return null;
            Transform carro = new GameObject("Carro").transform;
            carro.SetParent(objeto.transform);
            int bucle = g.ReproducirBucle("VEH_FRENO_DERRAPE", carro);
            Assert.AreNotEqual(Sonidos.SinBucle, bucle, "suena");
            yield return null;
            AudioSource fuente = objeto.transform.Find("Bucle_VEH_FRENO_DERRAPE").GetComponent<AudioSource>();
            Assert.IsTrue(fuente.loop);
            float seccion = g.Volumen(SeccionSonido.General) * g.Volumen(SeccionSonido.Efectos) * g.Catalogo.Ganancia(SeccionSonido.Efectos);
            Assert.AreEqual(ReglaMezclaSonidos.Para("VEH_FRENO_DERRAPE").Volumen * seccion, fuente.volume, 0.01f, "con su volumen relativo");
            g.DetenerBucle(bucle);
            yield return null;
            Assert.IsFalse(g.IdsSonando().Contains("VEH_FRENO_DERRAPE"), "se corta al terminar el derrape");
        }
    }
}

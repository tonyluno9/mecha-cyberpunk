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
    /// T-620: con el CatalogoSonidos real (solo 24 IDs encendidos), un minuto de juego simulado con tiroteo, carros, patrullas, pasos, voces e interfaz
    /// nunca pasa de 12 voces, nunca repite un ID a la vez y ningun ID apagado suena.
    /// </summary>
    public class SonidoMinimoT620Pruebas
    {
        private const string RutaCatalogo = "Assets/_Proyecto/Progresion/Datos/CatalogoSonidos.asset";
        private GameObject objeto;
        private float escalaAnterior;

        [SetUp]
        public void Preparar()
        {
            escalaAnterior = Time.timeScale;
            Registros.LimpiarTodos();
        }

        [TearDown]
        public void Limpiar()
        {
            Time.timeScale = escalaAnterior;
            if (objeto != null) Object.Destroy(objeto);
            Registros.LimpiarTodos();
        }

        private GestorSonidos CrearGestor(out CatalogoSonidos catalogo)
        {
            catalogo = AssetDatabase.LoadAssetAtPath<CatalogoSonidos>(RutaCatalogo);
            Assert.IsNotNull(catalogo);
            objeto = new GameObject("GestorSonidos");
            GestorSonidos g = objeto.AddComponent<GestorSonidos>();
            g.Catalogo = catalogo;
            return g;
        }

        [UnityTest]
        public IEnumerator IdApagado_NoSuena_PeroSeCuentaElPedido()
        {
            GestorSonidos g = CrearGestor(out _);
            yield return null;
            g.Reproducir("UI_TEXTO");
            g.ReproducirEn("JUG_PASO_INTERIOR", Vector3.zero); // T-977: el de asfalto ya regresó
            Assert.AreEqual(1, g.Pedidos("UI_TEXTO"));
            Assert.AreEqual(0, g.VocesTotales, "apagados: no suena nada");
            Assert.AreEqual(Sonidos.SinBucle, g.ReproducirBucle("AMB_TIANGUIS", null));
            g.Reproducir("UI_ACEPTAR");
            Assert.AreEqual(1, g.VocesTotales, "encendido: suena");
        }

        [UnityTest]
        public IEnumerator MismoIdAntesDeUnaDecima_NoSeRepite_YDespuesSi()
        {
            GestorSonidos g = CrearGestor(out _);
            yield return null;
            g.Reproducir("ARM_VACIO");
            g.Reproducir("ARM_VACIO");
            Assert.AreEqual(1, g.VocesTotales);
            yield return new WaitForSecondsRealtime(0.15f);
            g.Reproducir("ARM_VACIO");
            Assert.AreEqual(1, g.IdsSonando().FindAll(i => i == "ARM_VACIO").Count, "una sola voz por ID");
        }

        [UnityTest]
        public IEnumerator UnMinutoDeTiroteoYCarros_Max12VocesSinIdsRepetidosNiApagados()
        {
            GestorSonidos g = CrearGestor(out CatalogoSonidos catalogo);
            yield return null;
            // clip -> id, para saber que ID suena en cada fuente.
            Dictionary<AudioClip, string> idDelClip = new Dictionary<AudioClip, string>();
            List<string> todosLosIds = new List<string>();
            foreach (CatalogoSonidos.Entrada e in catalogo.Entradas)
            {
                todosLosIds.Add(e.id);
                foreach (AudioClip c in e.finales) idDelClip[c] = e.id;
                foreach (AudioClip c in e.provisionales) idDelClip[c] = e.id;
            }
            Transform carroA = new GameObject("CarroA").transform;
            Transform carroB = new GameObject("CarroB").transform;
            Transform patrulla = new GameObject("Patrulla").transform;
            Transform patrulla2 = new GameObject("Patrulla2").transform;
            g.ReproducirBucle("VEH_VIEJO_MOTOR_RALENTI", carroA);
            g.ReproducirBucle("VEH_VIEJO_MOTOR_RALENTI", carroB);   // el segundo del mismo ID no entra
            g.ReproducirBucle("CAL_PATRULLA_SIRENA", patrulla);
            g.ReproducirBucle("CAL_PATRULLA_SIRENA", patrulla2);
            string[] armas = { "ARM_PIS_DISPARO", "ARM_ESC_DISPARO", "ARM_FUS_DISPARO", "ARM_SNI_DISPARO", "ARM_PIS_RECARGA", "ARM_ESC_RECARGA_CARTUCHO",
                "ARM_FUS_RECARGA", "ARM_SNI_RECARGA", "ARM_VACIO", "ARM_BALA_IMPACTO_CARNE", "ARM_BALA_IMPACTO_CONCRETO", "ARM_CASQUILLO", "ARM_DISPARO_LEJANO" };
            string[] carros = { "VEH_CHOQUE_LEVE", "VEH_CHOQUE_FUERTE", "VEH_ALARMA_ROBO", "VEH_EXPLOSION", "VEH_CLAXON_TRAFICO", "VEH_CLAXON_VIEJO", "VEH_PUERTA_ABRIR", "VEH_PASA_CERCA" };
            string[] cuerpo = { "PUN_GOLPE_CARNE", "JUG_DANIO", "JUG_CAE_DERROTADO", "JUG_PASO_ASFALTO", "JUG_SALTO", "JUG_CORRER_RESPIRA", "PAN_RIVAL_DANIO@F",
                "PAN_RIVAL_CAE", "PAN_RIVAL_ALERTA" };
            string[] ui = { "UI_MOVER", "UI_ACEPTAR", "UI_CANCELAR", "UI_TEXTO", "ZON_CONQUISTADA", "ECO_COMPRA" };

            Time.timeScale = 4f;
            float simulado = 0f;
            int maxVoces = 0;
            int maxFuentesReales = 0;
            int repetidos = 0;
            int apagadosSonando = 0;
            int cuadro = 0;
            HashSet<string> sonaron = new HashSet<string>();
            while (simulado < 60f)
            {
                simulado += Time.deltaTime;
                cuadro++;
                // Tiroteo: varias armas, impactos y casquillos en cada cuadro; carros, cuerpo, interfaz y un barrido de todos los IDs.
                for (int i = 0; i < 4; i++) g.ReproducirEn(armas[(cuadro + i * 3) % armas.Length], Vector3.forward * (cuadro % 20));
                g.ReproducirEn(carros[cuadro % carros.Length], Vector3.right * 5f);
                g.ReproducirEn(cuerpo[cuadro % cuerpo.Length], Vector3.left * 3f);
                g.Reproducir(ui[cuadro % ui.Length]);
                g.Reproducir(todosLosIds[cuadro % todosLosIds.Count]);
                maxVoces = Mathf.Max(maxVoces, g.VocesTotales);
                List<string> ids = g.IdsSonando();
                HashSet<string> distintos = new HashSet<string>(ids);
                if (distintos.Count != ids.Count) repetidos++;
                int reales = 0;
                foreach (AudioSource a in objeto.GetComponentsInChildren<AudioSource>())
                {
                    if (!a.isPlaying || a.clip == null || a.name.StartsWith("Ambiente_")) continue;
                    reales++;
                    if (!idDelClip.TryGetValue(a.clip, out string id) || !catalogo.Encendido(id)) apagadosSonando++;
                    else sonaron.Add(id);
                }
                maxFuentesReales = Mathf.Max(maxFuentesReales, reales);
                yield return null;
            }
            Debug.Log("[T-620] Minuto simulado (" + cuadro + " cuadros): voces maximas " + maxVoces + ", fuentes reales maximas " + maxFuentesReales
                + ", IDs distintos que sonaron " + sonaron.Count + ".");
            Assert.LessOrEqual(maxVoces, ReglaMezclaSonidos.MaxVocesTotal, "voces del gestor");
            Assert.LessOrEqual(maxFuentesReales, ReglaMezclaSonidos.MaxVocesTotal, "fuentes que de verdad suenan");
            Assert.AreEqual(0, repetidos, "nunca dos voces del mismo ID");
            Assert.AreEqual(0, apagadosSonando, "ningun ID apagado suena");
            foreach (string id in new[] { "ARM_PIS_DISPARO", "ARM_ESC_DISPARO", "VEH_CHOQUE_LEVE", "VEH_EXPLOSION", "PUN_GOLPE_CARNE", "UI_ACEPTAR" })
            {
                Assert.IsTrue(sonaron.Contains(id), id + " debia sonar");
            }
            Assert.IsFalse(sonaron.Contains("UI_TEXTO"));
            Assert.IsFalse(sonaron.Contains("VEH_CLAXON_TRAFICO"));
        }
    }
}

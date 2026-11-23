using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using MechaCyberpunk.Personaje.Visual.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>T-53: Override_HumanoJugador tiene exactamente los clips del jugador. Necesita haber corrido HumanoJugadorT53.Aplicar.</summary>
    public class HumanoJugadorT53Pruebas
    {
        private static AnimatorOverrideController Cargar(string ruta)
        {
            var o = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ruta);
            Assert.IsNotNull(o, "Falta " + ruta + ": corre HumanoJugadorT53.Aplicar");
            return o;
        }

        [Test]
        public void Override_TieneLosMismosClipsQueElJugador_EnCadaEstado()
        {
            Dictionary<string, AnimationClip> jugador = HumanoJugadorT53.Pares(Cargar(ConstructorControladorBase.RutaOverride("Jugador")));
            Dictionary<string, AnimationClip> humano = HumanoJugadorT53.Pares(Cargar(HumanoJugadorT53.RutaOverride));
            Assert.Greater(jugador.Count, 50);
            Assert.AreEqual(jugador.Count, humano.Count);
            foreach (KeyValuePair<string, AnimationClip> par in jugador)
            {
                Assert.IsTrue(humano.ContainsKey(par.Key), par.Key);
                Assert.AreSame(par.Value, humano[par.Key], "clip distinto en " + par.Key);
            }
        }

        [TestCase("Loco_Caminar", "PJ01_Caminar")]
        [TestCase("Loco_Correr", "PJ01_CorrerRapido")]
        [TestCase("Loco_Idle", "PJ01_Loco_Idle")]
        [TestCase("Loco_SaltoInicio", "PJ01_Salto_Despegue")]
        [TestCase("Loco_Agacharse", "PJ01_Loco_Agacharse")]
        [TestCase("Pistola_Sacar", "PJ01_Pistola_Sacar")]
        [TestCase("Pistola_Guardar", "PJ01_Pistola_Guardar")]
        [TestCase("Pistola_Disparo", "PJ01_Pistola_Disparo")]
        [TestCase("Pistola_Recargar", "PJ01_Pistola_Recargar")]
        [TestCase("Pistola_Dano_Frente", "PJ01_Dano_Frente")]
        [TestCase("Muerte_Adelante", "PJ01_Muerte_Frente")]
        [TestCase("Cobertura_Entrar", "PJ01_Cobertura_Entrar")]
        public void Override_ClavesDeEstado_SonLosClipsDelJugador(string clipBase, string esperado)
        {
            Dictionary<string, AnimationClip> humano = HumanoJugadorT53.Pares(Cargar(HumanoJugadorT53.RutaOverride));
            Assert.IsTrue(humano.ContainsKey(clipBase), clipBase);
            Assert.AreEqual(esperado, humano[clipBase].name);
        }

        [Test]
        public void Visuales_HumanosUsanElOverrideYSeMarcanComoJugador()
        {
            var ov = Cargar(HumanoJugadorT53.RutaOverride);
            foreach (string nombre in HumanoJugadorT53.Visuales)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoJugadorT53.CarpetaVisuales + nombre + ".prefab");
                Assert.IsNotNull(prefab, nombre);
                Animator a = prefab.GetComponentInChildren<Animator>(true);
                Assert.AreSame(ov, a.runtimeAnimatorController, nombre);
                Assert.IsTrue(prefab.GetComponent<VisualPersonaje>().ComoJugador, nombre);
            }
        }
    }
}

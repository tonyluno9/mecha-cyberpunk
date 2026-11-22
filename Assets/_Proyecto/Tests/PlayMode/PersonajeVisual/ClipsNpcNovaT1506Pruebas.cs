using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.PersonajeVisual
{
    /// <summary>
    /// T-1506: los NPC y los enemigos de Nova juegan sus clips nuevos según lo que les pasa: el civil herido usa NPC_Balazo_*, el que camina según su tipo (mayor con bastón,
    /// celular, mujer), el E15 su impacto, el E16 su muerte y el perro robot muerde con E17_Morder y muere con E17_Muerte.
    /// </summary>
    public class ClipsNpcNovaT1506Pruebas
    {
        private const string CarpetaVisuales = "Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_";
        private readonly List<GameObject> creados = new List<GameObject>();

        [TearDown]
        public void Limpiar()
        {
            foreach (GameObject g in creados)
            {
                if (g != null) { Object.Destroy(g); }
            }
            creados.Clear();
        }

        private VisualPersonaje Crear(string visual)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CarpetaVisuales + visual + ".prefab");
            Assert.IsNotNull(prefab, visual);
            GameObject go = Object.Instantiate(prefab, new Vector3(0f, 0f, 30f), Quaternion.identity);
            creados.Add(go);
            VisualPersonaje vp = go.GetComponent<VisualPersonaje>();
            vp.LeerDelPadre = false;
            return vp;
        }

        private static bool Reproduce(Animator a, int capa, string clip)
        {
            foreach (AnimatorClipInfo c in a.GetCurrentAnimatorClipInfo(capa)) { if (c.clip != null && c.clip.name == clip) { return true; } }
            foreach (AnimatorClipInfo c in a.GetNextAnimatorClipInfo(capa)) { if (c.clip != null && c.clip.name == clip) { return true; } }
            return false;
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, string que, float segundos = 3f)
        {
            float fin = Time.time + segundos;
            while (Time.time < fin && !condicion())
            {
                yield return null;
            }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator CivilHerido_DeFrente_JuegaNPCBalazoFrente_YDeEspalda_NPCBalazoEspalda()
        {
            VisualPersonaje civil = Crear("NP07_Pescador_a");
            yield return null;
            civil.Herir(-civil.transform.forward);
            yield return Hasta(() => Reproduce(civil.Animador, 2, "NPC_Balazo_Frente"), "NPC_Balazo_Frente");
            civil.CortarReaccion();
            yield return new WaitForSeconds(0.8f);
            civil.Herir(civil.transform.forward);
            yield return Hasta(() => Reproduce(civil.Animador, 2, "NPC_Balazo_Espalda"), "NPC_Balazo_Espalda");
        }

        [UnityTest]
        public IEnumerator CivilMuerto_CaeConLaMuerteDeNPC()
        {
            VisualPersonaje civil = Crear("NP10_Elotero_a");
            yield return null;
            civil.RegistrarGolpe(-civil.transform.forward);
            civil.Caido = true;
            yield return Hasta(() => Reproduce(civil.Animador, 0, "PJ01_Muerte_Frente") || Reproduce(civil.Animador, 0, "PJ01_Muerte_Espaldas") || Reproduce(civil.Animador, 0, "PJ01_Muerte_Desmayo"),
                "PJ01_Muerte_* o el desmayo (decisión de Tony 21-nov: el NPC muere como el jugador)");
        }

        [UnityTest]
        public IEnumerator Peaton_CaminaSegunSuTipo_Mayor_Celular_Mujer_Hombre()
        {
            foreach ((string visual, string clip, float velocidad) in new[]
            {
                ("NP12_Abuela_a", "NPC_Caminar_Mayor_Baston", 0.6f), ("NP08_Turista_a", "NPC_Caminar_Celular", 1.2f),
                ("NP14_Oficinista_a", "NPC_Caminar_Mujer", 1.3f), ("NP07_Pescador_a", "NPC_Caminar_Hombre", 1.4f),
            })
            {
                VisualPersonaje p = Crear(visual);
                p.Velocidad = velocidad;
                yield return Hasta(() => Reproduce(p.Animador, 0, clip), visual + " camina con " + clip);
            }
        }

        [UnityTest]
        public IEnumerator Peaton_Quieto_JuegaEsperar_YHuyendoCorreConLosBrazosDesordenados()
        {
            VisualPersonaje p = Crear("NP11_Estibador_a");
            yield return Hasta(() => Reproduce(p.Animador, 0, "NPC_Esperar"), "NPC_Esperar quieto");
            p.Velocidad = 4f;
            yield return Hasta(() => Reproduce(p.Animador, 0, "NPC_Huir_Corriendo"), "NPC_Huir_Corriendo", 4f);
        }

        [UnityTest]
        public IEnumerator E15_Herido_JuegaE15Impacto()
        {
            VisualPersonaje e15 = Crear("E15_CentinelaNova_a");
            yield return null;
            e15.Herir(-e15.transform.forward);
            yield return Hasta(() => Reproduce(e15.Animador, 2, "E15_Impacto"), "E15_Impacto");
            // La caída de los robots de Nova le gustó a Tony: su muerte se queda con el clip de antes (sin E15_Muerte).
        }

        [UnityTest]
        public IEnumerator E16_Camina_YGolpea_ConSusClips()
        {
            VisualPersonaje e16 = Crear("E16_RobotPesadoNova_a");
            e16.Velocidad = 0.8f;
            yield return Hasta(() => Reproduce(e16.Animador, 0, "E16_Caminar"), "E16_Caminar");
            e16.Velocidad = 0f;
            e16.Golpear(false);
            yield return Hasta(() => Reproduce(e16.Animador, 1, "E16_Golpe_Escudo"), "E16_Golpe_Escudo");
            // La caída de los robots de Nova le gustó a Tony: su muerte se queda con el clip de antes (sin E16_Muerte).
        }

        [UnityTest]
        public IEnumerator E16_ConElEscudoArriba_CaminaConE16CaminarEscudo_YSinEscudoConE16Caminar()
        {
            // T-1553 (decisión de Tony 20-nov): el canal ConImplante del controlador base es "escudo arriba" en el E16.
            VisualPersonaje e16 = Crear("E16_RobotPesadoNova_a");
            e16.Velocidad = 0.8f;
            yield return Hasta(() => Reproduce(e16.Animador, 0, "E16_Caminar"), "E16_Caminar sin escudo");
            Assert.IsFalse(Reproduce(e16.Animador, 0, "E16_CaminarEscudo"));
            e16.EscudoArriba = true;
            yield return Hasta(() => e16.PesoEscudo >= 0.99f && Reproduce(e16.Animador, 0, "E16_CaminarEscudo"), "E16_CaminarEscudo con el escudo arriba");
            Assert.AreEqual(1f, e16.PesoEscudo, 0.01f);
            e16.EscudoArriba = false;
            yield return Hasta(() => e16.PesoEscudo < 0.01f && !Reproduce(e16.Animador, 0, "E16_CaminarEscudo"), "vuelve a E16_Caminar");
            Assert.IsTrue(Reproduce(e16.Animador, 0, "E16_Caminar"));
        }

        [UnityTest]
        public IEnumerator E18_Camina_Corre_YGolpea_ConSusClips()
        {
            // T-1553: el E18 no lleva arma y pelea con sus propios clips (caminar 1.62 m/s, correr 4.53 m/s, golpe de derecha).
            VisualPersonaje e18 = Crear("E18_EliteNova_a");
            e18.Velocidad = 1.62f;
            yield return Hasta(() => Reproduce(e18.Animador, 0, "E18_Caminar"), "E18_Caminar");
            e18.Velocidad = 4.53f;
            yield return Hasta(() => Reproduce(e18.Animador, 0, "E18_Correr"), "E18_Correr", 4f);
            e18.Velocidad = 0f;
            e18.Golpear(false);
            yield return Hasta(() => Reproduce(e18.Animador, 1, "E18_Golpe_Der"), "E18_Golpe_Der");
        }

        [UnityTest]
        [Ignore("Pendiente: los clips E17_* de Blender son del rig a escala real (escala_real/E17); el perro del juego sigue con su modelo y sus clips Perro_*")]
        public IEnumerator PerroRobot_Corre_Muerde_YMuere_ConLosClipsDeE17()
        {
            VisualPersonaje perro = Crear("E17_PerroRobotNova_a");
            perro.Velocidad = 4f;
            yield return Hasta(() => Reproduce(perro.Animador, 0, "E17_Correr"), "E17_Correr");
            perro.Velocidad = 0f;
            perro.Golpear(false);
            yield return Hasta(() => Reproduce(perro.Animador, 0, "E17_Morder") || Reproduce(perro.Animador, 1, "E17_Morder"), "E17_Morder");
            perro.Caido = true;
            yield return Hasta(() => Reproduce(perro.Animador, 0, "E17_Muerte"), "E17_Muerte");
        }

        [UnityTest]
        public IEnumerator Mesera_CaminaConLaCharola()
        {
            VisualPersonaje mesera = Crear("MeseraRobot");
            mesera.Velocidad = 1.2f;
            yield return Hasta(() => Reproduce(mesera.Animador, 0, "NPC_Mesera_Charola"), "NPC_Mesera_Charola");
        }

        [UnityTest]
        public IEnumerator Hacker_JuegaElClipSentado_YSigueMirandoAlJugador()
        {
            VisualPersonaje hacker = Crear("Hacker");
            yield return Hasta(() => Reproduce(hacker.Animador, 0, "NPC_Hacker_Tecleando"), "NPC_Hacker_Tecleando");
            var ik = hacker.Animador.GetComponent<IKPiesYMirada>();
            Assert.IsNotNull(ik, "conserva el IK de mirada");
            Assert.IsTrue(ik.MirarAlJugador, "el hacker te voltea a ver");
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.IA.APie;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-624 (T-1004: con los clips de PJ01): SKULLS, su jefe y los guardias de Nova juegan sus clips (idle, burla, ataque, muerte, presumir) según lo que les pasa.</summary>
    public class EnemigosAnim2Pruebas : BaseJugadorAPie
    {
        private readonly List<GameObject> creados = new List<GameObject>();

        public override void Setup()
        {
            EventosRuido.LimpiarSuscriptores();
            base.Setup();
        }

        public override void TearDown()
        {
            foreach (GameObject c in creados) { if (c != null) Object.Destroy(c); }
            creados.Clear();
            EventosRuido.LimpiarSuscriptores();
            base.TearDown();
        }

        private GameObject Crear(string ruta, Vector3 posicion)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(prefab, ruta);
            GameObject go = Object.Instantiate(prefab, posicion, Quaternion.LookRotation(Vector3.back));
            creados.Add(go);
            return go;
        }

        // T-1004: los clips son de PJ01; un nombre con * al final es un prefijo (p. ej. PJ01_Muerte_*).
        private static bool Coincide(string nombre, string clip) => clip.EndsWith("*") ? nombre.StartsWith(clip.TrimEnd('*')) : nombre == clip;

        private static bool Reproduce(Animator a, int capa, string clip)
        {
            foreach (AnimatorClipInfo c in a.GetCurrentAnimatorClipInfo(capa)) { if (c.clip != null && Coincide(c.clip.name, clip)) return true; }
            foreach (AnimatorClipInfo c in a.GetNextAnimatorClipInfo(capa)) { if (c.clip != null && Coincide(c.clip.name, clip)) return true; }
            return false;
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, string que, float limite = 4f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin) { yield return null; }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        [UnityTest]
        public IEnumerator Skull_ParadoJuegaSkullIdle_YSeBurlaAlAlertarse()
        {
            yield return Calentar();
            GameObject skull = Crear("Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab", new Vector3(8f, 0f, 8f));
            var pandillero = skull.GetComponent<PandilleroAPie>();
            VisualPersonaje visual = skull.GetComponentInChildren<VisualPersonaje>(true);
            Assert.IsNotNull(visual, "El skull tiene visual");
            Animator a = visual.Animador;
            yield return Hasta(() => Reproduce(a, 0, "PJ01_Loco_Idle"), "Skull_Idle parado");
            pandillero.Alerta = true;
            yield return Hasta(() => Reproduce(a, 0, "PJ01_Burla"), "Skull_Burla al verte"); // Decisión de Tony 21-nov: la burla de todos es PJ01_Burla (antes Skull_Burla de Mixamo)
        }

        [UnityTest]
        public IEnumerator Skull_CaminandoJuegaSkullCaminar()
        {
            yield return Calentar();
            GameObject go = Crear("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_SkullHombre.prefab", new Vector3(8f, 0f, 8f));
            VisualPersonaje visual = go.GetComponent<VisualPersonaje>();
            visual.LeerDelPadre = false;
            visual.Velocidad = 1.3f;
            yield return Hasta(() => Reproduce(visual.Animador, 0, "PJ01_Caminar_SinImplante"), "PJ01_Caminar_SinImplante");
        }

        [UnityTest]
        public IEnumerator Jefe_IdleBurlaAtaqueYMuerte_ConSusClips()
        {
            yield return Calentar();
            GameObject jefe = Crear("Assets/_Proyecto/IA/Prefabs/PandilleroArmado_JefeSkulls.prefab", new Vector3(8f, 0f, 8f));
            VisualPersonaje visual = jefe.GetComponentInChildren<VisualPersonaje>(true);
            Animator a = visual.Animador;
            Assert.AreEqual("Override_Jefe", a.runtimeAnimatorController.name);
            yield return Hasta(() => Reproduce(a, 0, "PJ01_Loco_Idle"), "PJ01_Loco_Idle");
            jefe.GetComponent<PandilleroAPie>().Alerta = true;
            yield return Hasta(() => Reproduce(a, 0, "PJ01_Burla"), "Jefe_Burla de entrada");
            yield return Hasta(() => !Reproduce(a, 0, "PJ01_Burla"), "termina la burla", 8f);
            visual.TipoArma = TipoArmaVisual.Pistola;
            yield return Esperar(0.4f);
            visual.Disparar();
            yield return Hasta(() => Reproduce(a, 1, "PJ01_Pistola_Disparo"), "Jefe_Ataque al atacar");
            visual.Caido = true;
            yield return Hasta(() => Reproduce(a, 0, "PJ01_Muerte_*"), "PJ01_Muerte_*");
        }

        [UnityTest]
        public IEnumerator GuardiaNova_VigilaPresumeGolpeaYMuere_ConClipsDeNova()
        {
            yield return Calentar();
            GameObject guardia = Crear("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_GuardiaNova.prefab", new Vector3(0f, 0f, 6f));
            GenteDeBanqueta gente = guardia.GetComponent<GenteDeBanqueta>() ?? guardia.AddComponent<GenteDeBanqueta>();
            var nova = guardia.GetComponent<GuardiaNova>() ?? guardia.AddComponent<GuardiaNova>();
            nova.ConfigurarGestos(1f, 0.5f);
            VisualPersonaje visual = guardia.GetComponent<VisualPersonaje>();
            Animator a = visual.Animador;
            Assert.AreEqual("Override_Guardias", a.runtimeAnimatorController.name);
            yield return Hasta(() => Reproduce(a, 0, "PJ01_Loco_Idle"), "Nova_Idle vigilando");

            yield return Hasta(() => Reproduce(a, 0, "PJ01_Presumir"), "PJ01_Presumir al presumir (decisión de Tony 21-nov: Nova_Flexing -> PJ01_Presumir)", 8f);

            // Alerta con el jugador encima: golpes y barrida.
            guardia.transform.position = new Vector3(0f, 0f, 1.5f);
            EventosRuido.PublicarDisparo(guardia.transform.position);
            yield return Hasta(() => nova.GolpesDados >= 1, "golpe de amenaza", 4f);
            yield return Hasta(() => Reproduce(a, 1, "PJ01_Punos_Golpe*"), "PJ01_Punos_Golpe*");
            yield return Hasta(() => nova.GolpesDados >= 3, "tercer golpe", 6f);
            yield return Hasta(() => Reproduce(a, 1, "PJ01_Punos_Fuerte"), "PJ01_Punos_Fuerte", 2f);

            visual.Caido = true;
            yield return Hasta(() => Reproduce(a, 0, "Guardia_Muerte"), "Guardia_Muerte"); // T-1506
        }

        [UnityTest]
        public IEnumerator GuardiaNova_CorriendoJuegaNovaCorrer()
        {
            yield return Calentar();
            GameObject go = Crear("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_GuardiaNova.prefab", new Vector3(8f, 0f, 8f));
            VisualPersonaje visual = go.GetComponent<VisualPersonaje>();
            visual.LeerDelPadre = false;
            visual.Velocidad = 6f;
            yield return Hasta(() => Reproduce(visual.Animador, 0, "PJ01_Correr_SinImplante"), "PJ01_Correr_SinImplante");
        }
    
        private Camera CamaraFrente(GameObject objetivo, float distancia = 3.2f)
        {
            var cam = new GameObject("CamExterna").AddComponent<Camera>();
            creados.Add(cam.gameObject);
            Vector3 centro = objetivo.transform.position + Vector3.up * 1f;
            cam.transform.position = centro - objetivo.transform.forward * distancia + Vector3.up * 0.4f + objetivo.transform.right * 0.8f;
            cam.transform.LookAt(centro);
            return cam;
        }

        [UnityTest]
        public IEnumerator Capturas_SkullBurla_Jefe_NovaGolpea()
        {
            yield return Calentar();
            GameObject skull = Crear("Assets/_Proyecto/IA/Prefabs/PandilleroArmado_Skulls.prefab", new Vector3(20f, 0f, 20f));
            yield return Esperar(0.5f);
            skull.GetComponent<PandilleroAPie>().Alerta = true;
            Animator aSkull = skull.GetComponentInChildren<VisualPersonaje>(true).Animador;
            yield return Hasta(() => Reproduce(aSkull, 0, "PJ01_Burla"), "burla");
            yield return Esperar(0.6f);
            Debug.Log("[T624] skull base=" + aSkull.GetCurrentAnimatorClipInfo(0)[0].clip.name + " w=" + aSkull.GetLayerWeight(1));
            yield return AnimacionCarroPruebas.Capturar(CamaraFrente(skull), "docs/capturas/anim2-skull-burla.png");

            GameObject jefe = Crear("Assets/_Proyecto/IA/Prefabs/PandilleroArmado_JefeSkulls.prefab", new Vector3(40f, 0f, 20f));
            yield return Esperar(0.5f);
            jefe.GetComponent<PandilleroAPie>().Alerta = true;
            Animator aJefe = jefe.GetComponentInChildren<VisualPersonaje>(true).Animador;
            yield return Hasta(() => Reproduce(aJefe, 0, "PJ01_Burla"), "burla jefe");
            yield return Esperar(0.6f);
            yield return AnimacionCarroPruebas.Capturar(CamaraFrente(jefe, 3.8f), "docs/capturas/anim2-jefe-burla.png");

            GameObject guardia = Crear("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_GuardiaNova.prefab", new Vector3(0f, 0f, 1.5f));
            guardia.transform.rotation = Quaternion.LookRotation(Vector3.back);
            var nova = guardia.GetComponent<GuardiaNova>() ?? guardia.AddComponent<GuardiaNova>();
            nova.ConfigurarGestos(60f, 0.5f);
            EventosRuido.PublicarDisparo(guardia.transform.position);
            float limite = Time.time + 4f;
            while (nova.GolpesDados < 1 && Time.time < limite) { yield return null; }
            yield return Esperar(0.25f);
            Camera cam = CamaraFrente(guardia);
            cam.transform.position = guardia.transform.position + new Vector3(3f, 1.4f, 0.6f);
            cam.transform.LookAt(guardia.transform.position + Vector3.up * 1f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/anim2-nova-golpe.png");
        }
    }
}

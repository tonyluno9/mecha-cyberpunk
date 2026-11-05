using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-623: abrir puerta, botón, recoger, sacar el teléfono y el NPC con el celular cambian el estado del Animator.</summary>
    public class GestosInteraccionPruebas : BaseJugadorAPie
    {
        private readonly List<GameObject> creados = new List<GameObject>();
        private VisualPersonaje visual;
        private GestosJugador gestos;

        public override void Setup()
        {
            LocalesDePrueba.LimpiarGlobales();
            base.Setup();
            visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            gestos = jugador.GetComponent<GestosJugador>();
        }

        public override void TearDown()
        {
            foreach (GameObject c in creados) { if (c != null) Object.Destroy(c); }
            creados.Clear();
            base.TearDown();
            LocalesDePrueba.LimpiarGlobales();
        }

        private static bool EnEstado(Animator a, int capa, string nombre)
        {
            AnimatorStateInfo actual = a.GetCurrentAnimatorStateInfo(capa);
            AnimatorStateInfo siguiente = a.GetNextAnimatorStateInfo(capa);
            return a.IsInTransition(capa) ? siguiente.IsName(nombre) || actual.IsName(nombre) : actual.IsName(nombre);
        }

        private static IEnumerator Hasta(System.Func<bool> condicion, string que, float limite = 4f)
        {
            float fin = Time.time + limite;
            while (!condicion() && Time.time < fin) { yield return null; }
            Assert.IsTrue(condicion(), "No llegó: " + que);
        }

        private IEnumerator PresionarE()
        {
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Puerta_AlAbrirConE_JuegaInterAbrirPuerta()
        {
            yield return Calentar();
            Assert.IsNotNull(gestos, "JugadorAPie agrega GestosJugador");
            creados.Add(LocalesDePrueba.Crear(IdLugar.Bar, new Vector3(0f, 0f, 1.5f), new HorarioLugar(20f, 6f), false, true));
            yield return null;
            yield return PresionarE();
            Assert.AreEqual(GestoVisual.AbrirPuerta, gestos.UltimoGesto);
            yield return Hasta(() => EnEstado(visual.Animador, 0, "AbrirPuerta"), "estado AbrirPuerta");
            yield return Hasta(() => visual.PesoCapaArriba < 0.1f, "el torso se apaga durante el gesto");
            yield return Hasta(() => EnEstado(visual.Animador, 0, "Idle"), "vuelve a Idle", 6f);
        }

        [UnityTest]
        public IEnumerator Interactuable_ConE_JuegaInterBoton()
        {
            yield return Calentar();
            var go = new GameObject("Boton");
            go.transform.position = new Vector3(0f, 0f, 1.5f);
            go.AddComponent<InteractuableDePrueba>();
            creados.Add(go);
            yield return null;
            yield return PresionarE();
            Assert.AreEqual(GestoVisual.Boton, gestos.UltimoGesto);
            yield return Hasta(() => EnEstado(visual.Animador, 0, "Boton"), "estado Boton");
        }

        [UnityTest]
        public IEnumerator ArmaEnPiso_ConE_JuegaInterRecoger()
        {
            yield return Calentar();
            var go = new GameObject("PistolaEnPiso");
            go.transform.position = new Vector3(0f, 0.1f, 1.5f);
            GameObject pieza = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pieza.transform.SetParent(go.transform, false);
            go.AddComponent<ArmaEnPiso>();
            creados.Add(go);
            yield return null;
            yield return PresionarE();
            Assert.AreEqual(GestoVisual.Recoger, gestos.UltimoGesto);
            yield return Hasta(() => EnEstado(visual.Animador, 0, "Recoger"), "estado Recoger");
        }

        [UnityTest]
        public IEnumerator SacarTelefono_JuegaInterTelefono()
        {
            yield return Calentar();
            gestos.SacarTelefono();
            yield return Hasta(() => EnEstado(visual.Animador, 0, "Telefono"), "estado Telefono");
        }

        [UnityTest]
        public IEnumerator NPC_EnModoCelular_VeElTelefonoEnBucle()
        {
            yield return Calentar();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Personaje/Visual/Prefabs/Visual_Doctor.prefab");
            GameObject npc = Object.Instantiate(prefab, new Vector3(3f, 0f, 3f), Quaternion.identity);
            creados.Add(npc);
            GenteDeBanqueta gente = npc.GetComponent<GenteDeBanqueta>() ?? npc.AddComponent<GenteDeBanqueta>();
            gente.Configurar(ModoGente.Celular, 0.3f, 1f, null, Vector3.zero, Vector3.zero, 1f, 1f, npc.transform);
            Animator a = npc.GetComponent<VisualPersonaje>().Animador;
            yield return Hasta(() => EnEstado(a, 0, "VerTelefono"), "estado VerTelefono");
            yield return Esperar(2f);
            Assert.IsTrue(EnEstado(a, 0, "VerTelefono"), "Sigue viendo el celular (bucle)");
        }
    
        [UnityTest]
        public IEnumerator Captura_Recoger()
        {
            yield return Calentar();
            gestos.Gestear(GestoVisual.Recoger);
            var cam = new GameObject("CamExterna").AddComponent<Camera>();
            creados.Add(cam.gameObject);
            cam.transform.position = jugador.transform.position + new Vector3(-1.5f, 1.2f, 2.4f);
            cam.transform.LookAt(jugador.transform.position + Vector3.up * 0.9f);
            yield return Esperar(1.7f);
            yield return AnimacionCarroPruebas.Capturar(cam, "docs/capturas/anim2-recoger.png");
        }
    }
}

using System.Collections;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-964 (A5): el gesto del teléfono del jugador usa el tramo útil de PJ01_Inter_Telefono (7.2 s de 37): avisa al llevarlo al oído y vuelve a Idle.</summary>
    public class TelefonoPJ01T964Pruebas : BaseJugadorAPie
    {
        [UnityTest]
        public IEnumerator SacarTelefono_LlegaAlOido_YVuelveAIdleEnMenosDeNueveSegundos()
        {
            yield return Calentar();
            VisualPersonaje visual = jugador.GetComponentInChildren<VisualPersonaje>(true);
            Animator a = visual.Animador;
            a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            int antes = visual.VecesTelefonoEnOreja;
            GestosJugador gestos = jugador.GetComponent<GestosJugador>();
            Assert.IsNotNull(gestos, "el jugador tiene GestosJugador");
            float inicio = Time.time;
            gestos.SacarTelefono();
            float fin = Time.time + 3f;
            while (!a.GetCurrentAnimatorStateInfo(0).IsName("Telefono") && Time.time < fin) { yield return null; }
            Assert.IsTrue(a.GetCurrentAnimatorStateInfo(0).IsName("Telefono"), "juega el gesto del teléfono");
            AnimatorClipInfo[] clips = a.GetCurrentAnimatorClipInfo(0);
            Assert.AreEqual("PJ01_Inter_Telefono", clips[clips.Length - 1].clip.name);
            Assert.Less(clips[clips.Length - 1].clip.length, 8f, "recortado al tramo útil");
            fin = Time.time + 9f;
            while (!a.GetCurrentAnimatorStateInfo(0).IsName("Idle") && Time.time < fin) { yield return null; }
            Assert.AreEqual(antes + 1, visual.VecesTelefonoEnOreja, "el evento avisa que llegó al oído");
            Assert.IsTrue(a.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "vuelve a Idle");
            Debug.Log("[Prueba] T-964 teléfono: de Idle a Idle en " + (Time.time - inicio).ToString("F1") + " s.");
        }
    }
}

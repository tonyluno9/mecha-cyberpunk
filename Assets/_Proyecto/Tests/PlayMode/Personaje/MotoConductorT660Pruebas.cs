using System.Collections;
using System.Collections.Generic;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.Personaje.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-660: en la moto el conductor queda sentado ENCIMA (cadera en el asiento), con las manos en el manubrio y los pies en los estribos,
    /// y la moto no lleva faros ni calaveras.
    /// </summary>
    public class MotoConductorT660Pruebas : BaseJugadorAPie
    {
        private const float MaxDistanciaPunto = 0.3f;
        private const float MaxDistanciaCadera = 0.2f;
        private const float AlturaMinimaCadera = 0.5f;
        private readonly List<GameObject> motos = new List<GameObject>();

        public override void TearDown()
        {
            foreach (GameObject m in motos) { if (m != null) { Object.Destroy(m); } }
            motos.Clear();
            base.TearDown();
        }

        private IEnumerator Montar(string codigo, System.Action<GameObject, IMontable, PuntosMoto> alTerminar)
        {
            yield return Calentar();
            // En el modo batch no hay cámara que vea al conductor y el Animator se apaga; así se mide la pose real.
            jugador.GetComponentInChildren<VisualPersonaje>(true).Animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            GameObject moto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Proyecto/Vehiculos/Prefabs/Carro_" + codigo + ".prefab"),
                new Vector3(0f, 0.3f, 6f), Quaternion.identity);
            motos.Add(moto);
            IMontable montable = null;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.gameObject == moto) { montable = m; }
            }
            Assert.IsNotNull(montable, "La moto se registra");
            PuntosMoto puntos = moto.GetComponent<PuntosMoto>();
            Assert.IsNotNull(puntos, "La moto trae sus puntos de conductor");
            Assert.IsTrue(puntos.Completo);
            yield return Esperar(1f);
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.8f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            yield return Esperar(0.3f);
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Sube a la moto");
            yield return Esperar(1.2f);
            alTerminar(moto, montable, puntos);
        }

        private void Medir(string codigo, GameObject moto, PuntosMoto puntos, out float cadera, out float manoIzq, out float manoDer, out float pieIzq, out float pieDer, out float alturaCadera)
        {
            Animator a = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            Vector3 hips = a.GetBoneTransform(HumanBodyBones.Hips).position;
            cadera = Vector3.Distance(hips, puntos.Asiento.position);
            alturaCadera = hips.y - moto.transform.position.y;
            Transform m = moto.transform;
            manoIzq = Vector3.Distance(m.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.LeftHand).position), m.InverseTransformPoint(puntos.ManubrioIzquierdo.position));
            manoDer = Vector3.Distance(m.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.RightHand).position), m.InverseTransformPoint(puntos.ManubrioDerecho.position));
            pieIzq = Vector3.Distance(m.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.LeftFoot).position), m.InverseTransformPoint(puntos.EstriboIzquierdo.position));
            pieDer = Vector3.Distance(m.InverseTransformPoint(a.GetBoneTransform(HumanBodyBones.RightFoot).position), m.InverseTransformPoint(puntos.EstriboDerecho.position));
        }

        private IEnumerator ProbarMoto(string codigo)
        {
            GameObject moto = null;
            PuntosMoto puntos = null;
            yield return Montar(codigo, (m, mo, p) => { moto = m; puntos = p; });
            yield return null;
            yield return null;
            float cadera = 0f, manoIzq = 0f, manoDer = 0f, pieIzq = 0f, pieDer = 0f, altura = 0f;
            yield return null;
            yield return null;
            Medir(codigo, moto, puntos, out cadera, out manoIzq, out manoDer, out pieIzq, out pieDer, out altura);
            string resumen = codigo + ": cadera " + cadera.ToString("F2") + " m del asiento, a " + altura.ToString("F2") + " m sobre el piso; manos " + manoIzq.ToString("F2") + "/"
                + manoDer.ToString("F2") + "; pies " + pieIzq.ToString("F2") + "/" + pieDer.ToString("F2");
            System.IO.File.AppendAllText("Logs/t660-medidas.txt", resumen + "\n");
            Vector3 centro = moto.transform.position + Vector3.up * 0.9f;
            CapturaMundoS1.Foto("s1-moto-" + codigo + "-lado", centro + moto.transform.right * 3.2f, centro, 35f);
            CapturaMundoS1.Foto("s1-moto-" + codigo + "-tres-cuartos", centro + moto.transform.right * 2.2f + moto.transform.forward * 2.2f + Vector3.up * 0.4f, centro, 40f);
            Animator an = jugador.GetComponentInChildren<VisualPersonaje>(true).Animador;
            PoseMoto pm = an.GetComponent<PoseMoto>();
            System.IO.File.AppendAllText("Logs/t660-medidas.txt", "  objetivos (locales de la moto): manoIzq " + moto.transform.InverseTransformPoint(puntos.ManubrioIzquierdo.position).ToString("F2")
                + " manoDer " + moto.transform.InverseTransformPoint(puntos.ManubrioDerecho.position).ToString("F2") + " pieIzq " + moto.transform.InverseTransformPoint(puntos.EstriboIzquierdo.position).ToString("F2")
                + " huesos: manoIzq " + moto.transform.InverseTransformPoint(an.GetBoneTransform(HumanBodyBones.LeftHand).position).ToString("F2")
                + " manoDer " + moto.transform.InverseTransformPoint(an.GetBoneTransform(HumanBodyBones.RightHand).position).ToString("F2")
                + " pieIzq " + moto.transform.InverseTransformPoint(an.GetBoneTransform(HumanBodyBones.LeftFoot).position).ToString("F2") + "\n");
            System.IO.File.AppendAllText("Logs/t660-medidas.txt", "  ik: avance " + pm.Avance + " pesoManoIzq " + an.GetIKPositionWeight(AvatarIKGoal.LeftHand)
                + " pesoPieIzq " + an.GetIKPositionWeight(AvatarIKGoal.LeftFoot) + " cull " + an.cullingMode + " layers " + an.layerCount + "\n");
            Assert.Less(cadera, MaxDistanciaCadera, "La cadera queda en el asiento. " + resumen);
            Assert.Greater(altura, AlturaMinimaCadera, "Sentado encima de la moto, no hundido. " + resumen);
            Assert.Less(manoIzq, MaxDistanciaPunto, "Mano izquierda en el manubrio. " + resumen);
            Assert.Less(manoDer, MaxDistanciaPunto, "Mano derecha en el manubrio. " + resumen);
            Assert.Less(pieIzq, MaxDistanciaPunto, "Pie izquierdo en el estribo. " + resumen);
            Assert.Less(pieDer, MaxDistanciaPunto, "Pie derecho en el estribo. " + resumen);

        }

        [UnityTest] public IEnumerator Montar_MotoV07_ElConductorQuedaSentadoEncima() { yield return ProbarMoto("V07"); }

        [UnityTest] public IEnumerator Montar_MotoV08_ElConductorQuedaSentadoEncima() { yield return ProbarMoto("V08"); }

        [UnityTest] public IEnumerator Montar_MotoC19_ElConductorQuedaSentadoEncima() { yield return ProbarMoto("C19"); }

        [UnityTest]
        public IEnumerator Bajar_DeLaMoto_ElPersonajeVuelveAPieYSeVeEntero()
        {
            GameObject moto = null;
            IMontable montable = null;
            yield return Montar("V07", (m, mo, p) => { moto = m; montable = mo; });
            AnimacionCarro anim = jugador.GetComponent<AnimacionCarro>();
            Assert.AreEqual(AnimacionCarro.FaseCarro.Dentro, anim.Fase);
            Press(teclado.eKey);
            yield return null;
            Release(teclado.eKey);
            Assert.AreEqual(ModoJugador.APie, jugadorAPie.Modo, "La E baja de la moto");
            float limite = Time.time + 3f;
            while (anim.Fase != AnimacionCarro.FaseCarro.Fuera && Time.time < limite) { yield return null; }
            Assert.AreEqual(AnimacionCarro.FaseCarro.Fuera, anim.Fase);
            Transform visual = jugador.GetComponentInChildren<VisualPersonaje>(true).transform;
            Assert.Less(Vector3.Distance(visual.position, jugador.transform.position), 0.05f, "El visual vuelve a los pies del jugador");
            Assert.IsFalse(montable.Ocupado);
        }
    }
}

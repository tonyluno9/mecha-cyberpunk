using System.Collections;
using System.Linq;
using MechaCyberpunk.VR;
using MechaCyberpunk.VR.Manos;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR40: las manos nuevas MN01. Los 2 FBX existen con sus 17 huesos y 9 clips (tras ImportarManosVR40.Aplicar); las reglas de pose; y en el
    /// rig las manos MN01 reemplazan al puño de PJ01: el grip cierra la mano y el gatillo dobla solo el índice.
    /// </summary>
    public class VR40ManosMN01Pruebas : BaseJugadorVR
    {
        private const string FbxDerecha = "Assets/Arte/Personajes/PJ01/ManosVR/MN01_ManosVR_R.fbx";
        private const string FbxIzquierda = "Assets/Arte/Personajes/PJ01/ManosVR/MN01_ManosVR_L.fbx";
        private const float Tolerancia = 2f;

        [TestCase(FbxDerecha, true)]
        [TestCase(FbxIzquierda, false)]
        public void Fbx_Tiene17HuesosY9Clips(string ruta, bool derecha)
        {
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            Assert.IsNotNull(modelo, "Falta " + ruta);
            string[] nombres = modelo.GetComponentsInChildren<Transform>(true).Select(t => t.name).ToArray();
            string[] huesos = ReglaManosMN01VR.Huesos(derecha);
            Assert.AreEqual(17, huesos.Length);
            foreach (string h in huesos)
            {
                CollectionAssert.Contains(nombres, h, "El FBX debe traer el hueso " + h);
            }
            string[] clips = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__"))
                .Select(c => ReglaManosMN01VR.NombreLimpio(c.name)).ToArray();
            Assert.AreEqual(9, ReglaManosMN01VR.Clips.Length);
            foreach (string c in ReglaManosMN01VR.Clips)
            {
                CollectionAssert.Contains(clips, c, "El FBX debe traer el clip " + c);
            }
            var importador = (ModelImporter)AssetImporter.GetAtPath(ruta);
            Assert.AreEqual(ModelImporterAnimationType.Generic, importador.animationType, "Rig Generic (corre ImportarManosVR40.Aplicar).");
            ModelImporterClipAnimation[] ajustes = importador.clipAnimations;
            Assert.IsTrue(ajustes.Any(a => a.name == ReglaManosMN01VR.ClipAbrirCerrar && a.loopTime), "Loop Time en Mano_AbrirCerrar.");
            Assert.IsFalse(ajustes.Any(a => a.name != ReglaManosMN01VR.ClipAbrirCerrar && a.loopTime), "Solo Mano_AbrirCerrar va en bucle.");
        }

        [Test]
        public void Elegir_PosePorLoQueHaceCadaMano()
        {
            Assert.AreEqual(PoseManoVR.Libre, ReglaManosMN01VR.Elegir(true, false, false, false, false, false, false));
            Assert.AreEqual(PoseManoVR.Pistola, ReglaManosMN01VR.Elegir(true, false, false, true, false, false, false));
            Assert.AreEqual(PoseManoVR.Pistola, ReglaManosMN01VR.Elegir(true, false, false, true, true, false, false), "Con arma larga la derecha empuña.");
            Assert.AreEqual(PoseManoVR.Granada, ReglaManosMN01VR.Elegir(true, false, false, false, false, true, false));
            Assert.AreEqual(PoseManoVR.Senalar, ReglaManosMN01VR.Elegir(true, true, false, true, false, false, false), "Con menú, señala.");
            Assert.AreEqual(PoseManoVR.ArmaLargaApoyo, ReglaManosMN01VR.Elegir(false, false, false, true, true, false, false));
            Assert.AreEqual(PoseManoVR.Libre, ReglaManosMN01VR.Elegir(false, false, false, true, false, false, false), "Con pistola la izquierda va libre.");
            Assert.AreEqual(PoseManoVR.Cargador, ReglaManosMN01VR.Elegir(false, false, false, true, true, false, true));
            Assert.AreEqual("Mano_Puno", ReglaManosMN01VR.NombreLimpio("MN01_ManosVR_R_rig|Mano_Puno"));
            Assert.IsTrue(ReglaManosMN01VR.EsIndice(5) && ReglaManosMN01VR.EsIndice(7));
            Assert.IsFalse(ReglaManosMN01VR.EsIndice(8));
            float min = float.MaxValue;
            float max = float.MinValue;
            for (float t = 0f; t < ReglaManosMN01VR.PeriodoPulso; t += 0.05f)
            {
                min = Mathf.Min(min, ReglaManosMN01VR.Pulso(t));
                max = Mathf.Max(max, ReglaManosMN01VR.Pulso(t));
            }
            Assert.Greater(min, 0f, "La prótesis siempre brilla algo.");
            Assert.Greater(max - min, 0.5f, "El brillo pulsa.");
        }

        [UnityTest]
        public IEnumerator ArmarVR_ManosMN01_GripCierraYGatilloDoblaSoloElIndice()
        {
            Assert.IsNotNull(Resources.Load<GameObject>(PunosVisiblesVR.RecursoManoDerecha), "Falta el prefab de la mano derecha (corre ImportarManosVR40.Aplicar).");
            Assert.IsNotNull(Resources.Load<GameObject>(PunosVisiblesVR.RecursoManoIzquierda), "Falta el prefab de la mano izquierda.");
            yield return ArmarVR();
            PunosVisiblesVR punos = jugador.GetComponent<PunosVisiblesVR>();
            Assert.IsNotNull(punos);
            for (int i = 0; i < 60 && !punos.UsaManosMN01; i++)
            {
                yield return null;
            }
            Assert.IsTrue(punos.UsaManosMN01, "Las manos visibles son las MN01.");
            ManoMN01VR izq = punos.ManoMN01(false);
            Assert.AreEqual(17, izq.CantidadHuesos);
            Assert.GreaterOrEqual(izq.CantidadPoses, 9);
            Assert.Less(Vector3.Distance(punos.PuntoNudillos(false), adaptador.ManoIzquierda.transform.position), 0.2f, "La mano queda en el control.");

            // Sin nada: abierta.
            yield return Segundos(0.3f);
            PoseManoMN01 abierta = izq.PoseDe(ReglaManosMN01VR.ClipAbierta);
            PoseManoMN01 puno = izq.PoseDe(ReglaManosMN01VR.ClipPuno);
            PoseManoMN01 gatillo = izq.PoseDe(ReglaManosMN01VR.ClipGatillo);
            Assert.Less(Quaternion.Angle(izq.Hueso(9).localRotation, abierta.Rotaciones[9]), Tolerancia, "Sin grip, el medio está abierto.");

            // Grip a fondo: puño.
            Set(controlIzq.grip, 1f);
            yield return Segundos(0.4f);
            Assert.Less(Quaternion.Angle(izq.Hueso(9).localRotation, puno.Rotaciones[9]), Tolerancia, "Con el grip, el medio se cierra como en Mano_Puno.");
            Set(controlIzq.grip, 0f);
            yield return Segundos(0.4f);

            // Solo gatillo: el índice se dobla y el medio sigue abierto.
            Set(controlIzq.trigger, 1f);
            yield return Segundos(0.4f);
            Assert.Less(Quaternion.Angle(izq.Hueso(6).localRotation, gatillo.Rotaciones[6]), Tolerancia, "El gatillo dobla el índice.");
            Assert.Less(Quaternion.Angle(izq.Hueso(9).localRotation, abierta.Rotaciones[9]), Tolerancia, "El gatillo no mueve el medio.");
            Set(controlIzq.trigger, 0f);
            yield return null;
        }
    }
}

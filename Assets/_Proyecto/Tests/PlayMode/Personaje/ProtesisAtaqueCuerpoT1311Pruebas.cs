using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Habilidades;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1311: las piezas de PJ01_Implantes_Ataque.fbx (brazo láser y hombrera de misiles) están en el cuerpo de PJ01 apagadas; se ven solo con la prótesis instalada y
    /// sus puntos de salida (vacíos del fbx) existen, pegados a su hueso. El rayo nace en la salida del láser.
    /// </summary>
    public class ProtesisAtaqueCuerpoT1311Pruebas : BaseJugadorAPie
    {
        private const float DistanciaRayo = 0.1f;

        private PiezasProtesisAtaquePJ01 piezas;
        private ProtesisVisiblesAPie visibles;
        private Animator animador;

        private IEnumerator Preparar()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            animador = jugador.GetComponentInChildren<Animator>();
            piezas = animador.GetComponentInChildren<PiezasProtesisAtaquePJ01>(true);
            visibles = jugador.GetComponent<ProtesisVisiblesAPie>();
            Assert.IsNotNull(piezas, "El cuerpo de PJ01 trae las piezas de prótesis de ataque");
            Assert.IsNotNull(visibles);
            Assert.IsNotNull(piezas.Laser);
            Assert.IsNotNull(piezas.Misiles);
        }

        private static void Dar(HabilidadProtesis h)
        {
            Registro<ServicioInventario>.Actual.Agregar(CatalogoProtesisAtaque.Actual.De(h), 1);
        }

        [UnityTest]
        public IEnumerator SinProtesis_LasPiezasEstanOcultas()
        {
            yield return Preparar();
            Assert.IsFalse(piezas.Laser.activeInHierarchy);
            Assert.IsFalse(piezas.Misiles.activeInHierarchy);
            Assert.IsNull(visibles.ModeloLaser);
            Assert.IsNull(visibles.PuntaLaser);
        }

        [UnityTest]
        public IEnumerator ConLaser_LaPiezaSeVe_YSuPuntoDeSalidaExiste()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Laser);
            yield return Esperar(0.3f);
            Assert.IsTrue(piezas.Laser.activeInHierarchy, "El brazo láser se ve");
            Assert.IsFalse(piezas.Misiles.activeInHierarchy, "La hombrera no");
            Assert.IsNotNull(piezas.SalidaLaser, "Existe la salida del rayo");
            Assert.AreEqual(animador.GetBoneTransform(HumanBodyBones.RightLowerArm), piezas.SalidaLaser.parent, "Colgada del antebrazo derecho");
            Assert.AreSame(piezas.SalidaLaser, visibles.PuntaLaser);
        }

        [UnityTest]
        public IEnumerator ConMisiles_LaPiezaSeVe_YSuPuntoDeSalidaExiste()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Misiles);
            yield return Esperar(0.3f);
            Assert.IsTrue(piezas.Misiles.activeInHierarchy, "La hombrera se ve");
            Assert.IsFalse(piezas.Laser.activeInHierarchy, "El brazo no");
            Assert.IsNotNull(piezas.SalidaMisiles, "Existe la salida de los misiles");
            Assert.AreSame(piezas.SalidaMisiles, visibles.SalidaMisiles);
        }

        [UnityTest]
        public IEnumerator Laser_ElRayoNaceEnLaSalidaDelFbx()
        {
            yield return Preparar();
            Dar(HabilidadProtesis.Laser);
            yield return Esperar(0.3f);
            Press(teclado.digit4Key);
            yield return null;
            yield return null;
            Release(teclado.digit4Key);
            yield return Esperar(0.3f);
            LineRenderer rayo = GameObject.Find("LaserBrazo").GetComponent<LineRenderer>();
            Assert.IsTrue(rayo.enabled);
            Assert.Less(Vector3.Distance(rayo.GetPosition(0), piezas.SalidaLaser.position), DistanciaRayo, "El rayo sale de IMP04_Salida");
        }
    }
}

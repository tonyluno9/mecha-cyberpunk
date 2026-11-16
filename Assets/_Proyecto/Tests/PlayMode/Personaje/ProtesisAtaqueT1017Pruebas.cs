using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Habilidades;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-1017: las prótesis de ataque del Doc con entrada simulada: sin la prótesis la tecla avisa y no hace nada; con el brazo láser, [4] dispara un rayo
    /// que daña lo que toca mientras dura; con la hombrera, [5] lanza misiles teledirigidos que explotan en los enemigos de enfrente.
    /// </summary>
    public class ProtesisAtaqueT1017Pruebas : BaseJugadorAPie
    {
        private class Blanco : MonoBehaviour, IRecibeDanio
        {
            public float Total;

            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Total += info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private HabilidadesProtesis habilidades;
        private ProtesisAtaqueAPie ataque;

        private Blanco PonerBlanco(Vector3 donde)
        {
            GameObject g = Caja(donde, new Vector3(0.6f, 1.8f, 0.6f));
            return g.AddComponent<Blanco>();
        }

        private void Dar(HabilidadProtesis h)
        {
            DatosProtesisAtaque p = CatalogoProtesisAtaque.Actual.De(h);
            Assert.IsNotNull(p, "Hay prótesis de " + h);
            Registro<ServicioInventario>.Actual.Agregar(p, 1);
        }

        private IEnumerator Preparar()
        {
            yield return Calentar();
            yield return Esperar(0.3f);
            habilidades = jugador.GetComponent<HabilidadesProtesis>();
            ataque = jugador.GetComponent<ProtesisAtaqueAPie>();
            Assert.IsNotNull(habilidades);
            Assert.IsNotNull(ataque, "HabilidadesProtesis agrega ProtesisAtaqueAPie");
        }

        private IEnumerator Tecla(UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            Press(tecla);
            yield return null;
            yield return null;
            Release(tecla);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SinLaProtesis_LaTeclaNoHaceNada()
        {
            yield return Preparar();
            bool avisado = false;
            habilidades.SinImplante += h => avisado |= h == HabilidadProtesis.Laser;
            Assert.AreEqual(0, habilidades.Nivel(HabilidadProtesis.Laser));
            yield return Tecla(teclado.digit4Key);
            Assert.AreEqual(EstadoHabilidad.Lista, habilidades.Estado(HabilidadProtesis.Laser));
            Assert.IsTrue(avisado, "Avisa que falta la prótesis");
            Assert.IsFalse(ataque.LaserVisible);
        }

        [UnityTest]
        public IEnumerator BrazoLaser_Tecla4_DisparaUnRayoQueDanaMientrasDura()
        {
            Blanco blanco = PonerBlanco(new Vector3(0.3f, 1.2f, 9f));
            yield return Preparar();
            Dar(HabilidadProtesis.Laser);
            yield return null;
            Assert.AreEqual(1, habilidades.Nivel(HabilidadProtesis.Laser), "Con la prótesis en el inventario, la habilidad está");
            yield return Tecla(teclado.digit4Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Laser), "[4] activa el láser");
            yield return Esperar(1.5f);
            Assert.IsTrue(ataque.LaserVisible, "Se ve el rayo");
            Debug.Log("[T1017] láser: blanco " + blanco.Total + " fin " + ataque.FinLaser);
            Assert.Greater(blanco.Total, 40f, "El rayo daña lo que toca");
            float duracion = habilidades.Config.Tiempos(HabilidadProtesis.Laser).duracionNivel1;
            yield return Esperar(duracion);
            Assert.AreEqual(EstadoHabilidad.Recargando, habilidades.Estado(HabilidadProtesis.Laser), "Al acabarse recarga");
            Assert.IsFalse(ataque.LaserVisible, "Y el rayo se apaga");
        }

        [UnityTest]
        public IEnumerator HombreraMisiles_Tecla5_LanzaMisilesQueVanALosEnemigosYExplotan()
        {
            Blanco izquierda = PonerBlanco(new Vector3(-4f, 0.9f, 14f));
            Blanco derecha = PonerBlanco(new Vector3(5f, 0.9f, 16f));
            Blanco atras = PonerBlanco(new Vector3(0f, 0.9f, -15f));
            yield return Preparar();
            Dar(HabilidadProtesis.Misiles);
            yield return null;
            int antes = ExplosionAPie.Cuantas;
            yield return Tecla(teclado.digit5Key);
            Assert.AreEqual(EstadoHabilidad.Activa, habilidades.Estado(HabilidadProtesis.Misiles), "[5] lanza los misiles");
            yield return Esperar(4f);
            int cuantos = habilidades.Config.CantidadMisiles;
            Debug.Log("[T1017] misiles: " + ataque.MisilesLanzados + " lanzados, " + (ExplosionAPie.Cuantas - antes) + " explosiones; izq " + izquierda.Total
                + ", der " + derecha.Total + ", atrás " + atras.Total);
            Assert.AreEqual(cuantos, ataque.MisilesLanzados, "Salen todos los misiles");
            Assert.AreEqual(cuantos, ExplosionAPie.Cuantas - antes, "Todos explotan");
            Assert.Greater(izquierda.Total, 0f, "Buscan a los enemigos de enfrente (izquierda)");
            Assert.Greater(derecha.Total, 0f, "Y a los de la derecha");
            Assert.AreEqual(0f, atras.Total, "El de atrás no es blanco");
        }
    }
}

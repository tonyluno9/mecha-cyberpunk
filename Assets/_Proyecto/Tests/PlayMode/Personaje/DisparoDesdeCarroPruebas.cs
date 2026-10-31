using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Combate;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>
    /// T-445: manejando un carro de prueba (con cámara propia) se dispara con clic izquierdo la pistola o el fusil: la bala sale del asiento
    /// hacia donde mira la cámara del carro, le pega a lo que tiene enfrente y no al carro propio; la escopeta y los puños no disparan desde ahí.
    /// </summary>
    public class DisparoDesdeCarroPruebas : BaseJugadorAPie
    {
        private const string RutaCarro = "Assets/_Proyecto/Personaje/Pruebas/CarroDePrueba.prefab";
        private const float DistanciaVictima = 9f;
        private const float AlturaVictima = 1.2f;
        private const int Disparos = 3;

        private sealed class Victima : MonoBehaviour, IRecibeDanio
        {
            public float Recibido;
            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Recibido += info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private GameObject carroObjeto;
        private Victima victima;
        private ArmasAPie armas;
        private PistolaAPie pistola;
        private int disparosOidos;

        public override void TearDown()
        {
            if (carroObjeto != null)
            {
                Object.Destroy(carroObjeto);
            }
            if (victima != null)
            {
                Object.Destroy(victima.gameObject);
            }
            base.TearDown();
        }

        private IEnumerator Clic()
        {
            Press(raton.leftButton);
            yield return null;
            Release(raton.leftButton);
            yield return null;
            yield return null;
        }

        // El jugador sube al carro de prueba mirando hacia +Z, con una víctima enfrente (a la altura de la mira).
        private IEnumerator SubirAlCarro()
        {
            yield return Calentar();
            armas = jugador.GetComponent<ArmasAPie>();
            pistola = jugador.GetComponent<PistolaAPie>();
            pistola.Disparo += () => disparosOidos++;
            carroObjeto = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0.05f, 4f), Quaternion.identity);
            yield return null;
            IMontable carro = carroObjeto.GetComponent<IMontable>();
            Assert.IsNotNull(carro, "El prefab es un montable");
            GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.name = "Victima";
            cuerpo.transform.position = new Vector3(0f, AlturaVictima, carroObjeto.transform.position.z + DistanciaVictima);
            victima = cuerpo.AddComponent<Victima>();
            Assert.IsTrue(jugadorAPie.Subir(carro), "El jugador sube al carro");
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo);
            yield return Esperar(0.3f);
        }

        [UnityTest]
        public IEnumerator Carro_ConPistola_DisparaYLeLlegaALaVictima()
        {
            yield return SubirAlCarro();
            Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola), "La pistola se tiene y se pone en la mano");
            for (int i = 0; i < Disparos; i++)
            {
                yield return Clic();
                yield return Esperar(0.35f);
            }
            Debug.Log("[Prueba] T-445 pistola desde el carro: disparos " + disparosOidos + ", daño a la víctima " + victima.Recibido);
            Assert.GreaterOrEqual(disparosOidos, Disparos, "Cada clic dispara desde el carro");
            Assert.Greater(victima.Recibido, 0f, "La bala le llega a lo que tiene enfrente");
            Assert.AreEqual(ModoJugador.Carro, jugadorAPie.Modo, "Sigue en el carro");
        }

        [UnityTest]
        public IEnumerator Carro_ConPunosEnLaMano_ElClicSacaLaPistola()
        {
            yield return SubirAlCarro();
            Assert.AreEqual(ArmaAPie.Punos, armas.ArmaActiva, "Empieza con los puños en la mano");
            yield return Clic();
            yield return Esperar(0.35f);
            Assert.GreaterOrEqual(disparosOidos, 1, "Con puños en la mano el clic saca la pistola");
            Assert.Greater(victima.Recibido, 0f);
        }

        [UnityTest]
        public IEnumerator APie_ConPuños_NoDisparaLaPistola()
        {
            yield return Calentar();
            pistola = jugador.GetComponent<PistolaAPie>();
            pistola.Disparo += () => disparosOidos++;
            yield return Clic();
            yield return Esperar(0.35f);
            Assert.AreEqual(0, disparosOidos, "A pie con puños el clic golpea, no dispara");
        }
    }
}

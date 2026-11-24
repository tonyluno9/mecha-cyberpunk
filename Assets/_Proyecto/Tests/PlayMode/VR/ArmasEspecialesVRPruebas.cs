using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// VR5: las armas especiales en VR con el simulador: el lanzagranadas dispara desde el cañón en la mano hacia donde apunta el control, la granada
    /// se toma con el grip y sale con la velocidad de la mano al soltarlo, el rifle de plasma carga y suelta el rayo con el gatillo sostenido, y el
    /// stick derecho en vertical ya no cambia de arma (VR19: las armas van en la rueda); mantenerlo abajo 1 s recentra.
    /// </summary>
    public class ArmasEspecialesVRPruebas : BaseJugadorVR
    {
        private static readonly Vector3 ManoDerReposo = new Vector3(0.2f, 1.2f, 0.3f);

        /// <summary>Un blanco que cuenta el daño que recibe.</summary>
        private class Blanco : MonoBehaviour, IRecibeDanio
        {
            public float Total;

            public ResultadoDanio RecibirDanio(InfoDanio info)
            {
                Total += info.Cantidad;
                return new ResultadoDanio(info.Cantidad, TipoBloqueo.Ninguno, false);
            }
        }

        private PistolaVR pistolaVR;
        private PistolaAPie pistola;
        private ArmasAPie armas;
        private ArmasEspecialesAPie especiales;
        private ArmasEspecialesVR especialesVR;
        private CambioArmaStickVR stick;

        private IEnumerator Preparar(ArmaAPie arma)
        {
            CargadorPistola.MunicionInfinita = false;
            yield return ArmarVR();
            pistolaVR = jugador.GetComponent<PistolaVR>();
            pistola = jugador.GetComponent<PistolaAPie>();
            armas = jugador.GetComponent<ArmasAPie>();
            especiales = jugador.GetComponent<ArmasEspecialesAPie>();
            especialesVR = jugador.GetComponent<ArmasEspecialesVR>();
            stick = jugador.GetComponent<CambioArmaStickVR>();
            Assert.IsNotNull(pistolaVR, "PistolaVR debe agregarse sola al armarse el rig.");
            Assert.IsNotNull(especiales, "El jugador lleva ArmasEspecialesAPie.");
            Assert.IsNotNull(especialesVR, "ArmasEspecialesVR debe agregarse solo al armarse el rig.");
            Assert.IsNotNull(stick, "CambioArmaStickVR debe agregarse solo al armarse el rig.");
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Assert.IsNotNull(arsenal, "Hay arsenal.");
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(arma, 0), 1);
            yield return Cuadros(2);
            Assert.IsTrue(armas.Equipar(arma), arma + " se equipa.");
            PonerMano(true, ManoDerReposo, Quaternion.identity);
            yield return Cuadros(3);
        }

        private Blanco PonerBlanco(Vector3 donde)
        {
            GameObject g = Caja(donde, new Vector3(0.8f, 1.8f, 0.8f));
            g.name = "BlancoVR5";
            return g.AddComponent<Blanco>();
        }

        [UnityTest]
        public IEnumerator Lanzagranadas_GatilloDerecho_LanzaDesdeLaBocaHaciaElCanon()
        {
            yield return Preparar(ArmaAPie.Lanzagranadas);
            // El control gira a un lado y sube un poco; la cabeza mira al frente: la granada debe seguir al cañón, no a la vista.
            PonerMano(true, ManoDerReposo, Quaternion.Euler(-10f, 35f, 0f));
            yield return Cuadros(4);
            Assert.AreEqual(ArmaAPie.Lanzagranadas, pistolaVR.ArmaMostrada, "El lanzagranadas debe verse en la mano derecha.");
            Assert.AreEqual(pistolaVR.Boca, especiales.BocaExterna, "El disparo especial sale de la boca del arma en la mano.");

            Vector3 origen = Vector3.zero;
            int tiros = 0;
            System.Action<ArmaAPie, Vector3> alDisparar = (arma, boca) =>
            {
                origen = boca;
                tiros++;
            };
            pistola.Fogonazo += alDisparar;
            ProyectilAPie antes = especiales.UltimoProyectil;
            int balasAntes = pistola.Balas;
            Vector3 boca0 = pistolaVR.Boca.position;
            Vector3 canon = pistolaVR.Boca.forward;
            yield return Pulsar(controlDer.triggerButton);
            pistola.Fogonazo -= alDisparar;

            ProyectilAPie p = especiales.UltimoProyectil;
            Assert.IsNotNull(p, "El gatillo debe lanzar la granada del lanzagranadas.");
            Assert.AreNotSame(antes, p);
            Assert.AreEqual(1, tiros, "Un gatillo, un tiro.");
            Assert.AreEqual(balasAntes - 1, pistola.Balas, "El tiro gasta uno del cargador.");
            float distancia = Vector3.Distance(origen, boca0);
            float angulo = Vector3.Angle(canon, p.Velocidad);
            Debug.Log("[VR] Lanzagranadas: origen a " + distancia.ToString("0.000") + " m de la boca, " + angulo.ToString("0.0") + "° del cañón, " + p.Velocidad.magnitude.ToString("0.0") + " m/s");
            Assert.Less(distancia, 0.1f, "Sale de la boca del arma en la mano.");
            Assert.Less(angulo, 6f, "Sale en la dirección del cañón.");
            Assert.Greater(Vector3.Angle(canon, adaptador.Camara.transform.forward), 15f, "El control apunta distinto que la cabeza.");
        }

        [UnityTest]
        public IEnumerator Granada_GripLaTomaYSoltarLaLanzaConLaVelocidadDeLaMano()
        {
            yield return Preparar(ArmaAPie.Granada);
            Assert.IsFalse(pistolaVR.ArmaVisible, "Sin grip la granada no está en la mano.");
            Mantener(controlDer.gripButton);
            yield return Cuadros(3);
            Assert.IsTrue(especialesVR.GranadaTomada, "El grip toma la granada.");
            Assert.IsTrue(pistolaVR.ArmaVisible, "La granada se ve en la mano.");
            Assert.AreEqual(ArmaAPie.Granada, pistolaVR.ArmaMostrada);

            int balasAntes = pistola.Balas;
            ProyectilAPie antes = especiales.UltimoProyectil;
            // Lanzamiento: 0.9 m hacia adelante y arriba en 0.15 s (unos 6 m/s); el grip se suelta en pleno movimiento.
            Vector3 desde = ManoDerReposo;
            Vector3 hasta = ManoDerReposo + new Vector3(0f, 0.3f, 0.85f);
            const float segundos = 0.15f;
            float t = 0f;
            bool solto = false;
            while (t < segundos)
            {
                t += Time.deltaTime;
                PonerMano(true, Vector3.Lerp(desde, hasta, Mathf.Clamp01(t / segundos)), Quaternion.identity);
                if (!solto && t >= segundos * 0.75f)
                {
                    Soltar(controlDer.gripButton);
                    solto = true;
                }
                yield return null;
            }
            if (!solto)
            {
                Soltar(controlDer.gripButton);
            }
            yield return Cuadros(2);

            ProyectilAPie p = especiales.UltimoProyectil;
            Assert.IsNotNull(p, "Soltar el grip lanza la granada.");
            Assert.AreNotSame(antes, p);
            Assert.AreEqual(1, especialesVR.Lanzadas);
            Assert.IsFalse(especialesVR.GranadaTomada);
            Assert.AreEqual(balasAntes - 1, pistola.Balas, "La granada se gasta.");
            Vector3 mano = especialesVR.UltimaVelocidadMano;
            Vector3 granada = especialesVR.UltimaVelocidadGranada;
            Vector3 movimiento = adaptador.Rig.TransformDirection(hasta - desde) / segundos;
            Debug.Log("[VR] Granada: mano " + mano.ToString("F2") + " (" + mano.magnitude.ToString("0.0") + " m/s), esperada ~" + movimiento.magnitude.ToString("0.0")
                + " m/s, granada " + granada.magnitude.ToString("0.0") + " m/s, origen a " + Vector3.Distance(especialesVR.UltimoOrigenGranada, adaptador.ManoDerecha.Palma).ToString("0.00") + " m de la palma");
            Assert.Greater(mano.magnitude, movimiento.magnitude * 0.5f, "La velocidad de la mano al soltar se parece a la del movimiento.");
            Assert.Less(mano.magnitude, movimiento.magnitude * 1.6f);
            Assert.Less(Vector3.Angle(mano, movimiento), 25f, "La mano iba hacia adelante y arriba.");
            Assert.Less(Vector3.Distance(granada, mano * ArmasEspecialesVR.FactorLanzamiento), 0.01f, "La granada sale con la velocidad de la mano (por el factor).");
            Assert.Less(Vector3.Angle(granada, p.Velocidad), 10f, "El proyectil vuela con esa velocidad.");
            Assert.Less(Vector3.Distance(especialesVR.UltimoOrigenGranada, adaptador.ManoDerecha.Palma), 0.6f, "Sale desde la mano.");
            Assert.IsFalse(pistolaVR.ArmaVisible, "Tras lanzarla la mano queda vacía.");
        }

        [UnityTest]
        public IEnumerator RiflePlasma_GatilloSostenido_CargaYSueltaElRayoPorElCanon()
        {
            yield return Preparar(ArmaAPie.RiflePlasma);
            PonerMano(true, ManoDerReposo, Quaternion.Euler(0f, -30f, 0f));
            yield return Cuadros(4);
            Assert.AreEqual(ArmaAPie.RiflePlasma, pistolaVR.ArmaMostrada, "El rifle de plasma debe verse en la mano derecha.");
            Transform boca = pistolaVR.Boca;
            Blanco blanco = PonerBlanco(boca.position + boca.forward * 6f);
            yield return Cuadros(2);
            Assert.IsTrue(especiales.LaserVisible, "En VR la línea de mira sale del cañón.");

            Mantener(controlDer.triggerButton);
            yield return Segundos(1f);
            Assert.AreEqual(EstadoPlasma.Cargando, especiales.EstadoPlasma, "Con el gatillo sostenido carga.");
            Assert.AreEqual(0f, blanco.Total, "Mientras carga no daña.");
            float fin = Time.time + 4f;
            while (especiales.EstadoPlasma != EstadoPlasma.Rayo && Time.time < fin)
            {
                yield return null;
            }
            Assert.AreEqual(EstadoPlasma.Rayo, especiales.EstadoPlasma, "Al acabar la carga sale el rayo.");
            yield return Segundos(0.6f);
            Vector3 haciaFin = especiales.FinRayo - pistolaVR.Boca.position;
            float angulo = Vector3.Angle(haciaFin, pistolaVR.Boca.forward);
            Debug.Log("[VR] Plasma: rayo a " + angulo.ToString("0.0") + "° del cañón; daño al blanco " + blanco.Total.ToString("0.0"));
            Assert.Less(angulo, 3f, "El rayo sigue al cañón del arma en la mano.");
            Assert.Greater(blanco.Total, 0f, "El rayo daña lo que tiene delante del cañón.");
            Soltar(controlDer.triggerButton);
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator StickDerechoArribaYAbajo_YaNoCambiaDeArma()
        {
            yield return Preparar(ArmaAPie.Granada);
            Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola));
            yield return Cuadros(3);

            Stick(true, new Vector2(0f, 1f));
            yield return Cuadros(5);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "VR19: arriba ya no cambia de arma.");
            Stick(true, Vector2.zero);
            yield return Cuadros(2);

            Stick(true, new Vector2(0f, -1f));
            yield return Cuadros(3);
            Stick(true, Vector2.zero);
            yield return Cuadros(3);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "VR19: un toque abajo ya no cambia de arma.");
            Assert.AreEqual(0, stick.Cambios);
            Assert.AreEqual(0, stick.Recentrados, "Un toque no recentra.");
        }

        [UnityTest]
        public IEnumerator StickDerechoAbajoUnSegundo_RecentraYNoCambiaDeArma()
        {
            yield return Preparar(ArmaAPie.Granada);
            Assert.IsTrue(armas.Equipar(ArmaAPie.Pistola));
            yield return Cuadros(3);
            int cambiosAntes = stick.Cambios;
            Stick(true, new Vector2(0f, -1f));
            yield return Segundos(1.3f);
            Assert.AreEqual(1, stick.Recentrados, "Mantener abajo 1 s recentra una vez.");
            Stick(true, Vector2.zero);
            yield return Cuadros(3);
            Assert.AreEqual(ArmaAPie.Pistola, armas.ArmaActiva, "Mantener abajo no cambia de arma.");
            Assert.AreEqual(cambiosAntes, stick.Cambios);
            Assert.AreEqual(1, stick.Recentrados, "Soltar no vuelve a recentrar.");
        }

        [Test]
        public void ReglaStick_TiemposDeAbajo()
        {
            var regla = new ReglaStickArmaVR();
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(new Vector2(0f, 0.9f), 0.01f), "VR19: arriba no pide nada.");
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(new Vector2(0f, 0.9f), 2f));
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(Vector2.zero, 0.01f));
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(new Vector2(0.95f, 0.8f), 0.01f), "En diagonal manda el giro.");
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(Vector2.zero, 0.01f));

            // Toque abajo de 0.3 s: VR19, ya no pide el arma anterior.
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(new Vector2(0f, -0.9f), 0.1f));
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(new Vector2(0f, -0.9f), 0.3f));
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(Vector2.zero, 0.01f));

            // Abajo 0.7 s: al soltar no pide nada.
            regla.Avanzar(new Vector2(0f, -0.9f), 0.1f);
            regla.Avanzar(new Vector2(0f, -0.9f), 0.7f);
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(Vector2.zero, 0.01f));

            // Abajo 1 s: recentra una vez y no cambia al soltar.
            regla.Avanzar(new Vector2(0f, -0.9f), 0.1f);
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(new Vector2(0f, -0.9f), 0.6f));
            Assert.AreEqual(GestoStickVR.Recentrar, regla.Avanzar(new Vector2(0f, -0.9f), 0.5f));
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(new Vector2(0f, -0.9f), 2f));
            Assert.AreEqual(GestoStickVR.Nada, regla.Avanzar(Vector2.zero, 0.01f));
        }

        [Test]
        public void ReglaStick_SiguienteDaLaVueltaEntreLasQueSeTienen()
        {
            System.Func<ArmaAPie, bool> tiene = a => a == ArmaAPie.Punos || a == ArmaAPie.Pistola || a == ArmaAPie.Granada;
            Assert.AreEqual(ArmaAPie.Granada, ReglaStickArmaVR.Siguiente(ArmaAPie.Pistola, 1, tiene));
            Assert.AreEqual(ArmaAPie.Punos, ReglaStickArmaVR.Siguiente(ArmaAPie.Granada, 1, tiene));
            Assert.AreEqual(ArmaAPie.Granada, ReglaStickArmaVR.Siguiente(ArmaAPie.Punos, -1, tiene));
            Assert.AreEqual(ArmaAPie.Pistola, ReglaStickArmaVR.Siguiente(ArmaAPie.Granada, -1, tiene));
        }
    }
}

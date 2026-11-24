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
    /// VR51/T91: el gatillo izquierdo apunta. Francotirador: visor a vista completa (bordes negros) con zoom en el stick derecho
    /// (sin recentrar ese rato). Lanzagranadas: el arco de caída. Plasma: carga con el izquierdo, avisa al llenarse y el derecho suelta el rayo.
    /// T91: la retícula va pegada a la cabeza, una por arma (pistola y revólver cruz, subfusil punto rojo, fusil y escopeta mira) y su centro
    /// marca dónde pega la bala.
    /// </summary>
    public class VR51MiraArmaVRPruebas : BaseJugadorVR
    {
        private static readonly Vector3 ManoDerReposo = new Vector3(0.2f, 1.2f, 0.3f);

        [Test]
        public void ReglaMira_ColimadaMarcaLaDireccionDelCanon_YElZoomSeLimita()
        {
            Vector3 vidrio = new Vector3(0f, 1.5f, 0.5f);
            Assert.IsTrue(ReglaMiraVR.PuntoColimado(new Vector3(0f, 1.5f, 0f), Vector3.forward, vidrio, Vector3.forward, out Vector3 p));
            Assert.Less(Vector3.Distance(vidrio, p), 1e-4f, "Con el ojo en la línea del cañón la retícula queda al centro.");
            Assert.IsTrue(ReglaMiraVR.PuntoColimado(new Vector3(0.01f, 1.5f, 0f), Vector3.forward, vidrio, Vector3.forward, out p));
            Assert.AreEqual(0.01f, p.x, 1e-4f, "Con el ojo a un lado la retícula se corre igual (está colimada).");
            Assert.IsFalse(ReglaMiraVR.PuntoColimado(new Vector3(0f, 1.5f, 1f), Vector3.forward, vidrio, Vector3.forward, out p), "Vidrio detrás del ojo: no se ve.");
            Assert.IsTrue(ReglaMiraVR.DentroDelVidrio(new Vector2(10f, 0f), 20f));
            Assert.IsFalse(ReglaMiraVR.DentroDelVidrio(new Vector2(30f, 0f), 20f));

            var regla = new ReglaMiraVR();
            Assert.AreEqual(ReglaMiraVR.FovInicial, regla.Fov);
            Assert.AreEqual(ReglaMiraVR.FovInicial, regla.AvanzarZoom(0.1f, 1f), "Un stick casi suelto no hace zoom.");
            Assert.Less(regla.AvanzarZoom(1f, 0.2f), ReglaMiraVR.FovInicial, "Arriba acerca.");
            for (int i = 0; i < 100; i++)
            {
                regla.AvanzarZoom(1f, 0.1f);
            }
            Assert.AreEqual(ReglaMiraVR.FovMinimo, regla.Fov, 1e-4f);
            for (int i = 0; i < 100; i++)
            {
                regla.AvanzarZoom(-1f, 0.1f);
            }
            Assert.AreEqual(ReglaMiraVR.FovMaximo, regla.Fov, 1e-4f);
        }

        [Test]
        public void ReglaPlasma_ConRetener_LaCargaLlenaEsperaAlDisparo_YSinRetenerSaleSola()
        {
            var sola = new ReglaRayoPlasma(1f, 0.5f);
            sola.Avanzar(0.6f, true, () => true);
            Assert.IsTrue(sola.Avanzar(0.6f, true, () => true), "Sin retener (PC) la carga llena suelta el rayo sola.");

            var retenida = new ReglaRayoPlasma(1f, 0.5f) { RetenerCarga = true };
            retenida.Avanzar(0.6f, true, () => true, false);
            Assert.IsFalse(retenida.Avanzar(0.6f, true, () => true, false));
            Assert.IsTrue(retenida.CargaLlena, "Llena, esperando.");
            Assert.IsFalse(retenida.Avanzar(2f, true, () => true, false), "Sigue esperando el disparo.");
            Assert.AreEqual(EstadoPlasma.Cargando, retenida.Estado);
            Assert.IsTrue(retenida.Avanzar(0.01f, true, () => true, true), "El disparo suelta el rayo.");
            Assert.AreEqual(EstadoPlasma.Rayo, retenida.Estado);

            var soltada = new ReglaRayoPlasma(1f, 0.5f) { RetenerCarga = true };
            soltada.Avanzar(1.2f, true, () => true, false);
            soltada.Avanzar(0.01f, false, () => true, false);
            Assert.AreEqual(EstadoPlasma.Reposo, soltada.Estado, "Soltar la carga antes del disparo la cancela.");
        }

        private PistolaVR pistolaVR;
        private ArmasAPie armas;
        private MiraArmaVR mira;
        private ArmasEspecialesAPie especiales;

        private IEnumerator Preparar(ArmaAPie arma)
        {
            CargadorPistola.MunicionInfinita = false;
            yield return ArmarVR();
            pistolaVR = jugador.GetComponent<PistolaVR>();
            armas = jugador.GetComponent<ArmasAPie>();
            especiales = jugador.GetComponent<ArmasEspecialesAPie>();
            mira = jugador.GetComponent<MiraArmaVR>();
            Assert.IsNotNull(mira, "MiraArmaVR se agrega sola al armarse el rig.");
            if (arma != ArmaAPie.Pistola)
            {
                ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
                Assert.IsNotNull(arsenal, "Hay arsenal.");
                Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(arma, 0), 1);
                yield return Cuadros(2);
            }
            if (armas.ArmaActiva != arma)
            {
                Assert.IsTrue(armas.Equipar(arma), arma + " se equipa.");
            }
            PonerMano(true, ManoDerReposo, Quaternion.identity);
            yield return Cuadros(4);
            Assert.AreEqual(arma, pistolaVR.ArmaMostrada, arma + " en la mano derecha.");
        }

        [Test]
        public void ReglaReticula_VaSobreLaLineaOjoImpacto_YUnaPorArma()
        {
            Vector3 objetivo = new Vector3(2f, 1f, 20f);
            Assert.IsTrue(ReglaMiraVR.PosicionReticula(objetivo, 0.3f, 60f, out Vector3 p));
            Assert.Less(Vector3.Distance(objetivo, p), 1e-3f, "Dentro del rango, la retícula va justo en el punto de impacto.");
            Assert.IsTrue(ReglaMiraVR.PosicionReticula(objetivo * 10f, 0.3f, 60f, out p));
            Assert.AreEqual(60f, p.magnitude, 1e-3f, "Lejos se limita la distancia...");
            Assert.Greater(Vector3.Dot(p.normalized, objetivo.normalized), 0.99999f, "...sin salirse de la línea ojo-impacto.");
            Assert.IsTrue(ReglaMiraVR.PosicionReticula(new Vector3(0f, 0f, 0.05f), 0.3f, 60f, out p));
            Assert.AreEqual(0.3f, p.magnitude, 1e-4f, "Pegado al ojo se aleja al mínimo.");
            Assert.IsFalse(ReglaMiraVR.PosicionReticula(new Vector3(0f, 0f, -5f), 0.3f, 60f, out p), "Detrás de la cabeza no se ve.");
            Assert.AreEqual(2f * 10f * Mathf.Tan(5f * Mathf.Deg2Rad), ReglaMiraVR.LadoAngular(10f, 10f), 1e-4f);
            Assert.AreEqual(2f * ReglaMiraVR.LadoAngular(1f, 8f), ReglaMiraVR.LadoAngular(2f, 8f), 1e-5f, "Tamaño angular fijo: crece con la distancia.");

            Assert.AreEqual(MiraArmaVR.TipoMira.Cruz, MiraArmaVR.MiraDe(ArmaAPie.Pistola));
            Assert.AreEqual(MiraArmaVR.TipoMira.Cruz, MiraArmaVR.MiraDe(ArmaAPie.Revolver));
            Assert.AreEqual(MiraArmaVR.TipoMira.PuntoRojo, MiraArmaVR.MiraDe(ArmaAPie.Subfusil));
            Assert.AreEqual(MiraArmaVR.TipoMira.Mira, MiraArmaVR.MiraDe(ArmaAPie.Fusil));
            Assert.AreEqual(MiraArmaVR.TipoMira.Mira, MiraArmaVR.MiraDe(ArmaAPie.Escopeta));
            Assert.AreEqual(MiraArmaVR.TipoMira.Visor, MiraArmaVR.MiraDe(ArmaAPie.Sniper));
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, MiraArmaVR.MiraDe(ArmaAPie.Lanzagranadas), "El lanzagranadas sigue con su arco.");
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, MiraArmaVR.MiraDe(ArmaAPie.RiflePlasma), "El plasma sigue con su carga.");
        }

        // T91: la retícula pegada a la cabeza (hija de la cámara XR), con su centro sobre la línea ojo-impacto del cañón; al soltar se quita.
        private IEnumerator ComprobarReticula(MiraArmaVR.TipoMira esperada)
        {
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, mira.Activa, "Sin el gatillo izquierdo no hay retícula.");
            Mantener(controlIzq.triggerButton);
            yield return Cuadros(3);
            Assert.AreEqual(esperada, mira.Activa, "El gatillo izquierdo saca la retícula del arma.");
            Assert.IsNotNull(mira.Lienzo);
            Assert.IsTrue(mira.Lienzo.gameObject.activeInHierarchy, "La retícula se ve.");
            Assert.IsTrue(mira.ReticulaVisible);
            Assert.AreEqual(RenderMode.WorldSpace, mira.Lienzo.renderMode);
            Transform cabeza = adaptador.Camara.transform;
            Assert.IsTrue(mira.Lienzo.transform.IsChildOf(cabeza), "La retícula va pegada a la cabeza.");
            Assert.IsFalse(mira.Lienzo.transform.IsChildOf(pistolaVR.Arma), "Ya no hay mira en el arma.");
            Vector3 haciaReticula = (mira.Lienzo.transform.position - cabeza.position).normalized;
            Vector3 haciaImpacto = (mira.PuntoImpacto - cabeza.position).normalized;
            Assert.Greater(Vector3.Dot(haciaReticula, haciaImpacto), 0.9999f, "El centro de la retícula tapa el punto donde pega la bala.");
            Vector3 alImpacto = mira.PuntoImpacto - pistolaVR.Boca.position;
            Assert.Greater(Vector3.Dot(alImpacto.normalized, pistolaVR.Boca.forward), 0.999f, "El punto de impacto está sobre la línea del cañón.");
            Assert.IsFalse(mira.CamaraVisor != null && mira.CamaraVisor.enabled, "Sin zoom: el visor no dibuja.");
            Soltar(controlIzq.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, mira.Activa);
            Assert.IsFalse(mira.Lienzo.gameObject.activeInHierarchy, "Al soltar vuelve la vista normal.");
        }

        [UnityTest]
        public IEnumerator Subfusil_GatilloIzquierdo_SacaElPuntoRojoAlineadoConElCanon()
        {
            yield return Preparar(ArmaAPie.Subfusil);
            yield return ComprobarReticula(MiraArmaVR.TipoMira.PuntoRojo);
        }

        [UnityTest]
        public IEnumerator Pistola_GatilloIzquierdo_SacaLaCruzAlineadaConElCanon()
        {
            yield return Preparar(ArmaAPie.Pistola);
            yield return ComprobarReticula(MiraArmaVR.TipoMira.Cruz);
        }

        [UnityTest]
        public IEnumerator Pistola_ConUnaParedDelante_LaReticulaVaEnElImpacto()
        {
            yield return Preparar(ArmaAPie.Pistola);
            // Una pared delante del cañón: el punto de impacto es la pared, no el alcance máximo.
            Vector3 boca = pistolaVR.Boca.position;
            Vector3 centro = boca + pistolaVR.Boca.forward * 6f;
            GameObject pared = Caja(centro, new Vector3(8f, 8f, 0.2f));
            pared.transform.rotation = Quaternion.LookRotation(pistolaVR.Boca.forward);
            Physics.SyncTransforms();
            yield return Cuadros(2);
            Mantener(controlIzq.triggerButton);
            yield return Cuadros(3);
            Assert.AreEqual(MiraArmaVR.TipoMira.Cruz, mira.Activa);
            float distancia = Vector3.Distance(pistolaVR.Boca.position, mira.PuntoImpacto);
            Assert.Less(distancia, 7f, "La bala pega en la pared.");
            Assert.Greater(distancia, 5f);
            Soltar(controlIzq.triggerButton);
            yield return Cuadros(2);
        }

        [UnityTest]
        public IEnumerator Francotirador_VisorConZoomEnElStickDerecho_SinRecentrar()
        {
            yield return Preparar(ArmaAPie.Sniper);
            CambioArmaStickVR stick = jugador.GetComponent<CambioArmaStickVR>();
            Mantener(controlIzq.triggerButton);
            yield return Cuadros(3);
            Assert.AreEqual(MiraArmaVR.TipoMira.Visor, mira.Activa);
            Assert.IsTrue(mira.CamaraVisor.enabled, "El visor dibuja la cámara del cañón.");
            Assert.AreEqual(StereoTargetEyeMask.None, mira.CamaraVisor.stereoTargetEye, "La cámara del visor no va al casco.");
            Assert.IsTrue(mira.LienzoVisor.gameObject.activeInHierarchy, "El visor a vista completa se ve.");
            Assert.IsTrue(mira.LienzoVisor.transform.IsChildOf(adaptador.Camara.transform), "El visor va pegado a la cabeza.");
            Assert.AreEqual(0, mira.CamaraVisor.cullingMask & (1 << mira.LienzoVisor.gameObject.layer), "La cámara del cañón no dibuja el visor (si no, se vería negro).");
            Assert.IsTrue(MiraArmaVR.BloqueaGiro);
            float antes = mira.Fov;
            Stick(true, new Vector2(0f, 1f));
            yield return Segundos(0.4f);
            Assert.Less(mira.Fov, antes, "Stick derecho arriba acerca.");
            Assert.AreEqual(mira.Fov, mira.CamaraVisor.fieldOfView, 1e-3f);
            int recentrados = stick.Recentrados;
            Stick(true, new Vector2(0f, -1f));
            yield return Segundos(1.4f);
            Assert.AreEqual(recentrados, stick.Recentrados, "Con el visor, el stick abajo es zoom, no recentrar.");
            Stick(true, Vector2.zero);
            Soltar(controlIzq.triggerButton);
            yield return Cuadros(2);
            Assert.IsFalse(MiraArmaVR.BloqueaGiro);
            Assert.IsFalse(mira.CamaraVisor.enabled, "Al soltar el visor deja de dibujar.");
            Assert.IsFalse(mira.LienzoVisor.gameObject.activeInHierarchy, "Al soltar vuelve la vista normal.");
        }

        [UnityTest]
        public IEnumerator Lanzagranadas_GatilloIzquierdo_MuestraElArco()
        {
            yield return Preparar(ArmaAPie.Lanzagranadas);
            yield return Cuadros(2);
            Assert.IsFalse(especiales.ArcoVisible, "Sin el gatillo izquierdo no hay arco.");
            Mantener(controlIzq.triggerButton);
            yield return Cuadros(3);
            Assert.IsTrue(especiales.ArcoVisible, "El gatillo izquierdo muestra la trayectoria.");
            Soltar(controlIzq.triggerButton);
            yield return Cuadros(3);
            Assert.IsFalse(especiales.ArcoVisible);
        }

        [UnityTest]
        public IEnumerator Plasma_IzquierdoCarga_AvisaLlena_YElDerechoDispara()
        {
            yield return Preparar(ArmaAPie.RiflePlasma);
            ArmasEspecialesVR especialesVR = jugador.GetComponent<ArmasEspecialesVR>();
            Mantener(controlIzq.triggerButton);
            yield return Cuadros(3);
            Assert.AreEqual(EstadoPlasma.Cargando, especiales.EstadoPlasma, "El gatillo izquierdo carga.");
            float fin = Time.time + 6f;
            while (!especiales.CargaPlasmaLlena && Time.time < fin)
            {
                yield return null;
            }
            Assert.IsTrue(especiales.CargaPlasmaLlena, "La carga se llena.");
            yield return Cuadros(2);
            Assert.AreEqual(1, especialesVR.AvisosCargaLlena, "Al llenarse avisa una vez.");
            yield return Segundos(0.5f);
            Assert.AreEqual(EstadoPlasma.Cargando, especiales.EstadoPlasma, "Llena, no dispara sola: espera al gatillo derecho.");
            Mantener(controlDer.triggerButton);
            yield return Cuadros(3);
            Assert.AreEqual(EstadoPlasma.Rayo, especiales.EstadoPlasma, "El gatillo derecho suelta el rayo.");
            Soltar(controlDer.triggerButton);
            Soltar(controlIzq.triggerButton);
            yield return Cuadros(2);
        }
    }
}

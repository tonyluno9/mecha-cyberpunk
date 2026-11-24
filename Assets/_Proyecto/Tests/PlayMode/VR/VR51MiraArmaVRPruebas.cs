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
    /// VR51/T91/T101: el gatillo izquierdo apunta. T101: con pistola, revólver, subfusil, fusil, escopeta y francotirador todo se pone negro
    /// salvo el círculo de la mira (pegado a la cabeza, su centro marca dónde pega la bala), con la imagen de una cámara de mira a una
    /// RenderTexture: 1.5× fijo, y el francotirador de 2× a 8× con el stick derecho (sin girar ni recentrar ese rato). Al soltar, fundido de 0.1 s.
    /// Lanzagranadas: el arco de caída. Plasma: carga con el izquierdo, avisa al llenarse y el derecho suelta el rayo.
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
            Assert.AreEqual(ReglaMiraVR.AumentoInicial, regla.AumentoFrancotirador);
            Assert.AreEqual(ReglaMiraVR.AumentoInicial, regla.AvanzarZoom(0.1f, 1f), "Un stick casi suelto no hace zoom.");
            Assert.Greater(regla.AvanzarZoom(1f, 0.2f), ReglaMiraVR.AumentoInicial, "Adelante acerca.");
            for (int i = 0; i < 100; i++)
            {
                regla.AvanzarZoom(1f, 0.1f);
            }
            Assert.AreEqual(ReglaMiraVR.AumentoMaximo, regla.AumentoFrancotirador, 1e-4f, "Hasta 8×.");
            for (int i = 0; i < 100; i++)
            {
                regla.AvanzarZoom(-1f, 0.1f);
            }
            Assert.AreEqual(ReglaMiraVR.AumentoMinimo, regla.AumentoFrancotirador, 1e-4f, "Desde 2×.");
        }

        [Test]
        public void ReglaMiraT101_QueArmasApuntan_ZoomSoloEnElFrancotirador_FundidoYBloqueoDeGiro()
        {
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, ReglaMiraVR.MiraDe(ArmaAPie.Pistola));
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, ReglaMiraVR.MiraDe(ArmaAPie.Revolver));
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, ReglaMiraVR.MiraDe(ArmaAPie.Subfusil));
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, ReglaMiraVR.MiraDe(ArmaAPie.Fusil));
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, ReglaMiraVR.MiraDe(ArmaAPie.Escopeta));
            Assert.AreEqual(MiraArmaVR.TipoMira.Zoom, ReglaMiraVR.MiraDe(ArmaAPie.Sniper));
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, ReglaMiraVR.MiraDe(ArmaAPie.Lanzagranadas), "El lanzagranadas sigue con su arco.");
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, ReglaMiraVR.MiraDe(ArmaAPie.RiflePlasma), "El plasma sigue con su carga.");

            // El campo de visión de la cámara de mira da el aumento dentro del círculo (sin zoom, el mismo ángulo).
            Assert.AreEqual(ReglaMiraVR.AnguloCirculo, ReglaMiraVR.FovCamara(ReglaMiraVR.AnguloCirculo, 1f), 1e-3f);
            float fov = ReglaMiraVR.FovCamara(ReglaMiraVR.AnguloCirculo, 4f);
            Assert.AreEqual(4f, Mathf.Tan(ReglaMiraVR.AnguloCirculo * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad), 1e-3f);

            var regla = new ReglaMiraVR();
            Assert.IsFalse(regla.Visible);
            regla.Avanzar(MiraArmaVR.TipoMira.Fija, true, 1f, 0.05f);
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, regla.Activa);
            Assert.AreEqual(0.5f, regla.Alfa, 1e-4f, "Entra con fundido de 0.1 s.");
            regla.Avanzar(MiraArmaVR.TipoMira.Fija, true, 1f, 0.2f);
            Assert.AreEqual(1f, regla.Alfa, 1e-4f, "Todo negro salvo la mira.");
            Assert.AreEqual(ReglaMiraVR.AumentoFijo, regla.Aumento, "Las demás armas: 1.5× fijo.");
            Assert.AreEqual(ReglaMiraVR.AumentoInicial, regla.AumentoFrancotirador, "El stick no hace zoom con un arma de aumento fijo.");
            Assert.IsFalse(regla.BloqueaGiro, "Con un arma de aumento fijo el stick sigue girando.");
            regla.Avanzar(MiraArmaVR.TipoMira.Fija, false, 0f, 0.05f);
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, regla.Activa, "Al soltar ya no se apunta...");
            Assert.AreEqual(0.5f, regla.Alfa, 1e-4f, "...pero la vista vuelve con fundido de 0.1 s.");
            Assert.IsTrue(regla.Visible);
            regla.Avanzar(MiraArmaVR.TipoMira.Fija, false, 0f, 0.06f);
            Assert.IsFalse(regla.Visible, "A los 0.1 s la vista es la normal.");

            regla.Avanzar(MiraArmaVR.TipoMira.Zoom, true, 1f, 0.5f);
            Assert.IsTrue(regla.BloqueaGiro, "Con el francotirador el stick derecho no gira.");
            Assert.Greater(regla.Aumento, ReglaMiraVR.AumentoInicial, "Adelante acerca.");
            Assert.Less(regla.Fov, ReglaMiraVR.FovCamara(ReglaMiraVR.AnguloCirculo, ReglaMiraVR.AumentoInicial));
            float aumento = regla.Aumento;
            regla.Avanzar(MiraArmaVR.TipoMira.Zoom, false, 0f, 0.5f);
            Assert.IsFalse(regla.BloqueaGiro, "Al soltar vuelve el giro.");
            regla.Avanzar(MiraArmaVR.TipoMira.Zoom, true, 0f, 0.5f);
            Assert.AreEqual(aumento, regla.Aumento, 1e-4f, "El francotirador recuerda el último zoom.");
            regla.Cortar();
            Assert.IsFalse(regla.Visible);
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, regla.Activa);
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
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, MiraArmaVR.MiraDe(ArmaAPie.Pistola), "MiraArmaVR usa la regla.");
        }

        // T101: todo negro salvo el círculo de la mira, pegado a la cabeza y con su centro sobre la línea ojo-impacto del cañón; dentro, la
        // cámara de mira con el aumento. Al soltar, la vista vuelve con fundido.
        private IEnumerator ComprobarMira(MiraArmaVR.TipoMira esperada)
        {
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, mira.Activa, "Sin el gatillo izquierdo no hay mira.");
            Assert.IsFalse(mira.ReticulaVisible);
            float fovVisor = adaptador.Camara.fieldOfView;
            Mantener(controlIzq.triggerButton);
            yield return Segundos(0.25f);
            Assert.AreEqual(esperada, mira.Activa, "El gatillo izquierdo saca la mira del arma.");
            Assert.IsNotNull(mira.Lienzo);
            Assert.IsTrue(mira.ReticulaVisible, "La mira se ve.");
            Assert.AreEqual(1f, mira.Alfa, 1e-4f, "Pasado el fundido, todo negro salvo el círculo.");
            Assert.AreEqual(RenderMode.WorldSpace, mira.Lienzo.renderMode);
            Transform cabeza = adaptador.Camara.transform;
            Assert.IsTrue(mira.Lienzo.transform.IsChildOf(cabeza), "La mira va pegada a la cabeza (igual en los dos ojos).");
            Assert.IsFalse(mira.Lienzo.transform.IsChildOf(pistolaVR.Arma), "No hay mira en el arma.");
            Vector3 haciaMira = (mira.Lienzo.transform.position - cabeza.position).normalized;
            Vector3 haciaImpacto = (mira.PuntoImpacto - cabeza.position).normalized;
            Assert.Greater(Vector3.Dot(haciaMira, haciaImpacto), 0.9999f, "El centro del círculo tapa el punto donde pega la bala.");
            Vector3 alImpacto = mira.PuntoImpacto - pistolaVR.Boca.position;
            Assert.Greater(Vector3.Dot(alImpacto.normalized, pistolaVR.Boca.forward), 0.999f, "El punto de impacto está sobre la línea del cañón.");
            Assert.IsNotNull(mira.CamaraMira);
            Assert.IsTrue(mira.CamaraMira.enabled, "La cámara de mira dibuja mientras se apunta.");
            Assert.AreEqual(StereoTargetEyeMask.None, mira.CamaraMira.stereoTargetEye, "La cámara de mira no va al casco.");
            Assert.AreSame(mira.TexturaMira, mira.CamaraMira.targetTexture, "Dibuja a la RenderTexture del círculo.");
            Assert.AreEqual(MiraArmaVR.ResolucionMira, mira.TexturaMira.width);
            Assert.AreEqual(0, mira.CamaraMira.cullingMask & (1 << mira.Lienzo.gameObject.layer), "La cámara de mira no se dibuja a sí misma.");
            Assert.AreEqual(adaptador.Camara.cullingMask & ~(1 << mira.Lienzo.gameObject.layer), mira.CamaraMira.cullingMask, "Ve lo mismo que el visor.");
            Assert.Greater(Vector3.Dot(mira.CamaraMira.transform.forward, pistolaVR.Boca.forward), 0.999f, "La cámara de mira mira por el cañón.");
            Assert.AreEqual(mira.Fov, mira.CamaraMira.fieldOfView, 1e-3f);
            Assert.AreEqual(fovVisor, adaptador.Camara.fieldOfView, 1e-3f, "El zoom nunca cambia el campo de visión del visor.");
            Soltar(controlIzq.triggerButton);
            yield return Cuadros(2);
            Assert.AreEqual(MiraArmaVR.TipoMira.Ninguna, mira.Activa, "Al soltar deja de apuntar.");
            Assert.Less(mira.Alfa, 1f, "Al soltar empieza el fundido.");
            yield return Segundos(0.25f);
            Assert.IsFalse(mira.ReticulaVisible, "Pasado el fundido vuelve la vista normal.");
            Assert.IsFalse(mira.CamaraMira.enabled, "Sin mira, la cámara de mira no dibuja.");
        }

        [UnityTest]
        public IEnumerator Subfusil_GatilloIzquierdo_TodoNegroSalvoLaMiraAlineadaConElCanon()
        {
            yield return Preparar(ArmaAPie.Subfusil);
            yield return ComprobarMira(MiraArmaVR.TipoMira.Fija);
            Assert.AreEqual(ReglaMiraVR.AumentoFijo, mira.Aumento, 1e-4f, "El subfusil va a 1.5×.");
        }

        [UnityTest]
        public IEnumerator Pistola_GatilloIzquierdo_TodoNegroSalvoLaMiraAlineadaConElCanon()
        {
            yield return Preparar(ArmaAPie.Pistola);
            yield return ComprobarMira(MiraArmaVR.TipoMira.Fija);
            Assert.IsFalse(MiraArmaVR.BloqueaGiro, "Con la pistola el stick sigue girando.");
        }

        [UnityTest]
        public IEnumerator Pistola_ConUnaParedDelante_LaMiraVaEnElImpacto()
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
            Assert.AreEqual(MiraArmaVR.TipoMira.Fija, mira.Activa);
            float distancia = Vector3.Distance(pistolaVR.Boca.position, mira.PuntoImpacto);
            Assert.Less(distancia, 7f, "La bala pega en la pared.");
            Assert.Greater(distancia, 5f);
            Soltar(controlIzq.triggerButton);
            yield return Segundos(0.25f);
        }

        [UnityTest]
        public IEnumerator Francotirador_ZoomConElStickDerecho_SinGirarNiRecentrar()
        {
            yield return Preparar(ArmaAPie.Sniper);
            CambioArmaStickVR stick = jugador.GetComponent<CambioArmaStickVR>();
            yield return ComprobarMira(MiraArmaVR.TipoMira.Zoom);
            Mantener(controlIzq.triggerButton);
            yield return Segundos(0.25f);
            Assert.IsTrue(MiraArmaVR.BloqueaGiro);
            float antes = mira.Aumento;
            Assert.GreaterOrEqual(antes, ReglaMiraVR.AumentoMinimo);
            Stick(true, new Vector2(0f, 1f));
            yield return Segundos(0.4f);
            Assert.Greater(mira.Aumento, antes, "Stick derecho adelante acerca.");
            Assert.LessOrEqual(mira.Aumento, ReglaMiraVR.AumentoMaximo);
            Assert.AreEqual(mira.Fov, mira.CamaraMira.fieldOfView, 1e-3f, "El zoom va en la cámara de mira.");
            int recentrados = stick.Recentrados;
            float acercado = mira.Aumento;
            Stick(true, new Vector2(0f, -1f));
            yield return Segundos(1.4f);
            Assert.Less(mira.Aumento, acercado, "Stick derecho atrás aleja.");
            Assert.AreEqual(recentrados, stick.Recentrados, "Con la mira, el stick atrás es zoom, no recentrar.");
            float giro = adaptador.GiroRig;
            Stick(true, new Vector2(1f, 0f));
            yield return Segundos(0.4f);
            Assert.AreEqual(giro, adaptador.GiroRig, 1e-3f, "Apuntando con el francotirador el stick derecho no gira la vista.");
            Stick(true, Vector2.zero);
            Soltar(controlIzq.triggerButton);
            yield return Segundos(0.25f);
            Assert.IsFalse(MiraArmaVR.BloqueaGiro);
            Assert.IsFalse(mira.CamaraMira.enabled, "Al soltar la cámara de mira deja de dibujar.");
            Assert.IsFalse(mira.ReticulaVisible, "Al soltar vuelve la vista normal.");
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

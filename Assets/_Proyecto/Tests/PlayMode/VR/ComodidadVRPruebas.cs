using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Inicio;
using MechaCyberpunk.UI.Kit;
using MechaCyberpunk.VR;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// V8–V10 con el simulador: giro suave gradual, VIÑETA: NO sin viñeta al moverse, CORRER ALTERNAR (clic enciende, clic apaga, soltar
    /// el stick apaga) y Recentrar (ojos a 1.72 m y la cabeza mirando al frente del cuerpo). Las opciones de VR del editor se guardan y
    /// se restauran al terminar.
    /// </summary>
    public class ComodidadVRPruebas : BaseJugadorVR
    {
        private ModoGiroVR giroAntes;
        private NivelVinetaVR vinetaAntes;
        private bool rutaAntes;
        private ModoCorrerVR correrAntes;
        private float velocidadAntes;

        public override void Setup()
        {
            base.Setup();
            OpcionesVR.Recargar();
            giroAntes = OpcionesVR.Giro;
            vinetaAntes = OpcionesVR.NivelVineta;
            rutaAntes = OpcionesVR.Ruta;
            correrAntes = OpcionesVR.Correr;
            velocidadAntes = OpcionesVR.VelocidadGiro;
            OpcionesVR.FijarGiro(OpcionesVR.GiroPorDefecto);
            OpcionesVR.FijarVelocidadGiro(OpcionesVR.VelocidadGiroPorDefecto);
            OpcionesVR.FijarNivelVineta(OpcionesVR.NivelVinetaPorDefecto);
            OpcionesVR.FijarRuta(OpcionesVR.RutaPorDefecto);
            OpcionesVR.FijarCorrer(OpcionesVR.CorrerPorDefecto);
        }

        public override void TearDown()
        {
            OpcionesVR.FijarGiro(giroAntes);
            OpcionesVR.FijarNivelVineta(vinetaAntes);
            OpcionesVR.FijarRuta(rutaAntes);
            OpcionesVR.FijarCorrer(correrAntes);
            OpcionesVR.FijarVelocidadGiro(velocidadAntes);
            base.TearDown();
        }

        private static float YawPlano(Vector3 adelante)
        {
            adelante.y = 0f;
            return Mathf.Atan2(adelante.x, adelante.z) * Mathf.Rad2Deg;
        }

        [UnityTest]
        public IEnumerator GiroSuaveGiraPocoAPocoYConVineta()
        {
            OpcionesVR.FijarGiro(ModoGiroVR.Suave);
            yield return ArmarVR();
            float antes = adaptador.GiroRig;
            Stick(true, new Vector2(1f, 0f));
            yield return Segundos(0.2f);
            float parcial = Mathf.DeltaAngle(antes, adaptador.GiroRig);
            Assert.IsTrue(adaptador.VinetaPuesta, "Con VIÑETA: SÍ la viñeta sale mientras gira suave.");
            yield return Segundos(0.4f);
            float total = Mathf.DeltaAngle(antes, adaptador.GiroRig);
            Stick(true, Vector2.zero);
            yield return Cuadros(3);
            float quieto = Mathf.DeltaAngle(antes, adaptador.GiroRig);
            Debug.Log("[VR] Giro suave: " + parcial.ToString("0.0") + "° en 0.2 s, " + total.ToString("0.0") + "° en 0.6 s.");
            Assert.Greater(parcial, 5f, "Empieza a girar enseguida.");
            Assert.Less(parcial, 35f, "No es un paso de 45°: es gradual (~120°/s).");
            Assert.Greater(total, parcial + 20f, "Sigue girando mientras se mantiene el stick.");
            Assert.AreEqual(AdaptadorVR.VelocidadGiroSuave * 0.6f, total, 25f, "A fondo gira a ~120°/s (VELOCIDAD DE GIRO).");
            Assert.AreEqual(total, quieto, 3f, "Al soltar el stick deja de girar.");
        }

        [UnityTest]
        public IEnumerator VinetaNoNoSeMuestraAlMoverse()
        {
            yield return ArmarVR();
            Stick(false, new Vector2(0f, 1f));
            yield return Segundos(0.6f);
            Assert.IsTrue(adaptador.VinetaPuesta, "Con VIÑETA: SÍ (por defecto) la viñeta sale al caminar.");
            Stick(false, Vector2.zero);
            yield return Segundos(0.5f);

            OpcionesVR.FijarVineta(false);
            Stick(false, new Vector2(0f, 1f));
            bool algunaVez = false;
            float fin = Time.time + 0.6f;
            while (Time.time < fin)
            {
                yield return null;
                algunaVez |= adaptador.VinetaPuesta;
            }
            Stick(false, Vector2.zero);
            Stick(true, new Vector2(1f, 0f));
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                algunaVez |= adaptador.VinetaPuesta;
            }
            Stick(true, Vector2.zero);
            Assert.IsFalse(algunaVez, "Con VIÑETA: NO la viñeta no sale ni al caminar ni al girar.");
        }

        [UnityTest]
        public IEnumerator CorrerAlternarConUnClicYSeApagaConOtroOAlSoltar()
        {
            OpcionesVR.FijarCorrer(ModoCorrerVR.Alternar);
            yield return ArmarVR();
            Assert.AreEqual(ModoCorrerVR.Alternar, adaptador.ModoCorrer);

            // Caminando: sin clic no corre.
            Stick(false, new Vector2(0f, 1f));
            yield return Segundos(0.4f);
            Vector3 inicio = jugador.transform.position;
            yield return Segundos(1f);
            float caminando = (jugador.transform.position - inicio).magnitude;

            // Un clic (y soltarlo) enciende correr.
            yield return Pulsar(controlIzq.primary2DAxisClick);
            Assert.IsTrue(adaptador.CorrerAlternado, "Un clic enciende correr.");
            yield return Segundos(0.4f);
            Assert.IsTrue(controlador.PidiendoCorrer, "Con el clic ya soltado sigue corriendo.");
            inicio = jugador.transform.position;
            yield return Segundos(1f);
            float corriendo = (jugador.transform.position - inicio).magnitude;

            // Otro clic lo apaga.
            yield return Pulsar(controlIzq.primary2DAxisClick);
            Assert.IsFalse(adaptador.CorrerAlternado, "Otro clic apaga correr.");
            yield return Segundos(0.4f);
            Assert.IsFalse(controlador.PidiendoCorrer);
            inicio = jugador.transform.position;
            yield return Segundos(1f);
            float otraVezCaminando = (jugador.transform.position - inicio).magnitude;
            Debug.Log("[VR] Alternar: caminando " + caminando.ToString("0.00") + " m/s, corriendo " + corriendo.ToString("0.00") + " m/s, apagado " + otraVezCaminando.ToString("0.00") + " m/s.");
            Assert.Less(caminando, 2.2f, "Sin clic camina (1.62 m/s).");
            Assert.Greater(corriendo, 3.5f, "Alternado corre (4.53 m/s).");
            Assert.Less(otraVezCaminando, 2.2f, "Apagado vuelve a caminar.");

            // Encendido y soltando el stick de mover se apaga solo.
            yield return Pulsar(controlIzq.primary2DAxisClick);
            Assert.IsTrue(adaptador.CorrerAlternado);
            Stick(false, Vector2.zero);
            yield return Segundos(AdaptadorVR.SegundosApagarCorrer + 0.3f);
            Assert.IsFalse(adaptador.CorrerAlternado, "Al soltar el stick de mover se apaga correr.");
            Assert.IsFalse(controlador.PidiendoCorrer);

            // Con MANTENER vuelve el clic sostenido de siempre.
            OpcionesVR.FijarCorrer(ModoCorrerVR.Mantener);
            yield return Cuadros(2);
            Assert.AreEqual(ModoCorrerVR.Mantener, adaptador.ModoCorrer);
            Mantener(controlIzq.primary2DAxisClick);
            yield return Cuadros(3);
            Assert.IsTrue(controlador.PidiendoCorrer, "Con MANTENER corre mientras se mantiene el clic.");
            Soltar(controlIzq.primary2DAxisClick);
            yield return Cuadros(3);
            Assert.IsFalse(controlador.PidiendoCorrer, "Con MANTENER, al soltar el clic deja de correr.");
        }

        [UnityTest]
        public IEnumerator OpcionesMuestranGiroVinetaYCorrerSoloEnVRYLosGuardan()
        {
            var raiz = new GameObject("PruebaOpcionesVR");
            extras.Add(raiz);
            Canvas canvas = KitUI.CrearCanvas(raiz.transform, "Canvas", 5);
            PanelOpcionesInicio panel = PanelOpcionesInicio.Crear(canvas.transform);
            panel.Refrescar();
            yield return null;
            Assert.IsTrue(panel.FilasVRVisibles, "En VR salen las filas de VR.");
            Assert.IsTrue(panel.GiroVR.gameObject.activeSelf);
            Assert.IsTrue(panel.VinetaVR.gameObject.activeSelf);
            Assert.IsTrue(panel.CorrerVR.gameObject.activeSelf);
            Assert.IsFalse(panel.Sensibilidad.gameObject.activeSelf, "En VR no sale la sensibilidad del ratón.");
            Assert.IsTrue(panel.VelocidadGiroVR.gameObject.activeSelf, "VR18: sale la VELOCIDAD DE GIRO.");
            Assert.AreEqual(PanelOpcionesInicio.TextosGiroVR[(int)ModoGiroVR.Suave], panel.GiroVR.TextoValor, "VR18: GIRO es SUAVE por defecto.");
            Assert.AreEqual(PanelOpcionesInicio.TextoVelocidadGiro(120f), panel.VelocidadGiroVR.TextoValor);
            Assert.AreEqual(PanelOpcionesInicio.TextosVinetaVR[(int)NivelVinetaVR.Baja], panel.VinetaVR.TextoValor, "VR26: VIÑETA es BAJA por defecto.");
            Assert.IsTrue(panel.RutaVR.gameObject.activeSelf, "Sale RUTA en VR.");
            Assert.IsTrue(panel.RutaVR.Activo, "RUTA: SÍ por defecto.");
            Assert.IsFalse(panel.InvertirY.gameObject.activeSelf, "En VR no sale INVERTIR EJE Y.");
            Assert.AreEqual(PanelOpcionesInicio.TextosCorrerVR[(int)ModoCorrerVR.Mantener], panel.CorrerVR.TextoValor);

            panel.GiroVR.Mover(1);
            panel.VelocidadGiroVR.Mover(-1);
            panel.VinetaVR.Mover(-1);
            panel.CorrerVR.Mover(1);
            panel.RutaVR.Mover(-1);
            Assert.IsFalse(OpcionesVR.Ruta, "RUTA → NO se guarda.");
            Assert.AreEqual(ModoGiroVR.PorPasos, OpcionesVR.Giro, "GIRO → POR PASOS se guarda.");
            Assert.AreEqual(120f - OpcionesVR.PasoVelocidadGiro, OpcionesVR.VelocidadGiro, 0.01f, "VELOCIDAD DE GIRO se guarda.");
            Assert.IsFalse(OpcionesVR.Vineta, "VIÑETA → NO se guarda.");
            Assert.AreEqual(ModoCorrerVR.Alternar, OpcionesVR.Correr, "CORRER → ALTERNAR se guarda.");
            OpcionesVR.Recargar();
            Assert.AreEqual(ModoGiroVR.PorPasos, OpcionesVR.Giro);
            Assert.AreEqual(120f - OpcionesVR.PasoVelocidadGiro, OpcionesVR.VelocidadGiro, 0.01f);
            Assert.IsFalse(OpcionesVR.Vineta);
            Assert.AreEqual(ModoCorrerVR.Alternar, OpcionesVR.Correr);

            // Sin VR las filas de VR no se ven y vuelve la sensibilidad.
            ModoVR.Fijar(false);
            panel.Refrescar();
            Assert.IsFalse(panel.FilasVRVisibles);
            Assert.IsFalse(panel.GiroVR.gameObject.activeSelf);
            Assert.IsTrue(panel.Sensibilidad.gameObject.activeSelf);
            ModoVR.Fijar(true, true);
        }

        [UnityTest]
        public IEnumerator RecentrarDejaLosOjosA172YLaCabezaAlFrente()
        {
            yield return ArmarVR();
            float frente = jugador.transform.eulerAngles.y;
            // Quien juega se agachó, se movió en el cuarto y volteó 70° a la derecha.
            PonerCabeza(new Vector3(0.3f, 1.3f, 0.2f), Quaternion.Euler(10f, 70f, 0f));
            yield return Cuadros(5);
            Camera cam = adaptador.Camara;
            float antes = Mathf.Abs(Mathf.DeltaAngle(YawPlano(cam.transform.forward), frente));
            Assert.Greater(antes, 30f, "Antes de recentrar la cabeza no mira al frente del cuerpo.");

            Assert.AreSame(adaptador, AdaptadorVR.Actual);
            AdaptadorVR.Actual.Recentrar();
            yield return Cuadros(3);

            float altura = cam.transform.position.y - jugador.transform.position.y;
            float despues = Mathf.DeltaAngle(YawPlano(cam.transform.forward), jugador.transform.eulerAngles.y);
            Debug.Log("[VR] Recentrar: altura " + altura.ToString("0.000") + " m, desvío " + despues.ToString("0.0") + "° (antes " + antes.ToString("0.0") + "°).");
            Assert.AreEqual(AdaptadorVR.AlturaOjos, altura, 0.03f, "Los ojos quedan a 1.72 m.");
            Assert.AreEqual(0f, despues, 2f, "La cabeza queda mirando al frente del cuerpo.");
            Assert.AreEqual(jugador.transform.position.x, cam.transform.position.x, 0.02f, "La cabeza sigue sobre la cápsula.");
            Assert.AreEqual(jugador.transform.position.z, cam.transform.position.z, 0.02f, "La cabeza sigue sobre la cápsula.");
        }
    }
}

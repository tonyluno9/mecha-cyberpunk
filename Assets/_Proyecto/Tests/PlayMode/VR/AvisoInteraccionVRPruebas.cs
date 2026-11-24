using System.Collections;
using MechaCyberpunk.CampoTiro;
using MechaCyberpunk.Core;
using MechaCyberpunk.Personaje.Interaccion;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.VR.Menus;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.VR
{
    /// <summary>
    /// V6: en VR el aviso de interactuar sale como un letrero sobre lo que se usa, de cara a la cabeza y con B en lugar de E;
    /// lejos, o si no se puede usar, no sale. Los carros dicen "NO DISPONIBLE EN VR". La terminal del campo de tiro se usa con B.
    /// </summary>
    public class AvisoInteraccionVRPruebas : BaseJugadorVR
    {
        private const string RutaCarro = "Assets/_Proyecto/Vehiculos/Prefabs/CarroViejo.prefab";

        // Un cubo de 1 m (sin collider, para no chocar) con un interactuable de prueba.
        private InteractuableDePrueba CrearInteractuable(Vector3 centro, string texto)
        {
            GameObject cubo = Caja(centro, Vector3.one);
            cubo.name = "InteractuablePruebaVR";
            Object.Destroy(cubo.GetComponent<Collider>());
            InteractuableDePrueba usable = cubo.AddComponent<InteractuableDePrueba>();
            usable.Texto = texto;
            usable.PuedeUsar = true;
            return usable;
        }

        private IEnumerator EsperarCandidato(InteraccionAPie interaccion, System.Func<bool> listo)
        {
            float fin = Time.time + 1.5f;
            while (!listo() && Time.time < fin)
            {
                yield return null;
            }
            yield return Cuadros(2);
        }

        private AvisoInteraccionVR Aviso()
        {
            AvisoInteraccionVR aviso = jugador.GetComponent<AvisoInteraccionVR>();
            Assert.IsNotNull(aviso, "El letrero se agrega solo al jugador cuando el rig VR queda armado.");
            return aviso;
        }

        private void AssertDeCaraALaCabeza(Transform letrero)
        {
            Vector3 hacia = letrero.position - adaptador.Camara.transform.position;
            hacia.y = 0f;
            Assert.Greater(Vector3.Dot(letrero.forward, hacia.normalized), 0.95f, "El letrero mira a la cabeza (se lee derecho).");
            Assert.Less(Mathf.Abs(letrero.forward.y), 0.01f, "El letrero va de pie.");
        }

        [UnityTest]
        public IEnumerator InteractuableCerca_LetreroSobreElConB_LejosDesaparece()
        {
            yield return ArmarVR();
            AvisoInteraccionVR aviso = Aviso();
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            Vector3 pies = jugador.transform.position;
            InteractuableDePrueba usable = CrearInteractuable(new Vector3(pies.x, 0.5f, pies.z + 1.5f), "Hablar");
            yield return EsperarCandidato(interaccion, () => ReferenceEquals(interaccion.InteractuableCandidato, usable));
            Assert.AreSame(usable, interaccion.InteractuableCandidato, "El cubo es el candidato de B.");

            Assert.IsTrue(aviso.Visible, "Cerca sale el letrero.");
            Assert.AreEqual("B · Hablar", aviso.Texto);
            Transform letrero = aviso.Letrero;
            Debug.Log("[VR] Letrero '" + aviso.Texto + "' en " + letrero.position.ToString("F2") + ", escala " + letrero.localScale.x.ToString("0.00") + ".");
            Assert.AreEqual(1f + ReglaAvisoInteraccionVR.MargenArriba, letrero.position.y, 0.05f, "Justo encima de los bounds del cubo (arriba en 1 m).");
            Assert.AreEqual(usable.transform.position.x, letrero.position.x, 0.05f);
            Assert.AreEqual(usable.transform.position.z, letrero.position.z, 0.05f);
            AssertDeCaraALaCabeza(letrero);
            Assert.IsFalse(letrero.IsChildOf(jugador.transform), "No cuelga del jugador (su cuerpo se dibuja solo como sombra).");
            Renderer dibujo = letrero.GetComponent<Renderer>();
            Assert.IsNotNull(dibujo);
            Assert.AreNotEqual(ShadowCastingMode.ShadowsOnly, dibujo.shadowCastingMode);

            // El texto que ya trae la tecla cambia la E por la B, sin tocar la E de las palabras.
            usable.Texto = "E: ENCARGO · Robo";
            yield return Cuadros(2);
            Assert.AreEqual("B: ENCARGO · Robo", aviso.Texto);

            // Lejos: ya no es candidato y el letrero se va.
            usable.transform.position = new Vector3(pies.x, 0.5f, pies.z + 12f);
            yield return Cuadros(4);
            Assert.IsNull(interaccion.InteractuableCandidato);
            Assert.IsFalse(aviso.Visible, "Lejos el letrero desaparece.");
        }

        [UnityTest]
        public IEnumerator InteractuableQueNoSePuedeUsar_NoSaleLetrero()
        {
            yield return ArmarVR();
            AvisoInteraccionVR aviso = Aviso();
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            Vector3 pies = jugador.transform.position;
            InteractuableDePrueba usable = CrearInteractuable(new Vector3(pies.x, 0.5f, pies.z + 1.5f), "TALLER BLOQUEADO");
            usable.PuedeUsar = false;
            yield return EsperarCandidato(interaccion, () => ReferenceEquals(interaccion.InteractuableCandidato, usable));
            Assert.AreSame(usable, interaccion.InteractuableCandidato);
            Assert.IsFalse(aviso.Visible, "Lo que no se puede usar no sale en el letrero (ese aviso ya va en el panel del HUD).");
            usable.PuedeUsar = true;
            yield return Cuadros(2);
            Assert.IsTrue(aviso.Visible);
            Assert.AreEqual("B · TALLER BLOQUEADO", aviso.Texto);
        }

        [UnityTest]
        public IEnumerator CarroCerca_LetreroDiceNoDisponibleEnVR()
        {
            yield return ArmarVR();
            AvisoInteraccionVR aviso = Aviso();
            GameObject carro = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaCarro), new Vector3(0f, 0f, 4f), Quaternion.identity);
            extras.Add(carro);
            yield return Segundos(0.8f);
            IMontable montable = null;
            foreach (IMontable m in Registro<IMontable>.Todos)
            {
                if (m is Component c && c.transform.IsChildOf(carro.transform))
                {
                    montable = m;
                }
            }
            Assert.IsNotNull(montable, "El carro se registra.");
            jugadorAPie.Colocar(new Pose(montable.PuntoSubida + new Vector3(-0.6f, 0.05f, 0f), Quaternion.LookRotation(Vector3.right)));
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            yield return EsperarCandidato(interaccion, () => interaccion.MontableCandidato != null);
            Assert.IsNotNull(interaccion.MontableCandidato, "El carro es el candidato de B.");
            Assert.IsTrue(aviso.Visible);
            Assert.AreEqual(MenusVR.TextoNoDisponible, aviso.Texto, "En VR los carros no se manejan.");
            Debug.Log("[VR] Letrero del carro en " + aviso.Letrero.position.ToString("F2") + " (subida en " + montable.PuntoSubida.ToString("F2") + ").");
            Assert.Greater(aviso.Letrero.position.y, carro.transform.position.y + 1.2f, "Sobre el techo del carro.");
            AssertDeCaraALaCabeza(aviso.Letrero);
        }

        [UnityTest]
        public IEnumerator TerminalDelCampoDeTiro_EnVRSeUsaConBYTieneLetrero()
        {
            yield return ArmarVR();
            AvisoInteraccionVR aviso = Aviso();
            GameObject sala = SalaCampoTiro.Construir(null, new Vector3(0f, 0f, 60f));
            extras.Add(sala);
            TerminalCampoTiro terminal = SalaCampoTiro.UltimaTerminal;
            Assert.IsNotNull(terminal);
            yield return null;
            CollectionAssert.Contains(Registro<IInteractuable>.Todos, terminal, "En VR la terminal es un interactuable (la B).");
            // La cámara del rig mira a +Z: el jugador queda detrás (en -Z) del punto de uso de la terminal.
            jugadorAPie.Colocar(new Pose(terminal.Posicion + new Vector3(0f, 0.05f, -1.2f), Quaternion.identity));
            InteraccionAPie interaccion = jugador.GetComponent<InteraccionAPie>();
            yield return EsperarCandidato(interaccion, () => ReferenceEquals(interaccion.InteractuableCandidato, terminal));
            Assert.AreSame(terminal, interaccion.InteractuableCandidato, "La terminal es el candidato de B.");
            Assert.IsTrue(aviso.Visible);
            Assert.AreEqual("B · " + TerminalCampoTiro.TextoInteractuar, aviso.Texto);
            Assert.IsFalse(terminal.LetreroVisible, "En VR no sale el letrero de la E (no se duplica).");
            Vector3 plano = aviso.Letrero.position - terminal.transform.position;
            plano.y = 0f;
            Assert.Less(plano.magnitude, 0.8f, "Sobre la terminal.");
            Assert.Greater(aviso.Letrero.position.y, terminal.transform.position.y + 1.1f, "Encima del panel.");
        }
    }
}

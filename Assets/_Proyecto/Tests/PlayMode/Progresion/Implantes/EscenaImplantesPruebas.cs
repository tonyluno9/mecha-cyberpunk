using System.Collections;
using System.Globalization;
using MechaCyberpunk.Core;
using MechaCyberpunk.Mecha.Pruebas;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Pruebas;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.UI.Clinica;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Progresion.Implantes
{
    /// <summary>T-193: lo que trae Pruebas_Implantes al arrancar. Las alturas de las cajas se comparan con los assets (salto base, tabla de implantes, stepOffset).</summary>
    public class EscenaImplantesPruebas : BaseImplantesPruebas
    {
        private const float DistanciaMinimaPandilla = 25f;
        private const float HoraInicialEsperada = 10f;
        private const float ToleranciaHora = 0.5f;
        private const float MetrosPista = 30f;
        private const int Munecos = 3;

        private static Collider Caja(string nombre)
        {
            GameObject objeto = GameObject.Find("Caja salto " + nombre);
            Assert.IsNotNull(objeto, "la escena trae la caja de salto " + nombre);
            return objeto.GetComponent<Collider>();
        }

        private static IPuntoAparicion Aparicion(string id)
        {
            foreach (IPuntoAparicion p in Registro<IPuntoAparicion>.Todos)
            {
                if (p.Id == id) return p;
            }
            Assert.Fail("la escena no trae el punto de aparición " + id);
            return null;
        }

        private static ILugar Lugar(IdLugar id)
        {
            foreach (ILugar l in Registro<ILugar>.Todos)
            {
                if (l.Id == id) return l;
            }
            Assert.Fail("la escena no trae el lugar " + id);
            return null;
        }

        [UnityTest]
        public IEnumerator Escena_TraeLaClinicaLaCamillaElEspejoYElSitioDePandilla()
        {
            yield return CargarEscena();

            Assert.AreEqual(1, Object.FindObjectsByType<MostradorClinica>(FindObjectsSortMode.None).Length, "un mostrador de la clínica");
            ILugar clinica = Lugar(IdLugar.Clinica);
            Assert.IsTrue(clinica.Horario.EstaAbierto(HoraInicialEsperada), "la clínica abre a las 10:00");
            Assert.IsFalse(clinica.Horario.EstaAbierto(HoraNoche), "la clínica cierra a las 21:00");
            Assert.IsTrue(clinica.Horario.EstaAbierto(8f) && !clinica.Horario.EstaAbierto(20f), "horario de 8:00 a 20:00");
            Assert.IsTrue(clinica.EstaAbierto, "empieza abierta");
            Assert.IsNotNull(Lugar(IdLugar.Casa), "hay un lugar Casa");

            IPuntoAparicion camilla = Aparicion("Clinica_Camilla");
            Assert.AreEqual(ModoJugador.APie, camilla.Modo, "la camilla despierta a pie");
            GameObject camillaCaja = GameObject.Find("Camilla");
            Assert.IsNotNull(camillaCaja, "hay una camilla (caja)");
            Assert.Less(Vector3.Distance(camilla.Pose.position, camillaCaja.transform.position), 4f, "la aparición queda junto a la camilla");
            IPuntoAparicion cama = Aparicion("Casa_Cama");
            Assert.AreEqual(ModoJugador.APie, cama.Modo);
            Assert.IsNotNull(Object.FindFirstObjectByType<EspejoJugador>(), "hay un espejo");

            GameObject pandilla = GameObject.Find("Sitio_Pandilla");
            Assert.IsNotNull(pandilla, "hay un sitio vacío Sitio_Pandilla");
            Assert.GreaterOrEqual(Vector3.Distance(pandilla.transform.position, mostrador.Posicion), DistanciaMinimaPandilla, "la pandilla queda lejos de la clínica");
            Assert.AreEqual(0, pandilla.transform.childCount + pandilla.GetComponents<Component>().Length - 1, "el sitio está vacío");

            Assert.AreEqual(HoraInicialEsperada, reloj.Hora, ToleranciaHora, "arranca de día, a las 10:00");
            Assert.IsFalse(reloj.EsDeNoche);
            Assert.AreEqual(EtapaPrologo.Libre, Registro<IHistoria>.Actual.Etapa, "historia libre");
            VerificarFinal("escena-clinica");
        }

        [UnityTest]
        public IEnumerator Escena_TraePistaCajasMunecosTallerYArena()
        {
            yield return CargarEscena();

            for (float metro = 0f; metro <= MetrosPista + 0.01f; metro += 5f)
            {
                Assert.IsNotNull(GameObject.Find("Marca " + metro.ToString("0", CultureInfo.InvariantCulture) + " m"), "marca de pista a " + metro + " m");
            }
            Assert.AreEqual(Munecos, Object.FindObjectsByType<MunecoAPie>(FindObjectsSortMode.None).Length, "tres muñecos");
            Assert.IsNotNull(Object.FindFirstObjectByType<TallerDePrueba>(), "hay taller con banco");
            Assert.IsNotNull(Object.FindFirstObjectByType<PuntoLlamadoDePrueba>(), "hay punto de llamado en la calle");
            bool hayMecha = false;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            {
                if (t.parent == null && t.name.StartsWith("MechaEstacionado")) hayMecha = true;
            }
            Assert.IsTrue(hayMecha, "hay un MechaEstacionado");
            Assert.IsNotNull(Object.FindFirstObjectByType<MostradorPeleas>(), "hay mostrador de peleas");
            VerificarFinal("escena-pista");
        }

        [UnityTest]
        public IEnumerator Escena_CajasDeSaltoCoherentesConLosAssets()
        {
            yield return CargarEscena();

            DatosPersonaje datos = AssetDatabase.LoadAssetAtPath<DatosPersonaje>(RutaDatosPersonaje);
            float stepOffset = AssetDatabase.LoadAssetAtPath<GameObject>(RutaJugadorPrefab).GetComponent<CharacterController>().stepOffset;
            float saltoBase = datos.AlturaSalto;
            float saltoI = saltoBase * Datos(EspacioImplante.Piernas, 1).Efectos.multiplicadorSalto;
            float saltoII = saltoBase * Datos(EspacioImplante.Piernas, 2).Efectos.multiplicadorSalto;
            float alturaBase = Caja("BASE").bounds.size.y;
            float alturaI = Caja("PIERNAS I").bounds.size.y;
            float alturaII = Caja("PIERNAS II").bounds.size.y;
            Debug.Log("[T-193] saltos base/I/II = " + saltoBase.ToString("0.###", CultureInfo.InvariantCulture) + "/" + saltoI.ToString("0.###", CultureInfo.InvariantCulture)
                + "/" + saltoII.ToString("0.###", CultureInfo.InvariantCulture) + " m; cajas = " + alturaBase.ToString("0.###", CultureInfo.InvariantCulture) + "/"
                + alturaI.ToString("0.###", CultureInfo.InvariantCulture) + "/" + alturaII.ToString("0.###", CultureInfo.InvariantCulture) + " m; stepOffset " + stepOffset);

            Assert.Greater(alturaBase, stepOffset, "la caja base no se sube caminando");
            Assert.Less(alturaBase, saltoBase, "la caja base se sube con el salto base");
            Assert.Greater(alturaI, saltoBase, "la caja de PIERNAS I no se sube con el salto base");
            Assert.Less(alturaI, saltoI, "la caja de PIERNAS I se sube con PIERNAS I");
            Assert.Greater(alturaII, saltoI, "la caja de PIERNAS II no se sube con PIERNAS I");
            Assert.Less(alturaII, saltoII, "la caja de PIERNAS II se sube con PIERNAS II");
            VerificarFinal("escena-cajas");
        }
    }
}

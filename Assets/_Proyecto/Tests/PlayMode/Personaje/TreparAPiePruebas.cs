using System.Collections;
using MechaCyberpunk.Core.Trepar;
using MechaCyberpunk.Personaje.Movimiento;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>S2: el jugador a pie se agarra de bordes, sube por una escalera hasta un techo y salta entre techos, con entrada simulada.</summary>
    public class TreparAPiePruebas : BaseJugadorAPie
    {
        private const float AlturaContenedor = 1.6f;
        private const float AlturaTecho = 6f;
        private const float SeparacionTechos = 2.5f;
        private const float XEscalera = 20f;
        private const float ZPared = 9.5f;
        private const float LargoTecho = 8f;
        private const float XPretil = -20f;
        private const float AlturaPretil = 1f;
        private const float MaxSegundos = 8f;
        private const float Tolerancia = 0.15f;

        private TreparAPie trepar;

        private void Teletransportar(Vector3 posicion)
        {
            CharacterController cc = jugador.GetComponent<CharacterController>();
            cc.enabled = false;
            jugador.transform.position = posicion;
            cc.enabled = true;
            controlador.ReiniciarMovimiento();
        }

        private void ArmarEscenaTechos()
        {
            trepar = jugador.GetComponent<TreparAPie>();
            Assert.IsNotNull(trepar, "El prefab JugadorAPie debe traer TreparAPie");
            // Contenedor de 1,6 m.
            Caja(new Vector3(0f, AlturaContenedor / 2f, 4f), new Vector3(4f, AlturaContenedor, 2f));
            // Pared de 6 m con techo caminable.
            Caja(new Vector3(XEscalera, AlturaTecho / 2f, ZPared + 0.5f), new Vector3(6f, AlturaTecho, 1f));
            Caja(new Vector3(XEscalera, AlturaTecho - 0.25f, ZPared + LargoTecho / 2f), new Vector3(6f, 0.5f, LargoTecho));
            // Segundo techo separado 2,5 m.
            float zFinTecho = ZPared + LargoTecho;
            Caja(new Vector3(XEscalera, AlturaTecho - 0.25f, zFinTecho + SeparacionTechos + LargoTecho / 2f), new Vector3(6f, 0.5f, LargoTecho));
            // Pretil delgado de 1 m.
            Caja(new Vector3(XPretil, AlturaPretil / 2f, 4f), new Vector3(3f, AlturaPretil, 0.15f));

            GameObject escalera = new GameObject("PuntoTrepar_Prueba");
            escalera.transform.position = new Vector3(XEscalera, 0f, ZPared);
            escalera.transform.rotation = Quaternion.identity;
            PuntoTrepar punto = escalera.AddComponent<PuntoTrepar>();
            SerializedObject so = new SerializedObject(punto);
            so.FindProperty("alturaSubida").floatValue = AlturaTecho;
            so.FindProperty("salida").vector3Value = new Vector3(0f, AlturaTecho, 0.8f);
            so.ApplyModifiedPropertiesWithoutUndo();
            punto.AjustarTrigger();
            Registrar(escalera);
        }

        private readonly System.Collections.Generic.List<GameObject> propios = new System.Collections.Generic.List<GameObject>();

        private void Registrar(GameObject objeto)
        {
            propios.Add(objeto);
        }

        public override void TearDown()
        {
            foreach (GameObject objeto in propios)
            {
                if (objeto != null)
                {
                    Object.Destroy(objeto);
                }
            }
            propios.Clear();
            base.TearDown();
        }

        private IEnumerator CorrerHastaZ(float z, float maxSegundos)
        {
            Press(teclado.wKey);
            yield return null;
            Press(teclado.leftShiftKey);
            yield return null;
            float fin = Time.time + maxSegundos;
            while (jugador.transform.position.z < z && Time.time < fin)
            {
                yield return null;
            }
        }

        private IEnumerator EsperarHasta(System.Func<bool> condicion, float maxSegundos)
        {
            float fin = Time.time + maxSegundos;
            while (!condicion() && Time.time < fin)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator Contenedor_CorrerYSaltar_TerminaParadoArriba()
        {
            yield return Calentar();
            ArmarEscenaTechos();
            yield return Esperar(0.3f);
            yield return CorrerHastaZ(2f, 3f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            yield return EsperarHasta(() => trepar.Trepando, MaxSegundos);
            Assert.IsTrue(trepar.Trepando, "Debe haberse agarrado del borde");
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
            yield return null;
            yield return EsperarHasta(() => !trepar.Trepando, MaxSegundos);
            yield return Esperar(0.5f);
            Assert.AreEqual(AlturaContenedor, jugador.transform.position.y, Tolerancia, "Debe quedar parado sobre el contenedor");
            Assert.IsTrue(controlador.EnPiso, "Debe estar en el piso del contenedor");
            Assert.Greater(jugador.transform.position.z, 3f, "Debe estar sobre el contenedor, no delante");
        }

        [UnityTest]
        public IEnumerator Escalera_ConW_SubeYTerminaSobreElTecho()
        {
            yield return Calentar();
            ArmarEscenaTechos();
            Teletransportar(new Vector3(XEscalera, 0.05f, ZPared - 3f));
            yield return Esperar(0.3f);
            Press(teclado.wKey);
            bool trepo = false;
            float fin = Time.time + MaxSegundos * 2f;
            while (Time.time < fin)
            {
                trepo |= trepar.Trepando;
                if (trepo && !trepar.Trepando && jugador.transform.position.y > AlturaTecho - Tolerancia)
                {
                    break;
                }
                yield return null;
            }
            Release(teclado.wKey);
            yield return Esperar(0.5f);
            Assert.IsTrue(trepo, "Debe haberse enganchado a la escalera");
            Assert.AreEqual(AlturaTecho, jugador.transform.position.y, Tolerancia);
            Assert.Greater(jugador.transform.position.z, ZPared + 0.5f, "Debe quedar sobre el techo, en la Salida");
        }

        [UnityTest]
        public IEnumerator Techo_CorrerYSaltarAlOtroTecho_TerminaSobreElSegundo()
        {
            yield return Calentar();
            ArmarEscenaTechos();
            float zFin = ZPared + LargoTecho;
            Teletransportar(new Vector3(XEscalera, AlturaTecho + 0.05f, ZPared + 2f));
            yield return Esperar(0.5f);
            float minimoY = jugador.transform.position.y;
            yield return CorrerHastaZ(zFin - 0.6f, 3f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            float fin = Time.time + 3f;
            while (Time.time < fin)
            {
                minimoY = Mathf.Min(minimoY, jugador.transform.position.y);
                if (jugador.transform.position.z > zFin + SeparacionTechos + 0.5f && controlador.EnPiso && !trepar.Trepando)
                {
                    break;
                }
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
            yield return null;
            yield return Esperar(0.5f);
            Assert.GreaterOrEqual(jugador.transform.position.y, AlturaTecho - 0.1f, "Debe terminar sobre el segundo techo");
            Assert.Greater(jugador.transform.position.z, zFin + SeparacionTechos, "Debe haber cruzado el hueco");
            Assert.GreaterOrEqual(minimoY, AlturaTecho - 0.1f, "No debe caer por debajo del techo");
        }

        [UnityTest]
        public IEnumerator Techo_Caminar_NoLoAtraviesa()
        {
            yield return Calentar();
            ArmarEscenaTechos();
            Teletransportar(new Vector3(XEscalera, AlturaTecho + 0.05f, ZPared + 1.5f));
            yield return Esperar(0.5f);
            float minimoY = float.MaxValue;
            Press(teclado.wKey);
            float fin = Time.time + 1f;
            while (Time.time < fin)
            {
                minimoY = Mathf.Min(minimoY, jugador.transform.position.y);
                yield return null;
            }
            Release(teclado.wKey);
            Assert.GreaterOrEqual(minimoY, AlturaTecho - 0.1f, "Caminar sobre el techo no lo atraviesa");
        }

        [UnityTest]
        public IEnumerator Pretil_Delgado_NoSeAgarra()
        {
            yield return Calentar();
            ArmarEscenaTechos();
            Teletransportar(new Vector3(XPretil, 0.05f, 0f));
            yield return Esperar(0.3f);
            float maximoY = 0f;
            yield return CorrerHastaZ(2.5f, 3f);
            Press(teclado.spaceKey);
            yield return null;
            Release(teclado.spaceKey);
            float fin = Time.time + 2f;
            while (Time.time < fin)
            {
                maximoY = Mathf.Max(maximoY, jugador.transform.position.y);
                yield return null;
            }
            Release(teclado.wKey);
            yield return null;
            Release(teclado.leftShiftKey);
            yield return null;
            Assert.Less(maximoY, AlturaPretil - 0.1f, "No debe subirse al pretil");
            Assert.Less(jugador.transform.position.y, 0.2f, "Debe seguir en el piso");
        }
    }
}

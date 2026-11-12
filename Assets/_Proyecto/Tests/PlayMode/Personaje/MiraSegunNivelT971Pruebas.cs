using System.Collections;
using MechaCyberpunk.Core;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje.Combate;
using MechaCyberpunk.Personaje.Jugador;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-971: la mira de la pistola en la mano solo se ve con la pistola en nivel III (la mejora Mira comprada).</summary>
    public class MiraSegunNivelT971Pruebas : BaseJugadorAPie
    {
        private const string RutaAgarre = "Assets/Arte/Armas/W01_Pistola/W01_Pistola_agarre.prefab";

        private void Comprar(int nivel)
        {
            ArsenalJugador arsenal = Registro<ArsenalJugador>.Actual;
            Registro<ServicioInventario>.Actual.Agregar(arsenal.Catalogo.Buscar(ArmaAPie.Pistola, nivel), 1);
        }

        [UnityTest]
        public IEnumerator Mira_ApagadaConLaPistolaEnNivel2_PrendidaEnNivel3()
        {
            yield return Calentar();
            MiraSegunNivel componente = jugador.GetComponentInChildren<MiraSegunNivel>(true);
            Assert.IsNotNull(componente, "El prefab del jugador trae MiraSegunNivel en la pistola de la mano");
            Assert.IsNotNull(componente.Mira, "Tiene su objeto Mira");
            Comprar(1);
            Comprar(2);
            componente.Revisar();
            Assert.AreEqual(2, componente.Nivel);
            Assert.IsFalse(componente.Mira.activeSelf, "Con el nivel II no hay mira");
            // Se saca la pistola (se prenden la mano y la pistola) y la mira se revisa sola.
            for (Transform t = componente.transform; t != null && t != jugador.transform; t = t.parent)
            {
                t.gameObject.SetActive(true);
            }
            Comprar(3);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(3, componente.Nivel, "Se entera sola del nuevo nivel");
            Assert.IsTrue(componente.Mira.activeSelf, "Con el nivel III la mira se ve");
        }

        [UnityTest]
        public IEnumerator Mira_SinArmasAPieEnLosPadres_SeQuedaApagada()
        {
            yield return Calentar();
            Comprar(1);
            Comprar(2);
            Comprar(3);
            GameObject piso = (GameObject)Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(RutaAgarre), new Vector3(50f, 1f, 50f), Quaternion.identity);
            yield return null;
            MiraSegunNivel componente = piso.GetComponent<MiraSegunNivel>();
            Assert.IsNotNull(componente);
            Assert.IsFalse(componente.Mira.activeSelf, "Sin ArmasAPie la mira sigue apagada aunque la pistola sea nivel III");
            Object.Destroy(piso);
        }
    }
}

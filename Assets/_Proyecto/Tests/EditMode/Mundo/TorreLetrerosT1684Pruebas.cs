using System.Linq;
using MechaCyberpunk.Mundo.Editor;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mundo
{
    /// <summary>
    /// T47 (T-1684): la torre de Nova en el recinto (más de 3 veces el edificio más alto, con collider y luz), al menos 20 letreros nuevos pegados a una pared
    /// con su luz, y la escena con menos de 1000 luces. Hay que correr TorreLetrerosT1684.Aplicar antes.
    /// </summary>
    public class TorreLetrerosT1684Pruebas
    {
        private GameObject raiz;

        [OneTimeSetUp]
        public void AbrirEscena()
        {
            EditorSceneManager.OpenScene(CreadorPrologoCiudad.RutaEscena, OpenSceneMode.Single);
            Physics.SyncTransforms();
            raiz = GameObject.Find(TorreLetrerosT1684.NombreRaiz);
        }

        [Test]
        public void Torre_Existe_MasDeTresVecesElEdificioMasAlto()
        {
            Assert.IsNotNull(raiz, "falta " + TorreLetrerosT1684.NombreRaiz);
            Transform torre = raiz.transform.Find(TorreLetrerosT1684.NombreTorre);
            Assert.IsNotNull(torre, "falta la torre");
            float alto = TorreLetrerosT1684.EdificioMasAlto();
            float torreAlto = TorreLetrerosT1684.AlturaDe(torre);
            Assert.Greater(alto, 50f, "no se midió el edificio más alto");
            Assert.Greater(torreAlto, 3f * alto, "torre " + torreAlto + " m, edificio más alto " + alto + " m");
        }

        [Test]
        public void Torre_EnElRecintoDeNova_ConColliderYLuz()
        {
            Transform torre = raiz.transform.Find(TorreLetrerosT1684.NombreTorre);
            Vector3 historia = GameObject.Find("Historia_Torre").transform.position;
            Bounds b = TorreLetrerosT1684.Caja(torre);
            float d = Vector2.Distance(new Vector2(historia.x, historia.z), new Vector2(Mathf.Clamp(historia.x, b.min.x, b.max.x), Mathf.Clamp(historia.z, b.min.z, b.max.z)));
            Assert.Less(d, 15f, "la torre debe quedar junto a Historia_Torre");
            Assert.IsTrue(FueraDelMuroT1543.LineaDelMuro().Contains(new Vector2(b.center.x, b.center.z)), "la torre debe quedar dentro del muro");
            Assert.IsTrue(torre.GetComponentsInChildren<BoxCollider>().Any(c => !c.isTrigger), "sin collider");
            Assert.IsTrue(torre.GetComponentsInChildren<Light>().Length > 0, "sin luz en la base");
            // No se atraviesa: un rayo a la altura del jugador contra la torre pega en ella.
            Vector3 o = new Vector3(historia.x, 1.5f, historia.z);
            Vector3 dir = new Vector3(b.center.x, 1.5f, b.center.z) - o;
            Assert.IsTrue(Physics.RaycastAll(o, dir.normalized, dir.magnitude + 5f).Any(h => h.collider.transform.IsChildOf(torre)), "el rayo no pega en la torre");
        }

        [Test]
        public void Letreros_AlMenosVeinte_PegadosAPared_ConLuz()
        {
            Transform padre = raiz.transform.Find(TorreLetrerosT1684.NombreLetreros);
            Assert.IsNotNull(padre);
            int pegados = 0;
            foreach (Transform l in padre)
            {
                Bounds b = TorreLetrerosT1684.Caja(l);
                Vector3 haciaPared = l.forward;
                Vector3 o = l.position - haciaPared * 1.5f;
                bool pared = Physics.Raycast(o, haciaPared, out RaycastHit hit, 2.2f, ~0, QueryTriggerInteraction.Ignore) && !hit.collider.transform.IsChildOf(l);
                float alto = b.min.y;
                Assert.IsTrue(pared, l.name + " no tiene pared detrás");
                Assert.Less(Mathf.Abs(hit.distance - 1.5f), 0.25f, l.name + " no está pegado (separación " + (hit.distance - 1.5f) + " m)");
                Assert.That(alto, Is.InRange(2.8f, 9f), l.name + " orilla de abajo a " + alto + " m");
                Assert.IsNotNull(l.GetComponentInChildren<Light>(), l.name + " sin luz");
                pegados++;
            }
            Assert.GreaterOrEqual(pegados, 20);
        }

        [Test]
        public void Escena_LucesTotales_MenosDeMil()
        {
            int n = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            Assert.Less(n, 1000, "luces totales " + n);
        }
    }
}

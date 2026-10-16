using MechaCyberpunk.UI.Cabina;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.Mecha
{
    /// <summary>La pantalla de calor de la cabina (T-067) se ve entera: ni fuera de cuadro ni tapada por el tablero.</summary>
    public class PantallaCalorVistaPruebas
    {
        private const string RutaPrefab = "Assets/_Proyecto/Mecha/Prefabs/MechaJugador.prefab";
        private const string NombreHueco = "Hueco";
        private const float Margen = 0.01f;

        private static Renderer BuscarRenderer(Transform raiz, string nombre)
        {
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == nombre)
                {
                    return r;
                }
            }
            return null;
        }

        [Test]
        public void Pantalla_Barra_QuedaDentroDeLaVistaYSobreElTablero()
        {
            GameObject mecha = PrefabUtility.LoadPrefabContents(RutaPrefab);
            try
            {
                Camera camara = mecha.GetComponentInChildren<Camera>(true);
                PantallaCalorCabina pantalla = mecha.GetComponentInChildren<PantallaCalorCabina>(true);
                Assert.IsNotNull(pantalla);
                RectTransform hueco = (RectTransform)pantalla.transform.Find(NombreHueco);
                Renderer tablero = BuscarRenderer(mecha.transform, "Tablero");
                Assert.IsNotNull(camara, "El mecha trae cámara");
                Assert.IsNotNull(hueco, "La pantalla trae el hueco de la barra");
                Assert.IsNotNull(tablero, "La cabina trae el tablero");

                // Sin escena, los renderers no tienen límites hasta que se toca la pieza: se usa el centro y tamaño del cubo.
                float alturaTablero = tablero.transform.position.y + tablero.transform.lossyScale.y / 2f;
                Vector3[] esquinas = new Vector3[4];
                hueco.GetWorldCorners(esquinas);
                foreach (Vector3 esquina in esquinas)
                {
                    Vector3 vista = camara.WorldToViewportPoint(esquina);
                    Assert.Greater(vista.z, 0f, "La barra está delante de la cámara");
                    Assert.That(vista.x, Is.InRange(Margen, 1f - Margen), "La barra cabe en horizontal");
                    Assert.That(vista.y, Is.InRange(Margen, 1f - Margen), "La barra cabe en vertical");
                    Assert.GreaterOrEqual(esquina.y, alturaTablero, "El tablero no tapa la barra");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(mecha);
            }
        }
    }
}

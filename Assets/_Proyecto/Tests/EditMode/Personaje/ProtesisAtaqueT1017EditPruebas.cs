using System.IO;
using MechaCyberpunk.Economia;
using MechaCyberpunk.Personaje;
using MechaCyberpunk.Personaje.Habilidades;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Personaje
{
    /// <summary>T-1017: las prótesis de ataque del Doc existen, se guardan con la partida y tienen sus teclas [4] y [5].</summary>
    public class ProtesisAtaqueT1017EditPruebas
    {
        [Test]
        public void Catalogo_BrazoLaserYHombreraConPrecioTiendaYEnElCatalogoDeObjetos()
        {
            CatalogoProtesisAtaque c = AssetDatabase.LoadAssetAtPath<CatalogoProtesisAtaque>("Assets/_Proyecto/Personaje/Datos/Resources/CatalogoProtesisAtaque.asset");
            Assert.IsNotNull(c);
            Assert.IsNotNull(c.Tienda, "Hay tienda del Doc");
            Assert.AreEqual(2, c.Tienda.Ofertas.Count);
            CatalogoObjetos objetos = AssetDatabase.LoadAssetAtPath<CatalogoObjetos>("Assets/_Proyecto/Economia/Datos/CatalogoObjetos_Base.asset");
            foreach (HabilidadProtesis h in new[] { HabilidadProtesis.Laser, HabilidadProtesis.Misiles })
            {
                DatosProtesisAtaque p = c.De(h);
                Assert.IsNotNull(p, h + " tiene prótesis");
                Assert.AreEqual(TipoObjeto.Protesis, p.Tipo);
                Assert.AreEqual(1, p.Maximo, "Se compra una vez");
                Assert.Greater(p.Oferta.Precio, 0, "Tiene precio");
                Assert.AreSame(p, objetos.Buscar(p.Id), "Está en el catálogo de objetos: se guarda con la partida");
                Assert.IsTrue(HabilidadesProtesis.EsDeAtaque(h));
            }
        }

        [Test]
        public void Entrada_LaserEnEl4YMisilesEnEl5()
        {
            string json = File.ReadAllText("Assets/_Proyecto/Personaje/Entrada/ControlesAPie.inputactions");
            Assert.AreEqual("HabilidadLaser", ConfigHabilidadesProtesis.NombreAccion(HabilidadProtesis.Laser));
            Assert.AreEqual("HabilidadMisiles", ConfigHabilidadesProtesis.NombreAccion(HabilidadProtesis.Misiles));
            StringAssert.Contains("\"path\": \"<Keyboard>/4\"", json);
            StringAssert.Contains("\"path\": \"<Keyboard>/5\"", json);
            StringAssert.Contains("\"action\": \"HabilidadLaser\"", json);
            StringAssert.Contains("\"action\": \"HabilidadMisiles\"", json);
        }
    }
}

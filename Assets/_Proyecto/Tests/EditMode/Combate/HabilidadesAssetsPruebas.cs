using System.Linq;
using MechaCyberpunk.Combate.Habilidades;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests
{
    public class HabilidadesAssetsPruebas
    {
        private const string Carpeta = "Assets/_Proyecto/Combate/Datos/Habilidades/";

        private static DatosHabilidad Cargar(string archivo)
        {
            var h = AssetDatabase.LoadAssetAtPath<DatosHabilidad>($"{Carpeta}DatosHabilidad_{archivo}.asset");
            Assert.NotNull(h, archivo);
            return h;
        }

        [Test]
        public void Son8ConIdUnico()
        {
            string[] archivos = { "Misil", "Salva", "Lanzallamas", "OndaDeCalor", "Descarga", "Escudo", "Martillo", "Embestida" };
            var todas = archivos.Select(Cargar).ToList();
            Assert.AreEqual(8, todas.Select(h => h.Id).Distinct().Count());
            Assert.IsTrue(todas.All(h => h.Nombre == h.Nombre.ToUpperInvariant()));
        }

        [Test]
        public void Misil_Salva_Proyectiles()
        {
            DatosHabilidad m = Cargar("Misil");
            Assert.AreEqual("MISIL", m.Nombre);
            Assert.AreEqual(TipoEfectoHabilidad.Proyectil, m.Efecto);
            Assert.AreEqual(20f, m.Velocidad); Assert.AreEqual(25f, m.Alcance); Assert.AreEqual(20f, m.Danio);
            Assert.AreEqual(1.2f, m.RadioExplosion, 0.0001f); Assert.AreEqual(8f, m.Enfriamiento); Assert.AreEqual(1, m.Cantidad);
            DatosHabilidad s = Cargar("Salva");
            Assert.AreEqual("SALVA", s.Nombre);
            Assert.AreEqual(TipoEfectoHabilidad.Proyectil, s.Efecto);
            Assert.AreEqual(3, s.Cantidad); Assert.AreEqual(6f, s.Dispersion); Assert.AreEqual(8f, s.Danio);
            Assert.AreEqual(0.8f, s.RadioExplosion, 0.0001f); Assert.AreEqual(10f, s.Enfriamiento);
        }

        [Test]
        public void Conos_ConLosValoresDeLaTabla()
        {
            DatosHabilidad l = Cargar("Lanzallamas");
            Assert.AreEqual(TipoEfectoHabilidad.Cono, l.Efecto);
            Assert.AreEqual(4f, l.AlcanceCono); Assert.AreEqual(60f, l.Angulo); Assert.AreEqual(14f, l.Danio); Assert.AreEqual(6f, l.Enfriamiento);
            DatosHabilidad o = Cargar("OndaDeCalor");
            Assert.AreEqual(TipoEfectoHabilidad.Cono, o.Efecto);
            Assert.AreEqual(3f, o.AlcanceCono); Assert.AreEqual(360f, o.Angulo); Assert.AreEqual(15f, o.Danio);
            Assert.AreEqual(1.5f, o.Empuje); Assert.AreEqual(9f, o.Enfriamiento);
            DatosHabilidad d = Cargar("Descarga");
            Assert.AreEqual(TipoEfectoHabilidad.Cono, d.Efecto);
            Assert.AreEqual(3.5f, d.AlcanceCono); Assert.AreEqual(30f, d.Angulo); Assert.AreEqual(6f, d.Danio);
            Assert.AreEqual(1f, d.Aturdir); Assert.AreEqual(5f, d.Enfriamiento);
            DatosHabilidad ma = Cargar("Martillo");
            Assert.AreEqual(TipoEfectoHabilidad.Cono, ma.Efecto);
            Assert.AreEqual(2.8f, ma.AlcanceCono, 0.0001f); Assert.AreEqual(40f, ma.Angulo); Assert.AreEqual(16f, ma.Danio);
            Assert.IsTrue(ma.RompeGuardia); Assert.AreEqual(6f, ma.Enfriamiento);
        }

        [Test]
        public void Escudo_Embestida_ConLosValoresDeLaTabla()
        {
            DatosHabilidad e = Cargar("Escudo");
            Assert.AreEqual(TipoEfectoHabilidad.Escudo, e.Efecto);
            Assert.AreEqual(0.5f, e.Reduccion); Assert.AreEqual(3f, e.Duracion); Assert.AreEqual(8f, e.Enfriamiento);
            DatosHabilidad b = Cargar("Embestida");
            Assert.AreEqual(TipoEfectoHabilidad.Embestida, b.Efecto);
            Assert.AreEqual(5f, b.Distancia); Assert.AreEqual(0.3f, b.DuracionEmbestida, 0.0001f);
            Assert.AreEqual(14f, b.Danio); Assert.AreEqual(2f, b.Empuje); Assert.AreEqual(7f, b.Enfriamiento);
        }
    }
}

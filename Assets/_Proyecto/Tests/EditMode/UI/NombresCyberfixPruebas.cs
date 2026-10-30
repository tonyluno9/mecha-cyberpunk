using MechaCyberpunk.Core;
using MechaCyberpunk.UI.Mapa;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MechaCyberpunk.Tests.UI
{
    /// <summary>T-414: los nombres del documento del juego (SUBURBIOS, SKULLS, SNAKES, EL MERCADO, I+D+I, EL FOX, VIVACOIN) y los Biits.</summary>
    public class NombresCyberfixPruebas
    {
        private static readonly string[] Rutas =
        {
            "Assets/_Proyecto/Progresion/Datos/Territorio/DatosZona_fosa.asset",
            "Assets/_Proyecto/Progresion/Datos/Territorio/DatosZona_tianguis.asset",
            "Assets/_Proyecto/Progresion/Datos/Territorio/DatosZona_neon.asset",
            "Assets/_Proyecto/Mundo/Datos/DatosLugar_Bar.asset",
            "Assets/_Proyecto/Mundo/Datos/DatosLugar_Casino.asset",
            "Assets/_Proyecto/Mundo/Datos/DatosLugar_MercadoNegro.asset",
            "Assets/_Proyecto/Mundo/Datos/DatosLugar_Clinica.asset",
            "Assets/_Proyecto/Economia/Datos/DatosTienda_MercadoNegro.asset",
            "Assets/_Proyecto/UI/Datos/ConfigPantallaTienda_Base.asset",
            "Assets/_Proyecto/UI/Datos/ConfigPantallaClinica_Base.asset",
            "Assets/_Proyecto/UI/Datos/ConfigHudAPie_Base.asset",
            "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Calaveras.asset",
            "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeCalaveras.asset",
            "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_Viboras.asset",
            "Assets/_Proyecto/IA/Datos/DatosPandilleroAPie_JefeViboras.asset",
        };

        private static readonly string[] NombresViejos =
        {
            "LA FOSA", "CALAVERA", "VÍBORA", "VIBORA", "MERCADO NEGRO", "Mercado negro", "Mercado Negro", "CLÍNICA", "Clínica", "El Foco", "Casino Aurora",
        };

        [Test]
        public void DatosYMoneda()
        {
            foreach (string ruta in Rutas)
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(ruta);
                Assert.IsNotNull(asset, "Existe " + ruta);
                SerializedObject so = new SerializedObject(asset);
                SerializedProperty p = so.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.String || p.name == "m_Name")
                    {
                        continue;
                    }
                    foreach (string viejo in NombresViejos)
                    {
                        StringAssert.DoesNotContain(viejo, p.stringValue, ruta + " > " + p.propertyPath);
                    }
                }
            }

            Assert.AreEqual("SUBURBIOS", new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(Rutas[0])).FindProperty("nombre").stringValue);
            Assert.AreEqual("El Fox", new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(Rutas[3])).FindProperty("nombre").stringValue);
            Assert.AreEqual("VivaCoin", new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(Rutas[4])).FindProperty("nombre").stringValue);
            Assert.AreEqual("El Mercado", new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(Rutas[5])).FindProperty("nombre").stringValue);
            Assert.AreEqual("I+D+I", new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>(Rutas[6])).FindProperty("nombre").stringValue);

            Assert.AreEqual("Ƀ0", Moneda.Texto(0));
            Assert.AreEqual("Ƀ340", Moneda.Texto(340));
            Assert.AreEqual("Ƀ1,250", Moneda.Texto(1250));
            Assert.AreEqual("-Ƀ150", Moneda.Texto(-150));
            Assert.AreEqual("SKULLS", FormatoTerritorio.NombreDe(IdPandilla.Calaveras));
            Assert.AreEqual("SNAKES", FormatoTerritorio.NombreDe(IdPandilla.Viboras));

            Font fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fuente.RequestCharactersInTexture(Moneda.Simbolo);
            Assert.IsTrue(fuente.HasCharacter(Moneda.Simbolo[0]), "La fuente integrada dibuja " + Moneda.Simbolo);
        }
    }
}

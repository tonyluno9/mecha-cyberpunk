using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion.Opciones;
using NUnit.Framework;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-411: las reglas de las opciones recortan lo que se sale de rango, el JSON va y vuelve igual y el texto roto da lo de por defecto.</summary>
    public class ReglaOpcionesPruebas
    {
        private const float Tolerancia = 0.0001f;

        [Test]
        public void LimitesYArchivo()
        {
            // Por defecto: 1x, Y normal, 80 % salvo la Música (silencio), MEDIO (T-978), nativa y pantalla completa.
            ValoresOpciones defecto = ReglaOpciones.PorDefecto();
            Assert.AreEqual(1f, defecto.sensibilidad, Tolerancia);
            Assert.IsFalse(defecto.invertirY);
            Assert.AreEqual(ReglaOpciones.CantidadSecciones, defecto.volumenes.Length);
            Assert.AreEqual(0.8f, defecto.volumenes[(int)SeccionSonido.General], Tolerancia);
            Assert.AreEqual(0.8f, defecto.volumenes[(int)SeccionSonido.Efectos], Tolerancia);
            Assert.AreEqual(0.8f, defecto.volumenes[(int)SeccionSonido.Ambiente], Tolerancia, "T-442: el ambiente suena por defecto");
            Assert.AreEqual(0f, defecto.volumenes[(int)SeccionSonido.Musica], Tolerancia);
            Assert.AreEqual(CalidadGrafica.Media, defecto.calidad, "T-978: MEDIO por defecto");
            Assert.AreEqual(0, defecto.resolucionAncho);
            Assert.IsTrue(defecto.pantallaCompleta);

            // Fuera de rango se recorta (y al paso de 5 %).
            ValoresOpciones raros = new ValoresOpciones
            {
                sensibilidad = 9f,
                volumenes = new[] { 1.7f, -0.3f, 0.52f },
                calidad = (CalidadGrafica)7,
                resolucionAncho = -5,
                resolucionAlto = 720,
            };
            ReglaOpciones.Recortar(raros);
            Assert.AreEqual(ReglaOpciones.SensibilidadMaxima, raros.sensibilidad, Tolerancia);
            Assert.AreEqual(ReglaOpciones.SensibilidadMinima, ReglaOpciones.RecortarSensibilidad(0.01f), Tolerancia);
            Assert.AreEqual(ReglaOpciones.CantidadSecciones, raros.volumenes.Length, "Las secciones que faltan se llenan");
            Assert.AreEqual(1f, raros.volumenes[0], Tolerancia);
            Assert.AreEqual(0f, raros.volumenes[1], Tolerancia);
            Assert.AreEqual(0.5f, raros.volumenes[2], Tolerancia);
            Assert.AreEqual(0.8f, raros.volumenes[(int)SeccionSonido.Interfaz], Tolerancia);
            Assert.AreEqual(CalidadGrafica.Media, raros.calidad);
            Assert.AreEqual(0, raros.resolucionAncho, "Una resolución inválida vuelve a la nativa");
            Assert.AreEqual(0, raros.resolucionAlto);

            // Ida y vuelta.
            ValoresOpciones propios = ReglaOpciones.PorDefecto();
            propios.sensibilidad = 2.35f;
            propios.invertirY = true;
            propios.volumenes[(int)SeccionSonido.General] = 0.6f;
            propios.volumenes[(int)SeccionSonido.Voces] = 0.15f;
            propios.calidad = CalidadGrafica.Baja;
            propios.resolucionAncho = 2560;
            propios.resolucionAlto = 1440;
            propios.pantallaCompleta = false;
            Assert.IsTrue(ReglaOpciones.DeJson(ReglaOpciones.AJson(propios), out ValoresOpciones leidos));
            Assert.AreEqual(propios.sensibilidad, leidos.sensibilidad, Tolerancia);
            Assert.AreEqual(propios.invertirY, leidos.invertirY);
            CollectionAssert.AreEqual(propios.volumenes, leidos.volumenes);
            Assert.AreEqual(propios.calidad, leidos.calidad);
            Assert.AreEqual(2560, leidos.resolucionAncho);
            Assert.AreEqual(1440, leidos.resolucionAlto);
            Assert.IsFalse(leidos.pantallaCompleta);

            // Texto roto o vacío: false y lo de por defecto.
            Assert.IsFalse(ReglaOpciones.DeJson("{ esto no es json", out ValoresOpciones roto));
            Assert.AreEqual(1f, roto.sensibilidad, Tolerancia);
            Assert.AreEqual(0.8f, roto.volumenes[(int)SeccionSonido.General], Tolerancia);
            Assert.IsFalse(ReglaOpciones.DeJson(string.Empty, out ValoresOpciones vacio));
            Assert.AreEqual(CalidadGrafica.Media, vacio.calidad);

            // Calidad: BAJA 0.75 / 40 m / MSAA 1; ALTA 1 / 120 m / 4.
            Assert.AreEqual(0.75f, ReglaOpciones.EscalaRender(CalidadGrafica.Baja), Tolerancia);
            Assert.AreEqual(40f, ReglaOpciones.DistanciaSombras(CalidadGrafica.Baja), Tolerancia);
            Assert.AreEqual(1, ReglaOpciones.Msaa(CalidadGrafica.Baja));
            Assert.AreEqual(4, ReglaOpciones.Msaa(CalidadGrafica.Alta));

            // T-978: BAJO dibuja hasta 150 m, sin reflejos de charcos ni postproceso; MEDIO hasta 300 m con todo; ALTO hasta el plano lejano (0).
            Assert.AreEqual(150f, ReglaOpciones.DistanciaDibujo(CalidadGrafica.Baja), Tolerancia);
            Assert.AreEqual(300f, ReglaOpciones.DistanciaDibujo(CalidadGrafica.Media), Tolerancia);
            Assert.AreEqual(0f, ReglaOpciones.DistanciaDibujo(CalidadGrafica.Alta), Tolerancia);
            Assert.IsFalse(ReglaOpciones.ReflejosCharcos(CalidadGrafica.Baja));
            Assert.IsTrue(ReglaOpciones.ReflejosCharcos(CalidadGrafica.Media));
            Assert.IsTrue(ReglaOpciones.ReflejosCharcos(CalidadGrafica.Alta));
            Assert.IsFalse(ReglaOpciones.Postproceso(CalidadGrafica.Baja));
            Assert.IsTrue(ReglaOpciones.Postproceso(CalidadGrafica.Media));
            Assert.IsTrue(ReglaOpciones.Postproceso(CalidadGrafica.Alta));

            // T-978: "-calidad Bajo" en la línea de comandos.
            Assert.IsTrue(ReglaOpciones.CalidadDeArgumentos(new[] { "CyberCity.exe", "-calidad", "Bajo" }, out CalidadGrafica deArgs));
            Assert.AreEqual(CalidadGrafica.Baja, deArgs);
            Assert.IsTrue(ReglaOpciones.CalidadDeArgumentos(new[] { "-CALIDAD", "alta" }, out deArgs));
            Assert.AreEqual(CalidadGrafica.Alta, deArgs);
            Assert.IsFalse(ReglaOpciones.CalidadDeArgumentos(new[] { "-calidad" }, out _), "Sin valor no cuenta");
            Assert.IsFalse(ReglaOpciones.CalidadDeArgumentos(new[] { "-calidad", "ultra" }, out _));
        }
    }
}

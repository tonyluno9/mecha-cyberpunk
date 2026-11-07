using System;
using MechaCyberpunk.Core;
using MechaCyberpunk.Progresion;
using MechaCyberpunk.Progresion.Prologo;
using MechaCyberpunk.Progresion.Prologo.Pandillas;
using NUnit.Framework;
using UnityEditor;

namespace MechaCyberpunk.Tests.Progresion
{
    /// <summary>T-716: en todo paso de la historia hay un objetivo con una frase corta y un destino que sale de la escena o de un lugar de Core.</summary>
    public class ObjetivoSiempreT716Pruebas
    {
        [Test]
        public void HistoriaDeLaDemo_TodoPasoTieneObjetivoCorto_MenosElFinal()
        {
            foreach (PasoHistoria paso in Enum.GetValues(typeof(PasoHistoria)))
            {
                bool esFinal = paso == PasoHistoria.Continuara || paso == PasoHistoria.Libre;
                Assert.AreEqual(!esFinal, DirectorPandillas.LlevaObjetivo(paso), paso + ": solo Continuara y Libre quedan sin objetivo");
                if (esFinal)
                {
                    continue;
                }
                string texto = DirectorPandillas.TextoDelPaso(paso);
                Assert.IsNotEmpty(texto, paso + " sin frase");
                Assert.LessOrEqual(texto.Length, DirectorPandillas.LargoMaximoObjetivo, paso + ": frase demasiado larga para el HUD");
            }
        }

        [Test]
        public void HistoriaDeLaDemo_LosPasosNuevosYViejosEstanCubiertos()
        {
            // Si alguien agrega un paso a la historia sin frase, esta prueba lo avisa: cada valor tiene que resolverse.
            int conObjetivo = 0;
            foreach (PasoHistoria paso in Enum.GetValues(typeof(PasoHistoria)))
            {
                if (DirectorPandillas.LlevaObjetivo(paso)) conObjetivo++;
            }
            Assert.AreEqual(Enum.GetValues(typeof(PasoHistoria)).Length - 2, conObjetivo);
        }

        [Test]
        public void HistoriaDeLaDemo_ElRespaldoDelDestinoEsUnLugarDeCore_NoUnaCoordenada()
        {
            Assert.AreNotEqual(IdLugar.Ninguno, DirectorPandillas.LugarRespaldo);
            Objetivo respaldo = new Objetivo(DirectorPandillas.TextoSalir, DirectorPandillas.LugarRespaldo);
            Assert.IsFalse(respaldo.TienePosicion, "El respaldo apunta a un lugar, no a una posición fija");
        }

        [Test]
        public void Prologo_TodaEtapaDeLaNocheYDelPresente_TieneObjetivoSalvoLasQueLoPonenOtros()
        {
            foreach (EtapaPrologo etapa in Enum.GetValues(typeof(EtapaPrologo)))
            {
                if (!MaquinaPrologo.Maneja(etapa))
                {
                    continue;
                }
                MaquinaPrologo maquina = new MaquinaPrologo("Ve al carro", "Vuelve a casa", "Ve a la cama", 2, "Ve al clandestino", "Habla con el encargado", "Gana tu pelea");
                maquina.Iniciar(etapa, false);
                bool loPoneOtro = etapa == EtapaPrologo.Emboscada // la pone la emboscada
                    || etapa == EtapaPrologo.Flashback // otra escena
                    || etapa == EtapaPrologo.Despertar // instante: avanza a PeleaCasera en el mismo cuadro
                    || etapa == EtapaPrologo.Continuara; // pantalla de fin
                ObjetivoPrologo objetivo = maquina.ObjetivoActual;
                if (loPoneOtro)
                {
                    continue;
                }
                Assert.AreNotEqual(DestinoObjetivo.Ninguno, objetivo.Destino, etapa + " sin destino");
                Assert.IsNotEmpty(objetivo.Texto, etapa + " sin frase");
            }
        }

        [Test]
        public void Misiones_TodoPasoDeCadaMisionDelCatalogoTieneFrase()
        {
            string[] guids = AssetDatabase.FindAssets("t:DatosMision");
            Assert.Greater(guids.Length, 0, "No hay misiones");
            foreach (string guid in guids)
            {
                DatosMision mision = AssetDatabase.LoadAssetAtPath<DatosMision>(AssetDatabase.GUIDToAssetPath(guid));
                foreach (PasoMision paso in mision.Pasos)
                {
                    if (paso.Tipo == TipoPaso.Escapar) continue; // escapar no lleva punto, pero sí frase
                    Assert.IsNotEmpty(paso.Texto, mision.Id + ": paso sin frase");
                    if (paso.Tipo == TipoPaso.IrA || paso.Tipo == TipoPaso.Recoger || paso.Tipo == TipoPaso.Entregar)
                    {
                        Assert.IsNotEmpty(paso.IdPunto, mision.Id + ": paso " + paso.Tipo + " sin punto de misión de la escena");
                    }
                }
            }
        }
    }
}

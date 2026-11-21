using System;

namespace MechaCyberpunk.Personaje.Combate
{
    /// <summary>Lo que debe sonar en un cuadro del rifle de plasma (se pueden sumar varios).</summary>
    [Flags]
    public enum EventoSonidoPlasma
    {
        Ninguno = 0,
        /// <summary>Se apretó el gatillo (ARM_W07_GATILLO).</summary>
        Gatillo = 1,
        /// <summary>Terminó la carga y sale el rayo (ARM_W07_DISPARO + el bucle del rayo y su capa).</summary>
        Disparo = 2,
        /// <summary>Se terminó de cargar y no había celda (ARM_W07_SIN_ENERGIA).</summary>
        SinEnergia = 4,
        /// <summary>Se acabó el rayo (ARM_W07_ENFRIAR).</summary>
        Enfriar = 8,
    }

    /// <summary>
    /// T-1532: el flujo de sonido del plasma W07 por estados (lógica pura). GATILLO al apretar, CARGA en bucle mientras carga (lo maneja quien llama
    /// con el estado), DISPARO al terminar la carga, RAYO_LOOP + RAYO_CAPA_EXTRA en bucle mientras dura el rayo, ENFRIAR al acabar el rayo, y
    /// SIN_ENERGIA si la carga termina sin celda.
    /// </summary>
    public static class ReglaSonidoPlasma
    {
        /// <summary>Los eventos de un cuadro según el estado de antes y el de después, si empezó el rayo y si faltó energía.</summary>
        public static EventoSonidoPlasma Evaluar(EstadoPlasma antes, EstadoPlasma despues, bool empezoRayo, bool sinEnergia)
        {
            EventoSonidoPlasma e = EventoSonidoPlasma.Ninguno;
            bool arrancoElCuadro = antes == EstadoPlasma.Reposo && (despues == EstadoPlasma.Cargando || empezoRayo || sinEnergia);
            if (arrancoElCuadro)
            {
                e |= EventoSonidoPlasma.Gatillo;
            }
            if (empezoRayo)
            {
                e |= EventoSonidoPlasma.Disparo;
            }
            if (sinEnergia)
            {
                e |= EventoSonidoPlasma.SinEnergia;
            }
            if (antes == EstadoPlasma.Rayo && despues == EstadoPlasma.Reposo)
            {
                e |= EventoSonidoPlasma.Enfriar;
            }
            return e;
        }

        /// <summary>true si en ese estado deben sonar los bucles de la carga.</summary>
        public static bool SuenaCarga(EstadoPlasma estado) => estado == EstadoPlasma.Cargando;

        /// <summary>true si en ese estado deben sonar el bucle del rayo y su capa extra.</summary>
        public static bool SuenaRayo(EstadoPlasma estado) => estado == EstadoPlasma.Rayo;

        /// <summary>Los IDs que suenan en bucle mientras dura el rayo (el principal y su capa extra).</summary>
        public static string[] IdsBucleRayo() => new[] { SonidosArmasT1472.PlasmaRayo, SonidosArmasT1472.PlasmaCapaRayo };
    }
}

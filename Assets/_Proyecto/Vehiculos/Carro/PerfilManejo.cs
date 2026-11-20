using System;
using System.Collections.Generic;

namespace MechaCyberpunk.Vehiculos.Carro
{
    /// <summary>T-1488: tipo de vehículo para el manejo (peso, aceleración, frenos, dirección y suspensión).</summary>
    public enum TipoManejo
    {
        Compacto,
        Sedan,
        Camioneta,
        Deportivo,
        Pesado,
        Moto,
    }

    /// <summary>
    /// T-1488: valores de manejo de un tipo de vehículo. Tabla única: <see cref="Para"/> da el perfil y <see cref="Campos"/> lo convierte en los campos
    /// serializados de ConfigCarro (los escribe el método de editor CrearPerfilesManejoT1488). La rigidez sale de la masa (para que el
    /// carro se hunda <see cref="CaidaReposo"/> m en reposo, como las ruedas ya colocadas) y el amortiguamiento de la relación Zeta.
    /// </summary>
    public readonly struct PerfilManejo
    {
        /// <summary>Cuánto se hunde el carro en reposo (m): es lo que dejan los prefabs (largoMaximo - largoReposo).</summary>
        public const float CaidaReposo = 0.15f;

        public readonly TipoManejo Tipo;
        public readonly float Masa, AlturaCentroMasa, FuerzaAntivuelco;
        public readonly float VelocidadMaxima, VelocidadReversa, FuerzaMotor, FuerzaReversa, FuerzaFreno, FuerzaFrenoMano, ResistenciaRodadura;
        public readonly float AnguloParado, AnguloMaximo, ExponenteDireccion, DistanciaEntreEjes;
        public readonly float AceleracionLateral, AgarreLateral, RespuestaGiro, VelocidadVolante, VelocidadCentradoVolante;
        public readonly float Zeta, FactorRebote, FactorCabeceo, FactorBalanceo, GiroFrenoMano, GananciaContravolante;
        public readonly float AntivuelcoBajaVelocidad, AmortiguacionInclinacion, Marchas;

        private PerfilManejo(TipoManejo tipo, float masa, float cm, float antivuelco,
            float vMax, float vReversa, float motor, float reversa, float freno, float frenoMano, float rodadura,
            float angParado, float angMax, float expDir, float entreEjes,
            float aLat, float agarre, float respGiro, float volante, float centrado,
            float zeta, float rebote, float cabeceo, float balanceo, float giroMano, float contravolante,
            float antivuelcoLento, float amortInclinacion, float marchas)
        {
            Tipo = tipo; Masa = masa; AlturaCentroMasa = cm; FuerzaAntivuelco = antivuelco;
            VelocidadMaxima = vMax; VelocidadReversa = vReversa; FuerzaMotor = motor; FuerzaReversa = reversa; FuerzaFreno = freno;
            FuerzaFrenoMano = frenoMano; ResistenciaRodadura = rodadura;
            AnguloParado = angParado; AnguloMaximo = angMax; ExponenteDireccion = expDir; DistanciaEntreEjes = entreEjes;
            AceleracionLateral = aLat; AgarreLateral = agarre; RespuestaGiro = respGiro; VelocidadVolante = volante; VelocidadCentradoVolante = centrado;
            Zeta = zeta; FactorRebote = rebote; FactorCabeceo = cabeceo; FactorBalanceo = balanceo; GiroFrenoMano = giroMano;
            GananciaContravolante = contravolante; AntivuelcoBajaVelocidad = antivuelcoLento; AmortiguacionInclinacion = amortInclinacion; Marchas = marchas;
        }

        /// <summary>Rigidez (N/m) de cada resorte.</summary>
        public float Rigidez => CalculoSuspension.RigidezParaCaida(Masa, CaidaReposo);

        /// <summary>Amortiguamiento (N·s/m) de cada resorte.</summary>
        public float Amortiguamiento => CalculoSuspension.AmortiguamientoParaRelacion(Masa, Rigidez, Zeta);

        /// <summary>Aceleración máxima del motor parado (m/s²).</summary>
        public float AceleracionMotor => FuerzaMotor / Masa;

        /// <summary>Desaceleración del freno de pie (m/s²).</summary>
        public float DesaceleracionFreno => FuerzaFreno / Masa;

        /// <summary>Cabeceo estático (°) al frenar a fondo.</summary>
        public float CabeceoAlFrenarGrados =>
            CalculoPeso.CabeceoEstimadoGrados(FuerzaFreno, AlturaCentroMasa, FactorCabeceo, Rigidez, DistanciaEntreEjes);

        /// <summary>Los tipos que existen.</summary>
        public static readonly TipoManejo[] Tipos = (TipoManejo[])Enum.GetValues(typeof(TipoManejo));

        /// <summary>El perfil de un tipo.</summary>
        public static PerfilManejo Para(TipoManejo tipo)
        {
            switch (tipo)
            {
                case TipoManejo.Compacto:
                    return new PerfilManejo(tipo, 900f, 0.45f, 3200f, 15f, 5f, 6300f, 3000f, 15000f, 7000f, 600f,
                        34f, 12f, 0.5f, 2.4f, 8.5f, 10f, 9f, 4.5f, 6.5f, 0.38f, 1.3f, 0.9f, 0.35f, 1.6f, 2.2f, 1f, 3600f, 4f);
                case TipoManejo.Camioneta:
                    return new PerfilManejo(tipo, 2000f, 0.7f, 7500f, 17f, 4.5f, 10000f, 4500f, 24000f, 10000f, 1100f,
                        30f, 8f, 0.5f, 3.1f, 7.5f, 9f, 6.5f, 3.5f, 5f, 0.45f, 1.3f, 1f, 0.5f, 1.3f, 1.8f, 1f, 8500f, 5f);
                case TipoManejo.Deportivo:
                    return new PerfilManejo(tipo, 1300f, 0.38f, 4800f, 28f, 6f, 15600f, 5000f, 28000f, 12000f, 700f,
                        30f, 8.5f, 0.5f, 2.6f, 12f, 14f, 11f, 5.5f, 8f, 0.38f, 1.3f, 0.8f, 0.3f, 1.8f, 2.5f, 1f, 5200f, 6f);
                case TipoManejo.Pesado:
                    return new PerfilManejo(tipo, 8000f, 1.1f, 36000f, 14f, 4f, 24000f, 12000f, 60000f, 24000f, 2500f,
                        24f, 6f, 0.5f, 5f, 5f, 6f, 4f, 2.5f, 3.5f, 0.5f, 1.3f, 1f, 0.6f, 1.2f, 1.5f, 1f, 32000f, 6f);
                case TipoManejo.Moto:
                    return new PerfilManejo(tipo, 230f, 0.4f, 1400f, 26f, 3f, 1800f, 600f, 6400f, 3000f, 140f,
                        40f, 16f, 0.6f, 1.4f, 11f, 11f, 10f, 5f, 8f, 0.5f, 1.2f, 0.15f, 0f, 1.4f, 1.5f, 3f, 1400f, 5f);
                default:
                    return new PerfilManejo(TipoManejo.Sedan, 1100f, 0.5f, 4000f, 18f, 5f, 8800f, 3500f, 20000f, 9000f, 900f,
                        32f, 10f, 0.5f, 2.7f, 9f, 11f, 8f, 4f, 6f, 0.42f, 1.3f, 0.9f, 0.4f, 1.5f, 2f, 1f, 5000f, 5f);
            }
        }

        /// <summary>Valores del perfil con el nombre del campo serializado de ConfigCarro al que van.</summary>
        public IReadOnlyList<KeyValuePair<string, float>> Campos()
        {
            return new List<KeyValuePair<string, float>>
            {
                new KeyValuePair<string, float>("masa", Masa),
                new KeyValuePair<string, float>("alturaCentroMasa", AlturaCentroMasa),
                new KeyValuePair<string, float>("fuerzaAntivuelco", FuerzaAntivuelco),
                new KeyValuePair<string, float>("velocidadMaxima", VelocidadMaxima),
                new KeyValuePair<string, float>("velocidadReversa", VelocidadReversa),
                new KeyValuePair<string, float>("fuerzaMotor", FuerzaMotor),
                new KeyValuePair<string, float>("fuerzaReversa", FuerzaReversa),
                new KeyValuePair<string, float>("fuerzaFreno", FuerzaFreno),
                new KeyValuePair<string, float>("fuerzaFrenoMano", FuerzaFrenoMano),
                new KeyValuePair<string, float>("resistenciaRodadura", ResistenciaRodadura),
                new KeyValuePair<string, float>("anguloDireccionParado", AnguloParado),
                new KeyValuePair<string, float>("anguloDireccionMaximo", AnguloMaximo),
                new KeyValuePair<string, float>("exponenteDireccion", ExponenteDireccion),
                new KeyValuePair<string, float>("distanciaEntreEjes", DistanciaEntreEjes),
                new KeyValuePair<string, float>("aceleracionLateralMaxima", AceleracionLateral),
                new KeyValuePair<string, float>("agarreLateral", AgarreLateral),
                new KeyValuePair<string, float>("respuestaGiro", RespuestaGiro),
                new KeyValuePair<string, float>("velocidadVolante", VelocidadVolante),
                new KeyValuePair<string, float>("velocidadCentradoVolante", VelocidadCentradoVolante),
                new KeyValuePair<string, float>("rigidez", Rigidez),
                new KeyValuePair<string, float>("amortiguamiento", Amortiguamiento),
                new KeyValuePair<string, float>("factorRebote", FactorRebote),
                new KeyValuePair<string, float>("factorCabeceo", FactorCabeceo),
                new KeyValuePair<string, float>("factorBalanceo", FactorBalanceo),
                new KeyValuePair<string, float>("giroFrenoMano", GiroFrenoMano),
                new KeyValuePair<string, float>("gananciaContravolante", GananciaContravolante),
                new KeyValuePair<string, float>("antivuelcoBajaVelocidad", AntivuelcoBajaVelocidad),
                new KeyValuePair<string, float>("amortiguacionInclinacion", AmortiguacionInclinacion),
                new KeyValuePair<string, float>("marchasSonido", Marchas),
            };
        }
    }
}

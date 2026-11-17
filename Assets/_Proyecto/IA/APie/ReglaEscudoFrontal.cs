using MechaCyberpunk.Core;
using UnityEngine;

namespace MechaCyberpunk.IA.APie
{
    /// <summary>
    /// T-1109: el escudo del guardia pesado de Nova (E12). Lógica pura. Un golpe o bala que llega de frente (el ángulo entre -dirección y el frente del guardia
    /// es menor que <see cref="AnguloFrontal"/>) se reduce 85% y es <see cref="TipoBloqueo.Normal"/>. Gasta aguante con el daño bruto; al llegar a 0 el
    /// escudo se cae <see cref="SegundosSinEscudo"/> y vuelve completo. De lado o de espalda, o sin escudo, entra todo el daño.
    /// </summary>
    public sealed class ReglaEscudoFrontal
    {
        public const float AnguloFrontal = 70f;
        public const float Reduccion = 0.85f;
        public const float AguanteDiseno = 150f;
        public const float SegundosSinEscudo = 5f;

        public readonly struct Resultado
        {
            public readonly float Entra;
            public readonly TipoBloqueo Bloqueo;
            public readonly bool SeRompio;

            public Resultado(float entra, TipoBloqueo bloqueo, bool seRompio)
            {
                Entra = entra;
                Bloqueo = bloqueo;
                SeRompio = seRompio;
            }
        }

        private readonly float aguanteMaximo;
        private readonly float reduccion;
        private readonly float anguloMaximo;
        private readonly float segundosSinEscudo;
        private float sinEscudoRestante;

        public ReglaEscudoFrontal() : this(AguanteDiseno, Reduccion, AnguloFrontal, SegundosSinEscudo) { }

        public ReglaEscudoFrontal(float aguante, float reduccion, float anguloMaximo, float segundosSinEscudo)
        {
            aguanteMaximo = aguante;
            this.reduccion = reduccion;
            this.anguloMaximo = anguloMaximo;
            this.segundosSinEscudo = segundosSinEscudo;
            Aguante = aguante;
        }

        /// <summary>Daño bruto que todavía aguanta el escudo.</summary>
        public float Aguante { get; private set; }
        public float AguanteMaximo => aguanteMaximo;
        /// <summary>True mientras el escudo está arriba.</summary>
        public bool Activo => sinEscudoRestante <= 0f;
        /// <summary>Segundos que faltan para que el escudo vuelva (0 si está arriba).</summary>
        public float SinEscudoRestante => sinEscudoRestante;

        /// <summary>Ángulo (grados, en el plano) entre -<paramref name="direccionGolpe"/> y <paramref name="frente"/>. 180 si alguno no tiene dirección en el plano.</summary>
        public static float AnguloDeFrente(Vector3 direccionGolpe, Vector3 frente)
        {
            direccionGolpe.y = 0f;
            frente.y = 0f;
            if (direccionGolpe.sqrMagnitude < 0.0001f || frente.sqrMagnitude < 0.0001f)
            {
                return 180f;
            }
            return Vector3.Angle(-direccionGolpe, frente);
        }

        public bool EsDeFrente(Vector3 direccionGolpe, Vector3 frente) => AnguloDeFrente(direccionGolpe, frente) < anguloMaximo;

        /// <summary>Decide cuánto entra de <paramref name="cantidad"/> (daño bruto) y cómo se bloquea.</summary>
        public Resultado Filtrar(float cantidad, Vector3 direccionGolpe, Vector3 frente)
        {
            if (!Activo || !EsDeFrente(direccionGolpe, frente))
            {
                return new Resultado(cantidad, TipoBloqueo.Ninguno, false);
            }
            Aguante = Mathf.Max(0f, Aguante - cantidad);
            bool roto = Aguante <= 0f;
            if (roto)
            {
                sinEscudoRestante = segundosSinEscudo;
            }
            return new Resultado(cantidad * (1f - reduccion), TipoBloqueo.Normal, roto);
        }

        /// <summary>Pasa el tiempo: cuando termina el castigo el escudo vuelve completo.</summary>
        public void Avanzar(float dt)
        {
            if (sinEscudoRestante <= 0f)
            {
                return;
            }
            sinEscudoRestante -= dt;
            if (sinEscudoRestante <= 0f)
            {
                sinEscudoRestante = 0f;
                Aguante = aguanteMaximo;
            }
        }

        public void Reiniciar()
        {
            sinEscudoRestante = 0f;
            Aguante = aguanteMaximo;
        }
    }
}

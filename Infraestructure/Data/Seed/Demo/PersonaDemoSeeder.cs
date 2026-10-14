using Domain.Entities.Historico;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using static Infraestructure.Data.Seed.Demo.SipffaaDemo;

namespace Infraestructure.Data.Seed.Demo
{
    /// <summary>Solicitantes, beneficiarios y choferes de ejemplo.</summary>
    internal sealed record PersonasDemo(
        Persona DirectorTransportacion,
        Persona SubdirectorTransportacion,
        Persona EncargadoFlotilla,
        Persona DirectorTic,
        Persona EncargadoNomina,
        Persona AyudanteMinistro,
        Persona ChoferUno,
        Persona ChoferDos,
        Persona ChoferCivil,
        IReadOnlyDictionary<Guid, string> NombresRango)
    {
        /// <summary>Nombre del rango de la persona, o null si es civil.</summary>
        public string? RangoDe(Persona persona) => persona.RangoId is { } id ? NombresRango[id] : null;

        public IReadOnlyList<Persona> Todas =>
        [
            DirectorTransportacion, SubdirectorTransportacion, EncargadoFlotilla, DirectorTic,
            EncargadoNomina, AyudanteMinistro, ChoferUno, ChoferDos, ChoferCivil
        ];
    }

    internal static class PersonaDemoSeeder
    {
        public static async Task<PersonasDemo> SeedAsync(DatosDemo demo, CancellationToken cancellationToken)
        {
            // Los rangos los crea RangoSeeder; se buscan por nombre para no repetir sus Ids.
            var nombresRango = await demo.Context.Rangos.ToDictionaryAsync(r => r.Id, r => r.Nombre, cancellationToken);
            var rangos = nombresRango.ToDictionary(r => r.Value, r => r.Key);

            Persona Militar(string cedula, string nombre, string apellido, SexoEnum sexo, string rango,
                InstitucionEnum institucion, Item departamento, string telefono) =>
                demo.Registrar(new Persona
                {
                    Identificacion = cedula,
                    Nombre = nombre,
                    Apellido = apellido,
                    Sexo = sexo,
                    TipoPersona = TipoPersona.Militar,
                    RangoId = rangos[rango],
                    Institucion = institucion,
                    CodDependencia = departamento.Codigo,
                    Dependencia = departamento.Nombre,
                    Telefono = telefono
                });

            var choferCivil = demo.Registrar(new Persona
            {
                Identificacion = "999-0000009-9",
                Nombre = "Ramón Antonio",
                Apellido = "Peña Castillo",
                Sexo = SexoEnum.Masculino,
                TipoPersona = TipoPersona.Civil,
                CodDependencia = Departamentos.Transportacion.Codigo,
                Dependencia = Departamentos.Transportacion.Nombre,
                Telefono = "809-555-0109"
            });

            return new PersonasDemo(
                DirectorTransportacion: Militar("999-0000001-1", "Julio César", "Martínez Reyes", SexoEnum.Masculino,
                    "Coronel", InstitucionEnum.ERD, Departamentos.Transportacion, "809-555-0101"),
                SubdirectorTransportacion: Militar("999-0000002-2", "Ana María", "Rodríguez Peña", SexoEnum.Femenino,
                    "Teniente Coronel", InstitucionEnum.FARD, Departamentos.Transportacion, "809-555-0102"),
                EncargadoFlotilla: Militar("999-0000003-3", "Luis Manuel", "Santos Jiménez", SexoEnum.Masculino,
                    "Capitán", InstitucionEnum.ERD, Departamentos.Transportacion, "809-555-0103"),
                DirectorTic: Militar("999-0000004-4", "Carlos Alberto", "Fernández Díaz", SexoEnum.Masculino,
                    "Coronel", InstitucionEnum.ARD, Departamentos.Tic, "809-555-0104"),
                EncargadoNomina: Militar("999-0000005-5", "Yolanda", "Guzmán Herrera", SexoEnum.Femenino,
                    "Mayor", InstitucionEnum.MIDE, Departamentos.RecursosHumanos, "809-555-0105"),
                AyudanteMinistro: Militar("999-0000006-6", "Pedro José", "Almonte Vásquez", SexoEnum.Masculino,
                    "Primer Teniente", InstitucionEnum.ERD, Departamentos.Despacho, "809-555-0106"),
                ChoferUno: Militar("999-0000007-7", "Miguel Ángel", "Batista Ramírez", SexoEnum.Masculino,
                    "Segundo Teniente", InstitucionEnum.ERD, Departamentos.Transportacion, "809-555-0107"),
                ChoferDos: Militar("999-0000008-8", "Francisco", "De la Cruz Mejía", SexoEnum.Masculino,
                    "Subteniente", InstitucionEnum.ARD, Departamentos.Transportacion, "809-555-0108"),
                ChoferCivil: choferCivil,
                NombresRango: nombresRango);
        }
    }
}

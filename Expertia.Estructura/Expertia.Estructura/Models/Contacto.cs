﻿using Expertia.Estructura.Models.Auxiliar;
using Expertia.Estructura.Models.Behavior;
using Expertia.Estructura.Models.Foreign;
using System;
using System.Collections.Generic;

namespace Expertia.Estructura.Models
{
    public class Contacto : ICrm, IAuditable, IUnidadNegocio
    {
        #region Properties
        public string ID { get; set; }
        public int? DkAgencia_DM { get; set; }
        public int? DkAgencia_IA { get; set; }
        public string IDCuenta { get; set; }
        public string IdSalesforce { get; set; }
        public string IdCuentaSalesForce { get; set; }
        public string Nombre { get; set; }
        public string ApePaterno { get; set; }
        public string ApeMaterno { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string LogoFoto { get; set; }
        public int Hijos { get; set; }
        public int TiempoEmpresa { get; set; }
        public string Comentarios { get; set; }
        public string OrigenContacto { get; set; }
        #endregion

        #region ForeignKey
        public UnidadNegocio UnidadNegocio { get; set; }
        public EstadoCivil EstadoCivil { get; set; }
        public Genero Genero { get; set; }
        public Nacionalidad Nacionalidad { get; set; }
        public Profesion Profesion { get; set; }
        public CargoEmpresa CargoEmpresa { get; set; }
        public NivelRiesgo NivelRiesgo { get; set; }
        public RegionMercadoBranch RegionMercadoBranch { get; set; }
        public Estado Estado { get; set; }
        #endregion

        #region MultipleKey
        public IEnumerable<Documento> Documentos { get; set; }
        public IEnumerable<Direccion> Direcciones { get; set; }
        public IEnumerable<Telefono> Telefonos { get; set; }
        public IEnumerable<Sitio> Sitios { get; set; }
        public IEnumerable<Correo> Correos { get; set; }
        public IdiomaComunicCliente IdiomasComunicCliente { get; set; }
        #endregion

        #region Auditoria
        public Auditoria Auditoria { get; set; }
        #endregion
    }
}
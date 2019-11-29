﻿using Expertia.Estructura.Models;
using Expertia.Estructura.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Expertia.Estructura.Repository.Behavior
{
    public interface IContactoPtaRepository
    {
        Operation GetContactos();
        Operation Update(ContactoPta cuentaPta);
    }
}
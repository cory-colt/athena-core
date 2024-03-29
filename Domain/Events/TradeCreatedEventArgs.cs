﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Athena.Domain.Enums;
using Athena.Domain.Models;

namespace Athena.Domain.Events
{
    public class TradeCreatedEventArgs : EventArgs
    {
        public Trade TradeCreated { get; set; }
    }
}

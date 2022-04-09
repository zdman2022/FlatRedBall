﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlueControl.Dtos;


namespace GlueControl.Managers
{
    internal class GlueCommands
    {
        #region Fields/properties

        public static GlueCommands Self { get; }

        public GluxCommands GluxCommands { get; private set; }

        #endregion

        #region  Constructors

        static GlueCommands() => Self = new GlueCommands();

        public GlueCommands()
        {
            GluxCommands = new GluxCommands();
        }

        #endregion

        public void PrintOutput(string output)
        {
            SendToGame(nameof(PrintOutput), output);
        }

        private void SendToGame(string caller = null, params object[] parameters)
        {
            var dto = new GlueCommandDto();
            dto.Method = caller;
            dto.Parameters.AddRange(parameters);

            GlueControlManager.Self.SendToGlue(dto);
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Text;
using Tasks;

namespace GUI
{
    internal class ParametersForDataGrid
    {
        #region Public Constructors

        public ParametersForDataGrid(string FilePath)
        {            
            this.FilePath = FilePath;            
            FileName = System.IO.Path.GetFileName(FilePath);
        }

        #endregion Public Constructors

        #region Public Properties
               
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public bool InProgress { get; private set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Method to mark as in progress. Need the property setter to be private so user could not check off in GUI
        /// </summary>
        /// <param name="inProgress"></param>
        public void SetInProgress(bool inProgress)
        {
            InProgress = inProgress;
        }

        #endregion Public Methods
    }
}

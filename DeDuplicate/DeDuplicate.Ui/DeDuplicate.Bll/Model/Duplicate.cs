using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace DeDuplicate.Bll.Model
{
    public class Duplicate
    {
        #region Constructor

        public Duplicate()
        {
            this.DuplicatedLocations = new List<Duplicate>();
        }

        #endregion

        #region Properties

        public long Bytes { get; set; }
        public string Extension { get; set; }
        public List<Duplicate> DuplicatedLocations { get; set; }
        public string SourcePath { get; set; }
        public DateTime PictureTaken { get; set; }
        public DateTime Created { get; set; }

        #endregion
    }
}


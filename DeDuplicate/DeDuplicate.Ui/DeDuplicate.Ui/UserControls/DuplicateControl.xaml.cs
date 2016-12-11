using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using DeDuplicate.Ui.Helpers;
using System.Windows;

namespace DeDuplicate.Ui.UserControls
{
    /// <summary>
    /// Interaction logic for DuplicateControl.xaml
    /// </summary>
    public partial class DuplicateControl : UserControl
    {

        #region Commands

        public ICommand DeleteCommand { get; set; }
        public ICommand ViewFileCommand { get; set; }

        #endregion

        #region fields

        private Action<string> _removeFromList;

        #endregion /fields

        #region Constructor
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fullPathAndFile">Full path, with file name and extension</param>
        /// <param name="fileSize">The size in KB</param>
        public DuplicateControl(string fullPathAndFile, long fileSize, Action<string> removeFromList, bool turnOffAutoConfirmOnDelete, DateTime pictureTaken, DateTime created)
        {
            this.FilePath = fullPathAndFile;
            this.FileName = Path.GetFileName(fullPathAndFile);
            this.DirectoryPath = Path.GetDirectoryName(fullPathAndFile);
            this.FileSize = fileSize;
            this.PictureTaken = pictureTaken;
            this.Created = created; 
            this._removeFromList = removeFromList;
            this.DeleteCommand = new RelayCommand(new Action<object>(Delete));
            this.ViewFileCommand = new RelayCommand(a => {
                if (File.Exists(a.ToString()))
                    System.Diagnostics.Process.Start(a.ToString());
                else
                    if (Directory.Exists(a.ToString()))
                        System.Diagnostics.Process.Start(a.ToString());
                else
                    MessageBox.Show("File no longer exists");
            });
            InitializeComponent();
        }

        #endregion

        #region Properties

        public string FilePath { get; set; }
        public string FileName { get; set; }
        public DateTime PictureTaken { get; set; }
        public DateTime Created { get; set; }
        public long FileSize { get; set; }
        public string DirectoryPath { get; set; }
        public bool TurnOffAutoConfirmationOnDelete{get;set;}


        #endregion

        #region private methods

        private void Delete(object o)
        {
            //if (!TurnOffAutoConfirmationOnDelete)
                //if (MessageBox.Show("Are you sure you want to permenantly delete this file? It is not sent to the rycle bin.", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                  //  return;

            string s = Convert.ToString(o);
            if (string.IsNullOrEmpty(s))
                return;

            if (File.Exists(s))
                File.Delete(s);

            _removeFromList.Invoke(s);
        }

        #endregion
    }
}

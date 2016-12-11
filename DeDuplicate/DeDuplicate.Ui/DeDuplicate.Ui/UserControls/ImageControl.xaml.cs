using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Controls;
using DeDuplicate.Bll.Model;

namespace DeDuplicate.Ui.UserControls
{
    /// <summary>
    /// Interaction logic for ImageControl.xaml
    /// </summary>
    public partial class ImageControl : UserControl, INotifyPropertyChanged, IDisposable
    {

        public ImageControl(string path, long fileSize)
        {
            System.Diagnostics.Debug.Assert(false);
            this.FilePath = path;
            this.FileSize = fileSize;
            this.DataContext = this;
            InitializeComponent();
        }

        #region Properties

        private long _fileSize;
        public long FileSize
        {
            get { return _fileSize; }
            set
            {
                _fileSize = value;
                OnPropertyChanged("FileSize");
            }
        }

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
            set
            {
                _filePath = value;
                OnPropertyChanged("FilePath");
            }
        }

        #endregion

        #region Methods

        
        private void ResetValues(string deletedFilePath, string deletedFileSize)
        {
            this.FilePath = deletedFilePath;
            //this.FileSize = deletedFileSize;
        }

        private Duplicate ConvertToDuplicateItem()
        {
            return new Duplicate()
                {
                    Bytes = _fileSize,
                    SourcePath = _filePath,
                    Extension = Path.GetExtension(_filePath)
                };
        }

        #endregion


        #region INotifyPropertyChanged Members and Helpers

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        public void Dispose()
        {
            this.FilePath = null;
            this.FileSize = 0;
            PropertyChanged = null;
            this.DataContext = null;
        }
    }
}

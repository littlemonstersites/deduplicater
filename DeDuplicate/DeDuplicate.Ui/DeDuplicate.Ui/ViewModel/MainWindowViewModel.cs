using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using DeDuplicate.Bll;
using DeDuplicate.Bll.Model;
using DeDuplicate.Ui.Helpers;
using System.Collections.ObjectModel;
using DeDuplicate.Ui.UserControls;
using System.IO;
using System.Threading;
using System.ComponentModel;
using Microsoft.VisualBasic.FileIO;

namespace DeDuplicate.Ui.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        #region Commands

        public ICommand SelectSourceFolderCommand { get; set; }
        public ICommand SelectDestinationFolderCommand { get; set; }
        public ICommand StartCommand { get; set; }
        //public ICommand MarkForDeleteCommand { get; set; }
        //public ICommand DeleteCommand { get; set; }

        #endregion

        #region Constructors

        public MainWindowViewModel()
        {
            this.ParentDuplicate = new ObservableCollection<DuplicateControl>();
            this.ChildDuplicate = new ObservableCollection<DuplicateControl>();
            this.SelectSourceFolderCommand = new RelayCommand(new Action<object>(SelectSourceFolder));
            this.SelectDestinationFolderCommand = new RelayCommand(new Action<object>(SelectDestinationFolder));
            this.StartCommand = new RelayCommand(new Action<object>(StartProcess));
            //this.MarkForDeleteCommand = new RelayCommand(new Action<object>(MarkForDelete));
            //this.DeleteCommand = new RelayCommand(new Action<object>(Delete));
            this.ChildDuplicate.CollectionChanged += (sender, args) => OnPropertyChanged("ChildDuplicate");
            this.ParentDuplicate.CollectionChanged += (sender, args) => OnPropertyChanged("ParentDuplicate");
            this.DeleteList = new List<string>();

            this._logPath = Path.GetDirectoryName(SpecialDirectories.Desktop) + "\\DeDuplicateLog.txt";
        }



        #endregion

        #region Fields

        private readonly string _logPath;
        private int _totalFilesDeleted = -99;
        private bool _didDelete = false;
        private Thread _duplicatesThread;
        private List<Duplicate> _duplicateList;
        private Stopwatch _stopWatch;
        #endregion

        #region Properties

        public List<string> DeleteList { get; set; }

        private string _sourcePath;
        public string SourcePath
        {
            get { return _sourcePath; }
            set
            {
                if (_sourcePath == value)
                    return;

                _sourcePath = value;
                OnPropertyChanged("SourcePath");
            }
        }

        private string _destinationPath;
        public string DestinationPath
        {
            get { return _destinationPath; }
            set
            {
                if (_destinationPath == value)
                    return;

                _destinationPath = value;
                OnPropertyChanged("DestinationPath");
            }
        }

        private ObservableCollection<DuplicateControl> _parentDuplicate;
        public ObservableCollection<DuplicateControl> ParentDuplicate
        {
            get { return _parentDuplicate; }
            set
            {
                if (_parentDuplicate == value)
                    return;

                _parentDuplicate = value;
                OnPropertyChanged("ParentDuplicate");
            }
        }

        private ObservableCollection<DuplicateControl> _childDuplicate;
        public ObservableCollection<DuplicateControl> ChildDuplicate
        {
            get { return _childDuplicate; }
            set
            {
                if (_childDuplicate == value)
                    return;

                _childDuplicate = value;
                OnPropertyChanged("ChildDuplicate");
            }
        }

        private string _currentFile;
        public string CurrentFile
        {
            get { return _currentFile; }
            set
            {
                _currentFile = value;
                OnPropertyChanged("CurrentFile");
            }
        }

        private string _currentFileChecking;
        public string CurrentFileChecking
        {
            get { return _currentFileChecking; }
            set
            {
                _currentFileChecking = value;
                OnPropertyChanged("CurrentFileChecking");
            }
        }



        public string TimeTaken { get; set; }

        private bool _turnOffAutoConfirm;
        public bool TurnOffAutoConfirm
        {
            get { return _turnOffAutoConfirm; }

            set
            {
                if (_turnOffAutoConfirm == value)
                    return;

                _turnOffAutoConfirm = value;
                OnPropertyChanged("TurnOffAutoConfirm");
            }
        }

        private string _percentageComplete;
        public string PercentageComplete
        {
            get { return _percentageComplete; }
            set
            {
                if (value == _percentageComplete)
                    return;

                _percentageComplete = value;
                OnPropertyChanged("PercentageComplete");
            }
        }

        private DuplicateControl _selectedParent;
        public DuplicateControl SelectedParent
        {
            get { return _selectedParent; }
            set
            {
                if (value == _selectedParent)
                    return;

                _selectedParent = value;
                using (new WaitCursor())
                {
                    ShowChildItems();
                }
                OnPropertyChanged("SelectedParent");
            }
        }

        private bool _willAutomaticallyDelete;
        public bool WillAutomaticallyDelete
        {
            get { return this._willAutomaticallyDelete; }
            set
            {
                if (this._willAutomaticallyDelete == value)
                    return;

                this._willAutomaticallyDelete = value;
                OnPropertyChanged();
            }
        }

        private bool _willKeepNewFile;
        public bool WillKeepNewFile
        {
            get { return this._willKeepNewFile; }
            set
            {
                if (this._willKeepNewFile == value)
                    return;

                this._willKeepNewFile = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        private void Delete(object obj)
        {

            MessageBox.Show("Deleting anyway");

            this._duplicateList = null;
            this.ParentDuplicate = null;
            this.ChildDuplicate = null;
            this.SelectedParent = null;

            foreach (var filePath in this.DeleteList)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }


        //private void MarkForDelete(object obj)
        //{
        //    string filePath = obj.ToString();
        //    RemoveDeletedDuplicateFromList(filePath);
        //    this.DeleteList.Add(filePath);
        //}

        private void SelectSourceFolder(object obj)
        {
            this.SourcePath = SelectFolder();
        }

        private void SelectDestinationFolder(object obj)
        {
            this.DestinationPath = SelectFolder();
        }

        private string SelectFolder()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.SelectedPath;
            }
            return "";
        }


        private void StartProcess(object obj)
        {
            try
            {
                if (File.Exists(_logPath))
                    File.Delete(_logPath);

                this._totalFilesDeleted = 0;
                if (string.IsNullOrEmpty(this.SourcePath) || string.IsNullOrEmpty(this.DestinationPath))
                {
                    MessageBox.Show("Please select both directories first", "Error", MessageBoxButtons.OK);
                    return;
                }

                ClearResults();

                if (this.WillAutomaticallyDelete)
                    StartProcessAutomatic();
                else
                    StartProcessManual();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

        }

        private void StartProcessAutomatic()
        {
            this._didDelete = false;//must keep this here
            this._stopWatch = new Stopwatch();
            this.PercentageComplete = "Preparing...";
            Bll.Duplication duplication = new Duplication(this.SourcePath, this.DestinationPath);

            BackgroundWorker worker = new BackgroundWorker();
            //worker.RunWorkerCompleted += CreateParent;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += ((s, e) =>
            {

                try
                {
                    duplication.CurrentPercentageEvent += RefreshPercentage;
                    this._stopWatch.Start();
                    duplication.GetDuplicateList(new Action<string>(AddCurrentFile),
                                                 new Action<List<Duplicate>>(Complete),
                                                 new Action<string>(AddCurrentFileChecking));
                    DeleteTheDupes(duplication);
                    duplication.CurrentPercentageEvent -= RefreshPercentage;

                    this.CurrentFile = "Complete";
                    TimeTaken = this._stopWatch.Elapsed.ToString();
                    this._stopWatch.Stop();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n\n'Deduplication' cancelled...");
                    worker.CancelAsync();
                }
                finally
                {
                    if (this._didDelete)
                        StartProcessAutomatic();
                    else
                    {
                        MessageBox.Show("Well done, you moved " + this._totalFilesDeleted + " files to your recycle bin!");
                    }
                } //});
            });

            worker.RunWorkerAsync();
        }

        private void DeleteTheDupes(Bll.Duplication dupe)
        {
            try
            {
                for (int i = 0; i < dupe.DuplicateList.Count; i++)
                {
                    _didDelete = true;
                    if (dupe.DuplicateList[i].PictureTaken != new DateTime(1000, 01, 01))
                    {
                        //delete by date picture taken
                        foreach (var inner in dupe.DuplicateList[i].DuplicatedLocations)
                        {
                            if (dupe.DuplicateList[i].PictureTaken >= inner.PictureTaken)
                                MoveToRecycleBin(inner.SourcePath);
                            else
                            {
                                MoveToRecycleBin(dupe.DuplicateList[i].SourcePath);
                            }
                        }
                    }
                    else
                    {
                        //delete by date created
                        foreach (var inner in dupe.DuplicateList[i].DuplicatedLocations)
                        {
                            if (dupe.DuplicateList[i].Created >= inner.Created)
                                MoveToRecycleBin(inner.SourcePath);
                            else
                            {
                                MoveToRecycleBin(dupe.DuplicateList[i].SourcePath);
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                var msg = "<p>A fault with DeDuplciater, probably by Keith</p><p>Error:" + e.ToString() + "</p>";
                Utilities.Email.SendFaultEmail("dave@lmsites.co.uk", "Fault with DeDuplicater", true, msg)
            }
        }

        private void MoveToRecycleBin(string filePath)
        {
            this._totalFilesDeleted++;

            if (!File.Exists(_logPath))
                File.Create(_logPath).Close();

            Log(filePath);
            Thread.Sleep(100);
            if (File.Exists(filePath))
                FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }

        private void Log(string msg)
        {

            using (StreamWriter sw = new StreamWriter(this._logPath, true))
            {
                sw.WriteLine(msg);
            }
        }

        private void StartProcessManual()
        {
            using (new WaitCursor())
            {
                _stopWatch = new Stopwatch();
                this.PercentageComplete = "Preparing...";
                Bll.Duplication duplication = new Duplication(this.SourcePath, this.DestinationPath);

                BackgroundWorker worker = new BackgroundWorker();
                worker.RunWorkerCompleted += CreateParent;
                worker.WorkerSupportsCancellation = true;
                worker.DoWork += ((s, e) =>
                {

                    //this._duplicatesThread = new Thread(() =>
                    //{
                    try
                    {
                        duplication.CurrentPercentageEvent += RefreshPercentage;
                        _stopWatch.Start();
                        duplication.GetDuplicateList(new Action<string>(AddCurrentFile), new Action<List<Duplicate>>(Complete), new Action<string>(AddCurrentFileChecking));

                        duplication.CurrentPercentageEvent -= RefreshPercentage;

                        this.CurrentFile = "Complete";
                        TimeTaken = this._stopWatch.Elapsed.ToString();
                        _stopWatch.Stop();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message + "\n\nDeduplication cancelled...");
                        worker.CancelAsync();
                    }
                    //});
                });

                worker.RunWorkerAsync();


                //this._duplicatesThread.IsBackground = true;
                //this._duplicatesThread.Start();

            }
        }

        private void ClearResults()
        {
            //this.ParentDuplicate.Clear();
            //this.ChildDuplicate.Clear();
        }

        private void RefreshPercentage(double d)
        {
            this.PercentageComplete = d.ToString("N1") + "%";
        }

        private void AddCurrentFileChecking(string s)
        {
            this.CurrentFileChecking = s;
        }



        private void AddCurrentFile(string s)
        {
            this.CurrentFile = s;
        }

        private void Complete(List<Duplicate> duplicateList)
        {
            this._duplicateList = duplicateList;
        }

        private void CreateParent(object sender, RunWorkerCompletedEventArgs e)
        {
            if (this._duplicateList == null || this._duplicateList.Count <= 0)
            {
                MessageBox.Show("There were no duplicates...");
                return;
            }
            ObservableCollection<DuplicateControl> parent = new ObservableCollection<DuplicateControl>();

            foreach (var duplicate in this._duplicateList)
            {
                DuplicateControl ic = new DuplicateControl(duplicate.SourcePath, duplicate.Bytes, new Action<string>(RemoveDeletedDuplicateFromList), this.TurnOffAutoConfirm, duplicate.PictureTaken, duplicate.Created);
                parent.Add(ic);
            }

            if (!this.WillAutomaticallyDelete)
                this.ParentDuplicate = parent;
        }

        private void RemoveDeletedDuplicateFromList(string filePath)
        {

            DuplicateControl imageControl = this.ChildDuplicate.SingleOrDefault(a => a.FilePath == filePath);

            //remove from the ChildDuplicate if it exists (don't test separately, just do it)
            if (imageControl != null)
                this.ChildDuplicate.Remove(imageControl);
            else
            {
                //now the parent
                imageControl = this.ParentDuplicate.SingleOrDefault(a => a.FilePath == filePath);
                this.ParentDuplicate.Remove(imageControl);
            }

            //are there any children

        }

        private void ShowChildItems()
        {
            using (new WaitCursor())
            {
                if (this._selectedParent == null)
                    return;

                ObservableCollection<DuplicateControl> child = new ObservableCollection<DuplicateControl>();
                var duplicates =
                    (from d in this._duplicateList where d.SourcePath == this._selectedParent.FilePath select d).ToList();

                long bytes = duplicates.First().Bytes;

                foreach (var locations in duplicates.Select(a => a.DuplicatedLocations))
                {
                    foreach (var location in locations)
                    {
                        //TODO: Template
                        child.Add(new DuplicateControl(location.SourcePath, bytes, RemoveDeletedDuplicateFromList, this.TurnOffAutoConfirm, DateTime.Now, DateTime.Now));
                    }
                }
                this.ChildDuplicate = child;
            }
        }

        #endregion

    }
}

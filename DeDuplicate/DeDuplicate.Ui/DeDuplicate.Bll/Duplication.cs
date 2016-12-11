using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using DeDuplicate.Bll.Model;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

namespace DeDuplicate.Bll
{
    public class Duplication
    {

        #region Delegates

        public delegate void CurrentPercentage(double d);
        #endregion

        #region Events

        public event CurrentPercentage CurrentPercentageEvent;

        #endregion

        #region Constructor

        public Duplication(string source, string destination)
        {
            this.DuplicateList = new List<Duplicate>();
            this.SourcePath = source;
            this.DestinationPath = destination;

            if (this.SourcePath.Contains(".") || this.DestinationPath.Contains("."))
                throw new Exception("Oops, looks like you're passing a file instead of path!");
        }

        #endregion

        #region Fields

        private double _overallPercentage;

        #endregion /Fields

        #region Properties

        public string SourcePath { get; private set; }
        public string DestinationPath { get; private set; }
        public List<Duplicate> DuplicateList { get; private set; }


        #endregion


        #region Methods

        public void GetDuplicateList(Action<string> currentFile, Action<List<Duplicate>> completed, Action<string> currentFileIteration)
        {
            
            try
            {
                List<string> sourcePaths = Directory.GetFiles(this.SourcePath, "*.*", SearchOption.AllDirectories).ToList();
                List<string> destinationPaths = Directory.GetFiles(this.DestinationPath, "*.*", SearchOption.AllDirectories).ToList();

                double currentFileIterationDouble = 0.00;

                foreach (string sourcePath in sourcePaths)
                {
                    currentFile?.Invoke(sourcePath);

                    if (!this.DuplicateList.Any(a => a.DuplicatedLocations.Select(b => b.SourcePath).Contains(sourcePath)))
                    {
                        Duplicate dupe = GetDuplicate(sourcePath, destinationPaths, Convert.ToInt32(currentFileIterationDouble), currentFileIteration, sourcePaths.Count);

                        if (dupe != null)
                            DuplicateList.Add(dupe);
                    }


                    currentFileIterationDouble++;

                    this._overallPercentage = currentFileIterationDouble / Convert.ToDouble(sourcePaths.Count);

                    if (this.CurrentPercentageEvent!=null)
                        this.CurrentPercentageEvent(this._overallPercentage * 100);
                }

                //if (DuplicateList.Count > 0)
                completed?.Invoke(DuplicateList);
            }
            catch (Exception ex)
            {
                string s = ex.ToString();
                System.Diagnostics.Debug.Assert(false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Private Methods

        private Duplicate GetDuplicate(string sourcePath, List<string> paths, int fileToCheck, Action<string> currentFileIteration, int numberOfSourcePaths)
        {
            FileInfo sourceFileInfo = new FileInfo(sourcePath);
            Duplicate duplicate = null;

            if ((sourceFileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                return null;

            fileToCheck = 0;//added for testing
            Stopwatch sw = new Stopwatch();
            long allTicks = 0;
            string previousTime = "";
            for (int i = fileToCheck; i < paths.Count(); i++)
            {
                sw.Start();
                FileInfo destinationFileInfo = new FileInfo(paths[i]);

                if (sourceFileInfo.FullName == destinationFileInfo.FullName)
                {
                    UpdateGui(paths.Count, numberOfSourcePaths, sw.ElapsedTicks, i, ref allTicks, ref previousTime, currentFileIteration);
                    continue;
                }

                if (DoFilesMatch(sourceFileInfo, destinationFileInfo))
                {
                    if (duplicate == null)
                    {
                        duplicate = new Duplicate();
                        duplicate.Bytes = sourceFileInfo.Length;
                        duplicate.Extension = sourceFileInfo.Extension;
                        duplicate.SourcePath = sourcePath;
                        duplicate.Created = sourceFileInfo.CreationTime;
                        duplicate.PictureTaken = GetPictureTakenDate(sourcePath, new Regex(":"));
                    }

                    var dupeDest = new Duplicate();
                    dupeDest.Bytes = destinationFileInfo.Length;
                    dupeDest.Extension = destinationFileInfo.Extension;
                    dupeDest.SourcePath = destinationFileInfo.FullName;
                    dupeDest.Created = destinationFileInfo.CreationTime;
                    dupeDest.PictureTaken = GetPictureTakenDate(dupeDest.SourcePath, new Regex(":"));

                    duplicate.DuplicatedLocations.Add(dupeDest);
                }

                    UpdateGui(paths.Count, numberOfSourcePaths, sw.ElapsedTicks, i, ref allTicks, ref previousTime, currentFileIteration);

                allTicks += sw.ElapsedTicks;
                sw.Stop();
                sw.Reset();
            }
            return duplicate;
        }

        private DateTime GetPictureTakenDate(string path, Regex r)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    PropertyItem propItem = myImage.GetPropertyItem(36867);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                    return DateTime.Parse(dateTaken);
                }
            }
            catch (Exception e)
            {
                return new DateTime(1000,01,01);
            }
        }
        

        private void UpdateGui(int pathCount, int numberOfSourcePaths, long stopwatchEllapsedTicks, int i, ref long allTicks, ref string previousTime, Action<string> currentFileIteration)
        {
            string format = "yyyy/MM/dd hh:mm";
            var totalTicks = stopwatchEllapsedTicks * pathCount * numberOfSourcePaths;
            var estimatedFinish = DateTime.Now.AddTicks(totalTicks).ToString(format);
            string resultTime = "";
            if (allTicks == 0)
                resultTime = estimatedFinish;
            else
            {
                DateTime dt = DateTime.Now.AddTicks((allTicks * pathCount * numberOfSourcePaths) / i);
                DateTime previousDateTime = Convert.ToDateTime(previousTime);
                TimeSpan difference = new TimeSpan();

                if (dt >= previousDateTime)
                {
                    difference = dt - previousDateTime;
                    var halved = difference.Ticks / 2;

                    resultTime = previousDateTime.Add(new TimeSpan(halved)).ToString(format);
                }

                if (dt < previousDateTime)
                {
                    difference = previousDateTime - dt;
                    var halved = difference.Ticks / 2;

                    resultTime = dt.Add(new TimeSpan(halved)).ToString(format);
                }
            }

            var percentage = Math.Round((Convert.ToDouble(i + 1) / Convert.ToDouble(pathCount) * 100.00), 0).ToString();

            switch (percentage.Length)
            {
                case 1:
                    percentage = "00" + percentage;
                    break;
                case 2:
                    percentage = "0" + percentage;
                    break;
                default:
                    break;
            }

                currentFileIteration?.Invoke(percentage + "% complete of this phase... Estimated completed time: " + resultTime);
                previousTime = resultTime;
        }


        private static bool DoFilesMatch(FileInfo sourceFileInfo, FileInfo destinationFileInfo)
        {

            if (!File.Exists(sourceFileInfo.FullName) || !File.Exists(destinationFileInfo.FullName))
                return false;

            if (sourceFileInfo.Length == destinationFileInfo.Length && Path.GetExtension(sourceFileInfo.Name) == Path.GetExtension(destinationFileInfo.Name))
                return (DoFilesMatchWithCheckSum(sourceFileInfo, destinationFileInfo));


            return false;
        }

        private static bool DoFilesMatchWithCheckSum(FileInfo sourceFileInfo, FileInfo destinationFileInfo)
        {
            byte[] sourceChecksum;
            byte[] destinationChecksum;
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(sourceFileInfo.FullName))
                {
                    sourceChecksum = md5.ComputeHash(stream);
                }
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(destinationFileInfo.FullName))
                {
                    destinationChecksum = md5.ComputeHash(stream);
                }
            }

            if (sourceChecksum.SequenceEqual(destinationChecksum))
                return true;

            return false;

        }

        //private static List<Model.Duplicate> DistinctList(List<Model.Duplicate> list)
        //{
        //    List<Model.Duplicate> resultsList = new List<Duplicate>();
        //    foreach (var item in list)
        //    {
        //        bool doesExist = false;
        //        foreach (var result in resultsList)
        //        {
        //            if (item.DuplicatedLocations == result.DuplicatedLocations)
        //                doesExist = true;
        //        }
        //        if (!doesExist)
        //            resultsList.Add(item);
        //    }
        //    return resultsList;
        //}

        #endregion

        #region Enums

        //blic Enum Config

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeDuplicate.Ui.Helpers
{
    public static class MyExtensions
    {
        public static ObservableCollection<T> ConvertToObservableCollection<T>(this List<T> myList)
        {
            ObservableCollection<T> results = new ObservableCollection<T>();
            foreach (var item in myList)
            {
                results.Add(item);
            }
            return results;
        }
    }
}

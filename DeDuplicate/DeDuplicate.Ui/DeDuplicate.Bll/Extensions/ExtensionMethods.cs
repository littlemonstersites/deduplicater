using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using DeDuplicate.Bll.Model;

namespace DeDuplicate.Bll.Extensions
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts from a list to an ObservableCollection.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="sourceList">The list of source data.</param>
        /// <summary>        
        public static ObservableCollection<T> AddToObservableCollection<T>(this List<T> sourceList)
        {
            ObservableCollection<T> collection = new ObservableCollection<T>();
            foreach (var item in sourceList)
            {
                collection.Add(item);
            }
            return collection;
        }
    }
}

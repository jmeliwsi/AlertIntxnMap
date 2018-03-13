using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace FUL
{
	/// <summary>
	/// This is a class of extension methods which is used to add new methods on to existing classes.  This is very useful for adding
	/// methods to common classes like String or DataSet.
	/// See MSDN description at:  https://msdn.microsoft.com/en-us/library/bb383977.aspx
	/// </summary>
	public static class ExtensionMethods
	{
		/// <summary>
		/// Check to see if a data set is null or empty.  This will be true if 
		/// -- the data set is null
		/// -- the data set has no tables
		/// -- the data set has one or more tables with no rows.
		/// -- the data set has has one or more tables with rows and there are no rows with data in the (i.e. they are all = to null)
		/// </summary>
		/// <param name="dataSet">data set to check.  must be "this" to enable it on the calling object.</param>
		/// <returns>true if the data set is null or empty.  false if there is at least one row with data in it</returns>
		public static bool IsNullOrEmpty(this DataSet dataSet)
		{
			// start with the assumption the data set is empty.  rest of code is trying to prove it is not empty.
			bool result = true;
			if (dataSet != null && dataSet.Tables.Count > 0)
			{
				// data set is not null and there are one or more tables.
				foreach (DataTable dt in dataSet.Tables)
				{
					// check to see if any table has more than one row.
					if (dt.Rows.Count > 0)
					{
						// a table has more than one row...
						foreach (DataRow dr in dt.Rows)
						{
							// find one row with data.
							if (dr != null)
							{
								// one row with data was found, the data set is not emplty
								result = false;
								break;
							}
						}
					}
					if (!result) break;
				}
			}
			return (result);
		}

        /////////////////////////////
        //  General DataRow utility methods.
        ////////////////////////////

        // Return the value of a column
        // If there is no such column in the row, null is returned.
        public static T ColumnValue<T>( this DataRow row, String columnName ) where T : class
        {
            return row.Table.Columns.Contains( columnName ) ? row[ columnName ] as T : null;
        }

        // Return the value of a column as a nullable type (e.g. int? or DateTime?)
        // If there is no such column in the row, null is returned.
        public static Nullable<T> NullableColumnValue<T>( this DataRow row, String columnName ) where T : struct
        {
            return row.Table.Columns.Contains( columnName ) ? row[ columnName ] as Nullable<T> : null;
        }

        // Return the value of a column
        // If there is no such column in the row, or if the column value is null, the default value will be returned.
        public static T ColumnOrDefault<T>( this DataRow row, String columnName, T defaultValue ) where T : class
        {
            return row.ColumnValue<T>( columnName ) ?? defaultValue;
        }

        // Return the value of a column as a nullable type (e.g. int? or DateTime?)
        // If there is no such column in the row, or if the column value is null, the default value will be returned.
        public static T NullableColumnOrDefault<T>( this DataRow row, String columnName, T defaultValue ) where T : struct
        {
            return row.NullableColumnValue<T>( columnName ) ?? defaultValue;
        }

        // Return the value of a column split on the given separator into a list.
        // If there is no such column in the row, or if the column value is null, an empty list will be returned.
        public static List<String> ListFromColumn( this DataRow row, String columnName, char separator )
        {
            List <String> list = new List<String>();

            String columnValue = ColumnOrDefault( row, columnName, String.Empty );
            if ( !string.IsNullOrEmpty( columnValue ) )
            {
                list.AddRange( columnValue.Split( separator ) );
            }

            return list;
        }

        /// <summary>
        /// Extension method to retrieve a Datagridview cell value given a header column name.
        /// </summary>
        public static object GetCellValueFromColumnHeader(this DataGridViewCellCollection cellCollection, string headerText)
        {
            return cellCollection.Cast<DataGridViewCell>().First(c => c.OwningColumn.HeaderText == headerText).Value;
        }

        /// <summary>
        /// Extension method to return the parent of a control given a specified type.
        /// </summary>
        public static T GetParentOfType<T>(this Control control)
        {
            const int loopLimit = 100;
            var current = control;
            var iCount = 0;
            do
            {
                current = current.Parent;

                if (current == null) throw new Exception("Could not find parent of specified type");
                if (iCount++ > loopLimit) throw new Exception("Exceeded loop limit for parent search");

            } while (current.GetType() != typeof(T));

            return (T)Convert.ChangeType(current, typeof(T));
        }

    }
}

#region License
/*
    MTUtil - Miscellaneous utility classes 
    Copyright (C) 2009 Michael J. Thiesen
	mike@thiesen.us

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace MTUtil.UI
{
	public class SettingCollection : IEnumerable<Setting>, ICollection<Setting>, ICloneable
	{
		List<Setting> settings;

		public SettingCollection()
		{
			settings = new List<Setting>();
		}

		public SettingCollection( SettingCollection src )
		{
			settings = new List<Setting>();
			foreach( Setting s in src )
				settings.Add( s );
		}


		#region ICloneable Members

		public object Clone()
		{
			return new SettingCollection( this );
		}

		#endregion

		#region IEnumerable<Setting> Members

		public IEnumerator<Setting> GetEnumerator()
		{
			foreach( Setting s in settings )
				yield return s;
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region ICollection<Setting> Members

		public void Add(Setting item)
		{
			int i = settings.IndexOf( item );

			if( i == -1 )
				settings.Add( item );
			else
				settings[i] = item;
		}

		public void Clear()
		{
			settings.Clear();
		}

		public bool Contains(Setting item)
		{
			foreach( Setting s in settings )
				if( s.Name == item.Name )
					return true;

			return false;
		}

		public void CopyTo(Setting[] array, int arrayIndex)
		{
			settings.CopyTo( array, arrayIndex );
		}

		public int Count
		{
			get{
				return settings.Count;
			}
		}

		public bool IsReadOnly
		{
			get{
				return false;
			}
		}

		public bool Remove(Setting item)
		{
			return settings.Remove( item );
		}

		#endregion

		public Setting this[int i]
		{
			get{
				return settings[i];
			}
			set{
				settings[i] = value;
			}
		}

		public Setting this[string name]
		{
			get{
				foreach( Setting s in settings )
					if( s.Name.Equals( name ) )
						return s;
				return null;
			}
		}
	}
}

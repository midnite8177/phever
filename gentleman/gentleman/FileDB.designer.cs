﻿#pragma warning disable 1591
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.4927
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace gentleman
{
	using System.Data.Linq;
	using System.Data.Linq.Mapping;
	using System.Data;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Linq;
	using System.Linq.Expressions;
	using System.ComponentModel;
	using System;
	
	
	public partial class FileDBDataContext : System.Data.Linq.DataContext
	{
		
		private static System.Data.Linq.Mapping.MappingSource mappingSource = new AttributeMappingSource();
		
    #region Extensibility Method Definitions
    partial void OnCreated();
    partial void InsertFileDB(FileDB instance);
    partial void UpdateFileDB(FileDB instance);
    partial void DeleteFileDB(FileDB instance);
    #endregion
		
		public FileDBDataContext(string connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public FileDBDataContext(System.Data.IDbConnection connection) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public FileDBDataContext(string connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public FileDBDataContext(System.Data.IDbConnection connection, System.Data.Linq.Mapping.MappingSource mappingSource) : 
				base(connection, mappingSource)
		{
			OnCreated();
		}
		
		public System.Data.Linq.Table<FileDB> FileDBs
		{
			get
			{
				return this.GetTable<FileDB>();
			}
		}
	}
	
	[Table(Name="")]
	public partial class FileDB : INotifyPropertyChanging, INotifyPropertyChanged
	{
		
		private static PropertyChangingEventArgs emptyChangingEventArgs = new PropertyChangingEventArgs(String.Empty);
		
		private ulong _frn;
		
		private string _name;
		
		private ulong _parent_frn;
		
    #region Extensibility Method Definitions
    partial void OnLoaded();
    partial void OnValidate(System.Data.Linq.ChangeAction action);
    partial void OnCreated();
    partial void OnfrnChanging(ulong value);
    partial void OnfrnChanged();
    partial void OnnameChanging(string value);
    partial void OnnameChanged();
    partial void Onparent_frnChanging(ulong value);
    partial void Onparent_frnChanged();
    #endregion
		
		public FileDB()
		{
			OnCreated();
		}
		
		[Column(Storage="_frn", IsPrimaryKey=true)]
		public ulong frn
		{
			get
			{
				return this._frn;
			}
			set
			{
				if ((this._frn != value))
				{
					this.OnfrnChanging(value);
					this.SendPropertyChanging();
					this._frn = value;
					this.SendPropertyChanged("frn");
					this.OnfrnChanged();
				}
			}
		}
		
		[Column(Storage="_name", CanBeNull=false)]
		public string name
		{
			get
			{
				return this._name;
			}
			set
			{
				if ((this._name != value))
				{
					this.OnnameChanging(value);
					this.SendPropertyChanging();
					this._name = value;
					this.SendPropertyChanged("name");
					this.OnnameChanged();
				}
			}
		}
		
		[Column(Storage="_parent_frn")]
		public ulong parent_frn
		{
			get
			{
				return this._parent_frn;
			}
			set
			{
				if ((this._parent_frn != value))
				{
					this.Onparent_frnChanging(value);
					this.SendPropertyChanging();
					this._parent_frn = value;
					this.SendPropertyChanged("parent_frn");
					this.Onparent_frnChanged();
				}
			}
		}
		
		public event PropertyChangingEventHandler PropertyChanging;
		
		public event PropertyChangedEventHandler PropertyChanged;
		
		protected virtual void SendPropertyChanging()
		{
			if ((this.PropertyChanging != null))
			{
				this.PropertyChanging(this, emptyChangingEventArgs);
			}
		}
		
		protected virtual void SendPropertyChanged(String propertyName)
		{
			if ((this.PropertyChanged != null))
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
#pragma warning restore 1591

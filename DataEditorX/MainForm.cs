﻿/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 2014-10-20
 * 时间: 9:19
 * 
 */
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Configuration;
using WeifenLuo.WinFormsUI.Docking;

using DataEditorX.Language;
using DataEditorX.Core;

namespace DataEditorX
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		public const int CLOSE_ONE=1;
		public const int CLOSE_OTHER=2;
		public const int CLOSE_ALL=3;
		public const int WM_OPEN=0x0401;
		public const string TMPFILE="open.tmp";
		string datapath;
		string conflang,conflang_de,confmsg;
		Card[] tCards;
		Dictionary<DataEditForm,string> list;
		
		#region init
		public MainForm(string datapath, string file)
		{
			Init(datapath);
			Open(file);
		}
		public MainForm(string datapath)
		{
			Init(datapath);
		}
		void Init(string datapath)
		{
			tCards=null;
			list=new Dictionary<DataEditForm,string>();
			this.datapath=datapath;
			conflang = Path.Combine(datapath, "language-mainform.txt");
			conflang_de = Path.Combine(datapath, "language-dataeditor.txt");
			confmsg = Path.Combine(datapath, "message.txt");
			InitializeComponent();
			LANG.InitForm(this, conflang);
			
			LANG.LoadMessage(confmsg);
			LANG.SetLanguage(this);
		}
		#endregion
		
		#region message
		protected override void DefWndProc(ref System.Windows.Forms.Message m)
		{
			switch (m.Msg)
			{
				case MainForm.WM_OPEN://处理消息
					string file=Path.Combine(Application.StartupPath, MainForm.TMPFILE);
					if(File.Exists(file)){
						Open(File.ReadAllText(file));
						File.Delete(file);
					}
					break;
				default:
					base.DefWndProc(ref m);
					break;
			}
		}
		#endregion
		
		#region DataEditor
		public void Open(string file)
		{
			if(checkOpen(file))
				return;
			if(OpenInNull(file))
				return;
			DataEditForm def;
			if(string.IsNullOrEmpty(file)|| !File.Exists(file))
				def=new DataEditForm(datapath);
			else
				def=new DataEditForm(datapath,file);
			LANG.InitForm(def, conflang_de);
			LANG.SetLanguage(def);
			def.FormClosed+=new FormClosedEventHandler(def_FormClosed);
			def.Show(dockPanel1, DockState.Document);
			list.Add(def, "");
		}
		
		bool checkOpen(string file)
		{
			foreach(DataEditForm df in list.Keys)
			{
				if(df!=null && !df.IsDisposed)
				{
					if(df.getNowCDB()==file)
						return true;
				}
			}
			return false;
		}
		bool OpenInNull(string file)
		{
			if(string.IsNullOrEmpty(file) || !File.Exists(file))
				return false;
			foreach(DataEditForm df in list.Keys)
			{
				if(df!=null && !df.IsDisposed)
				{
					if(string.IsNullOrEmpty(df.getNowCDB())){
						df.Open(file);
						return true;
					}
				}
			}
			return false;
		}

		void def_FormClosed(object sender, FormClosedEventArgs e)
		{
			DataEditForm df=sender as DataEditForm;
			if(df!=null)
			{
				list.Remove(df);
			}
		}
		
		void DataEditorToolStripMenuItemClick(object sender, EventArgs e)
		{
			Open(null);
		}
		#endregion
		
		#region form
		void MainFormLoad(object sender, System.EventArgs e)
		{

		}
		
		void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			#if DEBUG
			LANG.SaveMessage(confmsg+".bak");
			#endif
		}
		#endregion
		
		#region windows
		void CloseToolStripMenuItemClick(object sender, EventArgs e)
		{
			CloseMdi(MainForm.CLOSE_ONE);
		}
		
		void CloseMdi(int type)
		{
			DockContentCollection contents = dockPanel1.Contents;
			int num = contents.Count-1;
			try{
				while (num >=0)
				{
					if (contents[num].DockHandler.DockState == DockState.Document)
					{
						if(type==MainForm.CLOSE_ALL)
							contents[num].DockHandler.Close();
						else if(type==MainForm.CLOSE_ONE
						        && dockPanel1.ActiveContent == contents[num])
							contents[num].DockHandler.Close();
						else if(type==MainForm.CLOSE_OTHER
						        && dockPanel1.ActiveContent != contents[num])
							contents[num].DockHandler.Close();
					}
					num--;
				}
			}catch{}
		}
		void CloseOtherToolStripMenuItemClick(object sender, EventArgs e)
		{
			CloseMdi(MainForm.CLOSE_OTHER);
		}
		
		void CloseAllToolStripMenuItemClick(object sender, EventArgs e)
		{
			CloseMdi(MainForm.CLOSE_ALL);
		}
		#endregion
		
		#region file
		void Menuitem_openClick(object sender, EventArgs e)
		{
			using(OpenFileDialog dlg=new OpenFileDialog())
			{
				dlg.Title=LANG.GetMsg(LMSG.SelectDataBasePath);
				dlg.Filter=LANG.GetMsg(LMSG.CdbType);
				if(dlg.ShowDialog()==DialogResult.OK)
				{
					Open(dlg.FileName);
				}
			}
		}
		
		void QuitToolStripMenuItemClick(object sender, EventArgs e)
		{
			Application.Exit();
		}
		
		void Menuitem_openLastDataBaseClick(object sender, EventArgs e)
		{
			string cdb=System.Configuration.ConfigurationManager.AppSettings["cdb"];
			if(File.Exists(cdb))
				Open(cdb);
		}
		
		void Menuitem_newClick(object sender, EventArgs e)
		{
			using(SaveFileDialog dlg=new SaveFileDialog())
			{
				dlg.Title=LANG.GetMsg(LMSG.SelectDataBasePath);
				dlg.Filter=LANG.GetMsg(LMSG.CdbType);
				if(dlg.ShowDialog()==DialogResult.OK)
				{
					if(DataBase.Create(dlg.FileName))
					{
						if(MyMsg.Question(LMSG.IfOpenDataBase))
							Open(dlg.FileName);
					}
				}
			}
		}
		#endregion
		
		#region copy
		DataEditForm GetActive()
		{
			foreach(DataEditForm df in list.Keys)
			{
				if(df==dockPanel1.ActiveContent)
					return df;
			}
			return null;
		}
		
		void Menuitem_copyselecttoClick(object sender, EventArgs e)
		{
			DataEditForm df =GetActive();
			if(df!=null)
			{
				tCards=df.getCardList(true);
				if(tCards!=null){
					SetCopyNumber(tCards.Length);
					MyMsg.Show(LMSG.CopyCards);
				}
			}
		}
		void SetCopyNumber(int c)
		{
			string tmp=menuitem_pastecards.Text;
			int t=tmp.LastIndexOf(" (");
			if(t>0)
				tmp=tmp.Substring(0,t);
			tmp=tmp+" ("+c.ToString()+")";
			menuitem_pastecards.Text=tmp;
		}

		void Menuitem_pastecardsClick(object sender, EventArgs e)
		{
			if(tCards==null)
				return;
			DataEditForm df =GetActive();
			if(df==null)
				return;
			df.SaveCards(tCards);
			MyMsg.Show(LMSG.PasteCards);
		}
	
		#endregion
	}
}